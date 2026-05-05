using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using EmployeeService.Data;
using EmployeeService.Mappings;
using EmployeeService.Middleware;
using EmployeeService.Repositories;
using EmployeeService.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// Database Configuration
// ──────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<EmployeeDbContext>(options =>
        options.UseInMemoryDatabase("EmployeeDb"));
}
else
{
    builder.Services.AddDbContext<EmployeeDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// ──────────────────────────────────────────────
// Dependency Injection
// ──────────────────────────────────────────────
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeServiceImpl>();

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
        Title = "Employee Service API",
        Version = "v1",
        Description = "COD Microservices - Employee Service"
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
    .AddDbContextCheck<EmployeeDbContext>("database");

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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Service V1");
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
    var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
