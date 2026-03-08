using Restaurante.AuthService.Domain.Entities;

namespace Restaurante.AuthService.Application.Interfaces;

public interface ISyncService
{
    Task SyncUserToMongoAsync(User user);
}