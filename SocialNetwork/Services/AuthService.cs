using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SocialNetwork.Data;
using SocialNetwork.Models.Auth;

namespace SocialNetwork.Services;

public class AuthService(string jwtKey)
{
    private readonly AuthRepository _authRepository = new(@"Data Source=D:\Univ\COURSACHS\NAP\SocialNetwork\SocialNetwork\DB\Auth.db");

    public bool Register(string username, string password)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User { Username = username, PasswordHash = passwordHash };
        return _authRepository.AddUser(user);
    }

    public string? Login(string username, string password)
    {
        var user = _authRepository.GetUserByUsername(username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return GenerateJwtToken(user.Username);
    }

    private string? GenerateJwtToken(string username)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtKey);
        
        if (key.Length < 32) 
        {
            throw new ArgumentException("JWT key must be at least 32 characters long.");
        }
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.Name, username)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}