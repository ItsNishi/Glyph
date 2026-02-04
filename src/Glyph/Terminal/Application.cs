namespace Glyph.Terminal;

/// <summary>
/// Main application class for the Glyph TUI framework.
/// Manages the event loop, screen rendering, and input handling.
/// </summary>
public static class Application
{
	private static View? _RootView;
	private static View? _FocusedView;
	private static Screen? _Screen;
	private static volatile bool _Running;
	private static volatile bool _Initialized;
	private static int _LastWidth;
	private static int _LastHeight;

	private static readonly Queue<Action> _InvokeQueue = new();
	private static readonly object _InvokeLock = new();

	private static readonly List<TimerEntry> _Timers = new();
	private static readonly object _TimerLock = new();

	/// <summary>
	/// Event raised when the terminal is resized.
	/// </summary>
	public static event EventHandler<ResizeEventArgs>? OnResize;

	/// <summary>
	/// The currently focused view.
	/// </summary>
	public static View? FocusedView => _FocusedView;

	/// <summary>
	/// The root view of the application.
	/// </summary>
	public static View? RootView => _RootView;

	/// <summary>
	/// The screen buffer used for rendering.
	/// </summary>
	public static Screen? Screen => _Screen;

	/// <summary>
	/// Whether the application is currently running.
	/// </summary>
	public static bool IsRunning => _Running;

	/// <summary>
	/// Initializes the terminal for TUI mode.
	/// </summary>
	public static void Init()
	{
		if (_Initialized)
		{
			return;
		}

		// Enter alternate screen buffer
		Console.Write("\x1b[?1049h");

		// Hide cursor
		Console.Write("\x1b[?25l");

		// Enable mouse tracking (SGR extended mode)
		Console.Write("\x1b[?1000h"); // Enable mouse click tracking
		Console.Write("\x1b[?1002h"); // Enable mouse drag tracking
		Console.Write("\x1b[?1006h"); // Enable SGR extended mouse mode

		// Disable line buffering for immediate input
		Console.TreatControlCAsInput = true;

		_LastWidth = Console.WindowWidth;
		_LastHeight = Console.WindowHeight;
		_Screen = new Screen(_LastWidth, _LastHeight);

		_Initialized = true;
	}

	/// <summary>
	/// Restores the terminal to its normal state.
	/// </summary>
	public static void Shutdown()
	{
		if (!_Initialized)
		{
			return;
		}

		// Disable mouse tracking
		Console.Write("\x1b[?1006l");
		Console.Write("\x1b[?1002l");
		Console.Write("\x1b[?1000l");

		// Show cursor
		Console.Write("\x1b[?25h");

		// Exit alternate screen buffer
		Console.Write("\x1b[?1049l");

		// Reset text attributes
		Console.Write("\x1b[0m");

		Console.TreatControlCAsInput = false;

		_Screen = null;
		_RootView = null;
		_FocusedView = null;
		_Initialized = false;
		_Running = false;

		lock (_TimerLock)
		{
			_Timers.Clear();
		}

		lock (_InvokeLock)
		{
			_InvokeQueue.Clear();
		}
	}

	/// <summary>
	/// Runs the main application loop with the specified root view.
	/// </summary>
	public static void Run(View Root_View)
	{
		if (!_Initialized)
		{
			Init();
		}

		_RootView = Root_View;
		_Running = true;

		// Set root view to fill the screen
		_RootView.X = 0;
		_RootView.Y = 0;
		_RootView.Width = _LastWidth;
		_RootView.Height = _LastHeight;

		// Set initial focus to the first focusable view
		var First_Focusable = Root_View.FindNextFocusable();
		if (First_Focusable != null)
		{
			SetFocused(First_Focusable);
		}

		// Initial draw - RenderFull writes every cell so no pre-clear needed
		if (_Screen != null)
		{
			_Screen.Clear();
			_RootView.Draw(_Screen);
			_Screen.RenderFull();
		}

		try
		{
			MainLoop();
		}
		finally
		{
			Shutdown();
		}
	}

