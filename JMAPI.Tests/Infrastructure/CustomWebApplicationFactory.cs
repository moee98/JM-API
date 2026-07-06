using JMAPI.Database;
using JMAPI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace JMAPI.Tests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DefaultPassword = "TestPassword123!";

    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());

        // Blank the SQL Server connection strings so the migrate-on-startup loop
        // in Program.cs skips them (it ignores whitespace-only entries). Tests run
        // entirely on the in-memory Sqlite connection below.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "",
                ["ConnectionStrings:TestConnection"] = "",
                // Locally this comes from user-secrets, which CI machines don't
                // have. Any 32+ char value works — tokens are only validated
                // against the same in-process key.
                ["JwtSettings:Key"] = "integration-test-signing-key-0123456789"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbConnection>();

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            // Create the schema up front: Program.cs seeds Identity roles during
            // startup, which happens before InitializeAsync runs.
            var schemaOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;
            using (var schemaContext = new AppDbContext(schemaOptions))
            {
                schemaContext.Database.EnsureCreated();
            }

            services.AddSingleton<DbConnection>(_ => _connection!);
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                var connection = serviceProvider.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Dispose();

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string userId = TestAuthHandler.DefaultUserId,
        string email = TestAuthHandler.DefaultEmail,
        string name = TestAuthHandler.DefaultName,
        params string[] roles)
    {
        await SeedIdentityUserAsync(userId, email, name, roles);

        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email);
        client.DefaultRequestHeaders.Add(TestAuthHandler.NameHeader, name);

        if (roles.Length > 0)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(",", roles));
        }

        return client;
    }

    public async Task SeedIdentityUserAsync(
        string userId,
        string email,
        string name,
        params string[] roles)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            user = new AppUser
            {
                Id = userId,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Name = name,
                Active = true
            };

            var createResult = await userManager.CreateAsync(user, DefaultPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to seed test user: {string.Join(", ", createResult.Errors.Select(x => x.Description))}");
            }
        }

        foreach (var role in roles.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to seed role '{role}': {string.Join(", ", roleResult.Errors.Select(x => x.Description))}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var addRoleResult = await userManager.AddToRoleAsync(user, role);
                if (!addRoleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to assign role '{role}': {string.Join(", ", addRoleResult.Errors.Select(x => x.Description))}");
                }
            }
        }
    }

    public async Task ExecuteScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    public async Task<T> ExecuteScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    public Task<int> SeedExpenseCategoryAsync(string name = "General")
    {
        return ExecuteScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<AppDbContext>();
            var category = new ExpenseCategory
            {
                Name = name
            };

            dbContext.ExpenseCategory.Add(category);
            await dbContext.SaveChangesAsync();
            return category.Id;
        });
    }

    public Task<(int CustomerId, int VehicleId, int ServiceId)> SeedJobDependenciesAsync()
    {
        return ExecuteScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<AppDbContext>();

            var customer = new Customer
            {
                Name = "Test Customer",
                Email = "customer@example.com",
                PhoneNumber = "01234567890"
            };

            var vehicle = new Vehicle
            {
                Make = "Toyota",
                Model = "Corolla",
                LicensePlate = $"TEST-{Guid.NewGuid():N}"[..12],
                Colour = "Blue"
            };

            var service = new Service
            {
                Name = $"Wash-{Guid.NewGuid():N}"[..12],
                Description = "Exterior wash",
                EstimatedPrice = 25
            };

            dbContext.Customers.Add(customer);
            dbContext.Vehicles.Add(vehicle);
            dbContext.Services.Add(service);
            await dbContext.SaveChangesAsync();

            return (customer.Id, vehicle.Id, service.Id);
        });
    }

    public Task<int> SeedJobAsync(
        string appUserId,
        int customerId,
        int vehicleId,
        int serviceId)
    {
        return ExecuteScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<AppDbContext>();
            var job = new Job
            {
                Description = "Initial job",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = "Pending",
                DueDate = DateTime.UtcNow.AddDays(1),
                Notes = "Initial notes",
                IsActive = true,
                Paid = false,
                ServiceCharge = 100,
                VehicleId = vehicleId,
                CustomerId = customerId,
                AppUserId = appUserId,
                JobServices =
                {
                    new JobServices
                    {
                        ServiceId = serviceId,
                        Price = 25,
                        Completed = false
                    }
                }
            };

            dbContext.Jobs.Add(job);
            await dbContext.SaveChangesAsync();
            return job.Id;
        });
    }
}
