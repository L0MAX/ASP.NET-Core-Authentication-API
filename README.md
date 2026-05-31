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
| `POST` | `/api/auth/send-verification` | No | Resend email verification link |
| `POST` | `/api/auth/verify-email` | No | Verify email with token |
| `POST` | `/api/auth/login` | No | Login and receive JWT tokens |
| `POST` | `/api/auth/forgot-password` | No | Request password reset email |
| `POST` | `/api/auth/reset-password` | No | Reset password with token |
| `POST` | `/api/auth/refresh` | No | Rotate refresh token, issue new access token |
| `GET` | `/api/auth/me` | Bearer | Get current user profile |
| `GET` | `/api/admin/dashboard` | Admin | Admin dashboard (sample) |
| `GET` | `/api/admin/settings` | Admin | Admin settings (sample) |
| `GET` | `/api/user/dashboard` | User or Admin | User dashboard (sample) |
| `GET` | `/api/user/activity` | User or Admin | User activity (sample) |

All endpoints return a standardized `ApiResponse<T>` envelope.

### Response envelope

**Success example:**

```json
{
  "success": true,
  "data": { },
  "message": "Login successful.",
  "traceId": "0HN5...",
  "timestamp": "2026-05-31T12:00:00.0000000+00:00"
}
```

**Validation error example (`400`):**

```json
{
  "success": false,
  "message": "One or more validation errors occurred.",
  "errorCode": "VALIDATION_FAILED",
  "errors": {
    "email": ["Email is required."],
    "password": ["Password must be at least 8 characters."]
  },
  "traceId": "0HN5...",
  "timestamp": "2026-05-31T12:00:00.0000000+00:00"
}
```

**Business error example (`401`):**

```json
{
  "success": false,
  "message": "Invalid email or password.",
  "errorCode": "UNAUTHORIZED",
  "traceId": "0HN5...",
  "timestamp": "2026-05-31T12:00:00.0000000+00:00"
}
```

| Field | Description |
|-------|-------------|
| `success` | Whether the request succeeded |
| `data` | Payload on success (`null` on errors) |
| `message` | Human-readable summary |
| `errors` | Field-level validation errors (key = camelCase property name) |
| `errorCode` | Machine-readable error identifier |
| `traceId` | Correlation ID for log lookup (also returned as `X-Correlation-ID` header) |
| `timestamp` | UTC time the response was generated |
| `details` | Stack trace / diagnostics — **Development only**, for unexpected `500` errors |

### Exception handling

Unhandled exceptions are caught by `GlobalExceptionHandlingMiddleware` and mapped to consistent HTTP status codes and error codes:

| Exception | HTTP | `errorCode` |
|-----------|------|-------------|
| `ValidationException` (FluentValidation) | `400` | `VALIDATION_FAILED` |
| `DomainException` | `400` | `DOMAIN_RULE_VIOLATION` |
| `ArgumentException` / `BadHttpRequestException` | `400` | `INVALID_ARGUMENT` / `BAD_REQUEST` |
| `UnauthorizedException` | `401` | `UNAUTHORIZED` |
| `ForbiddenException` | `403` | `FORBIDDEN` |
| `NotFoundException` | `404` | `NOT_FOUND` |
| `ConflictException` | `409` | `CONFLICT` |
| Unhandled | `500` | `INTERNAL_ERROR` |

**Logging behavior:**

- **500** — logged at `Error` with full exception and structured properties (`TraceId`, `ErrorCode`, `StatusCode`, path)
- **Validation** — logged at `Information` with field error count (no stack trace noise)
- **Other handled 4xx** — logged at `Warning` with error code and message

Pass `X-Correlation-ID` on requests to propagate a trace ID across services; otherwise one is generated automatically.

### Registration (`POST /api/auth/register`)

1. Validates request (FluentValidation)
2. Checks for duplicate email → `409 Conflict`
3. Hashes password (ASP.NET Core `PasswordHasher`)
4. Saves user and assigns default `User` role
5. Generates email verification token (24h, stored hashed in DB) and sends confirmation email
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

### Send verification (`POST /api/auth/send-verification`)

Resend a verification email for unconfirmed accounts. Returns success even if the email is unknown (prevents enumeration).

```bash
curl -X POST https://localhost:5001/api/auth/send-verification \
  -H "Content-Type: application/json" \
  -d '{"email": "jane@example.com"}'
```

