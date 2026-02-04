using System.Text;

namespace Glyph.Terminal;

/// <summary>
/// Represents a single cell in the terminal screen buffer.
/// </summary>
public struct Cell : IEquatable<Cell>
{
	public char Char;
	public Color Foreground;
	public Color Background;
	public TextAttribute Attributes;

	public Cell()
	{
		Char = ' ';
		Foreground = Color.Default;
		Background = Color.Default;
		Attributes = TextAttribute.None;
	}

	public Cell(char c, Color fg = default, Color bg = default, TextAttribute attrs = TextAttribute.None)
	{
		Char = c;
		Foreground = fg == default ? Color.Default : fg;
		Background = bg == default ? Color.Default : bg;
		Attributes = attrs;
	}

	public readonly bool Equals(Cell other)
	{
		return Char == other.Char &&
		       Foreground == other.Foreground &&
		       Background == other.Background &&
		       Attributes == other.Attributes;
	}

	public override readonly bool Equals(object? obj) => obj is Cell other && Equals(other);

	public override readonly int GetHashCode() => HashCode.Combine(Char, Foreground, Background, Attributes);

	public static bool operator ==(Cell left, Cell right) => left.Equals(right);

	public static bool operator !=(Cell left, Cell right) => !left.Equals(right);

	/// <summary>Empty cell with space character and default colors.</summary>
	public static Cell Empty => new(' ', Color.Default, Color.Default, TextAttribute.None);
}

/// <summary>
/// Unicode box drawing characters for drawing borders and frames.
/// </summary>
public static class BoxChars
{
	// Single line box drawing
	public const char Horizontal = '\u2500';          // -
	public const char Vertical = '\u2502';            // |
	public const char TopLeft = '\u250C';             // +
	public const char TopRight = '\u2510';            // +
	public const char BottomLeft = '\u2514';          // +
	public const char BottomRight = '\u2518';         // +
	public const char VerticalRight = '\u251C';       // |-
	public const char VerticalLeft = '\u2524';        // -|
	public const char HorizontalDown = '\u252C';      // T
	public const char HorizontalUp = '\u2534';        // upside-down T
	public const char Cross = '\u253C';               // +

	// Double line box drawing
	public const char DoubleHorizontal = '\u2550';
	public const char DoubleVertical = '\u2551';
	public const char DoubleTopLeft = '\u2554';
	public const char DoubleTopRight = '\u2557';
	public const char DoubleBottomLeft = '\u255A';
	public const char DoubleBottomRight = '\u255D';
	public const char DoubleVerticalRight = '\u2560';
	public const char DoubleVerticalLeft = '\u2563';
	public const char DoubleHorizontalDown = '\u2566';
	public const char DoubleHorizontalUp = '\u2569';
	public const char DoubleCross = '\u256C';

	// Rounded corners
	public const char RoundedTopLeft = '\u256D';
	public const char RoundedTopRight = '\u256E';
	public const char RoundedBottomLeft = '\u2570';
	public const char RoundedBottomRight = '\u256F';

	// Block elements
	public const char FullBlock = '\u2588';
	public const char LightShade = '\u2591';
	public const char MediumShade = '\u2592';
	public const char DarkShade = '\u2593';
	public const char UpperHalf = '\u2580';
	public const char LowerHalf = '\u2584';
	public const char LeftHalf = '\u258C';
	public const char RightHalf = '\u2590';
}

/// <summary>
/// Box style presets for DrawBox operations.
/// </summary>
public enum BoxStyle
{
	Single,
	Double,
	Rounded,
	Ascii
}

/// <summary>
/// Double-buffered screen rendering with diff-based updates.
/// Only writes changed cells to minimize terminal output.
/// </summary>
public class Screen : IDisposable
{
	private Cell[] _frontBuffer;
	private Cell[] _backBuffer;
	private int _width;
	private int _height;
	private bool _disposed;
	private readonly StringBuilder _outputBuilder;
	private Color _lastFg;
	private Color _lastBg;
	private TextAttribute _lastAttrs;

