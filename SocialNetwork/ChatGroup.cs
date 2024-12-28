using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace SocialNetwork;

public class ChatGroup(string name)
{
    private string Name { get; } = name;
    private readonly List<WebSocket> _clients = [];
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task AddClientAsync(HttpListenerContext context)
    {
        WebSocket? webSocket = null;

        try
        {
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            webSocket = webSocketContext.WebSocket;

            await _lock.WaitAsync();
            _clients.Add(webSocket);
            Console.WriteLine($"Client added to group '{Name}'.");

            _lock.Release();

            await ReceiveMessagesAsync(webSocket);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding client to group '{Name}': {ex.Message}");
            webSocket?.Dispose();
        }
    }

    private async Task ReceiveMessagesAsync(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received message in group '{Name}': {message}");

                    await BroadcastMessageAsync(message, webSocket);
                    break;
                }
                case WebSocketMessageType.Close:
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                    await RemoveClientAsync(webSocket);
                    break;
                case WebSocketMessageType.Binary:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private async Task BroadcastMessageAsync(string message, WebSocket sender)
    {
        var messageBuffer = Encoding.UTF8.GetBytes(message);

        await _lock.WaitAsync();
        try
        {
            foreach (var client in _clients.Where(client => client != sender && client.State == WebSocketState.Open))
            {
                await client.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RemoveClientAsync(WebSocket webSocket)
    {
        await _lock.WaitAsync();
        try
        {
            if (_clients.Remove(webSocket))
            {
                Console.WriteLine($"Client removed from group '{Name}'.");
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}