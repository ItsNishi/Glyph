namespace Glyph.Core;

/// <summary>
/// Interface for streaming text sources (e.g., LLM APIs, WebSockets).
/// </summary>
public interface IStreamSource
{
	/// <summary>
	/// Whether the source is currently connected/ready.
	/// </summary>
	bool IsConnected { get; }

	/// <summary>
	/// Streams tokens asynchronously in response to a prompt.
	/// </summary>
	/// <param name="Prompt">The input prompt/message.</param>
	/// <param name="Cancellation_Token">Cancellation token to stop the stream.</param>
	/// <returns>An async enumerable of stream tokens.</returns>
	IAsyncEnumerable<StreamToken> StreamAsync(
		string Prompt,
		CancellationToken Cancellation_Token = default);
}

/// <summary>
/// Event args for connection state changes.
/// </summary>
public class ConnectionStateChangedEventArgs : EventArgs
{
	/// <summary>
	/// Whether the source is now connected.
	/// </summary>
	public bool IsConnected { get; }

	/// <summary>
	/// Error message if disconnection was due to an error.
	/// </summary>
	public string? Error { get; }

	public ConnectionStateChangedEventArgs(bool Is_Connected, string? Error = null)
	{
		this.IsConnected = Is_Connected;
		this.Error = Error;
	}
}
