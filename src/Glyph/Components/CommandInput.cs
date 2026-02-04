namespace Glyph.Terminal;

/// <summary>
/// Event args for command submission events.
/// </summary>
public class CommandSubmittedEventArgs : EventArgs
{
	/// <summary>
	/// The submitted command text.
	/// </summary>
	public string Command { get; }

	public CommandSubmittedEventArgs(string Command)
	{
		this.Command = Command;
	}
}

/// <summary>
/// Single-line command input with history navigation.
/// </summary>
public class CommandInput : View
{
	private string _Text = string.Empty;
	private string _Prompt = "> ";
	private int _CursorPosition;
	private readonly List<string> _History = new();
	private int _HistoryIndex = -1;
	private string _CurrentInput = string.Empty;

	/// <summary>
	/// Current input text.
	/// </summary>
	public string Text
	{
		get => _Text;
		set
		{
			_Text = value ?? string.Empty;
			_CursorPosition = Math.Clamp(_CursorPosition, 0, _Text.Length);
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Prompt displayed before the input text.
	/// </summary>
	public string Prompt
	{
		get => _Prompt;
		set
		{
			_Prompt = value ?? string.Empty;
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Current cursor position within the text.
	/// </summary>
	public int CursorPosition
	{
		get => _CursorPosition;
		set
		{
			_CursorPosition = Math.Clamp(value, 0, _Text.Length);
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Read-only access to command history.
	/// </summary>
	public IReadOnlyList<string> History => _History;

	/// <summary>
	/// Maximum number of history entries to retain. 0 means unlimited.
	/// </summary>
	public int MaxHistorySize { get; set; } = 100;

	/// <summary>
	/// Foreground color for the prompt.
	/// </summary>
	public Color PromptColor { get; set; } = Color.Cyan;

	/// <summary>
	/// Foreground color for the input text.
	/// </summary>
	public Color TextColor { get; set; } = Color.Default;

	/// <summary>
	/// Background color for the cursor position.
	/// </summary>
	public Color CursorColor { get; set; } = Color.White;

	/// <summary>
	/// Background color for the input area.
	/// </summary>
	public Color Background { get; set; } = Color.Default;

	/// <summary>
	/// Event raised when a command is submitted (Enter pressed).
	/// </summary>
	public event EventHandler<CommandSubmittedEventArgs>? CommandSubmitted;

	public CommandInput() : base()
	{
		CanFocus = true;
		Height = 1;
	}

	public CommandInput(int X, int Y, int Width) : base(X, Y, Width, 1)
	{
		CanFocus = true;
	}

	/// <summary>
	/// Adds a command to the history.
	/// </summary>
	public void AddToHistory(string Command)
	{
		if (string.IsNullOrWhiteSpace(Command))
		{
			return;
		}

		// Avoid duplicating the most recent entry
		if (_History.Count > 0 && _History[^1] == Command)
		{
			return;
		}

		_History.Add(Command);

		// Trim history if it exceeds max size
		if (MaxHistorySize > 0 && _History.Count > MaxHistorySize)
		{
			_History.RemoveAt(0);
		}
	}

	/// <summary>
	/// Clears the command history.
	/// </summary>
	public void ClearHistory()
	{
		_History.Clear();
		_HistoryIndex = -1;
	}

	public override void Draw(Screen Screen)
	{
		if (!Visible)
		{
			return;
		}

		var (Screen_X, Screen_Y) = LocalToScreen(0, 0);

		// Fill background first
		for (int i = 0; i < Width; i++)
		{
			Screen.SetCell(Screen_X + i, Screen_Y, ' ', TextColor, Background);
		}

		// Calculate available width for text
		int Available_Width = Width - _Prompt.Length;
		if (Available_Width <= 0)
		{
			// Just draw what we can of the prompt
			string Truncated_Prompt = _Prompt.Length > Width ? _Prompt[..Width] : _Prompt;
			Screen.DrawString(Screen_X, Screen_Y, Truncated_Prompt, PromptColor, Background);
			base.Draw(Screen);
			return;
		}

		// Draw prompt
		Screen.DrawString(Screen_X, Screen_Y, _Prompt, PromptColor, Background);

		// Calculate scroll offset to keep cursor visible
		int Scroll_Offset = 0;
		if (_CursorPosition >= Available_Width)
		{
			Scroll_Offset = _CursorPosition - Available_Width + 1;
		}

		// Draw text with scrolling
		string Visible_Text = _Text.Length > Scroll_Offset
			? _Text[Scroll_Offset..]
			: string.Empty;

		if (Visible_Text.Length > Available_Width)
		{
			Visible_Text = Visible_Text[..Available_Width];
		}

		int Text_X = Screen_X + _Prompt.Length;
		Screen.DrawString(Text_X, Screen_Y, Visible_Text, TextColor, Background);

		// Fill remaining space with spaces (clear old characters)
		int Remaining = Available_Width - Visible_Text.Length;
		for (int i = 0; i < Remaining; i++)
		{
			Screen.SetCell(Text_X + Visible_Text.Length + i, Screen_Y, ' ', TextColor, Background);
		}

		// Draw cursor (highlight the character at cursor position)
		if (HasFocus)
		{
			int Cursor_Screen_X = Text_X + (_CursorPosition - Scroll_Offset);
			if (Cursor_Screen_X >= Text_X && Cursor_Screen_X < Text_X + Available_Width)
			{
				char Cursor_Char = _CursorPosition < _Text.Length ? _Text[_CursorPosition] : ' ';
				Screen.SetCell(Cursor_Screen_X, Screen_Y, Cursor_Char, Color.Black, CursorColor);
			}
		}

		base.Draw(Screen);
	}

	public override bool HandleKey(KeyInfo Key)
	{
		if (!Enabled)
		{
			return false;
		}

		// Let base class handle events first
		var Args = new KeyEventArgs(Key);
		// Check OnKeyPress event via base behavior
		if (base.HandleKey(Key))
		{
			return true;
		}

		// Handle Ctrl+C - clear input
		if (Key.IsCtrl && Key.CtrlLetter == 'C')
		{
			_Text = string.Empty;
			_CursorPosition = 0;
			_HistoryIndex = -1;
			SetNeedsDraw();
			return true;
		}

		// Handle Enter - submit command
		if (Key.Key == ExtendedKey.Enter)
		{
			string Command = _Text;
			AddToHistory(Command);
			_Text = string.Empty;
			_CursorPosition = 0;
			_HistoryIndex = -1;
			_CurrentInput = string.Empty;
			SetNeedsDraw();
			CommandSubmitted?.Invoke(this, new CommandSubmittedEventArgs(Command));
			return true;
		}

		// Handle Backspace - delete character before cursor
		if (Key.Key == ExtendedKey.Backspace)
		{
			if (_CursorPosition > 0)
			{
				_Text = _Text.Remove(_CursorPosition - 1, 1);
				_CursorPosition--;
				ResetHistoryNavigation();
				SetNeedsDraw();
			}
			return true;
		}

		// Handle Delete - delete character at cursor
		if (Key.Key == ExtendedKey.Delete)
		{
			if (_CursorPosition < _Text.Length)
			{
				_Text = _Text.Remove(_CursorPosition, 1);
				ResetHistoryNavigation();
				SetNeedsDraw();
			}
			return true;
		}

		// Handle Left arrow - move cursor left
		if (Key.Key == ExtendedKey.LeftArrow)
		{
			if (_CursorPosition > 0)
			{
				_CursorPosition--;
				SetNeedsDraw();
			}
			return true;
		}

		// Handle Right arrow - move cursor right
		if (Key.Key == ExtendedKey.RightArrow)
		{
			if (_CursorPosition < _Text.Length)
			{
				_CursorPosition++;
				SetNeedsDraw();
			}
			return true;
		}

		// Handle Home - move to start
		if (Key.Key == ExtendedKey.Home)
		{
			_CursorPosition = 0;
			SetNeedsDraw();
			return true;
		}

		// Handle End - move to end
		if (Key.Key == ExtendedKey.End)
		{
			_CursorPosition = _Text.Length;
			SetNeedsDraw();
			return true;
		}

		// Handle Up arrow - navigate history backward
		if (Key.Key == ExtendedKey.UpArrow)
		{
			NavigateHistoryUp();
			return true;
		}

		// Handle Down arrow - navigate history forward
		if (Key.Key == ExtendedKey.DownArrow)
		{
			NavigateHistoryDown();
			return true;
		}

		// Handle printable characters - insert at cursor
		if (Key.IsPrintable)
		{
			_Text = _Text.Insert(_CursorPosition, Key.Char.ToString());
			_CursorPosition++;
			ResetHistoryNavigation();
			SetNeedsDraw();
			return true;
		}

		return false;
	}

	private void NavigateHistoryUp()
	{
		if (_History.Count == 0)
		{
			return;
		}

		// Save current input when starting history navigation
		if (_HistoryIndex == -1)
		{
			_CurrentInput = _Text;
		}

		if (_HistoryIndex < _History.Count - 1)
		{
			_HistoryIndex++;
			_Text = _History[_History.Count - 1 - _HistoryIndex];
			_CursorPosition = _Text.Length;
			SetNeedsDraw();
		}
	}

	private void NavigateHistoryDown()
	{
		if (_HistoryIndex > 0)
		{
			_HistoryIndex--;
			_Text = _History[_History.Count - 1 - _HistoryIndex];
			_CursorPosition = _Text.Length;
			SetNeedsDraw();
		}
		else if (_HistoryIndex == 0)
		{
			// Restore the original input
			_HistoryIndex = -1;
			_Text = _CurrentInput;
			_CursorPosition = _Text.Length;
			SetNeedsDraw();
		}
	}

	private void ResetHistoryNavigation()
	{
		_HistoryIndex = -1;
		_CurrentInput = string.Empty;
	}
}
