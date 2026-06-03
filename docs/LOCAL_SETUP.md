# Run the full stack locally (PostgreSQL → API → React)

This guide connects the portfolio app to your **existing** `FundsManagementDB` database (same as grafana-expenses).

## Architecture

```
PostgreSQL (FundsManagementDB)
        │
        ▼
ASP.NET Core API  :5000  ← reads accountbalancemanagement
        │
        ▼
React dashboard   :5173  ← Vite proxy /api → :5000
```

Optional: Python categorization service on `:8000` (only needed for CSV import / auto-categorise).

---

## 1. PostgreSQL

Ensure Postgres is running and contains your data:

```bash
psql -U postgres -d FundsManagementDB -c "SELECT COUNT(*) FROM accountbalancemanagement;"
```

You should see your row count (not zero).

---

## 2. Configure the API connection

```bash
cd src/ExpenseIntelligence.Api
cp appsettings.Local.json.example appsettings.Local.json
```

Edit `appsettings.Local.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=FundsManagementDB;Username=postgres;Password=YOUR_PASSWORD"
  },
  "Database": {
    "UseLegacySchema": true,
    "LegacyTableName": "accountbalancemanagement"
  }
}
```

**Remote Postgres** — use the same JSON or an environment variable:

```bash
export ConnectionStrings__DefaultConnection="Host=db.example.com;Port=5432;Database=FundsManagementDB;Username=postgres;Password=...;Ssl Mode=Require"
```

If your table was created with quoted mixed-case name:

```json
"LegacyTableName": "AccountBalanceManagement"
```

---

## 3. Start the API

```bash
cd src/ExpenseIntelligence.Api
dotnet restore
dotnet run
```

Verify:

- Swagger: http://localhost:5000/swagger  
- Health: http://localhost:5000/api/health  

Expected health response:

```json
{
  "status": "healthy",
  "database": "FundsManagementDB",
  "schema": "legacy",
  "transactionCount": 1234
}
```

Test analytics:

```bash
curl "http://localhost:5000/api/analytics/months"
curl "http://localhost:5000/api/analytics/spend-by-category?month=2023-01"
```

---

## 4. Start the React dashboard

New terminal:

```bash
cd web/dashboard
npm install
npm run dev
```

Open **http://localhost:5173**

Vite proxies `/api` to `http://localhost:5000` (see `web/dashboard/vite.config.js`).

### API on a different host/port

```bash
# e.g. API running elsewhere
VITE_API_URL=http://localhost:5000 npm run dev
```

Update CORS in `appsettings.json` if the dashboard origin changes:

```json
"Cors": { "Origins": [ "http://localhost:5173" ] }
```

---

## 5. Optional: categorization service

Only required for `POST /api/import/csv` and auto-categorise on new transactions.

```bash
cd services/categorization
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn main:app --reload --port 8000
```

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `Cannot connect to PostgreSQL` | Check Postgres is running, password in `appsettings.Local.json`, database name `FundsManagementDB` |
| `transactionCount: 0` | Wrong table name — try `AccountBalanceManagement` vs `accountbalancemanagement` |
| Dashboard shows API errors | Ensure API is running on :5000; check browser Network tab |
| CORS errors | Add your dashboard URL to `Cors:Origins` in API appsettings |
| Savings gauge empty | Normal if you don't have a `savings` table |
| Month dropdown empty | No rows in DB or date column not mapped — check `/api/analytics/months` |

---

## All three services (quick reference)

```bash
# Terminal 1 — API
cd src/ExpenseIntelligence.Api && dotnet run

# Terminal 2 — Dashboard
cd web/dashboard && npm run dev

# Terminal 3 — Python (optional)
cd services/categorization && uvicorn main:app --reload
```

---

## What the dashboard shows

Panels mirror your Grafana dashboard — see [GRAFANA_PANELS.md](./GRAFANA_PANELS.md) for the full mapping.

Filters:

- **Month** → same as Grafana `$Month` (stored as `yyyy-MM` in the API)
- **Category chips** → same as Grafana `$Source` multi-select
