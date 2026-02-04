namespace Glyph.Terminal;

/// <summary>
/// Base class for all UI elements in the Glyph TUI framework.
/// </summary>
public class View
{
	private int _X;
	private int _Y;
	private int _Width;
	private int _Height;
	private bool _Visible = true;
	private bool _Enabled = true;
	private bool _CanFocus;
	private bool _HasFocus;
	private bool _NeedsDraw = true;
	private View? _Parent;
	private readonly List<View> _Children = new();

	/// <summary>
	/// X position relative to parent (or screen if no parent).
	/// </summary>
	public int X
	{
		get => _X;
		set
		{
			if (_X != value)
			{
				_X = value;
				SetNeedsDraw();
			}
		}
	}

	/// <summary>
	/// Y position relative to parent (or screen if no parent).
	/// </summary>
	public int Y
	{
		get => _Y;
		set
		{
			if (_Y != value)
			{
				_Y = value;
				SetNeedsDraw();
			}
		}
	}

	/// <summary>
	/// Width of the view.
	/// </summary>
	public int Width
	{
		get => _Width;
		set
		{
			if (_Width != value)
			{
				_Width = Math.Max(0, value);
				SetNeedsDraw();
			}
		}
	}

	/// <summary>
	/// Height of the view.
	/// </summary>
	public int Height
	{
		get => _Height;
		set
		{
			if (_Height != value)
			{
				_Height = Math.Max(0, value);
				SetNeedsDraw();
			}
		}
	}

	/// <summary>
	/// Whether the view is visible.
	/// </summary>
	public bool Visible
	{
		get => _Visible;
		set
		{
			if (_Visible != value)
			{
				_Visible = value;
				SetNeedsDraw();
			}
		}
	}

	/// <summary>
	/// Whether the view can receive input.
	/// </summary>
	public bool Enabled
	{
		get => _Enabled;
		set
		{
			if (_Enabled != value)
			{
				_Enabled = value;
				SetNeedsDraw();
			}
		}
	}

	/// <summary>
	/// Whether the view can receive focus.
	/// </summary>
	public bool CanFocus
	{
		get => _CanFocus;
		set => _CanFocus = value;
	}

	/// <summary>
	/// Whether this view currently has focus.
	/// </summary>
	public bool HasFocus => _HasFocus;

	/// <summary>
	/// Whether the view needs to be redrawn.
	/// </summary>
	public bool NeedsDraw => _NeedsDraw;

	/// <summary>
	/// Parent view, or null if this is a root view.
	/// </summary>
	public View? Parent => _Parent;

	/// <summary>
	/// Read-only list of child views.
	/// </summary>
	public IReadOnlyList<View> Children => _Children;

	/// <summary>
	/// Event raised when the view is drawn.
	/// </summary>
	public event EventHandler<DrawEventArgs>? OnDraw;

	/// <summary>
	/// Event raised when a key is pressed.
	/// </summary>
	public event EventHandler<KeyEventArgs>? OnKeyPress;

	/// <summary>
	/// Event raised when a mouse event occurs.
	/// </summary>
	public event EventHandler<MouseEventArgs>? OnMouseEvent;

	/// <summary>
	/// Event raised when focus changes.
	/// </summary>
	public event EventHandler<FocusChangedEventArgs>? OnFocusChanged;

	public View()
	{
	}

	public View(int X, int Y, int Width, int Height)
	{
		_X = X;
		_Y = Y;
		_Width = Math.Max(0, Width);
		_Height = Math.Max(0, Height);
	}

	/// <summary>
	/// Adds a child view.
	/// </summary>
	public void Add(View Child)
	{
		if (Child._Parent != null)
		{
			Child._Parent.Remove(Child);
		}

		Child._Parent = this;
		_Children.Add(Child);
		SetNeedsDraw();
	}

	/// <summary>
	/// Removes a child view.
	/// </summary>
	public bool Remove(View Child)
	{
		if (_Children.Remove(Child))
		{
			Child._Parent = null;
			SetNeedsDraw();
			return true;
		}
		return false;
	}

	/// <summary>
	/// Removes all child views.
	/// </summary>
	public void Clear()
	{
		foreach (var Child in _Children)
		{
			Child._Parent = null;
		}
		_Children.Clear();
		SetNeedsDraw();
	}

	/// <summary>
	/// Draws the view and its children to the screen.
	/// </summary>
	public virtual void Draw(Screen Screen)
	{
		if (!Visible)
		{
			return;
		}

		var Args = new DrawEventArgs(Screen);
		OnDraw?.Invoke(this, Args);

		foreach (var Child in _Children)
		{
			if (Child.Visible)
			{
				Child.Draw(Screen);
			}
		}

		_NeedsDraw = false;
	}

