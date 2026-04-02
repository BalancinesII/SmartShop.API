# SmartShop API

ASP.NET Core 8 REST API for an ecommerce platform with AI-powered features using the Claude API.

## Features

- **AI product descriptions** — automatically generates compelling product descriptions using Claude
- **AI customer support chatbot** — multi-turn conversational assistant with persistent chat history
- **JWT authentication** — register and login with token-based auth
- **Product management** — full CRUD for products
- **Clean Architecture** — domain-centric design with clear separation of concerns
- **CQRS with MediatR** — every use case is an isolated, testable handler
- **13 automated tests** — unit and integration test coverage

## Tech stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 + SQL Server |
| AI | Anthropic Claude API (`claude-sonnet-4-5`) |
| CQRS | MediatR |
| Validation | FluentValidation |
| Auth | JWT Bearer |
| Docs | Swagger / OpenAPI |
| Tests | xUnit + Moq + FluentAssertions |

## Architecture

The solution follows Clean Architecture with 4 layers:
```
SmartShop.Domain          → Entities, interfaces, no external dependencies
SmartShop.Application     → Use cases (CQRS handlers), DTOs
SmartShop.Infrastructure  → EF Core, Claude API client, JWT service
SmartShop.API             → Controllers, middleware, DI composition
```

Dependency rule: outer layers depend on inner layers, never the reverse.
`IAIService` in Domain defines the AI contract — `ClaudeAIService` in Infrastructure implements it.

## Endpoints

### Auth
| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/Auth/register` | Register a new user |
| POST | `/api/Auth/login` | Login and get JWT token |

### Products
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/Products` | — | Get all active products |
| POST | `/api/Products` | — | Create a product |
| POST | `/api/Products/{id}/describe` | ✅ | Generate AI description |

### Chat
| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/Chat/message` | Send a message to the AI assistant |

## Getting started

### Prerequisites
- .NET 8 SDK
- SQL Server or LocalDB (included with Visual Studio)
- Anthropic API key — get one at [console.anthropic.com](https://console.anthropic.com)

### Setup

1. Clone the repository
```bash
git clone https://github.com/BalancinesII/SmartShop.API.git
cd SmartShop.API
```

2. Create `appsettings.Development.json` in `SmartShop.API/` with your credentials:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SmartShopDb;Trusted_Connection=True;"
  },
  "Claude": {
    "ApiKey": "your-anthropic-api-key"
  },
  "Jwt": {
    "Key": "your-secret-key-min-32-characters",
    "Issuer": "SmartShop.API",
    "Audience": "SmartShop.Client"
  }
}
```

3. Apply database migrations
```bash
cd SmartShop.API
dotnet ef database update --project ../SmartShop.Infrastructure
```

4. Run the API
```bash
dotnet run
```

5. Open Swagger at `https://localhost:7024/swagger`

### Running tests
```bash
dotnet test
```

## AI integration

### Product description generation
Sends product name, category and price to Claude and returns a 2-3 sentence persuasive description in the user's language. The description is persisted to the database.

### Customer support chatbot
Maintains conversation history per session (stored in SQL Server). Each request includes the last 10 messages as context so Claude can follow the conversation naturally.

## Project status

MVP complete. Planned improvements:
- [ ] Role-based authorization (Admin / Customer)
- [ ] Order management
- [ ] Product search with semantic similarity
- [ ] Azure deployment
