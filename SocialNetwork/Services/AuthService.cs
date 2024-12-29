using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SocialNetwork.Data;
using SocialNetwork.Models;

namespace SocialNetwork.Services;

public class AuthService(string jwtKey, string connectionString)
{
    private readonly AuthRepository _authRepository = new(connectionString);

    public bool Register(string username, string password)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        if (_authRepository.GetUserByUsername(username) != null) return false;
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

        var token = GenerateJwtToken(user.Id);
        return token;
    }

    private string? GenerateJwtToken(int id)
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
                new("id", id.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public User? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtKey);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false, 
                ValidateAudience = false, 
                ClockSkew = TimeSpan.Zero 
            }, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            var idClaim = principal.FindFirst("id");
            return idClaim == null ? null : _authRepository.GetUserById(int.Parse(idClaim.Value));
        }
        catch
        {
            return null;
        }
    }
}