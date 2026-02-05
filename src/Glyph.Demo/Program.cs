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
		case "showcase":
			RunShowcaseDemo(args);
			break;
		case "image":
			RunImageDemo(args);
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
				consoleWindow.WriteInfo("Demo modes: console, console-left, console-right, console-styled, chat, showcase, image");
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
		BorderStyle = BoxStyle.Double,
		TitleAlignment = TitleAlignment.Center,

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
	consoleWindow.SetStatus("Border", "Double");
	consoleWindow.SetStatus("Title", "Center");
	consoleWindow.SetStatus("Uptime", "0s");

	// Welcome message
	consoleWindow.WriteInfo("Welcome to Styled Console Demo");
	consoleWindow.WriteInfo("Double border, centered title, right-side status panel");
	consoleWindow.WriteLine(string.Empty);
	consoleWindow.WriteSuccess("Border Style: Double");
	consoleWindow.WriteSuccess("Title Alignment: Center");
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

void RunShowcaseDemo(string[] Args)
{
	int Width = Console.WindowWidth;
	int Height = Console.WindowHeight;

	var Root = new View(0, 0, Width, Height);

	// Layout: top row = image (left) + table (right), ~55% height
	// Bottom row = status (left) + output (right)
	// Progress bar at very bottom

	int Top_Height = (int)(Height * 0.55);
	int Bottom_Height = Height - Top_Height - 1;
	int Left_Width = Width / 2;
	int Right_Width = Width - Left_Width;

	// -- Build pattern list: loaded image (if provided) + test patterns --
	var Patterns = new List<(string Name, Color[,] Pixels)>();
	string? File_Path = Args.Length > 1 ? Args[1] : null;

	if (File_Path != null)
	{
		var Loaded = ImageLoader.TryLoad(File_Path);
		if (Loaded != null)
		{
			Patterns.Add((Path.GetFileName(File_Path), Loaded));
		}
	}

	Patterns.Add(("Rainbow", ImageView.GenerateTestPattern(320, 240)));
	Patterns.Add(("Color Bars", ImageView.GenerateColorBars(320, 240)));

	// -- Image View (top-left) --
	var Image_View = new ImageView(0, 0, Left_Width, Top_Height);
	Image_View.SetPixels(Patterns[0].Pixels);
	Root.Add(Image_View);

	// -- Table View (top-right) --
	var Table = new TableView(Left_Width, 0, Right_Width, Top_Height)
	{
		CanFocus = true
	};
	Table.SetHeaders("Service", "Status", "Latency");
	Table.AddRow("API Gateway", "Online", "12ms");
	Table.AddRow("Database", "Online", "3ms");
	Table.AddRow("Cache", "Warning", "45ms");
	Table.AddRow("Auth", "Online", "8ms");
	Table.AddRow("Storage", "Online", "15ms");
	Table.AddRow("Queue", "Degraded", "120ms");
	Table.AddRow("DNS", "Online", "1ms");
	Table.AddRow("CDN", "Online", "6ms");
	Root.Add(Table);

	// -- Status Panel (bottom-left) --
	var Status = new StatusPanel(0, Top_Height, Left_Width, Bottom_Height)
	{
		KeyForeground = Color.Cyan,
		ValueForeground = Color.White,
		SeparatorForeground = Color.BrightBlack,
		Separator = " : "
	};
	Status.Set("CPU", "0%");
	Status.Set("Memory", "0.0 GB");
	Status.Set("Uptime", "00:00:00");
	Status.Set("Pattern", Patterns[0].Name);
	Status.Set("Fit Mode", "Fit");
	Status.Set("Bilinear", "Off");
	Root.Add(Status);

	// -- Output View (bottom-right) --
	var Output = new OutputView(Left_Width, Top_Height, Right_Width, Bottom_Height)
	{
		Foreground = Color.White,
		CanFocus = true
	};
	Output.AppendLine("[*] Glyph Showcase initialized");
	Output.AppendLine("[+] All components loaded");
	if (File_Path != null && Patterns[0].Name != "Rainbow")
	{
		Output.AppendLine($"[+] Loaded: {Patterns[0].Name}");
	}
	Output.AppendLine("[*] Tab to cycle focus, Ctrl+Q to exit");
	Output.AppendLine("[*] P:pattern  F:fit mode  B:bilinear");
	Root.Add(Output);

	// -- Progress Bar (bottom) --
	var Progress = new ProgressIndicator(0, Height - 1, Width);
	Progress.SetProgress(0.0f, "Initializing...");
	Root.Add(Progress);

	// Animated timers
	var Start_Time = DateTime.Now;
	var Rng = new Random();
	float Sim_Progress = 0f;
	bool Progress_Forward = true;
	int Pattern_Index = 0;

	// Uptime + simulated stats
	Application.AddTimeout(TimeSpan.FromSeconds(1), () =>
	{
		var Elapsed = DateTime.Now - Start_Time;
		Status.Set("Uptime", Elapsed.ToString(@"hh\:mm\:ss"));
		Status.Set("CPU", $"{Rng.Next(5, 85)}%");
		Status.Set("Memory", $"{1.5 + Rng.NextDouble() * 2.0:F1} GB");
		return true;
	});

	// Service table jitter
	Application.AddTimeout(TimeSpan.FromSeconds(2), () =>
	{
		string[] Statuses = { "Online", "Online", "Online", "Warning", "Degraded" };
		var Rows = new List<string[]>();
		string[] Services = { "API Gateway", "Database", "Cache", "Auth", "Storage", "Queue", "DNS", "CDN" };

		foreach (var Svc in Services)
		{
			var S = Statuses[Rng.Next(Statuses.Length)];
			var Lat = S == "Online" ? $"{Rng.Next(1, 30)}ms" :
			          S == "Warning" ? $"{Rng.Next(30, 100)}ms" : $"{Rng.Next(100, 500)}ms";
			Rows.Add(new[] { Svc, S, Lat });
		}

		Table.SetRows(Rows);
		return true;
	});

	// Output log messages
	string[] Log_Messages =
	{
		"[*] Health check passed",
		"[+] Connection pool refreshed",
		"[!] Cache latency elevated",
		"[*] Metrics exported",
		"[+] TLS certificates valid",
		"[*] Backup completed",
		"[!] Queue depth increasing",
		"[+] Auto-scaling triggered",
		"[*] DNS resolution nominal"
	};
	int Log_Index = 0;

	Application.AddTimeout(TimeSpan.FromSeconds(3), () =>
	{
		Output.AppendLine(Log_Messages[Log_Index % Log_Messages.Length]);
		Log_Index++;
		return true;
	});

	// Progress bar animation
	Application.AddTimeout(TimeSpan.FromMilliseconds(200), () =>
	{
		if (Progress_Forward)
		{
			Sim_Progress += 0.02f;
			if (Sim_Progress >= 1.0f)
			{
				Sim_Progress = 1.0f;
				Progress_Forward = false;
			}
		}
		else
		{
			Sim_Progress -= 0.02f;
			if (Sim_Progress <= 0.0f)
			{
				Sim_Progress = 0.0f;
				Progress_Forward = true;
			}
		}

		Progress.SetProgress(Sim_Progress, $"Processing... {(int)(Sim_Progress * 100)}%");
		return true;
	});

	// Key handling
	Root.OnKeyPress += (Sender, E) =>
	{
		if (E.Key.IsCtrl && E.Key.CtrlLetter == 'Q')
		{
			Application.RequestStop();
			E.Handled = true;
			return;
		}

		if (E.Key.Key == ExtendedKey.Tab)
		{
			Application.FocusNext();
			E.Handled = true;
			return;
		}

		if (E.Key.Char == 'p' || E.Key.Char == 'P')
		{
			Pattern_Index = (Pattern_Index + 1) % Patterns.Count;
			Image_View.SetPixels(Patterns[Pattern_Index].Pixels);
			Status.Set("Pattern", Patterns[Pattern_Index].Name);
			Output.AppendLine($"[*] Pattern: {Patterns[Pattern_Index].Name}");
			E.Handled = true;
			return;
		}

		if (E.Key.Char == 'f' || E.Key.Char == 'F')
		{
			Image_View.FitMode = Image_View.FitMode switch
			{
				ImageFitMode.Fit => ImageFitMode.Fill,
				ImageFitMode.Fill => ImageFitMode.Stretch,
				_ => ImageFitMode.Fit
			};
			Status.Set("Fit Mode", Image_View.FitMode.ToString());
			Output.AppendLine($"[*] Fit mode: {Image_View.FitMode}");
			E.Handled = true;
			return;
		}

		if (E.Key.Char == 'b' || E.Key.Char == 'B')
		{
			Image_View.UseBilinear = !Image_View.UseBilinear;
			Status.Set("Bilinear", Image_View.UseBilinear ? "On" : "Off");
			Output.AppendLine($"[*] Bilinear: {(Image_View.UseBilinear ? "On" : "Off")}");
			E.Handled = true;
		}
	};

	// Handle resize
	Application.OnResize += (Sender, E) =>
	{
		int W = E.Width;
		int H = E.Height;
		Root.Width = W;
		Root.Height = H;

		int Th = (int)(H * 0.55);
		int Bh = H - Th - 1;
		int Lw = W / 2;
		int Rw = W - Lw;

		Image_View.Width = Lw;
		Image_View.Height = Th;

		Table.X = Lw;
		Table.Width = Rw;
		Table.Height = Th;

		Status.Y = Th;
		Status.Width = Lw;
		Status.Height = Bh;

		Output.X = Lw;
		Output.Y = Th;
		Output.Width = Rw;
		Output.Height = Bh;

		Progress.Y = H - 1;
		Progress.Width = W;
	};

	Application.Run(Root);
}