	/// <summary>
	/// Requests the application to stop running.
	/// </summary>
	public static void RequestStop()
	{
		_Running = false;
	}

	/// <summary>
	/// Sets the focused view.
	/// </summary>
	internal static void SetFocused(View? View)
	{
		if (_FocusedView == View)
		{
			return;
		}

		_FocusedView?.SetFocusState(false);
		_FocusedView = View;
		_FocusedView?.SetFocusState(true);
	}

	/// <summary>
	/// Moves focus to the next focusable view.
	/// </summary>
	public static void FocusNext()
	{
		if (_RootView == null)
		{
			return;
		}

		var Focusables = _RootView.GetFocusableViews();
		if (Focusables.Count == 0)
		{
			return;
		}

		if (_FocusedView == null)
		{
			SetFocused(Focusables[0]);
			return;
		}

		int Current_Index = Focusables.IndexOf(_FocusedView);
		int Next_Index = (Current_Index + 1) % Focusables.Count;
		SetFocused(Focusables[Next_Index]);
	}

	/// <summary>
	/// Moves focus to the previous focusable view.
	/// </summary>
	public static void FocusPrevious()
	{
		if (_RootView == null)
		{
			return;
		}

		var Focusables = _RootView.GetFocusableViews();
		if (Focusables.Count == 0)
		{
			return;
		}

		if (_FocusedView == null)
		{
			SetFocused(Focusables[^1]);
			return;
		}

		int Current_Index = Focusables.IndexOf(_FocusedView);
		int Prev_Index = Current_Index <= 0 ? Focusables.Count - 1 : Current_Index - 1;
		SetFocused(Focusables[Prev_Index]);
	}

	/// <summary>
	/// Queues an action to be invoked on the main thread.
	/// </summary>
	public static void Invoke(Action Action)
	{
		lock (_InvokeLock)
		{
			_InvokeQueue.Enqueue(Action);
		}
	}

	/// <summary>
	/// Adds a timer that fires after the specified interval.
	/// </summary>
	/// <param name="Interval">Time until the timer fires.</param>
	/// <param name="Callback">Callback to invoke. Return true to repeat, false to stop.</param>
	/// <returns>A token that can be used to cancel the timer.</returns>
	public static object AddTimeout(TimeSpan Interval, Func<bool> Callback)
	{
		var Entry = new TimerEntry
		{
			Interval = Interval,
			Callback = Callback,
			NextFire = DateTime.UtcNow + Interval,
			Cancelled = false
		};

		lock (_TimerLock)
		{
			_Timers.Add(Entry);
		}

		return Entry;
	}

	/// <summary>
	/// Cancels a previously added timer.
	/// </summary>
	public static void RemoveTimeout(object Token)
	{
		if (Token is TimerEntry Entry)
		{
			lock (_TimerLock)
			{
				Entry.Cancelled = true;
				_Timers.Remove(Entry);
			}
		}
	}

	/// <summary>
	/// Forces a full redraw of the screen.
	/// </summary>
	public static void Refresh()
	{
		_RootView?.SetNeedsDraw();
	}

	private static void MainLoop()
	{
		while (_Running)
		{
			// Check for terminal resize
			CheckResize();

			// Process queued invocations
			ProcessInvokeQueue();

			// Process timers
			ProcessTimers();

			// Read and handle input (non-blocking)
			ProcessInput();

			// Redraw if needed
			if (_RootView?.NeedsDraw == true && _Screen != null)
			{
				_Screen.Clear();
				_RootView.Draw(_Screen);
				_Screen.Render();
			}

			// Small sleep to prevent busy-waiting
			Thread.Sleep(10);
		}
	}

