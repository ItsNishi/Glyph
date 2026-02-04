# Components Guide

Glyph provides a set of reusable UI components that can be composed to build custom interfaces.

## OutputView

Scrollable view for displaying text output. Supports streaming, categories, and auto-scroll.

```csharp
var output = new OutputView
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

// Append lines
output.AppendLine("Normal message");
output.AppendLine("Error occurred", "error");

// Streaming (append to last line)
output.AppendLine("Loading", "status");
output.AppendToLast("...");
output.AppendToLast(" done!");

// Update last line entirely
output.UpdateLast("Completed successfully");

// Clear all
output.Clear();
```

### Custom Formatting

```csharp
output.Formatter = line => line.Category switch
{
    "error" => $"[ERROR] {line.Text}",
    "warn" => $"[WARN] {line.Text}",
    _ => line.Text
};
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Lines` | `IReadOnlyList<OutputLine>` | All output lines |
| `Formatter` | `Func<OutputLine, string>?` | Custom line formatter |

---

## CommandInput

Single-line command input with history navigation.

```csharp
var input = new CommandInput
{
    X = 0,
    Y = Pos.AnchorEnd() - 1,
    Width = Dim.Fill(),
    Prompt = "> "
};

input.CommandSubmitted += (_, e) =>
{
    Console.WriteLine($"Command: {e.Command}");
};
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Text` | `string` | Current input text |
| `Prompt` | `string` | Prompt prefix |
| `History` | `IReadOnlyList<string>` | Command history |

### Methods

| Method | Description |
|--------|-------------|
| `SetFocus()` | Focus the input |
| `ClearInput()` | Clear current text |
| `AddToHistory(string)` | Add to history manually |

---

## TextInput

Multi-line text input. Enter submits, Shift+Enter adds newline.

```csharp
var input = new TextInput
{
    X = 0,
    Y = Pos.AnchorEnd() - 3,
    Width = Dim.Fill(),
    Height = 3
};

input.TextSubmitted += (_, e) =>
{
    Console.WriteLine($"Submitted: {e.Text}");
};

input.SubmitCancelled += (_, _) =>
{
    Console.WriteLine("Cancelled");
};
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Text` | `string` | Current text |
| `InputEnabled` | `bool` | Enable/disable input |
| `History` | `IReadOnlyList<string>` | Input history |

---

## StatusPanel

Displays key-value pairs in a panel.

```csharp
var status = new StatusPanel
{
    Title = "Status",
    X = 0,
    Y = 0,
    Width = 30,
    Height = Dim.Fill()
};

status.Set("Connection", "Active");
status.Set("Users", "5");
status.Set("Uptime", "2h 30m");

// Update a value
status.Set("Users", "6");

// Remove a key
status.Remove("Uptime");

// Clear all
status.ClearAll();
```

---

## SimpleTableView

Data table with row selection and keyboard navigation.

```csharp
var table = new SimpleTableView
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

table.SetHeaders("Name", "Status", "IP");
table.AddRow("Server 1", "Online", "192.168.1.10");
table.AddRow("Server 2", "Offline", "192.168.1.11");

table.RowSelected += (_, e) =>
{
    Console.WriteLine($"Selected: {e.Row[0]}");
};
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `SelectedRowIndex` | `int` | Currently selected row (-1 if none) |
| `SelectedRow` | `string[]?` | Selected row data |
| `RowCount` | `int` | Total number of rows |

### Methods

| Method | Description |
|--------|-------------|
| `SetHeaders(params string[])` | Set column headers |
| `AddRow(params string[])` | Add a data row |
| `SetRows(IEnumerable<string[]>)` | Set all rows |
| `ClearRows()` | Clear all rows |

---

## ProgressIndicator

Progress bar or indeterminate spinner.

```csharp
var progress = new ProgressIndicator
{
    X = 0,
    Y = 0,
    Width = Dim.Fill()
};

