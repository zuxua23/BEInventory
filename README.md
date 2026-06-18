# InventoryControl

An ASP.NET Core-based inventory management system with comprehensive item tracking, stock management, and role-based access control.

## 📋 Overview

InventoryControl is a web application designed to manage inventory operations including stock in/out transactions, item master data, tag printing, and stock-taking activities. Built with modern technologies including C#, ASP.NET Core, and SQL Server.

## 🛠️ Tech Stack

- **Language**: C#
- **Framework**: ASP.NET Core 8 (.NET 8)
- **Database**: SQL Server
- **ORM**: Entity Framework Core
- **Cache**: Redis (`StackExchange.Redis`)
- **Authentication**: JWT Bearer
- **Password Hashing**: BCrypt
- **Logging**: Serilog
- **API Format**: JSON with circular reference handling

## 📁 Project Structure

```
InventoryControl/
├── Controllers/           # API Endpoints (Auth, User)
├── Database/
│   ├── Seeder/            # Role/Permission Seeders
│   └── AppDBContext.cs    # EF Core DbContext
├── DTO/                   # Request/Response DTOs
├── Entity/                # Database Entity Models
├── Migrations/            # EF Core Migration Files
├── Service/               # Business Logic Services
├── Utility/               # Helper Utilities (JWT, Logger, etc.)
├── Program.cs             # Dependency Injection & Middleware Pipeline
└── appsettings.json       # Application Configuration
```

## 📋 Prerequisites

Make sure you have the following installed:
1. **.NET SDK 8** or higher
2. **SQL Server** (local or remote)
3. **Redis** (default: `localhost:6379`)

## ⚙️ Configuration

Edit the `InventoryControl/appsettings.json` file according to your environment.

Example configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=InventoryControl;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "your-secret-key-min-16-char",
    "Issuer": "InventoryControl",
    "Audience": "InventoryControlClient",
    "ExpireMinutes": 60
  },
  "Redis": {
    "Connection": "localhost:6379"
  },
  "Urls": "http://0.0.0.0:24000"
}
```

## 🚀 Running the Application

From the root repository directory:

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --project InventoryControl/InventoryControl.csproj
```

The application will run on the configured URL (default: `http://0.0.0.0:24000`)

## 🗄️ Database Migration (EF Core)

To create or update the database schema:

```bash
dotnet ef database update --project InventoryControl/InventoryControl.csproj
```

> If the `dotnet-ef` tool is not installed, install it first:
> ```bash
> dotnet tool install --global dotnet-ef
> ```

## 🔐 Data Access Seeding

When the application starts, the seeder will initialize:
- Permission data
- Role data (OPERATOR, ADMIN)
- Role-Permission mappings

The seeder runs automatically on startup from `Program.cs` via `SeedAccess.Initialize(...)`.

## 📡 Main API Endpoints

Base URL (default): `http://localhost:24000`

### 1) Login
- **POST** `/auth/login`
- Body:

```json
{
  "username": "admin",
  "password": "admin123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": 1,
    "username": "admin",
    "role": "ADMIN"
  }
}
```

### 2) Get Current User Profile
- **GET** `/auth/me`
- Header: `Authorization: Bearer <token>`

### 3) Logout
- **POST** `/auth/logout`
- Header: `Authorization: Bearer <token>`

### 4) User Management
All `/user` endpoints require a bearer token with appropriate permissions.
- **GET** `/user` - List all users
- **GET** `/user/{id}` - Get user by ID
- **POST** `/user` - Create new user
- **PUT** `/user/{id}` - Update user
- **DELETE** `/user/{id}` - Delete user

## 💡 Usage Examples (cURL)

```bash
# Login
curl -X POST http://localhost:24000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Get current user profile (replace <TOKEN> with token from login)
curl http://localhost:24000/auth/me \
  -H "Authorization: Bearer <TOKEN>"

# List all users
curl http://localhost:24000/user \
  -H "Authorization: Bearer <TOKEN>"

# Get specific user
curl http://localhost:24000/user/1 \
  -H "Authorization: Bearer <TOKEN>"

# Create new user
curl -X POST http://localhost:24000/user \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{"username":"newuser","password":"pass123","roleId":2}'
```

## 🔐 Role-Based Access Control (RBAC)

This system uses roles and permissions to control access:
- **ADMIN**: Full access to all features
- **OPERATOR**: Limited access based on assigned permissions

Role-permission mappings are configured through the seeder and can be customized in the database.

## 📝 Important Notes

- The application returns custom JSON responses for token-related errors (missing, invalid, or expired).
- Current configuration combines web session and JWT auth; adjust as needed if using API-only mode.
- **Ensure Redis is running** for proper token validation according to the implementation.
- All passwords are hashed using BCrypt for security.
- Circular references in JSON responses are handled in the configuration.
- Default admin credentials can be changed through the seeder or database directly.

## 🐛 Troubleshooting

**Issue**: Connection to SQL Server fails
- Solution: Verify connection string in `appsettings.json` and ensure SQL Server is running.

**Issue**: Redis connection error
- Solution: Check that Redis is running on `localhost:6379` or update the connection string.

**Issue**: JWT token validation fails
- Solution: Ensure the JWT secret key in `appsettings.json` is consistent and Redis token storage is working.

**Issue**: Migration fails
- Solution: Delete `bin` and `obj` folders, then run `dotnet restore` and retry the migration.

## 📄 License

Please update with your project's license information.

---

**Last Updated**: 2026-05-11
**Version**: 1.0
