# Custom Components

This guide covers building your own UI components in Glyph.

## Overview

All Glyph components inherit from `View`. To create a custom component:

1. Inherit from `View`
2. Override `Draw()` for rendering
3. Override `HandleKey()` for keyboard input
4. Override `HandleMouse()` for mouse input
5. Call `SetNeedsDraw()` when state changes

## Basic Component

```csharp
public class MyButton : View
{
    private string _label = "Button";
    private bool _pressed = false;

    public string Label
    {
        get => _label;
        set
        {
            _label = value;
            SetNeedsDraw();
        }
    }

    public event EventHandler? Clicked;

    public MyButton()
    {
        CanFocus = true;
        Width = 10;
        Height = 1;
    }

    public override void Draw(Screen screen)
    {
        if (!Visible) return;

        var (sx, sy) = LocalToScreen(0, 0);

        // Choose colors based on state
        var fg = HasFocus ? Color.Black : Color.White;
        var bg = HasFocus ? Color.Cyan : (_pressed ? Color.Blue : Color.BrightBlack);

        // Draw button background
        screen.FillRect(sx, sy, Width, Height, ' ', fg, bg);

        // Center the label
        int labelX = sx + (Width - _label.Length) / 2;
        screen.DrawString(labelX, sy, _label, fg, bg);
    }

    public override bool HandleKey(KeyInfo key)
    {
        if (key.Key == ExtendedKey.Enter || key.Character == ' ')
        {
            _pressed = true;
            SetNeedsDraw();
            Clicked?.Invoke(this, EventArgs.Empty);

            // Visual feedback - reset after short delay
            Application.AddTimeout(TimeSpan.FromMilliseconds(100), () =>
            {
                _pressed = false;
                SetNeedsDraw();
                return false;
            });

            return true;
        }

        return base.HandleKey(key);
    }

    public override bool HandleMouse(MouseInfo mouse)
    {
        if (mouse.Action == MouseAction.Press && mouse.Button == MouseButton.Left)
        {
            if (!HasFocus) SetFocus();

            _pressed = true;
            SetNeedsDraw();
            Clicked?.Invoke(this, EventArgs.Empty);
            return true;
        }

        if (mouse.Action == MouseAction.Release)
        {
            _pressed = false;
            SetNeedsDraw();
            return true;
        }

        return base.HandleMouse(mouse);
    }
}
```

## Component Lifecycle

```
Constructor
    │
    ▼
Add() to parent ──> Parent property set
    │
    ▼
Application.Run()
    │
    ▼
┌─────────────────────────────────┐
│  Main Loop                      │
│                                 │
│  SetNeedsDraw() ──> NeedsDraw   │
│       │                         │
│       ▼                         │
│  Draw() called                  │
│                                 │
│  Input event ──> HandleKey()    │
│              ──> HandleMouse()  │
│                                 │
└─────────────────────────────────┘
    │
    ▼
Application.RequestStop()
    │
    ▼
Shutdown
```

## Drawing

### Screen Coordinates

```csharp
public override void Draw(Screen screen)
{
    // Convert local (0,0) to screen coordinates
    var (screenX, screenY) = LocalToScreen(0, 0);

    // Draw at screen position
    screen.DrawString(screenX, screenY, "Hello");

    // Draw a box filling the entire view
    screen.DrawBox(screenX, screenY, Width, Height, Color.White);
}
```

### Clipping

Drawing is not automatically clipped to view bounds. Ensure you stay within:

```csharp
public override void Draw(Screen screen)
{
    var (sx, sy) = LocalToScreen(0, 0);

    for (int y = 0; y < Height; y++)
    {
        for (int x = 0; x < Width; x++)
        {
            screen.SetCell(sx + x, sy + y, '.', Color.Gray);
        }
    }
}
```

### Drawing Children

If your component has children, call `base.Draw()`:

```csharp
public override void Draw(Screen screen)
{
    // Draw this component's content first
    var (sx, sy) = LocalToScreen(0, 0);
    screen.DrawBox(sx, sy, Width, Height, Color.White);

    // Then draw children
    base.Draw(screen);
}
```

## State Management

### Triggering Redraws

```csharp
private int _value;

public int Value
{
    get => _value;
    set
    {
        if (_value != value)
        {
            _value = value;
            SetNeedsDraw();  // Request redraw
        }
    }
}
```

### Dirty Tracking

For complex components, track what changed:

```csharp
private bool _layoutDirty = true;
private bool _contentDirty = true;

public override void Draw(Screen screen)
{
    if (_layoutDirty)
    {
        RecalculateLayout();
        _layoutDirty = false;
    }

    if (_contentDirty)
    {
        RenderContent(screen);
        _contentDirty = false;
    }
}

private void OnResize()
{
    _layoutDirty = true;
    SetNeedsDraw();
}

private void OnContentChanged()
{
    _contentDirty = true;
    SetNeedsDraw();
}
```

## Container Components

### Managing Children

```csharp
public class Panel : View
{
    private string _title = "";

    public string Title
    {
        get => _title;
        set { _title = value; SetNeedsDraw(); }
    }

    public override void Draw(Screen screen)
    {
        var (sx, sy) = LocalToScreen(0, 0);

        // Draw border
        screen.DrawBox(sx, sy, Width, Height, Color.White, Color.Default, BoxStyle.Rounded);

        // Draw title
        if (!string.IsNullOrEmpty(_title))
        {
            screen.DrawString(sx + 2, sy, $" {_title} ", Color.BrightWhite, Color.Default, TextAttribute.Bold);
        }

        // Draw children (they should be positioned inside the border)
        base.Draw(screen);
    }

    public void AddChild(View child)
    {
        // Offset child position to account for border
        child.X += 1;
        child.Y += 1;
        Add(child);
    }
}
```

