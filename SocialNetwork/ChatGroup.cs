using System.Net;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using SocialNetwork.Models;
using SocialNetwork.Services;

namespace SocialNetwork;

public class ChatGroup(Group group, MessageService messageService)
{
    private Group Group { get; } = group;
    private readonly Dictionary<WebSocket, User> _clients = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task AddClientAsync(HttpListenerContext context, User user)
    {
        WebSocket? webSocket = null;

        try
        {
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            webSocket = webSocketContext.WebSocket;

            await _lock.WaitAsync();
            _clients.Add(webSocket, user);
            Console.WriteLine($"Client '{user.Username}' added to group '{Group.Name}'.");

            _lock.Release();

            await ReceiveMessagesAsync(webSocket);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error with client '{user.Username}' in group '{Group.Name}': {ex.Message}");
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

                    var request = JsonConvert.DeserializeObject<MessageRequest>(message);
                    if (request is not { Type: "Message" })
                    {
                        continue;
                    }
                    
                    if (_clients.TryGetValue(webSocket, out var user))
                    {
                        Console.WriteLine($"Received message from '{user.Username}' in group '{Group.Name}': {request.Content}");
                        await BroadcastMessageAsync(user, request.Content, webSocket);
                    }
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

    private async Task BroadcastMessageAsync(User sender, string message, WebSocket senderSocket)
    {
        var dbMessageModel = new Message
        {
            SenderId = sender.Id,
            ReceiverId = Group.Id,
            Content = message,
            Timestamp = DateTime.UtcNow
        };

        await _lock.WaitAsync();
        try
        {
            var id = messageService.SendMessage(dbMessageModel);
            
            var messageModel = new MessageResponse
            {
                Id = id,
                Sender = sender,
                ReceiverId = Group.Id,
                Content = message,
                Timestamp = DateTime.UtcNow
            };
            
            message = JsonConvert.SerializeObject(messageModel);
        
            var messageBuffer = Encoding.UTF8.GetBytes(message);
            
            foreach (var client in _clients.Keys.Where(client => client != senderSocket && client.State == WebSocketState.Open))
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
            if (_clients.Remove(webSocket, out var user))
            {
                Console.WriteLine($"Client '{user.Username}' removed from group '{Group.Name}'.");
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
