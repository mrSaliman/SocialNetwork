using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SocialNetwork.Models;
using SocialNetwork.Services;
using Group = SocialNetwork.Models.Group;

namespace SocialNetwork;

public partial class Server
{
    private readonly HttpListener _listener;
    private readonly AuthService _authService;
    private readonly BlogService _blogService;
    private readonly GroupService _groupService;
    private readonly UserService _userService;
    private readonly MessageService _messageService;
    private readonly ConcurrentDictionary<int, ChatGroup> _chatGroups = [];

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
        _messageService = new MessageService(connectionString!);

        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
    }

    private const string JwtKey = "verysecretverysecretverysecret1234";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _listener.Start();
        Console.WriteLine("Server started.");

        while (!cancellationToken.IsCancellationRequested)
        {
            var context = await _listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                var type = context.Request.QueryString["Type"];
                if (string.IsNullOrEmpty(type) || type != "Join")
                {
                    continue;
                }
                
                var tokenString = context.Request.QueryString["Token"];
                if (string.IsNullOrEmpty(tokenString) || !IsTokenAuthorized(tokenString, out var user))
                {
                    await WriteResponseAsync(context.Response, HttpStatusCode.Unauthorized, "Invalid token.");
                    continue;
                }
                
                var groupIdString = context.Request.QueryString["GroupId"];
                if (string.IsNullOrEmpty(groupIdString))
                {
                    context.Response.StatusCode = 400;
                    context.Response.StatusDescription = "GroupId query parameter is required.";
                    context.Response.Close();
                    continue;
                }
                var group = _groupService.GetGroup(int.Parse(groupIdString));
                if (group == null)
                {
                    context.Response.StatusCode = 400;
                    context.Response.StatusDescription = "There are no groups with such id.";
                    context.Response.Close();
                    continue;
                }

                var chatGroup = _chatGroups.GetOrAdd(group.Id, CreateChatGroup(group));

                _ = chatGroup.AddClientAsync(context, user!);
            }
            else
            {
                _ = Task.Run(() => HandleRequest(context), cancellationToken);
            }
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;
            
            AddCorsHeaders(response);

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                response.Close();
                return;
            }

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
                await HandleJoinGroup(request, response);
                break;
            case "GET" when request.Url!.AbsolutePath == "/groups":
                await HandleGetGroups(response, user);
                break;
            case "GET" when request.Url!.AbsolutePath.StartsWith("/group/"):
                await HandleGetGroup(request, response);
                break;
            case "GET" when request.Url!.AbsolutePath.StartsWith("/messages/"):
                await HandleGetMessages(request, response);
                break;
            case "POST" when request.Url.AbsolutePath == "/add-friend":
                await HandleAddFriend(request, response, user);
                break;
            case "POST" when request.Url.AbsolutePath == "/remove-friend":
                await HandleRemoveFriend(request, response, user);
                break;
            case "POST" when request.Url.AbsolutePath == "/block-user":
                await HandleBlockUser(request, response, user);
                break;
            case "POST" when request.Url.AbsolutePath == "/unblock-user":
                await HandleUnblockUser(request, response, user);
                break;
            case "GET" when request.Url!.AbsolutePath.StartsWith("/friends/"):
                await HandleGetFriends(request, response);
                break;
            case "GET" when request.Url!.AbsolutePath.StartsWith("/group-friends/"):
                await HandleGetGroupFriends(request, response, user);
                break;
            case "GET" when request.Url!.AbsolutePath.StartsWith("/not-friends/"):
                await HandleGetNotFriends(request, response);
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
            case "GET" when request.Url.AbsolutePath.StartsWith("/user/"):
                await HandleGetUser(request, response);
                break;
            default:
                await WriteResponseAsync(response, HttpStatusCode.NotFound, "Route not found");
                break;
        }
    }
    
    private static void AddCorsHeaders(HttpListenerResponse response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "*"); 
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS"); 
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, ngrok-skip-browser-warning"); 
    }

    private async Task HandleCreateGroup(HttpListenerRequest request, HttpListenerResponse response, User user)
    {
        var groupData = DeserializeRequestBody<GroupRequest>(request);
        if (groupData == null)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid request body");
            return;
        }

        var groupId = _groupService.CreateGroup(groupData.Name, user.Id);
        await WriteResponseAsync(response, HttpStatusCode.OK, JsonConvert.SerializeObject(groupId));
    }

    private async Task HandleJoinGroup(HttpListenerRequest request, HttpListenerResponse response)
    {
        var joinData = DeserializeRequestBody<GroupJoinRequest>(request);
        if (joinData == null)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid request body");
            return;
        }

        var result = _groupService.AddMember(joinData.GroupId, joinData.FriendId);
        await WriteResponseAsync(response, result ? HttpStatusCode.OK : HttpStatusCode.BadRequest, result ? "Joined group" : "Could not join group");
    }
    
    private async Task HandleGetGroups(HttpListenerResponse response, User user)
    {
        var result = _groupService.GetGroups(user.Id);
        await WriteResponseAsync(response, HttpStatusCode.OK, JsonConvert.SerializeObject(result));
    }
    
    private async Task HandleGetGroup(HttpListenerRequest request, HttpListenerResponse response)
    {
        var groupId = GetIdFromRequest(request);
        if (!groupId.HasValue)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid group ID.");
            return;
        }

        var result = _groupService.GetGroup(groupId.Value);
        await WriteResponseAsync(response, HttpStatusCode.OK, JsonConvert.SerializeObject(result));
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
    
    private async Task HandleGetMessages(HttpListenerRequest request, HttpListenerResponse response)
    {
        var groupId = GetIdFromRequest(request);
        if (!groupId.HasValue)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid group ID.");
            return;
        }

        var messages = _messageService.GetMessages(groupId.Value);
        await WriteResponseAsync(response, HttpStatusCode.OK, JsonConvert.SerializeObject(messages));
    }
    
    private async Task HandleRemoveFriend(HttpListenerRequest request, HttpListenerResponse response, User user)
    {
        var data = DeserializeRequestBody<FriendRequest>(request);
        if (data == null)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid request body");
            return;
        }

        var result = _userService.RemoveFriend(user.Id, data.FriendId);
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
    
    private async Task HandleUnblockUser(HttpListenerRequest request, HttpListenerResponse response, User user)
    {
        var data = DeserializeRequestBody<FriendRequest>(request);
        if (data == null)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid request body");
            return;
        }

        var result = _userService.UnblockUser(user.Id, data.FriendId);
        await WriteResponseAsync(response, result ? HttpStatusCode.OK : HttpStatusCode.BadRequest, result ? "User blocked successfully" : "User already blocked");
    }

    private async Task HandleGetFriends(HttpListenerRequest request, HttpListenerResponse response)
    {
        var userId = GetIdFromRequest(request);
        if (!userId.HasValue)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid user ID.");
            return;
        }

        var friends = _userService.GetFriends(userId.Value);
        await WriteResponseAsync(response, HttpStatusCode.OK, JsonConvert.SerializeObject(friends));
    }
    
    private async Task HandleGetGroupFriends(HttpListenerRequest request, HttpListenerResponse response, User user)
    {
        var groupId = GetIdFromRequest(request);
        if (!groupId.HasValue)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid user ID.");
            return;
        }

        var friends = _userService.GetGroupFriends(user.Id, groupId.Value);
        await WriteResponseAsync(response, HttpStatusCode.OK, JsonConvert.SerializeObject(friends));
    }
    
    private async Task HandleGetNotFriends(HttpListenerRequest request, HttpListenerResponse response)
    {
        var userId = GetIdFromRequest(request);
        if (!userId.HasValue)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid user ID.");
            return;
        }

        var friends = _userService.GetNotFriends(userId.Value);
        await WriteResponseAsync(response, HttpStatusCode.OK, JsonConvert.SerializeObject(friends));
    }

    private async Task HandleGetBlockedUsers(HttpListenerRequest request, HttpListenerResponse response)
    {
        var userId = GetIdFromRequest(request);
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
        var userId = GetIdFromRequest(request);
        if (!userId.HasValue)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid user ID.");
            return;
        }

        var posts = _blogService.GetAllPosts(userId.Value);
        await WriteResponseAsync(response, HttpStatusCode.OK, JsonConvert.SerializeObject(posts));
    }
    
    private async Task HandleGetUser(HttpListenerRequest request, HttpListenerResponse response)
    {
        var userId = GetIdFromRequest(request);
        if (!userId.HasValue)
        {
            await WriteResponseAsync(response, HttpStatusCode.BadRequest, "Invalid user ID.");
            return;
        }

        var user = _userService.GetUserById(userId.Value);
        await WriteResponseAsync(response, HttpStatusCode.OK, JsonConvert.SerializeObject(user));
    }

    private bool IsRequestAuthorized(HttpListenerRequest request, out User? userId)
    {
        userId = null;
        var tokenHeader = request.Headers["Authorization"];
        if (string.IsNullOrEmpty(tokenHeader) || !tokenHeader.StartsWith("Bearer ")) return false;

        var token = tokenHeader["Bearer ".Length..].Trim();
        userId = _authService.ValidateToken(token);
        return userId != null;
    }
    
    private bool IsTokenAuthorized(string token, out User? user)
    {
        user = _authService.ValidateToken(token);
        return user != null;
    }

    private static int? GetIdFromRequest(HttpListenerRequest request)
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

    [GeneratedRegex(@"^/(friends|blocked|blogs|user|not-friends|messages|group|group-friends)/(\d+)$")]
    private static partial Regex GetIdRegex();
    
    private ChatGroup CreateChatGroup(Group group) => new(group, _messageService);
}
