using BotFatura.Application.Common.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BotFatura.Api.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthService(IConfiguration configuration, IDateTimeProvider dateTimeProvider)
    {
        _configuration = configuration;
        _dateTimeProvider = dateTimeProvider;
    }

    public string? Authenticate(string email, string password)
    {
        var adminEmail = _configuration["DefaultAdmin:Email"];
        var adminPassword = _configuration["DefaultAdmin:Password"];

        if (email != adminEmail || password != adminPassword)
            return null;

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = Encoding.ASCII.GetBytes(jwtSettings["Secret"]!);
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, "Admin")
            }),
            Expires = _dateTimeProvider.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryInMinutes"] ?? "1440")),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
