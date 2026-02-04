using Xunit;
using Glyph.Backends;
using Glyph.Core;

namespace Glyph.Tests;

public class MockStreamSourceTests
{
	[Fact]
	public async Task StreamAsync_ReturnsTokens()
	{
		var source = new MockStreamSource(tokenDelayMs: 1, chunkSize: 2);
		var tokens = new List<StreamToken>();

		await foreach (var token in source.StreamAsync("Hello"))
		{
			tokens.Add(token);
		}

		Assert.NotEmpty(tokens);
		Assert.True(tokens[^1].IsComplete);
	}

	[Fact]
	public async Task StreamAsync_CancellationStopsStream()
	{
		var source = new MockStreamSource(tokenDelayMs: 100, chunkSize: 1);
		var cts = new CancellationTokenSource();
		var tokens = new List<StreamToken>();

		cts.CancelAfter(50);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
		{
			await foreach (var token in source.StreamAsync("Hello", cts.Token))
			{
				tokens.Add(token);
			}
		});
	}

	[Fact]
	public void IsConnected_ReturnsTrue()
	{
		var source = new MockStreamSource();
		Assert.True(source.IsConnected);
	}
}
