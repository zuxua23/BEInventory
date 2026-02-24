# InventoryControl

Aplikasi backend **Inventory Control** berbasis **ASP.NET Core 8 Web API** dengan dukungan:
- Autentikasi JWT
- Otorisasi berbasis role/permission
- Penyimpanan token aktif di Redis
- Database SQL Server via Entity Framework Core

## Teknologi yang Digunakan
- .NET 8 (`net8.0`)
- ASP.NET Core Web API
- Entity Framework Core + SQL Server
- JWT Bearer Authentication
- Redis (`StackExchange.Redis`)
- Serilog
- BCrypt untuk hashing password

## Struktur Folder (ringkas)
```text
InventoryControl/
├── Controllers/           # Endpoint API (Auth, User)
├── Database/
│   ├── Seeder/            # Seeder role/permission
│   └── AppDBContext.cs    # EF Core DbContext
├── DTO/                   # Request/response DTO
├── Entity/                # Model entity database
├── Migrations/            # File migrasi EF Core
├── Service/               # Business logic service
├── Utility/               # Helper (JWT, logger, dll)
├── Program.cs             # DI, auth, middleware pipeline
└── appsettings.json       # Konfigurasi aplikasi
```

## Prasyarat
Pastikan sudah terpasang:
1. **.NET SDK 8**
2. **SQL Server** (local/remote)
3. **Redis** (default: `localhost:6379`)

## Konfigurasi
Edit file `InventoryControl/appsettings.json` sesuai environment Anda.

Contoh konfigurasi penting:

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

## Menjalankan Proyek
Dari root repository:

```bash
dotnet restore
dotnet build
dotnet run --project InventoryControl/InventoryControl.csproj
```

Aplikasi akan berjalan di URL yang dikonfigurasi (`http://0.0.0.0:24000` jika default).

## Migrasi Database (EF Core)
Jika perlu membuat/update skema database:

```bash
dotnet ef database update --project InventoryControl/InventoryControl.csproj
```

> Jika tool `dotnet-ef` belum ada, install dulu:
> `dotnet tool install --global dotnet-ef`

## Seeder Data Akses
Saat aplikasi start, seeder akan menginisialisasi:
- Data `Permission`
- Data `Role` (`OPERATOR`, `ADMIN`)
- Mapping `Role_Permission`

Seeder dijalankan pada startup dari `Program.cs` melalui `SeedAccess.Initialize(...)`.

## Endpoint API Utama
Base URL default: `http://localhost:24000`

### 1) Login
- **POST** `/auth/login`
- Body:

```json
{
  "username": "admin",
  "password": "admin123"
}
```

### 2) Profil user login
- **GET** `/auth/me`
- Header: `Authorization: Bearer <token>`

### 3) Logout
- **POST** `/auth/logout`
- Header: `Authorization: Bearer <token>`

### 4) CRUD User
Semua endpoint `/user` membutuhkan bearer token.
- **GET** `/user`
- **GET** `/user/{id}`
- **POST** `/user`
- **PUT** `/user/{id}`
- **DELETE** `/user/{id}`

## Contoh Penggunaan Singkat (cURL)
```bash
# Login
curl -X POST http://localhost:24000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Ambil profile (ganti <TOKEN>)
curl http://localhost:24000/auth/me \
  -H "Authorization: Bearer <TOKEN>"
```

## Catatan
- Untuk endpoint API, aplikasi mengembalikan JSON khusus untuk kasus token tidak ada / invalid / expired.
- Konfigurasi saat ini menggabungkan web session dan JWT auth; jika hanya API, session bisa disesuaikan.
- Pastikan Redis aktif agar validasi token berjalan sesuai implementasi.
