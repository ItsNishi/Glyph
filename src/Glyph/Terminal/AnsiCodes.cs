namespace Glyph.Terminal;

/// <summary>
/// Static class containing ANSI escape sequence constants and helper methods.
/// Supports VT100/VT220 compatible terminals on both Windows and Unix.
/// </summary>
public static class AnsiCodes
{
	// Escape sequence prefix
	public const string Escape = "\x1b";
	public const string Csi = "\x1b[";

	#region Cursor Movement

	/// <summary>Move cursor up N lines.</summary>
	public static string CursorUp(int n = 1) => $"{Csi}{n}A";

	/// <summary>Move cursor down N lines.</summary>
	public static string CursorDown(int n = 1) => $"{Csi}{n}B";

	/// <summary>Move cursor right N columns.</summary>
	public static string CursorRight(int n = 1) => $"{Csi}{n}C";

	/// <summary>Move cursor left N columns.</summary>
	public static string CursorLeft(int n = 1) => $"{Csi}{n}D";

	/// <summary>Move cursor to beginning of line N lines down.</summary>
	public static string CursorNextLine(int n = 1) => $"{Csi}{n}E";

	/// <summary>Move cursor to beginning of line N lines up.</summary>
	public static string CursorPrevLine(int n = 1) => $"{Csi}{n}F";

	/// <summary>Move cursor to column N.</summary>
	public static string CursorColumn(int n) => $"{Csi}{n}G";

	/// <summary>Move cursor to row, column (1-indexed).</summary>
	public static string CursorPosition(int row, int col) => $"{Csi}{row};{col}H";

	/// <summary>Save cursor position (DEC).</summary>
	public const string CursorSave = $"{Escape}7";

	/// <summary>Restore cursor position (DEC).</summary>
	public const string CursorRestore = $"{Escape}8";

	/// <summary>Save cursor position (SCO).</summary>
	public const string CursorSaveSco = $"{Csi}s";

	/// <summary>Restore cursor position (SCO).</summary>
	public const string CursorRestoreSco = $"{Csi}u";

	/// <summary>Hide cursor.</summary>
	public const string CursorHide = $"{Csi}?25l";

	/// <summary>Show cursor.</summary>
	public const string CursorShow = $"{Csi}?25h";

	/// <summary>Move cursor to home position (1,1).</summary>
	public const string CursorHome = $"{Csi}H";

	#endregion

	#region Screen Clearing

	/// <summary>Clear entire screen.</summary>
	public const string ClearScreen = $"{Csi}2J";

	/// <summary>Clear screen from cursor to end.</summary>
	public const string ClearScreenToEnd = $"{Csi}0J";

	/// <summary>Clear screen from cursor to beginning.</summary>
	public const string ClearScreenToBeginning = $"{Csi}1J";

	/// <summary>Clear entire line.</summary>
	public const string ClearLine = $"{Csi}2K";

	/// <summary>Clear line from cursor to end.</summary>
	public const string ClearLineToEnd = $"{Csi}0K";

	/// <summary>Clear line from cursor to beginning.</summary>
	public const string ClearLineToBeginning = $"{Csi}1K";

	#endregion

	#region Text Styles

	/// <summary>Reset all attributes.</summary>
	public const string Reset = $"{Csi}0m";

	/// <summary>Bold/increased intensity.</summary>
	public const string Bold = $"{Csi}1m";

	/// <summary>Dim/decreased intensity.</summary>
	public const string Dim = $"{Csi}2m";

	/// <summary>Italic.</summary>
	public const string Italic = $"{Csi}3m";

	/// <summary>Underline.</summary>
	public const string Underline = $"{Csi}4m";

	/// <summary>Slow blink.</summary>
	public const string Blink = $"{Csi}5m";

	/// <summary>Rapid blink.</summary>
	public const string BlinkRapid = $"{Csi}6m";

	/// <summary>Reverse video (swap foreground/background).</summary>
	public const string Reverse = $"{Csi}7m";

