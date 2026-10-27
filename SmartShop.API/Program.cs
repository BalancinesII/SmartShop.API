using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;
using SmartShop.API.Middleware;
using SmartShop.API.OpenApi;
using SmartShop.Application;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;
using SmartShop.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Structured logging with Serilog, written to the console only (no paid sinks
// or external services). Readable text in Development; compact JSON elsewhere,
// so Azure's log stream and Docker keep one structured event per line.
// Minimum levels come from the "Serilog" section of appsettings.
builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();

    if (builder.Environment.IsDevelopment())
        loggerConfiguration.WriteTo.Console();
    else
        loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());
});

builder.Services.AddControllers();
// Native OpenAPI document (/openapi/v1.json) with the JWT Bearer scheme, so
// endpoints can be tried from Scalar with a token. Only mapped in Development.
builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

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
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
        options.AddPreferredSecuritySchemes(BearerSecuritySchemeTransformer.SchemeName));
}

// One structured log line per request (method, path, status, elapsed time).
// Registered before ExceptionMiddleware so it records the final status code.
app.UseSerilogRequestLogging();
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

// Health check that does NOT touch the database, so pinging it never wakes up
// Azure SQL (which would burn the free tier's monthly vCore-seconds).
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Migrations and the Admin seed run in the background once the API is already
// accepting requests. Before, they ran before app.Run(): with the free Azure SQL
// tier paused, EnableRetryOnFailure could hold the startup for a long time and
// every visitor waited for it. Failures are logged and never bring the app down.
app.Lifetime.ApplicationStarted.Register(() => _ = Task.Run(() => InitializeDatabaseAsync(app)));

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    try
    {
        using var migrationScope = app.Services.CreateScope();
        var db = migrationScope.ServiceProvider.GetRequiredService<SmartShop.Infrastructure.Persistence.SmartShopDbContext>();

        // Migrations only exist for relational providers. The integration tests
        // swap in the InMemory provider, where MigrateAsync throws and logged a
        // misleading error on every run.
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to apply migrations on startup.");
    }

    try
    {
        await SeedAdminUserAsync(app);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to seed the Admin user on startup. The app continues without it.");
    }
}

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