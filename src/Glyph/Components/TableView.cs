namespace Glyph.Terminal;

/// <summary>
/// Event args for row selection events in TableView.
/// </summary>
public class RowSelectedEventArgs : EventArgs
{
	/// <summary>
	/// Index of the selected row (-1 if none).
	/// </summary>
	public int RowIndex { get; }

	/// <summary>
	/// Data of the selected row (null if none).
	/// </summary>
	public string[]? Row { get; }

	public RowSelectedEventArgs(int Row_Index, string[]? Row)
	{
		this.RowIndex = Row_Index;
		this.Row = Row;
	}
}

/// <summary>
/// A data table view with row selection support.
/// Displays headers, a separator line, and scrollable rows.
/// </summary>
public class TableView : View
{
	private string[] _Headers = Array.Empty<string>();
	private readonly List<string[]> _Rows = new();
	private int _SelectedRowIndex = -1;
	private int _ScrollOffset = 0;
	private int[] _ColumnWidths = Array.Empty<int>();

	/// <summary>
	/// Event raised when a row is selected (Enter pressed or mouse click).
	/// </summary>
	public event EventHandler<RowSelectedEventArgs>? RowSelected;

	/// <summary>
	/// The currently selected row index (-1 if no selection).
	/// </summary>
	public int SelectedRowIndex
	{
		get => _SelectedRowIndex;
		set
		{
			int New_Value = Math.Clamp(value, -1, _Rows.Count - 1);
			if (_SelectedRowIndex != New_Value)
			{
				_SelectedRowIndex = New_Value;
				EnsureSelectedVisible();
				SetNeedsDraw();
			}
		}
	}

	/// <summary>
	/// The currently selected row data, or null if no selection.
	/// </summary>
	public string[]? SelectedRow
	{
		get
		{
			if (_SelectedRowIndex >= 0 && _SelectedRowIndex < _Rows.Count)
			{
				return _Rows[_SelectedRowIndex];
			}
			return null;
		}
	}

	/// <summary>
	/// The column headers.
	/// </summary>
	public IReadOnlyList<string> Headers => _Headers;

	/// <summary>
	/// The data rows.
	/// </summary>
	public IReadOnlyList<string[]> Rows => _Rows;

	/// <summary>
	/// Number of rows that can be displayed (excluding header and separator).
	/// </summary>
	private int VisibleRowCount => Math.Max(0, Height - 2);

	public TableView() : base()
	{
		CanFocus = true;
	}

	public TableView(int X, int Y, int Width, int Height) : base(X, Y, Width, Height)
	{
		CanFocus = true;
	}

	/// <summary>
	/// Sets the column headers.
	/// </summary>
	public void SetHeaders(params string[] Headers)
	{
		_Headers = Headers ?? Array.Empty<string>();
		RecalculateColumnWidths();
		SetNeedsDraw();
	}

	/// <summary>
	/// Adds a row of data.
	/// </summary>
	public void AddRow(params string[] Row)
	{
		_Rows.Add(Row ?? Array.Empty<string>());
		RecalculateColumnWidths();

		// Auto-select first row if none selected
		if (_SelectedRowIndex == -1 && _Rows.Count > 0)
		{
			_SelectedRowIndex = 0;
		}

		SetNeedsDraw();
	}

	/// <summary>
	/// Sets all rows at once, replacing any existing rows.
	/// </summary>
	public void SetRows(IEnumerable<string[]> Rows)
	{
		_Rows.Clear();
		if (Rows != null)
		{
			_Rows.AddRange(Rows);
		}

		RecalculateColumnWidths();

		// Reset selection
		if (_Rows.Count > 0)
		{
			_SelectedRowIndex = Math.Clamp(_SelectedRowIndex, 0, _Rows.Count - 1);
		}
		else
		{
			_SelectedRowIndex = -1;
		}

		_ScrollOffset = 0;
		EnsureSelectedVisible();
		SetNeedsDraw();
	}

	/// <summary>
	/// Clears all rows.
	/// </summary>
	public void ClearRows()
	{
		_Rows.Clear();
		_SelectedRowIndex = -1;
		_ScrollOffset = 0;
		RecalculateColumnWidths();
		SetNeedsDraw();
	}

