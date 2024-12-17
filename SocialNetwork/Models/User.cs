namespace SocialNetwork.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<int> Friends { get; set; } = new();
    public List<int> BlockedUsers { get; set; } = new();
}