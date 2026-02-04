namespace Glyph.Terminal;

using System.Runtime.InteropServices;

#region Enums

/// <summary>
/// Keyboard modifier flags.
/// </summary>
[Flags]
public enum KeyModifiers
{
	None = 0,
	Shift = 1 << 0,
	Alt = 1 << 1,
	Ctrl = 1 << 2
}

/// <summary>
/// Mouse button identifiers.
/// </summary>
public enum MouseButton
{
	None = 0,
	Left = 1,
	Middle = 2,
	Right = 3,
	ScrollUp = 4,
	ScrollDown = 5,
	ScrollLeft = 6,
	ScrollRight = 7
}

/// <summary>
/// Mouse action types.
/// </summary>
public enum MouseAction
{
	Press,
	Release,
	Move,
	Scroll
}

/// <summary>
/// Scroll direction for mouse wheel events.
/// </summary>
public enum ScrollDirection
{
	None,
	Up,
	Down,
	Left,
	Right
}

/// <summary>
/// Extended key codes for special keys not covered by ConsoleKey.
/// </summary>
public enum ExtendedKey
{
	None = 0,

	// Standard keys (mapped from ConsoleKey)
	Backspace = ConsoleKey.Backspace,
	Tab = ConsoleKey.Tab,
	Enter = ConsoleKey.Enter,
	Escape = ConsoleKey.Escape,
	Spacebar = ConsoleKey.Spacebar,
	Delete = ConsoleKey.Delete,
	Insert = ConsoleKey.Insert,
	Home = ConsoleKey.Home,
	End = ConsoleKey.End,
	PageUp = ConsoleKey.PageUp,
	PageDown = ConsoleKey.PageDown,

	// Arrow keys
	UpArrow = ConsoleKey.UpArrow,
	DownArrow = ConsoleKey.DownArrow,
	LeftArrow = ConsoleKey.LeftArrow,
	RightArrow = ConsoleKey.RightArrow,

	// Function keys
	F1 = ConsoleKey.F1,
	F2 = ConsoleKey.F2,
	F3 = ConsoleKey.F3,
	F4 = ConsoleKey.F4,
	F5 = ConsoleKey.F5,
	F6 = ConsoleKey.F6,
	F7 = ConsoleKey.F7,
	F8 = ConsoleKey.F8,
	F9 = ConsoleKey.F9,
	F10 = ConsoleKey.F10,
	F11 = ConsoleKey.F11,
	F12 = ConsoleKey.F12,

	// Extended function keys (F13-F24 for completeness)
	F13 = ConsoleKey.F13,
	F14 = ConsoleKey.F14,
	F15 = ConsoleKey.F15,
	F16 = ConsoleKey.F16,
	F17 = ConsoleKey.F17,
	F18 = ConsoleKey.F18,
	F19 = ConsoleKey.F19,
	F20 = ConsoleKey.F20,
	F21 = ConsoleKey.F21,
	F22 = ConsoleKey.F22,
	F23 = ConsoleKey.F23,
	F24 = ConsoleKey.F24,

	// Special extended keys
	ShiftEnter = 1000,
	ShiftTab = 1001,
}

#endregion

#region KeyInfo

/// <summary>
/// Represents a keyboard input event with key, character, and modifier information.
/// </summary>
public readonly record struct KeyInfo
{
	/// <summary>
	/// The extended key code.
	/// </summary>
	public ExtendedKey Key { get; init; }

	/// <summary>
	/// The character representation of the key, if applicable.
	/// </summary>
	public char Char { get; init; }

	/// <summary>
	/// The modifier keys held during this key press.
	/// </summary>
	public KeyModifiers Modifiers { get; init; }

	/// <summary>
	/// The original ConsoleKey value, if available.
	/// </summary>
	public ConsoleKey ConsoleKey { get; init; }

	/// <summary>
	/// Whether the Ctrl modifier is active.
	/// </summary>
	public bool IsCtrl => (Modifiers & KeyModifiers.Ctrl) != 0;

	/// <summary>
	/// Whether the Alt modifier is active.
	/// </summary>
	public bool IsAlt => (Modifiers & KeyModifiers.Alt) != 0;

	/// <summary>
	/// Whether the Shift modifier is active.
	/// </summary>
	public bool IsShift => (Modifiers & KeyModifiers.Shift) != 0;

	/// <summary>
	/// Whether this is a printable character.
	/// </summary>
	public bool IsPrintable => !char.IsControl(Char) && Char != '\0';

	/// <summary>
	/// Whether this is a Ctrl+letter combination.
	/// </summary>
	public bool IsCtrlLetter => IsCtrl && Char >= '\x01' && Char <= '\x1A';

	/// <summary>
	/// Gets the letter for a Ctrl+letter combination (e.g., Ctrl+C returns 'C').
	/// </summary>
	public char CtrlLetter => IsCtrlLetter ? (char)(Char + 'A' - 1) : '\0';

	/// <summary>
	/// Creates a KeyInfo from a ConsoleKeyInfo.
	/// </summary>
	public static KeyInfo FromConsoleKeyInfo(ConsoleKeyInfo cki)
	{
		var modifiers = KeyModifiers.None;
		if ((cki.Modifiers & ConsoleModifiers.Shift) != 0)
		{
			modifiers |= KeyModifiers.Shift;
		}
		if ((cki.Modifiers & ConsoleModifiers.Alt) != 0)
		{
			modifiers |= KeyModifiers.Alt;
		}
		if ((cki.Modifiers & ConsoleModifiers.Control) != 0)
		{
			modifiers |= KeyModifiers.Ctrl;
		}

		var extKey = (ExtendedKey)cki.Key;

		// Detect Shift+Enter
		if (cki.Key == ConsoleKey.Enter && (cki.Modifiers & ConsoleModifiers.Shift) != 0)
		{
			extKey = ExtendedKey.ShiftEnter;
		}
		// Detect Shift+Tab
		else if (cki.Key == ConsoleKey.Tab && (cki.Modifiers & ConsoleModifiers.Shift) != 0)
		{
			extKey = ExtendedKey.ShiftTab;
		}

		return new KeyInfo
		{
			Key = extKey,
			Char = cki.KeyChar,
			Modifiers = modifiers,
			ConsoleKey = cki.Key
		};
	}

	public override string ToString()
	{
		var parts = new List<string>();
		if (IsCtrl) parts.Add("Ctrl");
		if (IsAlt) parts.Add("Alt");
		if (IsShift) parts.Add("Shift");

		if (Key != ExtendedKey.None)
		{
			parts.Add(Key.ToString());
		}
		else if (IsPrintable)
		{
			parts.Add($"'{Char}'");
		}
		else if (Char != '\0')
		{
			parts.Add($"0x{(int)Char:X2}");
		}

		return string.Join("+", parts);
	}
}

