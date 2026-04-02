namespace SmartShop.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core necesita un constructor sin parámetros
    private Product() { }

    public static Product Create(string name, decimal price, int stock, string category)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            Stock = stock,
            Category = category,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, decimal price, int stock, string category)
    {
        Name = name;
        Price = price;
        Stock = stock;
        Category = category;
        UpdatedAt = DateTime.UtcNow;
    }
}