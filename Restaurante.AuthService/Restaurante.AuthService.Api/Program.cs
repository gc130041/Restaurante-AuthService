using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;

// Importaciones de tus capas
using Restaurante.AuthService.Application.Interfaces;
using Restaurante.AuthService.Application.Services;
using Restaurante.AuthService.Infrastructure.Data;
using Restaurante.AuthService.Infrastructure.Repositories;
using Restaurante.AuthService.Infrastructure.Providers;
using Restaurante.AuthService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Controladores
builder.Services.AddControllers()
    .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Habilitar cookies trans-origen
    });
});

// 2. Base de Datos
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

// 3. INYECCIÓN DE DEPENDENCIAS (El corazón de Clean Architecture)
// 3.1. Repositorios
builder.Services.AddScoped<IUserRepository, UserRepository>();

// 3.2. Proveedores de Infraestructura (Detalles técnicos ocultos detrás de interfaces)
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtProvider, JwtProvider>();

// 3.3. Servicios de Aplicación (Casos de uso)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// 4. Configuración de JWT (Para que la API sepa cómo VALIDAR tokens entrantes)
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey)) throw new ArgumentNullException(nameof(jwtKey), "Jwt Key is missing in configuration");
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

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var cookieToken = context.Request.Cookies["X-Auth-Token"];
                if (!string.IsNullOrEmpty(cookieToken))
                {
                    context.Token = cookieToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// 5. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// 6. Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 7. Preparación de la Base de Datos al arranque
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AuthDbContext>();
    var passwordHasher = services.GetRequiredService<IPasswordHasher>();
    var configuration = services.GetRequiredService<IConfiguration>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    int retryCount = 0;
    while (retryCount < 10)
    {
        try
        {
            logger.LogInformation($"Intentando conectar a AuthDB (Intento {retryCount + 1}/10)...");
            context.Database.EnsureCreated();
            
            // === EXECUTING AUTOMATED SCHEMA UPGRADE MIGRATION ===
            logger.LogInformation("Verificando y aplicando migración atómica para campos y OTP en PostgreSQL...");
            context.Database.ExecuteSqlRaw(@"
                ALTER TABLE users ADD COLUMN IF NOT EXISTS branch_mongo_id VARCHAR(64);
                ALTER TABLE users ADD COLUMN IF NOT EXISTS company_mongo_id VARCHAR(64);
                ALTER TABLE users ADD COLUMN IF NOT EXISTS name VARCHAR(100);
                ALTER TABLE users ADD COLUMN IF NOT EXISTS surname VARCHAR(100);
                ALTER TABLE users ADD COLUMN IF NOT EXISTS phone VARCHAR(20);
                ALTER TABLE users ADD COLUMN IF NOT EXISTS username VARCHAR(50);
                ALTER TABLE users ADD COLUMN IF NOT EXISTS verification_otp VARCHAR(6);
                ALTER TABLE users ADD COLUMN IF NOT EXISTS verification_otp_expiry TIMESTAMP WITH TIME ZONE;
            ");
            try 
            {
                context.Database.ExecuteSqlRaw(@"
                    CREATE UNIQUE INDEX IF NOT EXISTS ix_users_verification_otp_unique ON users (verification_otp) WHERE (verification_otp IS NOT NULL);
                ");
                context.Database.ExecuteSqlRaw(@"
                    CREATE UNIQUE INDEX IF NOT EXISTS ix_users_username_unique ON users (username) WHERE (username IS NOT NULL);
                ");
            }
            catch (Exception exIndex)
            {
                logger.LogWarning($"No se pudo crear índices únicos (posiblemente ya existen): {exIndex.Message}");
            }
            
            await DataSeeder.SeedAsync(context, passwordHasher, configuration);

            logger.LogInformation("Base de datos de AuthDB lista y seeder ejecutado.");
            break;
        }
        catch (Exception ex)
        {
            retryCount++;
            if (retryCount >= 10)
            {
                logger.LogError($"Error crítico: No se pudo conectar a AuthDB. {ex.Message}");
                throw;
            }
            logger.LogWarning($"Postgres (AuthDB) no responde. Reintentando en 3s... ({ex.Message})");
            Thread.Sleep(3000);
        }
    }
}

app.Run();