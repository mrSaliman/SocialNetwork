namespace SocialNetwork.Models;

public class FriendRequest
{
    public int FriendId { get; set; }
}

public class GroupRequest
{
    public string Name { get; set; }
}

public class GroupJoinRequest
{
    public int GroupId { get; set; }
    public int FriendId { get; set; }
}

public class WebSocketMessage
{
    public string Type { get; set; }
    public int SenderId { get; set; }
    public string GroupName { get; set; }
    public string Content { get; set; }
}
