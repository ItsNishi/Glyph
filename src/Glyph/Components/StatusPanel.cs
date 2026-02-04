namespace Glyph.Terminal;

/// <summary>
/// A view that displays key-value pairs in a formatted panel.
/// Maintains insertion order and auto-sizes the key column.
/// </summary>
public class StatusPanel : View
{
	private readonly List<string> _KeyOrder = new();
	private readonly Dictionary<string, string> _Values = new();
	private readonly object _Lock = new();

	/// <summary>
	/// Separator string between keys and values.
	/// </summary>
	public string Separator { get; set; } = " : ";

	/// <summary>
	/// Foreground color for keys.
	/// </summary>
	public Color KeyForeground { get; set; } = Color.Default;

	/// <summary>
	/// Foreground color for values.
	/// </summary>
	public Color ValueForeground { get; set; } = Color.Default;

	/// <summary>
	/// Foreground color for the separator.
	/// </summary>
	public Color SeparatorForeground { get; set; } = Color.Default;

	/// <summary>
	/// Background color for the panel.
	/// </summary>
	public Color Background { get; set; } = Color.Default;

	/// <summary>
	/// Number of entries in the panel.
	/// </summary>
	public int Count
	{
		get
		{
			lock (_Lock)
			{
				return _KeyOrder.Count;
			}
		}
	}

	/// <summary>
	/// All keys in insertion order.
	/// </summary>
	public IReadOnlyList<string> Keys
	{
		get
		{
			lock (_Lock)
			{
				return _KeyOrder.ToList().AsReadOnly();
			}
		}
	}

	public StatusPanel()
	{
	}

	public StatusPanel(int X, int Y, int Width, int Height) : base(X, Y, Width, Height)
	{
	}

	/// <summary>
	/// Sets a key-value pair. Adds if key doesn't exist, updates if it does.
	/// </summary>
	/// <param name="Key">The key name.</param>
	/// <param name="Value">The value to display.</param>
	public void Set(string Key, string Value)
	{
		lock (_Lock)
		{
			if (!_Values.ContainsKey(Key))
			{
				_KeyOrder.Add(Key);
			}
			_Values[Key] = Value;
		}

		SetNeedsDraw();
	}

	/// <summary>
	/// Removes a key-value pair.
	/// </summary>
	/// <param name="Key">The key to remove.</param>
	/// <returns>True if the key was found and removed.</returns>
	public bool Remove(string Key)
	{
		bool Removed;
		lock (_Lock)
		{
			Removed = _Values.Remove(Key);
			if (Removed)
			{
				_KeyOrder.Remove(Key);
			}
		}

		if (Removed)
		{
			SetNeedsDraw();
		}

		return Removed;
	}

	/// <summary>
	/// Gets the value for a key.
	/// </summary>
	/// <param name="Key">The key to look up.</param>
	/// <returns>The value, or null if the key doesn't exist.</returns>
	public string? Get(string Key)
	{
		lock (_Lock)
		{
			return _Values.TryGetValue(Key, out var Value) ? Value : null;
		}
	}

	/// <summary>
	/// Checks if a key exists.
	/// </summary>
	/// <param name="Key">The key to check.</param>
	/// <returns>True if the key exists.</returns>
	public bool Contains(string Key)
	{
		lock (_Lock)
		{
			return _Values.ContainsKey(Key);
		}
	}

	/// <summary>
	/// Clears all key-value pairs.
	/// </summary>
	public void ClearAll()
	{
		lock (_Lock)
		{
			_KeyOrder.Clear();
			_Values.Clear();
		}

		SetNeedsDraw();
	}

	public override void Draw(Screen Screen)
	{
		if (!Visible)
		{
			return;
		}

		var (Screen_X, Screen_Y) = LocalToScreen(0, 0);

		// Fill background
		for (int Row = 0; Row < Height; Row++)
		{
			for (int Col = 0; Col < Width; Col++)
			{
				Screen.SetCell(Screen_X + Col, Screen_Y + Row, ' ', Color.Default, Background);
			}
		}

		// Calculate the maximum key width for alignment
		int Max_Key_Width = 0;
		List<(string Key, string Value)> Entries;

		lock (_Lock)
		{
			Entries = new List<(string, string)>();
			foreach (var Key in _KeyOrder)
			{
				if (_Values.TryGetValue(Key, out var Value))
				{
					Entries.Add((Key, Value));
					if (Key.Length > Max_Key_Width)
					{
						Max_Key_Width = Key.Length;
					}
				}
			}
		}

		// Draw each entry
		int Row_Index = 0;
		foreach (var (Key, Value) in Entries)
		{
			if (Row_Index >= Height)
			{
				break;
			}

			int Col = 0;

			// Draw key (right-padded to max width)
			string Padded_Key = Key.PadRight(Max_Key_Width);
			for (int i = 0; i < Padded_Key.Length && Col < Width; i++, Col++)
			{
				Screen.SetCell(Screen_X + Col, Screen_Y + Row_Index, Padded_Key[i], KeyForeground, Background);
			}

			// Draw separator
			for (int i = 0; i < Separator.Length && Col < Width; i++, Col++)
			{
				Screen.SetCell(Screen_X + Col, Screen_Y + Row_Index, Separator[i], SeparatorForeground, Background);
			}

			// Draw value (truncate if necessary)
			int Remaining_Width = Width - Col;
			string Display_Value = Value.Length > Remaining_Width
				? Value.Substring(0, Math.Max(0, Remaining_Width - 1)) + "~"
				: Value;

			for (int i = 0; i < Display_Value.Length && Col < Width; i++, Col++)
			{
				Screen.SetCell(Screen_X + Col, Screen_Y + Row_Index, Display_Value[i], ValueForeground, Background);
			}

			Row_Index++;
		}

		base.Draw(Screen);
	}
}
