using System.Runtime.CompilerServices;
using Glyph.Core;

namespace Glyph.Backends;

/// <summary>
/// A mock stream source for testing and demonstration purposes.
/// Simulates streaming responses character by character.
/// </summary>
public class MockStreamSource : IStreamSource
{
	private readonly int _TokenDelayMs;
	private readonly int _ChunkSize;
	private readonly string[] _Responses;
	private int _ResponseIndex;

	/// <summary>
	/// Creates a mock stream source with configurable delay and chunk size.
	/// </summary>
	/// <param name="tokenDelayMs">Delay between tokens in milliseconds.</param>
	/// <param name="chunkSize">Number of characters per token.</param>
	/// <param name="responses">Optional custom responses. If null, uses default responses.</param>
	public MockStreamSource(int tokenDelayMs = 50, int chunkSize = 1, string[]? responses = null)
	{
		_TokenDelayMs = Math.Max(1, tokenDelayMs);
		_ChunkSize = Math.Max(1, chunkSize);
		_Responses = responses ?? DefaultResponses;
		_ResponseIndex = 0;
	}

	/// <inheritdoc />
	public bool IsConnected => true;

	/// <inheritdoc />
	public async IAsyncEnumerable<StreamToken> StreamAsync(
		string Prompt,
		[EnumeratorCancellation] CancellationToken Cancellation_Token = default)
	{
		// Get the next response
		var response = GetNextResponse(Prompt);

		// Stream the response character by character (or in chunks)
		int index = 0;
		while (index < response.Length)
		{
			Cancellation_Token.ThrowIfCancellationRequested();

			// Calculate chunk end
			int chunkEnd = Math.Min(index + _ChunkSize, response.Length);
			var chunk = response.Substring(index, chunkEnd - index);

			yield return StreamToken.FromText(chunk);

			index = chunkEnd;

			// Delay between tokens
			await Task.Delay(_TokenDelayMs, Cancellation_Token);
		}

		// Signal completion
		yield return StreamToken.Complete();
	}

	private string GetNextResponse(string Prompt)
	{
		// Simple response selection - cycles through available responses
		var response = _Responses[_ResponseIndex % _Responses.Length];
		_ResponseIndex++;

		// If response contains {prompt}, replace it
		if (response.Contains("{prompt}"))
		{
			response = response.Replace("{prompt}", Prompt);
		}

		return response;
	}

	private static readonly string[] DefaultResponses =
	[
		"Hello! I'm a mock assistant. You said: \"{prompt}\". How can I help you today?",
		"That's an interesting question about \"{prompt}\". Let me think about that for a moment... Actually, I'm just a mock response generator!",
		"I understand you're asking about \"{prompt}\". While I'm just a demo, I can simulate how a real assistant would respond.",
		"Thank you for your message. As a mock backend, I'm demonstrating streaming text output. Your input was: \"{prompt}\".",
		"Greetings! I received your message: \"{prompt}\". This response is being streamed character by character to demonstrate the interface."
	];
}
