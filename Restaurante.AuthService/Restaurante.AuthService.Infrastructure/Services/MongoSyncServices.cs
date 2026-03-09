using System.Text;
using System.Text.Json;
using Restaurante.AuthService.Application.Interfaces;
using Restaurante.AuthService.Domain.Entities;

namespace Restaurante.AuthService.Infrastructure.Services;

public class MongoSyncService : ISyncService
{
    private readonly HttpClient _httpClient;

    public MongoSyncService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SyncUserToMongoAsync(User user)
    {
        try
        {
            var syncData = new { email = user.Email, role = user.Role };
            var jsonContent = new StringContent(JsonSerializer.Serialize(syncData), Encoding.UTF8, "application/json");

            await _httpClient.PostAsync("http://server-admin:3001/restaurant/v1/users/sync", jsonContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Advertencia: Usuario creado en Postgres, pero falló la sincronización con Mongo: {ex.Message}");
        }
    }
}