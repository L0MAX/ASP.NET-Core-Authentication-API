# Clean Architecture ASP.NET Core 9 Web API

Production-ready ASP.NET Core 9 Web API scaffold using Clean Architecture.

## Project Structure

```
src/
├── Api/              # Presentation layer (HTTP, middleware, Swagger)
├── Application/      # Use cases, interfaces, application services
├── Domain/           # Entities, domain rules (no external dependencies)
└── Infrastructure/   # EF Core, SQL Server, JWT, external integrations
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (local, Docker, or Azure SQL)

## Quick Start

```bash
# Configure environment variables
cp .env.example .env
# Edit .env with your SQL Server password and JWT secret

# Restore and build
dotnet restore CleanArchitecture.sln
dotnet build CleanArchitecture.sln

# Apply database migrations
dotnet ef database update \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Api/Api.csproj

# Run the API
dotnet run --project src/Api/Api.csproj
```

Open Swagger UI at `https://localhost:5001/swagger`.

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
| `JwtSettings__ExpirationInMinutes` | Token lifetime |
| `ASPNETCORE_ENVIRONMENT` | `Development`, `Staging`, or `Production` |

Non-secret defaults (Serilog, etc.) remain in `src/Api/appsettings.json`. Environment variables from `.env` override those values at runtime and during EF migrations.

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

## Docker SQL Server (optional)

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```

## Layer Dependencies

```
Api → Application → Domain
Api → Infrastructure → Application → Domain
```

Domain has zero project references. All dependencies point inward.
