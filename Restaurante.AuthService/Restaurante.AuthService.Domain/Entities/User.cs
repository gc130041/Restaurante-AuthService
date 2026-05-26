namespace Restaurante.AuthService.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "WAITER"; // ADMIN, WAITER, CHEF, CASHIER
    public bool IsActive { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Para mantener la relación con el perfil guardado en MongoDB (opcional ahora)
    public string? MongoId { get; set; } 

    // === Username (para login rápido sin email completo) ===
    public string? Username { get; set; }

    // === Perfil del empleado ===
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Phone { get; set; }

    // === Email Verification ===
    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }

    // === Password Reset ===
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // === Refresh Token ===
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    // === Tenant Link ===
    public string? CompanyMongoId { get; set; }
    public string? BranchMongoId { get; set; }

    // === Verification OTP ===
    public string? VerificationOtp { get; set; }
    public DateTime? VerificationOtpExpiry { get; set; }

    public void UpdateVerificationOtp(string otp, DateTime expiry)
    {
        VerificationOtp = otp;
        VerificationOtpExpiry = expiry;
    }
}