void RunImageDemo(string[] Args)
{
	int Width = Console.WindowWidth;
	int Height = Console.WindowHeight;

	var Root = new View(0, 0, Width, Height);

	// Image view fills everything except bottom status bar
	var Image_View = new ImageView(0, 0, Width, Height - 1);
	Root.Add(Image_View);

	// Bottom status bar
	var Status_Bar = new StatusPanel(0, Height - 1, Width, 1)
	{
		KeyForeground = Color.BrightBlack,
		ValueForeground = Color.White,
		SeparatorForeground = Color.BrightBlack,
		Separator = ":"
	};
	Root.Add(Status_Bar);

	// Load file if path provided, otherwise use test pattern
	string Current_Source = "Rainbow";
	string? File_Path = Args.Length > 1 ? Args[1] : null;

	if (File_Path != null)
	{
		var Loaded = ImageLoader.TryLoad(File_Path);
		if (Loaded != null)
		{
			Image_View.SetPixels(Loaded);
			Current_Source = Path.GetFileName(File_Path);
			Status_Bar.Set("Size", $"{Loaded.GetLength(0)}x{Loaded.GetLength(1)}");
		}
		else
		{
			// Fall back to test pattern on load failure
			Image_View.SetPixels(ImageView.GenerateTestPattern(640, 480));
			Current_Source = $"FAILED: {Path.GetFileName(File_Path)}";
			Status_Bar.Set("Size", "640x480");
		}
	}
	else
	{
		Image_View.SetPixels(ImageView.GenerateTestPattern(640, 480));
		Status_Bar.Set("Size", "640x480");
	}

	Status_Bar.Set("Source", Current_Source);
	Status_Bar.Set("Fit", Image_View.FitMode.ToString());
	Status_Bar.Set("Keys", "F:fit P:pattern B:bilinear Q:quit");

	Root.OnKeyPress += (Sender, E) =>
	{
		if (E.Key.Char == 'q' || E.Key.Char == 'Q' || (E.Key.IsCtrl && E.Key.CtrlLetter == 'Q'))
		{
			Application.RequestStop();
			E.Handled = true;
			return;
		}

		if (E.Key.Char == 'f' || E.Key.Char == 'F')
		{
			Image_View.FitMode = Image_View.FitMode switch
			{
				ImageFitMode.Fit => ImageFitMode.Fill,
				ImageFitMode.Fill => ImageFitMode.Stretch,
				_ => ImageFitMode.Fit
			};
			Status_Bar.Set("Fit", Image_View.FitMode.ToString());
			E.Handled = true;
			return;
		}

		if (E.Key.Char == 'b' || E.Key.Char == 'B')
		{
			Image_View.UseBilinear = !Image_View.UseBilinear;
			Status_Bar.Set("Filter", Image_View.UseBilinear ? "Bilinear" : "Nearest");
			E.Handled = true;
			return;
		}

		if (E.Key.Char == 'p' || E.Key.Char == 'P')
		{
			if (Current_Source == "Rainbow")
			{
				Current_Source = "Color Bars";
				Image_View.SetPixels(ImageView.GenerateColorBars(640, 480));
			}
			else
			{
				Current_Source = "Rainbow";
				Image_View.SetPixels(ImageView.GenerateTestPattern(640, 480));
			}
			Status_Bar.Set("Source", Current_Source);
			E.Handled = true;
		}
	};

	// Handle resize
	Application.OnResize += (Sender, E) =>
	{
		Root.Width = E.Width;
		Root.Height = E.Height;
		Image_View.Width = E.Width;
		Image_View.Height = E.Height - 1;
		Status_Bar.Y = E.Height - 1;
		Status_Bar.Width = E.Width;
	};

	Application.Run(Root);
}