| Status | Condition | Message |
|--------|-----------|---------|
| `200` | Request accepted | `"If the email exists and is not yet verified, a verification link has been sent."` |
| `409` | Email already verified | `"Email address is already verified."` |

### Verify email (`POST /api/auth/verify-email`)

Required before login. Invalidates the token after use (one-time).

```bash
curl -X POST https://localhost:5001/api/auth/verify-email \
  -H "Content-Type: application/json" \
  -d '{"email": "jane@example.com", "token": "<verification_token>"}'
```

| Status | Condition | Message |
|--------|-----------|---------|
| `200` | Success | `"Email verified successfully. You can now log in."` |
| `401` | Invalid or expired token | `"Invalid or expired email verification token."` |

**Security:** Plain tokens are sent by email only. Only an HMAC-SHA256 hash is stored in `EmailVerificationTokens`. Tokens expire after 24 hours (configurable). Issuing a new token invalidates previous unused tokens for that user.

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

### Forgot password (`POST /api/auth/forgot-password`)

1. Validates email
2. Looks up user (returns success even if unknown — **OWASP: no account enumeration**)
3. Invalidates previous unused reset tokens for that user
4. Generates cryptographically random token, stores **HMAC-SHA256 hash** in DB (1h expiry)
5. Emails reset link: `{ResetLinkBaseUrl}?email=...&token=...`

```bash
curl -X POST https://localhost:5001/api/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{"email": "jane@example.com"}'
```

**Response:** Always `200` with `"If the email exists, a password reset link has been sent."` when the email format is valid.

> **Development:** Reset links are logged to the console by `EmailService`.

### Reset password (`POST /api/auth/reset-password`)

1. Validates email, token, and new password (strength rules)
2. Verifies token hash, expiry, and one-time use
3. Ensures token belongs to the given email
4. Hashes and saves new password
5. Revokes all refresh tokens (forces re-login on all devices)
6. Marks reset token as used

```bash
curl -X POST https://localhost:5001/api/auth/reset-password \
  -H "Content-Type: application/json" \
  -d '{
    "email": "jane@example.com",
    "token": "<token_from_reset_link>",
    "newPassword": "NewPassword1",
    "confirmPassword": "NewPassword1"
  }'
```

| Status | Condition | Message |
|--------|-----------|---------|
| `200` | Success | `"Password reset successful."` |
| `400` | Validation failure | Password strength / mismatch errors |
| `401` | Invalid or expired token | `"Invalid or expired password reset token."` |

**OWASP practices applied:**

- Generic response on forgot-password (no email enumeration)
- Short-lived tokens (1 hour, configurable)
- One-time use tokens stored as HMAC hashes (plain token only in email)
- Previous reset tokens invalidated when a new one is issued
- All sessions revoked after password change
- Same error message for invalid email/token mismatches on reset

### Refresh token (`POST /api/auth/refresh`)

1. Validates request (refresh token required)
2. Looks up token in the database
3. Verifies token is not revoked
4. Verifies token is not expired
5. Revokes the previous refresh token (rotation)
6. Generates new access token and refresh token
7. Persists the new refresh token and returns `AuthResponse`

```bash
curl -X POST https://localhost:5001/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken": "<refresh_token>"}'
```

**Success response:** Same shape as login — new `accessToken`, `refreshToken`, `accessTokenExpiresAt`, and `user`.

**Error responses:**

| Status | Condition | Message |
|--------|-----------|---------|
| `400` | Missing refresh token | `"Refresh token is required."` |
| `401` | Unknown, revoked, or expired token | `"Invalid or expired refresh token."` |

**Security practices:**

- **Token rotation** — each refresh invalidates the previous token and issues a new one
- **Reuse detection** — if a revoked token is reused, all user sessions are revoked (possible token theft)
- **Generic errors** — invalid, expired, and revoked tokens return the same message
- **Server-side storage** — refresh tokens are persisted in `RefreshTokens` and can be revoked individually or in bulk (e.g. on password reset)
- **Cryptographic tokens** — 512-bit random values, not predictable JWTs

> Always replace the stored refresh token with the new one from the response. Do not reuse old refresh tokens.

### Get current user

```bash
curl https://localhost:5001/api/auth/me \
  -H "Authorization: Bearer <access_token>"
```

### Admin endpoints (Admin role required)

```bash
# Admin dashboard
curl https://localhost:5001/api/admin/dashboard \
  -H "Authorization: Bearer <access_token>"

# Admin settings
curl https://localhost:5001/api/admin/settings \
  -H "Authorization: Bearer <access_token>"
```