#endregion

#region MouseInfo

/// <summary>
/// Represents a mouse input event.
/// </summary>
public readonly record struct MouseInfo
{
	/// <summary>
	/// X coordinate (column), 0-based.
	/// </summary>
	public int X { get; init; }

	/// <summary>
	/// Y coordinate (row), 0-based.
	/// </summary>
	public int Y { get; init; }

	/// <summary>
	/// The mouse button involved in this event.
	/// </summary>
	public MouseButton Button { get; init; }

	/// <summary>
	/// The type of mouse action.
	/// </summary>
	public MouseAction Action { get; init; }

	/// <summary>
	/// Scroll direction for wheel events.
	/// </summary>
	public ScrollDirection ScrollDirection { get; init; }

	/// <summary>
	/// Modifier keys held during this mouse event.
	/// </summary>
	public KeyModifiers Modifiers { get; init; }

	/// <summary>
	/// Whether the Ctrl modifier is active.
	/// </summary>
	public bool IsCtrl => (Modifiers & KeyModifiers.Ctrl) != 0;

	/// <summary>
	/// Whether the Alt modifier is active.
	/// </summary>
	public bool IsAlt => (Modifiers & KeyModifiers.Alt) != 0;

	/// <summary>
	/// Whether the Shift modifier is active.
	/// </summary>
	public bool IsShift => (Modifiers & KeyModifiers.Shift) != 0;

	/// <summary>
	/// Whether this is a scroll event.
	/// </summary>
	public bool IsScroll => Action == MouseAction.Scroll;

	/// <summary>
	/// Whether this is a click event (press).
	/// </summary>
	public bool IsClick => Action == MouseAction.Press;

	public override string ToString()
	{
		var parts = new List<string>();
		if (IsCtrl) parts.Add("Ctrl");
		if (IsAlt) parts.Add("Alt");
		if (IsShift) parts.Add("Shift");

		parts.Add(Action.ToString());

		if (Action == MouseAction.Scroll)
		{
			parts.Add(ScrollDirection.ToString());
		}
		else
		{
			parts.Add(Button.ToString());
		}

		parts.Add($"@({X},{Y})");

		return string.Join("+", parts);
	}
}

#endregion

#region Event Args

/// <summary>
/// Event arguments for key press events (used by InputReader).
/// </summary>
public sealed class KeyPressedEventArgs : EventArgs
{
	public KeyInfo Key { get; }
	public bool Handled { get; set; }

	public KeyPressedEventArgs(KeyInfo key)
	{
		Key = key;
	}
}

// Note: MouseEventArgs is defined in View.cs to avoid duplication

#endregion

#region InputReader

/// <summary>
/// Handles terminal input including keyboard and mouse events.
/// Supports both Windows and Unix terminals with ANSI sequence parsing.
/// </summary>
public sealed class InputReader : IDisposable
{
	private static readonly bool _IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
	private static readonly bool _IsUnix = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
	                                        RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

	// Static instance for Application.cs compatibility
	private static readonly InputReader _StaticInstance = new();

	private bool _MouseTrackingEnabled;
	private bool _RawModeEnabled;
	private bool _Disposed;

