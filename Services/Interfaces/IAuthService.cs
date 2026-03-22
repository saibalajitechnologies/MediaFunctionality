using FunctionalitiesWebAPI.Models;

namespace FunctionalitiesWebAPI.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> Register(RegisterRequest model);

    Task<AuthResponse> Login(LoginRequest model, CancellationToken token);

    Task<AuthResponse> RefreshToken(string refreshToken);

    Task<string> ForgotPassword(string email);

    Task ResetPassword(string token, string newPassword);

    Task ChangePassword(int userId, string oldPassword, string newPassword);

    Task Logout(string refreshToken);
}
