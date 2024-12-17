namespace SocialNetwork.Services;

using SocialNetwork.Data;
using SocialNetwork.Models;

public class BlogService
{
    private readonly BlogRepository _blogRepository = new(@"Data Source=D:\Univ\COURSACHS\NAP\SocialNetwork\SocialNetwork\DB\SocialNetwork.db");

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
}