# DriveEasy — Vehicle Rental Management System

A full-stack vehicle rental platform built with **ASP.NET Core 10 Web API** and **Blazor WebAssembly**, developed as a final project for the Service Oriented Architecture course at South East European University.

**Live Demo:** https://driveeasy-app-gacyfdfccvhgdngx.francecentral-01.azurewebsites.net  
**API Docs (Swagger):** https://driveeasy-app-gacyfdfccvhgdngx.francecentral-01.azurewebsites.net/swagger

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
| Cloud | Microsoft Azure App Service + Azure SQL |

---

## Project Structure

```
VehicleRental/
├── src/
│   ├── VehicleRental.API/                  ← ASP.NET Core 10 Web API
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs           ← Register, Login (issues JWT)
│   │   │   ├── VehiclesController.cs       ← Vehicle CRUD endpoints
│   │   │   ├── ReservationsController.cs   ← Reservation management
│   │   │   └── UsersController.cs          ← User management (Admin only)
│   │   ├── Services/
│   │   │   ├── IAuthService.cs / AuthService.cs
│   │   │   ├── IVehicleService.cs / VehicleService.cs
│   │   │   ├── IReservationService.cs / ReservationService.cs
│   │   │   └── IUserService.cs / UserService.cs
│   │   ├── Repositories/
│   │   │   ├── IVehicleRepository.cs / VehicleRepository.cs
│   │   │   ├── IReservationRepository.cs / ReservationRepository.cs
│   │   │   └── IUserRepository.cs / UserRepository.cs
│   │   ├── Models/
│   │   │   ├── User.cs                     ← User entity (EF Core model)
│   │   │   ├── Vehicle.cs                  ← Vehicle entity
│   │   │   └── Reservation.cs              ← Reservation entity
│   │   ├── DTOs/                           ← Data Transfer Objects (request/response shapes)
│   │   │   ├── Auth/                       ← LoginDto, RegisterDto, AuthResponseDto
│   │   │   ├── Vehicle/                    ← VehicleReadDto, VehicleCreateDto, VehicleUpdateDto
│   │   │   ├── Reservation/                ← ReservationReadDto, CreateReservationDto
│   │   │   └── User/                       ← UserReadDto, UpdateUserDto
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs             ← EF Core DbContext
│   │   │   └── Migrations/                 ← Auto-generated EF Core migrations
│   │   ├── Middleware/
│   │   │   └── ExceptionMiddleware.cs      ← Global error handling
│   │   ├── Profiles/
│   │   │   └── MappingProfile.cs           ← AutoMapper entity ↔ DTO mappings
│   │   ├── wwwroot/                        ← Blazor WASM static files (served by API)
│   │   ├── appsettings.json                ← App configuration (connection string, JWT)
│   │   └── Program.cs                      ← App entry point, DI setup, middleware pipeline
│   │
│   └── VehicleRental.Web/                  ← Blazor WebAssembly frontend
│       ├── Pages/
│       │   ├── Auth/
│       │   │   ├── Login.razor             ← Login page
│       │   │   └── Register.razor          ← Registration page
│       │   ├── Admin/
│       │   │   ├── AdminVehicles.razor     ← Vehicle CRUD (admin)
│       │   │   ├── AdminReservations.razor ← All reservations (admin)
│       │   │   └── AdminUsers.razor        ← User management (admin)
│       │   ├── Customer/
│       │   │   ├── Vehicles.razor          ← Vehicle catalogue with filters
│       │   │   ├── VehicleDetail.razor     ← Vehicle detail + booking form
│       │   │   ├── MyReservations.razor    ← Customer's reservations
│       │   │   └── Profile.razor           ← Edit profile
│       │   └── Index.razor                 ← Home / redirect
│       ├── Shared/
│       │   ├── MainLayout.razor            ← App shell with sidebar nav
│       │   └── NavMenu.razor               ← Navigation menu
│       ├── Services/
│       │   ├── AuthClientService.cs        ← HTTP calls to /api/auth
│       │   ├── VehicleClientService.cs     ← HTTP calls to /api/vehicles
│       │   ├── ReservationClientService.cs ← HTTP calls to /api/reservations
│       │   ├── UserClientService.cs        ← HTTP calls to /api/users
│       │   └── JwtAuthStateProvider.cs     ← Reads JWT from localStorage, sets auth state
│       └── Program.cs                      ← WASM entry point, HttpClient, DI
│
└── tests/
    └── VehicleRental.Tests/                ← xUnit unit test project
        ├── Services/
        │   ├── AuthServiceTests.cs         ← 16 tests for AuthService
        │   ├── VehicleServiceTests.cs      ← 15 tests for VehicleService
        │   ├── ReservationServiceTests.cs  ← 17 tests for ReservationService
        │   └── UserServiceTests.cs         ← 11 tests for UserService
        └── Controllers/
            ├── VehiclesControllerTests.cs  ← 14 tests for VehiclesController
            ├── ReservationsControllerTests.cs ← 14 tests for ReservationsController
            └── UsersControllerTests.cs     ← 14 tests for UsersController
```

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB is fine for development)
- Visual Studio 2022 or VS Code

