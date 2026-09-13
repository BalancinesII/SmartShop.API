namespace SmartShop.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core requires a parameterless constructor
    private Product() { }

    public static Product Create(string name, decimal price, int stock, string category, string? imageUrl = null)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            Stock = stock,
            Category = category,
            ImageUrl = imageUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, decimal price, int stock, string category, string? imageUrl = null)
    {
        Name = name;
        Price = price;
        Stock = stock;
        Category = category;
        ImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }
}
