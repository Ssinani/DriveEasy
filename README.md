# DriveEasy — Vehicle Rental Management System

A full-stack vehicle rental platform built with **ASP.NET Core 10 Web API** and **Blazor WebAssembly**, developed as a final project for the Service Oriented Architecture course at South East European University.

---

## Features

### Customer
- Register / Login with JWT authentication
- Browse vehicle catalogue with filters (category, fuel type, transmission, price)
- View vehicle details and get a real-time cost estimate before booking
- Make, view, and cancel reservations
- Edit personal profile

### Admin
- Manage vehicles — full CRUD with 11 categories, 6 fuel types
- View and manage all reservations (confirm, complete, cancel)
- Manage user accounts — change roles, deactivate accounts

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 10 Web API |
| Frontend | Blazor WebAssembly (.NET 10) |
| Database | SQL Server + Entity Framework Core 10 |
| Auth | JWT Bearer Tokens (HS256) |
| Mapping | AutoMapper 13 |
| Testing | xUnit 2.9.2 + NSubstitute 5.1 |
| CI/CD | GitHub Actions |
| Cloud | Microsoft Azure |

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB is fine for development)
- Visual Studio 2022 or VS Code

### Run locally

1. **Clone the repo**
   ```bash
   git clone https://github.com/YOUR-USERNAME/YOUR-REPO-NAME.git
   cd YOUR-REPO-NAME
   ```

2. **Configure the connection string**

   Edit `src/VehicleRental.API/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=VehicleRentalDb;Trusted_Connection=True;"
   }
   ```

3. **Run the API** (migrations run automatically on startup)
   ```bash
   cd src/VehicleRental.API
   dotnet run
   ```

4. **Run the frontend** (in a separate terminal)
   ```bash
   cd src/VehicleRental.Web
   dotnet run
   ```

5. Open your browser at the URL shown in the terminal (e.g. `https://localhost:5173`)

### Default admin account
```
Email:    admin@vehiclerental.com
Password: Admin@123
```

---

## Project Structure

```
VehicleRental/
├── src/
│   ├── VehicleRental.API/       ← ASP.NET Core Web API
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Repositories/
│   │   ├── Models/
│   │   ├── DTOs/
│   │   ├── Data/                ← EF Core DbContext + Migrations
│   │   ├── Middleware/          ← Global exception handler
│   │   └── Profiles/            ← AutoMapper
│   └── VehicleRental.Web/       ← Blazor WebAssembly frontend
│       ├── Pages/
│       ├── Shared/
│       └── Services/
└── tests/
    └── VehicleRental.Tests/     ← xUnit unit tests (91 tests)
```

---

## Running Tests

```bash
cd tests/VehicleRental.Tests
dotnet test
```

Expected output: **91 passed, 0 failed**

---

## API Documentation

Swagger UI is available at `/swagger` when running in development mode.

---

## Course Info

| | |
|---|---|
| Course | Service Oriented Architecture |
| Institution | South East European University |
| Academic Year | 2025 / 2026 |
