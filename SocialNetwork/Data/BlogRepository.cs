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
    
    public List<BlogPost> GetPosts(int userId)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        var command = new SQLiteCommand("SELECT AuthorId, Content, Timestamp FROM BlogPosts WHERE AuthorId = @userId;", connection);
    
        command.Parameters.AddWithValue("@userId", userId);

        var reader = command.ExecuteReader();

        var posts = new List<BlogPost>();
        while (reader.Read())
        {
            posts.Add(new BlogPost
            {
                AuthorId = reader.GetInt32(0),
                Content = reader.GetString(1),
                Timestamp = reader.GetDateTime(2)
            });
        }

        return posts;
    }

}