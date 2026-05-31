# Clean Architecture ASP.NET Core 9 Web API

Production-ready ASP.NET Core 9 Web API using Clean Architecture — authentication domain model, CQRS application layer, JWT, ASP.NET Core password hashing, and SQL Server via EF Core.

## Project Structure

```
src/
├── Api/              # HTTP entry point — controllers, middleware, Swagger, Serilog
├── Application/      # CQRS commands/queries, DTOs, validators, interfaces
├── Domain/           # Entities, domain rules (zero external dependencies)
└── Infrastructure/   # EF Core, SQL Server, JWT, email, password hashing
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

### Application Layer

Organized with **CQRS** (MediatR) and **FluentValidation**:

```
Auth/
├── Commands/     Register, Login, ForgotPassword, ResetPassword, RefreshToken
├── Queries/      GetUserById
├── DTOs/         Requests + Responses
├── Validators/   Request and command validators
└── Services/     AuthService (facade over MediatR)
```

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

> Migrations and default role seeding also run automatically on startup.

## API Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/api/health` | No | Health check |
| `POST` | `/api/auth/register` | No | Register user, send email verification |
| `POST` | `/api/auth/confirm-email` | No | Verify email with token from registration |
| `POST` | `/api/auth/login` | No | Login and receive JWT tokens |
| `POST` | `/api/auth/forgot-password` | No | Request password reset email |
| `POST` | `/api/auth/reset-password` | No | Reset password with token |
| `POST` | `/api/auth/refresh-token` | No | Rotate refresh token |
| `GET` | `/api/auth/me` | Bearer | Get current user profile |

All endpoints return a wrapped `ApiResponse<T>` with `success`, `data`, and `message` fields.

### Registration (`POST /api/auth/register`)

1. Validates request (FluentValidation)
2. Checks for duplicate email → `409 Conflict`
3. Hashes password (ASP.NET Core `PasswordHasher`)
4. Saves user and assigns default `User` role
5. Generates email verification token (24h) and sends confirmation email
6. Returns user profile — **no JWT tokens** until the user logs in

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

**Response:**

```json
{
  "success": true,
  "data": {
    "user": {
      "id": "...",
      "firstName": "Jane",
      "lastName": "Doe",
      "email": "jane@example.com",
      "emailConfirmed": false,
      "roles": ["User"]
    }
  },
  "message": "Registration successful. Please check your email to verify your account."
}
```

> **Development:** Verification and reset tokens are logged to the console by `EmailService` (no real SMTP configured).

### Confirm email (`POST /api/auth/confirm-email`)

Required before login. Use the token from the registration email (or API logs in development).

```bash
curl -X POST https://localhost:5001/api/auth/confirm-email \
  -H "Content-Type: application/json" \
  -d '{"email": "jane@example.com", "token": "<verification_token>"}'
```

### Login (`POST /api/auth/login`)

1. Validates request (email format, password required)
2. Looks up user by email
3. Verifies password hash
4. Requires email to be confirmed
5. Generates JWT access token (15 min) and refresh token (7 days)
6. Persists refresh token to the database
7. Returns `AuthResponse`

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "jane@example.com", "password": "Password1"}'
```

**Success response:**

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "base64...",
    "accessTokenExpiresAt": "2026-05-31T12:15:00Z",
    "user": {
      "id": "...",
      "firstName": "Jane",
      "lastName": "Doe",
      "email": "jane@example.com",
      "emailConfirmed": true,
      "roles": ["User"]
    }
  },
  "message": "Login successful."
}
```

**Error responses:**

| Status | Condition | Message |
|--------|-----------|---------|
| `400` | Invalid request body | Validation errors (e.g. `"Email is required."`) |
| `401` | Unknown email or wrong password | `"Invalid email or password."` |
| `403` | Email not verified | `"Please verify your email address before logging in."` |

> Invalid email and wrong password return the same `401` message to prevent account enumeration.

### Get current user

```bash
curl https://localhost:5001/api/auth/me \
  -H "Authorization: Bearer <access_token>"
```

## Authentication

### JWT claims

| Claim | Description |
|-------|-------------|
| `userId` | User identifier |
| `email` | User email |
| `role` | One claim per role (e.g. `User`, `Admin`) |

### Token expiration

| Token | Lifetime | Storage | Purpose |
|-------|----------|---------|---------|
| **Access token** | 15 minutes | Client only | Sent on every API request |
| **Refresh token** | 7 days | Database (`RefreshTokens`) | Obtain new access token via `/api/auth/refresh-token` |

Access tokens are short-lived to limit exposure if stolen. Refresh tokens are stored server-side, rotated on use, and revoked on password reset.

### Password hashing

`PasswordService` uses ASP.NET Core Identity's `PasswordHasher<User>` (PBKDF2, per-password salt, 100k iterations).

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
| `JwtSettings__AccessTokenExpirationMinutes` | Access token lifetime (default: 15) |
| `JwtSettings__RefreshTokenExpirationInDays` | Refresh token lifetime (default: 7) |
| `ASPNETCORE_ENVIRONMENT` | `Development`, `Staging`, or `Production` |

Non-secret defaults remain in `src/Api/appsettings.json`. Environment variables from `.env` override those values at runtime and during EF migrations.

> **Docker note:** Set `MSSQL_SA_PASSWORD` to the same value as the password in your `.env` connection string.

## Database Schema

| Table | Purpose |
|-------|---------|
| `Users` | User accounts with audit fields and soft-delete |
| `Roles` | Authorization roles |
| `UserRoles` | Many-to-many join between users and roles |
| `RefreshTokens` | Refresh tokens linked to users (cascade delete) |

## Entity Framework Core

### Configuration

| Component | Location | Purpose |
|-----------|----------|---------|
| `ApplicationDbContext` | `Infrastructure/Persistence/` | DbSets, audit timestamps, soft-delete filter |
| `UserConfiguration` | `Configurations/` | User properties + one-to-many refresh tokens |
| `UserRoleConfiguration` | `Configurations/` | Many-to-many User ↔ Role via `UserRoles` |
| `RoleConfiguration` | `Configurations/` | Role properties + `HasData` seed metadata |
| `RefreshTokenConfiguration` | `Configurations/` | Token properties, indexes, FK to User |
| `ApplicationDbContextFactory` | `Persistence/` | Design-time factory for CLI migrations |
| `DatabaseExtensions` | `Persistence/` | Auto-migrate + runtime role seed on startup |

**Default roles seeded:** `Admin`, `User` (via migration + runtime fallback)

### Migration Commands

```bash
# Install EF Core CLI (once)
dotnet tool install --global dotnet-ef

# Apply all pending migrations
dotnet ef database update \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Api/Api.csproj

# Add a new migration
dotnet ef migrations add YourMigrationName \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Api/Api.csproj \
  --output-dir Persistence/Migrations

# Remove last migration (if not applied)
dotnet ef migrations remove \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Api/Api.csproj

# Generate SQL script
dotnet ef migrations script \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Api/Api.csproj \
  --output migrations.sql

# List migrations
dotnet ef migrations list \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Api/Api.csproj
```

### Migrations History

| Migration | Description |
|-----------|-------------|
| `InitialCreate` | Creates `Users` table |
| `AddAuthEntities` | Adds `Roles`, `RefreshTokens`, `UserRoles`; updates `Users` |
| `SeedDefaultRoles` | Inserts `Admin` and `User` roles (idempotent) |

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
