# Glyph

```
 ██████╗ ██╗  ██╗   ██╗██████╗ ██╗  ██╗
██╔════╝ ██║  ╚██╗ ██╔╝██╔══██╗██║  ██║
██║  ███╗██║   ╚████╔╝ ██████╔╝███████║
██║   ██║██║    ╚██╔╝  ██╔═══╝ ██╔══██║
╚██████╔╝███████╗██║   ██║     ██║  ██║
 ╚═════╝ ╚══════╝╚═╝   ╚═╝     ╚═╝  ╚═╝
```

**A zero-dependency, high-performance TUI framework for .NET**

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey?style=flat-square)]()

---

> 🎯 Build beautiful terminal interfaces without any external dependencies

![Glyph Styled Console](assets/styled.png)

## 🚀 Features

| Feature | Description |
|---------|-------------|
| 🎨 **Rich Colors** | 16, 256, and 24-bit true color support |
| 🖱️ **Mouse Support** | Click, drag, and scroll wheel via SGR extended mode |
| ⌨️ **Advanced Input** | Shift+Enter, modifier keys, full keyboard handling |
| 📐 **Auto Resize** | Automatic terminal resize detection and re-layout |
| 🔄 **Double Buffered** | Flicker-free rendering with diff-based updates |
| 🧩 **Component Library** | Ready-to-use UI components and presets |
| 📦 **Zero Dependencies** | Pure .NET - no native libraries required |

## 📦 Installation

```bash
git clone https://github.com/ItsNishi/Glyph.git
cd Glyph
dotnet build
```

**Add to your project:**
```xml
<ProjectReference Include="path/to/Glyph/src/Glyph/Glyph.csproj" />
```

**Run the demos:**
```bash
dotnet run --project src/Glyph.Demo                # Console (status bottom)
dotnet run --project src/Glyph.Demo console-left   # Status panel on left
dotnet run --project src/Glyph.Demo console-right  # Status panel on right
dotnet run --project src/Glyph.Demo console-styled # Custom colors
dotnet run --project src/Glyph.Demo chat           # Chat interface
```

## 🏃 Quick Start

```csharp
using Glyph.Terminal;
using Glyph.Presets;

var console = new ConsoleWindow { Title = "My App" };
console.SetupLayout(Console.WindowWidth, Console.WindowHeight);

console.SetStatus("Status", "Running");
console.WriteInfo("Application started");
console.WriteSuccess("Ready");

console.CommandReceived += (s, e) =>
{
    console.WriteLine($"Command: {e.Command}");
};

Application.Run(console);
```

## 🧩 Components

| Component | Description |
|-----------|-------------|
| `OutputView` | Scrollable text output with word wrap and auto-scroll |
| `TextInput` | Multi-line input (Enter=submit, Shift+Enter=newline) |
| `CommandInput` | Single-line command input with history |
| `StatusPanel` | Key-value display with auto-sizing columns |

## 🎨 Presets

### ConsoleWindow

Complete console layout with output, status panel, and command input.

![Console Window](assets/console.png)

**Layout Options:**
```csharp
console.StatusLayout = StatusLayout.Bottom;  // Horizontal row (default)
console.StatusLayout = StatusLayout.Left;    // Vertical column left
console.StatusLayout = StatusLayout.Right;   // Vertical column right

console.StatusPanelHeight = 4;   // For Bottom layout
console.StatusPanelWidth = 25;   // For Left/Right layout
```

**Color Theming:**
```csharp
var console = new ConsoleWindow
{
    Title = "Themed App",
    StatusLayout = StatusLayout.Right,
    BackgroundColor = Color.FromRgb(15, 20, 45),
    BorderColor = Color.FromRgb(100, 140, 200),
    TitleColor = Color.FromRgb(255, 220, 100),
    TextColor = Color.FromRgb(220, 230, 255),
    SeparatorLabelColor = Color.FromRgb(255, 150, 80)
};
```

### ChatWindow

Chat interface with streaming support for LLM/AI applications.

![Chat Window](assets/chat.png)

```csharp
var chat = new ChatWindow { Title = "AI Assistant" };
chat.SetupLayout(Console.WindowWidth, Console.WindowHeight);

chat.MessageReceived += async (s, e) =>
{
    chat.BeginStreaming();
    foreach (char c in response)
    {
        Application.Invoke(() => chat.AppendStreamingToken(c.ToString()));
        await Task.Delay(30);
    }
    Application.Invoke(() => chat.EndStreaming());
};

Application.Run(chat);
```

## 🎨 Colors

```csharp
// Standard 16 colors
Color.Red, Color.BrightGreen, Color.Cyan

// 256-color palette
Color.From256(208)  // Orange

// True color (24-bit RGB)
Color.FromRgb(255, 128, 0)
Color.FromHex("#FF8000")

// Text attributes
Screen.DrawString(x, y, "Bold!", Color.White, Color.Default, TextAttribute.Bold);
```

## ⌨️ Keybindings

| Key | Action |
|-----|--------|
| `Tab` / `Shift+Tab` | Cycle focus |
| `Enter` | Submit input |
| `Shift+Enter` | Insert newline (TextInput) |
| `Ctrl+C` | Cancel/Clear |
| `Ctrl+Q` | Quit application |
| `Ctrl+L` | Clear output |
| `j` / `k` | Vim-style scroll |
| `g` / `G` | Jump to top/bottom |

## 📁 Project Structure

```
Glyph/
├── src/
│   ├── Glyph/                    # Main library
│   │   ├── Terminal/             # Core TUI engine
│   │   ├── Components/           # UI components
│   │   ├── Presets/              # Ready-to-use layouts
│   │   └── Backends/             # Streaming backends
│   └── Glyph.Demo/               # Demo application
└── docs/                         # Documentation
```

## 📚 Documentation

- [Architecture Overview](docs/architecture.md) - Core concepts and design
- [Components Guide](docs/components.md) - Component reference
- [Layouts](docs/layouts.md) - Layout system and positioning
- [Styling & Colors](docs/styling.md) - Colors, attributes, and theming
- [Input Handling](docs/input.md) - Keyboard and mouse events
- [Custom Components](docs/custom-components.md) - Building your own
- [Backends](docs/backends.md) - Streaming data sources

## 🔧 Requirements

- .NET 10 SDK
- Terminal with ANSI support (most modern terminals)
- No native dependencies - works out of the box on Windows, Linux, and macOS

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

<p align="center">
  <strong>Built with ❤️ for the terminal</strong>
</p>
