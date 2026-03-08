using Restaurante.AuthService.Application.Interfaces;
using BCrypt.Net;

namespace Restaurante.AuthService.Infrastructure.Providers;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}