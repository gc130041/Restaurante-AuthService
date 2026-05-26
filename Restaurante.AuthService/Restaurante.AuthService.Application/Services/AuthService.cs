using System.Globalization;
using Microsoft.Extensions.Configuration;
using Restaurante.AuthService.Application.DTOs;
using Restaurante.AuthService.Application.Helpers;
using Restaurante.AuthService.Application.Interfaces;
using Restaurante.AuthService.Domain.Entities;

namespace Restaurante.AuthService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        User? user;
        if (loginDto.EmailOrUsername.Contains('@'))
        {
            user = await _userRepository.GetByEmailAsync(loginDto.EmailOrUsername);
        }
        else
        {
            user = await _userRepository.GetByUsernameAsync(loginDto.EmailOrUsername);
        }

        if (user == null || !_passwordHasher.Verify(loginDto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenciales incorrectas.");
        }

        if (!user.EmailVerified)
        {
            throw new UnauthorizedAccessException("Debes verificar tu correo antes de iniciar sesión.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("El usuario está desactivado.");
        }

        var token = _jwtProvider.GenerateToken(user);
        
        user.RefreshToken = TokenGenerator.GenerateSecureToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        int expiresInMinutes = int.TryParse(_configuration["Jwt:ExpiryInMinutes"], out int exp) ? exp : 480;

        return new AuthResponseDto 
        {
            AccessToken = token,
            RefreshToken = user.RefreshToken,
            ExpiresIn = expiresInMinutes * 60,
            UserDetails = new UserDetailsDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                MongoId = user.MongoId
            }
        };
    }

    public async Task<RegisterResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        var existingUser = await _userRepository.GetByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("El correo ya está registrado.");
        }

        if (!string.IsNullOrEmpty(registerDto.Username))
        {
            var existingUsername = await _userRepository.GetByUsernameAsync(registerDto.Username);
            if (existingUsername != null)
            {
                throw new InvalidOperationException("El nombre de usuario ya está registrado.");
            }
        }

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = registerDto.Email,
            Username = registerDto.Username,
            PasswordHash = _passwordHasher.Hash(registerDto.Password),
            Role = string.IsNullOrEmpty(registerDto.Role) ? "COMPANY_ADMIN" : registerDto.Role,
            IsActive = false,
            EmailVerified = false,
            CompanyMongoId = registerDto.CompanyMongoId,
            BranchMongoId = registerDto.BranchMongoId,
            MongoId = registerDto.MongoId,
            VerificationOtp = TokenGenerator.GenerateNumericOtp(),
            VerificationOtpExpiry = DateTime.UtcNow.AddMinutes(15)
        };

        await _userRepository.AddAsync(newUser);
        await _userRepository.SaveChangesAsync();

        Task.Run(() => _emailService.SendEmailVerificationAsync(newUser.Email, newUser.Username ?? newUser.Email, newUser.VerificationOtp));

        return new RegisterResponseDto
        {
            Success = true,
            AuthUserId = newUser.Id,
            Email = newUser.Email,
            Message = "Credenciales creadas. Email de verificación enviado."
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Token de refresco inválido o expirado.");
        }

        var token = _jwtProvider.GenerateToken(user);
        user.RefreshToken = TokenGenerator.GenerateSecureToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        int expiresInMinutes = int.TryParse(_configuration["Jwt:ExpiryInMinutes"], out int exp) ? exp : 480;

        return new AuthResponseDto 
        {
            AccessToken = token,
            RefreshToken = user.RefreshToken,
            ExpiresIn = expiresInMinutes * 60,
            UserDetails = new UserDetailsDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                MongoId = user.MongoId
            }
        };
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var user = await _userRepository.GetByVerificationOtpAsync(token);

        if (user == null || user.VerificationOtpExpiry < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Código OTP inválido o expirado.");
        }

        user.EmailVerified = true;
        user.IsActive = true;
        user.VerificationOtp = null;
        user.VerificationOtpExpiry = null;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();
        
        Task.Run(() => _emailService.SendWelcomeEmailAsync(user.Email, user.Username ?? user.Email));

        return true;
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            return true;
        }

        user.PasswordResetToken = TokenGenerator.GenerateSecureToken();
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        Task.Run(() => _emailService.SendPasswordResetAsync(user.Email, user.Username ?? user.Email, user.PasswordResetToken));

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var user = await _userRepository.GetByPasswordResetTokenAsync(token);

        if (user == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Token inválido o expirado.");
        }

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return true;
    }

    public async Task<User?> ValidateUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive) return null;
        return user;
    }

    public async Task<bool> ResendVerificationOtpAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            return true;
        }

        var newOtp = TokenGenerator.GenerateNumericOtp();
        user.UpdateVerificationOtp(newOtp, DateTime.UtcNow.AddMinutes(15));

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        Task.Run(() => _emailService.SendEmailVerificationAsync(user.Email, user.Username ?? user.Email, user.VerificationOtp));

        return true;
    }

    public async Task<(List<User> Users, int TotalCount)> GetUsersAsync(int page, int limit, string? companyMongoId = null)
    {
        return await _userRepository.GetAllAsync(page, limit, companyMongoId);
    }

    public async Task<bool> UpdateUserRoleAsync(Guid userId, string newRole, string requesterRole, string? requesterCompanyId)
    {
        var allowedRoles = new[] { "SUPER_ADMIN", "COMPANY_ADMIN", "BRANCH_MANAGER", "WAITER", "CHEF", "CASHIER", "RECEPTIONIST", "CLIENT" };
        newRole = newRole.ToUpper(CultureInfo.InvariantCulture);

        if (!allowedRoles.Contains(newRole))
        {
            throw new InvalidOperationException($"El rol '{newRole}' no es válido.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new InvalidOperationException("Usuario no encontrado.");

        // Solo SUPER_ADMIN puede asignar COMPANY_ADMIN
        if (newRole == "COMPANY_ADMIN" && requesterRole != "SUPER_ADMIN" && requesterRole != "ADMIN_ROLE")
        {
            throw new UnauthorizedAccessException("Solo un SUPER_ADMIN puede asignar el rol COMPANY_ADMIN.");
        }

        // Si es COMPANY_ADMIN, solo puede editar usuarios de su misma compañía
        if (requesterRole == "COMPANY_ADMIN" && user.CompanyMongoId != requesterCompanyId)
        {
            throw new UnauthorizedAccessException("No tienes permiso para modificar usuarios de otra compañía.");
        }

        user.Role = newRole;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateUserProfileAsync(
        Guid userId,
        UpdateProfileDto dto,
        string requesterRole,
        string? requesterCompanyId,
        IPasswordHasher passwordHasher)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("Usuario no encontrado.");
        }

        // Si el solicitante es COMPANY_ADMIN, solo puede editar usuarios de su misma compañía
        if (requesterRole == "COMPANY_ADMIN" && user.CompanyMongoId != requesterCompanyId)
        {
            throw new UnauthorizedAccessException("No tienes permiso para modificar usuarios de otra compañía.");
        }

        // Actualizar campos si se proporcionan
        if (!string.IsNullOrEmpty(dto.Name))
            user.Name = dto.Name;

        if (!string.IsNullOrEmpty(dto.Surname))
            user.Surname = dto.Surname;

        if (!string.IsNullOrEmpty(dto.Phone))
            user.Phone = dto.Phone;

        if (!string.IsNullOrEmpty(dto.Username))
        {
            if (dto.Username != user.Username)
            {
                var existingUser = await _userRepository.GetByUsernameAsync(dto.Username);
                if (existingUser != null)
                {
                    throw new InvalidOperationException("El nombre de usuario ya está registrado.");
                }
            }
            user.Username = dto.Username;
        }

        if (!string.IsNullOrEmpty(dto.Role))
        {
            var allowedRoles = new[] { "SUPER_ADMIN", "COMPANY_ADMIN", "BRANCH_MANAGER", "WAITER", "CHEF", "CASHIER", "RECEPTIONIST", "CLIENT" };
            var newRole = dto.Role.ToUpper(CultureInfo.InvariantCulture);

            if (!allowedRoles.Contains(newRole))
            {
                throw new InvalidOperationException($"El rol '{newRole}' no es válido.");
            }

            // Solo SUPER_ADMIN puede asignar COMPANY_ADMIN
            if (newRole == "COMPANY_ADMIN" && requesterRole != "SUPER_ADMIN" && requesterRole != "ADMIN_ROLE")
            {
                throw new UnauthorizedAccessException("Solo un SUPER_ADMIN puede asignar el rol COMPANY_ADMIN.");
            }

            user.Role = newRole;
        }

        if (!string.IsNullOrEmpty(dto.Password))
        {
            user.PasswordHash = passwordHasher.Hash(dto.Password);
        }

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return true;
    }
}