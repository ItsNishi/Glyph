using Glyph.Terminal;

namespace Glyph.Presets;

/// <summary>
/// Event args for message received events.
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
	/// <summary>
	/// The message text.
	/// </summary>
	public string Message { get; }

	public MessageReceivedEventArgs(string Message)
	{
		this.Message = Message;
	}
}

/// <summary>
/// A preset chat window layout with output area, status bar, and multi-line input.
/// Designed for chat applications and LLM interfaces.
/// </summary>
public class ChatWindow : View, IDisposable
{
	private readonly OutputView _OutputView;
	private readonly TextInput _Input;
	private readonly View _StatusBar;
	private string _Title = string.Empty;
	private string _StatusText = "Ready";
	private bool _IsStreaming;
	private CancellationTokenSource? _StreamingCts;
	private BoxStyle _BorderStyle = BoxStyle.Rounded;
	private TitleAlignment _TitleAlignment = TitleAlignment.Left;
	private bool _Disposed;

	/// <summary>
	/// Event raised when the user submits a message.
	/// </summary>
	public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

	/// <summary>
	/// Event raised when the user cancels input (Ctrl+C).
	/// </summary>
	public event EventHandler? InputCancelled;

	/// <summary>
	/// The title displayed in the window header.
	/// </summary>
	public string Title
	{
		get => _Title;
		set
		{
			_Title = value ?? string.Empty;
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// The status text displayed in the status bar.
	/// </summary>
	public string StatusText
	{
		get => _StatusText;
		set
		{
			_StatusText = value ?? string.Empty;
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Whether the chat is currently streaming a response.
	/// </summary>
	public bool IsStreaming => _IsStreaming;

	/// <summary>
	/// The output view for displaying messages.
	/// </summary>
	public OutputView OutputView => _OutputView;

	/// <summary>
	/// The input area for user text entry.
	/// </summary>
	public TextInput Input => _Input;

	/// <summary>
	/// Height of the input area (default 3).
	/// </summary>
	public int InputHeight { get; set; } = 3;

	/// <summary>
	/// Border style for the window frame (default: Rounded).
	/// </summary>
	public BoxStyle BorderStyle
	{
		get => _BorderStyle;
		set
		{
			_BorderStyle = value;
			SetNeedsDraw();
		}
	}

	/// <summary>
	/// Title alignment within the top border (default: Left).
	/// </summary>
	public TitleAlignment TitleAlignment
	{
		get => _TitleAlignment;
		set
		{
			_TitleAlignment = value;
			SetNeedsDraw();
		}
	}

	public ChatWindow()
	{
		_OutputView = new OutputView
		{
			CanFocus = false // Output is read-only, no need for focus
		};
		_Input = new TextInput
		{
			CanFocus = true
		};
		_StatusBar = new View
		{
			CanFocus = false
		};

		Add(_OutputView);
		Add(_StatusBar);
		Add(_Input);

		_Input.TextSubmitted += OnTextSubmitted;
		_Input.SubmitCancelled += OnSubmitCancelled;

		Application.OnResize += OnResize;
	}

	/// <summary>
	/// Sets up the layout based on terminal size.
	/// </summary>
	public void SetupLayout(int Width, int Height)
	{
		this.Width = Width;
		this.Height = Height;

		// Layout: Border(1) + Output + StatusSep(1) + InputSep(1) + Input(InputHeight) + Border(1)
		// Total non-output = 2 (borders) + 1 (status sep) + 1 (input sep) + InputHeight
		int Non_Output_Height = 2 + 1 + 1 + InputHeight;
		int Output_Height = Math.Max(1, Height - Non_Output_Height);

		_OutputView.X = 1;
		_OutputView.Y = 1;
		_OutputView.Width = Width - 2;
		_OutputView.Height = Output_Height;

		// Status bar is drawn on the separator line itself (no separate view needed)
		_StatusBar.X = 1;
		_StatusBar.Y = 1 + Output_Height; // On the status separator line
		_StatusBar.Width = Width - 2;
		_StatusBar.Height = 1;

		// Input starts after the input separator line
		_Input.X = 1;
		_Input.Y = 1 + Output_Height + 2; // +2 for status sep and input sep
		_Input.Width = Width - 2;
		_Input.Height = InputHeight;

		SetNeedsDraw();
	}

	private void OnResize(object? Sender, ResizeEventArgs E)
	{
		SetupLayout(E.Width, E.Height);
	}

	private void OnTextSubmitted(object? Sender, TextSubmittedEventArgs E)
	{
		if (string.IsNullOrWhiteSpace(E.Text))
		{
			return;
		}

		// Add user message to output
		AddUserMessage(E.Text);

		// Raise event for handling
		MessageReceived?.Invoke(this, new MessageReceivedEventArgs(E.Text));
	}

	private void OnSubmitCancelled(object? Sender, SubmitCancelledEventArgs E)
	{
		if (_IsStreaming)
		{
			CancelStreaming();
		}
		InputCancelled?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Adds a user message to the output.
	/// </summary>
	public void AddUserMessage(string Message)
	{
		_OutputView.AppendLine($"You: {Message}", "user");
	}

	/// <summary>
	/// Adds an assistant message to the output.
	/// </summary>
	public void AddAssistantMessage(string Message)
	{
		_OutputView.AppendLine($"Assistant: {Message}", "assistant");
	}

	/// <summary>
	/// Adds a system message to the output.
	/// </summary>
	public void AddSystemMessage(string Message)
	{
		_OutputView.AppendLine($"[System] {Message}", "system");
	}

	/// <summary>
	/// Begins streaming an assistant response.
	/// </summary>
	public void BeginStreaming()
	{
		_IsStreaming = true;
		_StreamingCts = new CancellationTokenSource();
		_OutputView.AppendLine("Assistant: ", "assistant");
		StatusText = "Streaming...";
	}

	/// <summary>
	/// Appends text to the current streaming response.
	/// </summary>
	public void AppendStreamingToken(string Token)
	{
		if (!_IsStreaming)
		{
			return;
		}

		_OutputView.AppendToLast(Token);
	}

	/// <summary>
	/// Ends the current streaming response.
	/// </summary>
	public void EndStreaming()
	{
		_IsStreaming = false;
		_StreamingCts?.Dispose();
		_StreamingCts = null;
		StatusText = "Ready";
	}

	/// <summary>
	/// Cancels the current streaming response.
	/// </summary>
	public void CancelStreaming()
	{
		_StreamingCts?.Cancel();
		EndStreaming();
		_OutputView.AppendToLast(" [Cancelled]");
	}

	/// <summary>
	/// Gets the cancellation token for the current streaming operation.
	/// </summary>
	public CancellationToken GetStreamingCancellationToken()
	{
		return _StreamingCts?.Token ?? CancellationToken.None;
	}

	/// <summary>
	/// Clears all messages from the output.
	/// </summary>
	public void ClearMessages()
	{
		_OutputView.Clear();
	}

	public override void Draw(Screen Screen)
	{
		if (!Visible)
		{
			return;
		}

		var (Screen_X, Screen_Y) = LocalToScreen(0, 0);

		// Clear the entire area first
		Screen.FillRect(Screen_X, Screen_Y, Width, Height, ' ', Color.Default, Color.Default);

		// Draw outer border
		if (_BorderStyle != BoxStyle.None)
		{
			Screen.DrawBox(Screen_X, Screen_Y, Width, Height, Color.BrightBlack, Color.Default, _BorderStyle);
		}

		// Draw title in top border
		if (!string.IsNullOrEmpty(_Title) && Width > 4)
		{
			int Max_Title_Length = Math.Max(0, Width - 7);
			string Title_Display = _Title.Length > Width - 4 && Max_Title_Length > 0
				? _Title[..Max_Title_Length] + "..."
				: _Title;
			string Title_Text = $" {Title_Display} ";
			int Title_X = _TitleAlignment switch
			{
				TitleAlignment.Center => Screen_X + (Width - Title_Text.Length) / 2,
				TitleAlignment.Right => Screen_X + Width - Title_Text.Length - 2,
				_ => Screen_X + 2
			};
			Screen.DrawString(Title_X, Screen_Y, Title_Text, Color.BrightWhite, Color.Default, TextAttribute.Bold);
		}

		var (H, _, _, _, _, _, VR, VL, _, _, _) = Terminal.Screen.GetBoxChars(
			_BorderStyle == BoxStyle.None ? BoxStyle.Rounded : _BorderStyle);

		// Draw status bar separator
		int Status_Y = Screen_Y + 1 + _OutputView.Height;
		if (_BorderStyle != BoxStyle.None)
		{
			Screen.SetCell(Screen_X, Status_Y, VR, Color.BrightBlack);
			Screen.SetCell(Screen_X + Width - 1, Status_Y, VL, Color.BrightBlack);
		}
		Screen.DrawHLine(Screen_X + 1, Status_Y, Width - 2, H, Color.BrightBlack);

		// Draw status text
		Color Status_Color = _IsStreaming ? Color.Yellow : Color.Green;
		int Max_Status_Length = Math.Max(0, Width - 7);
		string Status_Display = _StatusText.Length > Width - 4 && Max_Status_Length > 0
			? _StatusText[..Max_Status_Length] + "..."
			: _StatusText;
		Screen.DrawString(Screen_X + 2, Status_Y, $" {Status_Display} ", Status_Color);

		// Draw input separator
		int Input_Y = Status_Y + 1;
		if (_BorderStyle != BoxStyle.None)
		{
			Screen.SetCell(Screen_X, Input_Y, VR, Color.BrightBlack);
			Screen.SetCell(Screen_X + Width - 1, Input_Y, VL, Color.BrightBlack);
		}
		Screen.DrawHLine(Screen_X + 1, Input_Y, Width - 2, H, Color.BrightBlack);

		// Draw input prompt indicator
		Screen.DrawString(Screen_X + 2, Input_Y, " Input ", Color.Cyan);

		// Let children draw themselves
		base.Draw(Screen);
	}

	public override bool HandleKey(KeyInfo Key)
	{
		// Handle Ctrl+Q to quit
		if (Key.IsCtrl && Key.CtrlLetter == 'Q')
		{
			Application.RequestStop();
			return true;
		}

		return base.HandleKey(Key);
	}

	/// <summary>
	/// Releases resources used by the chat window.
	/// </summary>
	public void Dispose()
	{
		if (_Disposed)
		{
			return;
		}

		Application.OnResize -= OnResize;
		_Input.TextSubmitted -= OnTextSubmitted;
		_Input.SubmitCancelled -= OnSubmitCancelled;
		_StreamingCts?.Dispose();
		_StreamingCts = null;
		_Disposed = true;
		GC.SuppressFinalize(this);
	}
}
