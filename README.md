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

Update `src/Api/appsettings.json`:

| Section | Purpose |
|---------|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `JwtSettings` | JWT issuer, audience, secret, token lifetime |
| `Serilog` | Structured logging (console + rolling file) |

**Important:** Replace the JWT secret and SQL password before deploying to production. Use User Secrets or environment variables for local development:

```bash
dotnet user-secrets init --project src/Api/Api.csproj
dotnet user-secrets set "JwtSettings:Secret" "your-production-secret-at-least-32-chars" --project src/Api/Api.csproj
```

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
