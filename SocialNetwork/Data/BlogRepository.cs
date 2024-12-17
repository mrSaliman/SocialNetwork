namespace SocialNetwork.Data;
using System.Data.SQLite;
using Models;

public class BlogRepository(string connectionString)
{
    public bool CreatePost(BlogPost post)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = "INSERT INTO BlogPosts (AuthorId, Content, Timestamp) VALUES (@authorId, @content, @timestamp)";
        command.Parameters.AddWithValue("@authorId", post.AuthorId);
        command.Parameters.AddWithValue("@content", post.Content);
        command.Parameters.AddWithValue("@timestamp", post.Timestamp);

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