	// For non-blocking reads
	private readonly Queue<char> _InputBuffer = new();
	private readonly object _BufferLock = new();

	// Escape sequence buffer for parsing
	private readonly List<char> _EscapeBuffer = new();

	// ANSI escape sequences for mouse tracking
	private const string MouseTrackingOn = "\x1b[?1000h";   // Basic mouse tracking
	private const string MouseSgrOn = "\x1b[?1006h";        // SGR extended mode (coordinates > 223)
	private const string MouseMotionOn = "\x1b[?1003h";     // All motion events
	private const string MouseTrackingOff = "\x1b[?1000l";
	private const string MouseSgrOff = "\x1b[?1006l";
	private const string MouseMotionOff = "\x1b[?1003l";

	/// <summary>
	/// Fired when a key is pressed.
	/// </summary>
	public event EventHandler<KeyPressedEventArgs>? OnKeyPressed;

	/// <summary>
	/// Fired when a mouse event occurs.
	/// </summary>
	public event EventHandler<MouseEventArgs>? OnMouseEvent;

	/// <summary>
	/// Whether mouse tracking is currently enabled.
	/// </summary>
	public bool IsMouseTrackingEnabled => _MouseTrackingEnabled;

	/// <summary>
	/// Whether raw mode is currently enabled.
	/// </summary>
	public bool IsRawModeEnabled => _RawModeEnabled;

	#region Static API for Application.cs

	/// <summary>
	/// Reads the next input event (key or mouse) from the console.
	/// Returns either KeyInfo or MouseInfo based on what was read.
	/// </summary>
	public static object? Read()
	{
		if (!Console.KeyAvailable)
		{
			return null;
		}

		var cki = Console.ReadKey(intercept: true);

		// Check for escape sequence (mouse or special keys)
		if (cki.Key == ConsoleKey.Escape)
		{
			return _StaticInstance.ParseEscapeSequenceStatic(cki);
		}

		return KeyInfo.FromConsoleKeyInfo(cki);
	}

	/// <summary>
	/// Parses escape sequences statically for the Read() method.
	/// </summary>
	private object? ParseEscapeSequenceStatic(ConsoleKeyInfo escapeKey)
	{
		// Check if more input is available
		if (!Console.KeyAvailable)
		{
			// Just a bare ESC key
			return new KeyInfo
			{
				Key = ExtendedKey.Escape,
				Char = '\x1b',
				Modifiers = KeyModifiers.None,
				ConsoleKey = ConsoleKey.Escape
			};
		}

		// Read the next character
		var next = Console.ReadKey(intercept: true);

		// CSI sequence: ESC [
		if (next.KeyChar == '[')
		{
			return ParseCsiSequenceStatic();
		}

		// SS3 sequence: ESC O (used by some terminals for function keys)
		if (next.KeyChar == 'O')
		{
			return ParseSs3SequenceStatic();
		}

		// Alt+key: ESC followed by a character
		if (next.KeyChar != '\0')
		{
			var modifiers = KeyModifiers.Alt;
			if ((next.Modifiers & ConsoleModifiers.Shift) != 0)
			{
				modifiers |= KeyModifiers.Shift;
			}
			if ((next.Modifiers & ConsoleModifiers.Control) != 0)
			{
				modifiers |= KeyModifiers.Ctrl;
			}

			return new KeyInfo
			{
				Key = (ExtendedKey)next.Key,
				Char = next.KeyChar,
				Modifiers = modifiers,
				ConsoleKey = next.Key
			};
		}

		return null;
	}

	/// <summary>
	/// Parses CSI sequences for static Read().
	/// </summary>
	private object? ParseCsiSequenceStatic()
	{
		var parameters = new List<int>();
		var currentParam = 0;
		var hasParam = false;

		while (Console.KeyAvailable)
		{
			var c = Console.ReadKey(intercept: true).KeyChar;

			// Check for mouse SGR sequence: ESC [ <
			if (c == '<' && parameters.Count == 0 && !hasParam)
			{
				return ParseMouseSgrSequence();
			}

			// Parameter bytes (0-9, ;)
			if (c >= '0' && c <= '9')
			{
				currentParam = currentParam * 10 + (c - '0');
				hasParam = true;
				continue;
			}

			if (c == ';')
			{
				parameters.Add(hasParam ? currentParam : 0);
				currentParam = 0;
				hasParam = false;
				continue;
			}

			// Final byte
			if (hasParam || parameters.Count > 0)
			{
				parameters.Add(hasParam ? currentParam : 0);
			}

			return MapCsiFinalByte(c, parameters);
		}

		return null;
	}

