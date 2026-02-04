namespace Glyph.Terminal;

/// <summary>
/// A progress indicator that can display either a determinate progress bar
/// or an indeterminate animated spinner.
/// </summary>
public class ProgressIndicator : View, IDisposable
{
	private float _Progress = 0.0f;
	private string _StatusText = string.Empty;
	private bool _IsIndeterminate = false;
	private int _SpinnerIndex = 0;
	private object? _TimerToken = null;

	private static readonly char[] SpinnerFrames = { '|', '/', '-', '\\' };

	/// <summary>
	/// Current progress value (0.0 to 1.0).
	/// Only used when IsIndeterminate is false.
	/// </summary>
	public float Progress
	{
		get => _Progress;
		set
		{
			float New_Value = Math.Clamp(value, 0.0f, 1.0f);
			if (Math.Abs(_Progress - New_Value) > 0.001f)
			{
				_Progress = New_Value;
				SetNeedsDraw();
			}
		}
	}

	/// <summary>
	/// Status text displayed next to the progress indicator.
	/// </summary>
	public string StatusText
	{
		get => _StatusText;
		set
		{
			string New_Value = value ?? string.Empty;
			if (_StatusText != New_Value)
			{
				_StatusText = New_Value;
				SetNeedsDraw();
			}
		}
	}

	/// <summary>
	/// When true, shows an animated spinner instead of a progress bar.
	/// </summary>
	public bool IsIndeterminate
	{
		get => _IsIndeterminate;
		set
		{
			if (_IsIndeterminate != value)
			{
				_IsIndeterminate = value;

				if (_IsIndeterminate)
				{
					StartSpinnerAnimation();
				}
				else
				{
					StopSpinnerAnimation();
				}

				SetNeedsDraw();
			}
		}
	}

	public ProgressIndicator() : base()
	{
		Height = 1;
	}

	public ProgressIndicator(int X, int Y, int Width) : base(X, Y, Width, 1)
	{
	}