	/// <summary>Hidden/invisible text.</summary>
	public const string Hidden = $"{Csi}8m";

	/// <summary>Strikethrough.</summary>
	public const string Strikethrough = $"{Csi}9m";

	/// <summary>Reset bold/dim.</summary>
	public const string ResetBold = $"{Csi}22m";

	/// <summary>Reset italic.</summary>
	public const string ResetItalic = $"{Csi}23m";

	/// <summary>Reset underline.</summary>
	public const string ResetUnderline = $"{Csi}24m";

	/// <summary>Reset blink.</summary>
	public const string ResetBlink = $"{Csi}25m";

	/// <summary>Reset reverse.</summary>
	public const string ResetReverse = $"{Csi}27m";

	/// <summary>Reset hidden.</summary>
	public const string ResetHidden = $"{Csi}28m";

	/// <summary>Reset strikethrough.</summary>
	public const string ResetStrikethrough = $"{Csi}29m";

	#endregion

	#region Standard Colors (16 colors)

	// Foreground colors (30-37, 90-97 for bright)
	public const string FgBlack = $"{Csi}30m";
	public const string FgRed = $"{Csi}31m";
	public const string FgGreen = $"{Csi}32m";
	public const string FgYellow = $"{Csi}33m";
	public const string FgBlue = $"{Csi}34m";
	public const string FgMagenta = $"{Csi}35m";
	public const string FgCyan = $"{Csi}36m";
	public const string FgWhite = $"{Csi}37m";
	public const string FgDefault = $"{Csi}39m";

	// Bright foreground colors
	public const string FgBrightBlack = $"{Csi}90m";
	public const string FgBrightRed = $"{Csi}91m";
	public const string FgBrightGreen = $"{Csi}92m";
	public const string FgBrightYellow = $"{Csi}93m";
	public const string FgBrightBlue = $"{Csi}94m";
	public const string FgBrightMagenta = $"{Csi}95m";
	public const string FgBrightCyan = $"{Csi}96m";
	public const string FgBrightWhite = $"{Csi}97m";

	// Background colors (40-47, 100-107 for bright)
	public const string BgBlack = $"{Csi}40m";
	public const string BgRed = $"{Csi}41m";
	public const string BgGreen = $"{Csi}42m";
	public const string BgYellow = $"{Csi}43m";
	public const string BgBlue = $"{Csi}44m";
	public const string BgMagenta = $"{Csi}45m";
	public const string BgCyan = $"{Csi}46m";
	public const string BgWhite = $"{Csi}47m";
	public const string BgDefault = $"{Csi}49m";

	// Bright background colors
	public const string BgBrightBlack = $"{Csi}100m";
	public const string BgBrightRed = $"{Csi}101m";
	public const string BgBrightGreen = $"{Csi}102m";
	public const string BgBrightYellow = $"{Csi}103m";
	public const string BgBrightBlue = $"{Csi}104m";
	public const string BgBrightMagenta = $"{Csi}105m";
	public const string BgBrightCyan = $"{Csi}106m";
	public const string BgBrightWhite = $"{Csi}107m";

	#endregion

	#region 256 Color Support

	/// <summary>Set foreground to 256-color palette (0-255).</summary>
	public static string Fg256(byte color) => $"{Csi}38;5;{color}m";

	/// <summary>Set background to 256-color palette (0-255).</summary>
	public static string Bg256(byte color) => $"{Csi}48;5;{color}m";

	#endregion

	#region True Color (24-bit RGB)

	/// <summary>Set foreground to RGB color.</summary>
	public static string FgRgb(byte r, byte g, byte b) => $"{Csi}38;2;{r};{g};{b}m";

	/// <summary>Set background to RGB color.</summary>
	public static string BgRgb(byte r, byte g, byte b) => $"{Csi}48;2;{r};{g};{b}m";

	#endregion

	#region Mouse Support

	/// <summary>Enable mouse tracking (X10 mode - button press only).</summary>
	public const string MouseEnableX10 = $"{Csi}?9h";

