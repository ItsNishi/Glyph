using Glyph.Terminal;

namespace Glyph.Presets;

/// <summary>
/// Layout options for the status panel.
/// </summary>
public enum StatusLayout
{
	/// <summary>
	/// Status panel appears as a horizontal row below the output.
	/// </summary>
	Bottom,

	/// <summary>
	/// Status panel appears as a vertical column on the left side.
	/// </summary>
	Left,

	/// <summary>
	/// Status panel appears as a vertical column on the right side.
	/// </summary>
	Right
}

/// <summary>
/// Event args for command received events.
/// </summary>
public class CommandReceivedEventArgs : EventArgs
{
	/// <summary>
	/// The command text.
	/// </summary>
	public string Command { get; }

	public CommandReceivedEventArgs(string Command)
	{
		this.Command = Command;
	}
}

/// <summary>
/// A preset console window layout with output area, status panel, and command input.
/// Designed for command-line tools and utilities.
/// </summary>
public class ConsoleWindow : View, IDisposable
{
	private readonly OutputView _OutputView;
	private readonly StatusPanel _StatusPanel;
	private readonly CommandInput _CommandInput;
	private string _Title = string.Empty;
	private int _StatusPanelHeight = 4;
	private int _StatusPanelWidth = 25;
	private StatusLayout _StatusLayout = StatusLayout.Bottom;

	// Color properties
	private Color _BackgroundColor = Color.Default;
	private Color _BorderColor = Color.BrightBlack;
	private Color _TitleColor = Color.BrightWhite;
	private Color _TextColor = Color.White;
	private Color _SeparatorLabelColor = Color.Yellow;
	private bool _Disposed;

	/// <summary>
	/// Event raised when the user submits a command.
	/// </summary>
	public event EventHandler<CommandReceivedEventArgs>? CommandReceived;

