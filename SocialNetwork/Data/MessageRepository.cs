namespace SocialNetwork.Data;

using System.Data.SQLite;
using Models;
using System.Collections.Generic;

public class MessageRepository(string connectionString)
{
    public int SendMessage(Message message)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = @"
        INSERT INTO Messages (SenderId, ReceiverId, Content, Timestamp)
        VALUES (@senderId, @receiverId, @content, @timestamp);
        SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@senderId", message.SenderId);
        command.Parameters.AddWithValue("@receiverId", message.ReceiverId);
        command.Parameters.AddWithValue("@content", message.Content);
        command.Parameters.AddWithValue("@timestamp", message.Timestamp);

        try
        {
            var result = command.ExecuteScalar();
            return (int)(result != null ? Convert.ToInt64(result) : -1);
        }
        catch (SQLiteException)
        {
            return -1; // Возвращаем -1, чтобы указать, что произошла ошибка.
        }
    }


    public List<MessageResponse> GetMessages(int receiverId)
    {
        var messages = new List<MessageResponse>();

        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = @"
        SELECT m.Id, m.SenderId, m.ReceiverId, m.Content, m.Timestamp, u.Id, u.Username
        FROM Messages m
        JOIN Users u ON m.SenderId = u.Id
        WHERE m.ReceiverId = @receiverId";
        command.Parameters.AddWithValue("@receiverId", receiverId);

        try
        {
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var messageResponse = new MessageResponse
                {
                    Id = reader.GetInt32(0),
                    Sender = new User
                    {
                        Id = reader.GetInt32(5),
                        Username = reader.GetString(6)
                    },
                    ReceiverId = reader.GetInt32(2),
                    Content = reader.GetString(3),
                    Timestamp = reader.GetDateTime(4)
                };
                messages.Add(messageResponse);
            }
        }
        catch (SQLiteException ex)
        {
            Console.WriteLine($"Database error: {ex.Message}");
        }

        return messages;
    }
}
