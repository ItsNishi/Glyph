# Input Handling

This document covers keyboard and mouse input handling in Glyph.

## Overview

Glyph provides a unified input system that:

- Parses ANSI escape sequences for special keys
- Supports modifier keys (Ctrl, Alt, Shift)
- Handles SGR extended mouse mode
- Routes input through the focus system

## Keyboard Input

### KeyInfo Structure

```csharp
public struct KeyInfo
{
    public ConsoleKey ConsoleKey { get; }    // .NET ConsoleKey enum
    public ExtendedKey Key { get; }          // Extended key enumeration
    public char Character { get; }            // Character if printable
    public bool IsCtrl { get; }              // Ctrl modifier pressed
    public bool IsAlt { get; }               // Alt modifier pressed
    public bool IsShift { get; }             // Shift modifier pressed
    public char CtrlLetter { get; }          // Letter for Ctrl+Letter combos
}
```

### Handling Key Events

```csharp
public override bool HandleKey(KeyInfo key)
{
    // Check for Ctrl+Q to quit
    if (key.IsCtrl && key.CtrlLetter == 'Q')
    {
        Application.RequestStop();
        return true;  // Mark as handled
    }

    // Check for specific keys
    if (key.Key == ExtendedKey.Enter)
    {
        SubmitInput();
        return true;
    }

    // Check for Shift+Enter (newline in input)
    if (key.Key == ExtendedKey.Enter && key.IsShift)
    {
        InsertNewline();
        return true;
    }

    // Let parent handle unprocessed keys
    return base.HandleKey(key);
}
```

### Extended Keys

```csharp
public enum ExtendedKey
{
    None,

    // Navigation
    Up, Down, Left, Right,
    Home, End,
    PageUp, PageDown,

    // Editing
    Enter, Tab, ShiftTab,
    Backspace, Delete,
    Insert,

    // Function keys
    F1, F2, F3, F4, F5, F6,
    F7, F8, F9, F10, F11, F12,

    // Special
    Escape,

    // Character input
    Character
}
```

### Modifier Key Detection

```csharp
// Ctrl+Letter combinations
if (key.IsCtrl && key.CtrlLetter == 'C')
{
    // Handle Ctrl+C
}

// Alt combinations
if (key.IsAlt && key.Character == 'x')
{
    // Handle Alt+X
}

// Shift combinations
if (key.IsShift && key.Key == ExtendedKey.Tab)
{
    // Handle Shift+Tab
}
```

### Common Key Patterns

```csharp
public override bool HandleKey(KeyInfo key)
{
    switch (key.Key)
    {
        case ExtendedKey.Up:
            ScrollUp();
            return true;

        case ExtendedKey.Down:
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
            return true;
    }

    // Handle printable characters
    if (key.Key == ExtendedKey.Character && !key.IsCtrl && !key.IsAlt)
    {
        InsertCharacter(key.Character);
        return true;
    }

    return base.HandleKey(key);
}
```

## Mouse Input

### MouseInfo Structure

```csharp
public struct MouseInfo
{
    public int X { get; }                    // Screen X coordinate
    public int Y { get; }                    // Screen Y coordinate
    public MouseButton Button { get; }       // Which button
    public MouseAction Action { get; }       // Press, Release, Move, Scroll
    public bool IsCtrl { get; }             // Ctrl held
    public bool IsAlt { get; }              // Alt held
    public bool IsShift { get; }            // Shift held
}

public enum MouseButton
{
    None,
    Left,
    Middle,
    Right,
    ScrollUp,
    ScrollDown
}

public enum MouseAction
{
    Press,
    Release,
    Move,
    Drag
}
```

### Handling Mouse Events

```csharp
public override bool HandleMouse(MouseInfo mouse)
{
    // Convert screen coordinates to local
    var (localX, localY) = ScreenToLocal(mouse.X, mouse.Y);

    // Check if click is within bounds
    if (!ContainsPoint(localX, localY))
    {
        return false;
    }

    switch (mouse.Action)
    {
        case MouseAction.Press when mouse.Button == MouseButton.Left:
            OnLeftClick(localX, localY);
            return true;

        case MouseAction.Press when mouse.Button == MouseButton.Right:
            OnRightClick(localX, localY);
            return true;

        case MouseAction.Drag:
            OnDrag(localX, localY);
            return true;
    }

    return base.HandleMouse(mouse);
}
```

### Scroll Wheel

```csharp
public override bool HandleMouse(MouseInfo mouse)
{
    if (mouse.Button == MouseButton.ScrollUp)
    {
        ScrollUp(3);  // Scroll 3 lines
        return true;
    }

    if (mouse.Button == MouseButton.ScrollDown)
    {
        ScrollDown(3);
        return true;
    }

    return base.HandleMouse(mouse);
}
```

### Click to Focus

