# Architecture Overview

This document describes the core architecture of the Glyph TUI framework.

## Design Principles

1. **Zero Dependencies** - Pure .NET implementation using only ANSI escape sequences
2. **Double Buffering** - Flicker-free rendering by computing diffs between frames
3. **Event-Driven** - All input and updates flow through an event system
4. **Composable** - UI is built from a tree of View components

## Core Components

```
┌─────────────────────────────────────────────────────────────────┐
│                         Application                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │  Event Loop  │  │    Screen    │  │    Input Reader      │   │
│  │              │  │  (Rendering) │  │  (Keyboard/Mouse)    │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                          View Tree                               │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                      Root View                           │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  │    │
│  │  │ OutputView  │  │ StatusPanel │  │  CommandInput   │  │    │
│  │  └─────────────┘  └─────────────┘  └─────────────────┘  │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

## Application Lifecycle

```
Application.Run(rootView)
        │
        ▼
┌───────────────────┐
│   Init Terminal   │  Enter alt screen, hide cursor, enable mouse
└─────────┬─────────┘
          │
          ▼
┌───────────────────┐
│   Main Loop       │◄─────────────────────────────┐
│                   │                              │
│  1. Check Resize  │                              │
│  2. Process Queue │  (Application.Invoke)        │
│  3. Process Timers│  (Application.AddTimeout)    │
│  4. Process Input │                              │
│  5. Redraw        │  (if NeedsDraw)              │
│  6. Sleep(10ms)   │                              │
└─────────┬─────────┘                              │
          │                                        │
          │ (while Running)                        │
          └────────────────────────────────────────┘
          │
          ▼ (RequestStop called)
┌───────────────────┐
│  Shutdown         │  Exit alt screen, show cursor, disable mouse
└───────────────────┘
```

## Screen Rendering

The `Screen` class implements double-buffered rendering:

```
┌─────────────────┐     ┌─────────────────┐
│   Back Buffer   │     │  Front Buffer   │
│                 │     │                 │
│  (Draw here)    │────▶│  (Last render)  │
│                 │     │                 │
└─────────────────┘     └─────────────────┘
         │                      │
         │      Compare         │
         └──────────┬───────────┘
                    │
                    ▼
            ┌───────────────┐
            │ Write only    │
            │ changed cells │
            │ to terminal   │
            └───────────────┘
```

### Rendering Process

1. **Clear** - Set all back buffer cells to empty
2. **Draw** - Views write to back buffer via `Screen.SetCell()` / `Screen.DrawString()`
3. **Render** - Compare buffers, write only changed cells
4. **Sync** - Copy back buffer to front buffer

## View Hierarchy

Views form a tree structure:

```csharp
var root = new View();
var output = new OutputView();
var input = new CommandInput();

root.Add(output);  // output.Parent = root
root.Add(input);   // input.Parent = root
```

### Coordinate Systems

- **Local Coordinates** - Relative to parent view (0,0 is top-left of view)
- **Screen Coordinates** - Absolute position on terminal

```csharp
// Convert local to screen
var (screenX, screenY) = view.LocalToScreen(localX, localY);

// Convert screen to local
var (localX, localY) = view.ScreenToLocal(screenX, screenY);
```

## Input Handling

Input flows from Application to focused View:

```
Terminal Input
      │
      ▼
┌─────────────┐
│ InputReader │  Parse ANSI sequences
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ Application │  Handle Tab (focus cycling)
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ Focused View│  HandleKey() / HandleMouse()
└──────┬──────┘
       │
       ▼ (if not handled)
┌─────────────┐
│  Root View  │  Bubble up to parent
└─────────────┘
```

### Key Events

```csharp
public override bool HandleKey(KeyInfo key)
{
    if (key.Key == ExtendedKey.Enter)
    {
        // Handle Enter
        return true;  // Mark as handled
    }

    return base.HandleKey(key);  // Let parent handle
}
```

### Mouse Events

```csharp
public override bool HandleMouse(MouseInfo mouse)
{
    if (mouse.Action == MouseAction.Press && mouse.Button == MouseButton.Left)
    {
        // Handle click at mouse.X, mouse.Y
        return true;
    }

    return base.HandleMouse(mouse);
}
```

## Focus Management

Only one View can have focus at a time:

```csharp
// Set focus programmatically
myInput.SetFocus();

// Check if view has focus
if (view.HasFocus) { ... }

// Cycle focus with Tab
Application.FocusNext();      // Tab
Application.FocusPrevious();  // Shift+Tab
```

Focus order is determined by:
1. Order views were added to parent
2. `CanFocus` property (must be true)
3. `Enabled` and `Visible` state

## Thread Safety

UI updates from background threads must use `Application.Invoke()`:

```csharp
// Background task
Task.Run(async () =>
{
    var data = await FetchDataAsync();

    // Update UI on main thread
    Application.Invoke(() =>
    {
        outputView.AppendLine(data);
    });
});
```

## Timers

Non-blocking timers for animations and periodic updates:

```csharp
// One-shot timer
Application.AddTimeout(TimeSpan.FromSeconds(5), () =>
{
    DoSomething();
    return false;  // Don't repeat
});

// Repeating timer
var token = Application.AddTimeout(TimeSpan.FromMilliseconds(100), () =>
{
    UpdateAnimation();
    return true;  // Keep repeating
});

// Cancel timer
Application.RemoveTimeout(token);
```

## Memory Model

```
┌─────────────────────────────────────────┐
│              Application                 │
│  _RootView ──────────┐                  │
│  _FocusedView ───────┼──┐               │
│  _Screen ────────────┼──┼───┐           │
│  _InvokeQueue        │  │   │           │
│  _Timers             │  │   │           │
└──────────────────────┼──┼───┼───────────┘
                       │  │   │
                       ▼  │   ▼
              ┌────────────┴─────────────┐
              │        View Tree         │
              │                          │
              │  View ◄── View ◄── View  │
              │   │                      │
              │   └─── Children[]        │
              └──────────────────────────┘
```

## Performance Considerations

1. **Diff-based rendering** - Only changed cells are written
2. **Batched output** - All changes written in single Console.Write()
3. **Lazy layout** - Recalculate only when dimensions change
4. **Event coalescing** - Multiple SetNeedsDraw() calls result in single redraw
