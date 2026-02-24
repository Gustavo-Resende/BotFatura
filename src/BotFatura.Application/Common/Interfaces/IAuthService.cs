namespace BotFatura.Application.Common.Interfaces;

public interface IAuthService
{
    string? Authenticate(string email, string password);
}
