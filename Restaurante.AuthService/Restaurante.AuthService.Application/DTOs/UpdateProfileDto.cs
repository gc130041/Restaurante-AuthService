using System.ComponentModel.DataAnnotations;

namespace Restaurante.AuthService.Application.DTOs;

/// <summary>
/// DTO para actualizar el perfil de un empleado (nombre, apellido, teléfono, rol, contraseña).
/// Enviado por el proxy Node.js cuando el COMPANY_ADMIN edita un usuario.
/// </summary>
public class UpdateProfileDto
{
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string? Name { get; set; }

    [MaxLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
    public string? Surname { get; set; }

    [MaxLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    public string? Phone { get; set; }

    [MaxLength(50, ErrorMessage = "El username no puede exceder 50 caracteres")]
    public string? Username { get; set; }

    public string? Role { get; set; }

    /// <summary>Nueva contraseña (opcional). Si se omite o es vacía, no se cambia.</summary>
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string? Password { get; set; }
}
