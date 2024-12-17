using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using SocialNetwork.Data;
using SocialNetwork.Models.Auth;
using SocialNetwork.Services;

namespace SocialNetwork;

public partial class Server(string ip, int port)
{
    private readonly TcpListener _listener = new(IPAddress.Parse(ip), port);

    private readonly AuthService _authService = new(JwtKey);
    private const string JwtKey = "verysecretverysecretverysecret1234";


    public async Task Start()
    {
        _listener.Start();
        Console.WriteLine("Server started.");

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClient(client));
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        var stream = client.GetStream();
        var buffer = new byte[1024];
        
        // Читаем запрос от клиента
        var bytesRead = await stream.ReadAsync(buffer);
        var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        if (request.Contains("Upgrade: websocket")) 
        {
            // Обработка WebSocket подключения
            await HandleWebSocket(client, request);
        }
        else 
        {
            // Обработка HTTP запросов
            await HandleHttpRequest(client, request);
        }
    }
    
    private async Task HandleHttpRequest(TcpClient client, string request)
    {
        var stream = client.GetStream();
        var response = string.Empty;

        if (request.StartsWith("POST /register"))
        {
            var requestBody = GetRequestBody(request);
            var user = JsonConvert.DeserializeObject<User>(requestBody);

            if (user != null && _authService.Register(user.Username, user.PasswordHash))
            {
                response = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nUser registered";
            }
            else
            {
                response = "HTTP/1.1 409 Conflict\r\nContent-Type: text/plain\r\n\r\nUsername already exists";
            }
        }
        else if (request.StartsWith("POST /login"))
        {
            var requestBody = GetRequestBody(request);
            var loginData = JsonConvert.DeserializeObject<User>(requestBody);
            if (loginData != null)
            {
                var token = _authService.Login(loginData.Username, loginData.PasswordHash);

                response = token != null
                    ? $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\n{{\"token\": \"{token}\"}}"
                    : "HTTP/1.1 401 Unauthorized\r\nContent-Type: text/plain\r\n\r\nInvalid credentials";
            }
        }
        else
        {
            response = "HTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\n\r\nPage not found";
        }

        await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
        client.Close();
    }
    
    private static string GetRequestBody(string request)
    {
        var splitRequest = request.Split("\r\n\r\n", 2);
        return splitRequest.Length > 1 ? splitRequest[1] : string.Empty;
    }
    
    private static async Task HandleWebSocket(TcpClient client, string request)
    {
        var stream = client.GetStream();
        
        // Чтение заголовка Sec-WebSocket-Key из запроса
        var key = MyRegex().Match(request).Groups[1].Value.Trim();
        var acceptKey = Convert.ToBase64String(
            System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
        
        // Отправка ответа для установления WebSocket соединения
        var response = "HTTP/1.1 101 Switching Protocols\r\n" +
                       "Connection: Upgrade\r\n" +
                       "Upgrade: websocket\r\n" +
                       "Sec-WebSocket-Accept: " + acceptKey + "\r\n\r\n";
        
        await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
        
        // Чтение и обработка WebSocket сообщений
        var buffer = new byte[1024];
        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer);
            if (bytesRead == 0) break;

            var decodedMessage = DecodeWebSocketMessage(buffer);
            Console.WriteLine("Received message: " + decodedMessage);

            var responseMessage = EncodeWebSocketMessage("Server: " + decodedMessage);
            await stream.WriteAsync(responseMessage);
        }

        client.Close();
    }

    private static string DecodeWebSocketMessage(byte[] buffer)
    {
        var dataStart = 2;
        var dataLength = buffer[1] & 0x7F;

        dataStart = dataLength switch
        {
            126 => 4,
            127 => 10,
            _ => dataStart
        };

        var key = new byte[4];
        Array.Copy(buffer, dataStart - 4, key, 0, 4);

        var decodedMessage = new byte[dataLength];
        for (var i = 0; i < dataLength; i++)
        {
            decodedMessage[i] = (byte)(buffer[i + dataStart] ^ key[i % 4]);
        }

        return Encoding.UTF8.GetString(decodedMessage);
    }

    private static byte[] EncodeWebSocketMessage(string message)
    {
        var bytesRaw = Encoding.UTF8.GetBytes(message);
        var frame = new byte[10];

        int indexStartRawData;
        var length = bytesRaw.Length;

        frame[0] = 129;
        switch (length)
        {
            case <= 125:
                frame[1] = (byte)length;
                indexStartRawData = 2;
                break;
            case <= 65535:
                frame[1] = 126;
                frame[2] = (byte)((length >> 8) & 255);
                frame[3] = (byte)(length & 255);
                indexStartRawData = 4;
                break;
            default:
                frame[1] = 127;
                frame[2] = (byte)((length >> 56) & 255);
                frame[3] = (byte)((length >> 48) & 255);
                frame[4] = (byte)((length >> 40) & 255);
                frame[5] = (byte)((length >> 32) & 255);
                frame[6] = (byte)((length >> 24) & 255);
                frame[7] = (byte)((length >> 16) & 255);
                frame[8] = (byte)((length >> 8) & 255);
                frame[9] = (byte)(length & 255);

                indexStartRawData = 10;
                break;
        }

        var response = new byte[indexStartRawData + length];
        int i, responseIdx = 0;

        for (i = 0; i < indexStartRawData; i++)
        {
            response[responseIdx] = frame[i];
            responseIdx++;
        }

        for (i = 0; i < length; i++)
        {
            response[responseIdx] = bytesRaw[i];
            responseIdx++;
        }

        return response;
    }

    [System.Text.RegularExpressions.GeneratedRegex("Sec-WebSocket-Key: (.*)")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}