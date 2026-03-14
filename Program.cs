using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using swd.Application.Facades;
using swd.Application.Services;
using swd.Domain.Interfaces;
using swd.Infrastructure.Persistence;
using swd.Infrastructure.Repositories;
using swd.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SWD API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhap JWT token. Vi du: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

builder.Services.Configure<GeminiSettings>(
    builder.Configuration.GetSection("GeminiSettings"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = builder.Configuration
        .GetSection("MongoDbSettings")
        .Get<MongoDbSettings>();
    return new MongoClient(settings!.ConnectionString);
});

builder.Services.AddScoped<MongoDbContext>();

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();

builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<PromotionService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<TokenRevocationService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<CartComboService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CheckoutCatalogService>();
builder.Services.AddScoped<CheckoutCatalogSeedService>();
builder.Services.AddHttpClient<GeminiRecommendationService>();

builder.Services.AddScoped<CheckoutFacade>();

var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>()!;

var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? context.Principal?.FindFirstValue("sub")
                         ?? context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(userId))
            {
                context.Fail("Invalid token.");
                return;
            }

            var issuedAtValue = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Iat);
            if (!long.TryParse(issuedAtValue, out var issuedAtSeconds))
            {
                context.Fail("Invalid token.");
                return;
            }

            var issuedAtUtc = DateTimeOffset.FromUnixTimeSeconds(issuedAtSeconds).UtcDateTime;
            var tokenRevocationService = context.HttpContext.RequestServices.GetRequiredService<TokenRevocationService>();
            var isRevoked = await tokenRevocationService.IsTokenRevokedAsync(userId, issuedAtUtc);
            if (isRevoked)
            {
                context.Fail("Token has been revoked.");
            }
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"message\": \"Ban can dang nhap de truy cap.\"}");
        },
        OnForbidden = context =>
        {
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"message\": \"Ban khong co quyen truy cap vao tai nguyen nay.\"}");
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "https://localhost:3000",
                "https://localhost:5173"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var checkoutCatalogSeedService = scope.ServiceProvider.GetRequiredService<CheckoutCatalogSeedService>();
    await checkoutCatalogSeedService.SeedAsync();
}

var enableSwagger = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled");
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var staticFileContentTypeProvider = new FileExtensionContentTypeProvider();
staticFileContentTypeProvider.Mappings[".webp"] = "image/webp";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = staticFileContentTypeProvider
});

// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();



