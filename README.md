# OrderManagementAPI

## Overview

`OrderManagementAPI` is an ASP.NET Core Web API for managing workshop and order flow data.
The API currently uses:

- ASP.NET Core on `net10.0`
- Entity Framework Core with SQL Server
- ASP.NET Identity with `AppUser`
- JWT bearer authentication
- Swagger / OpenAPI in Development

The main business areas in the project are:

- authentication and user identity
- jobs / work orders
- customers
- vehicles
- vehicle inspections
- services and job services
- expense items and expense categories
- company details
- payment methods

## Solution Layout

Root project:

- `JMAPI.csproj`: main API project
- `Program.cs`: application startup, DI, auth, CORS, Swagger
- `Database/`: EF Core `AppDbContext` and design-time factory
- `Controllers/`: API endpoints
- `Services/`: application services used by selected controllers
- `Models/`: EF entities and request models
- `Migrations/`: EF Core migration history
- `Properties/launchSettings.json`: local launch profiles
- `JMAPI.Tests/`: integration test project

## Main Runtime Flow

### Authentication

Identity is the active user system for the application.

- `AppUser` extends `IdentityUser`
- registration creates an Identity user and assigns the default `User` role
- login issues a short-lived access token and refresh token
- refresh validates the stored refresh token and issues a new access token

JWT configuration is read from:

- `JwtSettings:Key`
- `JwtSettings:Issuer`
- `JwtSettings:Audience`

### Database

The application uses `AppDbContext` in `Database/DbContext.cs`.

Important note:

- the active runtime user model is `AppUser`
- historical migrations still contain older schema history from the pre-Identity user model
- if database cleanup is needed, add a new EF migration rather than editing old migrations

## Local Development

### Prerequisites

- .NET 10 SDK
- SQL Server reachable from your machine
- a valid connection string for `DefaultConnection`

### Configuration

Current config files:

- `appsettings.json`
- `appsettings.Development.json`

At minimum, these settings must be valid:

- `ConnectionStrings:DefaultConnection`
- `JwtSettings:Key`
- `JwtSettings:Issuer`
- `JwtSettings:Audience`

Recommended:

- keep secrets out of source control
- move connection strings and JWT secrets to user secrets or environment variables

### Run the API

From the repository root:

```powershell
dotnet run --project JMAPI.csproj
```

Launch settings currently expose:

- HTTPS on `https://0.0.0.0:7230`
- HTTP on `http://0.0.0.0:5114`

Swagger is enabled in Development.

### Restore and Build

```powershell
dotnet restore
dotnet build
```

## API Surface

Current controller groups include:

- `api/Auth`
- `api/Users`
- `api/Job`
- `api/Customers`
- `api/Vehicles`
- `api/VehicleInspections`
- `api/Services`
- `api/JobServices`
- `api/ExpenseItems`
- `api/Company`

Most business controllers require authentication.

## Testing

The repository includes an integration test project at `JMAPI.Tests`.

Run all tests:

```powershell
dotnet test JMAPI.Tests\JMAPI.Tests.csproj
```

List discovered tests:

```powershell
dotnet test JMAPI.Tests\JMAPI.Tests.csproj --list-tests
```

Show detailed per-test console output:

```powershell
dotnet test JMAPI.Tests\JMAPI.Tests.csproj --logger "console;verbosity=detailed"
```

For test project details, see `JMAPI.Tests/README.md`.

## Current Test Coverage

The initial integration suite currently covers:

- `AuthController`
- `UsersController`
- `ExpenseItemsController`
- `JobController`

The tests use a custom `WebApplicationFactory` and override the production SQL Server database with in-memory SQLite for repeatable test runs.

## Notes

- `JMAPI.Tests` is nested under the main project folder, so the main project explicitly excludes that folder from its compile items in `JMAPI.csproj`
- if you add new test folders under `JMAPI.Tests`, they will still be excluded correctly by that rule
- if you move the test project elsewhere, update the solution and any relative project references
