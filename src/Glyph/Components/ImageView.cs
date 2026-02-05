namespace Glyph.Terminal;

/// <summary>
/// How the image is scaled to fit the view bounds.
/// </summary>
public enum ImageFitMode
{
	/// <summary>Scale to fit within bounds, preserving aspect ratio (letterboxed).</summary>
	Fit,

	/// <summary>Scale to fill bounds, preserving aspect ratio (cropped).</summary>
	Fill,

	/// <summary>Scale to exactly match bounds, ignoring aspect ratio.</summary>
	Stretch
}

/// <summary>
/// Renders a pixel buffer as half-block characters with true color.
/// Each terminal cell displays two vertical pixels using the upper-half block character,
/// with the top pixel as foreground and bottom pixel as background.
/// </summary>
public class ImageView : View
{
	private Color[,]? _Pixels;
	private Color[,]? _Scaled_Cache;
	private int _Cache_Width;
	private int _Cache_Height;
	private ImageFitMode _FitMode = ImageFitMode.Fit;

	/// <summary>
	/// The pixel buffer. Set via SetPixels().
	/// </summary>
	public Color[,]? Pixels => _Pixels;

	/// <summary>
	/// How the image is scaled to fit the view.
	/// </summary>
	public ImageFitMode FitMode
	{
		get => _FitMode;
		set
		{
			if (_FitMode != value)
			{
				_FitMode = value;
				Invalidate_Cache();
				SetNeedsDraw();
			}
		}
	}

	/// <summary>
	/// Color used for letterbox/pillarbox areas in Fit mode.
	/// </summary>
	public Color LetterboxColor { get; set; } = Color.Black;

	/// <summary>
	/// When true, uses bilinear interpolation for scaling. Otherwise nearest-neighbor.
	/// </summary>
	public bool UseBilinear { get; set; } = false;

	/// <summary>
	/// Width of the source pixel buffer (0 if no pixels set).
	/// </summary>
	public int PixelWidth => _Pixels?.GetLength(0) ?? 0;

	/// <summary>
	/// Height of the source pixel buffer (0 if no pixels set).
	/// </summary>
	public int PixelHeight => _Pixels?.GetLength(1) ?? 0;

	public ImageView()
	{
	}

	public ImageView(int X, int Y, int Width, int Height) : base(X, Y, Width, Height)
	{
	}

	/// <summary>
	/// Sets the pixel buffer. Pixels[x, y] where x is column, y is row.
	/// </summary>
	public void SetPixels(Color[,] Pixels)
	{
		_Pixels = Pixels;
		Invalidate_Cache();
		SetNeedsDraw();
	}

	/// <summary>
	/// Clears the pixel buffer.
	/// </summary>
	public void ClearPixels()
	{
		_Pixels = null;
		Invalidate_Cache();
		SetNeedsDraw();
	}