	/// <summary>Disable mouse tracking (X10 mode).</summary>
	public const string MouseDisableX10 = $"{Csi}?9l";

	/// <summary>Enable mouse tracking (button press/release).</summary>
	public const string MouseEnableNormal = $"{Csi}?1000h";

	/// <summary>Disable mouse tracking (button press/release).</summary>
	public const string MouseDisableNormal = $"{Csi}?1000l";

	/// <summary>Enable mouse tracking with highlighting.</summary>
	public const string MouseEnableHighlight = $"{Csi}?1001h";

	/// <summary>Disable mouse tracking with highlighting.</summary>
	public const string MouseDisableHighlight = $"{Csi}?1001l";

	/// <summary>Enable mouse button event tracking.</summary>
	public const string MouseEnableButton = $"{Csi}?1002h";

	/// <summary>Disable mouse button event tracking.</summary>
	public const string MouseDisableButton = $"{Csi}?1002l";

	/// <summary>Enable mouse any event tracking (movement + buttons).</summary>
	public const string MouseEnableAny = $"{Csi}?1003h";

	/// <summary>Disable mouse any event tracking.</summary>
	public const string MouseDisableAny = $"{Csi}?1003l";

	/// <summary>Enable SGR extended mouse mode (better coordinate support).</summary>
	public const string MouseEnableSgr = $"{Csi}?1006h";

	/// <summary>Disable SGR extended mouse mode.</summary>
	public const string MouseDisableSgr = $"{Csi}?1006l";

	#endregion

	#region Alternate Screen Buffer

	/// <summary>Enter alternate screen buffer.</summary>
	public const string AltScreenEnter = $"{Csi}?1049h";

	/// <summary>Exit alternate screen buffer.</summary>
	public const string AltScreenExit = $"{Csi}?1049l";

	/// <summary>Enter alternate screen buffer (older method).</summary>
	public const string AltScreenEnterLegacy = $"{Csi}?47h";

	/// <summary>Exit alternate screen buffer (older method).</summary>
	public const string AltScreenExitLegacy = $"{Csi}?47l";

	#endregion

	#region Terminal Modes

	/// <summary>Enable line wrap.</summary>
	public const string LineWrapEnable = $"{Csi}?7h";

	/// <summary>Disable line wrap.</summary>
	public const string LineWrapDisable = $"{Csi}?7l";

	/// <summary>Enable bracketed paste mode.</summary>
	public const string BracketedPasteEnable = $"{Csi}?2004h";

	/// <summary>Disable bracketed paste mode.</summary>
	public const string BracketedPasteDisable = $"{Csi}?2004l";

	#endregion

	#region Helper Methods

	/// <summary>
	/// Build a complete SGR (Select Graphic Rendition) sequence with multiple attributes.
	/// </summary>
	public static string Sgr(params int[] codes)
	{
		return $"{Csi}{string.Join(";", codes)}m";
	}

	/// <summary>
	/// Combine foreground color, background color, and attributes into a single sequence.
	/// </summary>
	public static string Style(Color? fg = null, Color? bg = null, TextAttribute attrs = TextAttribute.None)
	{
		var parts = new List<int>();

		// Add attribute codes
		if (attrs.HasFlag(TextAttribute.Bold)) parts.Add(1);
		if (attrs.HasFlag(TextAttribute.Dim)) parts.Add(2);
		if (attrs.HasFlag(TextAttribute.Italic)) parts.Add(3);
		if (attrs.HasFlag(TextAttribute.Underline)) parts.Add(4);
		if (attrs.HasFlag(TextAttribute.Blink)) parts.Add(5);
		if (attrs.HasFlag(TextAttribute.Reverse)) parts.Add(7);
		if (attrs.HasFlag(TextAttribute.Hidden)) parts.Add(8);
		if (attrs.HasFlag(TextAttribute.Strikethrough)) parts.Add(9);

		// Add foreground color
		if (fg.HasValue)
		{
			parts.AddRange(fg.Value.ToForegroundCodes());
		}

		// Add background color
		if (bg.HasValue)
		{
			parts.AddRange(bg.Value.ToBackgroundCodes());
		}

		if (parts.Count == 0)
		{
			return string.Empty;
		}

		return Sgr(parts.ToArray());
	}