	/// <summary>
	/// Parses mouse SGR extended mode sequence.
	/// Format: ESC [ < button ; x ; y M (press) or m (release)
	/// </summary>
	private MouseInfo? ParseMouseSgrSequence()
	{
		var buttonStr = ReadUntilStatic(';');
		var xStr = ReadUntilStatic(';');
		var yAndFinal = ReadUntilFinalStatic();

		if (buttonStr == null || xStr == null || yAndFinal == null)
		{
			return null;
		}

		if (!int.TryParse(buttonStr, out var buttonCode) ||
		    !int.TryParse(xStr, out var x) ||
		    !int.TryParse(yAndFinal.Value.Str, out var y))
		{
			return null;
		}

		// Coordinates are 1-based, convert to 0-based
		x--;
		y--;

		var isRelease = yAndFinal.Value.FinalChar == 'm';

		return ParseSgrMouseCode(buttonCode, x, y, isRelease);
	}

	private static string? ReadUntilStatic(char delimiter)
	{
		var result = new List<char>();
		while (Console.KeyAvailable)
		{
			var c = Console.ReadKey(intercept: true).KeyChar;
			if (c == delimiter)
			{
				return new string(result.ToArray());
			}
			result.Add(c);
		}
		return null;
	}

	private static (string Str, char FinalChar)? ReadUntilFinalStatic()
	{
		var result = new List<char>();
		while (Console.KeyAvailable)
		{
			var c = Console.ReadKey(intercept: true).KeyChar;
			if (c == 'M' || c == 'm')
			{
				return (new string(result.ToArray()), c);
			}
			result.Add(c);
		}
		return null;
	}

	/// <summary>
	/// Parses SS3 sequences for static Read().
	/// </summary>
	private KeyInfo? ParseSs3SequenceStatic()
	{
		if (!Console.KeyAvailable)
		{
			return null;
		}

		var c = Console.ReadKey(intercept: true).KeyChar;

		ExtendedKey key;
		ConsoleKey consoleKey;

		switch (c)
		{
			// Function keys in SS3 mode
			case 'P':
				key = ExtendedKey.F1;
				consoleKey = ConsoleKey.F1;
				break;
			case 'Q':
				key = ExtendedKey.F2;
				consoleKey = ConsoleKey.F2;
				break;
			case 'R':
				key = ExtendedKey.F3;
				consoleKey = ConsoleKey.F3;
				break;
			case 'S':
				key = ExtendedKey.F4;
				consoleKey = ConsoleKey.F4;
				break;

			// Keypad navigation
			case 'H':
				key = ExtendedKey.Home;
				consoleKey = ConsoleKey.Home;
				break;
			case 'F':
				key = ExtendedKey.End;
				consoleKey = ConsoleKey.End;
				break;

			// Arrow keys (some terminals)
			case 'A':
				key = ExtendedKey.UpArrow;
				consoleKey = ConsoleKey.UpArrow;
				break;
			case 'B':
				key = ExtendedKey.DownArrow;
				consoleKey = ConsoleKey.DownArrow;
				break;
			case 'C':
				key = ExtendedKey.RightArrow;
				consoleKey = ConsoleKey.RightArrow;
				break;
			case 'D':
				key = ExtendedKey.LeftArrow;
				consoleKey = ConsoleKey.LeftArrow;
				break;

			default:
				return null;
		}

		return new KeyInfo
		{
			Key = key,
			Char = '\0',
			Modifiers = KeyModifiers.None,
			ConsoleKey = consoleKey
		};
	}

	#endregion

	/// <summary>
	/// Enables mouse tracking in the terminal.
	/// </summary>
	/// <param name="trackMotion">Whether to track all mouse motion, not just clicks.</param>
	public void EnableMouseTracking(bool trackMotion = false)
	{
		if (_MouseTrackingEnabled)
		{
			return;
		}

		Console.Write(MouseTrackingOn);
		Console.Write(MouseSgrOn);

		if (trackMotion)
		{
			Console.Write(MouseMotionOn);
		}

		_MouseTrackingEnabled = true;
	}

	/// <summary>
	/// Disables mouse tracking in the terminal.
	/// </summary>
	public void DisableMouseTracking()
	{
		if (!_MouseTrackingEnabled)
		{
			return;
		}

		Console.Write(MouseMotionOff);
		Console.Write(MouseSgrOff);
		Console.Write(MouseTrackingOff);

		_MouseTrackingEnabled = false;
	}

	/// <summary>
	/// Enables raw mode (disables line buffering and echo).
	/// Note: On .NET, Console.ReadKey(true) already provides some raw mode behavior.
	/// Full raw mode requires platform-specific handling.
	/// </summary>
	public void EnableRawMode()
	{
		if (_RawModeEnabled)
		{
			return;
		}

		// .NET's Console.ReadKey(true) already handles most raw mode needs
		// For full raw mode on Unix, you would need to use termios
		// For now, we mark it as enabled and rely on Console.ReadKey behavior
		_RawModeEnabled = true;
	}

	/// <summary>
	/// Disables raw mode.
	/// </summary>
	public void DisableRawMode()
	{
		if (!_RawModeEnabled)
		{
			return;
		}

		_RawModeEnabled = false;
	}

