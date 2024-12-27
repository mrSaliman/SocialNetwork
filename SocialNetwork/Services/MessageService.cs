namespace SocialNetwork.Services;

using SocialNetwork.Data;
using SocialNetwork.Models;

public class MessageService(string connectionString)
{
    private readonly MessageRepository _messageRepository = new(connectionString);

    public bool SendMessage(int senderId, int receiverId, string content)
    {
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            Timestamp = DateTime.UtcNow
        };

        return _messageRepository.SendMessage(message);
    }
}