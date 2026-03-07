# JMAPI.Tests

## Overview

`JMAPI.Tests` contains integration tests for the main API.

The project uses:

- `xUnit`
- `FluentAssertions`
- `Microsoft.AspNetCore.Mvc.Testing`
- `Microsoft.AspNetCore.TestHost`
- `Microsoft.EntityFrameworkCore.Sqlite`

These tests exercise the real ASP.NET Core request pipeline rather than calling controllers directly.

## Test Strategy

The current test setup is integration-first.

That means:

- the application is booted with `WebApplicationFactory<Program>`
- the production SQL Server `AppDbContext` registration is replaced with SQLite in-memory
- protected endpoints use a fake authentication scheme for most controller tests
- Identity users and roles are seeded into the test database where needed

This gives better coverage than direct unit tests for controller behavior, routing, auth, model binding, and EF persistence.

## Project Layout

- `Controllers/`: endpoint-level integration tests
- `Infrastructure/CustomWebApplicationFactory.cs`: test host and database override
- `Infrastructure/TestAuthHandler.cs`: fake auth handler for protected endpoints
- `GlobalUsings.cs`: shared xUnit global using

## Current Tests

Current integration coverage includes:

- `AuthControllerTests`
- `UsersControllerTests`
- `ExpenseItemsControllerTests`
- `JobControllerTests`

## Running Tests

From the repository root:

```powershell
dotnet test JMAPI.Tests\JMAPI.Tests.csproj
```

To see each test name:

```powershell
dotnet test JMAPI.Tests\JMAPI.Tests.csproj --logger "console;verbosity=detailed"
```

To list all discovered tests without running them:

```powershell
dotnet test JMAPI.Tests\JMAPI.Tests.csproj --list-tests
```

## Test Infrastructure

### CustomWebApplicationFactory

`CustomWebApplicationFactory` is responsible for:

- booting the app in the `Development` environment
- clearing logging providers that are noisy or unsuitable for the test host
- replacing SQL Server with SQLite in-memory
- seeding test users, roles, categories, and job dependencies

### TestAuthHandler

`TestAuthHandler` provides a fake authentication scheme named `Test`.

It reads optional request headers:

- `X-Test-UserId`
- `X-Test-Email`
- `X-Test-Name`
- `X-Test-Roles`

If headers are not supplied, default values are used.

This is useful for controller tests that need authenticated requests without going through the login flow.

## Adding New Tests

Recommended pattern:

1. Add a new test file under `Controllers/` or `Services/`
2. Reuse `CustomWebApplicationFactory`
3. Seed only the minimum data required for the scenario
4. Prefer asserting both HTTP response behavior and persisted database state

Examples already in the repo:

- auth tests that use the real controller endpoints
- protected endpoint tests that use `CreateAuthenticatedClientAsync(...)`
- job tests that seed customer, vehicle, service, and job records before exercising the API

## Practical Guidelines

- prefer integration tests for controller behavior
- keep test data small and explicit
- avoid sharing mutable test state between tests
- use descriptive test names in the `Method_Scenario_ExpectedResult` style already used in this project

## Known Constraints

- the test project lives inside the main repository root under `JMAPI.Tests`
- the main API project explicitly excludes `JMAPI.Tests/**` from compile items to prevent cross-compilation
- if tests begin to cover more service-only logic, a separate unit-test layer can be added later without changing the current integration setup
