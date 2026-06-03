# Architecture

## Overview

AI Expense Intelligence is a small polyglot system for importing bank-style transactions, categorising them, and exposing spending analytics over HTTP.

```
┌─────────────┐     REST      ┌──────────────────┐     HTTP      ┌─────────────────────┐
│   React     │ ────────────► │  ASP.NET Core    │ ────────────► │  Python FastAPI     │
│  Dashboard  │               │       API          │               │  (rules / ML)       │
└─────────────┘               └────────┬───────────┘               └─────────────────────┘
                                       │
                                       │ EF Core
                                       ▼
                              ┌──────────────────┐
                              │   PostgreSQL     │
                              └──────────────────┘
```

## Components

| Layer | Responsibility |
|-------|----------------|
| **ExpenseIntelligence.Api** | REST endpoints, CSV import, Swagger, CORS for dashboard |
| **ExpenseIntelligence.Domain** | `Transaction` entity and category catalog models |
| **ExpenseIntelligence.Infrastructure** | EF Core persistence, analytics queries, category config |
| **categorization service** | Keyword rules engine (`POST /categorize`); ML can be added later |
| **web/dashboard** | Recharts visualisations over analytics endpoints |

## Data flow

1. User uploads CSV or posts a transaction via the API.
2. If category is missing, API calls the categorization microservice.
3. Transactions are stored in PostgreSQL.
4. Analytics endpoints aggregate spend (ported from original Grafana SQL queries).
5. React dashboard consumes JSON and renders charts.

## Database modes

- **Legacy** (`Database:UseLegacySchema: true`): maps EF to `accountbalancemanagement` (`change_id`, `date`, `source`, `description`, `expenditure`, `amount`) — same as the original Python/grafana pipeline.
- **Portfolio** (`false`): separate `transactions` table created by EF migrations for demo/Docker use.

Connection string: `ConnectionStrings:DefaultConnection` in `appsettings.Local.json` (gitignored) or `ConnectionStrings__DefaultConnection` env var for remote hosts.

## Origin

This project generalises a personal grafana-expenses workflow:

- UK bank CSV parsing (`dd/mm/yyyy`, debit/credit columns)
- Category taxonomy in `seed/categories.json`
- Monthly spend and merchant rollups from `queries.sql`

## Azure (target deployment)

- **API**: Azure App Service or Container Apps
- **Database**: Azure Database for PostgreSQL
- **Categorization**: Container Apps (Python image)
- **Dashboard**: Static Web Apps or App Service static hosting
- **CI/CD**: GitHub Actions (see `.github/workflows/dotnet.yml`)