	/// <summary>
	/// Reads a key from the console, blocking until a key is available.
	/// Handles ANSI escape sequences for special keys.
	/// </summary>
	/// <returns>The key information.</returns>
	public KeyInfo ReadKey()
	{
		while (true)
		{
			var cki = Console.ReadKey(intercept: true);

			// Check for escape sequence
			if (cki.Key == ConsoleKey.Escape)
			{
				var result = TryParseEscapeSequence(cki);
				if (result.HasValue)
				{
					var args = new KeyPressedEventArgs(result.Value);
					OnKeyPressed?.Invoke(this, args);
					return result.Value;
				}
			}

			var keyInfo = KeyInfo.FromConsoleKeyInfo(cki);
			var eventArgs = new KeyPressedEventArgs(keyInfo);
			OnKeyPressed?.Invoke(this, eventArgs);
			return keyInfo;
		}
	}

	/// <summary>
	/// Attempts to read a key without blocking.
	/// </summary>
	/// <param name="keyInfo">The key information if available.</param>
	/// <returns>True if a key was read, false otherwise.</returns>
	public bool TryReadKey(out KeyInfo keyInfo)
	{
		keyInfo = default;

		if (!Console.KeyAvailable)
		{
			return false;
		}

		keyInfo = ReadKey();
		return true;
	}

	/// <summary>
	/// Attempts to read and parse a mouse event from the input stream.
	/// Mouse events come as escape sequences when mouse tracking is enabled.
	/// </summary>
	/// <param name="mouseInfo">The parsed mouse information.</param>
	/// <returns>True if a mouse event was parsed, false otherwise.</returns>
	public bool TryReadMouse(out MouseInfo mouseInfo)
	{
		mouseInfo = default;

		if (!_MouseTrackingEnabled)
		{
			return false;
		}

		if (!Console.KeyAvailable)
		{
			return false;
		}

		var cki = Console.ReadKey(intercept: true);

		if (cki.Key != ConsoleKey.Escape)
		{
			// Not an escape sequence, buffer it for later
			lock (_BufferLock)
			{
				_InputBuffer.Enqueue(cki.KeyChar);
			}
			return false;
		}

		// Try to parse as mouse sequence
		var result = TryParseMouseSequence();
		if (result.HasValue)
		{
			mouseInfo = result.Value;
			var args = new MouseEventArgs(mouseInfo);
			OnMouseEvent?.Invoke(this, args);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Reads input in a loop, dispatching to event handlers.
	/// Call from a dedicated thread or use with async patterns.
	/// </summary>
	/// <param name="cancellationToken">Token to stop the input loop.</param>
	public void StartInputLoop(CancellationToken cancellationToken = default)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			if (Console.KeyAvailable)
			{
				var cki = Console.ReadKey(intercept: true);

				if (cki.Key == ConsoleKey.Escape)
				{
					// Could be mouse or special key
					var mouseResult = TryParseMouseSequence();
					if (mouseResult.HasValue)
					{
						var mouseArgs = new MouseEventArgs(mouseResult.Value);
						OnMouseEvent?.Invoke(this, mouseArgs);
						continue;
					}

					var keyResult = TryParseEscapeSequence(cki);
					if (keyResult.HasValue)
					{
						var keyArgs = new KeyPressedEventArgs(keyResult.Value);
						OnKeyPressed?.Invoke(this, keyArgs);
						continue;
					}
				}

				var keyInfo = KeyInfo.FromConsoleKeyInfo(cki);
				var eventArgs = new KeyPressedEventArgs(keyInfo);
				OnKeyPressed?.Invoke(this, eventArgs);
			}
			else
			{
				// Small delay to prevent busy-waiting
				Thread.Sleep(10);
			}
		}
	}

