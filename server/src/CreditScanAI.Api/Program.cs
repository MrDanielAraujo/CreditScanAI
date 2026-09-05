using System.Text;
using System.Threading.RateLimiting;
using CreditScanAI.Api.Middleware;
using CreditScanAI.Api.Services;
using CreditScanAI.Classification;
using CreditScanAI.Infrastructure.Persistence;
using CreditScanAI.Infrastructure.Persistence.Seed;
using CreditScanAI.PdfPipeline;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddPdfPipeline();
builder.Services.Configure<DocumentStorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton<IDocumentStorage, LocalDiskDocumentStorage>();
builder.Services.AddSingleton<IDocumentProcessingQueue, DocumentProcessingQueue>();
builder.Services.AddScoped<DocumentProcessingService>();
builder.Services.AddHostedService<DocumentProcessingBackgroundService>();
builder.Services.AddScoped<ICurrentTenantProvider, CurrentTenantProvider>();

builder.Services.AddClassificationEngine(builder.Configuration);
builder.Services.AddSingleton<IClassificationProcessingQueue, ClassificationProcessingQueue>();
builder.Services.AddScoped<ClassificationProcessingService>();
builder.Services.AddHostedService<ClassificationProcessingBackgroundService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key não configurada.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

const string FrontendCorsPolicy = "Frontend";
builder.Services.AddCors(options =>
{
    // Dev-only: the Vite dev server's origin. Revisit with a real
    // per-environment allowlist when this goes beyond local development.
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var rateLimitSection = builder.Configuration.GetSection("RateLimiting");
var permitLimit = rateLimitSection.GetValue("PermitLimit", 1000);
var windowMinutes = rateLimitSection.GetValue("WindowMinutes", 60);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(windowMinutes),
                QueueLimit = 0
            }));
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

namespace CreditScanAI.Api
{
    public partial class Program;
}