	/// <summary>
	/// Writes raw text to the console without any processing.
	/// </summary>
	public static void RawWrite(string text)
	{
		Console.Write(text);
	}

	/// <summary>
	/// Writes raw text to the console followed by a newline.
	/// </summary>
	public static void RawWriteLine(string text)
	{
		Console.WriteLine(text);
	}

	/// <summary>
	/// Flushes the console output stream.
	/// </summary>
	public static void Flush()
	{
		Console.Out.Flush();
	}

	/// <summary>
	/// Initialize terminal for TUI mode (hide cursor, enter alt screen, enable mouse).
	/// </summary>
	public static void InitializeTui(bool useMouse = true)
	{
		EnableVirtualTerminal();
		Console.Write(AltScreenEnter);
		Console.Write(CursorHide);
		Console.Write(ClearScreen);
		Console.Write(CursorHome);

		if (useMouse)
		{
			Console.Write(MouseEnableNormal);
			Console.Write(MouseEnableSgr);
		}

		Console.Out.Flush();
	}

	/// <summary>
	/// Restore terminal to normal mode (show cursor, exit alt screen, disable mouse).
	/// </summary>
	public static void RestoreTui()
	{
		Console.Write(MouseDisableSgr);
		Console.Write(MouseDisableNormal);
		Console.Write(CursorShow);
		Console.Write(AltScreenExit);
		Console.Write(Reset);
		Console.Out.Flush();
	}

	/// <summary>
	/// Enable virtual terminal processing on Windows.
	/// On Unix systems, this is typically enabled by default.
	/// </summary>
	public static void EnableVirtualTerminal()
	{
		if (OperatingSystem.IsWindows())
		{
			EnableWindowsVirtualTerminal();
		}
		// Unix terminals typically support ANSI by default
	}

	private static void EnableWindowsVirtualTerminal()
	{
		// Use P/Invoke to enable ANSI escape sequences on Windows
		try
		{
			var handle = GetStdHandle(-11); // STD_OUTPUT_HANDLE
			if (GetConsoleMode(handle, out uint mode))
			{
				mode |= 0x0004; // ENABLE_VIRTUAL_TERMINAL_PROCESSING
				SetConsoleMode(handle, mode);
			}
		}
		catch (EntryPointNotFoundException)
		{
			// P/Invoke entry point not found - running on non-Windows or unsupported platform
		}
		catch (System.ComponentModel.Win32Exception)
		{
			// Windows API call failed - virtual terminal might already be enabled or not supported
		}
	}

	[System.Runtime.InteropServices.DllImport("kernel32.dll")]
	private static extern IntPtr GetStdHandle(int nStdHandle);

	[System.Runtime.InteropServices.DllImport("kernel32.dll")]
	private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

	[System.Runtime.InteropServices.DllImport("kernel32.dll")]
	private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

	#endregion
}

/// <summary>
/// Text attributes that can be combined using flags.
/// </summary>
[Flags]
public enum TextAttribute : byte
{
	None = 0,
	Bold = 1 << 0,
	Dim = 1 << 1,
	Italic = 1 << 2,
	Underline = 1 << 3,
	Blink = 1 << 4,
	Reverse = 1 << 5,
	Hidden = 1 << 6,
	Strikethrough = 1 << 7
}