	/// <summary>
	/// Recalculates column widths based on header and row content.
	/// </summary>
	private void RecalculateColumnWidths()
	{
		int Column_Count = _Headers.Length;
		foreach (var Row in _Rows)
		{
			Column_Count = Math.Max(Column_Count, Row.Length);
		}

		if (Column_Count == 0)
		{
			_ColumnWidths = Array.Empty<int>();
			return;
		}

		_ColumnWidths = new int[Column_Count];

		// Include header widths
		for (int i = 0; i < _Headers.Length; i++)
		{
			_ColumnWidths[i] = _Headers[i]?.Length ?? 0;
		}

		// Include row content widths
		foreach (var Row in _Rows)
		{
			for (int i = 0; i < Row.Length; i++)
			{
				int Cell_Width = Row[i]?.Length ?? 0;
				if (Cell_Width > _ColumnWidths[i])
				{
					_ColumnWidths[i] = Cell_Width;
				}
			}
		}

		// Add padding (1 space on each side)
		for (int i = 0; i < _ColumnWidths.Length; i++)
		{
			_ColumnWidths[i] += 2;
		}
	}

	/// <summary>
	/// Ensures the selected row is visible by adjusting scroll offset.
	/// </summary>
	private void EnsureSelectedVisible()
	{
		if (_SelectedRowIndex < 0 || VisibleRowCount <= 0)
		{
			return;
		}

		if (_SelectedRowIndex < _ScrollOffset)
		{
			_ScrollOffset = _SelectedRowIndex;
		}
		else if (_SelectedRowIndex >= _ScrollOffset + VisibleRowCount)
		{
			_ScrollOffset = _SelectedRowIndex - VisibleRowCount + 1;
		}
	}

	/// <summary>
	/// Draws the table to the screen.
	/// </summary>
	public override void Draw(Screen Screen)
	{
		if (!Visible)
		{
			return;
		}

		var (Screen_X, Screen_Y) = LocalToScreen(0, 0);

		Color Header_Fg = Color.BrightWhite;
		Color Header_Bg = Color.Default;
		Color Normal_Fg = Color.Default;
		Color Normal_Bg = Color.Default;
		Color Selected_Fg = Color.Black;
		Color Selected_Bg = HasFocus ? Color.Cyan : Color.BrightBlack;
		Color Separator_Fg = Color.BrightBlack;

		// Draw header row
		if (_Headers.Length > 0)
		{
			int Col_X = Screen_X;
			for (int i = 0; i < _ColumnWidths.Length && Col_X < Screen_X + Width; i++)
			{
				string Header_Text = i < _Headers.Length ? _Headers[i] ?? string.Empty : string.Empty;
				string Padded = $" {Header_Text} ".PadRight(_ColumnWidths[i]);

				// Truncate if exceeds available width
				int Available = Screen_X + Width - Col_X;
				if (Padded.Length > Available)
				{
					Padded = Padded[..Available];
				}

				Screen.DrawString(Col_X, Screen_Y, Padded, Header_Fg, Header_Bg, TextAttribute.Bold);
				Col_X += _ColumnWidths[i];
			}

			// Clear rest of header line
			for (int x = Col_X; x < Screen_X + Width; x++)
			{
				Screen.SetCell(x, Screen_Y, ' ', Header_Fg, Header_Bg);
			}
		}

		// Draw separator line
		int Separator_Y = Screen_Y + 1;
		if (Separator_Y < Screen_Y + Height)
		{
			int Col_X = Screen_X;
			for (int i = 0; i < _ColumnWidths.Length && Col_X < Screen_X + Width; i++)
			{
				for (int j = 0; j < _ColumnWidths[i] && Col_X + j < Screen_X + Width; j++)
				{
					Screen.SetCell(Col_X + j, Separator_Y, BoxChars.Horizontal, Separator_Fg, Normal_Bg);
				}
				Col_X += _ColumnWidths[i];
			}

			// Clear rest of separator line
			for (int x = Col_X; x < Screen_X + Width; x++)
			{
				Screen.SetCell(x, Separator_Y, BoxChars.Horizontal, Separator_Fg, Normal_Bg);
			}
		}

		// Draw data rows
		int Row_Start_Y = Screen_Y + 2;
		for (int Row_Idx = 0; Row_Idx < VisibleRowCount; Row_Idx++)
		{
			int Data_Row_Idx = _ScrollOffset + Row_Idx;
			int Row_Y = Row_Start_Y + Row_Idx;

			if (Row_Y >= Screen_Y + Height)
			{
				break;
			}

			bool Is_Selected = Data_Row_Idx == _SelectedRowIndex;
			Color Row_Fg = Is_Selected ? Selected_Fg : Normal_Fg;
			Color Row_Bg = Is_Selected ? Selected_Bg : Normal_Bg;

			if (Data_Row_Idx < _Rows.Count)
			{
				var Row = _Rows[Data_Row_Idx];
				int Col_X = Screen_X;

				for (int Col_Idx = 0; Col_Idx < _ColumnWidths.Length && Col_X < Screen_X + Width; Col_Idx++)
				{
					string Cell_Text = Col_Idx < Row.Length ? Row[Col_Idx] ?? string.Empty : string.Empty;
					string Padded = $" {Cell_Text} ".PadRight(_ColumnWidths[Col_Idx]);

					// Truncate if exceeds available width
					int Available = Screen_X + Width - Col_X;
					if (Padded.Length > Available)
					{
						Padded = Padded[..Available];
					}

					Screen.DrawString(Col_X, Row_Y, Padded, Row_Fg, Row_Bg);
					Col_X += _ColumnWidths[Col_Idx];
				}

				// Fill rest of row with selection background
				for (int x = Col_X; x < Screen_X + Width; x++)
				{
					Screen.SetCell(x, Row_Y, ' ', Row_Fg, Row_Bg);
				}
			}
			else
			{
				// Empty row
				for (int x = Screen_X; x < Screen_X + Width; x++)
				{
					Screen.SetCell(x, Row_Y, ' ', Normal_Fg, Normal_Bg);
				}
			}
		}

		base.Draw(Screen);
	}