	public override void Draw(Screen Screen)
	{
		if (!Visible)
		{
			return;
		}

		var (Sx, Sy) = LocalToScreen(0, 0);

		if (_Pixels == null || Width <= 0 || Height <= 0)
		{
			Fill_Letterbox(Screen, Sx, Sy, 0, 0, Width, Height);
			base.Draw(Screen);
			return;
		}

		int Src_W = _Pixels.GetLength(0);
		int Src_H = _Pixels.GetLength(1);

		if (Src_W == 0 || Src_H == 0)
		{
			Fill_Letterbox(Screen, Sx, Sy, 0, 0, Width, Height);
			base.Draw(Screen);
			return;
		}

		// Each cell represents 2 vertical pixels
		int Target_W = Width;
		int Target_H = Height * 2;

		int Scaled_W;
		int Scaled_H;
		int Offset_X = 0;
		int Offset_Y = 0;

		switch (_FitMode)
		{
			case ImageFitMode.Fit:
			{
				float Scale_X = (float)Target_W / Src_W;
				float Scale_Y = (float)Target_H / Src_H;
				float Scale = Math.Min(Scale_X, Scale_Y);
				Scaled_W = Math.Max(1, (int)(Src_W * Scale));
				Scaled_H = Math.Max(1, (int)(Src_H * Scale));
				Offset_X = (Target_W - Scaled_W) / 2;
				Offset_Y = (Target_H - Scaled_H) / 2;
				break;
			}
			case ImageFitMode.Fill:
			{
				float Scale_X = (float)Target_W / Src_W;
				float Scale_Y = (float)Target_H / Src_H;
				float Scale = Math.Max(Scale_X, Scale_Y);
				Scaled_W = Math.Max(1, (int)(Src_W * Scale));
				Scaled_H = Math.Max(1, (int)(Src_H * Scale));
				Offset_X = (Target_W - Scaled_W) / 2;
				Offset_Y = (Target_H - Scaled_H) / 2;
				break;
			}
			case ImageFitMode.Stretch:
			default:
			{
				Scaled_W = Target_W;
				Scaled_H = Target_H;
				break;
			}
		}

		var Scaled = Get_Scaled_Pixels(_Pixels, Scaled_W, Scaled_H);

		for (int Cy = 0; Cy < Height; Cy++)
		{
			for (int Cx = 0; Cx < Width; Cx++)
			{
				int Px_Top = Cx - Offset_X;
				int Py_Top = (Cy * 2) - Offset_Y;
				int Py_Bot = Py_Top + 1;

				bool Top_Valid = Px_Top >= 0 && Px_Top < Scaled_W && Py_Top >= 0 && Py_Top < Scaled_H;
				bool Bot_Valid = Px_Top >= 0 && Px_Top < Scaled_W && Py_Bot >= 0 && Py_Bot < Scaled_H;

				Color Top_Color = Top_Valid ? Scaled[Px_Top, Py_Top] : LetterboxColor;
				Color Bot_Color = Bot_Valid ? Scaled[Px_Top, Py_Bot] : LetterboxColor;

				Screen.SetCell(Sx + Cx, Sy + Cy, BoxChars.UpperHalf, Top_Color, Bot_Color);
			}
		}

		base.Draw(Screen);
	}

	private void Fill_Letterbox(Screen Screen, int Sx, int Sy, int X, int Y, int W, int H)
	{
		for (int Cy = Y; Cy < Y + H; Cy++)
		{
			for (int Cx = X; Cx < X + W; Cx++)
			{
				Screen.SetCell(Sx + Cx, Sy + Cy, ' ', LetterboxColor, LetterboxColor);
			}
		}
	}

	private Color[,] Get_Scaled_Pixels(Color[,] Source, int Target_W, int Target_H)
	{
		if (_Scaled_Cache != null && _Cache_Width == Target_W && _Cache_Height == Target_H)
		{
			return _Scaled_Cache;
		}

		_Scaled_Cache = Scale_Pixels(Source, Target_W, Target_H);
		_Cache_Width = Target_W;
		_Cache_Height = Target_H;
		return _Scaled_Cache;
	}

	private Color[,] Scale_Pixels(Color[,] Source, int Target_W, int Target_H)
	{
		int Src_W = Source.GetLength(0);
		int Src_H = Source.GetLength(1);
		var Result = new Color[Target_W, Target_H];

		for (int Y = 0; Y < Target_H; Y++)
		{
			float V = (float)Y / (Target_H - 1);
			if (Target_H == 1) V = 0.5f;

			for (int X = 0; X < Target_W; X++)
			{
				float U = (float)X / (Target_W - 1);
				if (Target_W == 1) U = 0.5f;

				Result[X, Y] = UseBilinear
					? Sample_Bilinear(Source, U, V)
					: Sample_Nearest(Source, U, V);
			}
		}

		return Result;
	}

	private static Color Sample_Nearest(Color[,] Src, float U, float V)
	{
		int W = Src.GetLength(0);
		int H = Src.GetLength(1);
		int X = Math.Clamp((int)(U * (W - 1) + 0.5f), 0, W - 1);
		int Y = Math.Clamp((int)(V * (H - 1) + 0.5f), 0, H - 1);
		return Src[X, Y];
	}

