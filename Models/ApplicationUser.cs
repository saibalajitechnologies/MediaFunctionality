namespace FunctionalitiesWebAPI.Models;

#nullable disable
public class ApplicationUser
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Email { get; set; }

    public string PasswordHash { get; set; }

    public string Role { get; set; }

    public string ResetToken { get; set; }

    public DateTime? ResetTokenExpiry { get; set; }

    public string RefreshToken { get; set; }

    public bool EmailVerified { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }
}
