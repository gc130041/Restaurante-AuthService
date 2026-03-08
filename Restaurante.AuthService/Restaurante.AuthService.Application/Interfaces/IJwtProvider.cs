using Restaurante.AuthService.Domain.Entities;

namespace Restaurante.AuthService.Application.Interfaces;

public interface IJwtProvider
{
    string GenerateToken(User user);
}