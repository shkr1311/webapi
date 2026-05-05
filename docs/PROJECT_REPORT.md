# COD Microservices — End-to-End Project Report

## Table of Contents
1. [Project Overview](#1-project-overview)
2. [Architecture Design](#2-architecture-design)
3. [Technology Stack](#3-technology-stack)
4. [Project Structure](#4-project-structure)
5. [Service-by-Service Implementation](#5-service-by-service-implementation)
6. [Clean Architecture Pattern](#6-clean-architecture-pattern)
7. [Database Design](#7-database-design)
8. [Inter-Service Communication](#8-inter-service-communication)
9. [Authentication & Security](#9-authentication--security)
10. [Middleware Pipeline](#10-middleware-pipeline)
11. [Business Logic Flow](#11-business-logic-flow)
12. [API Reference](#12-api-reference)
13. [Docker & Containerization](#13-docker--containerization)
14. [AWS Deployment](#14-aws-deployment)
15. [Testing Results](#15-testing-results)
16. [Advanced Concepts Used](#16-advanced-concepts-used)

---

## 1. Project Overview

### What is this project?
A **Cash On Delivery (COD) Proof of Concept** — a system where customers can place orders, a delivery employee is auto-assigned, the order gets delivered, and payment is collected as cash on delivery.

### Why Microservices?
Instead of one big application (monolith), we split into **3 independent services**:

| Service | Responsibility | Port |
|---------|---------------|------|
| **Product Service** | Manage products (add, list, validate) | 5001 |
| **Employee Service** | Manage delivery employees & availability | 5002 |
| **Order Service** | Orchestrate orders, delivery, payment | 5003 |

### Benefits of this approach:
- **Independent deployment** — Update Product Service without touching Order Service
- **Independent scaling** — If orders spike, scale only Order Service
- **Technology freedom** — Each service could use different tech (we use .NET 8 for all)
- **Fault isolation** — If Employee Service goes down, Product Service still works

---

## 2. Architecture Design

### High-Level Architecture
```
┌─────────────────────────────────────────────────────────┐
│                    CLIENT / POSTMAN                       │
└────────┬──────────────┬──────────────┬──────────────────┘
         │              │              │
    ┌────▼────┐   ┌─────▼─────┐  ┌────▼────┐
    │ Product │   │  Employee  │  │  Order  │
    │ Service │   │  Service   │  │ Service │
    │  :5001  │   │   :5002    │  │  :5003  │
    └────┬────┘   └─────┬─────┘  └────┬────┘
         │              │              │
         │              │     ┌────────┤
         │              │     │ HTTP   │ HTTP
         │              │◄────┘ calls  │ calls
         │◄────────────────────────────┘
         │              │              │
    ┌────▼────┐   ┌─────▼─────┐  ┌────▼────┐
    │ProductDb│   │EmployeeDb │  │ OrderDb │
    └─────────┘   └───────────┘  └─────────┘
```

### Communication Pattern
- **Synchronous REST** — Order Service calls Product & Employee services via HTTP
- **Each service has its own database** — No shared database (Database per Service pattern)

---

## 3. Technology Stack

| Technology | Purpose | Version |
|-----------|---------|---------|
| .NET 8 | Web API framework | 8.0 |
| Entity Framework Core | ORM / Database access | 8.0 |
| EF Core InMemory | Development database | 8.0 |
| EF Core SQL Server | Production database | 8.0 |
| JWT Bearer | Authentication | 8.0 |
| AutoMapper | Object-to-object mapping | 12.0 |
| Swashbuckle | Swagger / OpenAPI docs | 6.5 |
| HttpClientFactory | Inter-service HTTP calls | 8.0 |
| Docker | Containerization | Latest |

---

## 4. Project Structure

```
d:\placemet\webapi\
│
├── CodMicroservices.sln          # Solution file (links all projects)
├── NuGet.Config                  # Package source configuration
├── docker-compose.yml            # Multi-container Docker setup
├── .gitignore                    # Files excluded from Git
├── README.md                     # Project documentation
│
├── src/
│   ├── ProductService/           # === PRODUCT MICROSERVICE ===
│   │   ├── Models/
│   │   │   └── Product.cs        # Entity: Id, Name, Price, Stock
│   │   ├── DTOs/
│   │   │   └── ProductDto.cs     # Data Transfer Objects
│   │   ├── Data/
│   │   │   └── ProductDbContext.cs  # EF Core database context
│   │   ├── Repositories/
│   │   │   ├── IProductRepository.cs  # Interface (contract)
│   │   │   └── ProductRepository.cs   # Implementation (data access)
│   │   ├── Services/
│   │   │   ├── IProductService.cs     # Interface (contract)
│   │   │   └── ProductServiceImpl.cs  # Implementation (business logic)
│   │   ├── Controllers/
│   │   │   ├── ProductsController.cs  # API endpoints
│   │   │   └── AuthController.cs      # JWT token generation
│   │   ├── Mappings/
│   │   │   └── MappingProfile.cs      # AutoMapper configuration
│   │   ├── Middleware/
│   │   │   ├── ExceptionMiddleware.cs       # Global error handling
│   │   │   └── RequestLoggingMiddleware.cs  # Request/response logging
│   │   ├── Program.cs            # Entry point + Dependency Injection
│   │   ├── appsettings.json      # Configuration
│   │   └── Dockerfile            # Container image definition
│   │
│   ├── EmployeeService/          # === EMPLOYEE MICROSERVICE ===
│   │   └── (same structure as ProductService)
│   │
│   └── OrderService/             # === ORDER MICROSERVICE ===
│       └── (same structure + HttpClient for inter-service calls)
│
└── docs/
    ├── api-endpoints.md          # Full API reference
    ├── aws-deployment-guide.md   # AWS deployment steps
    ├── PROJECT_REPORT.md         # This report
    └── CodMicroservices.postman_collection.json  # Postman collection
```

### Why this structure?
Each layer has a single responsibility:
- **Models** → Define what data looks like
- **DTOs** → Define what API sends/receives (never expose raw models)
- **Repositories** → Talk to database only
- **Services** → Business logic only
- **Controllers** → Handle HTTP requests only
- **Middleware** → Cross-cutting concerns (logging, errors)

---

## 5. Service-by-Service Implementation

### 5.1 Product Service (Port 5001)

**Purpose:** Manage the product catalog

**Model — Product.cs:**
```csharp
public class Product
{
    public int Id { get; set; }          // Auto-generated primary key
    public string Name { get; set; }      // Product name
    public decimal Price { get; set; }    // Unit price
    public int Stock { get; set; }        // Available quantity
    public DateTime CreatedAt { get; set; }
}
```

**Endpoints:**
| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| GET | `/api/products` | List all products | No |
| GET | `/api/products/{id}` | Get one product | No |
| POST | `/api/products` | Add product | Yes |
| PUT | `/api/products/{id}` | Update product | Yes |
| DELETE | `/api/products/{id}` | Delete product | Yes |

**Key Design Decisions:**
- GET endpoints are public (other services need to validate products)
- Write endpoints require JWT authentication
- Uses AutoMapper to convert between `Product` ↔ `ProductDto`

---

### 5.2 Employee Service (Port 5002)

**Purpose:** Manage delivery employees and their availability

**Model — Employee.cs:**
```csharp
public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Phone { get; set; }
    public bool IsAvailable { get; set; } = true;  // Defaults to available
    public DateTime CreatedAt { get; set; }
}
```

**Endpoints:**
| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| GET | `/api/employees` | List all | No |
| GET | `/api/employees/{id}` | Get one | No |
| GET | `/api/employees/available` | First available employee | No |
| POST | `/api/employees` | Add employee | Yes |
| PUT | `/api/employees/{id}/availability` | Toggle availability | No |

**Key Feature — `/available` endpoint:**
- Returns the **first available** employee (ordered by creation date)
- Used by Order Service to auto-assign delivery agents
- No auth required since Order Service calls it internally

---

### 5.3 Order Service (Port 5003)

**Purpose:** Orchestrate the entire COD flow

**Model — Order.cs:**
```csharp
public class Order
{
    public int Id { get; set; }
    public int ProductId { get; set; }         // References Product Service
    public int Quantity { get; set; }
    public int? EmployeeId { get; set; }       // References Employee Service
    public string Status { get; set; }          // Created → Delivered
    public string PaymentStatus { get; set; }   // Pending → Paid
    // Denormalized fields (cached from other services)
    public string? ProductName { get; set; }
    public decimal? ProductPrice { get; set; }
    public string? EmployeeName { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? PaidAt { get; set; }
}
```

**Why denormalized fields?**
Since microservices have separate databases, we store `ProductName`, `ProductPrice`, and `EmployeeName` directly in the order. This way we don't need to call other services just to display order details.

**Endpoints:**
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/orders` | Create order (calls Product + Employee services) |
| PUT | `/api/orders/{id}/deliver` | Mark delivered (releases employee) |
| PUT | `/api/orders/{id}/pay` | Mark COD payment collected |

---

## 6. Clean Architecture Pattern

Each service follows the same layered architecture:

```
┌─────────────────────────────┐
│       Controller Layer       │  ← Handles HTTP (routes, status codes)
│    ProductsController.cs     │
├─────────────────────────────┤
│        Service Layer         │  ← Business logic (validation, rules)
│    ProductServiceImpl.cs     │
├─────────────────────────────┤
│      Repository Layer        │  ← Data access (SQL queries via EF Core)
│    ProductRepository.cs      │
├─────────────────────────────┤
│       Database (EF Core)     │  ← Actual database
│     ProductDbContext.cs      │
└─────────────────────────────┘
```

### How data flows (Create Product example):

```
1. Client sends POST /api/products with JSON body
2. Controller receives CreateProductDto
3. Controller calls _productService.CreateProductAsync(dto)
4. Service uses AutoMapper to convert CreateProductDto → Product entity
5. Service calls _repository.CreateAsync(product)
6. Repository adds to DbContext and calls SaveChangesAsync()
7. Repository returns created Product entity
8. Service uses AutoMapper to convert Product → ProductDto
9. Controller returns 201 Created with ProductDto
```

### Why interfaces everywhere?
```csharp
// Interface (contract)
public interface IProductRepository { ... }

// Implementation
public class ProductRepository : IProductRepository { ... }

// Registration in Program.cs
builder.Services.AddScoped<IProductRepository, ProductRepository>();
```

**Benefits:**
- **Testability** — Swap real database with mock for unit tests
- **Loose coupling** — Controller doesn't know/care about database details
- **Swappability** — Change from SQL Server to MongoDB without touching business logic

---

## 7. Database Design

### Entity Framework Core Setup

**DbContext** defines the database structure:
```csharp
public class ProductDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            entity.HasIndex(p => p.Name);  // Index for faster searches
        });
    }
}
```

### Database Strategy

| Environment | Database | Configuration |
|-------------|----------|---------------|
| Development | **InMemory** | No setup needed, data resets on restart |
| Production | **SQL Server** | Set connection string in appsettings |

**Smart switching in Program.cs:**
```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
    options.UseInMemoryDatabase("ProductDb");   // Dev mode
else
    options.UseSqlServer(connectionString);      // Production
```

### Database Per Service
```
ProductDb   → Only Product Service can read/write
EmployeeDb  → Only Employee Service can read/write
OrderDb     → Only Order Service can read/write
```
No service directly accesses another service's database. They communicate via REST APIs.

---

## 8. Inter-Service Communication

### How Order Service talks to other services

**Registration (Program.cs):**
```csharp
builder.Services.AddHttpClient("ProductService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("EmployeeService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5002");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

**Usage (OrderServiceImpl.cs):**
```csharp
private async Task<ProductResponseDto?> ValidateProductAsync(int productId)
{
    var client = _httpClientFactory.CreateClient("ProductService");
    var response = await client.GetAsync($"/api/products/{productId}");

    if (!response.IsSuccessStatusCode) return null;

    var content = await response.Content.ReadAsStringAsync();
    var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductResponseDto>>(content);
    return apiResponse?.Data;
}
```

### Why HttpClientFactory?
- **Connection pooling** — Reuses TCP connections (no socket exhaustion)
- **Named clients** — Each service has its own configured client
- **Resilience** — Built-in timeout handling

### Communication Flow for Order Creation:
```
Order Service                   Product Service       Employee Service
     │                               │                      │
     │── GET /api/products/1 ───────►│                      │
     │◄── { product details } ───────│                      │
     │                               │                      │
     │── GET /api/employees/available ──────────────────────►│
     │◄── { available employee } ────────────────────────────│
     │                               │                      │
     │── PUT /api/employees/1/availability {false} ─────────►│
     │◄── { OK } ───────────────────────────────────────────│
     │                               │                      │
     │ [Create order in OrderDb]     │                      │
     │                               │                      │
```

---

## 9. Authentication & Security

### JWT (JSON Web Token) Flow

```
1. Client sends POST /api/auth/token with {username, password}
2. Server validates credentials
3. Server generates JWT token with claims (name, role, expiry)
4. Client includes token in all subsequent requests:
   Header: Authorization: Bearer eyJhbGciOi...
5. Server validates token on each [Authorize] endpoint
```

### JWT Configuration (Program.cs):
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,           // Check who created the token
            ValidateAudience = true,         // Check who it's meant for
            ValidateLifetime = true,         // Check expiry
            ValidateIssuerSigningKey = true,  // Verify signature
            ValidIssuer = "CODMicroservices",
            ValidAudience = "CODMicroservicesClient",
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });
```

### Token Structure (decoded):
```json
{
  "header": { "alg": "HS256", "typ": "JWT" },
  "payload": {
    "name": "admin",
    "role": "Admin",
    "jti": "unique-id",
    "exp": 1703520000,
    "iss": "CODMicroservices",
    "aud": "CODMicroservicesClient"
  }
}
```

### Swagger Integration:
Users can click "Authorize" in Swagger UI to enter their token, enabling testing of protected endpoints directly from the browser.

---

## 10. Middleware Pipeline

### Request Processing Order:
```
Client Request
    │
    ▼
┌─────────────────────────┐
│  ExceptionMiddleware     │  ← Catches ALL unhandled exceptions
├─────────────────────────┤
│  RequestLoggingMiddleware│  ← Logs request method, path, timing
├─────────────────────────┤
│  Authentication          │  ← Validates JWT token
├─────────────────────────┤
│  Authorization           │  ← Checks [Authorize] attribute
├─────────────────────────┤
│  Controller Action       │  ← Executes business logic
└─────────────────────────┘
    │
    ▼
Client Response
```

### Exception Middleware — What it does:
```csharp
// Catches exceptions and returns clean JSON responses:
ArgumentNullException    → 400 Bad Request
KeyNotFoundException     → 404 Not Found
InvalidOperationException → 400 Bad Request (with custom message)
Any other exception      → 500 Internal Server Error
```

### Logging Middleware — Output example:
```
HTTP POST /api/products started at 2026-05-05T12:21:24
HTTP POST /api/products responded 201 in 518ms
```

---

## 11. Business Logic Flow

### Complete COD Lifecycle:

```
┌──────────┐    ┌───────────┐    ┌───────────┐    ┌──────────┐
│  CREATE   │───►│  CREATED  │───►│ DELIVERED │───►│   PAID   │
│  ORDER    │    │  Pending  │    │  Pending  │    │ Delivered│
└──────────┘    └───────────┘    └───────────┘    └──────────┘

Step 1:          Step 2:          Step 3:          Step 4:
POST /orders    Status=Created   PUT /deliver     PUT /pay
                Payment=Pending  Status=Delivered Payment=Paid
                Employee=Busy    Employee=Free
```

### Validation Rules:
| Rule | When | Error Message |
|------|------|---------------|
| Product must exist | Order creation | "Product with ID X not found" |
| Sufficient stock | Order creation | "Insufficient stock" |
| Employee available | Order creation | "No available delivery employees" |
| Not already delivered | Mark delivered | "Order is already delivered" |
| Not cancelled | Mark delivered | "Cannot deliver cancelled order" |
| Must be delivered first | Mark paid | "Must be delivered before payment" |
| Not already paid | Mark paid | "Order is already paid" |

---

## 12. API Reference

### Authentication (all services)
```
POST /api/auth/token
Request:  { "username": "admin", "password": "admin123" }
Response: { "success": true, "token": "eyJ...", "expiration": "..." }
```

### Product Service (:5001)
```
GET    /api/products         → List all products
GET    /api/products/{id}    → Get single product
POST   /api/products         → Create product   [Auth Required]
PUT    /api/products/{id}    → Update product    [Auth Required]
DELETE /api/products/{id}    → Delete product    [Auth Required]
GET    /health               → Health check
```

### Employee Service (:5002)
```
GET    /api/employees              → List all employees
GET    /api/employees/{id}         → Get single employee
GET    /api/employees/available    → Get first available
POST   /api/employees              → Create employee    [Auth Required]
PUT    /api/employees/{id}/availability → Toggle availability
DELETE /api/employees/{id}         → Delete employee    [Auth Required]
GET    /health                     → Health check
```

### Order Service (:5003)
```
GET    /api/orders              → List all orders
GET    /api/orders/{id}         → Get single order
POST   /api/orders              → Create order       [Auth Required]
PUT    /api/orders/{id}/deliver → Mark delivered      [Auth Required]
PUT    /api/orders/{id}/pay     → Mark COD paid       [Auth Required]
GET    /health                  → Health check
```

### Sample Request/Response — Create Order:
```json
// Request: POST http://localhost:5003/api/orders
{ "productId": 1, "quantity": 2 }

// Response: 201 Created
{
  "success": true,
  "data": {
    "id": 1,
    "productId": 1,
    "productName": "iPhone 15 Pro",
    "productPrice": 999.99,
    "quantity": 2,
    "totalAmount": 1999.98,
    "employeeId": 1,
    "employeeName": "Rahul Sharma",
    "status": "Created",
    "paymentStatus": "Pending",
    "createdAt": "2026-05-05T12:35:10Z"
  },
  "message": "Order created successfully"
}
```

---

## 13. Docker & Containerization

### Dockerfile (multi-stage build):
```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime (smaller image)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll"]
```

### Docker Compose — All services + database:
```yaml
services:
  sqlserver:     # SQL Server database
  product-service:    # Port 5001
  employee-service:   # Port 5002
  order-service:      # Port 5003 (depends on product + employee)
```

### Run with one command:
```bash
docker-compose up -d --build
```

---

## 14. AWS Deployment

### Architecture on AWS:
```
EC2 Instance (t3.medium)
├── Product Service  → :5001
├── Employee Service → :5002
└── Order Service    → :5003
        │
        ▼
AWS RDS (SQL Server Express)
├── ProductDb
├── EmployeeDb
└── OrderDb
```

### Steps Summary:
1. Launch EC2 (Ubuntu 22.04, t3.medium)
2. Install .NET 8 SDK on EC2
3. Create RDS SQL Server instance
4. Publish and deploy services
5. Configure connection strings for RDS
6. Create systemd services for auto-start
7. Open ports 5001-5003 in Security Group

**Estimated cost: ~$58/month** (EC2 + RDS)

See [aws-deployment-guide.md](aws-deployment-guide.md) for detailed commands.

---

## 15. Testing Results

### Build Verification:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### End-to-End Test Results:

| # | Test | Status | Details |
|---|------|--------|---------|
| 1 | Health Check (all 3) | ✅ PASS | All return "Healthy" |
| 2 | JWT Token Generation | ✅ PASS | 24-hour token generated |
| 3 | Create Product | ✅ PASS | iPhone 15 Pro (ID: 1) |
| 4 | Create Employee | ✅ PASS | Rahul Sharma (Available: true) |
| 5 | Create Order (inter-service) | ✅ PASS | Product validated, employee assigned |
| 6 | Employee marked unavailable | ✅ PASS | isAvailable → false |
| 7 | Mark Delivered | ✅ PASS | Status → Delivered |
| 8 | Employee released | ✅ PASS | isAvailable → true |
| 9 | Mark COD Paid | ✅ PASS | PaymentStatus → Paid |
| 10 | Unauthorized access blocked | ✅ PASS | 401 without token |

---

## 16. Advanced Concepts Used

### 1. Dependency Injection (DI)
Every class receives its dependencies through the constructor, registered in `Program.cs`:
```csharp
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductServiceImpl>();
```

### 2. Repository Pattern
Separates data access from business logic. The service layer never writes SQL or knows about EF Core.

### 3. DTO Pattern
API never exposes raw database entities. DTOs control what data goes in/out:
- `CreateProductDto` — what client sends (no Id, no CreatedAt)
- `ProductDto` — what API returns (includes Id, CreatedAt)

### 4. AutoMapper
Eliminates manual property-by-property copying:
```csharp
CreateMap<Product, ProductDto>();           // Entity → Response
CreateMap<CreateProductDto, Product>();     // Request → Entity
```

### 5. HttpClientFactory
Manages HTTP connection lifecycle, preventing socket exhaustion in high-traffic scenarios.

### 6. Global Exception Handling
One middleware catches ALL exceptions — no try/catch needed in every controller action.

### 7. Structured Logging
Uses `ILogger<T>` with structured message templates for searchable logs:
```csharp
_logger.LogInformation("Product created with ID: {ProductId}", product.Id);
```

### 8. Health Checks
Built-in health monitoring with database connectivity check:
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProductDbContext>("database");
```

### 9. Data Denormalization
Order stores product/employee names to avoid cross-service queries for read operations.

### 10. Async/Await Throughout
Every I/O operation (database, HTTP) is asynchronous — no thread blocking, better scalability.

---

## Summary

This project demonstrates a **production-grade microservices architecture** with:
- ✅ 3 independent, deployable services
- ✅ Clean architecture with proper layering
- ✅ Inter-service REST communication
- ✅ JWT authentication
- ✅ Full CRUD operations
- ✅ Complete COD business workflow
- ✅ Docker containerization
- ✅ AWS deployment ready
- ✅ Swagger API documentation
- ✅ Postman collection for testing

**GitHub Repository:** https://github.com/shkr1311/webapi
