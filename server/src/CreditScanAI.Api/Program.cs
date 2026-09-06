using System.Text;
using System.Threading.RateLimiting;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Middleware;
using CreditScanAI.Api.Services;
using CreditScanAI.Classification;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using CreditScanAI.Infrastructure.Persistence.Seed;
using CreditScanAI.PdfPipeline;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Serilog;

// Fase 10: licença Community, gratuita para o porte deste projeto (o pacote
// exige declarar isso explicitamente antes de gerar qualquer PDF).
QuestPDF.Settings.License = LicenseType.Community;

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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenantProvider, CurrentTenantProvider>();
builder.Services.AddScoped<FinancialCalculationService>();
builder.Services.AddScoped<ConsolidationService>();

builder.Services.AddClassificationEngine(builder.Configuration);
builder.Services.AddScoped<IClassificationHistoryProvider, EfClassificationHistoryProvider>();
builder.Services.AddScoped<ICrossCompanyPatternProvider, EfCrossCompanyPatternProvider>();
builder.Services.AddSingleton<IClassificationProcessingQueue, ClassificationProcessingQueue>();
builder.Services.AddScoped<ClassificationProcessingService>();
builder.Services.AddHostedService<ClassificationProcessingBackgroundService>();

// Fase 8: autenticação real. AddIdentityCore (não AddIdentity) - é a
// variante enxuta para APIs, sem os pressupostos de cookie/Razor Pages da
// versão completa. Só o user store é registrado (AddEntityFrameworkStores) -
// não há RoleManager/AspNetRoles em uso, papéis são o enum UserRole simples.
builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();

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

        // Sem isso, uma requisição sem token (401) ou sem o papel exigido
        // (403) volta com corpo vazio - quebra o parser do frontend, que
        // espera o envelope ApiResponse<T> em toda resposta, erro ou não.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("UNAUTHORIZED", "Autenticação necessária."));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("FORBIDDEN", "Você não tem permissão para executar esta ação."));
            }
        };
    });

// Fase 8 Parte 2: aplica a matriz de permissões de 10_CASOS_DE_USO.md seção
// 5 (Analyst/Reviewer/CFO/Admin/Compliance x Login/Upload/Review/Consolidate/
// Export/Admin). "Export" (leitura) não tem policy própria - qualquer
// usuário autenticado já cobre isso via FallbackPolicy, então só as ações de
// escrita/processamento mais restritas ganham uma policy nomeada.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(AuthorizationPolicies.CanUpload, p => p.RequireRole(nameof(UserRole.Analyst), nameof(UserRole.Admin)))
    .AddPolicy(AuthorizationPolicies.CanReview, p => p.RequireRole(nameof(UserRole.Analyst), nameof(UserRole.Reviewer), nameof(UserRole.Admin)))
    .AddPolicy(AuthorizationPolicies.CanConsolidate, p => p.RequireRole(nameof(UserRole.Analyst), nameof(UserRole.Admin)))
    .AddPolicy(AuthorizationPolicies.CanAdmin, p => p.RequireRole(nameof(UserRole.Admin)))
    .AddPolicy(AuthorizationPolicies.CanAudit, p => p.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Compliance)));

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
