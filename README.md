# ClefCraft Backend

ASP.NET Core 10 API for ClefCraft: boards, calendar, comments and notifications. PostgreSQL holds
the data; EF Core manages the schema. The Angular client lives in the `ClefCraft-Frontend` repo.

## Prerequisites

- **.NET 10 SDK.** `global.json` requires 10.0.100 or a later 10.0 SDK.
- **PostgreSQL 18**, installed natively (the default). If you'd rather not install it, the
  [optional Docker setup](#optional-postgresql-in-docker) runs only the database in a container.
- **EF Core tools 10**, for resetting the database and working with migrations:

  ```bash
  dotnet tool install --global dotnet-ef --version "10.*"
  ```

## Configuration (user-secrets)

Secrets are never committed. Set them once for the API project, from the repo root:

```bash
dotnet user-secrets set "ConnectionStrings:ClefCraftDatabaseConnectionString" "Host=localhost;Port=5432;Database=clefcraft_db;Username=postgres;Password=<your password>" --project ClefCraft.Api
dotnet user-secrets set "JwtSettings:Key" "<random string, at least 32 bytes>" --project ClefCraft.Api
dotnet user-secrets set "DevSeed:AdminPassword" "<password>" --project ClefCraft.Api
dotnet user-secrets set "DevSeed:UserPassword" "<password>" --project ClefCraft.Api
```

| Setting | Required | Notes |
|---|---|---|
| `ConnectionStrings:ClefCraftDatabaseConnectionString` | yes | Both DbContexts use it. The database is created on first start. |
| `JwtSettings:Key` | yes | At least 32 bytes (UTF-8). `Issuer` and `Audience` come from `appsettings.json`. |
| `DevSeed:AdminPassword`, `DevSeed:UserPassword` | for the dev accounts | Must meet the Identity password policy: 6+ characters with an upper-case letter, a lower-case letter, a digit and a symbol. |
| `AIService:BaseUrl` | yes | Defaults to `http://localhost:8000` in `appsettings.json`. Must be an absolute http(s) URL. |
| `AIService:PredictApiKey` | no | Must match `AI_PREDICT_API_KEY` in the `clef_ai` service. Without it, attendance predictions are simply empty. |

The required settings are validated at startup: a missing or invalid value stops the API with a
message naming the setting, before it touches the database.

## Database

Migrations run automatically when the API starts, in every environment except `Testing`. There is
one `Initial` migration per DbContext (`ClefCraft.Persistence` and `ClefCraft.Identity`), and the
Persistence one also creates the standard statuses and priorities every board offers.

On the very first start the database doesn't exist yet, so EF logs two errors ("An error occurred
using the connection to database ...") while checking for pending migrations, then creates the
database and applies the migrations. Those two lines are expected.

### Development accounts

In the Development environment, startup creates two accounts (only if they don't exist yet), with
the passwords from the `DevSeed:*` secrets:

| Account | Role | Used by the demo data for |
|---|---|---|
| `admin@localhost.com` | Administrator | boards and event types |
| `user@localhost.com` | — | the calendar |

Existing accounts are never changed. To pick up a changed `DevSeed` password, reset the database.

### Reset

```bash
dotnet ef database drop --project ClefCraft.Persistence --startup-project ClefCraft.Api --context ClefCraftDatabaseContext
```

This drops the whole database (both contexts). The next start of the API recreates it.

### Demo data

`scripts/dev-seed/` has three psql scripts. Load them into a freshly migrated database, after the
first start of the API, in this order:

```bash
psql -v ON_ERROR_STOP=1 -h localhost -U postgres -d clefcraft_db -f scripts/dev-seed/01_board_seed_postgresql.sql
psql -v ON_ERROR_STOP=1 -h localhost -U postgres -d clefcraft_db -f scripts/dev-seed/02_event_types_postgresql.sql
psql -v ON_ERROR_STOP=1 -h localhost -U postgres -d clefcraft_db -f scripts/dev-seed/03_calendar_seed_postgresql.sql
```

Each script runs in one transaction and refuses to run twice. Sign in as `admin@localhost.com` for
the boards and as `user@localhost.com` for the calendar (the events are in June 2026).

## Run

```bash
dotnet dev-certs https --trust
dotnet run --project ClefCraft.Api --launch-profile https
```

The API listens on `https://localhost:7287` (and `http://localhost:5166`, which redirects to
HTTPS). Swagger UI is at `https://localhost:7287/swagger` in Development. The frontend calls the
HTTPS address, so the browser has to trust the development certificate.

## Test

```bash
dotnet test ClefCraft.sln
```

No database is needed: the tests use EF InMemory and SQLite.

## Optional: PostgreSQL in Docker

`docker-compose.yml` runs only PostgreSQL 18, with its data in a named volume:

```bash
cp .env.example .env
docker compose up -d
```

Choose your own password in `.env` (git ignores it) and use the same values in your connection
string. The container publishes port 5432, so stop a native PostgreSQL first. The API still runs
natively.

## Repository layout

| Project | Contents |
|---|---|
| `ClefCraft.Api` | Controllers, SignalR hub, middleware, startup |
| `ClefCraft.Application` | Commands, queries, DTOs, validation |
| `ClefCraft.Domain` | Entities |
| `ClefCraft.Persistence` | `ClefCraftDatabaseContext`, repositories, migrations, reference data |
| `ClefCraft.Identity` | `ClefCraftIdentityDbContext`, authentication, development accounts |
| `ClefCraft.Infrastructure` | Reminder scheduling and sending, board/calendar access checks, attachments, AI client |
| `*.UnitTests`, `*.IntegrationTests` | Tests |
| `scripts/dev-seed` | Demo data |
| `docs/PLAN.md` | Roadmap |
