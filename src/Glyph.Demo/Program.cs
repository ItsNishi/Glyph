using Glyph.Terminal;
using Glyph.Presets;

// Parse command line for demo mode
var mode = args.Length > 0 ? args[0].ToLowerInvariant() : "console";

try
{
	switch (mode)
	{
		case "chat":
			RunChatDemo();
			break;
		case "console-left":
			RunConsoleDemo(StatusLayout.Left);
			break;
		case "console-right":
			RunConsoleDemo(StatusLayout.Right);
			break;
		case "console-styled":
			RunStyledConsoleDemo();
			break;
		case "console":
		default:
			RunConsoleDemo(StatusLayout.Bottom);
			break;
	}
}
catch (Exception ex)
{
	// Ensure terminal is restored on error
	Application.Shutdown();
	Console.WriteLine($"Error: {ex.Message}");
	Environment.Exit(1);
}

void RunChatDemo()
{
	var chatWindow = new ChatWindow
	{
		Title = "Glyph Chat Demo"
	};

	// Get terminal size and setup layout
	int width = Console.WindowWidth;
	int height = Console.WindowHeight;
	chatWindow.SetupLayout(width, height);

	chatWindow.AddSystemMessage("Welcome to Glyph Chat Demo!");
	chatWindow.AddSystemMessage("Type a message and press Enter to send. Shift+Enter for newline.");
	chatWindow.AddSystemMessage("Press Ctrl+C to cancel streaming, Ctrl+Q to quit.");

	// Handle messages
	chatWindow.MessageReceived += async (sender, e) =>
	{
		try
		{
			// Simulate streaming response
			chatWindow.BeginStreaming();

			var response = $"You said: \"{e.Message}\". This is a simulated streaming response that demonstrates the chat interface.";
			var token = chatWindow.GetStreamingCancellationToken();

			try
			{
				foreach (char c in response)
				{
					if (token.IsCancellationRequested)
					{
						break;
					}

					Application.Invoke(() =>
					{
						chatWindow.AppendStreamingToken(c.ToString());
					});

					await Task.Delay(30, token);
				}
			}
			catch (OperationCanceledException)
			{
				// Cancelled by user
			}
			finally
			{
				Application.Invoke(() =>
				{
					chatWindow.EndStreaming();
				});
			}
		}
		catch (Exception ex)
		{
			// Prevent async void from crashing the application
			Application.Invoke(() =>
			{
				chatWindow.AddSystemMessage($"Error: {ex.Message}");
				chatWindow.EndStreaming();
			});
		}
	};

	Application.Run(chatWindow);
}

void RunConsoleDemo(StatusLayout layout = StatusLayout.Bottom)
{
	var consoleWindow = new ConsoleWindow
	{
		Title = "Glyph Console Demo",
		StatusLayout = layout,
		StatusPanelWidth = 22
	};

	// Get terminal size and setup layout
	int width = Console.WindowWidth;
	int height = Console.WindowHeight;
	consoleWindow.SetupLayout(width, height);

	// Set up initial status
	consoleWindow.SetStatus("Mode", "Demo");
	consoleWindow.SetStatus("Connection", "Local");
	consoleWindow.SetStatus("Uptime", "0s");

	// Welcome message
	consoleWindow.WriteInfo("Welcome to Glyph Console Demo");
	consoleWindow.WriteInfo("Type 'help' for available commands");
	consoleWindow.WriteLine(string.Empty);

	// Simple uptime counter
	var startTime = DateTime.Now;
	Application.AddTimeout(TimeSpan.FromSeconds(1), () =>
	{
		var elapsed = DateTime.Now - startTime;
		consoleWindow.SetStatus("Uptime", $"{(int)elapsed.TotalSeconds}s");
		return true;
	});

	// Handle commands
	consoleWindow.CommandReceived += (sender, e) =>
	{
		var parts = e.Command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		var cmd = parts.Length > 0 ? parts[0].ToLowerInvariant() : string.Empty;

		switch (cmd)
		{
			case "help":
				consoleWindow.WriteLine("Available commands:");
				consoleWindow.WriteLine("  help     - Show this help");
				consoleWindow.WriteLine("  status   - Show status");
				consoleWindow.WriteLine("  scan     - Simulate a scan");
				consoleWindow.WriteLine("  progress - Show progress demo");
				consoleWindow.WriteLine("  clear    - Clear output");
				consoleWindow.WriteLine("  exit     - Exit application");
				consoleWindow.WriteLine(string.Empty);
				consoleWindow.WriteInfo("Demo modes: console, console-left, console-right, console-styled, chat");
				break;

			case "status":
				consoleWindow.WriteInfo("System Status:");
				consoleWindow.WriteSuccess("All systems operational");
				break;

			case "scan":
				SimulateScan(consoleWindow);
				break;

			case "progress":
				SimulateProgress(consoleWindow);
				break;

			default:
				consoleWindow.WriteError($"Unknown command: {cmd}");
				consoleWindow.WriteInfo("Type 'help' for available commands");
				break;
		}
	};

	Application.Run(consoleWindow);
}

