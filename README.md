# Kaza Dashboard — Backend API

[![CI](https://github.com/moee98/JM-API/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/moee98/JM-API/actions/workflows/ci-cd.yml)
[![Release](https://github.com/moee98/JM-API/actions/workflows/release-please.yml/badge.svg)](https://github.com/moee98/JM-API/actions/workflows/release-please.yml)
[Latest release](https://github.com/moee98/JM-API/releases/latest)

ASP.NET Core Web API powering the [Kaza Dashboard](https://github.com/moee98/JM-Frontend) business management system. Handles job management, customer and vehicle records, invoicing, expenses, authentication, and third-party integrations.

## Features

- **Jobs** — full CRUD for work orders including status tracking, assigned technician, and service line items
- **Customers & vehicles** — linked customer and vehicle records with vehicle inspection support
- **Invoicing** — automated HTML invoice generation with VAT calculation, sent to customers via SMTP email (MailKit)
- **Expenses** — expense items with category management and file attachments (images and PDFs stored in DB)
- **Payments** — multiple payment methods per job with amount tracking
- **Integrations** — OAuth token storage for eBay and Square
- **Notifications** — per-user in-app notification management
- **Authentication** — ASP.NET Core Identity with JWT access tokens (HttpOnly cookies) and refresh tokens
- **Role-based access** — User and Admin roles enforced on endpoints

## Tech Stack

| Area | Technology |
|------|-----------|
| Framework | ASP.NET Core (.NET 10) |
| Language | C# |
| ORM | Entity Framework Core |
| Database | Microsoft SQL Server |
| Auth | ASP.NET Core Identity, JWT |
| Email | MailKit (SMTP) |
| Containerisation | Docker |
| Testing | xUnit, WebApplicationFactory |

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server (local or Docker)

### Configuration

Copy and fill in the required settings in `appsettings.json` or use user secrets:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=KazaDB;..."
  },
  "JwtSettings": {
    "Key": "your-secret-key",
    "Issuer": "http://localhost:5173",
    "Audience": "http://localhost:5173"
  },
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "Username": "you@example.com",
    "Password": "your-app-password",
    "From": "Kaza Dashboard"
  }
}
```

### Run locally

```powershell
dotnet restore
dotnet run --project JMAPI.csproj
```

Swagger UI is available at `https://localhost:7230/swagger` in Development.

### Docker

A `Dockerfile` is included for containerised deployment. It is intended to be used via Docker Compose from the frontend repo:

```bash
docker compose up -d
```

For standalone build:

```bash
docker build -t kaza-api .
docker run -p 5000:5000 kaza-api
```

## API Endpoints

| Group | Base path |
|-------|-----------|
| Auth | `POST /api/Auth/login`, `POST /api/Auth/register`, `POST /api/Auth/refresh` |
| Users | `GET /api/Users`, `GET /api/Users/{id}` |
| Jobs | `GET/POST /api/Job`, `GET/PUT/DELETE /api/Job/{id}`, `POST /api/Job/{id}/send-invoice` |
| Customers | `GET/POST /api/Customers`, `GET/PUT/DELETE /api/Customers/{id}` |
| Vehicles | `GET/POST /api/Vehicles`, `GET/PUT/DELETE /api/Vehicles/{id}` |
| Services | `GET/POST /api/Services`, `GET/PUT/DELETE /api/Services/{id}` |
| Job Services | `GET/POST /api/JobServices`, `PUT/DELETE /api/JobServices/{id}` |
| Expenses | `GET/POST /api/ExpenseItems`, `POST /api/ExpenseItems/{id}/attachments` |
| Company | `GET/PUT /api/Company` |
| Payments | `GET/POST /api/PaymentMethods` |
| Integrations | `GET/POST /api/Integrations` |
| Notifications | `GET/POST/DELETE /api/Notifications` |

Most endpoints require a valid JWT. Admin-only endpoints are marked with `[Authorize(Roles = "Admin")]`.

## Project Structure

```
├── Controllers/        # API route handlers
├── Services/           # Business logic
├── Interfaces/         # Service contracts
├── Models/             # EF entities and request models
├── Database/           # AppDbContext and design-time factory
├── Migrations/         # EF Core migration history
├── appsettings.json    # Base configuration
├── appsettings.Production.json  # Production overrides
├── Dockerfile
└── JMAPI.Tests/        # Integration test project
```

## Testing

The project includes an integration test suite using a real `WebApplicationFactory` with an in-memory SQLite database.

```powershell
dotnet test JMAPI.Tests\JMAPI.Tests.csproj
```

Current coverage: `AuthController`, `UsersController`, `JobController`, `ExpenseItemsController`.

## Production Deployment

`appsettings.Production.json` contains overrides for self-hosted deployment including:
- SQL Server connection string
- Kestrel bound to `http://localhost:5000`
- `Cookies:Secure = false` for HTTP-only LAN deployments

The `Cookies:Secure` flag is made configurable so the JWT cookie works correctly on HTTP without requiring HTTPS on a local network.

## License

MIT License — Copyright (c) 2026 Mahomed Ebrahim
