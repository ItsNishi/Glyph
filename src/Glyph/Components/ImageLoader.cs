using System.IO.Compression;

namespace Glyph.Terminal;

/// <summary>
/// Zero-dependency image file loader. Supports PNG, BMP (24/32-bit uncompressed),
/// and PPM (P3/P6) formats. PNG decoding uses the runtime's built-in DeflateStream.
/// </summary>
public static class ImageLoader
{
	/// <summary>
	/// Loads an image file into a Color[,] pixel buffer.
	/// Format is detected from the file header bytes.
	/// </summary>
	/// <param name="File_Path">Path to the image file.</param>
	/// <returns>Color[x, y] pixel buffer.</returns>
	/// <exception cref="FileNotFoundException">File does not exist.</exception>
	/// <exception cref="NotSupportedException">Unrecognized or unsupported format.</exception>
	public static Color[,] Load(string File_Path)
	{
		if (!File.Exists(File_Path))
		{
			throw new FileNotFoundException("Image file not found.", File_Path);
		}

		var Data = File.ReadAllBytes(File_Path);

		if (Data.Length < 8)
		{
			throw new NotSupportedException("File too small to be a valid image.");
		}

		// Detect format from magic bytes
		if (Data[0] == 0x89 && Data[1] == 0x50 && Data[2] == 0x4E && Data[3] == 0x47) // PNG
		{
			return Load_Png(Data);
		}

		if (Data[0] == 0x42 && Data[1] == 0x4D) // "BM"
		{
			return Load_Bmp(Data);
		}

		if (Data[0] == (byte)'P' && (Data[1] == (byte)'6' || Data[1] == (byte)'3'))
		{
			return Load_Ppm(Data);
		}

		throw new NotSupportedException(
			$"Unsupported image format (magic: 0x{Data[0]:X2}{Data[1]:X2}). " +
			"Supported formats: PNG, BMP (24/32-bit), PPM (P3/P6).");
	}

	/// <summary>
	/// Tries to load an image file. Returns null on failure.
	/// </summary>
	public static Color[,]? TryLoad(string File_Path)
	{
		try
		{
			return Load(File_Path);
		}
		catch
		{
			return null;
		}
	}

	#region PNG Loader

	private static readonly byte[] Png_Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