Returns `403 Forbidden` if the token does not include the `Admin` role.

### User endpoints (User or Admin role)

```bash
# User dashboard
curl https://localhost:5001/api/user/dashboard \
  -H "Authorization: Bearer <access_token>"

# User activity
curl https://localhost:5001/api/user/activity \
  -H "Authorization: Bearer <access_token>"
```

Returns `403 Forbidden` if the token has no `User` or `Admin` role claim.

## Authentication

### JWT claims

| Claim | Description |
|-------|-------------|
| `userId` | User identifier |
| `email` | User email |
| `role` | One claim per role (e.g. `User`, `Admin`) |

### Role-based authorization

Two roles are seeded on startup: **Admin** and **User**. New registrations receive the **User** role by default.

#### Authorization policies

Policies are registered in `AuthorizationConfiguration` and referenced from controllers:

| Policy | Constant | Requirement | Example usage |
|--------|----------|-------------|---------------|
| Admin only | `AuthorizationPolicies.AdminOnly` | Must have `Admin` role | `[Authorize(Policy = AuthorizationPolicies.AdminOnly)]` |
| User or Admin | `AuthorizationPolicies.UserOrAdmin` | Must have `User` or `Admin` role | `[Authorize(Policy = AuthorizationPolicies.UserOrAdmin)]` |

You can also use role attributes directly:

```csharp
[Authorize(Roles = AuthRoles.Admin)]
public class AdminController : ControllerBase { ... }

[Authorize(Roles = $"{AuthRoles.User},{AuthRoles.Admin}")]
public IActionResult GetActivity() { ... }
```

#### Authorization flow

```
Client request with Authorization: Bearer <access_token>
        │
        ▼
┌─────────────────────────┐
│  JWT Bearer middleware  │  Validate signature, issuer, audience, expiry
└───────────┬─────────────┘
            │ valid token → build ClaimsPrincipal (userId, email, role claims)
            ▼
┌─────────────────────────┐
│  [Authorize] attribute  │  Endpoint requires authentication + role/policy
└───────────┬─────────────┘
            │
     ┌──────┴──────┐
     │ no token or │──► 401 Unauthorized
     │ invalid JWT │
     └──────┬──────┘
            │ authenticated
            ▼
┌─────────────────────────┐
│  Role / policy check    │  Compare JWT `role` claims to required roles
└───────────┬─────────────┘
            │
     ┌──────┴──────┐
     │ missing role│──► 403 Forbidden
     └──────┬──────┘
            │ authorized
            ▼
      Controller action runs
```

1. **Login** — `AuthService` loads the user's roles from `UserRoles` and `JwtService` embeds each role as a separate `role` claim in the access token.
2. **Request** — `JwtBearerConfiguration` maps the JWT `role` claim to `RoleClaimType`, so ASP.NET Core authorization can evaluate `[Authorize(Roles = ...)]` and named policies.
3. **Enforcement** — Missing or invalid tokens return **401**. Valid tokens without the required role return **403**.

#### Promoting a user to Admin

By default, registered users only have the **User** role. To test admin endpoints, assign the Admin role in SQL Server:

```sql
-- Replace with your user's Id and the Admin role Id from the Roles table
INSERT INTO UserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM Users u, Roles r
WHERE u.Email = 'admin@example.com' AND r.Name = 'Admin';
```

Log in again to receive a new access token that includes the `Admin` role claim.

### Token expiration

| Token | Lifetime | Storage | Purpose |
|-------|----------|---------|---------|
| **Access token** | 15 minutes | Client only | Sent on every API request |
| **Refresh token** | 7 days | Database (`RefreshTokens`) | Obtain new access token via `/api/auth/refresh` |

Access tokens are short-lived to limit exposure if stolen. Refresh tokens are stored server-side, rotated on each use, and revoked on password reset or reuse detection.

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
| `EmailVerificationSettings__TokenExpirationHours` | Email verification token lifetime (default: 24) |
| `PasswordResetSettings__TokenExpirationHours` | Password reset token lifetime (default: 1) |
| `PasswordResetSettings__ResetLinkBaseUrl` | Frontend reset page URL for email links |
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
| `EmailVerificationTokens` | Hashed one-time email verification tokens (cascade delete) |
| `PasswordResetTokens` | Hashed one-time password reset tokens (cascade delete) |

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
| `AddEmailVerificationTokens` | Creates `EmailVerificationTokens` table |
| `AddPasswordResetTokens` | Creates `PasswordResetTokens` table |

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
