using Restaurante.AuthService.Application.DTOs;

namespace Restaurante.AuthService.Application.Interfaces;

public interface IAuthService
{
    // Devuelve un token JWT (string) al hacer login
    Task<string> LoginAsync(LoginDto loginDto);
    
    // Devuelve true/false o un DTO de respuesta al registrar
    Task<bool> RegisterAsync(RegisterDto registerDto);
}