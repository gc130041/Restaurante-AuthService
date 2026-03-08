using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Importaciones de tus capas
using Restaurante.AuthService.Application.Interfaces;
using Restaurante.AuthService.Application.Services;
using Restaurante.AuthService.Infrastructure.Data;
using Restaurante.AuthService.Infrastructure.Repositories;
using Restaurante.AuthService.Infrastructure.Providers;
using Restaurante.AuthService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Controladores
builder.Services.AddControllers();

// 2. Base de Datos
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

// 3. INYECCIÓN DE DEPENDENCIAS (El corazón de Clean Architecture)
// 3.1. Repositorios
builder.Services.AddScoped<IUserRepository, UserRepository>();

// 3.2. Proveedores de Infraestructura (Detalles técnicos ocultos detrás de interfaces)
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtProvider, JwtProvider>();
// Usamos AddHttpClient porque MongoSyncService usa un HttpClient por dentro
builder.Services.AddHttpClient<ISyncService, MongoSyncService>(); 

// 3.3. Servicios de Aplicación (Casos de uso)
builder.Services.AddScoped<IAuthService, AuthService>();

// 4. Configuración de JWT (Para que la API sepa cómo VALIDAR tokens entrantes)
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey)) throw new ArgumentNullException("Jwt Key is missing in appsettings.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// 5. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 6. Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

// 7. Preparación de la Base de Datos al arranque
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AuthDbContext>();

    int retries = 0;
    while (retries < 10)
    {
        try
        {
            Console.WriteLine($"Intentando conectar a AuthDB (Intento {retries + 1}/10)...");
            context.Database.EnsureCreated(); 
            Console.WriteLine("Base de datos de AuthDB lista.");
            break;
        }
        catch (Exception ex)
        {
            retries++;
            if (retries >= 10) 
            {
                Console.WriteLine($"Error crítico: No se pudo conectar a AuthDB. {ex.Message}");
                throw;
            }
            Console.WriteLine($"Postgres (AuthDB) no responde. Reintentando en 3s... ({ex.Message})");
            Thread.Sleep(3000);
        }
    }
}

app.Run();