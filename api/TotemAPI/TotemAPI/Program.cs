using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TotemAPI.Infrastructure.Logging;
using TotemAPI.Infrastructure.Telemetry;
using TotemAPI.Features.Cart.Application.Abstractions;
using TotemAPI.Features.Cart.Application.UseCases;
using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Application.UseCases;
using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Application.UseCases;
using TotemAPI.Features.Checkout.Infrastructure;
using TotemAPI.Features.Dashboard.Application.Abstractions;
using TotemAPI.Features.Dashboard.Application.UseCases;
using TotemAPI.Features.Identity.Application.Abstractions;
using TotemAPI.Features.Identity.Application.UseCases;
using TotemAPI.Features.Identity.Domain;
using TotemAPI.Features.Identity.Infrastructure;
using TotemAPI.Features.Kitchen.Application.UseCases;
using TotemAPI.Features.Kitchen.Application.Abstractions;
using TotemAPI.Features.Pos.Application.Abstractions;
using TotemAPI.Features.Pos.Application.UseCases;
using TotemAPI.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddProvider(new JsonLineLoggerProvider(Console.WriteLine));
var logFilePath = builder.Configuration["Logging:File:Path"];
if (!string.IsNullOrWhiteSpace(logFilePath))
{
    builder.Logging.AddProvider(new JsonFileLoggerProvider(logFilePath));
}

// Add services to the container.

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<TefApiOptions>(builder.Configuration.GetSection(TefApiOptions.SectionName));

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "DefaultCors",
        policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
                return;
            }
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    );
});

var localDb = builder.Configuration.GetConnectionString("LocalDb") ?? "Data Source=totem.local.db";
builder.Services.AddDbContext<TotemDbContext>(options => options.UseSqlite(localDb));

builder.Services.AddHttpClient();

builder.Services.AddScoped<ITenantRepository, EfTenantRepository>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ISkuRepository, EfSkuRepository>();
builder.Services.AddScoped<ICategoryRepository, EfCategoryRepository>();
builder.Services.AddScoped<ICheckoutRepository, EfCheckoutRepository>();
builder.Services.AddScoped<ICartRepository, EfCartRepository>();
builder.Services.AddScoped<IKitchenSlaRepository, EfKitchenSlaRepository>();
builder.Services.AddScoped<ICashRegisterRepository, EfCashRegisterRepository>();
builder.Services.AddScoped<IDashboardRepository, EfDashboardRepository>();
builder.Services.AddSingleton<ITefPaymentService>(sp =>
{
    var options = sp.GetRequiredService<IOptions<TefApiOptions>>().Value;
    if (string.Equals(options.Mode, "Api", StringComparison.OrdinalIgnoreCase))
    {
        var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
        return new HttpTefPaymentService(http, Options.Create(options));
    }
    return new FakeTefPaymentService();
});

builder.Services.AddScoped<RegisterUser>();
builder.Services.AddScoped<LoginUser>();
builder.Services.AddScoped<CreateUser>();
builder.Services.AddScoped<CreateSku>();
builder.Services.AddScoped<GetSku>();
builder.Services.AddScoped<GetSkuByCode>();
builder.Services.AddScoped<SearchSkusPage>();
builder.Services.AddScoped<ListSkus>();
builder.Services.AddScoped<UpdateSku>();
builder.Services.AddScoped<DeleteSku>();
builder.Services.AddScoped<AddSkuStockEntry>();
builder.Services.AddScoped<ListSkuStockConsumptions>();
builder.Services.AddScoped<ReplaceSkuStockConsumptions>();
builder.Services.AddScoped<ListSkuStockLedger>();
builder.Services.AddScoped<CreateCategory>();
builder.Services.AddScoped<ListCategories>();
builder.Services.AddScoped<GetCategoryByCode>();
builder.Services.AddScoped<UpdateCategory>();
builder.Services.AddScoped<DeleteCategory>();
builder.Services.AddScoped<StartCheckout>();
builder.Services.AddScoped<ConfirmPayment>();
builder.Services.AddScoped<GetOrder>();
builder.Services.AddScoped<CreateCart>();
builder.Services.AddScoped<GetCart>();
builder.Services.AddScoped<SetCartItem>();
builder.Services.AddScoped<ClearCart>();

builder.Services.AddScoped<ListKitchenOrders>();
builder.Services.AddScoped<GetKitchenOrder>();
builder.Services.AddScoped<UpdateKitchenOrderStatus>();
builder.Services.AddScoped<GetKitchenSla>();
builder.Services.AddScoped<UpsertKitchenSla>();

builder.Services.AddScoped<ListPosOrdersByComanda>();
builder.Services.AddScoped<PayPosOrder>();
builder.Services.AddScoped<GetCurrentCashRegisterShift>();
builder.Services.AddScoped<OpenCashRegisterShift>();
builder.Services.AddScoped<CloseCashRegisterShift>();

builder.Services.AddScoped<GetDashboardOverview>();
builder.Services.AddScoped<ListDashboardOrders>();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var issuer = jwtSection.GetValue<string>("Issuer") ?? string.Empty;
var audience = jwtSection.GetValue<string>("Audience") ?? string.Empty;
var key = jwtSection.GetValue<string>("Key") ?? string.Empty;
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(15),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(
            serviceName: TotemActivitySource.ServiceName,
            serviceVersion: TotemActivitySource.ServiceVersion)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName.ToLowerInvariant(),
        }))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(TotemActivitySource.ServiceName)
            .AddAspNetCoreInstrumentation(o => o.RecordException = true)
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation(o => o.SetDbStatementForText = true);

        var otlpEndpoint = builder.Configuration["Otel:Endpoint"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        }

        if (builder.Environment.IsDevelopment())
        {
            var consoleOtel = builder.Configuration.GetValue<bool>("Otel:ConsoleExporter");
            if (consoleOtel)
            {
                tracing.AddConsoleExporter();
            }
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter();
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TotemDbContext>();
    db.Database.Migrate();
}

app.UseCors("DefaultCors");
app.UseAuthentication();
app.Use(
    async (context, next) =>
    {
        var tenantId = context.User.FindFirstValue("tenant_id") ?? "-";
        var userId =
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub")
            ?? "-";

        using (app.Logger.BeginScope(new Dictionary<string, object?> { ["tenant_id"] = tenantId, ["user_id"] = userId }))
        {
            await next();
        }
    }
);
app.Use(
    async (context, next) =>
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await next();
            sw.Stop();

            var tenantId = context.User.FindFirstValue("tenant_id") ?? "-";
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub") ?? "-";
            var rawEmail = context.User.FindFirstValue(ClaimTypes.Email) ?? context.User.FindFirstValue("email");
            var maskedEmail = rawEmail is null ? "-" : PiiMasker.MaskEmail(rawEmail);

            app.Logger.LogInformation(
                "HTTP {Method} {Path}{Query} => {StatusCode} in {ElapsedMs}ms tenant={TenantId} userId={UserId} email={MaskedEmail}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                tenantId,
                userId,
                maskedEmail
            );
        }
        catch (Exception ex)
        {
            sw.Stop();
            app.Logger.LogError(
                ex,
                "HTTP {Method} {Path}{Query} FAILED in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                sw.ElapsedMilliseconds
            );
            throw;
        }
    }
);
app.UseAuthorization();

app.MapPrometheusScrapingEndpoint("/metrics");

app.MapControllers();

app.Run();