	/// <summary>
	/// Async version of the input loop.
	/// </summary>
	public async Task StartInputLoopAsync(CancellationToken cancellationToken = default)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			if (Console.KeyAvailable)
			{
				var cki = Console.ReadKey(intercept: true);

				if (cki.Key == ConsoleKey.Escape)
				{
					var mouseResult = TryParseMouseSequence();
					if (mouseResult.HasValue)
					{
						var mouseArgs = new MouseEventArgs(mouseResult.Value);
						OnMouseEvent?.Invoke(this, mouseArgs);
						continue;
					}

					var keyResult = TryParseEscapeSequence(cki);
					if (keyResult.HasValue)
					{
						var keyArgs = new KeyPressedEventArgs(keyResult.Value);
						OnKeyPressed?.Invoke(this, keyArgs);
						continue;
					}
				}

				var keyInfo = KeyInfo.FromConsoleKeyInfo(cki);
				var eventArgs = new KeyPressedEventArgs(keyInfo);
				OnKeyPressed?.Invoke(this, eventArgs);
			}
			else
			{
				await Task.Delay(10, cancellationToken);
			}
		}
	}

	#region Escape Sequence Parsing

	/// <summary>
	/// Attempts to parse an escape sequence after ESC has been read.
	/// Handles arrow keys, function keys, and other special keys.
	/// </summary>
	private KeyInfo? TryParseEscapeSequence(ConsoleKeyInfo escapeKey)
	{
		// Check if more input is available
		if (!Console.KeyAvailable)
		{
			// Just a bare ESC key
			return new KeyInfo
			{
				Key = ExtendedKey.Escape,
				Char = '\x1b',
				Modifiers = KeyModifiers.None,
				ConsoleKey = ConsoleKey.Escape
			};
		}

		// Read the next character
		var next = Console.ReadKey(intercept: true);

		// CSI sequence: ESC [
		if (next.KeyChar == '[')
		{
			return ParseCsiSequence();
		}

		// SS3 sequence: ESC O (used by some terminals for function keys)
		if (next.KeyChar == 'O')
		{
			return ParseSs3Sequence();
		}

		// Alt+key: ESC followed by a character
		if (next.KeyChar != '\0')
		{
			var modifiers = KeyModifiers.Alt;
			if ((next.Modifiers & ConsoleModifiers.Shift) != 0)
			{
				modifiers |= KeyModifiers.Shift;
			}
			if ((next.Modifiers & ConsoleModifiers.Control) != 0)
			{
				modifiers |= KeyModifiers.Ctrl;
			}

			return new KeyInfo
			{
				Key = (ExtendedKey)next.Key,
				Char = next.KeyChar,
				Modifiers = modifiers,
				ConsoleKey = next.Key
			};
		}

		return null;
	}

	/// <summary>
	/// Parses a CSI (Control Sequence Introducer) sequence.
	/// Format: ESC [ [params] [intermediates] final
	/// </summary>
	private KeyInfo? ParseCsiSequence()
	{
		var parameters = new List<int>();
		var currentParam = 0;
		var hasParam = false;

		while (Console.KeyAvailable)
		{
			var c = Console.ReadKey(intercept: true).KeyChar;

			// Parameter bytes (0-9, ;)
			if (c >= '0' && c <= '9')
			{
				currentParam = currentParam * 10 + (c - '0');
				hasParam = true;
				continue;
			}

			if (c == ';')
			{
				parameters.Add(hasParam ? currentParam : 0);
				currentParam = 0;
				hasParam = false;
				continue;
			}

			// Final byte
			if (hasParam || parameters.Count > 0)
			{
				parameters.Add(hasParam ? currentParam : 0);
			}

			return MapCsiFinalByte(c, parameters);
		}

		return null;
	}

	/// <summary>
	/// Maps the final byte of a CSI sequence to a KeyInfo.
	/// </summary>
	private KeyInfo? MapCsiFinalByte(char finalByte, List<int> parameters)
	{
		var modifiers = KeyModifiers.None;

		// Modifier parameter is usually the second parameter
		if (parameters.Count >= 2)
		{
			var modParam = parameters[1] - 1; // Modifier parameter is 1-based
			if ((modParam & 1) != 0) modifiers |= KeyModifiers.Shift;
			if ((modParam & 2) != 0) modifiers |= KeyModifiers.Alt;
			if ((modParam & 4) != 0) modifiers |= KeyModifiers.Ctrl;
		}

		ExtendedKey key;
		ConsoleKey consoleKey;

		switch (finalByte)
		{
			// Arrow keys
			case 'A':
				key = ExtendedKey.UpArrow;
				consoleKey = ConsoleKey.UpArrow;
				break;
			case 'B':
				key = ExtendedKey.DownArrow;
				consoleKey = ConsoleKey.DownArrow;
				break;
			case 'C':
				key = ExtendedKey.RightArrow;
				consoleKey = ConsoleKey.RightArrow;
				break;
			case 'D':
				key = ExtendedKey.LeftArrow;
				consoleKey = ConsoleKey.LeftArrow;
				break;

			// Home/End
			case 'H':
				key = ExtendedKey.Home;
				consoleKey = ConsoleKey.Home;
				break;
			case 'F':
				key = ExtendedKey.End;
				consoleKey = ConsoleKey.End;
				break;

			// Shifted keys with Z
			case 'Z':
				key = ExtendedKey.ShiftTab;
				consoleKey = ConsoleKey.Tab;
				modifiers |= KeyModifiers.Shift;
				break;

			// Tilde sequences: ESC [ number ~
			case '~':
				if (parameters.Count > 0)
				{
					return MapTildeSequence(parameters[0], modifiers);
				}
				return null;

			default:
				return null;
		}

		return new KeyInfo
		{
			Key = key,
			Char = '\0',
			Modifiers = modifiers,
			ConsoleKey = consoleKey
		};
	}

	/// <summary>
	/// Maps tilde sequences (ESC [ number ~) to keys.
	/// </summary>
	private KeyInfo? MapTildeSequence(int code, KeyModifiers modifiers)
	{
		ExtendedKey key;
		ConsoleKey consoleKey;

		switch (code)
		{
			case 1:
				key = ExtendedKey.Home;
				consoleKey = ConsoleKey.Home;
				break;
			case 2:
				key = ExtendedKey.Insert;
				consoleKey = ConsoleKey.Insert;
				break;
			case 3:
				key = ExtendedKey.Delete;
				consoleKey = ConsoleKey.Delete;
				break;
			case 4:
				key = ExtendedKey.End;
				consoleKey = ConsoleKey.End;
				break;
			case 5:
				key = ExtendedKey.PageUp;
				consoleKey = ConsoleKey.PageUp;
				break;
			case 6:
				key = ExtendedKey.PageDown;
				consoleKey = ConsoleKey.PageDown;
				break;
			case 7:
				key = ExtendedKey.Home;
				consoleKey = ConsoleKey.Home;
				break;
			case 8:
				key = ExtendedKey.End;
				consoleKey = ConsoleKey.End;
				break;

			// Function keys
			case 11:
				key = ExtendedKey.F1;
				consoleKey = ConsoleKey.F1;
				break;
			case 12:
				key = ExtendedKey.F2;
				consoleKey = ConsoleKey.F2;
				break;
			case 13:
				key = ExtendedKey.F3;
				consoleKey = ConsoleKey.F3;
				break;
			case 14:
				key = ExtendedKey.F4;
				consoleKey = ConsoleKey.F4;
				break;
			case 15:
				key = ExtendedKey.F5;
				consoleKey = ConsoleKey.F5;
				break;
			case 17:
				key = ExtendedKey.F6;
				consoleKey = ConsoleKey.F6;
				break;
			case 18:
				key = ExtendedKey.F7;
				consoleKey = ConsoleKey.F7;
				break;
			case 19:
				key = ExtendedKey.F8;
				consoleKey = ConsoleKey.F8;
				break;
			case 20:
				key = ExtendedKey.F9;
				consoleKey = ConsoleKey.F9;
				break;
			case 21:
				key = ExtendedKey.F10;
				consoleKey = ConsoleKey.F10;
				break;
			case 23:
				key = ExtendedKey.F11;
				consoleKey = ConsoleKey.F11;
				break;
			case 24:
				key = ExtendedKey.F12;
				consoleKey = ConsoleKey.F12;
				break;

			// Extended function keys
			case 25:
				key = ExtendedKey.F13;
				consoleKey = ConsoleKey.F13;
				break;
			case 26:
				key = ExtendedKey.F14;
				consoleKey = ConsoleKey.F14;
				break;
			case 28:
				key = ExtendedKey.F15;
				consoleKey = ConsoleKey.F15;
				break;
			case 29:
				key = ExtendedKey.F16;
				consoleKey = ConsoleKey.F16;
				break;
			case 31:
				key = ExtendedKey.F17;
				consoleKey = ConsoleKey.F17;
				break;
			case 32:
				key = ExtendedKey.F18;
				consoleKey = ConsoleKey.F18;
				break;
			case 33:
				key = ExtendedKey.F19;
				consoleKey = ConsoleKey.F19;
				break;
			case 34:
				key = ExtendedKey.F20;
				consoleKey = ConsoleKey.F20;
				break;

			default:
				return null;
		}

		return new KeyInfo
		{
			Key = key,
			Char = '\0',
			Modifiers = modifiers,
			ConsoleKey = consoleKey
		};
	}

	/// <summary>
	/// Parses SS3 (Single Shift 3) sequences.
	/// Format: ESC O letter
	/// Used by some terminals for function keys and keypad.
	/// </summary>
	private KeyInfo? ParseSs3Sequence()
	{
		if (!Console.KeyAvailable)
		{
			return null;
		}

		var c = Console.ReadKey(intercept: true).KeyChar;

		ExtendedKey key;
		ConsoleKey consoleKey;

		switch (c)
		{
			// Function keys in SS3 mode
			case 'P':
				key = ExtendedKey.F1;
				consoleKey = ConsoleKey.F1;
				break;
			case 'Q':
				key = ExtendedKey.F2;
				consoleKey = ConsoleKey.F2;
				break;
			case 'R':
				key = ExtendedKey.F3;
				consoleKey = ConsoleKey.F3;
				break;
			case 'S':
				key = ExtendedKey.F4;
				consoleKey = ConsoleKey.F4;
				break;

			// Keypad navigation
			case 'H':
				key = ExtendedKey.Home;
				consoleKey = ConsoleKey.Home;
				break;
			case 'F':
				key = ExtendedKey.End;
				consoleKey = ConsoleKey.End;
				break;

			// Arrow keys (some terminals)
			case 'A':
				key = ExtendedKey.UpArrow;
				consoleKey = ConsoleKey.UpArrow;
				break;
			case 'B':
				key = ExtendedKey.DownArrow;
				consoleKey = ConsoleKey.DownArrow;
				break;
			case 'C':
				key = ExtendedKey.RightArrow;
				consoleKey = ConsoleKey.RightArrow;
				break;
			case 'D':
				key = ExtendedKey.LeftArrow;
				consoleKey = ConsoleKey.LeftArrow;
				break;

			default:
				return null;
		}

		return new KeyInfo
		{
			Key = key,
			Char = '\0',
			Modifiers = KeyModifiers.None,
			ConsoleKey = consoleKey
		};
	}

	#endregion

	#region Mouse Sequence Parsing

	/// <summary>
	/// Attempts to parse a mouse sequence.
	/// SGR extended mode format: ESC [ < button ; x ; y M (press) or m (release)
	/// </summary>
	private MouseInfo? TryParseMouseSequence()
	{
		if (!Console.KeyAvailable)
		{
			return null;
		}

		// Read '['
		var bracket = Console.ReadKey(intercept: true);
		if (bracket.KeyChar != '[')
		{
			return null;
		}

		if (!Console.KeyAvailable)
		{
			return null;
		}

		// Read '<' for SGR mode
		var lt = Console.ReadKey(intercept: true);
		if (lt.KeyChar != '<')
		{
			// Could be legacy X10 or normal mode - not implemented here
			return null;
		}

		// Parse: button ; x ; y M/m
		var buttonStr = ReadUntil(';');
		var xStr = ReadUntil(';');
		var yAndFinal = ReadUntilFinal();

		if (buttonStr == null || xStr == null || yAndFinal == null)
		{
			return null;
		}

		if (!int.TryParse(buttonStr, out var buttonCode) ||
		    !int.TryParse(xStr, out var x) ||
		    !int.TryParse(yAndFinal.Value.str, out var y))
		{
			return null;
		}

		// Coordinates are 1-based, convert to 0-based
		x--;
		y--;

		var isRelease = yAndFinal.Value.finalChar == 'm';

		return ParseSgrMouseCode(buttonCode, x, y, isRelease);
	}

	/// <summary>
	/// Reads characters until the specified delimiter.
	/// </summary>
	private string? ReadUntil(char delimiter)
	{
		var result = new List<char>();
		while (Console.KeyAvailable)
		{
			var c = Console.ReadKey(intercept: true).KeyChar;
			if (c == delimiter)
			{
				return new string(result.ToArray());
			}
			result.Add(c);
		}
		return null;
	}

	/// <summary>
	/// Reads characters until a final byte (M or m for mouse).
	/// </summary>
	private (string str, char finalChar)? ReadUntilFinal()
	{
		var result = new List<char>();
		while (Console.KeyAvailable)
		{
			var c = Console.ReadKey(intercept: true).KeyChar;
			if (c == 'M' || c == 'm')
			{
				return (new string(result.ToArray()), c);
			}
			result.Add(c);
		}
		return null;
	}

	/// <summary>
	/// Parses an SGR mouse button code into MouseInfo.
	/// Button code bits:
	///   0-1: button (0=left, 1=middle, 2=right, 3=release)
	///   2: shift
	///   3: meta/alt
	///   4: control
	///   5: motion
	///   6-7: scroll (64=scroll up, 65=scroll down)
	/// </summary>
	private MouseInfo ParseSgrMouseCode(int code, int x, int y, bool isRelease)
	{
		var modifiers = KeyModifiers.None;
		if ((code & 4) != 0) modifiers |= KeyModifiers.Shift;
		if ((code & 8) != 0) modifiers |= KeyModifiers.Alt;
		if ((code & 16) != 0) modifiers |= KeyModifiers.Ctrl;

		var isMotion = (code & 32) != 0;
		var isScroll = (code & 64) != 0;

		MouseButton button;
		MouseAction action;
		var scrollDirection = ScrollDirection.None;

		if (isScroll)
		{
			button = MouseButton.None;
			action = MouseAction.Scroll;

			var scrollCode = code & 3;
			scrollDirection = scrollCode switch
			{
				0 => ScrollDirection.Up,
				1 => ScrollDirection.Down,
				2 => ScrollDirection.Left,
				3 => ScrollDirection.Right,
				_ => ScrollDirection.None
			};

			// Also set button for convenience
			button = scrollDirection switch
			{
				ScrollDirection.Up => MouseButton.ScrollUp,
				ScrollDirection.Down => MouseButton.ScrollDown,
				ScrollDirection.Left => MouseButton.ScrollLeft,
				ScrollDirection.Right => MouseButton.ScrollRight,
				_ => MouseButton.None
			};
		}
		else
		{
			var buttonCode = code & 3;
			button = buttonCode switch
			{
				0 => MouseButton.Left,
				1 => MouseButton.Middle,
				2 => MouseButton.Right,
				3 => MouseButton.None,
				_ => MouseButton.None
			};

			if (isRelease)
			{
				action = MouseAction.Release;
			}
			else if (isMotion)
			{
				action = MouseAction.Move;
			}
			else
			{
				action = MouseAction.Press;
			}
		}

		return new MouseInfo
		{
			X = x,
			Y = y,
			Button = button,
			Action = action,
			ScrollDirection = scrollDirection,
			Modifiers = modifiers
		};
	}

	#endregion

	#region IDisposable

	public void Dispose()
	{
		if (_Disposed)
		{
			return;
		}

		if (_MouseTrackingEnabled)
		{
			DisableMouseTracking();
		}

		if (_RawModeEnabled)
		{
			DisableRawMode();
		}

		_Disposed = true;
	}

	#endregion
}

#endregion