// Progress bar mode
progress.SetProgress(0.5f, "Processing...");
progress.Progress = 0.75f;

// Spinner mode
progress.IsIndeterminate = true;
progress.StatusText = "Loading...";

// Reset
progress.Reset();
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Progress` | `float` | Progress value (0.0 to 1.0) |
| `StatusText` | `string` | Status message |
| `IsIndeterminate` | `bool` | Spinner mode |

---

## Presets

Glyph includes ready-to-use window presets that combine multiple components.

### ConsoleWindow

A complete console application layout with output, status panel, and command input.

```csharp
using Glyph.Presets;

var console = new ConsoleWindow
{
    Title = "My Console App"
};
console.SetupLayout(Console.WindowWidth, Console.WindowHeight);

// Output methods
console.WriteInfo("Information message");
console.WriteSuccess("Success message");
console.WriteWarning("Warning message");
console.WriteError("Error message");
console.WriteLine("Plain text");

// Status panel
console.SetStatus("Key", "Value");
console.RemoveStatus("Key");
console.ClearStatus();

// Handle commands
console.CommandReceived += (s, e) =>
{
    console.WriteLine($"Received: {e.Command}");
};

Application.Run(console);
```

#### Layout Options

The status panel can be positioned in three locations:

```csharp
// Bottom (default) - horizontal row below output
console.StatusLayout = StatusLayout.Bottom;
console.StatusPanelHeight = 4;

// Left - vertical column on left side
console.StatusLayout = StatusLayout.Left;
console.StatusPanelWidth = 25;

// Right - vertical column on right side
console.StatusLayout = StatusLayout.Right;
console.StatusPanelWidth = 25;
```

#### Color Customization

All visual elements can be customized:

```csharp
var console = new ConsoleWindow
{
    Title = "Styled Console",
    StatusLayout = StatusLayout.Right,

    // Colors
    BackgroundColor = Color.FromRgb(15, 20, 45),    // Window background
    BorderColor = Color.FromRgb(100, 140, 200),     // Border and separators
    TitleColor = Color.FromRgb(255, 220, 100),      // Title text
    TextColor = Color.FromRgb(220, 230, 255),       // Default text
    SeparatorLabelColor = Color.FromRgb(255, 150, 80) // "Status"/"Command" labels
};
```

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Title` | `string` | `""` | Window title |
| `StatusLayout` | `StatusLayout` | `Bottom` | Status panel position |
| `StatusPanelHeight` | `int` | `4` | Height when Bottom layout |
| `StatusPanelWidth` | `int` | `25` | Width when Left/Right layout |
| `BackgroundColor` | `Color` | `Default` | Window background |
| `BorderColor` | `Color` | `BrightBlack` | Border color |
| `TitleColor` | `Color` | `BrightWhite` | Title text color |
| `TextColor` | `Color` | `White` | Default text color |
| `SeparatorLabelColor` | `Color` | `Yellow` | Separator label color |

---

### ChatWindow

A chat interface with message display, streaming support, and multi-line input.

```csharp
using Glyph.Presets;

var chat = new ChatWindow
{
    Title = "AI Assistant"
};
chat.SetupLayout(Console.WindowWidth, Console.WindowHeight);

// Add messages
chat.AddSystemMessage("Welcome!");
chat.AddUserMessage("Hello");
chat.AddAssistantMessage("Hi there!");

// Streaming responses
chat.MessageReceived += async (s, e) =>
{
    chat.BeginStreaming();

    foreach (char c in "Response text")
    {
        Application.Invoke(() => chat.AppendStreamingToken(c.ToString()));
        await Task.Delay(30);
    }

    Application.Invoke(() => chat.EndStreaming());
};

Application.Run(chat);
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Title` | `string` | Window title |
| `StatusText` | `string` | Status bar text |
| `IsStreaming` | `bool` | Whether currently streaming |
| `InputHeight` | `int` | Height of input area (default 3) |