	/// <summary>Current screen width in characters.</summary>
	public int Width => _width;

	/// <summary>Current screen height in characters.</summary>
	public int Height => _height;

	/// <summary>Total number of cells.</summary>
	public int CellCount => _width * _height;

	/// <summary>Event raised when terminal is resized.</summary>
	public event EventHandler<ResizeEventArgs>? Resized;

	public Screen()
	{
		_width = Console.WindowWidth;
		_height = Console.WindowHeight;
		_frontBuffer = new Cell[_width * _height];
		_backBuffer = new Cell[_width * _height];
		_outputBuilder = new StringBuilder(4096);
		_lastFg = Color.Default;
		_lastBg = Color.Default;
		_lastAttrs = TextAttribute.None;

		InitializeBuffers();
	}

	public Screen(int width, int height)
	{
		_width = width;
		_height = height;
		_frontBuffer = new Cell[_width * _height];
		_backBuffer = new Cell[_width * _height];
		_outputBuilder = new StringBuilder(4096);
		_lastFg = Color.Default;
		_lastBg = Color.Default;
		_lastAttrs = TextAttribute.None;

		InitializeBuffers();
	}

	private void InitializeBuffers()
	{
		for (int i = 0; i < _frontBuffer.Length; i++)
		{
			_frontBuffer[i] = Cell.Empty;
			_backBuffer[i] = Cell.Empty;
		}
	}

