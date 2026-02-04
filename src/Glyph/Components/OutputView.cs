namespace Glyph.Terminal;

/// <summary>
/// Represents a line of output with associated metadata.
/// </summary>
/// <param name="Text">The text content of the line.</param>
/// <param name="Category">Optional category for styling/filtering.</param>
/// <param name="Timestamp">When the line was added.</param>
public record OutputLine(string Text, string? Category, DateTime Timestamp);

/// <summary>
/// A scrollable view for displaying output lines.
/// Supports auto-scroll, manual navigation, word wrapping, and custom formatting.
/// </summary>
public class OutputView : View
{
	private readonly List<OutputLine> _Lines = new();
	private readonly object _LinesLock = new();
	private int _ScrollOffset;
	private bool _AutoScroll = true;
	private List<string>? _WrappedLinesCache;
	private int _LastWidth = -1;

	/// <summary>
	/// Custom formatter for output lines. If null, uses default formatting.
	/// </summary>
	public Func<OutputLine, string>? Formatter { get; set; }

	/// <summary>
	/// Foreground color for text.
	/// </summary>
	public Color Foreground { get; set; } = Color.Default;

	/// <summary>
	/// Background color for the view.
	/// </summary>
	public Color Background { get; set; } = Color.Default;

	/// <summary>
	/// Whether to automatically scroll to bottom on new content.
	/// </summary>
	public bool AutoScroll
	{
		get => _AutoScroll;
		set => _AutoScroll = value;
	}

	/// <summary>
	/// Number of lines in the buffer.
	/// </summary>
	public int LineCount
	{
		get
		{
			lock (_LinesLock)
			{
				return _Lines.Count;
			}
		}
	}

	/// <summary>
	/// Current scroll offset (0 = top).
	/// </summary>
	public int ScrollOffset => _ScrollOffset;

	/// <summary>
	/// Read-only access to all lines.
	/// </summary>
	public IReadOnlyList<OutputLine> Lines
	{
		get
		{
			lock (_LinesLock)
			{
				return _Lines.ToList().AsReadOnly();
			}
		}
	}

	public OutputView()
	{
		CanFocus = true;
	}

	public OutputView(int X, int Y, int Width, int Height) : base(X, Y, Width, Height)
	{
		CanFocus = true;
	}

	/// <summary>
	/// Appends a new line to the output.
	/// </summary>
	/// <param name="Text">The text to append.</param>
	/// <param name="Category">Optional category for the line.</param>
	public void AppendLine(string Text, string? Category = null)
	{
		lock (_LinesLock)
		{
			_Lines.Add(new OutputLine(Text, Category, DateTime.Now));
			InvalidateWrappedCache();
		}

		if (_AutoScroll)
		{
			ScrollToBottom();
		}

		SetNeedsDraw();
	}

	/// <summary>
	/// Appends text to the last line (for streaming output).
	/// If no lines exist, creates a new line.
	/// </summary>
	/// <param name="Text">The text to append.</param>
	public void AppendToLast(string Text)
	{
		lock (_LinesLock)
		{
			if (_Lines.Count == 0)
			{
				_Lines.Add(new OutputLine(Text, null, DateTime.Now));
			}
			else
			{
				var Last = _Lines[^1];
				_Lines[^1] = Last with { Text = Last.Text + Text };
			}
			InvalidateWrappedCache();
		}

		if (_AutoScroll)
		{
			ScrollToBottom();
		}

		SetNeedsDraw();
	}

	/// <summary>
	/// Replaces the text of the last line.
	/// If no lines exist, creates a new line.
	/// </summary>
	/// <param name="Text">The new text for the last line.</param>
	public void UpdateLast(string Text)
	{
		lock (_LinesLock)
		{
			if (_Lines.Count == 0)
			{
				_Lines.Add(new OutputLine(Text, null, DateTime.Now));
			}
			else
			{
				var Last = _Lines[^1];
				_Lines[^1] = Last with { Text = Text, Timestamp = DateTime.Now };
			}
			InvalidateWrappedCache();
		}

		if (_AutoScroll)
		{
			ScrollToBottom();
		}

		SetNeedsDraw();
	}

	/// <summary>
	/// Clears all output lines.
	/// </summary>
	public new void Clear()
	{
		lock (_LinesLock)
		{
			_Lines.Clear();
			InvalidateWrappedCache();
		}

		_ScrollOffset = 0;
		SetNeedsDraw();
	}

	/// <summary>
	/// Scrolls to the bottom of the output.
	/// </summary>
	public void ScrollToBottom()
	{
		var Wrapped = GetWrappedLines();
		int Total_Lines = Wrapped.Count;
		int Visible_Lines = Height;

		_ScrollOffset = Math.Max(0, Total_Lines - Visible_Lines);
		SetNeedsDraw();
	}

	/// <summary>
	/// Scrolls to the top of the output.
	/// </summary>
	public void ScrollToTop()
	{
		_ScrollOffset = 0;
		_AutoScroll = false;
		SetNeedsDraw();
	}

	/// <summary>
	/// Scrolls up by the specified number of lines.
	/// </summary>
	public void ScrollUp(int Lines = 1)
	{
		_ScrollOffset = Math.Max(0, _ScrollOffset - Lines);
		_AutoScroll = false;
		SetNeedsDraw();
	}