/// <summary>
/// Represents a terminal color with support for default, 16-color, 256-color, and true color modes.
/// </summary>
public readonly struct Color : IEquatable<Color>
{
	private readonly ColorMode _mode;
	private readonly byte _index;
	private readonly byte _r;
	private readonly byte _g;
	private readonly byte _b;

	private Color(ColorMode mode, byte index = 0, byte r = 0, byte g = 0, byte b = 0)
	{
		_mode = mode;
		_index = index;
		_r = r;
		_g = g;
		_b = b;
	}

	/// <summary>Default terminal color.</summary>
	public static Color Default => new(ColorMode.Default);

	// Standard 16 colors
	public static Color Black => new(ColorMode.Standard, 0);
	public static Color Red => new(ColorMode.Standard, 1);
	public static Color Green => new(ColorMode.Standard, 2);
	public static Color Yellow => new(ColorMode.Standard, 3);
	public static Color Blue => new(ColorMode.Standard, 4);
	public static Color Magenta => new(ColorMode.Standard, 5);
	public static Color Cyan => new(ColorMode.Standard, 6);
	public static Color White => new(ColorMode.Standard, 7);
	public static Color BrightBlack => new(ColorMode.Standard, 8);
	public static Color BrightRed => new(ColorMode.Standard, 9);
	public static Color BrightGreen => new(ColorMode.Standard, 10);
	public static Color BrightYellow => new(ColorMode.Standard, 11);
	public static Color BrightBlue => new(ColorMode.Standard, 12);
	public static Color BrightMagenta => new(ColorMode.Standard, 13);
	public static Color BrightCyan => new(ColorMode.Standard, 14);
	public static Color BrightWhite => new(ColorMode.Standard, 15);

	/// <summary>Create a color from the 256-color palette.</summary>
	public static Color From256(byte index) => new(ColorMode.Palette256, index);

	/// <summary>Create a true color (24-bit RGB).</summary>
	public static Color FromRgb(byte r, byte g, byte b) => new(ColorMode.TrueColor, 0, r, g, b);

	/// <summary>Create a color from a hex string (e.g., "#FF5500" or "FF5500").</summary>
	public static Color FromHex(string hex)
	{
		hex = hex.TrimStart('#');
		if (hex.Length != 6)
		{
			throw new ArgumentException("Hex color must be 6 characters", nameof(hex));
		}

		var r = Convert.ToByte(hex[0..2], 16);
		var g = Convert.ToByte(hex[2..4], 16);
		var b = Convert.ToByte(hex[4..6], 16);
		return FromRgb(r, g, b);
	}

	// Public accessors for RGB components
	public byte R => _r;
	public byte G => _g;
	public byte B => _b;
	public bool IsTrueColor => _mode == ColorMode.TrueColor;

	/// <summary>
	/// Linear interpolation between two true colors.
	/// </summary>
	public static Color Lerp(Color A, Color B, float T)
	{
		T = Math.Clamp(T, 0f, 1f);
		return FromRgb(
			(byte)(A._r + (B._r - A._r) * T),
			(byte)(A._g + (B._g - A._g) * T),
			(byte)(A._b + (B._b - A._b) * T)
		);
	}

	/// <summary>Generate ANSI codes for foreground color.</summary>
	public int[] ToForegroundCodes()
	{
		return _mode switch
		{
			ColorMode.Default => [39],
			ColorMode.Standard => _index < 8 ? [30 + _index] : [90 + (_index - 8)],
			ColorMode.Palette256 => [38, 5, _index],
			ColorMode.TrueColor => [38, 2, _r, _g, _b],
			_ => [39]
		};
	}

	/// <summary>Generate ANSI codes for background color.</summary>
	public int[] ToBackgroundCodes()
	{
		return _mode switch
		{
			ColorMode.Default => [49],
			ColorMode.Standard => _index < 8 ? [40 + _index] : [100 + (_index - 8)],
			ColorMode.Palette256 => [48, 5, _index],
			ColorMode.TrueColor => [48, 2, _r, _g, _b],
			_ => [49]
		};
	}

	public bool Equals(Color other)
	{
		return _mode == other._mode && _index == other._index &&
		       _r == other._r && _g == other._g && _b == other._b;
	}

	public override bool Equals(object? obj) => obj is Color other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(_mode, _index, _r, _g, _b);

	public static bool operator ==(Color left, Color right) => left.Equals(right);

	public static bool operator !=(Color left, Color right) => !left.Equals(right);

	private enum ColorMode : byte
	{
		Default,
		Standard,
		Palette256,
		TrueColor
	}
}
