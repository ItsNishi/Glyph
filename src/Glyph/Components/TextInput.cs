namespace Glyph.Terminal;

/// <summary>
/// Event args for text submission events.
/// </summary>
public class TextSubmittedEventArgs : EventArgs
{
	/// <summary>
	/// The submitted text.
	/// </summary>
	public string Text { get; }

	public TextSubmittedEventArgs(string Text)
	{
		this.Text = Text;
	}
}

/// <summary>
/// Event args for submit cancellation events.
/// </summary>
public class SubmitCancelledEventArgs : EventArgs
{
	/// <summary>
	/// The text at the time of cancellation.
	/// </summary>
	public string Text { get; }

	public SubmitCancelledEventArgs(string Text)
	{
		this.Text = Text;
	}
}

/// <summary>
/// Multi-line text input with cursor navigation and history support.
/// </summary>
public class TextInput : View
{
	private readonly List<string> _Lines = new() { string.Empty };
	private int _CursorRow;
	private int _CursorCol;
	private int _ScrollRow;
	private int _ScrollCol;
	private readonly List<string> _History = new();
	private int _HistoryIndex = -1;
	private string _CurrentInput = string.Empty;

	/// <summary>
	/// Gets or sets the full text content.
	/// </summary>
	public string Text
	{
		get => string.Join("\n", _Lines);
		set
		{
			SetText(value ?? string.Empty);
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Current cursor row (0-indexed).
	/// </summary>
	public int CursorRow => _CursorRow;

	/// <summary>
	/// Current cursor column (0-indexed).
	/// </summary>
	public int CursorCol => _CursorCol;

	/// <summary>
	/// Number of lines in the text.
	/// </summary>
	public int LineCount => _Lines.Count;

	/// <summary>
	/// Read-only access to input history.
	/// </summary>
	public IReadOnlyList<string> History => _History;

	/// <summary>
	/// Maximum number of history entries to retain. 0 means unlimited.
	/// </summary>
	public int MaxHistorySize { get; set; } = 100;

	/// <summary>
	/// Whether to enable word wrap when drawing.
	/// </summary>
	public bool WordWrap { get; set; } = false;

	/// <summary>
	/// Foreground color for the text.
	/// </summary>
	public Color TextColor { get; set; } = Color.Default;

	/// <summary>
	/// Background color for the cursor position.
	/// </summary>
	public Color CursorColor { get; set; } = Color.White;

	/// <summary>
	/// Background color for the text area.
	/// </summary>
	public Color BackgroundColor { get; set; } = Color.Default;

	/// <summary>
	/// Event raised when text is submitted (Enter pressed).
	/// </summary>
	public event EventHandler<TextSubmittedEventArgs>? TextSubmitted;

	/// <summary>
	/// Event raised when input is cancelled (Ctrl+C pressed).
	/// </summary>
	public event EventHandler<SubmitCancelledEventArgs>? SubmitCancelled;

	public TextInput() : base()
	{
		CanFocus = true;
	}

	public TextInput(int X, int Y, int Width, int Height) : base(X, Y, Width, Height)
	{
		CanFocus = true;
	}

	/// <summary>
	/// Adds text to the history.
	/// </summary>
	public void AddToHistory(string Input)
	{
		if (string.IsNullOrWhiteSpace(Input))
		{
			return;
		}

		// Avoid duplicating the most recent entry
		if (_History.Count > 0 && _History[^1] == Input)
		{
			return;
		}

		_History.Add(Input);

		// Trim history if it exceeds max size
		if (MaxHistorySize > 0 && _History.Count > MaxHistorySize)
		{
			_History.RemoveAt(0);
		}
	}

	/// <summary>
	/// Clears the input history.
	/// </summary>
	public void ClearHistory()
	{
		_History.Clear();
		_HistoryIndex = -1;
	}

	/// <summary>
	/// Clears the text content.
	/// </summary>
	public new void Clear()
	{
		_Lines.Clear();
		_Lines.Add(string.Empty);
		_CursorRow = 0;
		_CursorCol = 0;
		_ScrollRow = 0;
		_ScrollCol = 0;
		SetNeedsDraw();
	}

	private void SetText(string Value)
	{
		_Lines.Clear();
		if (string.IsNullOrEmpty(Value))
		{
			_Lines.Add(string.Empty);
		}
		else
		{
			var Split = Value.Split('\n');
			foreach (var Line in Split)
			{
				// Remove carriage returns for Windows line endings
				_Lines.Add(Line.TrimEnd('\r'));
			}
		}

		_CursorRow = _Lines.Count - 1;
		_CursorCol = _Lines[_CursorRow].Length;
		EnsureCursorVisible();
	}

	public override void Draw(Screen Screen)
	{
		if (!Visible)
		{
			return;
		}

		var (Screen_X, Screen_Y) = LocalToScreen(0, 0);

		// Clear the text area
		for (int Row = 0; Row < Height; Row++)
		{
			for (int Col = 0; Col < Width; Col++)
			{
				Screen.SetCell(Screen_X + Col, Screen_Y + Row, ' ', TextColor, BackgroundColor);
			}
		}

		// Draw visible lines
		for (int Row = 0; Row < Height; Row++)
		{
			int Line_Index = _ScrollRow + Row;
			if (Line_Index >= _Lines.Count)
			{
				break;
			}

			string Line = _Lines[Line_Index];

			if (WordWrap)
			{
				DrawWrappedLine(Screen, Screen_X, Screen_Y + Row, Line);
			}
			else
			{
				DrawLine(Screen, Screen_X, Screen_Y + Row, Line, Line_Index);
			}
		}

		// Draw cursor
		if (HasFocus)
		{
			int Cursor_Screen_Row = _CursorRow - _ScrollRow;
			int Cursor_Screen_Col = _CursorCol - _ScrollCol;

			if (Cursor_Screen_Row >= 0 && Cursor_Screen_Row < Height &&
			    Cursor_Screen_Col >= 0 && Cursor_Screen_Col < Width)
			{
				string Current_Line = _Lines[_CursorRow];
				char Cursor_Char = _CursorCol < Current_Line.Length ? Current_Line[_CursorCol] : ' ';
				Screen.SetCell(
					Screen_X + Cursor_Screen_Col,
					Screen_Y + Cursor_Screen_Row,
					Cursor_Char,
					Color.Black,
					CursorColor
				);
			}
		}

		base.Draw(Screen);
	}

	private void DrawLine(Screen Screen, int Screen_X, int Screen_Y, string Line, int Line_Index)
	{
		// Apply horizontal scroll
		string Visible_Text = Line.Length > _ScrollCol
			? Line[_ScrollCol..]
			: string.Empty;

		if (Visible_Text.Length > Width)
		{
			Visible_Text = Visible_Text[..Width];
		}

		Screen.DrawString(Screen_X, Screen_Y, Visible_Text, TextColor, BackgroundColor);
	}

	private void DrawWrappedLine(Screen Screen, int Screen_X, int Screen_Y, string Line)
	{
		// Simple character-based wrap for now
		string Visible_Text = Line.Length > Width ? Line[..Width] : Line;
		Screen.DrawString(Screen_X, Screen_Y, Visible_Text, TextColor, BackgroundColor);
	}

	public override bool HandleKey(KeyInfo Key)
	{
		if (!Enabled)
		{
			return false;
		}

		// Let base class handle events first
		if (base.HandleKey(Key))
		{
			return true;
		}

		// Handle Ctrl+C - cancel/clear
		if (Key.IsCtrl && Key.CtrlLetter == 'C')
		{
			string Current_Text = Text;
			Clear();
			_HistoryIndex = -1;
			SubmitCancelled?.Invoke(this, new SubmitCancelledEventArgs(Current_Text));
			return true;
		}

		// Handle Enter - submit
		if (Key.Key == ExtendedKey.Enter)
		{
			string Submitted_Text = Text;
			AddToHistory(Submitted_Text);
			Clear();
			_HistoryIndex = -1;
			_CurrentInput = string.Empty;
			TextSubmitted?.Invoke(this, new TextSubmittedEventArgs(Submitted_Text));
			return true;
		}

		// Handle Shift+Enter - insert newline
		if (Key.Key == ExtendedKey.ShiftEnter)
		{
			InsertNewline();
			ResetHistoryNavigation();
			return true;
		}

		// Handle Backspace
		if (Key.Key == ExtendedKey.Backspace)
		{
			HandleBackspace();
			ResetHistoryNavigation();
			return true;
		}

		// Handle Delete
		if (Key.Key == ExtendedKey.Delete)
		{
			HandleDelete();
			ResetHistoryNavigation();
			return true;
		}

		// Handle cursor navigation
		if (Key.Key == ExtendedKey.LeftArrow)
		{
			MoveCursorLeft();
			return true;
		}

		if (Key.Key == ExtendedKey.RightArrow)
		{
			MoveCursorRight();
			return true;
		}

		if (Key.Key == ExtendedKey.UpArrow)
		{
			// Navigate history when at the first line
			if (_CursorRow == 0)
			{
				NavigateHistoryUp();
			}
			else
			{
				MoveCursorUp();
			}
			return true;
		}

		if (Key.Key == ExtendedKey.DownArrow)
		{
			// Navigate history when at the last line
			if (_CursorRow == _Lines.Count - 1)
			{
				NavigateHistoryDown();
			}
			else
			{
				MoveCursorDown();
			}
			return true;
		}

		if (Key.Key == ExtendedKey.Home)
		{
			_CursorCol = 0;
			EnsureCursorVisible();
			SetNeedsDraw();
			return true;
		}

		if (Key.Key == ExtendedKey.End)
		{
			_CursorCol = _Lines[_CursorRow].Length;
			EnsureCursorVisible();
			SetNeedsDraw();
			return true;
		}

		// Handle printable characters
		if (Key.IsPrintable)
		{
			InsertCharacter(Key.Char);
			ResetHistoryNavigation();
			return true;
		}

		return false;
	}

	private void InsertCharacter(char C)
	{
		string Current_Line = _Lines[_CursorRow];
		_Lines[_CursorRow] = Current_Line.Insert(_CursorCol, C.ToString());
		_CursorCol++;
		EnsureCursorVisible();
		SetNeedsDraw();
	}

	private void InsertNewline()
	{
		string Current_Line = _Lines[_CursorRow];
		string Before_Cursor = Current_Line[.._CursorCol];
		string After_Cursor = Current_Line[_CursorCol..];

		_Lines[_CursorRow] = Before_Cursor;
		_Lines.Insert(_CursorRow + 1, After_Cursor);

		_CursorRow++;
		_CursorCol = 0;
		EnsureCursorVisible();
		SetNeedsDraw();
	}

	private void HandleBackspace()
	{
		if (_CursorCol > 0)
		{
			// Delete character before cursor on same line
			string Current_Line = _Lines[_CursorRow];
			_Lines[_CursorRow] = Current_Line.Remove(_CursorCol - 1, 1);
			_CursorCol--;
			EnsureCursorVisible();
			SetNeedsDraw();
		}
		else if (_CursorRow > 0)
		{
			// Join with previous line
			string Current_Line = _Lines[_CursorRow];
			string Previous_Line = _Lines[_CursorRow - 1];

			_CursorCol = Previous_Line.Length;
			_Lines[_CursorRow - 1] = Previous_Line + Current_Line;
			_Lines.RemoveAt(_CursorRow);
			_CursorRow--;
			EnsureCursorVisible();
			SetNeedsDraw();
		}
	}

	private void HandleDelete()
	{
		string Current_Line = _Lines[_CursorRow];

		if (_CursorCol < Current_Line.Length)
		{
			// Delete character at cursor
			_Lines[_CursorRow] = Current_Line.Remove(_CursorCol, 1);
			SetNeedsDraw();
		}
		else if (_CursorRow < _Lines.Count - 1)
		{
			// Join with next line
			string Next_Line = _Lines[_CursorRow + 1];
			_Lines[_CursorRow] = Current_Line + Next_Line;
			_Lines.RemoveAt(_CursorRow + 1);
			SetNeedsDraw();
		}
	}

	private void MoveCursorLeft()
	{
		if (_CursorCol > 0)
		{
			_CursorCol--;
		}
		else if (_CursorRow > 0)
		{
			// Move to end of previous line
			_CursorRow--;
			_CursorCol = _Lines[_CursorRow].Length;
		}
		EnsureCursorVisible();
		SetNeedsDraw();
	}

	private void MoveCursorRight()
	{
		string Current_Line = _Lines[_CursorRow];

		if (_CursorCol < Current_Line.Length)
		{
			_CursorCol++;
		}
		else if (_CursorRow < _Lines.Count - 1)
		{
			// Move to start of next line
			_CursorRow++;
			_CursorCol = 0;
		}
		EnsureCursorVisible();
		SetNeedsDraw();
	}

	private void MoveCursorUp()
	{
		if (_CursorRow > 0)
		{
			_CursorRow--;
			// Clamp cursor column to line length
			_CursorCol = Math.Min(_CursorCol, _Lines[_CursorRow].Length);
			EnsureCursorVisible();
			SetNeedsDraw();
		}
	}

	private void MoveCursorDown()
	{
		if (_CursorRow < _Lines.Count - 1)
		{
			_CursorRow++;
			// Clamp cursor column to line length
			_CursorCol = Math.Min(_CursorCol, _Lines[_CursorRow].Length);
			EnsureCursorVisible();
			SetNeedsDraw();
		}
	}

	private void EnsureCursorVisible()
	{
		// Vertical scrolling
		if (_CursorRow < _ScrollRow)
		{
			_ScrollRow = _CursorRow;
		}
		else if (_CursorRow >= _ScrollRow + Height)
		{
			_ScrollRow = _CursorRow - Height + 1;
		}

		// Horizontal scrolling (only when word wrap is disabled)
		if (!WordWrap)
		{
			if (_CursorCol < _ScrollCol)
			{
				_ScrollCol = _CursorCol;
			}
			else if (_CursorCol >= _ScrollCol + Width)
			{
				_ScrollCol = _CursorCol - Width + 1;
			}
		}
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
			_CurrentInput = Text;
		}

		if (_HistoryIndex < _History.Count - 1)
		{
			_HistoryIndex++;
			Text = _History[_History.Count - 1 - _HistoryIndex];
		}
	}

	private void NavigateHistoryDown()
	{
		if (_HistoryIndex > 0)
		{
			_HistoryIndex--;
			Text = _History[_History.Count - 1 - _HistoryIndex];
		}
		else if (_HistoryIndex == 0)
		{
			// Restore the original input
			_HistoryIndex = -1;
			Text = _CurrentInput;
		}
	}

	private void ResetHistoryNavigation()
	{
		_HistoryIndex = -1;
		_CurrentInput = string.Empty;
	}
}
