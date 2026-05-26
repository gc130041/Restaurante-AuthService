using Restaurante.AuthService.Application.DTOs;
using Restaurante.AuthService.Domain.Entities;

namespace Restaurante.AuthService.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<RegisterResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    Task<bool> ResendVerificationOtpAsync(string email);
    
    // Métodos administrativos e introspección
    Task<User?> ValidateUserAsync(Guid userId);
    Task<(List<User> Users, int TotalCount)> GetUsersAsync(int page, int limit, string? companyMongoId = null);
    Task<bool> UpdateUserRoleAsync(Guid userId, string newRole, string requesterRole, string? requesterCompanyId);
    Task<bool> UpdateUserProfileAsync(Guid userId, UpdateProfileDto dto, string requesterRole, string? requesterCompanyId, IPasswordHasher passwordHasher);
}