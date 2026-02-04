# Styling & Colors

This document covers the color system, text attributes, and visual styling in Glyph.

## Color System

Glyph supports three color modes:

1. **Standard 16 colors** - Basic ANSI colors
2. **256-color palette** - Extended terminal colors
3. **True color (24-bit)** - Full RGB support

### Standard Colors

```csharp
// Foreground colors
Color.Black
Color.Red
Color.Green
Color.Yellow
Color.Blue
Color.Magenta
Color.Cyan
Color.White

// Bright variants
Color.BrightBlack   // Gray
Color.BrightRed
Color.BrightGreen
Color.BrightYellow
Color.BrightBlue
Color.BrightMagenta
Color.BrightCyan
Color.BrightWhite

// Special
Color.Default       // Terminal default
```

### 256-Color Palette

```csharp
// Create from palette index (0-255)
var orange = Color.From256(208);
var purple = Color.From256(93);
var gray = Color.From256(240);

// Grayscale ramp (232-255)
var darkGray = Color.From256(236);
var lightGray = Color.From256(250);
```

### True Color (24-bit RGB)

```csharp
// From RGB values
var custom = Color.FromRgb(255, 128, 0);      // Orange
var teal = Color.FromRgb(0, 128, 128);

// From hex string
var coral = Color.FromHex("#FF7F50");
var navy = Color.FromHex("#000080");
var lime = Color.FromHex("32CD32");           // # is optional
```

## Using Colors

### Drawing Text

```csharp
// Simple colored text
Screen.DrawString(x, y, "Hello!", Color.Green);

// With background color
Screen.DrawString(x, y, "Warning", Color.Black, Color.Yellow);

// With text attributes
Screen.DrawString(x, y, "Bold!", Color.White, Color.Default, TextAttribute.Bold);
```

### Setting Individual Cells

```csharp
// Set a single cell
Screen.SetCell(x, y, 'X', Color.Red);
Screen.SetCell(x, y, 'X', Color.Red, Color.Blue);

// Fill a rectangle
Screen.FillRect(x, y, width, height, ' ', Color.Default, Color.DarkBlue);
```

### Drawing Shapes

```csharp
// Horizontal line
Screen.DrawHLine(x, y, length, '─', Color.Gray);

// Vertical line
Screen.DrawVLine(x, y, length, '│', Color.Gray);

// Box with border
Screen.DrawBox(x, y, width, height, Color.Cyan);
Screen.DrawBox(x, y, width, height, Color.Cyan, Color.Default, BoxStyle.Rounded);
```

## Text Attributes

```csharp
public enum TextAttribute
{
    None = 0,
    Bold = 1,
    Dim = 2,
    Italic = 4,
    Underline = 8,
    Blink = 16,
    Reverse = 32,
    Hidden = 64,
    Strikethrough = 128
}
```

### Combining Attributes

```csharp
// Single attribute
Screen.DrawString(x, y, "Bold", Color.White, Color.Default, TextAttribute.Bold);

// Multiple attributes (combine with |)
var attrs = TextAttribute.Bold | TextAttribute.Underline;
Screen.DrawString(x, y, "Bold + Underline", Color.White, Color.Default, attrs);
```

### Attribute Examples

```csharp
// Bold - brighter/thicker text
Screen.DrawString(0, 0, "Bold Text", Color.White, Color.Default, TextAttribute.Bold);

// Dim - reduced intensity
Screen.DrawString(0, 1, "Dim Text", Color.White, Color.Default, TextAttribute.Dim);

// Italic - slanted text (terminal support varies)
Screen.DrawString(0, 2, "Italic Text", Color.White, Color.Default, TextAttribute.Italic);

// Underline
Screen.DrawString(0, 3, "Underlined", Color.White, Color.Default, TextAttribute.Underline);

// Reverse - swap foreground/background
Screen.DrawString(0, 4, "Reversed", Color.White, Color.Default, TextAttribute.Reverse);

// Strikethrough
Screen.DrawString(0, 5, "Deleted", Color.White, Color.Default, TextAttribute.Strikethrough);
```

## Box Drawing Characters

Glyph provides constants for box drawing:

```csharp
public static class BoxChars
{
    // Single line
    public const char Horizontal = '─';
    public const char Vertical = '│';
    public const char TopLeft = '┌';
    public const char TopRight = '┐';
    public const char BottomLeft = '└';
    public const char BottomRight = '┘';
    public const char VerticalRight = '├';
    public const char VerticalLeft = '┤';
    public const char HorizontalDown = '┬';
    public const char HorizontalUp = '┴';
    public const char Cross = '┼';

    // Rounded corners
    public const char RoundTopLeft = '╭';
    public const char RoundTopRight = '╮';
    public const char RoundBottomLeft = '╰';
    public const char RoundBottomRight = '╯';

    // Double line
    public const char DoubleHorizontal = '═';
    public const char DoubleVertical = '║';
    public const char DoubleTopLeft = '╔';
    public const char DoubleTopRight = '╗';
    public const char DoubleBottomLeft = '╚';
    public const char DoubleBottomRight = '╝';

    // Block elements
    public const char FullBlock = '█';
    public const char LightShade = '░';
    public const char MediumShade = '▒';
    public const char DarkShade = '▓';
}
```

### Box Styles

