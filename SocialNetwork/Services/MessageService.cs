using SocialNetwork.Data;
using SocialNetwork.Models;

namespace SocialNetwork.Services;

public class MessageService(string connectionString)
{
    private readonly MessageRepository _messageRepository = new(connectionString);

    public int SendMessage(Message message)
    {
        return _messageRepository.SendMessage(message);
    }

    public List<MessageResponse> GetMessages(int receiverId)
    {
        return _messageRepository.GetMessages(receiverId);
    }
}