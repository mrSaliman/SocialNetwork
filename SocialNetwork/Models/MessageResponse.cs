namespace SocialNetwork.Models;

public class MessageResponse
{
    public int Id { get; set; }
    public User Sender { get; set; }
    public int ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}