	/// <summary>
	/// Handles a key press event.
	/// </summary>
	/// <returns>True if the key was handled, false otherwise.</returns>
	public virtual bool HandleKey(KeyInfo Key)
	{
		if (!Enabled)
		{
			return false;
		}

		var Args = new KeyEventArgs(Key);
		OnKeyPress?.Invoke(this, Args);

		if (Args.Handled)
		{
			return true;
		}

		// Give children a chance to handle the key (focused child first)
		foreach (var Child in _Children)
		{
			if (Child.HasFocus && Child.HandleKey(Key))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Handles a mouse event.
	/// </summary>
	/// <returns>True if the event was handled, false otherwise.</returns>
	public virtual bool HandleMouse(MouseInfo Mouse)
	{
		if (!Enabled || !Visible)
		{
			return false;
		}

		var Args = new MouseEventArgs(Mouse);
		OnMouseEvent?.Invoke(this, Args);

		if (Args.Handled)
		{
			return true;
		}

		// Check children in reverse order (topmost first)
		for (int i = _Children.Count - 1; i >= 0; i--)
		{
			var Child = _Children[i];
			if (Child.Visible && Child.ContainsScreen(Mouse.X, Mouse.Y))
			{
				if (Child.HandleMouse(Mouse))
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Sets focus to this view.
	/// </summary>
	/// <returns>True if focus was set, false if the view cannot receive focus.</returns>
	public bool SetFocus()
	{
		if (!CanFocus || !Enabled || !Visible)
		{
			return false;
		}

		Application.SetFocused(this);
		return true;
	}

	/// <summary>
	/// Called by Application when focus state changes.
	/// </summary>
	internal void SetFocusState(bool Focused)
	{
		if (_HasFocus != Focused)
		{
			_HasFocus = Focused;
			SetNeedsDraw();
			OnFocusChanged?.Invoke(this, new FocusChangedEventArgs(Focused));
		}
	}

	/// <summary>
	/// Marks the view as needing to be redrawn.
	/// </summary>
	public void SetNeedsDraw()
	{
		_NeedsDraw = true;
		_Parent?.SetNeedsDraw();
	}

	/// <summary>
	/// Clears the needs draw flag without drawing.
	/// </summary>
	internal void ClearNeedsDraw()
	{
		_NeedsDraw = false;
	}

	/// <summary>
	/// Checks if the given local coordinates are within this view's bounds.
	/// </summary>
	public bool Contains(int Local_X, int Local_Y)
	{
		return Local_X >= 0 && Local_X < Width && Local_Y >= 0 && Local_Y < Height;
	}

	/// <summary>
	/// Checks if the given screen coordinates are within this view's bounds.
	/// </summary>
	public bool ContainsScreen(int Screen_X, int Screen_Y)
	{
		var (View_Screen_X, View_Screen_Y) = LocalToScreen(0, 0);
		return Screen_X >= View_Screen_X && Screen_X < View_Screen_X + Width &&
		       Screen_Y >= View_Screen_Y && Screen_Y < View_Screen_Y + Height;
	}

	/// <summary>
	/// Converts local coordinates to screen coordinates.
	/// </summary>
	public (int Screen_X, int Screen_Y) LocalToScreen(int Local_X, int Local_Y)
	{
		int Screen_X = Local_X + X;
		int Screen_Y = Local_Y + Y;

		var Current = Parent;
		while (Current != null)
		{
			Screen_X += Current.X;
			Screen_Y += Current.Y;
			Current = Current.Parent;
		}

		return (Screen_X, Screen_Y);
	}

	/// <summary>
	/// Converts screen coordinates to local coordinates.
	/// </summary>
	public (int Local_X, int Local_Y) ScreenToLocal(int Screen_X, int Screen_Y)
	{
		var (Origin_X, Origin_Y) = LocalToScreen(0, 0);
		return (Screen_X - Origin_X, Screen_Y - Origin_Y);
	}

	/// <summary>
	/// Gets the screen bounds of this view.
	/// </summary>
	public (int X, int Y, int Width, int Height) GetScreenBounds()
	{
		var (Screen_X, Screen_Y) = LocalToScreen(0, 0);
		return (Screen_X, Screen_Y, Width, Height);
	}

	/// <summary>
	/// Finds the next focusable view in tab order.
	/// </summary>
	internal View? FindNextFocusable(View? After = null)
	{
		bool Found_After = After == null;

		foreach (var Child in _Children)
		{
			if (!Child.Visible || !Child.Enabled)
			{
				continue;
			}

			if (Found_After && Child.CanFocus)
			{
				return Child;
			}

			if (Child == After)
			{
				Found_After = true;
			}

			var In_Children = Child.FindNextFocusable(Found_After ? null : After);
			if (In_Children != null)
			{
				return In_Children;
			}

			if (Found_After && Child.CanFocus)
			{
				return Child;
			}
		}

		return null;
	}

	/// <summary>
	/// Gets all focusable views in this view hierarchy.
	/// </summary>
	internal List<View> GetFocusableViews()
	{
		var Result = new List<View>();
		CollectFocusableViews(Result);
		return Result;
	}

	private void CollectFocusableViews(List<View> Result)
	{
		if (CanFocus && Enabled && Visible)
		{
			Result.Add(this);
		}

		foreach (var Child in _Children)
		{
			Child.CollectFocusableViews(Result);
		}
	}
}

/// <summary>
/// Event args for draw events.
/// </summary>
public class DrawEventArgs : EventArgs
{
	public Screen Screen { get; }

	public DrawEventArgs(Screen Screen)
	{
		this.Screen = Screen;
	}
}

/// <summary>
/// Event args for key press events.
/// </summary>
public class KeyEventArgs : EventArgs
{
	public KeyInfo Key { get; }
	public bool Handled { get; set; }

	public KeyEventArgs(KeyInfo Key)
	{
		this.Key = Key;
	}
}

/// <summary>
/// Event args for mouse events.
/// </summary>
public class MouseEventArgs : EventArgs
{
	public MouseInfo Mouse { get; }
	public bool Handled { get; set; }

	public MouseEventArgs(MouseInfo Mouse)
	{
		this.Mouse = Mouse;
	}
}

/// <summary>
/// Event args for focus change events.
/// </summary>
public class FocusChangedEventArgs : EventArgs
{
	public bool HasFocus { get; }

	public FocusChangedEventArgs(bool HasFocus)
	{
		this.HasFocus = HasFocus;
	}
}