### Layout Management

```csharp
public class VerticalStack : View
{
    private int _spacing = 1;

    public int Spacing
    {
        get => _spacing;
        set { _spacing = value; LayoutChildren(); }
    }

    public override void Add(View child)
    {
        base.Add(child);
        LayoutChildren();
    }

    private void LayoutChildren()
    {
        int y = 0;
        foreach (var child in Children)
        {
            child.X = 0;
            child.Y = y;
            child.Width = Width;
            y += child.Height + _spacing;
        }
        SetNeedsDraw();
    }
}
```

## Event Handling

### Custom Events

```csharp
public class Slider : View
{
    public event EventHandler<ValueChangedEventArgs>? ValueChanged;

    private double _value;

    public double Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, 0, 1);
            if (Math.Abs(_value - clamped) > 0.001)
            {
                var old = _value;
                _value = clamped;
                SetNeedsDraw();
                ValueChanged?.Invoke(this, new ValueChangedEventArgs(old, _value));
            }
        }
    }
}

public class ValueChangedEventArgs : EventArgs
{
    public double OldValue { get; }
    public double NewValue { get; }

    public ValueChangedEventArgs(double oldValue, double newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
```

### Bubbling Events

```csharp
public override bool HandleKey(KeyInfo key)
{
    // Try to handle locally first
    if (key.Key == ExtendedKey.Enter)
    {
        DoSomething();
        return true;  // Handled, stop bubbling
    }

    // Not handled, let parent try
    return base.HandleKey(key);  // Returns false by default
}
```

## Animation

### Using Timers

```csharp
public class Spinner : View
{
    private static readonly char[] _frames = { '|', '/', '-', '\\' };
    private int _frame = 0;
    private object? _timerToken;

    public void Start()
    {
        _timerToken = Application.AddTimeout(TimeSpan.FromMilliseconds(100), () =>
        {
            _frame = (_frame + 1) % _frames.Length;
            SetNeedsDraw();
            return true;  // Keep running
        });
    }

    public void Stop()
    {
        if (_timerToken != null)
        {
            Application.RemoveTimeout(_timerToken);
            _timerToken = null;
        }
    }

    public override void Draw(Screen screen)
    {
        var (sx, sy) = LocalToScreen(0, 0);
        screen.SetCell(sx, sy, _frames[_frame], Color.Cyan);
    }
}
```

### Smooth Transitions

```csharp
public class FadeLabel : View
{
    private string _text = "";
    private int _brightness = 255;
    private object? _fadeTimer;

    public void FadeIn(string text)
    {
        _text = text;
        _brightness = 0;

        _fadeTimer = Application.AddTimeout(TimeSpan.FromMilliseconds(16), () =>
        {
            _brightness = Math.Min(255, _brightness + 15);
            SetNeedsDraw();
            return _brightness < 255;
        });
    }

    public override void Draw(Screen screen)
    {
        var (sx, sy) = LocalToScreen(0, 0);
        var color = Color.FromRgb(_brightness, _brightness, _brightness);
        screen.DrawString(sx, sy, _text, color);
    }
}
```

## Best Practices

1. **Call SetNeedsDraw()** whenever visual state changes
2. **Use LocalToScreen()** for all drawing coordinates
3. **Return true from input handlers** only when the event was actually handled
4. **Call base.Draw()** if you have children that need drawing
5. **Dispose timers** when the component is no longer needed
6. **Keep Draw() fast** - avoid allocations and complex calculations
7. **Cache expensive calculations** using dirty flags

## Example: Progress Bar

```csharp
public class ProgressBar : View
{
    private double _value;
    private string _label = "";

    public double Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0, 1);
            SetNeedsDraw();
        }
    }

    public string Label
    {
        get => _label;
        set { _label = value ?? ""; SetNeedsDraw(); }
    }

    public Color FillColor { get; set; } = Color.Green;
    public Color EmptyColor { get; set; } = Color.BrightBlack;

    public ProgressBar()
    {
        Height = 1;
        Width = 20;
    }

    public override void Draw(Screen screen)
    {
        var (sx, sy) = LocalToScreen(0, 0);

        int fillWidth = (int)(Width * _value);

        // Draw filled portion
        for (int x = 0; x < fillWidth; x++)
        {
            screen.SetCell(sx + x, sy, '\u2588', FillColor);  // Full block
        }

        // Draw empty portion
        for (int x = fillWidth; x < Width; x++)
        {
            screen.SetCell(sx + x, sy, '\u2591', EmptyColor);  // Light shade
        }

        // Draw percentage label centered
        string pct = $"{_value * 100:F0}%";
        if (!string.IsNullOrEmpty(_label))
        {
            pct = $"{_label} {pct}";
        }

        int labelX = sx + (Width - pct.Length) / 2;
        if (labelX >= sx && labelX + pct.Length <= sx + Width)
        {
            screen.DrawString(labelX, sy, pct, Color.White);
        }
    }
}
```

## See Also

- [Components Guide](components.md) - Built-in components reference
- [Styling & Colors](styling.md) - Colors and visual styling
- [Input Handling](input.md) - Keyboard and mouse events
- [Architecture](architecture.md) - Framework internals
