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

        // Проверяем, не заблокированы ли пользователи друг у друга
        using (var checkCommand = new SQLiteCommand(connection))
        {
            checkCommand.CommandText = "SELECT COUNT(*) FROM BlockedUsers WHERE (UserId = @userId AND BlockedUserId = @friendId) OR (UserId = @friendId AND BlockedUserId = @userId)";
            checkCommand.Parameters.AddWithValue("@userId", userId);
            checkCommand.Parameters.AddWithValue("@friendId", friendId);

            var isBlocked = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
            if (isBlocked) return false;
        }

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

    public bool RemoveFriend(int userId, int friendId)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = "DELETE FROM Friends WHERE (UserId = @userId AND FriendId = @friendId) OR (UserId = @friendId AND FriendId = @userId)";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@friendId", friendId);

        return command.ExecuteNonQuery() > 0;
    }

    public bool BlockUser(int userId, int blockedUserId)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        // Удаляем дружеские связи
        using (var removeFriendsCommand = new SQLiteCommand(connection))
        {
            removeFriendsCommand.CommandText = "DELETE FROM Friends WHERE (UserId = @userId AND FriendId = @blockedUserId) OR (UserId = @blockedUserId AND FriendId = @userId)";
            removeFriendsCommand.Parameters.AddWithValue("@userId", userId);
            removeFriendsCommand.Parameters.AddWithValue("@blockedUserId", blockedUserId);
            removeFriendsCommand.ExecuteNonQuery();
        }

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

    public bool UnblockUser(int userId, int blockedUserId)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = "DELETE FROM BlockedUsers WHERE UserId = @userId AND BlockedUserId = @blockedUserId";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@blockedUserId", blockedUserId);

        return command.ExecuteNonQuery() > 0;
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
    
    public List<User> GetGroupFriends(int userId, int groupId)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = @"
        SELECT u.Id, u.Username 
        FROM Users u
        INNER JOIN Friends f ON u.Id = f.FriendId
        WHERE f.UserId = @userId
          AND u.Id NOT IN (
              SELECT gm.UserId
              FROM GroupMembers gm
              WHERE gm.GroupId = @groupId
          )";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@groupId", groupId);

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

    public List<User> GetNotFriends(int userId)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = @"SELECT u.Id, u.Username FROM Users u
                                WHERE u.Id != @userId
                                  AND u.Id NOT IN (SELECT FriendId FROM Friends WHERE UserId = @userId)
                                  AND u.Id NOT IN (SELECT BlockedUserId FROM BlockedUsers WHERE UserId = @userId)
                                  AND u.Id NOT IN (SELECT UserId FROM BlockedUsers WHERE BlockedUserId = @userId)";
        command.Parameters.AddWithValue("@userId", userId);

        var notFriends = new List<User>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            notFriends.Add(new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1)
            });
        }

        return notFriends;
    }
}
