using FunctionalitiesWebAPI.Data;
using FunctionalitiesWebAPI.Helper;
using FunctionalitiesWebAPI.Models;
using FunctionalitiesWebAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FunctionalitiesWebAPI.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtHelper _jwt;

    public AuthService(ApplicationDbContext context, JwtHelper jwt)
    {
        _context = context;
        _jwt = jwt;
    }

    public async Task<AuthResponse> Register(RegisterRequest model)
    {
        var user = new ApplicationUser
        {
            Name = model.Name,
            Email = model.Email,
            Role = "User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var accessToken = _jwt.GenerateToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    public async Task<AuthResponse> Login(LoginRequest model, CancellationToken token)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == model.Email, token);

        if (user == null ||
            !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            throw new Exception("Invalid credentials");
        }

        var accessToken = _jwt.GenerateToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync(token);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    public async Task<AuthResponse> RefreshToken(string refreshToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new Exception("Invalid refresh token");

        var newAccessToken = _jwt.GenerateToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };
    }

    public async Task<string> ForgotPassword(string email)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user == null)
            throw new Exception("User not found");

        user.ResetToken = Guid.NewGuid().ToString();
        user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);

        await _context.SaveChangesAsync();

        return user.ResetToken;
    }

    public async Task ResetPassword(string token, string newPassword)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.ResetToken == token);

        if (user == null || user.ResetTokenExpiry < DateTime.UtcNow)
            throw new Exception("Invalid token");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;

        await _context.SaveChangesAsync();
    }

    public async Task ChangePassword(int userId, string oldPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId) 
            ?? throw new Exception("User Not found");

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            throw new Exception("Old password incorrect");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        await _context.SaveChangesAsync();
    }

    public async Task Logout(string refreshToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);

        if (user == null)
            return;

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;

        await _context.SaveChangesAsync();
    }

    private string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}