using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Pty.Net;

namespace http_gateway_service.WebSockets;

/// <summary>
/// Handles a single WebSocket connection by spawning a real PTY shell inside
/// the container and bidirectionally piping data between the socket and the PTY.
///
/// Protocol:
///   Client → Server (binary)  raw keystroke bytes  →  PTY stdin
///   Client → Server (text)    {"type":"resize","cols":80,"rows":24}  →  pty.Resize()
///   Server → Client (binary)  raw PTY stdout bytes  →  xterm.js terminal.write()
/// </summary>
public static class PtyWebSocketHandler
{
    public static async Task HandleAsync(WebSocket webSocket, CancellationToken ct)
    {
        var pty = PtyProvider.Spawn(
            "/bin/bash",
            80,
            24,
            Directory.Exists("/project") ? "/project" : "/",
            BackendOptions.Default);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // PTY stdout → WebSocket via event (no ReaderStream in 0.1.16-pre)
        pty.PtyData += async (sender, data) =>
        {
            if (cts.Token.IsCancellationRequested) return;
            var bytes = Encoding.UTF8.GetBytes(data);
            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Binary,
                endOfMessage: true,
                cts.Token);
        };

        pty.PtyDisconnected += (sender) => cts.Cancel();

        // WebSocket → PTY stdin
        var buffer = new byte[4096];
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var msg = JsonSerializer.Deserialize<ResizeMessage>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (msg?.Type == "resize")
                        pty.Resize(msg.Cols, msg.Rows);
                }
                else
                {
                    var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await pty.WriteAsync(text);
                }
            }
        }
        catch (OperationCanceledException) { }

        if (webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Session ended",
                CancellationToken.None);
        }
    }

    private sealed record ResizeMessage(string Type, int Cols, int Rows);
}
