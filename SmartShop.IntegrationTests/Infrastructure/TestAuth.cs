using System.Net.Http.Headers;
using System.Net.Http.Json;
using SmartShop.Application.Auth.Commands.Login;
using SmartShop.Application.Auth.Commands.Register;

namespace SmartShop.IntegrationTests.Infrastructure;

// Helpers para obtener tokens reales a través de la API (login / register).
public static class TestAuth
{
    public const string CustomerPassword = "Customer123!";

    public static async Task<string> GetAdminTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/Auth/login",
            new LoginCommand(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword));
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return result!.Token;
    }

    // Registra un Customer nuevo. El email es único porque la base de datos en
    // memoria se comparte entre clases de test.
    public static async Task<(string Email, string Token)> RegisterCustomerAsync(HttpClient client)
    {
        var email = $"customer-{Guid.NewGuid():N}@smartshop.test";

        var response = await client.PostAsJsonAsync("/api/Auth/register",
            new RegisterCommand(email, CustomerPassword, "Customer", "Test"));
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return (email, result!.Token);
    }

    public static void UseBearerToken(this HttpRequestMessage request, string? token)
    {
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
