using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Services;
using SmartShop.Infrastructure.Persistence;

namespace SmartShop.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@smartshop.test";
    public const string AdminPassword = "AdminTest123!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configuración propia de los tests: así no dependen de
        // appsettings.Development.json, que está en .gitignore y no existe en el CI.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "integration-tests-secret-key-at-least-32-chars",
                ["Jwt:Issuer"] = "SmartShop.API",
                ["Jwt:Audience"] = "SmartShop.Client",
                ["Claude:ApiKey"] = "test-key-not-used",
                ["AdminSeed:Email"] = "",
                ["AdminSeed:Password"] = ""
            });
        });

        builder.ConfigureServices(services =>
        {
            // Quitar el registro real de SQL Server. Desde EF Core 9, AddDbContext
            // registra también IDbContextOptionsConfiguration<T>: si solo se borra
            // DbContextOptions<T>, EF sigue viendo SQL Server junto a InMemory.
            services.RemoveAll<DbContextOptions<SmartShopDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<SmartShopDbContext>>();

            // Add in-memory DbContext
            services.AddDbContext<SmartShopDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

            // Servicios externos sustituidos por fakes: ningún test llama a
            // Claude ni a Stripe de verdad (cuestan dinero y necesitan claves).
            services.RemoveAll<IAIService>();
            services.AddScoped<IAIService, FakeAIService>();
            services.RemoveAll<IPaymentService>();
            services.AddScoped<IPaymentService, FakePaymentService>();

            // Crear la base de datos en memoria y sembrar un Admin para los tests
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SmartShopDbContext>();
            db.Database.EnsureCreated();

            if (!db.Users.Any(u => u.Email == AdminEmail))
            {
                var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<object>>();
                var passwordHash = passwordHasher.HashPassword(new object(), AdminPassword);
                var admin = User.CreateAdmin(AdminEmail, passwordHash, "Admin", "Test");
                db.Users.Add(admin);
                db.SaveChanges();
            }
        });

        builder.UseEnvironment("Development");
    }
}