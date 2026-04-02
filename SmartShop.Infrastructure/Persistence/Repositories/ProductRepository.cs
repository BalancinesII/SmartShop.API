using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;

namespace SmartShop.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly SmartShopDbContext _context;

    public ProductRepository(SmartShopDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id) =>
        await _context.Products.FindAsync(id);

    public async Task<IEnumerable<Product>> GetAllAsync() =>
        await _context.Products.Where(p => p.IsActive).ToListAsync();

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string category) =>
        await _context.Products
            .Where(p => p.IsActive && p.Category == category)
            .ToListAsync();

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await GetByIdAsync(id);
        if (product is not null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id) =>
        await _context.Products.AnyAsync(p => p.Id == id);
}