	private static Color[,] Load_Png(byte[] Data)
	{
		// Validate signature
		for (int I = 0; I < 8; I++)
		{
			if (Data[I] != Png_Signature[I])
			{
				throw new NotSupportedException("Invalid PNG signature.");
			}
		}

		int Pos = 8;

		// IHDR must be the first chunk
		var Ihdr = Read_Png_Chunk(Data, ref Pos);
		if (Ihdr.Type != "IHDR" || Ihdr.Data.Length < 13)
		{
			throw new NotSupportedException("PNG missing IHDR chunk.");
		}

		int Width = Read_Int32_Be(Ihdr.Data, 0);
		int Height = Read_Int32_Be(Ihdr.Data, 4);
		int Bit_Depth = Ihdr.Data[8];
		int Color_Type = Ihdr.Data[9];
		int Compression = Ihdr.Data[10];
		int Filter_Method = Ihdr.Data[11];
		int Interlace = Ihdr.Data[12];

		if (Width <= 0 || Height <= 0)
		{
			throw new NotSupportedException($"Invalid PNG dimensions: {Width}x{Height}.");
		}

		if (Width > 32768 || Height > 32768)
		{
			throw new NotSupportedException($"PNG too large: {Width}x{Height}. Max 32768x32768.");
		}

		if (Compression != 0)
		{
			throw new NotSupportedException($"Unsupported PNG compression method: {Compression}.");
		}

		if (Filter_Method != 0)
		{
			throw new NotSupportedException($"Unsupported PNG filter method: {Filter_Method}.");
		}

		if (Interlace != 0)
		{
			throw new NotSupportedException("Interlaced PNGs are not supported. Re-save as non-interlaced.");
		}

		// Validate color type / bit depth combinations
		int Channels = Color_Type switch
		{
			0 => 1, // Grayscale
			2 => 3, // RGB
			3 => 1, // Indexed (palette)
			4 => 2, // Grayscale + Alpha
			6 => 4, // RGBA
			_ => throw new NotSupportedException($"Unsupported PNG color type: {Color_Type}.")
		};

		if (Bit_Depth != 8 && Bit_Depth != 16 && Color_Type != 3)
		{
			throw new NotSupportedException(
				$"Unsupported PNG bit depth {Bit_Depth} for color type {Color_Type}. Only 8/16-bit supported.");
		}

		if (Color_Type == 3 && Bit_Depth != 1 && Bit_Depth != 2 && Bit_Depth != 4 && Bit_Depth != 8)
		{
			throw new NotSupportedException(
				$"Unsupported PNG bit depth {Bit_Depth} for indexed color. Only 1/2/4/8-bit supported.");
		}

		// Collect IDAT data and optional PLTE
		byte[]? Palette = null;
		byte[]? Trns = null;
		using var Idat_Stream = new MemoryStream();

		while (Pos < Data.Length)
		{
			var Chunk = Read_Png_Chunk(Data, ref Pos);

			if (Chunk.Type == "IDAT")
			{
				Idat_Stream.Write(Chunk.Data, 0, Chunk.Data.Length);
			}
			else if (Chunk.Type == "PLTE")
			{
				Palette = Chunk.Data;
			}
			else if (Chunk.Type == "tRNS")
			{
				Trns = Chunk.Data;
			}
			else if (Chunk.Type == "IEND")
			{
				break;
			}
		}

		if (Idat_Stream.Length == 0)
		{
			throw new NotSupportedException("PNG contains no IDAT chunks.");
		}

		if (Color_Type == 3 && Palette == null)
		{
			throw new NotSupportedException("Indexed PNG missing PLTE chunk.");
		}

		// Decompress IDAT data (skip 2-byte zlib header)
		Idat_Stream.Position = 2; // Skip zlib CMF + FLG bytes
		byte[] Raw_Data;
		using (var Deflate = new DeflateStream(Idat_Stream, CompressionMode.Decompress))
		using (var Output = new MemoryStream())
		{
			Deflate.CopyTo(Output);
			Raw_Data = Output.ToArray();
		}

		// Calculate bytes per pixel and scanline stride
		int Bpp; // Bytes per complete pixel (used for filter byte offset)
		int Stride; // Bytes per scanline excluding the filter byte

		if (Color_Type == 3)
		{
			// Indexed: pixels are packed bit values
			Bpp = 1; // Filter operates on byte boundaries
			Stride = (Width * Bit_Depth + 7) / 8;
		}
		else
		{
			int Bits = Channels * Bit_Depth;
			Bpp = Math.Max(1, Bits / 8);
			Stride = Width * Bpp;
		}

		int Expected_Size = Height * (1 + Stride); // +1 for filter byte per row
		if (Raw_Data.Length < Expected_Size)
		{
			throw new NotSupportedException(
				$"PNG decompressed data too small: got {Raw_Data.Length}, expected {Expected_Size}.");
		}

		// Reconstruct scanlines (reverse PNG filtering)
		var Scanlines = new byte[Height][];
		var Prev_Row = new byte[Stride]; // Zero-filled for first row

		for (int Y = 0; Y < Height; Y++)
		{
			int Row_Offset = Y * (1 + Stride);
			byte Filter_Type = Raw_Data[Row_Offset];
			var Row = new byte[Stride];
			Array.Copy(Raw_Data, Row_Offset + 1, Row, 0, Stride);

			Apply_Png_Filter(Row, Prev_Row, Filter_Type, Bpp);

			Scanlines[Y] = Row;
			Prev_Row = Row;
		}

		// Convert scanlines to Color[,]
		var Pixels = new Color[Width, Height];

		for (int Y = 0; Y < Height; Y++)
		{
			var Row = Scanlines[Y];

			for (int X = 0; X < Width; X++)
			{
				Color C;

				switch (Color_Type)
				{
					case 0: // Grayscale
					{
						byte V;
						if (Bit_Depth == 16)
						{
							V = Row[X * 2]; // Use high byte
						}
						else
						{
							V = Row[X];
						}
						C = Color.FromRgb(V, V, V);
						break;
					}
					case 2: // RGB
					{
						if (Bit_Depth == 16)
						{
							byte R = Row[X * 6];
							byte G = Row[X * 6 + 2];
							byte B = Row[X * 6 + 4];
							C = Color.FromRgb(R, G, B);
						}
						else
						{
							byte R = Row[X * 3];
							byte G = Row[X * 3 + 1];
							byte B = Row[X * 3 + 2];
							C = Color.FromRgb(R, G, B);
						}
						break;
					}
					case 3: // Indexed
					{
						int Index = Get_Packed_Pixel(Row, X, Bit_Depth);
						int Pi = Index * 3;
						if (Palette != null && Pi + 2 < Palette.Length)
						{
							C = Color.FromRgb(Palette[Pi], Palette[Pi + 1], Palette[Pi + 2]);
						}
						else
						{
							C = Color.Black;
						}
						break;
					}
					case 4: // Grayscale + Alpha
					{
						byte V;
						if (Bit_Depth == 16)
						{
							V = Row[X * 4]; // High byte of gray
						}
						else
						{
							V = Row[X * 2];
						}
						C = Color.FromRgb(V, V, V);
						break;
					}
					case 6: // RGBA
					{
						if (Bit_Depth == 16)
						{
							byte R = Row[X * 8];
							byte G = Row[X * 8 + 2];
							byte B = Row[X * 8 + 4];
							C = Color.FromRgb(R, G, B);
						}
						else
						{
							byte R = Row[X * 4];
							byte G = Row[X * 4 + 1];
							byte B = Row[X * 4 + 2];
							C = Color.FromRgb(R, G, B);
						}
						break;
					}
					default:
						C = Color.Black;
						break;
				}

				Pixels[X, Y] = C;
			}
		}

		return Pixels;
	}