	/// <summary>
	/// The title displayed in the window header.
	/// </summary>
	public string Title
	{
		get => _Title;
		set
		{
			_Title = value ?? string.Empty;
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Height of the status panel when using Bottom layout (default 4).
	/// </summary>
	public int StatusPanelHeight
	{
		get => _StatusPanelHeight;
		set
		{
			_StatusPanelHeight = Math.Max(1, value);
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Width of the status panel when using Left or Right layout (default 25).
	/// </summary>
	public int StatusPanelWidth
	{
		get => _StatusPanelWidth;
		set
		{
			_StatusPanelWidth = Math.Max(10, value);
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Layout position for the status panel (Bottom, Left, or Right).
	/// </summary>
	public StatusLayout StatusLayout
	{
		get => _StatusLayout;
		set
		{
			_StatusLayout = value;
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Background color for the window (default: Default/transparent).
	/// </summary>
	public Color BackgroundColor
	{
		get => _BackgroundColor;
		set
		{
			_BackgroundColor = value;
			// Propagate to child components
			_OutputView.Background = value;
			_StatusPanel.Background = value;
			_CommandInput.Background = value;
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Border color for the window frame and separators (default: BrightBlack).
	/// </summary>
	public Color BorderColor
	{
		get => _BorderColor;
		set
		{
			_BorderColor = value;
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Title text color (default: BrightWhite).
	/// </summary>
	public Color TitleColor
	{
		get => _TitleColor;
		set
		{
			_TitleColor = value;
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Default text color for output (default: White).
	/// </summary>
	public Color TextColor
	{
		get => _TextColor;
		set
		{
			_TextColor = value;
			// Propagate to child components
			_OutputView.Foreground = value;
			_StatusPanel.ValueForeground = value;
			_CommandInput.TextColor = value;
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Color for separator labels like "Status" and "Command" (default: Yellow).
	/// </summary>
	public Color SeparatorLabelColor
	{
		get => _SeparatorLabelColor;
		set
		{
			_SeparatorLabelColor = value;
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// The output view for displaying messages.
	/// </summary>
	public OutputView OutputView => _OutputView;

	/// <summary>
	/// The status panel for displaying key-value pairs.
	/// </summary>
	public StatusPanel StatusPanel => _StatusPanel;

	/// <summary>
	/// The command input area.
	/// </summary>
	public CommandInput CommandInput => _CommandInput;

	public ConsoleWindow()
	{
		_OutputView = new OutputView
		{
			CanFocus = false // Output is read-only, no need for focus
		};
		_StatusPanel = new StatusPanel
		{
			KeyForeground = Color.Cyan,
			ValueForeground = Color.White,
			SeparatorForeground = Color.BrightBlack,
			CanFocus = false
		};
		_CommandInput = new CommandInput
		{
			Prompt = "> ",
			PromptColor = Color.Green,
			CanFocus = true
		};

		Add(_OutputView);
		Add(_StatusPanel);
		Add(_CommandInput);

		_CommandInput.CommandSubmitted += OnCommandSubmitted;

		Application.OnResize += OnResize;
	}

	/// <summary>
	/// Sets up the layout based on terminal size.
	/// </summary>
	public void SetupLayout(int Width, int Height)
	{
		this.Width = Width;
		this.Height = Height;

		switch (_StatusLayout)
		{
			case StatusLayout.Bottom:
				SetupBottomLayout(Width, Height);
				break;
			case StatusLayout.Left:
				SetupLeftLayout(Width, Height);
				break;
			case StatusLayout.Right:
				SetupRightLayout(Width, Height);
				break;
		}

		SetNeedsDraw();
	}

	private void SetupBottomLayout(int Width, int Height)
	{
		// Layout: Border(1) + Output + Separator(1) + StatusPanel + Separator(1) + Input(1) + Border(1)
		int Non_Output_Height = 2 + 1 + _StatusPanelHeight + 1 + 1;
		int Output_Height = Math.Max(1, Height - Non_Output_Height);

		_OutputView.X = 1;
		_OutputView.Y = 1;
		_OutputView.Width = Width - 2;
		_OutputView.Height = Output_Height;

		_StatusPanel.X = 1;
		_StatusPanel.Y = 1 + Output_Height + 1;
		_StatusPanel.Width = Width - 2;
		_StatusPanel.Height = _StatusPanelHeight;

		_CommandInput.X = 1;
		_CommandInput.Y = _StatusPanel.Y + _StatusPanelHeight + 1;
		_CommandInput.Width = Width - 2;
		_CommandInput.Height = 1;
	}

	private void SetupLeftLayout(int Width, int Height)
	{
		// Layout: Border + StatusColumn + Separator + OutputArea + Border
		//         Border + Input + Border (at bottom)
		int Status_Width = Math.Min(_StatusPanelWidth, Width / 3);
		int Output_Width = Width - Status_Width - 3; // -3 for borders and separator

		// Status panel on left
		_StatusPanel.X = 1;
		_StatusPanel.Y = 1;
		_StatusPanel.Width = Status_Width;
		_StatusPanel.Height = Height - 4; // Leave room for input row

		// Output area on right of status
		_OutputView.X = Status_Width + 2; // +2 for left border and separator
		_OutputView.Y = 1;
		_OutputView.Width = Output_Width;
		_OutputView.Height = Height - 4;

		// Command input spans full width at bottom
		_CommandInput.X = 1;
		_CommandInput.Y = Height - 2;
		_CommandInput.Width = Width - 2;
		_CommandInput.Height = 1;
	}

	private void SetupRightLayout(int Width, int Height)
	{
		// Layout: Border + OutputArea + Separator + StatusColumn + Border
		//         Border + Input + Border (at bottom)
		int Status_Width = Math.Min(_StatusPanelWidth, Width / 3);
		int Output_Width = Width - Status_Width - 3;

		// Output area on left
		_OutputView.X = 1;
		_OutputView.Y = 1;
		_OutputView.Width = Output_Width;
		_OutputView.Height = Height - 4;

		// Status panel on right
		_StatusPanel.X = Output_Width + 2;
		_StatusPanel.Y = 1;
		_StatusPanel.Width = Status_Width;
		_StatusPanel.Height = Height - 4;

		// Command input spans full width at bottom
		_CommandInput.X = 1;
		_CommandInput.Y = Height - 2;
		_CommandInput.Width = Width - 2;
		_CommandInput.Height = 1;
	}

	private void OnResize(object? Sender, ResizeEventArgs E)
	{
		SetupLayout(E.Width, E.Height);
	}

	private void OnCommandSubmitted(object? Sender, CommandSubmittedEventArgs E)
	{
		if (string.IsNullOrWhiteSpace(E.Command))
		{
			return;
		}

		// Handle built-in commands
		string Cmd = E.Command.Trim().ToLowerInvariant();

		if (Cmd == "clear" || Cmd == "cls")
		{
			_OutputView.Clear();
			return;
		}

		if (Cmd == "exit" || Cmd == "quit")
		{
			Application.RequestStop();
			return;
		}

		// Raise event for external handling
		CommandReceived?.Invoke(this, new CommandReceivedEventArgs(E.Command));
	}

	/// <summary>
	/// Writes a line to the output.
	/// </summary>
	public void WriteLine(string Text, string? Category = null)
	{
		_OutputView.AppendLine(Text, Category);
	}

	/// <summary>
	/// Writes an info message (cyan prefix).
	/// </summary>
	public void WriteInfo(string Text)
	{
		_OutputView.AppendLine($"[*] {Text}", "info");
	}

	/// <summary>
	/// Writes a success message (green prefix).
	/// </summary>
	public void WriteSuccess(string Text)
	{
		_OutputView.AppendLine($"[+] {Text}", "success");
	}

	/// <summary>
	/// Writes a warning message (yellow prefix).
	/// </summary>
	public void WriteWarning(string Text)
	{
		_OutputView.AppendLine($"[!] {Text}", "warning");
	}

	/// <summary>
	/// Writes an error message (red prefix).
	/// </summary>
	public void WriteError(string Text)
	{
		_OutputView.AppendLine($"[-] {Text}", "error");
	}

	/// <summary>
	/// Sets a status panel value.
	/// </summary>
	public void SetStatus(string Key, string Value)
	{
		_StatusPanel.Set(Key, Value);
	}

	/// <summary>
	/// Removes a status panel value.
	/// </summary>
	public void RemoveStatus(string Key)
	{
		_StatusPanel.Remove(Key);
	}

	/// <summary>
	/// Clears all status panel values.
	/// </summary>
	public void ClearStatus()
	{
		_StatusPanel.ClearAll();
	}

	/// <summary>
	/// Clears the output.
	/// </summary>
	public void ClearOutput()
	{
		_OutputView.Clear();
	}

	public override void Draw(Screen Screen)
	{
		if (!Visible)
		{
			return;
		}

		var (Screen_X, Screen_Y) = LocalToScreen(0, 0);

		// Clear the entire area with background color
		Screen.FillRect(Screen_X, Screen_Y, Width, Height, ' ', _TextColor, _BackgroundColor);

		// Draw outer border
		Screen.DrawBox(Screen_X, Screen_Y, Width, Height, _BorderColor, _BackgroundColor, BoxStyle.Single);

		// Draw title in top border
		if (!string.IsNullOrEmpty(_Title) && Width > 4)
		{
			int Max_Title_Length = Math.Max(0, Width - 7);
			string Title_Display = _Title.Length > Width - 4 && Max_Title_Length > 0
				? _Title[..Max_Title_Length] + "..."
				: _Title;
			int Title_X = Screen_X + 2;
			Screen.DrawString(Title_X, Screen_Y, $" {Title_Display} ", _TitleColor, _BackgroundColor, TextAttribute.Bold);
		}

		switch (_StatusLayout)
		{
			case StatusLayout.Bottom:
				DrawBottomLayout(Screen, Screen_X, Screen_Y);
				break;
			case StatusLayout.Left:
				DrawLeftLayout(Screen, Screen_X, Screen_Y);
				break;
			case StatusLayout.Right:
				DrawRightLayout(Screen, Screen_X, Screen_Y);
				break;
		}

		// Let children draw themselves
		base.Draw(Screen);
	}

	private void DrawBottomLayout(Screen Screen, int Screen_X, int Screen_Y)
	{
		// Draw status panel separator
		int Status_Y = Screen_Y + 1 + _OutputView.Height;
		Screen.SetCell(Screen_X, Status_Y, BoxChars.VerticalRight, _BorderColor);
		Screen.DrawHLine(Screen_X + 1, Status_Y, Width - 2, BoxChars.Horizontal, _BorderColor);
		Screen.SetCell(Screen_X + Width - 1, Status_Y, BoxChars.VerticalLeft, _BorderColor);

		// Draw status label
		Screen.DrawString(Screen_X + 2, Status_Y, " Status ", _SeparatorLabelColor, _BackgroundColor);

		// Draw input separator
		int Input_Y = Status_Y + _StatusPanelHeight;
		Screen.SetCell(Screen_X, Input_Y, BoxChars.VerticalRight, _BorderColor);
		Screen.DrawHLine(Screen_X + 1, Input_Y, Width - 2, BoxChars.Horizontal, _BorderColor);
		Screen.SetCell(Screen_X + Width - 1, Input_Y, BoxChars.VerticalLeft, _BorderColor);

		// Draw input label
		Screen.DrawString(Screen_X + 2, Input_Y, " Command ", Color.Green, _BackgroundColor);
	}

	private void DrawLeftLayout(Screen Screen, int Screen_X, int Screen_Y)
	{
		int Separator_X = Screen_X + _StatusPanel.Width + 1;

		// Draw vertical separator between status and output
		Screen.SetCell(Separator_X, Screen_Y, BoxChars.HorizontalDown, _BorderColor);
		Screen.DrawVLine(Separator_X, Screen_Y + 1, Height - 4, BoxChars.Vertical, _BorderColor);
		Screen.SetCell(Separator_X, Screen_Y + Height - 3, BoxChars.HorizontalUp, _BorderColor);

		// Draw horizontal separator above input
		int Input_Sep_Y = Screen_Y + Height - 3;
		Screen.SetCell(Screen_X, Input_Sep_Y, BoxChars.VerticalRight, _BorderColor);
		Screen.DrawHLine(Screen_X + 1, Input_Sep_Y, Width - 2, BoxChars.Horizontal, _BorderColor);
		Screen.SetCell(Screen_X + Width - 1, Input_Sep_Y, BoxChars.VerticalLeft, _BorderColor);

		// Fix the intersection point
		Screen.SetCell(Separator_X, Input_Sep_Y, BoxChars.HorizontalUp, _BorderColor);

		// Draw labels
		Screen.DrawString(Screen_X + 2, Screen_Y, " Status ", _SeparatorLabelColor, _BackgroundColor);
		Screen.DrawString(Screen_X + 2, Input_Sep_Y, " Command ", Color.Green, _BackgroundColor);
	}

	private void DrawRightLayout(Screen Screen, int Screen_X, int Screen_Y)
	{
		int Separator_X = Screen_X + _OutputView.Width + 1;

		// Draw vertical separator between output and status
		Screen.SetCell(Separator_X, Screen_Y, BoxChars.HorizontalDown, _BorderColor);
		Screen.DrawVLine(Separator_X, Screen_Y + 1, Height - 4, BoxChars.Vertical, _BorderColor);
		Screen.SetCell(Separator_X, Screen_Y + Height - 3, BoxChars.HorizontalUp, _BorderColor);

		// Draw horizontal separator above input
		int Input_Sep_Y = Screen_Y + Height - 3;
		Screen.SetCell(Screen_X, Input_Sep_Y, BoxChars.VerticalRight, _BorderColor);
		Screen.DrawHLine(Screen_X + 1, Input_Sep_Y, Width - 2, BoxChars.Horizontal, _BorderColor);
		Screen.SetCell(Screen_X + Width - 1, Input_Sep_Y, BoxChars.VerticalLeft, _BorderColor);

		// Fix the intersection point
		Screen.SetCell(Separator_X, Input_Sep_Y, BoxChars.HorizontalUp, _BorderColor);

		// Draw labels
		Screen.DrawString(Separator_X + 2, Screen_Y, " Status ", _SeparatorLabelColor, _BackgroundColor);
		Screen.DrawString(Screen_X + 2, Input_Sep_Y, " Command ", Color.Green, _BackgroundColor);
	}

	public override bool HandleKey(KeyInfo Key)
	{
		// Handle Ctrl+Q to quit
		if (Key.IsCtrl && Key.CtrlLetter == 'Q')
		{
			Application.RequestStop();
			return true;
		}

		// Handle Ctrl+L to clear
		if (Key.IsCtrl && Key.CtrlLetter == 'L')
		{
			_OutputView.Clear();
			return true;
		}

		return base.HandleKey(Key);
	}

	/// <summary>
	/// Releases resources used by the console window.
	/// </summary>
	public void Dispose()
	{
		if (_Disposed)
		{
			return;
		}

		Application.OnResize -= OnResize;
		_CommandInput.CommandSubmitted -= OnCommandSubmitted;
		_Disposed = true;
		GC.SuppressFinalize(this);
	}
}
