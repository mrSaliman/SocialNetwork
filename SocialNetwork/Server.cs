using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SocialNetwork.Models;
using SocialNetwork.Services;

namespace SocialNetwork;

public partial class Server
{
    private readonly HttpListener _listener;
    private readonly AuthService _authService;
    private readonly BlogService _blogService;
    private readonly GroupService _groupService;
    private readonly UserService _userService;

    public Server(string prefix)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(@"D:\Univ\COURSACHS\NAP\SocialNetwork\SocialNetwork\appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        _authService = new AuthService(JwtKey, connectionString!);
        _blogService = new BlogService(connectionString!);
        _groupService = new GroupService(connectionString!);
        _userService = new UserService(connectionString!);

        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
    }

    private const string JwtKey = "verysecretverysecretverysecret1234";

    public async Task Start()
    {
        _listener.Start();
        Console.WriteLine("Server started.");

        while (true)
        {
            var context = await _listener.GetContextAsync();
            _ = Task.Run(() => HandleRequest(context));
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            switch (request.HttpMethod)
            {
                case "POST" when request.Url!.AbsolutePath == "/register":
                    await HandleRegister(request, response);
                    break;
                case "POST" when request.Url.AbsolutePath == "/login":
                    await HandleLogin(request, response);
                    break;
                default:
                {
                    if (IsRequestAuthorized(request, out var user))
                    {
                        await HandleAuthorizedRequest(request, response, user!);
                    }
                    else
                    {
                        await WriteResponseAsync(response, HttpStatusCode.Unauthorized, "Invalid token.");
                    }

                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            await WriteResponseAsync(context.Response, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private async Task HandleRegister(HttpListenerRequest request, HttpListenerResponse response)
    {
        var user = DeserializeRequestBody<User>(request);
        if (user == null || !_authService.Register(user.Username, user.PasswordHash))
        {
            await WriteResponseAsync(response, HttpStatusCode.Conflict, "Username already exists");
            return;
        }

        await WriteResponseAsync(response, HttpStatusCode.OK, "User registered");
    }

    private async Task HandleLogin(HttpListenerRequest request, HttpListenerResponse response)
    {
        var loginData = DeserializeRequestBody<User>(request);
        if (loginData == null)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid request body");
            return;
        }

        var token = _authService.Login(loginData.Username, loginData.PasswordHash);
        if (token != null)
        {
            await WriteResponseAsync(response, HttpStatusCode.OK, JsonConvert.SerializeObject(new { token }));
        }
        else
        {
            await WriteResponseAsync(response, HttpStatusCode.Unauthorized, "Invalid credentials");
        }
    }

    private async Task HandleAuthorizedRequest(HttpListenerRequest request, HttpListenerResponse response, User user)
    {
        switch (request.HttpMethod)
        {
            case "POST" when request.Url!.AbsolutePath == "/create-group":
                await HandleCreateGroup(request, response, user);
                break;
            case "POST" when request.Url.AbsolutePath == "/join-group":
                await HandleJoinGroup(request, response, user);
                break;
            case "POST" when request.Url.AbsolutePath == "/add-friend":
                await HandleAddFriend(request, response, user);
                break;
            case "POST" when request.Url.AbsolutePath == "/block-user":
                await HandleBlockUser(request, response, user);
                break;
            case "GET" when request.Url!.AbsolutePath.StartsWith("/friends/"):
                await HandleGetFriends(request, response);
                break;
            case "GET" when request.Url.AbsolutePath.StartsWith("/blocked/"):
                await HandleGetBlockedUsers(request, response);
                break;
            case "POST" when request.Url.AbsolutePath == "/create-blog":
                await HandleCreateBlog(request, response, user);
                break;
            case "GET" when request.Url.AbsolutePath.StartsWith("/blogs/"):
                await HandleGetBlogs(request, response);
                break;
            default:
                await WriteResponseAsync(response, HttpStatusCode.NotFound, "Route not found");
                break;
        }
    }

    private async Task HandleCreateGroup(HttpListenerRequest request, HttpListenerResponse response, User user)
    {
        var groupData = DeserializeRequestBody<GroupRequest>(request);
        if (groupData == null)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid request body");
            return;
        }

        _groupService.CreateGroup(groupData.Name, user.Id);
        await WriteResponseAsync(response, HttpStatusCode.OK, "Group created");
    }

    private async Task HandleJoinGroup(HttpListenerRequest request, HttpListenerResponse response, User user)
    {
        var joinData = DeserializeRequestBody<GroupJoinRequest>(request);
        if (joinData == null)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid request body");
            return;
        }

        var result = _groupService.AddMember(joinData.GroupName, user.Id);
        await WriteResponseAsync(response, result ? HttpStatusCode.OK : HttpStatusCode.BadRequest, result ? "Joined group" : "Could not join group");
    }

    private async Task HandleAddFriend(HttpListenerRequest request, HttpListenerResponse response, User user)
    {
        var data = DeserializeRequestBody<FriendRequest>(request);
        if (data == null)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid request body");
            return;
        }

        var result = _userService.AddFriend(user.Id, data.FriendId);
        await WriteResponseAsync(response, result ? HttpStatusCode.OK : HttpStatusCode.BadRequest, result ? "Friend added successfully" : "Friend already added");
    }

    private async Task HandleBlockUser(HttpListenerRequest request, HttpListenerResponse response, User user)
    {
        var data = DeserializeRequestBody<FriendRequest>(request);
        if (data == null)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid request body");
            return;
        }

        var result = _userService.BlockUser(user.Id, data.FriendId);
        await WriteResponseAsync(response, result ? HttpStatusCode.OK : HttpStatusCode.BadRequest, result ? "User blocked successfully" : "User already blocked");
    }