	private static void Apply_Png_Filter(byte[] Row, byte[] Prev_Row, byte Filter_Type, int Bpp)
	{
		switch (Filter_Type)
		{
			case 0: // None
				break;

			case 1: // Sub
				for (int I = Bpp; I < Row.Length; I++)
				{
					Row[I] = (byte)(Row[I] + Row[I - Bpp]);
				}
				break;

			case 2: // Up
				for (int I = 0; I < Row.Length; I++)
				{
					Row[I] = (byte)(Row[I] + Prev_Row[I]);
				}
				break;

			case 3: // Average
				for (int I = 0; I < Row.Length; I++)
				{
					int A = I >= Bpp ? Row[I - Bpp] : 0;
					int B = Prev_Row[I];
					Row[I] = (byte)(Row[I] + (A + B) / 2);
				}
				break;

			case 4: // Paeth
				for (int I = 0; I < Row.Length; I++)
				{
					int A = I >= Bpp ? Row[I - Bpp] : 0;
					int B = Prev_Row[I];
					int C = I >= Bpp ? Prev_Row[I - Bpp] : 0;
					Row[I] = (byte)(Row[I] + Paeth_Predictor(A, B, C));
				}
				break;

			default:
				throw new NotSupportedException($"Unknown PNG filter type: {Filter_Type}.");
		}
	}

	private static int Paeth_Predictor(int A, int B, int C)
	{
		int P = A + B - C;
		int Pa = Math.Abs(P - A);
		int Pb = Math.Abs(P - B);
		int Pc = Math.Abs(P - C);

		if (Pa <= Pb && Pa <= Pc) return A;
		if (Pb <= Pc) return B;
		return C;
	}

	private static int Get_Packed_Pixel(byte[] Row, int X, int Bit_Depth)
	{
		if (Bit_Depth == 8)
		{
			return X < Row.Length ? Row[X] : 0;
		}

		int Pixels_Per_Byte = 8 / Bit_Depth;
		int Byte_Index = X / Pixels_Per_Byte;
		int Bit_Offset = (Pixels_Per_Byte - 1 - (X % Pixels_Per_Byte)) * Bit_Depth;
		int Mask = (1 << Bit_Depth) - 1;

		if (Byte_Index >= Row.Length)
		{
			return 0;
		}

		return (Row[Byte_Index] >> Bit_Offset) & Mask;
	}

	private readonly struct Png_Chunk
	{
		public readonly string Type;
		public readonly byte[] Data;

		public Png_Chunk(string Type, byte[] Data)
		{
			this.Type = Type;
			this.Data = Data;
		}
	}

	private static Png_Chunk Read_Png_Chunk(byte[] Data, ref int Pos)
	{
		if (Pos + 12 > Data.Length)
		{
			throw new NotSupportedException("Truncated PNG chunk.");
		}

		int Length = Read_Int32_Be(Data, Pos);
		Pos += 4;

		string Type = System.Text.Encoding.ASCII.GetString(Data, Pos, 4);
		Pos += 4;

		if (Length < 0 || Pos + Length + 4 > Data.Length)
		{
			throw new NotSupportedException($"Invalid PNG chunk length: {Length} for chunk {Type}.");
		}

		var Chunk_Data = new byte[Length];
		Array.Copy(Data, Pos, Chunk_Data, 0, Length);
		Pos += Length;

		// Skip CRC (4 bytes) -- not validating it
		Pos += 4;

		return new Png_Chunk(Type, Chunk_Data);
	}

