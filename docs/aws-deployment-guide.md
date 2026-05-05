# AWS Deployment Guide — COD Microservices

## Architecture on AWS

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  EC2 / ECS  │     │  EC2 / ECS  │     │  EC2 / ECS  │
│  Product    │◄───►│  Order      │◄───►│  Employee   │
│  Service    │     │  Service    │     │  Service    │
│  :5001      │     │  :5003      │     │  :5002      │
└──────┬──────┘     └──────┬──────┘     └──────┬──────┘
       │                   │                   │
       └───────────────────┼───────────────────┘
                           │
                    ┌──────▼──────┐
                    │  AWS RDS    │
                    │  SQL Server │
                    └─────────────┘
```

---

## Option 1: Deploy on AWS EC2

### Step 1: Launch EC2 Instance

1. Go to **AWS Console → EC2 → Launch Instance**
2. Choose **Amazon Linux 2023** or **Ubuntu 22.04 LTS**
3. Instance type: **t3.medium** (minimum for 3 services)
4. Configure Security Group:
   - SSH: Port 22 (your IP)
   - HTTP: Port 80
   - Custom TCP: Ports 5001, 5002, 5003
   - MSSQL: Port 1433 (from EC2 security group only)
5. Create/select a key pair
6. Launch instance

### Step 2: Connect to EC2

```bash
ssh -i your-key.pem ec2-user@<EC2_PUBLIC_IP>
# or for Ubuntu:
ssh -i your-key.pem ubuntu@<EC2_PUBLIC_IP>
```

### Step 3: Install .NET 8 on EC2

**Amazon Linux 2023:**
```bash
# Install .NET 8 SDK
sudo rpm -Uvh https://packages.microsoft.com/config/centos/8/packages-microsoft-prod.rpm
sudo dnf install -y dotnet-sdk-8.0

# Verify installation
dotnet --version
```

**Ubuntu 22.04:**
```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET 8 SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# Verify
dotnet --version
```

### Step 4: Set Up AWS RDS (SQL Server)

1. Go to **AWS Console → RDS → Create Database**
2. Choose **Microsoft SQL Server**
3. Edition: **SQL Server Express** (free tier eligible)
4. Settings:
   - DB Instance Identifier: `cod-microservices-db`
   - Master Username: `admin`
   - Master Password: `YourSecurePassword123!`
5. Instance: `db.t3.small`
6. Storage: 20 GB (General Purpose SSD)
7. Connectivity:
   - VPC: Same as EC2
   - Public Access: No
   - Security Group: Allow port 1433 from EC2 security group
8. Create database

**Note the RDS Endpoint:** `cod-microservices-db.xxxxx.us-east-1.rds.amazonaws.com`

### Step 5: Deploy Services to EC2

```bash
# Clone or copy your code to EC2
git clone <your-repo-url> /opt/cod-microservices
cd /opt/cod-microservices

# Publish all services
dotnet publish src/ProductService -c Release -o /opt/apps/product-service
dotnet publish src/EmployeeService -c Release -o /opt/apps/employee-service
dotnet publish src/OrderService -c Release -o /opt/apps/order-service
```

### Step 6: Configure Connection Strings

Create environment-specific appsettings for each service:

**Product Service** (`/opt/apps/product-service/appsettings.Production.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=cod-microservices-db.xxxxx.rds.amazonaws.com;Database=ProductDb;User Id=admin;Password=YourSecurePassword123!;TrustServerCertificate=True;"
  },
  "Urls": "http://0.0.0.0:5001"
}
```

**Employee Service** (`/opt/apps/employee-service/appsettings.Production.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=cod-microservices-db.xxxxx.rds.amazonaws.com;Database=EmployeeDb;User Id=admin;Password=YourSecurePassword123!;TrustServerCertificate=True;"
  },
  "Urls": "http://0.0.0.0:5002"
}
```

**Order Service** (`/opt/apps/order-service/appsettings.Production.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=cod-microservices-db.xxxxx.rds.amazonaws.com;Database=OrderDb;User Id=admin;Password=YourSecurePassword123!;TrustServerCertificate=True;"
  },
  "ServiceUrls": {
    "ProductService": "http://localhost:5001",
    "EmployeeService": "http://localhost:5002"
  },
  "Urls": "http://0.0.0.0:5003"
}
```

### Step 7: Create Systemd Services

Create a service file for each microservice to run them as background daemons:

**Product Service** (`/etc/systemd/system/product-service.service`):
```ini
[Unit]
Description=COD Product Service
After=network.target

[Service]
WorkingDirectory=/opt/apps/product-service
ExecStart=/usr/bin/dotnet /opt/apps/product-service/ProductService.dll
Restart=always
RestartSec=10
SyslogIdentifier=product-service
User=ec2-user
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

