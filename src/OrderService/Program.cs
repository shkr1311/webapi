using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderService.Data;
using OrderService.Mappings;
using OrderService.Middleware;
using OrderService.Repositories;
using OrderService.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// Database Configuration
// ──────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<OrderDbContext>(options =>
        options.UseInMemoryDatabase("OrderDb"));
}
else
{
    builder.Services.AddDbContext<OrderDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// ──────────────────────────────────────────────
// Dependency Injection
// ──────────────────────────────────────────────
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderServiceImpl>();

// ──────────────────────────────────────────────
// HttpClient for Inter-Service Communication
// ──────────────────────────────────────────────
var productServiceUrl = builder.Configuration["ServiceUrls:ProductService"] ?? "http://localhost:5001";
var employeeServiceUrl = builder.Configuration["ServiceUrls:EmployeeService"] ?? "http://localhost:5002";

builder.Services.AddHttpClient("ProductService", client =>
{
    client.BaseAddress = new Uri(productServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("EmployeeService", client =>
{
    client.BaseAddress = new Uri(employeeServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ──────────────────────────────────────────────
// AutoMapper
// ──────────────────────────────────────────────
builder.Services.AddAutoMapper(typeof(MappingProfile));

// ──────────────────────────────────────────────
// JWT Authentication
// ──────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"] ?? "CODMicroservicesSecretKey2024!@#$%^&*";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CODMicroservices";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CODMicroservicesClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// ──────────────────────────────────────────────
// Controllers & Swagger
// ──────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Order Service API",
        Version = "v1",
        Description = "COD Microservices - Order Service (orchestrates Product & Employee services)"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// ──────────────────────────────────────────────
// Health Checks
// ──────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>("database");

// ──────────────────────────────────────────────
// CORS
// ──────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// ──────────────────────────────────────────────
// Middleware Pipeline
// ──────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service V1");
    c.RoutePrefix = string.Empty;
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
