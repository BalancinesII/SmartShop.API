using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using SmartShop.Domain.Entities;
using SmartShop.Infrastructure.Persistence;

namespace SmartShop.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@smartshop.test";
    public const string AdminPassword = "AdminTest123!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Eliminar el DbContext real
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SmartShopDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // Add in-memory DbContext
            services.AddDbContext<SmartShopDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

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