	private static void CheckResize()
	{
		int Current_Width = Console.WindowWidth;
		int Current_Height = Console.WindowHeight;

		if (Current_Width != _LastWidth || Current_Height != _LastHeight)
		{
			_LastWidth = Current_Width;
			_LastHeight = Current_Height;

			_Screen?.Resize(Current_Width, Current_Height);

			// Update root view dimensions
			if (_RootView != null)
			{
				_RootView.Width = Current_Width;
				_RootView.Height = Current_Height;
				_RootView.SetNeedsDraw();
			}

			// Notify listeners
			OnResize?.Invoke(null, new ResizeEventArgs(Current_Width, Current_Height));

			// Force immediate redraw after resize
			if (_RootView != null && _Screen != null)
			{
				_Screen.Clear();
				_RootView.Draw(_Screen);
				_Screen.RenderFull();
			}
		}
	}

	private static void ProcessInvokeQueue()
	{
		while (true)
		{
			Action? Action = null;

			lock (_InvokeLock)
			{
				if (_InvokeQueue.Count > 0)
				{
					Action = _InvokeQueue.Dequeue();
				}
			}

			if (Action == null)
			{
				break;
			}

			try
			{
				Action();
			}
			catch (Exception Ex)
			{
				// Log or handle exception - for now, continue
				System.Diagnostics.Debug.WriteLine($"Invoke exception: {Ex}");
			}
		}
	}

	private static void ProcessTimers()
	{
		var Now = DateTime.UtcNow;
		var To_Process = new List<TimerEntry>();

		lock (_TimerLock)
		{
			for (int i = _Timers.Count - 1; i >= 0; i--)
			{
				var Entry = _Timers[i];
				if (Entry.Cancelled)
				{
					_Timers.RemoveAt(i);
					continue;
				}

				if (Now >= Entry.NextFire)
				{
					To_Process.Add(Entry);
				}
			}
		}

		foreach (var Entry in To_Process)
		{
			if (Entry.Cancelled)
			{
				continue;
			}

			try
			{
				bool Repeat = Entry.Callback();
				if (Repeat && !Entry.Cancelled)
				{
					Entry.NextFire = DateTime.UtcNow + Entry.Interval;
				}
				else
				{
					lock (_TimerLock)
					{
						Entry.Cancelled = true;
						_Timers.Remove(Entry);
					}
				}
			}
			catch (Exception Ex)
			{
				System.Diagnostics.Debug.WriteLine($"Timer exception: {Ex}");
				lock (_TimerLock)
				{
					Entry.Cancelled = true;
					_Timers.Remove(Entry);
				}
			}
		}
	}

	private static void ProcessInput()
	{
		// Use InputReader if available, otherwise fall back to Console
		if (!Console.KeyAvailable)
		{
			return;
		}

		var Input = InputReader.Read();

		if (Input is KeyInfo Key)
		{
			HandleKeyInput(Key);
		}
		else if (Input is MouseInfo Mouse)
		{
			HandleMouseInput(Mouse);
		}
	}

	private static void HandleKeyInput(KeyInfo Key)
	{
		// Handle Tab for focus cycling
		if (Key.Key == ExtendedKey.Tab || Key.ConsoleKey == ConsoleKey.Tab)
		{
			if (Key.IsShift)
			{
				FocusPrevious();
			}
			else
			{
				FocusNext();
			}
			return;
		}

		// Handle Shift+Tab explicitly
		if (Key.Key == ExtendedKey.ShiftTab)
		{
			FocusPrevious();
			return;
		}

		// Dispatch to focused view
		if (_FocusedView != null)
		{
			if (_FocusedView.HandleKey(Key))
			{
				return;
			}
		}

		// If not handled by focused view, try root view
		_RootView?.HandleKey(Key);
	}

	private static void HandleMouseInput(MouseInfo Mouse)
	{
		// Find the view under the mouse and dispatch
		_RootView?.HandleMouse(Mouse);
	}

	private class TimerEntry
	{
		public TimeSpan Interval;
		public Func<bool> Callback = null!;
		public DateTime NextFire;
		public bool Cancelled;
	}
}

/// <summary>
/// Event args for terminal resize events.
/// </summary>
public class ResizeEventArgs : EventArgs
{
	public int Width { get; }
	public int Height { get; }

	public ResizeEventArgs(int Width, int Height)
	{
		this.Width = Width;
		this.Height = Height;
	}
}