```csharp
public override bool HandleMouse(MouseInfo mouse)
{
    if (mouse.Action == MouseAction.Press && mouse.Button == MouseButton.Left)
    {
        if (CanFocus && !HasFocus)
        {
            SetFocus();
        }
    }

    return base.HandleMouse(mouse);
}
```

## Focus Management

### Focus Flow

```
Input Event
    │
    ▼
Application receives input
    │
    ▼
Tab/Shift+Tab? ──Yes──> Cycle focus
    │
    No
    │
    ▼
Dispatch to FocusedView.HandleKey()
    │
    ▼
Handled? ──No──> Dispatch to RootView.HandleKey()
    │
    Yes
    │
    ▼
Done
```

### Focus Properties

```csharp
public class View
{
    // Whether this view can receive focus
    public bool CanFocus { get; set; } = false;

    // Whether this view currently has focus
    public bool HasFocus { get; }

    // Request focus for this view
    public void SetFocus();
}
```

### Focus Cycling

```csharp
// Application handles Tab automatically
// Tab -> FocusNext()
// Shift+Tab -> FocusPrevious()

// Programmatic focus control
Application.FocusNext();
Application.FocusPrevious();

// Direct focus
myInput.SetFocus();
```

### Focus Order

Focus order is determined by:

1. Order views were added to parent (`Add()` call order)
2. `CanFocus` property must be `true`
3. `Visible` property must be `true`
4. `Enabled` property must be `true`

## Input Reader

The `InputReader` class handles raw terminal input:

```csharp
// Typically called by Application, not directly
var input = InputReader.Read();

if (input is KeyInfo key)
{
    // Handle keyboard input
}
else if (input is MouseInfo mouse)
{
    // Handle mouse input
}
```

### ANSI Escape Sequence Parsing

The input reader handles:

- CSI sequences (`\x1b[...`)
- SS3 sequences (`\x1bO...`)
- Mouse SGR sequences (`\x1b[<...M` and `\x1b[<...m`)
- Alt key sequences (`\x1b` + character)

## Common Patterns

### Text Input Field

```csharp
public class TextField : View
{
    private string _text = "";
    private int _cursorPos = 0;

    public override bool HandleKey(KeyInfo key)
    {
        switch (key.Key)
        {
            case ExtendedKey.Character when !key.IsCtrl:
                _text = _text.Insert(_cursorPos, key.Character.ToString());
                _cursorPos++;
                SetNeedsDraw();
                return true;

            case ExtendedKey.Backspace:
                if (_cursorPos > 0)
                {
                    _text = _text.Remove(_cursorPos - 1, 1);
                    _cursorPos--;
                    SetNeedsDraw();
                }
                return true;

            case ExtendedKey.Left:
                if (_cursorPos > 0) _cursorPos--;
                SetNeedsDraw();
                return true;

            case ExtendedKey.Right:
                if (_cursorPos < _text.Length) _cursorPos++;
                SetNeedsDraw();
                return true;
        }

        return base.HandleKey(key);
    }
}
```

### Scrollable List

```csharp
public class ListView : View
{
    private List<string> _items = new();
    private int _selectedIndex = 0;
    private int _scrollOffset = 0;

    public override bool HandleKey(KeyInfo key)
    {
        switch (key.Key)
        {
            case ExtendedKey.Up:
                if (_selectedIndex > 0)
                {
                    _selectedIndex--;
                    EnsureVisible(_selectedIndex);
                    SetNeedsDraw();
                }
                return true;

            case ExtendedKey.Down:
                if (_selectedIndex < _items.Count - 1)
                {
                    _selectedIndex++;
                    EnsureVisible(_selectedIndex);
                    SetNeedsDraw();
                }
                return true;

            case ExtendedKey.Enter:
                OnItemSelected(_selectedIndex);
                return true;
        }

        // Vim-style navigation
        if (key.Character == 'j') return HandleKey(new KeyInfo(ExtendedKey.Down));
        if (key.Character == 'k') return HandleKey(new KeyInfo(ExtendedKey.Up));

        return base.HandleKey(key);
    }

    public override bool HandleMouse(MouseInfo mouse)
    {
        if (mouse.Action == MouseAction.Press && mouse.Button == MouseButton.Left)
        {
            var (_, localY) = ScreenToLocal(mouse.X, mouse.Y);
            var clickedIndex = _scrollOffset + localY;
            if (clickedIndex >= 0 && clickedIndex < _items.Count)
            {
                _selectedIndex = clickedIndex;
                SetNeedsDraw();
                return true;
            }
        }

        return base.HandleMouse(mouse);
    }
}
```

## See Also

- [Components Guide](components.md) - Built-in input components
- [Custom Components](custom-components.md) - Building interactive components
- [Architecture](architecture.md) - Event loop and input flow