void SimulateScan(ConsoleWindow console)
{
	console.WriteInfo("Starting scan...");
	console.SetStatus("Status", "Scanning");

	var targets = new[] { "192.168.1.1", "192.168.1.10", "192.168.1.50", "192.168.1.100" };
	var index = 0;

	Application.AddTimeout(TimeSpan.FromMilliseconds(500), () =>
	{
		if (index >= targets.Length)
		{
			console.WriteSuccess("Scan complete!");
			console.SetStatus("Status", "Idle");
			console.SetStatus("Targets", targets.Length.ToString());
			return false;
		}

		console.WriteLine($"  Found: {targets[index]}");
		index++;
		return true;
	});
}

void SimulateProgress(ConsoleWindow console)
{
	console.WriteInfo("Starting progress simulation...");
	console.SetStatus("Status", "Processing");

	var progress = 0;

	Application.AddTimeout(TimeSpan.FromMilliseconds(100), () =>
	{
		progress += 2;

		if (progress > 100)
		{
			console.WriteSuccess("Progress complete!");
			console.SetStatus("Status", "Idle");
			console.SetStatus("Progress", "100%");
			return false;
		}

		console.SetStatus("Progress", $"{progress}%");

		// Update output every 10%
		if (progress % 10 == 0)
		{
			console.WriteLine($"  Progress: {progress}%");
		}

		return true;
	});
}

void RunStyledConsoleDemo()
{
	var consoleWindow = new ConsoleWindow
	{
		Title = "Styled Console Demo",
		StatusLayout = StatusLayout.Right,
		StatusPanelWidth = 20,

		// Custom colors - dark blue/purple theme
		BackgroundColor = Color.FromRgb(15, 20, 45),
		BorderColor = Color.FromRgb(100, 140, 200),
		TitleColor = Color.FromRgb(255, 220, 100),
		TextColor = Color.FromRgb(220, 230, 255),
		SeparatorLabelColor = Color.FromRgb(255, 150, 80)
	};

	// Get terminal size and setup layout
	int width = Console.WindowWidth;
	int height = Console.WindowHeight;
	consoleWindow.SetupLayout(width, height);

	// Set up initial status
	consoleWindow.SetStatus("Theme", "Custom");
	consoleWindow.SetStatus("Layout", "Right");
	consoleWindow.SetStatus("Uptime", "0s");

	// Welcome message
	consoleWindow.WriteInfo("Welcome to Styled Console Demo");
	consoleWindow.WriteInfo("Custom colors and right-side status panel");
	consoleWindow.WriteLine(string.Empty);
	consoleWindow.WriteSuccess("Background: RGB(15, 20, 45) - Dark blue");
	consoleWindow.WriteSuccess("Border: RGB(100, 140, 200) - Light blue");
	consoleWindow.WriteSuccess("Title: RGB(255, 220, 100) - Gold");
	consoleWindow.WriteLine(string.Empty);
	consoleWindow.WriteInfo("Type 'help' for commands");

	// Simple uptime counter
	var startTime = DateTime.Now;
	Application.AddTimeout(TimeSpan.FromSeconds(1), () =>
	{
		var elapsed = DateTime.Now - startTime;
		consoleWindow.SetStatus("Uptime", $"{(int)elapsed.TotalSeconds}s");
		return true;
	});

	// Handle commands
	consoleWindow.CommandReceived += (sender, e) =>
	{
		var parts = e.Command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		var cmd = parts.Length > 0 ? parts[0].ToLowerInvariant() : string.Empty;

		switch (cmd)
		{
			case "help":
				consoleWindow.WriteLine("Available commands:");
				consoleWindow.WriteLine("  help   - Show this help");
				consoleWindow.WriteLine("  colors - Show color info");
				consoleWindow.WriteLine("  clear  - Clear output");
				consoleWindow.WriteLine("  exit   - Exit application");
				break;

			case "colors":
				consoleWindow.WriteInfo("Current color scheme:");
				consoleWindow.WriteLine("  Background: RGB(15, 20, 45) - Dark blue");
				consoleWindow.WriteLine("  Border: RGB(100, 140, 200) - Light blue");
				consoleWindow.WriteLine("  Title: RGB(255, 220, 100) - Gold");
				consoleWindow.WriteLine("  Text: RGB(220, 230, 255) - Pale blue");
				consoleWindow.WriteLine("  Labels: RGB(255, 150, 80) - Orange");
				break;

			default:
				consoleWindow.WriteError($"Unknown command: {cmd}");
				break;
		}
	};

	Application.Run(consoleWindow);
}