**Employee Service** (`/etc/systemd/system/employee-service.service`):
```ini
[Unit]
Description=COD Employee Service
After=network.target

[Service]
WorkingDirectory=/opt/apps/employee-service
ExecStart=/usr/bin/dotnet /opt/apps/employee-service/EmployeeService.dll
Restart=always
RestartSec=10
SyslogIdentifier=employee-service
User=ec2-user
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

**Order Service** (`/etc/systemd/system/order-service.service`):
```ini
[Unit]
Description=COD Order Service
After=network.target product-service.service employee-service.service

[Service]
WorkingDirectory=/opt/apps/order-service
ExecStart=/usr/bin/dotnet /opt/apps/order-service/OrderService.dll
Restart=always
RestartSec=10
SyslogIdentifier=order-service
User=ec2-user
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

### Step 8: Start Services

```bash
# Reload systemd and enable services
sudo systemctl daemon-reload
sudo systemctl enable product-service employee-service order-service

# Start services
sudo systemctl start product-service
sudo systemctl start employee-service
sudo systemctl start order-service

# Check status
sudo systemctl status product-service
sudo systemctl status employee-service
sudo systemctl status order-service

# View logs
sudo journalctl -u product-service -f
sudo journalctl -u order-service -f
```

### Step 9: Verify Deployment

```bash
# Test health endpoints
curl http://localhost:5001/health
curl http://localhost:5002/health
curl http://localhost:5003/health

# Test from browser using EC2 public IP
# http://<EC2_PUBLIC_IP>:5001  → Product Service Swagger
# http://<EC2_PUBLIC_IP>:5002  → Employee Service Swagger
# http://<EC2_PUBLIC_IP>:5003  → Order Service Swagger
```

---

## Option 2: Deploy with Docker on EC2

### Step 1: Install Docker on EC2

```bash
# Amazon Linux 2023
sudo dnf install -y docker
sudo systemctl start docker
sudo systemctl enable docker
sudo usermod -aG docker ec2-user

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```

### Step 2: Deploy with Docker Compose

```bash
cd /opt/cod-microservices

# Update docker-compose.yml connection strings to point to RDS
# Or use the InMemory database for demo

# Build and run
docker-compose up -d --build

# Check status
docker-compose ps

# View logs
docker-compose logs -f
```

---

## Option 3: Deploy on AWS ECS (Fargate)

### Step 1: Push Images to ECR

```bash
# Create ECR repositories
aws ecr create-repository --repository-name cod/product-service
aws ecr create-repository --repository-name cod/employee-service
aws ecr create-repository --repository-name cod/order-service

# Login to ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com

# Build and push
docker build -t cod/product-service ./src/ProductService
docker tag cod/product-service:latest <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/cod/product-service:latest
docker push <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/cod/product-service:latest

# Repeat for employee-service and order-service
```

### Step 2: Create ECS Cluster

1. Go to **AWS Console → ECS → Create Cluster**
2. Choose **AWS Fargate** (serverless)
3. Name: `cod-microservices-cluster`

### Step 3: Create Task Definitions

Create a task definition for each service with:
- Container image: ECR image URI
- Port mappings: 80
- Environment variables: Connection strings, JWT settings, service URLs
- Memory: 512 MB
- CPU: 256

### Step 4: Create Services

For each task definition, create an ECS service:
- Launch type: Fargate
- Desired count: 1
- VPC: Same as RDS
- Security Group: Allow required ports
- Load Balancer: Application Load Balancer (optional)

### Step 5: Configure Service Discovery

Use **AWS Cloud Map** for service-to-service communication:
- Product Service: `product-service.cod.local`
- Employee Service: `employee-service.cod.local`
- Order Service references them by DNS names

---

## Security Best Practices

1. **Never hardcode secrets** — Use AWS Secrets Manager or Parameter Store
2. **Use HTTPS** — Configure SSL certificates via ACM
3. **Restrict security groups** — Only allow necessary ports
4. **Enable CloudWatch** — For monitoring and alerting
5. **Use IAM roles** — Instead of access keys on EC2
6. **Rotate JWT keys** — Store in AWS Secrets Manager
7. **Enable RDS encryption** — At-rest and in-transit

---

## Monitoring

```bash
# Install CloudWatch Agent for log forwarding
sudo yum install -y amazon-cloudwatch-agent

# Configure to stream service logs to CloudWatch
# Create /opt/aws/amazon-cloudwatch-agent/etc/config.json
```

---

## Cost Estimate (Monthly)

| Resource | Specification | Estimated Cost |
|----------|--------------|----------------|
| EC2 (t3.medium) | 1 instance | ~$30 |
| RDS SQL Server Express | db.t3.small | ~$25 |
| EBS Storage | 30 GB | ~$3 |
| Data Transfer | 10 GB | ~$1 |
| **Total** | | **~$59/month** |

> For ECS Fargate: ~$45/month for 3 services (0.25 vCPU, 0.5 GB each)
