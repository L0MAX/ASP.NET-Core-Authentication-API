# Clean Architecture ASP.NET Core 9 Web API

Production-ready ASP.NET Core 9 Web API scaffold using Clean Architecture, with an authentication domain model (User, Role, RefreshToken), JWT infrastructure, and SQL Server via EF Core.

## Project Structure

```
src/
├── Api/              # HTTP entry point — controllers, middleware, Swagger, Serilog
├── Application/      # Use cases, interfaces, DTOs, application services
├── Domain/           # Entities, domain rules (zero external dependencies)
└── Infrastructure/   # EF Core, SQL Server, JWT, persistence configurations
```

### Domain Layer (Authentication)

| Entity | Description |
|--------|-------------|
| `User` | Aggregate root — email, password hash, profile, email confirmation |
| `Role` | Named authorization role (`Admin`, `User`) |
| `RefreshToken` | Long-lived session token owned by a user |

**Relationships**

- `User` → `RefreshToken` — one-to-many (a user can have multiple active sessions)
- `User` ↔ `Role` — many-to-many via the `UserRoles` join table

Domain entities use private setters and factory/method-based state changes (`User.Create()`, `AssignRole()`, `IssueRefreshToken()`, etc.).

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`
- SQL Server (local, Docker, or Azure SQL)

## Quick Start

```bash
# 1. Configure environment variables
cp .env.example .env
# Edit .env — ensure the SQL password matches your SQL Server instance

# 2. (Optional) Start SQL Server via Docker
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest

# 3. Restore and build
dotnet restore CleanArchitecture.sln
dotnet build CleanArchitecture.sln

# 4. Apply database migrations
dotnet ef database update \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Api/Api.csproj

# 5. Run the API
dotnet run --project src/Api/Api.csproj
```

Open Swagger UI at `https://localhost:5001/swagger`.

## API Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/api/health` | No | Health check |
| `POST` | `/api/auth/register` | No | Register a new user |
| `POST` | `/api/auth/login` | No | Login and receive tokens |
| `POST` | `/api/auth/forgot-password` | No | Request password reset email |
| `POST` | `/api/auth/reset-password` | No | Reset password with token |
| `POST` | `/api/auth/refresh-token` | No | Rotate refresh token |
| `GET` | `/api/auth/me` | Bearer | Get current user profile |

### Example: Register

```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Jane",
    "lastName": "Doe",
    "email": "jane@example.com",
    "password": "Password1",
    "confirmPassword": "Password1"
  }'
```

### Example: Login

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "jane@example.com", "password": "Password1"}'
```

### Example: Get current user

```bash
curl https://localhost:5001/api/auth/me \
  -H "Authorization: Bearer <access_token>"
```

All endpoints return a wrapped `ApiResponse<T>` with `success`, `data`, and `message` fields.

## Configuration

Secrets and environment-specific values live in `.env` (gitignored). Copy from the template:

```bash
cp .env.example .env
```

| Variable | Purpose |
|----------|---------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `JwtSettings__Secret` | JWT signing key (min. 32 characters) |
| `JwtSettings__Issuer` / `JwtSettings__Audience` | JWT token validation |
| `JwtSettings__ExpirationInMinutes` | Access token lifetime |
| `ASPNETCORE_ENVIRONMENT` | `Development`, `Staging`, or `Production` |

Non-secret defaults (Serilog, etc.) remain in `src/Api/appsettings.json`. Environment variables from `.env` override those values at runtime and during EF migrations.

> **Docker note:** If you use the Docker command below, set `MSSQL_SA_PASSWORD` to the same value as the password in your `.env` connection string.

## Database Schema

Migrations create the following tables:

| Table | Purpose |
|-------|---------|
| `Users` | User accounts with audit fields and soft-delete |
| `Roles` | Authorization roles |
| `UserRoles` | Many-to-many join between users and roles |
| `RefreshTokens` | Refresh tokens linked to users (cascade delete) |

## Migrations

```bash
# Add a new migration
dotnet ef migrations add MigrationName \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Api/Api.csproj \
  --output-dir Persistence/Migrations

# Apply migrations
dotnet ef database update \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Api/Api.csproj
```

## Docker SQL Server

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```

If the container already exists:

```bash
docker start sqlserver
```

## Layer Dependencies

```
Api → Application → Domain
Api → Infrastructure → Application → Domain
```

Domain has zero project references. All dependencies point inward.