    private async Task HandleGetFriends(HttpListenerRequest request, HttpListenerResponse response)
    {
        var userId = GetUserIdFromRequest(request);
        if (!userId.HasValue)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid user ID.");
            return;
        }

        var friends = _userService.GetFriends(userId.Value);
        await WriteResponseAsync(response, HttpStatusCode.OK, JsonConvert.SerializeObject(friends));
    }

    private async Task HandleGetBlockedUsers(HttpListenerRequest request, HttpListenerResponse response)
    {
        var userId = GetUserIdFromRequest(request);
        if (!userId.HasValue)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid user ID.");
            return;
        }

        var blockedUsers = _userService.GetBlockedUsers(userId.Value);
        await WriteResponseAsync(response, HttpStatusCode.OK, JsonConvert.SerializeObject(blockedUsers));
    }

    private async Task HandleCreateBlog(HttpListenerRequest request, HttpListenerResponse response, User user)
    {
        var blogPost = DeserializeRequestBody<BlogPost>(request);
        if (blogPost == null)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid request body");
            return;
        }

        _blogService.CreatePost(user.Id, blogPost.Content);
        await WriteResponseAsync(response, HttpStatusCode.OK, "Blog post created");
    }

    private async Task HandleGetBlogs(HttpListenerRequest request, HttpListenerResponse response)
    {
        var userId = GetUserIdFromRequest(request);
        if (!userId.HasValue)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid user ID.");
            return;
        }

        var posts = _blogService.GetAllPosts(userId.Value);
        await WriteResponseAsync(response, HttpStatusCode.OK, JsonConvert.SerializeObject(posts));
    }

    private bool IsRequestAuthorized(HttpListenerRequest request, out User? userId)
    {
        userId = null;
        var tokenHeader = request.Headers["Authorization"];
        if (string.IsNullOrEmpty(tokenHeader) || !tokenHeader.StartsWith("Bearer ")) return false;

        var token = tokenHeader.Substring("Bearer ".Length).Trim();
        userId = _authService.ValidateToken(token);
        return userId != null;
    }

    private static int? GetUserIdFromRequest(HttpListenerRequest request)
    {
        var match = GetIdRegex().Match(request.Url!.AbsolutePath);
        if (match.Success)
        {
            return int.Parse(match.Groups[2].Value);
        }

        return null;
    }

    private static T? DeserializeRequestBody<T>(HttpListenerRequest request) where T : class
    {
        try
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<T>(body);
        }
        catch
        {
            return null;
        }
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, HttpStatusCode statusCode, string responseBody)
    {
        response.StatusCode = (int)statusCode;
        response.ContentType = "application/json";
        var responseBytes = Encoding.UTF8.GetBytes(responseBody);
        response.ContentLength64 = responseBytes.Length;

        await response.OutputStream.WriteAsync(responseBytes);
        response.Close();
    }

    [GeneratedRegex(@"^/(friends|blocked|blogs)/(\d+)$")]
    private static partial Regex GetIdRegex();
}
