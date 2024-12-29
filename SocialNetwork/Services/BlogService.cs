using SocialNetwork.Data;
using SocialNetwork.Models;

namespace SocialNetwork.Services;

public class BlogService(string connectionString)
{
    private readonly BlogRepository _blogRepository = new(connectionString);

    public bool CreatePost(int authorId, string content)
    {
        var post = new BlogPost
        {
            AuthorId = authorId,
            Content = content,
            Timestamp = DateTime.UtcNow
        };

        return _blogRepository.CreatePost(post);
    }
    
    public List<BlogPost> GetAllPosts(int userId) => _blogRepository.GetPosts(userId);
}