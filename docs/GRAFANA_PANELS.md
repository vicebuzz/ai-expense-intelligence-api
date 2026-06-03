# Grafana → React panel mapping

Source dashboard: `grafana-expenses/grafana-dashboard-json`

| Grafana panel | Type | React component | API endpoint |
|---------------|------|-----------------|--------------|
| Expenditure by category for given month | piechart | `CategoryPieChart` | `GET /api/analytics/spend-by-category?month=` |
| Total Expenditure by category | piechart | `CategoryPieChart` | `GET /api/analytics/spend-by-category` |
| Savings | gauge | `GaugeDisplay` | `GET /api/analytics/savings/latest` |
| Spending breakdown by 50/30/20 rule | piechart | `CategoryPieChart` | `GET /api/analytics/budget-502030/actual?month=` |
| Required spending breakdown (50/30/20) | bargauge | `HorizontalBudgetBars` | `GET /api/analytics/budget-502030/target?month=` |
| Monthly Expenditure By Selected Categories | timeseries | `TimeSeriesChart` | `GET /api/analytics/monthly-by-categories?categories=` |
| Daily expenditures | timeseries | `TimeSeriesChart` | `GET /api/analytics/daily-expenditures` |
| Monthly salary income | barchart | `StackedBarChart` | `GET /api/analytics/monthly-salary` |
| Monthly Earnings vs Expenditures | barchart | `ComparisonBarChart` | `GET /api/analytics/earnings-vs-expenditures` |
| Spending by category by month compared | barchart | `StackedBarChart` | `GET /api/analytics/category-pivot?metric=amount` |
| Amount of times spend on category per month | barchart | `StackedBarChart` | `GET /api/analytics/category-pivot?metric=count` |
| Total spending per category per month | table | `DataTable` | `GET /api/analytics/category-month-table` |
| Total Expenses vs Earnings | piechart | `CategoryPieChart` | `GET /api/analytics/expenses-vs-earnings` |
| Geomap (coordinates) | geomap | — | Not implemented (no coordinates in API) |

## Template variables

| Grafana variable | React equivalent |
|------------------|------------------|
| `$Month` (`Month YYYY`) | Month `<select>` → `yyyy-MM` query param |
| `$Source` (multi category) | Category chips → `monthly-by-categories` |
