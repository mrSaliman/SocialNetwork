using SocialNetwork.Models;
using SocialNetwork.Data;

namespace SocialNetwork.Services;

public class GroupService(string connectionString)
{
    private readonly GroupRepository _groupRepository = new(connectionString);

    public int CreateGroup(string name, int creatorId)
    {
        var group = new Group { Name = name };
        var createdId = _groupRepository.CreateGroup(group);
        if (createdId != -1)
        {
            AddMember(createdId, creatorId);
        }

        return createdId;
    }

    public bool AddMember(int groupId, int userId)
    {
        var member = new GroupMember { GroupId = groupId, UserId = userId };
        return _groupRepository.AddMember(member);
    }
    
    public List<Group> GetGroups(int userId) =>
        _groupRepository.GetGroups(userId);
    
    public Group? GetGroup(int groupId) =>
        _groupRepository.GetGroup(groupId);
}