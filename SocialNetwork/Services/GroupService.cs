namespace SocialNetwork.Services;

using SocialNetwork.Data;
using SocialNetwork.Models;

public class GroupService
{
    private readonly GroupRepository _groupRepository = new(@"Data Source=D:\Univ\COURSACHS\NAP\SocialNetwork\SocialNetwork\DB\SocialNetwork.db");

    public bool CreateGroup(string name)
    {
        var group = new Group { Name = name };
        return _groupRepository.CreateGroup(group);
    }

    public bool AddMember(int groupId, int userId)
    {
        var member = new GroupMember { GroupId = groupId, UserId = userId };
        return _groupRepository.AddMember(member);
    }
}