# Backends Guide

Backends provide streaming data sources for Glyph applications. All backends implement `IStreamSource`.

## IStreamSource Interface

```csharp
public interface IStreamSource
{
    IAsyncEnumerable<StreamToken> StreamAsync(
        string input,
        CancellationToken cancellationToken = default);

    bool IsConnected { get; }

    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
}
```

## StreamToken

```csharp
public record StreamToken
{
    public required string Text { get; init; }
    public bool IsComplete { get; init; }
    public string? Error { get; init; }
    public string? Metadata { get; init; }
}
```

---

## MockStreamSource

Simulated streaming for testing and demos.

```csharp
var backend = new MockStreamSource(
    tokenDelayMs: 50,   // Delay between tokens
    chunkSize: 3        // Words per token
);

await foreach (var token in backend.StreamAsync("Hello"))
{
    Console.Write(token.Text);
    if (token.IsComplete) break;
}
```

---

## HttpSseBackend

Server-Sent Events (SSE) backend for HTTP streaming APIs.

```csharp
var backend = new HttpSseBackend("https://api.example.com/stream");

// With custom HTTP client
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer token");
var backend = new HttpSseBackend("https://api.example.com/stream", httpClient);
```

### Custom Request Body

```csharp
var backend = new HttpSseBackend(
    "https://api.example.com/stream",
    requestBodyBuilder: input => new StringContent(
        JsonSerializer.Serialize(new { prompt = input, max_tokens = 100 }),
        Encoding.UTF8,
        "application/json"
    )
);
```

### Custom Response Parser

```csharp
var backend = new HttpSseBackend(
    "https://api.example.com/stream",
    responseParser: data =>
    {
        var json = JsonDocument.Parse(data);
        var text = json.RootElement.GetProperty("delta").GetString();
        var done = json.RootElement.GetProperty("finished").GetBoolean();
        return new StreamToken { Text = text ?? "", IsComplete = done };
    }
);
```

### Expected SSE Format

```
data: {"text": "Hello", "done": false}
data: {"text": " world", "done": false}
data: {"text": "!", "done": true}
```

Or:

```
data: {"content": "Hello world!"}
data: [DONE]
```

---

## WebSocketBackend

WebSocket backend for bidirectional streaming.

```csharp
var backend = new WebSocketBackend("wss://api.example.com/ws");

// With custom message serializer
var backend = new WebSocketBackend(
    "wss://api.example.com/ws",
    messageSerializer: input => JsonSerializer.Serialize(new { query = input })
);
```

### Custom Response Parser

```csharp
var backend = new WebSocketBackend(
    "wss://api.example.com/ws",
    responseParser: data =>
    {
        var json = JsonDocument.Parse(data);
        return new StreamToken
        {
            Text = json.RootElement.GetProperty("response").GetString() ?? "",
            IsComplete = json.RootElement.GetProperty("end").GetBoolean()
        };
    }
);
```

---

## Creating Custom Backends

Implement `IStreamSource` to create custom backends:

```csharp
public class MyBackend : IStreamSource
{
    public bool IsConnected => true;

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public async IAsyncEnumerable<StreamToken> StreamAsync(
        string input,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Your streaming logic here
        yield return new StreamToken { Text = "Response", IsComplete = true };
    }
}
```

### Connection State

Notify listeners of connection changes:

```csharp
private void OnDisconnected(string reason)
{
    ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
    {
        IsConnected = false,
        Reason = reason
    });
}
```