	/// <summary>
	/// Handles keyboard input for navigation and selection.
	/// </summary>
	public override bool HandleKey(KeyInfo Key)
	{
		if (!Enabled)
		{
			return false;
		}

		// Let base class handle events first
		if (base.HandleKey(Key))
		{
			return true;
		}

		if (_Rows.Count == 0)
		{
			return false;
		}

		// j / Down - move selection down
		if (Key.Key == ExtendedKey.DownArrow || Key.Char == 'j')
		{
			if (_SelectedRowIndex < _Rows.Count - 1)
			{
				SelectedRowIndex++;
			}
			return true;
		}

		// k / Up - move selection up
		if (Key.Key == ExtendedKey.UpArrow || Key.Char == 'k')
		{
			if (_SelectedRowIndex > 0)
			{
				SelectedRowIndex--;
			}
			return true;
		}

		// Home - go to first row
		if (Key.Key == ExtendedKey.Home)
		{
			SelectedRowIndex = 0;
			return true;
		}

		// End - go to last row
		if (Key.Key == ExtendedKey.End)
		{
			SelectedRowIndex = _Rows.Count - 1;
			return true;
		}

		// PageUp - page up
		if (Key.Key == ExtendedKey.PageUp)
		{
			int Page_Size = Math.Max(1, VisibleRowCount);
			SelectedRowIndex = Math.Max(0, _SelectedRowIndex - Page_Size);
			return true;
		}

		// PageDown - page down
		if (Key.Key == ExtendedKey.PageDown)
		{
			int Page_Size = Math.Max(1, VisibleRowCount);
			SelectedRowIndex = Math.Min(_Rows.Count - 1, _SelectedRowIndex + Page_Size);
			return true;
		}

		// Enter - activate selected row
		if (Key.Key == ExtendedKey.Enter)
		{
			if (_SelectedRowIndex >= 0)
			{
				RowSelected?.Invoke(this, new RowSelectedEventArgs(_SelectedRowIndex, SelectedRow));
			}
			return true;
		}

		return false;
	}

	/// <summary>
	/// Handles mouse input for row selection.
	/// </summary>
	public override bool HandleMouse(MouseInfo Mouse)
	{
		if (!Enabled || !Visible)
		{
			return false;
		}

		// Let base class handle events first
		if (base.HandleMouse(Mouse))
		{
			return true;
		}

		var (Local_X, Local_Y) = ScreenToLocal(Mouse.X, Mouse.Y);

		// Check if click is within bounds
		if (!Contains(Local_X, Local_Y))
		{
			return false;
		}

		// Handle mouse click
		if (Mouse.Action == MouseAction.Press && Mouse.Button == MouseButton.Left)
		{
			// Calculate which row was clicked (accounting for header and separator)
			int Row_Y = Local_Y - 2;  // Subtract header and separator

			if (Row_Y >= 0)
			{
				int Clicked_Row = _ScrollOffset + Row_Y;
				if (Clicked_Row >= 0 && Clicked_Row < _Rows.Count)
				{
					SelectedRowIndex = Clicked_Row;
					SetFocus();
					return true;
				}
			}
		}

		// Handle scroll wheel
		if (Mouse.Action == MouseAction.Scroll)
		{
			if (Mouse.ScrollDirection == ScrollDirection.Up)
			{
				if (_SelectedRowIndex > 0)
				{
					SelectedRowIndex--;
				}
				return true;
			}
			else if (Mouse.ScrollDirection == ScrollDirection.Down)
			{
				if (_SelectedRowIndex < _Rows.Count - 1)
				{
					SelectedRowIndex++;
				}
				return true;
			}
		}

		return false;
	}
}
