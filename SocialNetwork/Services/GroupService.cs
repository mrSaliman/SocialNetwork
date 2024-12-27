namespace SocialNetwork.Services;

using SocialNetwork.Data;
using SocialNetwork.Models;

public class GroupService(string connectionString)
{
    private readonly GroupRepository _groupRepository = new(connectionString);

    public bool CreateGroup(string name, int creatorId)
    {
        var group = new Group { Name = name };
        var created = _groupRepository.CreateGroup(group);
        if (created)
        {
            AddMember(name, creatorId);
        }

        return created;
    }

    public bool AddMember(string groupName, int userId)
    {
        var groupId = _groupRepository.GetGroupIdByName(groupName);
        if (!groupId.HasValue) return false;
        var member = new GroupMember { GroupId = groupId.Value, UserId = userId };
        return _groupRepository.AddMember(member);
    }
}