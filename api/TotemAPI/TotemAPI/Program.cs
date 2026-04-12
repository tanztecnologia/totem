using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using TotemAPI.Features.Cart.Application.Abstractions;
using TotemAPI.Features.Cart.Application.UseCases;
using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Application.UseCases;
using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Application.UseCases;
using TotemAPI.Features.Checkout.Infrastructure;
using TotemAPI.Features.Identity.Application.Abstractions;
using TotemAPI.Features.Identity.Application.UseCases;
using TotemAPI.Features.Identity.Domain;
using TotemAPI.Features.Identity.Infrastructure;
using TotemAPI.Features.Kitchen.Application.UseCases;
using TotemAPI.Features.Kitchen.Application.Abstractions;
using TotemAPI.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

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

var localDb = builder.Configuration.GetConnectionString("LocalDb") ?? "Data Source=totem.local.db";
builder.Services.AddDbContext<TotemDbContext>(options => options.UseSqlite(localDb));

builder.Services.AddHttpClient();

builder.Services.AddScoped<ITenantRepository, EfTenantRepository>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ISkuRepository, EfSkuRepository>();
builder.Services.AddScoped<ICheckoutRepository, EfCheckoutRepository>();
builder.Services.AddScoped<ICartRepository, EfCartRepository>();
builder.Services.AddScoped<IKitchenSlaRepository, EfKitchenSlaRepository>();
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
builder.Services.AddScoped<ListSkus>();
builder.Services.AddScoped<UpdateSku>();
builder.Services.AddScoped<DeleteSku>();
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

    if (app.Environment.IsDevelopment())
    {
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var tenant = db.Tenants.FirstOrDefault(x => x.NormalizedName == "EMPRESA X");
        if (tenant is not null)
        {
            var createdAt = DateTimeOffset.UtcNow;

            var adminEmail = "admin@empresax.local";
            var adminNormalizedEmail = adminEmail.Trim().ToLowerInvariant();
            if (!db.Users.Any(x => x.TenantId == tenant.Id && x.NormalizedEmail == adminNormalizedEmail))
            {
                db.Users.Add(
                    new UserRow
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenant.Id,
                        Email = adminEmail,
                        NormalizedEmail = adminNormalizedEmail,
                        PasswordHash = passwordHasher.Hash("123456"),
                        Role = UserRole.Admin,
                        CreatedAt = createdAt
                    }
                );
            }

            var staffEmail = "staff@empresax.local";
            var staffNormalizedEmail = staffEmail.Trim().ToLowerInvariant();
            if (!db.Users.Any(x => x.TenantId == tenant.Id && x.NormalizedEmail == staffNormalizedEmail))
            {
                db.Users.Add(
                    new UserRow
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenant.Id,
                        Email = staffEmail,
                        NormalizedEmail = staffNormalizedEmail,
                        PasswordHash = passwordHasher.Hash("123456"),
                        Role = UserRole.Staff,
                        CreatedAt = createdAt
                    }
                );
            }

            db.SaveChanges();
        }
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
