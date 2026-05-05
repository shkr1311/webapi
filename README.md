# 🛒 COD Microservices — Cash On Delivery POC

A production-ready **Cash On Delivery** proof-of-concept built with **.NET 8 Web API**, **Microservices Architecture**, and **AWS** deployment support.

## 🏗️ Architecture

```
Client / Postman
    ├── Product Service  (Port 5001)  ──► ProductDb
    ├── Employee Service (Port 5002)  ──► EmployeeDb
    └── Order Service    (Port 5003)  ──► OrderDb
            ├── calls Product Service (validate product)
            └── calls Employee Service (assign delivery agent)
```

## 🚀 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run Locally (3 terminals)

```bash
# Terminal 1 - Product Service
dotnet run --project src/ProductService

# Terminal 2 - Employee Service
dotnet run --project src/EmployeeService

# Terminal 3 - Order Service
dotnet run --project src/OrderService
```

### Swagger UIs
| Service | URL |
|---------|-----|
| Product | http://localhost:5001 |
| Employee | http://localhost:5002 |
| Order | http://localhost:5003 |

### Run with Docker
```bash
docker-compose up -d --build
```

## 🔐 Authentication

All write endpoints require a JWT token.

```bash
# Get token
POST /api/auth/token
Body: { "username": "admin", "password": "admin123" }

# Use token
Header: Authorization: Bearer <token>
```

## 📋 API Endpoints

### Product Service (:5001)
| Method | Endpoint | Auth |
|--------|----------|------|
| GET | `/api/products` | ❌ |
| GET | `/api/products/{id}` | ❌ |
| POST | `/api/products` | ✅ |
| PUT | `/api/products/{id}` | ✅ |
| DELETE | `/api/products/{id}` | ✅ |

### Employee Service (:5002)
| Method | Endpoint | Auth |
|--------|----------|------|
| GET | `/api/employees` | ❌ |
| GET | `/api/employees/{id}` | ❌ |
| GET | `/api/employees/available` | ❌ |
| POST | `/api/employees` | ✅ |
| PUT | `/api/employees/{id}/availability` | ❌ |
| DELETE | `/api/employees/{id}` | ✅ |

### Order Service (:5003)
| Method | Endpoint | Auth |
|--------|----------|------|
| GET | `/api/orders` | ❌ |
| GET | `/api/orders/{id}` | ❌ |
| POST | `/api/orders` | ✅ |
| PUT | `/api/orders/{id}/deliver` | ✅ |
| PUT | `/api/orders/{id}/pay` | ✅ |

## 🔄 COD Business Flow

```
1. POST /api/auth/token          → Get JWT token
2. POST /api/products            → Create a product
3. POST /api/employees           → Create a delivery employee
4. POST /api/orders              → Create order (auto-validates product + assigns employee)
5. PUT  /api/orders/{id}/deliver → Mark as delivered
6. PUT  /api/orders/{id}/pay     → Mark COD payment as paid
```

## 🛠️ Tech Stack

- .NET 8 Web API
- Entity Framework Core (InMemory / SQL Server)
- JWT Authentication
- AutoMapper
- Swagger / OpenAPI
- Docker & Docker Compose

## 📂 Project Structure

```
├── src/
│   ├── ProductService/
│   │   ├── Controllers/    # API endpoints
│   │   ├── DTOs/           # Data transfer objects
│   │   ├── Models/         # Entity models
│   │   ├── Data/           # DbContext
│   │   ├── Repositories/   # Data access layer
│   │   ├── Services/       # Business logic
│   │   ├── Mappings/       # AutoMapper profiles
│   │   ├── Middleware/     # Exception & logging
│   │   └── Program.cs     # Entry point + DI
│   ├── EmployeeService/    # Same structure
│   └── OrderService/       # Same + HttpClient
├── docs/
│   ├── api-endpoints.md
│   ├── aws-deployment-guide.md
│   └── CodMicroservices.postman_collection.json
├── docker-compose.yml
└── CodMicroservices.sln
```

## ☁️ AWS Deployment

See [docs/aws-deployment-guide.md](docs/aws-deployment-guide.md) for step-by-step deployment using:
- AWS EC2 (hosting)
- AWS RDS (SQL Server database)
- Docker / ECS (optional)

## 📬 Postman

Import `docs/CodMicroservices.postman_collection.json` into Postman to test all endpoints.

## ⚙️ Configuration

Copy `appsettings.json` and create environment-specific files:
- `appsettings.Development.json` — for local dev
- `appsettings.Production.json` — for production (add your RDS connection string)

## 📄 License

MIT