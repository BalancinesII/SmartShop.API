using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SmartShop.API.Middleware;
using SmartShop.Application;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;
using SmartShop.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Introduce el token JWT: Bearer {token}"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

const string AngularDevCorsPolicy = "AngularDev";

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCorsPolicy, policy =>
    {
        // En desarrollo, Angular puede arrancar en distintos puertos si el 4200
        // está ocupado (4201, 62655, etc.). Permitimos cualquier puerto de
        // localhost en vez de fijar uno solo para no romper esto cada vez.
        policy.SetIsOriginAllowed(origin =>
                  Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                  (uri.Host == "localhost" || uri.Host == "127.0.0.1"))
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors(AngularDevCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Aplica las migraciones pendientes al arrancar. Igual que con el seed del
// Admin, no debe tumbar la app si falla (p. ej. la base de datos aún está
// despertando) — simplemente lo registramos y la API sigue sirviendo lo que
// pueda; en el próximo arranque (o la próxima vez que EnableRetryOnFailure
// reintente) se aplicará igualmente.
try
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<SmartShop.Infrastructure.Persistence.SmartShopDbContext>();
    await db.Database.MigrateAsync();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "No se pudieron aplicar las migraciones al arrancar.");
}

// El seed del Admin no debe impedir que la API arranque: si la base de datos
// está "despertando" (tier gratuito serverless) o hay cualquier otro fallo
// transitorio, lo registramos y dejamos que la app siga sirviendo peticiones
// con normalidad (EnableRetryOnFailure ya cubre la mayoría de estos casos,
// esto es una red de seguridad adicional).
try
{
    await SeedAdminUserAsync(app);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "No se pudo sembrar el usuario Admin al arrancar. La app continúa sin él.");
}

app.Run();

static async Task SeedAdminUserAsync(WebApplication app)
{
    var adminEmail = app.Configuration["AdminSeed:Email"];
    var adminPassword = app.Configuration["AdminSeed:Password"];

    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        return;

    using var scope = app.Services.CreateScope();
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

    if (await userRepository.ExistsAsync(adminEmail))
        return;

    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<object>>();
    var passwordHash = passwordHasher.HashPassword(new object(), adminPassword);

    var admin = User.CreateAdmin(adminEmail, passwordHash, "Admin", "Admin");
    await userRepository.AddAsync(admin);
}

public partial class Program { }