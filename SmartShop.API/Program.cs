using System.Text;
using System.Threading.RateLimiting;
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
        Description = "Enter the JWT token: Bearer {token}"
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

// Rate limiting for the AI endpoints (generate description + chat), which call
// the Anthropic API and therefore cost money on every request. Limits per IP to
// stop abuse from running up the bill. Configurable via appsettings
// ("RateLimiting"). Defaults: 20 req / 60s.
var aiPermitLimit = builder.Configuration.GetValue("RateLimiting:AiPermitLimit", 20);
var aiWindowSeconds = builder.Configuration.GetValue("RateLimiting:AiWindowSeconds", 60);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("ai", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = aiPermitLimit,
                Window = TimeSpan.FromSeconds(aiWindowSeconds)
            }));
});

const string AngularDevCorsPolicy = "AngularDev";

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCorsPolicy, policy =>
    {
        // Production origins are configurable via appsettings ("Cors:AllowedOrigins")
        // — so the buyer sets their own frontend URL without touching code.
        // In development, any localhost port is always allowed.
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        policy.SetIsOriginAllowed(origin =>
                  allowedOrigins.Contains(origin) ||
                  (Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                   (uri.Host == "localhost" || uri.Host == "127.0.0.1")))
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

// HTTPS redirection is skipped inside containers (the container only exposes
// HTTP on 8080; TLS is terminated by the reverse proxy / cloud host in front).
if (!app.Environment.IsEnvironment("Docker"))
{
    app.UseHttpsRedirection();
}
app.UseCors(AngularDevCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

// Apply pending migrations on startup. Like the Admin seed, this must not bring
// the app down if it fails (e.g. the database is still waking up) — we just log
// it and the API keeps serving what it can; it'll be applied on the next startup
// (or the next time EnableRetryOnFailure retries).
try
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<SmartShop.Infrastructure.Persistence.SmartShopDbContext>();
    await db.Database.MigrateAsync();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to apply migrations on startup.");
}

// Seeding the Admin user must not prevent the API from starting: if the database
// is waking up (free serverless tier) or any other transient failure occurs, we
// log it and let the app keep serving requests normally (EnableRetryOnFailure
// already covers most of these cases; this is an extra safety net).
try
{
    await SeedAdminUserAsync(app);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to seed the Admin user on startup. The app continues without it.");
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