using Restaurante.AuthService.Application.DTOs;
using Restaurante.AuthService.Application.Interfaces;
using Restaurante.AuthService.Domain.Entities;

namespace Restaurante.AuthService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly ISyncService _syncService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider,
        ISyncService syncService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _syncService = syncService;
    }

    public async Task<string> LoginAsync(LoginDto request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenciales incorrectas.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("El usuario está desactivado.");
        }

        return _jwtProvider.GenerateToken(user);
    }

    public async Task<bool> RegisterAsync(RegisterDto request)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("El correo ya está registrado.");
        }

        var newUser = new User
        {
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role.ToUpper()
        };

        await _userRepository.AddAsync(newUser);
        await _userRepository.SaveChangesAsync();

        // Llamada externa delegada a la interfaz
        await _syncService.SyncUserToMongoAsync(newUser);

        return true;
    }
}