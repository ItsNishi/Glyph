namespace Glyph.Core;

/// <summary>
/// Represents a token in a streaming response.
/// </summary>
public readonly record struct StreamToken
{
	/// <summary>
	/// The text content of this token.
	/// </summary>
	public string Text { get; init; }

	/// <summary>
	/// Whether this is the final token in the stream.
	/// </summary>
	public bool IsComplete { get; init; }

	/// <summary>
	/// Error message if an error occurred, null otherwise.
	/// </summary>
	public string? Error { get; init; }

	/// <summary>
	/// Whether this token represents an error.
	/// </summary>
	public bool IsError => !string.IsNullOrEmpty(Error);

	/// <summary>
	/// Creates a text token.
	/// </summary>
	public static StreamToken FromText(string Text) => new()
	{
		Text = Text,
		IsComplete = false,
		Error = null
	};

	/// <summary>
	/// Creates a completion token.
	/// </summary>
	public static StreamToken Complete() => new()
	{
		Text = string.Empty,
		IsComplete = true,
		Error = null
	};

	/// <summary>
	/// Creates an error token.
	/// </summary>
	public static StreamToken FromError(string Error) => new()
	{
		Text = string.Empty,
		IsComplete = true,
		Error = Error
	};
}