	/// <summary>
	/// Scrolls down by the specified number of lines.
	/// </summary>
	public void ScrollDown(int Lines = 1)
	{
		var Wrapped = GetWrappedLines();
		int Max_Offset = Math.Max(0, Wrapped.Count - Height);
		_ScrollOffset = Math.Min(Max_Offset, _ScrollOffset + Lines);

		// Re-enable auto-scroll if we're at the bottom
		if (_ScrollOffset >= Max_Offset)
		{
			_AutoScroll = true;
		}

		SetNeedsDraw();
	}

	/// <summary>
	/// Scrolls up by one page.
	/// </summary>
	public void PageUp()
	{
		ScrollUp(Height);
	}

	/// <summary>
	/// Scrolls down by one page.
	/// </summary>
	public void PageDown()
	{
		ScrollDown(Height);
	}

	public override void Draw(Screen Screen)
	{
		if (!Visible)
		{
			return;
		}

		var (Screen_X, Screen_Y) = LocalToScreen(0, 0);

		// Fill background
		for (int Row = 0; Row < Height; Row++)
		{
			for (int Col = 0; Col < Width; Col++)
			{
				Screen.SetCell(Screen_X + Col, Screen_Y + Row, ' ', Foreground, Background);
			}
		}

		// Get wrapped lines and draw visible portion
		var Wrapped = GetWrappedLines();

		for (int Row = 0; Row < Height; Row++)
		{
			int Line_Index = _ScrollOffset + Row;
			if (Line_Index >= Wrapped.Count)
			{
				break;
			}

			string Line = Wrapped[Line_Index];
			Screen.DrawString(Screen_X, Screen_Y + Row, Line, Foreground, Background);
		}

		base.Draw(Screen);
	}

	public override bool HandleKey(KeyInfo Key)
	{
		if (!Enabled)
		{
			return false;
		}

		// Call base to allow event handlers
		if (base.HandleKey(Key))
		{
			return true;
		}

		// vim-style navigation
		switch (Key.Char)
		{
			case 'j':
				ScrollDown();
				return true;
			case 'k':
				ScrollUp();
				return true;
			case 'g':
				ScrollToTop();
				return true;
			case 'G':
				ScrollToBottom();
				_AutoScroll = true;
				return true;
		}

		// Standard navigation keys
		switch (Key.Key)
		{
			case ExtendedKey.UpArrow:
				ScrollUp();
				return true;
			case ExtendedKey.DownArrow:
				ScrollDown();
				return true;
			case ExtendedKey.PageUp:
				PageUp();
				return true;
			case ExtendedKey.PageDown:
				PageDown();
				return true;
			case ExtendedKey.Home:
				ScrollToTop();
				return true;
			case ExtendedKey.End:
				ScrollToBottom();
				_AutoScroll = true;
				return true;
		}

		return false;
	}

	public override bool HandleMouse(MouseInfo Mouse)
	{
		if (!Enabled || !Visible)
		{
			return false;
		}

		// Handle scroll wheel
		if (Mouse.Action == MouseAction.Scroll)
		{
			switch (Mouse.ScrollDirection)
			{
				case ScrollDirection.Up:
					ScrollUp(3);
					return true;
				case ScrollDirection.Down:
					ScrollDown(3);
					return true;
			}
		}

		return base.HandleMouse(Mouse);
	}

	private void InvalidateWrappedCache()
	{
		_WrappedLinesCache = null;
	}

	private List<string> GetWrappedLines()
	{
		lock (_LinesLock)
		{
			// Check if cache is valid
			if (_WrappedLinesCache != null && _LastWidth == Width)
			{
				return _WrappedLinesCache;
			}

			_LastWidth = Width;
			_WrappedLinesCache = new List<string>();

			foreach (var Line in _Lines)
			{
				string Formatted = Formatter != null ? Formatter(Line) : Line.Text;
				var Wrapped = WrapText(Formatted, Width);
				_WrappedLinesCache.AddRange(Wrapped);
			}

			return _WrappedLinesCache;
		}
	}

	private static List<string> WrapText(string Text, int Max_Width)
	{
		var Result = new List<string>();

		if (Max_Width <= 0)
		{
			Result.Add(string.Empty);
			return Result;
		}

		if (string.IsNullOrEmpty(Text))
		{
			Result.Add(string.Empty);
			return Result;
		}

		int Index = 0;
		while (Index < Text.Length)
		{
			// Find the end of this line
			int Remaining = Text.Length - Index;
			int Line_Length = Math.Min(Remaining, Max_Width);

			// If we're not at the end and the next char isn't whitespace,
			// try to break at a word boundary
			if (Index + Line_Length < Text.Length && !char.IsWhiteSpace(Text[Index + Line_Length]))
			{
				int Last_Space = Text.LastIndexOf(' ', Index + Line_Length - 1, Math.Min(Line_Length, Index + Line_Length));
				if (Last_Space > Index)
				{
					Line_Length = Last_Space - Index;
				}
			}

			string Line = Text.Substring(Index, Line_Length);
			Result.Add(Line);

			Index += Line_Length;

			// Skip leading whitespace on the next line
			while (Index < Text.Length && Text[Index] == ' ')
			{
				Index++;
			}
		}

		if (Result.Count == 0)
		{
			Result.Add(string.Empty);
		}

		return Result;
	}
}
