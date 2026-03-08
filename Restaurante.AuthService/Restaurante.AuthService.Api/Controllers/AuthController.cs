using Microsoft.AspNetCore.Mvc;
using Restaurante.AuthService.Application.DTOs;
using Restaurante.AuthService.Application.Interfaces;

namespace Restaurante.AuthService.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        try
        {
            await _authService.RegisterAsync(request);
            return StatusCode(201, new { message = "Usuario registrado exitosamente" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        try
        {
            var token = await _authService.LoginAsync(request);
            return Ok(new
            {
                message = "Login exitoso",
                token = token
                // Para devolver user.Id tendrías que cambiar la firma de LoginAsync a devolver un DTO en lugar de un string, o extraerlo del token en el cliente.
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}