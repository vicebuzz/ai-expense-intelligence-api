# Azure deployment notes

Suggested production layout:

| Resource | Purpose |
|----------|---------|
| Azure Database for PostgreSQL | `transactions` table via EF migrations |
| Azure Container Apps | `api` + `categorization` images from `docker-compose.yml` |
| Azure Static Web Apps | `web/dashboard` build output (`npm run build`) |
| Azure Key Vault | Connection strings and secrets (not in repo) |

Environment variables for the API:

- `ConnectionStrings__DefaultConnection`
- `CategorizationService__BaseUrl`
- `Cors__Origins__0` (dashboard URL)

Build and push images:

```bash
docker compose build
# tag and push to Azure Container Registry, then deploy via Container Apps
```
