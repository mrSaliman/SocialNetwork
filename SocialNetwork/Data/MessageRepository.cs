namespace SocialNetwork.Data;

using System.Data.SQLite;
using Models;

public class MessageRepository(string connectionString)
{
    public bool SendMessage(Message message)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = "INSERT INTO Messages (SenderId, ReceiverId, Content, Timestamp) VALUES (@senderId, @receiverId, @content, @timestamp)";
        command.Parameters.AddWithValue("@senderId", message.SenderId);
        command.Parameters.AddWithValue("@receiverId", message.ReceiverId);
        command.Parameters.AddWithValue("@content", message.Content);
        command.Parameters.AddWithValue("@timestamp", message.Timestamp);

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
}