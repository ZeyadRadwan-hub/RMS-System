using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using RMS_BACKEND.Data;
using RMS_BACKEND.Models;
using RMS_BACKEND.Repositories;
using RMS_BACKEND.Security;
using RMS_BACKEND.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddAuthentication(BearerSessionHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, BearerSessionHandler>(BearerSessionHandler.SchemeName, _ => { });
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().Build());
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<LoginAttemptGuard>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

// Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repositories
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// Register Services
builder.Services.AddScoped<ILeaveCalculationService, LeaveCalculationService>();
builder.Services.AddScoped<IRequestStateMachineService, RequestStateMachineService>();
builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<TransactionRequestValidator>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Browser origins are explicit; environment-specific deployment origins override this list.
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? Array.Empty<string>();
    options.AddPolicy("RmsFrontend", policy =>
    {
        policy.WithOrigins(origins)
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Content-Type", "Authorization");
    });
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (args.Contains("--provision-initial-hr", StringComparer.Ordinal))
{
    await InitialAdminProvisioner.RunAsync(app.Services);
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RMS API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("RmsFrontend");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
