# AI Expense Intelligence API

Portfolio project: an AI-assisted expense platform that imports bank-style transactions, auto-categorises spending, and exposes analytics through a REST API and React dashboard.

Evolved from a personal Grafana + PostgreSQL workflow; this repo uses **synthetic demo data only** (no real bank exports).

## Features

- **CSV upload** — UK-style columns (`dd/mm/yyyy`, debit/credit, description, optional category)
- **Auto categorisation** — Python microservice with keyword rules (ML-ready extension point)
- **REST API** — Transactions, categories, analytics, OpenAPI/Swagger
- **Spending dashboard** — React + Recharts (monthly by category, averages, top merchants)
- **Docker Compose** — PostgreSQL, API, categorization service

## Tech stack

| Layer | Technology |
|-------|------------|
| API | ASP.NET Core 7, EF Core, PostgreSQL |
| Categorisation | Python 3.11, FastAPI |
| Dashboard | React 18, Vite, Recharts |
| Deploy target | Azure App Service / Container Apps (see `infra/`) |

## Repository structure

```
├── src/
│   ExpenseIntelligence.Api/          # REST API + CSV import
│   ExpenseIntelligence.Domain/       # Entities & models
│   ExpenseIntelligence.Infrastructure/ # EF Core, analytics, migrations
├── services/categorization/            # FastAPI rules engine
├── web/dashboard/                      # React analytics UI
├── seed/                               # Demo categories + transactions
├── docs/                               # Architecture & API examples
├── docker-compose.yml
└── ExpenseIntelligence.sln
```

## Quick start

### Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for PostgreSQL + optional full stack)
- [Node.js 18+](https://nodejs.org/) (dashboard)
- [Python 3.11+](https://www.python.org/) (categorization service, optional if using Docker)

### 1. Start database (and categorization)

```bash
docker compose up -d postgres categorization
```

### 2. Run the API

```bash
cd src/ExpenseIntelligence.Api
dotnet restore
dotnet run
```

Swagger: [http://localhost:5000/swagger](http://localhost:5000/swagger)

With **legacy schema** (default), the API connects to your existing Postgres data and does not run migrations. With **portfolio schema** (`UseLegacySchema: false`), it migrates and seeds demo CSV data.

### 3. Run the dashboard

```bash
cd web/dashboard
npm install
npm run dev
```

Open [http://localhost:5173](http://localhost:5173) (proxies `/api` to the .NET API).

**Full local guide (existing PostgreSQL + dashboard):** see [`docs/LOCAL_SETUP.md`](docs/LOCAL_SETUP.md)  
**Grafana panel mapping:** see [`docs/GRAFANA_PANELS.md`](docs/GRAFANA_PANELS.md)

### Full stack with Docker

```bash
docker compose up --build
```

API: `http://localhost:5000` · Categorization: `http://localhost:8000` · Postgres: `localhost:5432`

## API overview

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/health` | Health check |
| `GET` | `/api/categories` | Expense & income category lists |
| `GET` | `/api/transactions` | List transactions (filters: `from`, `to`, `category`, `isExpense`) |
| `POST` | `/api/transactions` | Create transaction (auto-categorise if category omitted) |
| `POST` | `/api/import/csv` | Upload CSV file (`autoCategorize` query flag) |
| `POST` | `/api/categorization` | Categorise a description via Python service |
| `GET` | `/api/analytics/spending-by-category` | Monthly totals per category |
| `GET` | `/api/analytics/average-monthly-by-category` | Average monthly spend per category |
| `GET` | `/api/analytics/top-merchants` | Top merchants by total spend |
| `GET` | `/api/analytics/months` | Available months (dashboard filter) |
| `GET` | `/api/analytics/spend-by-category?month=` | Category pie (month / all time) |
| `GET` | `/api/analytics/budget-502030/actual` | 50/30/20 actual spend groups |
| `GET` | `/api/analytics/earnings-vs-expenditures` | Monthly income vs spend |
| `GET` | `/api/analytics/category-pivot` | Crosstab for stacked bar charts |

See Swagger for the full analytics surface.

Sample requests: [`docs/api.http`](docs/api.http)

## CSV format

Minimal portfolio format (see `seed/transactions.demo.csv`):

```csv
Transaction Date,Transaction Type,Transaction Description,Debit Amount,Credit Amount,Category
15/01/2025,DEB,FreshMart Supermarket,42.50,,Groceries
10/01/2025,CRED,Employer Payroll,,2500.00,Salary
```

Full UK export format (Sort Code / Account Number columns) is also supported — extra columns are ignored.

## Database connection (local or remote PostgreSQL)

By default the API connects to your **existing grafana-expenses database** (`FundsManagementDB`) and reads the `accountbalancemanagement` table (`change_id`, `date`, `source`, `description`, `expenditure`, `amount`).

### 1. Set your connection string (do not commit passwords)

Copy the example and edit your password:

```bash
cd src/ExpenseIntelligence.Api
cp appsettings.Local.json.example appsettings.Local.json
# Edit appsettings.Local.json — set Password=... for your local postgres user
```

Or use environment variables (good for remote / Azure):

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=FundsManagementDB;Username=postgres;Password=YOUR_PASSWORD"
```

Remote example:

```bash
export ConnectionStrings__DefaultConnection="Host=myserver.postgres.database.azure.com;Port=5432;Database=FundsManagementDB;Username=admin;Password=...;Ssl Mode=Require"
```

Or .NET user secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=FundsManagementDB;Username=postgres;Password=YOUR_PASSWORD"
```

### 2. Legacy vs portfolio schema

| Setting | Behaviour |
|---------|-----------|
| `Database:UseLegacySchema: true` (default) | Uses your filled `accountbalancemanagement` table — **no migrations** |
| `Database:UseLegacySchema: false` | Creates `transactions` table via EF migrations + demo seed (Docker compose) |

If the table was created with quoted mixed-case names, set:

```json
"LegacyTableName": "AccountBalanceManagement"
```

### 3. Verify

```bash
cd src/ExpenseIntelligence.Api && dotnet run
curl http://localhost:5000/api/health
```

You should see `transactionCount` matching your existing data and `"schema": "legacy"`.

### Base `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=FundsManagementDB;Username=postgres;Password=postgres"
  },
  "Database": {
    "UseLegacySchema": true,
    "LegacyTableName": "accountbalancemanagement"
  },
  "CategorizationService": {
    "BaseUrl": "http://localhost:8000"
  }
}
```