	private static Color Sample_Bilinear(Color[,] Src, float U, float V)
	{
		int W = Src.GetLength(0);
		int H = Src.GetLength(1);

		float Fx = U * (W - 1);
		float Fy = V * (H - 1);

		int X0 = Math.Clamp((int)Fx, 0, W - 1);
		int Y0 = Math.Clamp((int)Fy, 0, H - 1);
		int X1 = Math.Min(X0 + 1, W - 1);
		int Y1 = Math.Min(Y0 + 1, H - 1);

		float Tx = Fx - X0;
		float Ty = Fy - Y0;

		var Top = Color.Lerp(Src[X0, Y0], Src[X1, Y0], Tx);
		var Bot = Color.Lerp(Src[X0, Y1], Src[X1, Y1], Tx);
		return Color.Lerp(Top, Bot, Ty);
	}

	private void Invalidate_Cache()
	{
		_Scaled_Cache = null;
		_Cache_Width = 0;
		_Cache_Height = 0;
	}

	/// <summary>
	/// Generates an HSV rainbow test pattern.
	/// Horizontal axis = hue (0-360), vertical axis = brightness (1.0 to 0.0).
	/// </summary>
	public static Color[,] GenerateTestPattern(int Width, int Height)
	{
		var Pixels = new Color[Width, Height];

		for (int Y = 0; Y < Height; Y++)
		{
			float Brightness = 1.0f - (float)Y / Math.Max(1, Height - 1);

			for (int X = 0; X < Width; X++)
			{
				float Hue = (float)X / Math.Max(1, Width - 1) * 360f;
				var (R, G, B) = Hsv_To_Rgb(Hue, 1.0f, Brightness);
				Pixels[X, Y] = Color.FromRgb(R, G, B);
			}
		}

		return Pixels;
	}

	/// <summary>
	/// Generates SMPTE-style vertical color bars.
	/// </summary>
	public static Color[,] GenerateColorBars(int Width, int Height)
	{
		var Pixels = new Color[Width, Height];

		var Bar_Colors = new Color[]
		{
			Color.FromRgb(191, 191, 191), // White (75%)
			Color.FromRgb(191, 191, 0),   // Yellow
			Color.FromRgb(0, 191, 191),   // Cyan
			Color.FromRgb(0, 191, 0),     // Green
			Color.FromRgb(191, 0, 191),   // Magenta
			Color.FromRgb(191, 0, 0),     // Red
			Color.FromRgb(0, 0, 191),     // Blue
			Color.FromRgb(16, 16, 16),    // Black
		};

		for (int Y = 0; Y < Height; Y++)
		{
			for (int X = 0; X < Width; X++)
			{
				int Bar_Index = (int)((float)X / Width * Bar_Colors.Length);
				Bar_Index = Math.Clamp(Bar_Index, 0, Bar_Colors.Length - 1);
				Pixels[X, Y] = Bar_Colors[Bar_Index];
			}
		}

		return Pixels;
	}

	private static (byte R, byte G, byte B) Hsv_To_Rgb(float H, float S, float V)
	{
		H = H % 360f;
		if (H < 0) H += 360f;

		float C = V * S;
		float X = C * (1f - Math.Abs((H / 60f) % 2f - 1f));
		float M = V - C;

		float R, G, B;

		if (H < 60f) { R = C; G = X; B = 0; }
		else if (H < 120f) { R = X; G = C; B = 0; }
		else if (H < 180f) { R = 0; G = C; B = X; }
		else if (H < 240f) { R = 0; G = X; B = C; }
		else if (H < 300f) { R = X; G = 0; B = C; }
		else { R = C; G = 0; B = X; }

		return (
			(byte)Math.Clamp((int)((R + M) * 255), 0, 255),
			(byte)Math.Clamp((int)((G + M) * 255), 0, 255),
			(byte)Math.Clamp((int)((B + M) * 255), 0, 255)
		);
	}
}