	/// <summary>
	/// Check for terminal resize and update buffers if needed.
	/// </summary>
	/// <returns>True if the terminal was resized.</returns>
	public bool CheckResize()
	{
		int newWidth = Console.WindowWidth;
		int newHeight = Console.WindowHeight;

		if (newWidth != _width || newHeight != _height)
		{
			Resize(newWidth, newHeight);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Resize the screen buffers to the specified dimensions.
	/// </summary>
	public void Resize(int newWidth, int newHeight)
	{
		if (newWidth == _width && newHeight == _height)
		{
			return;
		}

		_width = newWidth;
		_height = newHeight;

		_frontBuffer = new Cell[_width * _height];
		_backBuffer = new Cell[_width * _height];

		// Initialize back buffer to empty cells
		for (int i = 0; i < _backBuffer.Length; i++)
		{
			_backBuffer[i] = Cell.Empty;
		}

		// Initialize front buffer to a different value to force full redraw
		// This ensures all cells will be written on next render
		for (int i = 0; i < _frontBuffer.Length; i++)
		{
			_frontBuffer[i] = new Cell('\0', Color.Default, Color.Default, TextAttribute.None);
		}

		// Don't clear screen here - let RenderFull handle it atomically
		Resized?.Invoke(this, new ResizeEventArgs(_width, _height));
	}

	/// <summary>
	/// Get the buffer index for a given position.
	/// </summary>
	private int GetIndex(int x, int y) => y * _width + x;

	/// <summary>
	/// Check if coordinates are within bounds.
	/// </summary>
	public bool InBounds(int x, int y) => x >= 0 && x < _width && y >= 0 && y < _height;

	/// <summary>
	/// Set a cell in the back buffer.
	/// </summary>
	public void SetCell(int x, int y, char c, Color fg = default, Color bg = default, TextAttribute attrs = TextAttribute.None)
	{
		if (!InBounds(x, y))
		{
			return;
		}

		int index = GetIndex(x, y);
		_backBuffer[index] = new Cell(c, fg, bg, attrs);
	}

	/// <summary>
	/// Set a cell in the back buffer using a Cell struct.
	/// </summary>
	public void SetCell(int x, int y, Cell cell)
	{
		if (!InBounds(x, y))
		{
			return;
		}

		int index = GetIndex(x, y);
		_backBuffer[index] = cell;
	}

	/// <summary>
	/// Get a cell from the back buffer.
	/// </summary>
	public Cell GetCell(int x, int y)
	{
		if (!InBounds(x, y))
		{
			return Cell.Empty;
		}

		return _backBuffer[GetIndex(x, y)];
	}

	/// <summary>
	/// Draw a string horizontally starting at (x, y).
	/// </summary>
	public void DrawString(int x, int y, string text, Color fg = default, Color bg = default, TextAttribute attrs = TextAttribute.None)
	{
		if (y < 0 || y >= _height)
		{
			return;
		}

		for (int i = 0; i < text.Length; i++)
		{
			int drawX = x + i;
			if (drawX < 0)
			{
				continue;
			}
			if (drawX >= _width)
			{
				break;
			}

			SetCell(drawX, y, text[i], fg, bg, attrs);
		}
	}

	/// <summary>
	/// Draw a string vertically starting at (x, y).
	/// </summary>
	public void DrawStringVertical(int x, int y, string text, Color fg = default, Color bg = default, TextAttribute attrs = TextAttribute.None)
	{
		if (x < 0 || x >= _width)
		{
			return;
		}

		for (int i = 0; i < text.Length; i++)
		{
			int drawY = y + i;
			if (drawY < 0)
			{
				continue;
			}
			if (drawY >= _height)
			{
				break;
			}

			SetCell(x, drawY, text[i], fg, bg, attrs);
		}
	}

	/// <summary>
	/// Draw a box with the specified style.
	/// </summary>
	public void DrawBox(int x, int y, int width, int height, Color fg = default, Color bg = default, BoxStyle style = BoxStyle.Single)
	{
		if (width < 2 || height < 2)
		{
			return;
		}

		char horizontal, vertical, topLeft, topRight, bottomLeft, bottomRight;

		switch (style)
		{
			case BoxStyle.Double:
				horizontal = BoxChars.DoubleHorizontal;
				vertical = BoxChars.DoubleVertical;
				topLeft = BoxChars.DoubleTopLeft;
				topRight = BoxChars.DoubleTopRight;
				bottomLeft = BoxChars.DoubleBottomLeft;
				bottomRight = BoxChars.DoubleBottomRight;
				break;
			case BoxStyle.Rounded:
				horizontal = BoxChars.Horizontal;
				vertical = BoxChars.Vertical;
				topLeft = BoxChars.RoundedTopLeft;
				topRight = BoxChars.RoundedTopRight;
				bottomLeft = BoxChars.RoundedBottomLeft;
				bottomRight = BoxChars.RoundedBottomRight;
				break;
			case BoxStyle.Ascii:
				horizontal = '-';
				vertical = '|';
				topLeft = '+';
				topRight = '+';
				bottomLeft = '+';
				bottomRight = '+';
				break;
			default: // Single
				horizontal = BoxChars.Horizontal;
				vertical = BoxChars.Vertical;
				topLeft = BoxChars.TopLeft;
				topRight = BoxChars.TopRight;
				bottomLeft = BoxChars.BottomLeft;
				bottomRight = BoxChars.BottomRight;
				break;
		}

		// Corners
		SetCell(x, y, topLeft, fg, bg);
		SetCell(x + width - 1, y, topRight, fg, bg);
		SetCell(x, y + height - 1, bottomLeft, fg, bg);
		SetCell(x + width - 1, y + height - 1, bottomRight, fg, bg);

		// Horizontal edges
		for (int i = 1; i < width - 1; i++)
		{
			SetCell(x + i, y, horizontal, fg, bg);
			SetCell(x + i, y + height - 1, horizontal, fg, bg);
		}

		// Vertical edges
		for (int i = 1; i < height - 1; i++)
		{
			SetCell(x, y + i, vertical, fg, bg);
			SetCell(x + width - 1, y + i, vertical, fg, bg);
		}
	}

	/// <summary>
	/// Fill a rectangular region with a cell.
	/// </summary>
	public void FillRect(int x, int y, int width, int height, Cell cell)
	{
		for (int row = y; row < y + height; row++)
		{
			for (int col = x; col < x + width; col++)
			{
				SetCell(col, row, cell);
			}
		}
	}

	/// <summary>
	/// Fill a rectangular region with a character and colors.
	/// </summary>
	public void FillRect(int x, int y, int width, int height, char c = ' ', Color fg = default, Color bg = default, TextAttribute attrs = TextAttribute.None)
	{
		FillRect(x, y, width, height, new Cell(c, fg, bg, attrs));
	}

	/// <summary>
	/// Draw a horizontal line.
	/// </summary>
	public void DrawHLine(int x, int y, int length, char c = BoxChars.Horizontal, Color fg = default, Color bg = default)
	{
		for (int i = 0; i < length; i++)
		{
			SetCell(x + i, y, c, fg, bg);
		}
	}

	/// <summary>
	/// Draw a vertical line.
	/// </summary>
	public void DrawVLine(int x, int y, int length, char c = BoxChars.Vertical, Color fg = default, Color bg = default)
	{
		for (int i = 0; i < length; i++)
		{
			SetCell(x, y + i, c, fg, bg);
		}
	}

	/// <summary>
	/// Clear the back buffer to empty cells.
	/// </summary>
	public void Clear()
	{
		for (int i = 0; i < _backBuffer.Length; i++)
		{
			_backBuffer[i] = Cell.Empty;
		}
	}

	/// <summary>
	/// Clear the back buffer with a specific cell.
	/// </summary>
	public void Clear(Cell cell)
	{
		for (int i = 0; i < _backBuffer.Length; i++)
		{
			_backBuffer[i] = cell;
		}
	}

	/// <summary>
	/// Clear the back buffer with a specific background color.
	/// </summary>
	public void Clear(Color bg)
	{
		var cell = new Cell(' ', Color.Default, bg, TextAttribute.None);
		Clear(cell);
	}

	/// <summary>
	/// Render the back buffer to the terminal, only writing changed cells.
	/// </summary>
	public void Render()
	{
		_outputBuilder.Clear();
		_lastFg = Color.Default;
		_lastBg = Color.Default;
		_lastAttrs = TextAttribute.None;

		bool needsReset = true;
		int lastX = -1;
		int lastY = -1;

		for (int y = 0; y < _height; y++)
		{
			for (int x = 0; x < _width; x++)
			{
				int index = GetIndex(x, y);
				ref Cell back = ref _backBuffer[index];
				ref Cell front = ref _frontBuffer[index];

				if (back != front)
				{
					// Position cursor if not sequential
					if (lastY != y || lastX != x - 1)
					{
						_outputBuilder.Append(AnsiCodes.CursorPosition(y + 1, x + 1));
					}

					// Apply style changes if needed
					if (needsReset || back.Attributes != _lastAttrs || back.Foreground != _lastFg || back.Background != _lastBg)
					{
						AppendStyleChange(back.Foreground, back.Background, back.Attributes, needsReset);
						needsReset = false;
					}

					_outputBuilder.Append(back.Char);
					front = back;

					lastX = x;
					lastY = y;
				}
			}
		}

		// Reset styling at end
		if (_outputBuilder.Length > 0)
		{
			_outputBuilder.Append(AnsiCodes.Reset);
		}

		// Write all at once
		if (_outputBuilder.Length > 0)
		{
			RawWrite(_outputBuilder.ToString());
		}
	}

	/// <summary>
	/// Force a full redraw of the entire screen.
	/// </summary>
	public void RenderFull()
	{
		_outputBuilder.Clear();

		// Position at home and reset - no clear needed since we write every cell
		_outputBuilder.Append(AnsiCodes.CursorHome);
		_outputBuilder.Append(AnsiCodes.Reset);

		_lastFg = Color.Default;
		_lastBg = Color.Default;
		_lastAttrs = TextAttribute.None;

		for (int y = 0; y < _height; y++)
		{
			for (int x = 0; x < _width; x++)
			{
				int index = GetIndex(x, y);
				ref Cell back = ref _backBuffer[index];
				ref Cell front = ref _frontBuffer[index];

				// Apply style if different from last
				if (back.Attributes != _lastAttrs || back.Foreground != _lastFg || back.Background != _lastBg)
				{
					AppendStyleChange(back.Foreground, back.Background, back.Attributes, true);
				}

				_outputBuilder.Append(back.Char);
				front = back;
			}
		}

		_outputBuilder.Append(AnsiCodes.Reset);
		RawWrite(_outputBuilder.ToString());
	}

	private void AppendStyleChange(Color fg, Color bg, TextAttribute attrs, bool reset)
	{
		if (reset)
		{
			_outputBuilder.Append(AnsiCodes.Reset);
		}

		var codes = new List<int>();

		// Attributes
		if (attrs.HasFlag(TextAttribute.Bold)) codes.Add(1);
		if (attrs.HasFlag(TextAttribute.Dim)) codes.Add(2);
		if (attrs.HasFlag(TextAttribute.Italic)) codes.Add(3);
		if (attrs.HasFlag(TextAttribute.Underline)) codes.Add(4);
		if (attrs.HasFlag(TextAttribute.Blink)) codes.Add(5);
		if (attrs.HasFlag(TextAttribute.Reverse)) codes.Add(7);
		if (attrs.HasFlag(TextAttribute.Hidden)) codes.Add(8);
		if (attrs.HasFlag(TextAttribute.Strikethrough)) codes.Add(9);

		// Colors
		codes.AddRange(fg.ToForegroundCodes());
		codes.AddRange(bg.ToBackgroundCodes());

		if (codes.Count > 0)
		{
			_outputBuilder.Append(AnsiCodes.Sgr(codes.ToArray()));
		}

		_lastFg = fg;
		_lastBg = bg;
		_lastAttrs = attrs;
	}

	/// <summary>
	/// Write raw ANSI string to console.
	/// </summary>
	public void RawWrite(string text)
	{
		Console.Write(text);
		Console.Out.Flush();
	}

	/// <summary>
	/// Write raw ANSI string with position.
	/// </summary>
	public void RawWriteAt(int x, int y, string text)
	{
		Console.Write(AnsiCodes.CursorPosition(y + 1, x + 1));
		Console.Write(text);
		Console.Out.Flush();
	}

	/// <summary>
	/// Swap front and back buffers (useful for double-buffer animations).
	/// </summary>
	public void SwapBuffers()
	{
		(_frontBuffer, _backBuffer) = (_backBuffer, _frontBuffer);
	}

	/// <summary>
	/// Copy the front buffer to the back buffer.
	/// </summary>
	public void CopyFrontToBack()
	{
		Array.Copy(_frontBuffer, _backBuffer, _frontBuffer.Length);
	}

	/// <summary>
	/// Mark all cells as dirty to force a full redraw on next Render().
	/// </summary>
	public void Invalidate()
	{
		for (int i = 0; i < _frontBuffer.Length; i++)
		{
			// Set front buffer to something that won't match anything
			_frontBuffer[i] = new Cell('\0', Color.Default, Color.Default, TextAttribute.None);
		}
	}

	/// <summary>
	/// Get a read-only span of the back buffer for inspection.
	/// </summary>
	public ReadOnlySpan<Cell> GetBackBuffer() => _backBuffer;

	/// <summary>
	/// Get a read-only span of the front buffer for inspection.
	/// </summary>
	public ReadOnlySpan<Cell> GetFrontBuffer() => _frontBuffer;

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		GC.SuppressFinalize(this);
	}
}