	private static int Read_Int32_Be(byte[] Data, int Offset)
	{
		return (Data[Offset] << 24)
			| (Data[Offset + 1] << 16)
			| (Data[Offset + 2] << 8)
			| Data[Offset + 3];
	}

	#endregion

	#region BMP Loader

	private static Color[,] Load_Bmp(byte[] Data)
	{
		// BMP File Header (14 bytes)
		if (Data.Length < 54)
		{
			throw new NotSupportedException("BMP file too small for valid headers.");
		}

		int Pixel_Offset = Read_Int32_Le(Data, 10);

		// DIB Header (BITMAPINFOHEADER starts at offset 14)
		int Dib_Size = Read_Int32_Le(Data, 14);

		if (Dib_Size < 40)
		{
			throw new NotSupportedException(
				$"Unsupported BMP DIB header size: {Dib_Size}. " +
				"Only BITMAPINFOHEADER (40+) is supported.");
		}

		int Width = Read_Int32_Le(Data, 18);
		int Height_Raw = Read_Int32_Le(Data, 22);
		int Bits_Per_Pixel = Read_Uint16_Le(Data, 28);
		int Compression = Read_Int32_Le(Data, 30);

		// Height can be negative (top-down) or positive (bottom-up)
		bool Top_Down = Height_Raw < 0;
		int Height = Math.Abs(Height_Raw);

		if (Width <= 0 || Height <= 0)
		{
			throw new NotSupportedException($"Invalid BMP dimensions: {Width}x{Height}.");
		}

		if (Width > 32768 || Height > 32768)
		{
			throw new NotSupportedException($"BMP too large: {Width}x{Height}. Max 32768x32768.");
		}

		// Only support uncompressed (BI_RGB = 0) and BI_BITFIELDS = 3 for 32-bit
		if (Compression != 0 && Compression != 3)
		{
			throw new NotSupportedException(
				$"Unsupported BMP compression: {Compression}. Only uncompressed (0) and bitfields (3) supported.");
		}

		if (Bits_Per_Pixel != 24 && Bits_Per_Pixel != 32)
		{
			throw new NotSupportedException(
				$"Unsupported BMP bit depth: {Bits_Per_Pixel}. Only 24-bit and 32-bit supported.");
		}

		var Pixels = new Color[Width, Height];
		int Bytes_Per_Pixel = Bits_Per_Pixel / 8;

		// BMP rows are padded to 4-byte boundaries
		int Row_Size = (Width * Bytes_Per_Pixel + 3) & ~3;

		for (int Y = 0; Y < Height; Y++)
		{
			// Bottom-up: first row in file is bottom of image
			int Src_Row = Top_Down ? Y : (Height - 1 - Y);
			int Row_Start = Pixel_Offset + Src_Row * Row_Size;

			for (int X = 0; X < Width; X++)
			{
				int Px = Row_Start + X * Bytes_Per_Pixel;

				if (Px + Bytes_Per_Pixel > Data.Length)
				{
					break;
				}

				// BMP stores BGR(A), not RGB
				byte B = Data[Px];
				byte G = Data[Px + 1];
				byte R = Data[Px + 2];

				Pixels[X, Y] = Color.FromRgb(R, G, B);
			}
		}

		return Pixels;
	}

	private static int Read_Int32_Le(byte[] Data, int Offset)
	{
		return Data[Offset]
			| (Data[Offset + 1] << 8)
			| (Data[Offset + 2] << 16)
			| (Data[Offset + 3] << 24);
	}

	private static int Read_Uint16_Le(byte[] Data, int Offset)
	{
		return Data[Offset] | (Data[Offset + 1] << 8);
	}

	#endregion

	#region PPM Loader