```csharp
public enum BoxStyle
{
    Single,     // ┌─┐ │ └─┘
    Double,     // ╔═╗ ║ ╚═╝
    Rounded,    // ╭─╮ │ ╰─╯
    Heavy,      // ┏━┓ ┃ ┗━┛
    Ascii       // +-+ | +-+
}

// Usage
Screen.DrawBox(0, 0, 20, 5, Color.White, Color.Default, BoxStyle.Rounded);
Screen.DrawBox(0, 6, 20, 5, Color.White, Color.Default, BoxStyle.Double);
```

## Terminal Compatibility

### Color Support Detection

Most modern terminals support true color, but for maximum compatibility:

| Terminal | 16 | 256 | True Color |
|----------|----|----|------------|
| Windows Terminal | Yes | Yes | Yes |
| iTerm2 | Yes | Yes | Yes |
| GNOME Terminal | Yes | Yes | Yes |
| Konsole | Yes | Yes | Yes |
| Alacritty | Yes | Yes | Yes |
| VS Code Terminal | Yes | Yes | Yes |
| xterm | Yes | Yes | Yes* |
| PuTTY | Yes | Yes | No |
| cmd.exe | Yes | No | No |

*Requires configuration

### Attribute Support

Not all terminals support all text attributes:

| Attribute | Support |
|-----------|---------|
| Bold | Universal |
| Dim | Most |
| Underline | Universal |
| Reverse | Universal |
| Italic | Most modern |
| Strikethrough | Some |
| Blink | Rare |

## Theming

### Creating a Theme

```csharp
public class Theme
{
    public Color Background { get; init; } = Color.Default;
    public Color Foreground { get; init; } = Color.White;
    public Color BorderColor { get; init; } = Color.BrightBlack;
    public Color TitleColor { get; init; } = Color.BrightWhite;
    public Color ErrorColor { get; init; } = Color.Red;
    public Color SuccessColor { get; init; } = Color.Green;
    public Color WarningColor { get; init; } = Color.Yellow;
    public Color InfoColor { get; init; } = Color.Cyan;
    public Color AccentColor { get; init; } = Color.Blue;

    public static Theme Default { get; } = new();

    public static Theme Dark { get; } = new()
    {
        Background = Color.FromRgb(30, 30, 30),
        Foreground = Color.FromRgb(220, 220, 220),
        BorderColor = Color.FromRgb(80, 80, 80),
        AccentColor = Color.FromRgb(0, 150, 255)
    };

    public static Theme Light { get; } = new()
    {
        Background = Color.FromRgb(250, 250, 250),
        Foreground = Color.FromRgb(30, 30, 30),
        BorderColor = Color.FromRgb(180, 180, 180),
        AccentColor = Color.FromRgb(0, 100, 200)
    };
}
```

### Applying a Theme

```csharp
var theme = Theme.Dark;

root.OnDraw += (s, e) =>
{
    e.Screen.FillRect(0, 0, root.Width, root.Height, ' ', theme.Foreground, theme.Background);
    e.Screen.DrawBox(0, 0, root.Width, root.Height, theme.BorderColor);
    e.Screen.DrawString(2, 0, " My App ", theme.TitleColor, theme.Background, TextAttribute.Bold);
};
```

### ConsoleWindow Theming

The `ConsoleWindow` preset has built-in color properties:

```csharp
using Glyph.Presets;

var console = new ConsoleWindow
{
    Title = "Themed Console",

    // Background fills the entire window interior
    BackgroundColor = Color.FromRgb(15, 20, 45),

    // Border and separator lines
    BorderColor = Color.FromRgb(100, 140, 200),

    // Title in the top border
    TitleColor = Color.FromRgb(255, 220, 100),

    // Default text color (propagates to child components)
    TextColor = Color.FromRgb(220, 230, 255),

    // "Status" and "Command" separator labels
    SeparatorLabelColor = Color.FromRgb(255, 150, 80)
};
```

Color changes propagate to child components automatically:
- `BackgroundColor` sets `OutputView.Background`, `StatusPanel.Background`, `CommandInput.Background`
- `TextColor` sets `OutputView.Foreground`, `StatusPanel.ValueForeground`, `CommandInput.TextColor`

### Example Themes

**Cyberpunk (Dark Blue)**
```csharp
BackgroundColor = Color.FromRgb(15, 20, 45),
BorderColor = Color.FromRgb(100, 140, 200),
TitleColor = Color.FromRgb(255, 220, 100),
TextColor = Color.FromRgb(220, 230, 255)
```

**Matrix (Green on Black)**
```csharp
BackgroundColor = Color.FromRgb(0, 10, 0),
BorderColor = Color.FromRgb(0, 100, 0),
TitleColor = Color.BrightGreen,
TextColor = Color.Green
```

**Solarized Dark**
```csharp
BackgroundColor = Color.FromRgb(0, 43, 54),
BorderColor = Color.FromRgb(88, 110, 117),
TitleColor = Color.FromRgb(181, 137, 0),
TextColor = Color.FromRgb(147, 161, 161)
```

## Best Practices

1. **Use semantic colors** - Define colors by purpose (error, success) not appearance
2. **Test in multiple terminals** - Verify colors look acceptable across terminals
3. **Provide fallbacks** - Use 16-color variants for critical UI elements
4. **Consider accessibility** - Ensure sufficient contrast ratios
5. **Don't rely solely on color** - Use symbols/text in addition to color coding

## See Also

- [Components Guide](components.md) - Using built-in components
- [Custom Components](custom-components.md) - Building your own
- [Architecture](architecture.md) - How rendering works
