# SmartShop API

ASP.NET Core 8 REST API for an ecommerce platform with AI-powered features using the Anthropic Claude API. Deployed live on Azure, with a companion Angular frontend.

🔗 **Live demo:** [smartshop-api-nachosaiz](https://smartshop-api-nachosaiz-d2fvhhgfgwdkc5cc.swedencentral-01.azurewebsites.net/api/Products) · Frontend: [SmartShop.Web](https://github.com/BalancinesII/SmartShop.Web) → [live app](https://ashy-bush-06c228b03.5.azurestaticapps.net)

## Features

- **AI product descriptions** — automatically generates compelling product descriptions using Claude
- **AI customer support chatbot** — multi-turn conversational assistant with persistent chat history and real product catalog context
- **JWT authentication with roles** — Admin/Customer role-based access control on all write operations
- **Full product CRUD** — create, read, update and delete products, with pagination on listing
- **Resilient database access** — automatic retry on transient SQL failures (e.g. serverless database auto-resume)
- **Automatic migrations** — schema is applied on startup, no manual step needed on a fresh database
- **Input validation** — FluentValidation pipeline with descriptive error messages
- **Global exception handling** — consistent error responses across all endpoints
- **Clean Architecture** — domain-centric design with clear separation of concerns
- **CQRS with MediatR** — every use case is an isolated, testable handler
- **14 automated tests** — unit and integration test coverage
- **CI/CD on Azure** — every push to `master` builds and deploys automatically via GitHub Actions

## Tech stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 + SQL Server |
| AI | Anthropic Claude API (`claude-sonnet-4-5`) |
| CQRS | MediatR |
| Validation | FluentValidation |
| Auth | JWT Bearer |
| Docs | Swagger / OpenAPI (Development only) |
| Tests | xUnit + Moq + FluentAssertions |
| Hosting | Azure App Service (Free tier) + Azure SQL (Free offer) |
| CI/CD | GitHub Actions |
| Frontend | Angular 18 + Material ([SmartShop.Web](https://github.com/BalancinesII/SmartShop.Web)) |

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
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/Auth/register` | — | Register a new user (role: Customer) |
| POST | `/api/Auth/login` | — | Login and get JWT token |
| PUT | `/api/Auth/{id}/promote` | ✅ Admin | Promote a user to Admin |

### Products
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/Products?pageNumber=&pageSize=` | — | Get paginated active products (max 50/page) |
| POST | `/api/Products` | ✅ Admin | Create a product |
| PUT | `/api/Products/{id}` | ✅ Admin | Update a product |
| DELETE | `/api/Products/{id}` | ✅ Admin | Delete a product |
| POST | `/api/Products/{id}/describe` | ✅ Admin | Generate AI description |

### Chat
| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/Chat/message` | Send a message to the AI assistant |

### Payments (Stripe)
| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/Payments/checkout` | Create a Stripe Checkout session for a product; returns the payment URL |
| POST | `/api/Payments/webhook` | Stripe calls this to confirm completed payments (signature-verified) |

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
  },
  "AdminSeed": {
    "Email": "admin@example.com",
    "Password": "a-strong-password"
  }
}
```
`AdminSeed` creates one Admin user on startup if it doesn't already exist — needed since new users always register as `Customer`, and only an existing Admin can promote others.

3. Run the API — database migrations are applied automatically on startup, no manual `dotnet ef database update` needed
```bash
dotnet run --project SmartShop.API
```

4. Open Swagger at `https://localhost:7024/swagger` (Development environment only)

### Running tests
```bash
dotnet test
```

## AI integration

### Product description generation
Sends product name, category and price to Claude and returns a 2-3 sentence persuasive description. The description is persisted to the database.

### Customer support chatbot
Maintains conversation history per session stored in SQL Server. Each request includes the last 10 messages as context and the full product catalog so Claude can answer questions about real inventory, prices and availability.

## Payments (Stripe)

Product checkout uses [Stripe Checkout](https://stripe.com/docs/payments/checkout) — the hosted payment page, so card details never touch this server.

1. Add your test keys to config (`Stripe:SecretKey`, starting with `sk_test_`).
2. `POST /api/Payments/checkout` with `{ "productId": "...", "quantity": 1 }` returns a `checkoutUrl` — redirect the customer there.
3. Pay with Stripe's test card `4242 4242 4242 4242` (any future expiry, any CVC).
4. To receive confirmation events locally, run the [Stripe CLI](https://stripe.com/docs/stripe-cli): `stripe listen --forward-to localhost:7024/api/Payments/webhook`, and put the printed `whsec_...` secret in `Stripe:WebhookSecret`.

The webhook handler logs completed payments — wire in your own order creation / stock update / email logic where indicated in `PaymentsController.Webhook`.

## Run with Docker

The fastest way to get the full backend running — no need to install .NET or SQL Server locally. Requires only Docker.

1. Copy `.env.example` to `.env` and add your Anthropic API key.
2. From the repo root:
```bash
docker-compose up --build
```

This spins up SQL Server and the API together. The API waits for the database to be ready, applies migrations automatically, and seeds an Admin user (`admin@smartshop.local` / `Admin123!`). The API is then available at `http://localhost:8080/api/Products`.

The Angular frontend lives in a [separate repo](https://github.com/BalancinesII/SmartShop.Web) — run it with `npm start` and it will talk to the containerized API (CORS already allows `http://localhost:4200`).

## Deployment

Runs on Azure entirely on free tiers:

- **App Service (F1 Free)** — hosts the API
- **Azure SQL (Free offer)** — 32GB storage, 100k vCore-seconds/month; the database auto-pauses when idle and resumes on the next request (handled transparently via `EnableRetryOnFailure`)
- **Static Web Apps (Free)** — hosts the [Angular frontend](https://github.com/BalancinesII/SmartShop.Web)
- **GitHub Actions** — pushing to `master` triggers build + deploy automatically for both repos

CORS is configured to allow any `localhost` port (for local frontend development) plus the deployed Static Web App origin.

## Project status

- [x] Clean Architecture with 4 layers
- [x] CQRS with MediatR
- [x] EF Core + SQL Server, with automatic migrations on startup
- [x] JWT Authentication with Admin/Customer roles
- [x] Pagination
- [x] AI product descriptions
- [x] AI chatbot with product catalog context
- [x] Input validation with FluentValidation
- [x] Global exception handling
- [x] Unit and integration tests (14)
- [x] Angular frontend
- [x] Azure deployment with CI/CD
- [x] Docker / docker-compose for local dev
- [x] Rate limiting on AI endpoints
- [x] Stripe checkout integration
- [ ] Automated tests for the frontend
