using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Persistence;

public class SmartShopDbContext : DbContext
{
    public SmartShopDbContext(DbContextOptions<SmartShopDbContext> options)
        : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartShopDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}