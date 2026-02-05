# Layouts Guide

Glyph provides layout containers for building complex multi-panel interfaces.

## SplitContainer

Splits the view into two panels, either horizontally or vertically.

```csharp
var split = new SplitContainer
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    SplitOrientation = Orientation.Vertical,  // Left/Right
    SplitPosition = 70  // Percentage (0-100)
};

split.Panel1 = new FrameView { Title = "Left" };
split.Panel2 = new FrameView { Title = "Right" };
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `SplitOrientation` | `Orientation` | `Vertical` (left/right) or `Horizontal` (top/bottom) |
| `SplitPosition` | `int` | Split position as percentage (10-90) |
| `Panel1` | `View?` | First panel (left or top) |
| `Panel2` | `View?` | Second panel (right or bottom) |

### Vertical Split (Left/Right)

```csharp
split.SplitOrientation = Orientation.Vertical;

// Panel1 = Left side
// Panel2 = Right side
```

### Horizontal Split (Top/Bottom)

```csharp
split.SplitOrientation = Orientation.Horizontal;

// Panel1 = Top
// Panel2 = Bottom
```

---

## Nested Splits

Create complex layouts by nesting SplitContainers:

```csharp
// Main horizontal split
var mainSplit = new SplitContainer
{
    SplitOrientation = Orientation.Horizontal,
    SplitPosition = 80
};

// Top area: vertical split
var topSplit = new SplitContainer
{
    SplitOrientation = Orientation.Vertical,
    SplitPosition = 70
};

topSplit.Panel1 = new OutputView();     // Main output (left)
topSplit.Panel2 = new StatusPanel();    // Status (right)

mainSplit.Panel1 = topSplit;            // Top
mainSplit.Panel2 = new CommandInput();  // Bottom (command input)
```

Result:
```
┌──────────────────────┬───────────┐
│                      │           │
│     Main Output      │  Status   │
│                      │           │
├──────────────────────┴───────────┤
│ > command input                  │
└──────────────────────────────────┘
```

---

## Using Presets

For common layouts, use the built-in presets.

### ConsoleWindow

Complete console layout with output, status panel, and command input.

```csharp
using Glyph.Presets;

var console = new ConsoleWindow
{
    Title = "My App"
};
console.SetupLayout(Console.WindowWidth, Console.WindowHeight);

// Access components
console.OutputView.AppendLine("Hello");
console.StatusPanel.Set("Key", "Value");
console.CommandInput.SetFocus();
```

#### Status Panel Layouts

The status panel supports three layout positions:

**Bottom (default)** - Horizontal row below output:
```csharp
console.StatusLayout = StatusLayout.Bottom;
console.StatusPanelHeight = 4;  // Number of rows
```
```
┌─────────────────────────────────┐
│ Output area                     │
│                                 │
├─ Status ────────────────────────┤
│ Key : Value                     │
├─ Command ───────────────────────┤
│ > _                             │
└─────────────────────────────────┘
```

**Left** - Vertical column on left side:
```csharp
console.StatusLayout = StatusLayout.Left;
console.StatusPanelWidth = 20;  // Column width
```
```
┌─ Status ──┬──────────────────────┐
│ Key : Val │ Output area          │
│           │                      │
├───────────┴──────────────────────┤
│ > _                              │
└──────────────────────────────────┘
```

**Right** - Vertical column on right side:
```csharp
console.StatusLayout = StatusLayout.Right;
console.StatusPanelWidth = 20;
```
```
┌──────────────────────┬─ Status ──┐
│ Output area          │ Key : Val │
│                      │           │
├──────────────────────┴───────────┤
│ > _                              │
└──────────────────────────────────┘
```

#### Border Style & Title Alignment

```csharp
// Border style: Single (default), Double, Rounded, Ascii, None
console.BorderStyle = BoxStyle.Double;

// Title alignment: Left (default), Center, Right
console.TitleAlignment = TitleAlignment.Center;
```

Internal separators automatically use junction characters matching the chosen border style.

#### Color Theming

Customize all visual elements:

```csharp
var console = new ConsoleWindow
{
    Title = "Themed Console",
    StatusLayout = StatusLayout.Right,
    StatusPanelWidth = 22,
    BorderStyle = BoxStyle.Double,
    TitleAlignment = TitleAlignment.Center,

    // Custom color scheme
    BackgroundColor = Color.FromRgb(15, 20, 45),
    BorderColor = Color.FromRgb(100, 140, 200),
    TitleColor = Color.FromRgb(255, 220, 100),
    TextColor = Color.FromRgb(220, 230, 255),
    SeparatorLabelColor = Color.FromRgb(255, 150, 80)
};
```

### ChatWindow

Chat/agent layout with message display and text input.

```csharp
using Glyph.Presets;

var chat = new ChatWindow
{
    Title = "AI Assistant",
    BorderStyle = BoxStyle.Rounded,        // Default: Rounded
    TitleAlignment = TitleAlignment.Center
};
chat.SetupLayout(Console.WindowWidth, Console.WindowHeight);

// Add messages
chat.AddSystemMessage("Welcome!");
chat.AddUserMessage("Hello");
chat.AddAssistantMessage("Hi there!");

// Streaming support
chat.BeginStreaming();
chat.AppendStreamingToken("Response ");
chat.AppendStreamingToken("text...");
chat.EndStreaming();
```

---

## Custom Layouts

Build your own layout from scratch:

```csharp
public class MyCustomWindow : Toplevel
{
    public OutputView MainOutput { get; }
    public SimpleTableView DataTable { get; }
    public CommandInput Input { get; }

    public MyCustomWindow()
    {
        Title = "My App";

        var split = new SplitContainer
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1,
            SplitOrientation = Orientation.Vertical,
            SplitPosition = 50
        };

        var outputFrame = new FrameView { Title = "Output" };
        MainOutput = new OutputView
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        outputFrame.Add(MainOutput);

        var tableFrame = new FrameView { Title = "Data" };
        DataTable = new SimpleTableView
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        tableFrame.Add(DataTable);

        split.Panel1 = outputFrame;
        split.Panel2 = tableFrame;

        Input = new CommandInput
        {
            X = 0,
            Y = Pos.AnchorEnd() - 1,
            Width = Dim.Fill()
        };

        Add(split, Input);

        Loaded += (_, _) => Input.SetFocus();
    }
}
```

---

## Layout Tips

1. **Use `Dim.Fill()`** for flexible sizing that adapts to terminal resize
2. **Use `Pos.AnchorEnd()`** to anchor elements to the bottom
3. **Wrap components in `FrameView`** for borders and titles
4. **Set focus** on the primary input after window loads
5. **Keep split positions** between 20-80% for usability
