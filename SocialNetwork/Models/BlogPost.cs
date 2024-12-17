namespace SocialNetwork.Models;

public class BlogPost
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}