	/// <summary>
	/// Sets both progress and status text at once.
	/// </summary>
	/// <param name="Progress">Progress value (0.0 to 1.0).</param>
	/// <param name="Status_Text">Optional status text.</param>
	public void SetProgress(float Progress, string? Status_Text = null)
	{
		float New_Progress = Math.Clamp(Progress, 0.0f, 1.0f);
		string New_Status = Status_Text ?? _StatusText;

		bool Changed = Math.Abs(_Progress - New_Progress) > 0.001f || _StatusText != New_Status;

		_Progress = New_Progress;
		_StatusText = New_Status;

		if (Changed)
		{
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Resets progress to 0 and clears status text.
	/// </summary>
	public void Reset()
	{
		_Progress = 0.0f;
		_StatusText = string.Empty;
		_SpinnerIndex = 0;
		SetNeedsDraw();
	}

	/// <summary>
	/// Starts the spinner animation timer.
	/// </summary>
	private void StartSpinnerAnimation()
	{
		if (_TimerToken != null)
		{
			return;
		}

		_TimerToken = Application.AddTimeout(
			TimeSpan.FromMilliseconds(100),
			() =>
			{
				if (!_IsIndeterminate)
				{
					return false;
				}

				_SpinnerIndex = (_SpinnerIndex + 1) % SpinnerFrames.Length;
				SetNeedsDraw();
				return true;
			}
		);
	}

	/// <summary>
	/// Stops the spinner animation timer.
	/// </summary>
	private void StopSpinnerAnimation()
	{
		if (_TimerToken != null)
		{
			Application.RemoveTimeout(_TimerToken);
			_TimerToken = null;
		}
	}

	/// <summary>
	/// Draws the progress indicator to the screen.
	/// </summary>
	public override void Draw(Screen Screen)
	{
		if (!Visible)
		{
			return;
		}

		var (Screen_X, Screen_Y) = LocalToScreen(0, 0);

		Color Bracket_Fg = Color.BrightBlack;
		Color Fill_Fg = Color.Green;
		Color Empty_Fg = Color.BrightBlack;
		Color Text_Fg = Color.Default;
		Color Bg = Color.Default;

		if (_IsIndeterminate)
		{
			DrawSpinner(Screen, Screen_X, Screen_Y, Bracket_Fg, Fill_Fg, Text_Fg, Bg);
		}
		else
		{
			DrawProgressBar(Screen, Screen_X, Screen_Y, Bracket_Fg, Fill_Fg, Empty_Fg, Text_Fg, Bg);
		}

		base.Draw(Screen);
	}

	/// <summary>
	/// Draws the spinner mode: [|] Status text
	/// </summary>
	private void DrawSpinner(Screen Screen, int X, int Y, Color Bracket_Fg, Color Spinner_Fg, Color Text_Fg, Color Bg)
	{
		// Draw [
		Screen.SetCell(X, Y, '[', Bracket_Fg, Bg);

		// Draw spinner character
		char Spinner_Char = SpinnerFrames[_SpinnerIndex];
		Screen.SetCell(X + 1, Y, Spinner_Char, Spinner_Fg, Bg);

		// Draw ]
		Screen.SetCell(X + 2, Y, ']', Bracket_Fg, Bg);

		// Draw status text
		if (!string.IsNullOrEmpty(_StatusText))
		{
			int Text_X = X + 4;  // Space after ]
			int Available_Width = Width - 4;

			string Display_Text = _StatusText;
			if (Display_Text.Length > Available_Width)
			{
				Display_Text = Display_Text[..Available_Width];
			}

			Screen.DrawString(Text_X, Y, Display_Text, Text_Fg, Bg);

			// Clear rest of line
			for (int i = Text_X + Display_Text.Length; i < X + Width; i++)
			{
				Screen.SetCell(i, Y, ' ', Text_Fg, Bg);
			}
		}
		else
		{
			// Clear rest of line
			for (int i = X + 3; i < X + Width; i++)
			{
				Screen.SetCell(i, Y, ' ', Text_Fg, Bg);
			}
		}
	}

	/// <summary>
	/// Draws the progress bar mode: [####----] 50% Status text
	/// </summary>
	private void DrawProgressBar(Screen Screen, int X, int Y, Color Bracket_Fg, Color Fill_Fg, Color Empty_Fg, Color Text_Fg, Color Bg)
	{
		// Calculate available space for the bar
		string Percent_Text = $" {(int)(_Progress * 100)}%";
		int Status_Width = string.IsNullOrEmpty(_StatusText) ? 0 : _StatusText.Length + 1;
		int Percent_Width = Percent_Text.Length;

		// Bar width: Total width - brackets (2) - percent - status
		int Bar_Width = Width - 2 - Percent_Width - Status_Width;
		Bar_Width = Math.Max(1, Bar_Width);

		int Filled_Count = (int)(Bar_Width * _Progress);
		Filled_Count = Math.Clamp(Filled_Count, 0, Bar_Width);
		int Empty_Count = Bar_Width - Filled_Count;

		int Current_X = X;

		// Draw [
		Screen.SetCell(Current_X++, Y, '[', Bracket_Fg, Bg);

		// Draw filled portion
		for (int i = 0; i < Filled_Count && Current_X < X + Width; i++)
		{
			Screen.SetCell(Current_X++, Y, '#', Fill_Fg, Bg);
		}

		// Draw empty portion
		for (int i = 0; i < Empty_Count && Current_X < X + Width; i++)
		{
			Screen.SetCell(Current_X++, Y, '-', Empty_Fg, Bg);
		}

		// Draw ]
		if (Current_X < X + Width)
		{
			Screen.SetCell(Current_X++, Y, ']', Bracket_Fg, Bg);
		}

		// Draw percentage
		if (Current_X < X + Width)
		{
			string Display_Percent = Percent_Text;
			int Available = X + Width - Current_X;
			if (Display_Percent.Length > Available)
			{
				Display_Percent = Display_Percent[..Available];
			}
			Screen.DrawString(Current_X, Y, Display_Percent, Text_Fg, Bg);
			Current_X += Display_Percent.Length;
		}

		// Draw status text
		if (!string.IsNullOrEmpty(_StatusText) && Current_X < X + Width)
		{
			Screen.SetCell(Current_X++, Y, ' ', Text_Fg, Bg);

			if (Current_X < X + Width)
			{
				int Available = X + Width - Current_X;
				string Display_Status = _StatusText;
				if (Display_Status.Length > Available)
				{
					Display_Status = Display_Status[..Available];
				}
				Screen.DrawString(Current_X, Y, Display_Status, Text_Fg, Bg);
				Current_X += Display_Status.Length;
			}
		}

		// Clear rest of line
		for (int i = Current_X; i < X + Width; i++)
		{
			Screen.SetCell(i, Y, ' ', Text_Fg, Bg);
		}
	}

	/// <summary>
	/// Releases resources used by the progress indicator.
	/// </summary>
	public void Dispose()
	{
		StopSpinnerAnimation();
		GC.SuppressFinalize(this);
	}
}
