# COD Microservices — API Endpoints Reference

## 🔐 Authentication (All Services)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/auth/token` | Generate JWT token | ❌ |

**Request:**
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**Response:**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2024-12-26T12:00:00Z"
}
```

---

## 📦 Product Service (Port 5001)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/products` | Get all products | ❌ |
| GET | `/api/products/{id}` | Get product by ID | ❌ |
| POST | `/api/products` | Create a new product | ✅ |
| PUT | `/api/products/{id}` | Update a product | ✅ |
| DELETE | `/api/products/{id}` | Delete a product | ✅ |
| GET | `/health` | Health check | ❌ |

### POST `/api/products` — Create Product
**Request:**
```json
{
  "name": "iPhone 15 Pro",
  "price": 999.99,
  "stock": 50
}
```

**Response (201):**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "name": "iPhone 15 Pro",
    "price": 999.99,
    "stock": 50,
    "createdAt": "2024-12-25T10:00:00Z"
  },
  "message": "Product created successfully"
}
```

### GET `/api/products` — Get All Products
**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "iPhone 15 Pro",
      "price": 999.99,
      "stock": 50,
      "createdAt": "2024-12-25T10:00:00Z"
    }
  ]
}
```

---

## 👷 Employee Service (Port 5002)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/employees` | Get all employees | ❌ |
| GET | `/api/employees/{id}` | Get employee by ID | ❌ |
| GET | `/api/employees/available` | Get first available employee | ❌ |
| POST | `/api/employees` | Create a new employee | ✅ |
| PUT | `/api/employees/{id}/availability` | Update availability | ❌ |
| DELETE | `/api/employees/{id}` | Delete an employee | ✅ |
| GET | `/health` | Health check | ❌ |

### POST `/api/employees` — Create Employee
**Request:**
```json
{
  "name": "Rahul Sharma",
  "phone": "+91-9876543210"
}
```

**Response (201):**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "name": "Rahul Sharma",
    "phone": "+91-9876543210",
    "isAvailable": true,
    "createdAt": "2024-12-25T10:00:00Z"
  },
  "message": "Employee created successfully"
}
```

### PUT `/api/employees/{id}/availability` — Update Availability
**Request:**
```json
{
  "isAvailable": false
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Employee availability updated to False"
}
```

---

## 🛒 Order Service (Port 5003)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/orders` | Get all orders | ❌ |
| GET | `/api/orders/{id}` | Get order by ID | ❌ |
| POST | `/api/orders` | Create order (validates product + assigns employee) | ✅ |
| PUT | `/api/orders/{id}/deliver` | Mark order as delivered | ✅ |
| PUT | `/api/orders/{id}/pay` | Mark COD payment as paid | ✅ |
| GET | `/health` | Health check | ❌ |

### POST `/api/orders` — Create Order
**Request:**
```json
{
  "productId": 1,
  "quantity": 2
}
```

**Response (201):**
```json
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
    "createdAt": "2024-12-25T10:05:00Z",
    "deliveredAt": null,
    "paidAt": null
  },
  "message": "Order created successfully"
}
```

### PUT `/api/orders/{id}/deliver` — Mark Delivered
**Response (200):**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "status": "Delivered",
    "paymentStatus": "Pending",
    "deliveredAt": "2024-12-25T11:00:00Z"
  },
  "message": "Order marked as delivered"
}
```

### PUT `/api/orders/{id}/pay` — Mark COD Paid
**Response (200):**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "status": "Delivered",
    "paymentStatus": "Paid",
    "paidAt": "2024-12-25T11:05:00Z"
  },
  "message": "Payment marked as COD Paid"
}
```

---

## 🔄 Complete COD Flow

```
1. POST /api/auth/token          → Get JWT token
2. POST /api/products             → Create a product
3. POST /api/employees            → Create an employee  
4. POST /api/orders               → Create order (auto-validates product + assigns employee)
5. PUT  /api/orders/{id}/deliver  → Mark order as delivered
6. PUT  /api/orders/{id}/pay      → Mark COD payment as paid
```

## Swagger UI URLs

| Service | URL |
|---------|-----|
| Product Service | http://localhost:5001 |
| Employee Service | http://localhost:5002 |
| Order Service | http://localhost:5003 |
