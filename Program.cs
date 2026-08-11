using JMAPI.Database;
using JMAPI.Interfaces;
using JMAPI.Models;
using JMAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using Swashbuckle.AspNetCore.Swagger;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
// ✅ REMOVE Microsoft Identity Web if not needed

// Registered as a singleton so the "which database is active" flag is
// shared across all requests/users - see ActiveDatabaseProvider.
builder.Services.AddSingleton<IActiveDatabaseProvider, ActiveDatabaseProvider>();

// The (serviceProvider, options) overload is re-invoked for every scoped
// AppDbContext (i.e. every request), so toggling ActiveDatabaseProvider's
// mode takes effect on the very next request - no restart required.
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var activeDatabase = serviceProvider.GetRequiredService<IActiveDatabaseProvider>();
    var connectionStringName = activeDatabase.IsTestMode ? "TestConnection" : "DefaultConnection";
    var connectionString = builder.Configuration.GetConnectionString(connectionStringName)
        ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' is not configured.");
    options.UseSqlServer(connectionString);
});

builder.Services.AddControllers()
     .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
     }); ;

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", "http://127.0.0.1:5173",
                "http://192.168.1.107:5173",
                "http://localhost",                          // Docker
                "http://kazadashboard", "https://kazadashboard")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // for cookies or auth headers
    });
});

builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = config["JwtSettings:Issuer"],
        ValidAudience = config["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSettings:Key"]!))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Read token from cookie if available
            if (context.Request.Cookies.ContainsKey("jwt"))
            {
                context.Token = context.Request.Cookies["jwt"];
            }
            return Task.CompletedTask;
        }
    };
});


builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IJobServicesService, JobServicesService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<RefreshTokenService>();

// Register the expense items service used by ExpenseItemsController
builder.Services.AddScoped<IExpenseItemsService, ExpenseItemsService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IIntegrationService, IntegrationService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var app = builder.Build();

// Apply pending EF Core migrations on startup, creating the database if it
// doesn't exist yet. Both the live and test databases are migrated
// regardless of which is currently active, so switching between them is
// always safe (the test DB might not have been touched in a while).
foreach (var connectionStringName in new[] { "DefaultConnection", "TestConnection" })
{
    var connectionString = config.GetConnectionString(connectionStringName);
    if (string.IsNullOrWhiteSpace(connectionString)) continue;

    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer(connectionString)
        .Options;
    using var db = new AppDbContext(options);
    db.Database.Migrate();
}

using (var scope = app.Services.CreateScope())
{
    // Seed the Admin/User roles and, if nobody holds the Admin role yet,
    // grant it to a known bootstrap account so a fresh install has one
    // usable admin without a manual database step.
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    foreach (var roleName in new[] { "Admin", "User" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var existingAdmins = await userManager.GetUsersInRoleAsync("Admin");
    if (existingAdmins.Count == 0)
    {
        var bootstrapAdminEmail = config["Bootstrap:AdminEmail"];
        if (!string.IsNullOrWhiteSpace(bootstrapAdminEmail))
        {
            var bootstrapAdmin = await userManager.FindByEmailAsync(bootstrapAdminEmail);
            if (bootstrapAdmin != null)
            {
                await userManager.AddToRoleAsync(bootstrapAdmin, "Admin");
            }
        }
    }
}

app.UseCors("AllowFrontend");

// Return unhandled exception details as JSON so they're visible in the API response
// (not just docker logs) while the team is debugging the dockerized deployment.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var ex = feature?.Error;

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = ex?.Message,
            type = ex?.GetType().FullName,
            stackTrace = ex?.StackTrace
        });
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); //  Needed before UseAuthorization
app.UseAuthorization();



app.MapControllers();

app.Run();

public partial class Program { }
