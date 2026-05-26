using Microsoft.EntityFrameworkCore;
using Restaurante.AuthService.Domain.Entities;
using Restaurante.AuthService.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Restaurante.AuthService.Infrastructure.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(AuthDbContext context, IPasswordHasher passwordHasher, IConfiguration configuration)
        {
            // Seed de un usuario administrador por defecto SOLO si no existen usuarios todavía
            if (!await context.Users.AnyAsync())
            {
                var adminPassword = configuration["SeedSettings:AdminPassword"];
                
                if (string.IsNullOrEmpty(adminPassword))
                {
                    throw new ArgumentNullException("SeedSettings:AdminPassword", "La contraseña de administrador por defecto no está configurada.");
                }

                var adminUser = new User
                {
                    Username = "superadmin",
                    Email = "admin@restaurante.local",
                    PasswordHash = passwordHasher.Hash(adminPassword),
                    Role = "SUPER_ADMIN",
                    IsActive = true,
                    EmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();
            }
        }
    }
}
