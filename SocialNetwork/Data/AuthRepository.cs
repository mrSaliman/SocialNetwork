using System.Data.SQLite;
using SocialNetwork.Models.Auth;

namespace SocialNetwork.Data;

public class AuthRepository(string connectionString)
{
    public bool AddUser(User user)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = "INSERT INTO Users (Username, PasswordHash) VALUES (@username, @passwordHash)";
        command.Parameters.AddWithValue("@username", user.Username);
        command.Parameters.AddWithValue("@passwordHash", user.PasswordHash);

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

    public User? GetUserByUsername(string username)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = "SELECT * FROM Users WHERE Username = @username";
        command.Parameters.AddWithValue("@username", username);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2)
            };
        }

        return null;
    }
}