	private static Color[,] Load_Ppm(byte[] Data)
	{
		int Pos = 0;

		// Read magic
		string Magic = Read_Ppm_Token(Data, ref Pos);

		if (Magic != "P6" && Magic != "P3")
		{
			throw new NotSupportedException($"Unsupported PPM magic: {Magic}. Only P3 and P6 supported.");
		}

		int Width = int.Parse(Read_Ppm_Token(Data, ref Pos));
		int Height = int.Parse(Read_Ppm_Token(Data, ref Pos));
		int Max_Val = int.Parse(Read_Ppm_Token(Data, ref Pos));

		if (Width <= 0 || Height <= 0)
		{
			throw new NotSupportedException($"Invalid PPM dimensions: {Width}x{Height}.");
		}

		if (Width > 32768 || Height > 32768)
		{
			throw new NotSupportedException($"PPM too large: {Width}x{Height}. Max 32768x32768.");
		}

		var Pixels = new Color[Width, Height];

		if (Magic == "P6")
		{
			// Binary format: after the last header token, skip exactly one whitespace byte
			// then read raw RGB data
			Pos++; // Skip the single whitespace delimiter after max value

			bool Wide = Max_Val > 255;
			int Bytes_Per_Sample = Wide ? 2 : 1;

			for (int Y = 0; Y < Height; Y++)
			{
				for (int X = 0; X < Width; X++)
				{
					if (Pos + Bytes_Per_Sample * 3 > Data.Length)
					{
						goto Done;
					}

					byte R, G, B;

					if (Wide)
					{
						// 16-bit samples, big-endian, scale to 8-bit
						int R16 = (Data[Pos] << 8) | Data[Pos + 1];
						int G16 = (Data[Pos + 2] << 8) | Data[Pos + 3];
						int B16 = (Data[Pos + 4] << 8) | Data[Pos + 5];
						R = (byte)(R16 * 255 / Max_Val);
						G = (byte)(G16 * 255 / Max_Val);
						B = (byte)(B16 * 255 / Max_Val);
						Pos += 6;
					}
					else
					{
						R = (byte)(Data[Pos] * 255 / Max_Val);
						G = (byte)(Data[Pos + 1] * 255 / Max_Val);
						B = (byte)(Data[Pos + 2] * 255 / Max_Val);
						Pos += 3;
					}

					Pixels[X, Y] = Color.FromRgb(R, G, B);
				}
			}
		}
		else // P3 - ASCII format
		{
			for (int Y = 0; Y < Height; Y++)
			{
				for (int X = 0; X < Width; X++)
				{
					string? R_Str = Read_Ppm_Token_Safe(Data, ref Pos);
					string? G_Str = Read_Ppm_Token_Safe(Data, ref Pos);
					string? B_Str = Read_Ppm_Token_Safe(Data, ref Pos);

					if (R_Str == null || G_Str == null || B_Str == null)
					{
						goto Done;
					}

					byte R = (byte)(int.Parse(R_Str) * 255 / Max_Val);
					byte G = (byte)(int.Parse(G_Str) * 255 / Max_Val);
					byte B = (byte)(int.Parse(B_Str) * 255 / Max_Val);

					Pixels[X, Y] = Color.FromRgb(R, G, B);
				}
			}
		}

		Done:
		return Pixels;
	}

	/// <summary>
	/// Reads the next whitespace-delimited token from PPM data, skipping comments.
	/// </summary>
	private static string Read_Ppm_Token(byte[] Data, ref int Pos)
	{
		var Token = Read_Ppm_Token_Safe(Data, ref Pos);

		if (Token == null)
		{
			throw new NotSupportedException("Unexpected end of PPM data while reading header.");
		}

		return Token;
	}

	private static string? Read_Ppm_Token_Safe(byte[] Data, ref int Pos)
	{
		// Skip whitespace and comments
		while (Pos < Data.Length)
		{
			byte C = Data[Pos];

			if (C == (byte)'#')
			{
				// Skip comment until end of line
				while (Pos < Data.Length && Data[Pos] != (byte)'\n')
				{
					Pos++;
				}
				continue;
			}

			if (C == (byte)' ' || C == (byte)'\t' || C == (byte)'\r' || C == (byte)'\n')
			{
				Pos++;
				continue;
			}

			break;
		}

		if (Pos >= Data.Length)
		{
			return null;
		}

		// Read token characters
		int Start = Pos;
		while (Pos < Data.Length)
		{
			byte C = Data[Pos];
			if (C == (byte)' ' || C == (byte)'\t' || C == (byte)'\r' || C == (byte)'\n')
			{
				break;
			}
			Pos++;
		}

		return System.Text.Encoding.ASCII.GetString(Data, Start, Pos - Start);
	}

	#endregion
}
