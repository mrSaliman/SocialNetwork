namespace SocialNetwork.Services;

using Data;
using Models;

public class UserService(string connectionString)
{
    private readonly UserRepository _userRepository = new(connectionString);

    public User? GetUserById(int id)
    {
        return _userRepository.GetUserById(id);
    }

    public bool AddFriend(int userId, int friendId)
    {
        var existingFriends = GetFriends(userId);
        return existingFriends.All(u => u.Id != friendId) && _userRepository.AddFriend(userId, friendId);
    }

    public bool BlockUser(int userId, int blockedUserId)
    {
        var existingBlockedUsers = GetBlockedUsers(userId);
        return existingBlockedUsers.All(u => u.Id != blockedUserId) && _userRepository.BlockUser(userId, blockedUserId);
    }
    
    public List<User> GetFriends(int userId) =>
        _userRepository.GetFriends(userId);

    public List<User> GetBlockedUsers(int userId) =>
        _userRepository.GetBlockedUsers(userId);
}