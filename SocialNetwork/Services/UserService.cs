namespace SocialNetwork.Services;

using SocialNetwork.Data;
using SocialNetwork.Models;

public class UserService
{
    private readonly UserRepository _userRepository = new(@"Data Source=D:\Univ\COURSACHS\NAP\SocialNetwork\SocialNetwork\DB\SocialNetwork.db");

    public User? GetUserById(int id)
    {
        return _userRepository.GetUserById(id);
    }

    public bool AddFriend(int userId, int friendId)
    {
        return _userRepository.AddFriend(userId, friendId);
    }

    public bool BlockUser(int userId, int blockedUserId)
    {
        return _userRepository.BlockUser(userId, blockedUserId);
    }
}