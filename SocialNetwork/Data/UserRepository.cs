namespace SocialNetwork.Data;
using System.Data.SQLite;
using Models;

public class UserRepository(string connectionString)
{
    public User? GetUserById(int id)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = "SELECT * FROM Users WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1)
            };
        }

        return null;
    }

    public bool AddFriend(int userId, int friendId)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = "INSERT INTO Friends (UserId, FriendId) VALUES (@userId, @friendId)";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@friendId", friendId);

        try
        {
            command.ExecuteNonQuery();
            return true;
        }
        catch (SQLiteException)
        {
            return false;
        }
    }

    public bool BlockUser(int userId, int blockedUserId)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = "INSERT INTO BlockedUsers (UserId, BlockedUserId) VALUES (@userId, @blockedUserId)";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@blockedUserId", blockedUserId);

        try
        {
            command.ExecuteNonQuery();
            return true;
        }
        catch (SQLiteException)
        {
            return false;
        }
    }
    
    public List<User> GetFriends(int userId)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = @"SELECT u.Id, u.Username FROM Users u 
                                INNER JOIN Friends f ON u.Id = f.FriendId 
                                WHERE f.UserId = @userId";
        command.Parameters.AddWithValue("@userId", userId);

        var friends = new List<User>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            friends.Add(new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1)
            });
        }

        return friends;
    }

    public List<User> GetBlockedUsers(int userId)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = @"SELECT u.Id, u.Username FROM Users u 
                                INNER JOIN BlockedUsers b ON u.Id = b.BlockedUserId 
                                WHERE b.UserId = @userId";
        command.Parameters.AddWithValue("@userId", userId);

        var blockedUsers = new List<User>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            blockedUsers.Add(new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1)
            });
        }

        return blockedUsers;
    }
}