### Run Locally

1. **Clone the repo**
   ```bash
   git clone https://github.com/Ssinani/DriveEasy.git
   cd DriveEasy
   ```

2. **Configure the connection string**

   Edit `src/VehicleRental.API/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=VehicleRentalDb;Trusted_Connection=True;"
   },
   "Jwt": {
     "Key": "DriveEasy@SuperSecretKey2026!XyZ#Secure",
     "Issuer": "https://localhost:5001",
     "Audience": "https://localhost:5001"
   }
   ```

3. **Run the API** — migrations and seed data run automatically on startup
   ```bash
   cd src/VehicleRental.API
   dotnet run
   ```
   API will be available at `https://localhost:5001`  
   Swagger UI at `https://localhost:5001/swagger`

4. **Run the frontend** (in a separate terminal)
   ```bash
   cd src/VehicleRental.Web
   dotnet run
   ```
   Open your browser at the URL shown in the terminal (e.g. `https://localhost:5173`)

### Default Admin Account
```
Email:    admin@vehiclerental.com
Password: Admin@123
```

---

## Running Tests

```bash
cd tests/VehicleRental.Tests
dotnet test
```

Expected output: **91 passed, 0 failed**

To run with detailed output:
```bash
dotnet test --verbosity normal
```

---

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | Public | Register a new customer |
| POST | `/api/auth/login` | Public | Login and receive JWT token |
| GET | `/api/vehicles` | Public | List all available vehicles |
| GET | `/api/vehicles/{id}` | Public | Get vehicle details |
| POST | `/api/vehicles` | Admin | Create a new vehicle |
| PUT | `/api/vehicles/{id}` | Admin | Update a vehicle |
| DELETE | `/api/vehicles/{id}` | Admin | Delete a vehicle |
| GET | `/api/reservations` | Admin | Get all reservations |
| GET | `/api/reservations/my` | Customer | Get my reservations |
| POST | `/api/reservations` | Customer | Create a reservation |
| PUT | `/api/reservations/{id}/status` | Admin | Update reservation status |
| DELETE | `/api/reservations/{id}` | Customer | Cancel a reservation |
| GET | `/api/users` | Admin | Get all users |
| GET | `/api/users/{id}` | Auth | Get user by ID |
| PUT | `/api/users/{id}` | Auth | Update user profile |
| PUT | `/api/users/{id}/role` | Admin | Change user role |
| PUT | `/api/users/{id}/deactivate` | Admin | Deactivate a user |

---

## CI/CD

GitHub Actions workflow (`.github/workflows/ci-cd.yml`) runs on every push to `main` or `develop`:
- Restores NuGet packages
- Builds the solution in Release mode
- Runs all 91 unit tests

---

## Course Info

| | |
|---|---|
| Course | Service Oriented Architecture |
| Institution | South East European University |
| Academic Year | 2025 / 2026 |
