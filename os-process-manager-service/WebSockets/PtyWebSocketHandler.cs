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
        var ptyOptions = new PtyOptions
        {
            Name = "xterm-256color",
            Rows = 24,
            Cols = 80,
            Cwd = Directory.Exists("/project") ? "/project" : "/",
            App = "/bin/bash",
            CommandLine = new[] { "/bin/bash" }
        };

        using var pty = await PtyProvider.SpawnAsync(ptyOptions, ct);

        // Linked token so either side closing shuts down both pumps cleanly
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Pump 1: PTY stdout → WebSocket  (container output → browser)
        var ptyToWs = Task.Run(async () =>
        {
            var buffer = new byte[4096];
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var bytesRead = await pty.ReaderStream.ReadAsync(buffer, cts.Token);
                    if (bytesRead == 0) break;

                    await webSocket.SendAsync(
                        new ArraySegment<byte>(buffer, 0, bytesRead),
                        WebSocketMessageType.Binary,
                        endOfMessage: true,
                        cts.Token);
                }
            }
            catch (OperationCanceledException) { }
            finally { cts.Cancel(); }
        }, cts.Token);

        // Pump 2: WebSocket → PTY stdin  (browser keystrokes → container shell)
        var wsToPty = Task.Run(async () =>
        {
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
                        // Only resize control messages arrive as text frames
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var msg = JsonSerializer.Deserialize<ResizeMessage>(json,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (msg?.Type == "resize")
                            pty.Resize(msg.Rows, msg.Cols);
                    }
                    else
                    {
                        await pty.WriterStream.WriteAsync(buffer.AsMemory(0, result.Count), cts.Token);
                        await pty.WriterStream.FlushAsync(cts.Token);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally { cts.Cancel(); }
        }, cts.Token);

        // Wait for whichever side closes first, then tear down both
        await Task.WhenAny(ptyToWs, wsToPty);

        pty.Kill();

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
