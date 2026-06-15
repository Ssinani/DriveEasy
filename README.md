# DriveEasy — Vehicle Rental Management System
**Course:** Service Oriented Architecture · Southeast European University  
**Stack:** ASP.NET Core 10 · Blazor WebAssembly · SQL Server · EF Core · JWT · AutoMapper

---

## Project structure

```
VehicleRental/
├── src/
│   ├── VehicleRental.API/          ← REST API
│   │   ├── Controllers/            ← Auth, Vehicles, Reservations, Users
│   │   ├── Services/               ← Business logic (interface + impl in same file)
│   │   ├── Repositories/           ← Data access (interface + impl in same file)
│   │   ├── Models/                 ← EF Core entities
│   │   ├── DTOs/Dtos.cs            ← All Data Transfer Objects
│   │   ├── Data/AppDbContext.cs    ← EF Core + seed data
│   │   ├── Profiles/               ← AutoMapper mapping profile
│   │   └── Middleware/             ← Global exception handler
│   └── VehicleRental.Web/          ← Blazor WebAssembly frontend
│       ├── Pages/                  ← Login, Register, Dashboard, Vehicles, Reservations
│       ├── Pages/Admin/            ← AdminVehicles, AdminReservations
│       ├── Shared/                 ← MainLayout, AuthLayout
│       └── Services/               ← HTTP client services + JWT auth provider
└── tests/
    └── VehicleRental.Tests/        ← xUnit + NSubstitute
        ├── Controllers/            ← Auth, Vehicles
        ├── Services/               ← Vehicle, Reservation
        └── Repositories/          ← Vehicle (EF InMemory)
```

---

## Getting started

### 1. Prerequisites
- .NET 10 SDK
- SQL Server / LocalDB

### 2. Run the API
```bash
cd src/VehicleRental.API
dotnet run
# Swagger UI → https://localhost:7100/swagger
```

### 3. Run the frontend
```bash
cd src/VehicleRental.Web
dotnet run
# → https://localhost:7200
```

### 4. Run tests
```bash
dotnet test
```

---

## Default credentials
| Role  | Email | Password |
|-------|-------|----------|
| Admin | admin@vehiclerental.com | Admin@123 |

---

## API endpoints

### Auth — `/api/auth`
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/login` | Public | Returns JWT on valid credentials |
| POST | `/register` | Public | Creates customer account, returns JWT |

### Vehicles — `/api/vehicles`
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/` | Public | All vehicles |
| GET | `/{id}` | Public | Vehicle by ID |
| GET | `/available?startDate&endDate` | Public | Availability search |
| GET | `/search?category&minRate&maxRate&fuelType&transmission` | Public | Multi-filter search |
| POST | `/` | Admin | Create vehicle |
| PUT | `/{id}` | Admin | Update vehicle |
| DELETE | `/{id}` | Admin | Delete vehicle |

### Reservations — `/api/reservations`
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/` | Admin | All reservations |
| GET | `/my` | Customer | Own reservations |
| GET | `/{id}` | Owner/Admin | Single reservation |
| GET | `/status/{status}` | Admin | Filter by status |
| GET | `/estimate?vehicleId&startDate&endDate` | Auth | Cost estimate with discounts + VAT |
| POST | `/` | Auth | Create reservation |
| PUT | `/{id}` | Owner/Admin | Update pending reservation |
| PATCH | `/{id}/cancel` | Owner/Admin | Cancel reservation |
| PATCH | `/{id}/confirm` | Admin | Confirm pending reservation |
| PATCH | `/{id}/complete` | Admin | Mark reservation completed |

### Users — `/api/users`
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/` | Admin | All users |
| GET | `/me` | Auth | Own profile |
| GET | `/{id}` | Owner/Admin | User by ID |
| GET | `/role/{role}` | Admin | Users by role |
| PUT | `/{id}` | Owner/Admin | Update profile |
| PATCH | `/{id}/deactivate` | Admin | Soft-delete user |
| PATCH | `/{id}/role` | Admin | Change user role |

---

## Business logic highlights
- **Tiered discounts:** 7+ days = 5%, 14+ days = 10%, 30+ days = 15%
- **18% VAT** applied after discount
- **Conflict detection:** no overlapping non-cancelled reservations
- **Validation rules:** no past start dates · min 1 day · max 90 days
- **IMemoryCache** with 60s TTL on `GetReservationById`; invalidated on status change
- **Soft delete** on users (`IsActive = false`)
- **Role-based access:** Admin vs Customer at both service and controller level

---

## CI/CD
GitHub Actions pipeline (`.github/workflows/ci-cd.yml`) runs on push to `main` or `develop`:
1. Build solution
2. Run all unit tests
3. Deploy API → Azure App Service
4. Deploy Blazor → Azure Static Web Apps

**Required GitHub secrets:** `AZURE_WEBAPP_NAME`, `AZURE_WEBAPP_PUBLISH_PROFILE`, `AZURE_STATIC_WEB_APPS_API_TOKEN`
