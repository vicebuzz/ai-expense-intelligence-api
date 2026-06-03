import { useEffect, useMemo, useState } from 'react';
import CategoryPieChart from './components/CategoryPieChart';
import ComparisonBarChart from './components/ComparisonBarChart';
import DataTable, { currencyColumn } from './components/DataTable';
import GaugeDisplay from './components/GaugeDisplay';
import HorizontalBudgetBars from './components/HorizontalBudgetBars';
import Panel from './components/Panel';
import StackedBarChart from './components/StackedBarChart';
import TimeSeriesChart from './components/TimeSeriesChart';
import AddTransactionPanel from './components/AddTransactionPanel';
import ImportPanel from './components/ImportPanel';
import EditCategoriesModal from './components/EditCategoriesModal';
import Modal from './components/Modal';
import { useDashboard } from './hooks/useDashboard';
import { colorForCategory } from './theme';
import { formatCurrency, formatMonth } from './utils/format';

function expensesVsEarningsPieData(totals) {
  if (!totals) return [];
  return [
    { category: 'Expenditures', total: totals.totalExpenses },
    { category: 'Earnings', total: totals.totalEarnings },
  ].filter((x) => x.total > 0);
}

function pivotToRows(pivot) {
  if (!pivot?.rows) return [];
  return pivot.rows.map((r) => ({ month: r.month, values: r.values }));
}

function salaryBarData(salaryRows) {
  return salaryRows.map((r) => ({
    month: r.month,
    values: { Salary: Number(r.total) },
  }));
}

function categorySeriesFromMonthly(rows) {
  const byCat = {};
  rows.forEach((r) => {
    if (!byCat[r.category]) byCat[r.category] = [];
    byCat[r.category].push({ x: r.month, y: Number(r.total) });
  });
  return Object.entries(byCat).map(([key, data]) => ({
    key,
    label: key,
    data,
  }));
}

export default function App() {
  const [selectedMonth, setSelectedMonth] = useState('');
  const [selectedCategories, setSelectedCategories] = useState([]);
  const [importOpen, setImportOpen] = useState(false);
  const [addOpen, setAddOpen] = useState(false);
  const [categoriesOpen, setCategoriesOpen] = useState(false);

  const dash = useDashboard(selectedMonth, selectedCategories);

  useEffect(() => {
    if (dash.months.length && !selectedMonth) {
      setSelectedMonth(dash.months[0].value);
    }
  }, [dash.months, selectedMonth]);

  useEffect(() => {
    if (dash.categories.length && selectedCategories.length === 0) {
      const defaults = ['Food', 'Groceries', 'Transport', 'Entertainment', 'Bills']
        .filter((c) => dash.categories.includes(c));
      setSelectedCategories(
        defaults.length ? defaults : dash.categories.slice(0, 5)
      );
    }
  }, [dash.categories, selectedCategories.length]);

  const dailySeries = useMemo(
    () => [
      {
        key: 'daily',
        label: 'Daily spend',
        data: dash.daily.map((d) => ({
          x: d.date,
          y: Number(d.total),
        })),
      },
    ],
    [dash.daily]
  );

  const totalsPie = useMemo(
    () => expensesVsEarningsPieData(dash.expensesVsEarnings),
    [dash.expensesVsEarnings]
  );

  const toggleCategory = (cat) => {
    setSelectedCategories((prev) =>
      prev.includes(cat) ? prev.filter((c) => c !== cat) : [...prev, cat]
    );
  };

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <div>
          <h1>Expense Intelligence</h1>
          {/* <p className="subtitle">
            React dashboard mirroring your Grafana panels — data from PostgreSQL
            via ASP.NET Core
          </p> */}
        </div>
        <div className="header-actions">
          {dash.health && (
            <div className="health-badge">
              <span className="dot" />
              {dash.health.database} · {dash.health.transactionCount} rows
            </div>
          )}
          <button
            type="button"
            className="btn-header"
            onClick={() => setImportOpen(true)}
          >
            Import CSV
          </button>
          <button
            type="button"
            className="btn-header btn-header--primary"
            onClick={() => setAddOpen(true)}
          >
            Add transaction
          </button>
        </div>
      </header>

      <Modal
        title="Import bank CSV"
        subtitle="UK export format — same columns as your grafana-expenses csv/ files"
        open={importOpen}
        onClose={() => setImportOpen(false)}
      >
        <ImportPanel onImported={dash.reload} />
      </Modal>

      <EditCategoriesModal
        open={categoriesOpen}
        catalog={dash.catalog}
        onClose={() => setCategoriesOpen(false)}
        onSaved={async (result) => {
          await dash.reload();
          const next = result?.expense ?? dash.catalog?.expense ?? [];
          setSelectedCategories((prev) =>
            prev.filter((c) => next.includes(c))
          );
        }}
      />

      <Modal
        title="Add transaction"
        subtitle="Single entry — category optional (ML will suggest)"
        open={addOpen}
        onClose={() => setAddOpen(false)}
      >
        <AddTransactionPanel
          categories={dash.catalog}
          onAdded={dash.reload}
        />
      </Modal>

      {dash.error && <div className="error">{dash.error}</div>}

      <div className="filters card">
        <label>
          Month
          <select
            value={selectedMonth}
            onChange={(e) => setSelectedMonth(e.target.value)}
            disabled={!dash.months.length}
          >
            {dash.months.map((m) => (
              <option key={m.value} value={m.value}>
                {m.label}
              </option>
            ))}
          </select>
        </label>
        <div className="category-filter">
          <div className="category-filter-head">
            <span className="filter-label">Categories (time series)</span>
            <button
              type="button"
              className="btn-edit-categories"
              onClick={() => setCategoriesOpen(true)}
            >
              Edit categories
            </button>
          </div>
          <div className="chips">
            {dash.categories.map((cat) => (
              <button
                key={cat}
                type="button"
                className={`chip ${selectedCategories.includes(cat) ? 'chip--on' : ''}`}
                style={{ '--chip-color': colorForCategory(cat) }}
                onClick={() => toggleCategory(cat)}
              >
                {cat}
              </button>
            ))}
          </div>
        </div>
        {dash.loading && <span className="loading-pill">Loading…</span>}
      </div>

      <div className="grid grid--2">
        <Panel
          className="panel--pie"
          title="Expenditure by category (month)"
          subtitle="Type: piechart — filtered by Month"
        >
          <CategoryPieChart data={dash.spendByMonth} size="large" />
        </Panel>
        <Panel
          className="panel--pie"
          title="Total expenditure by category"
          subtitle="Type: piechart — all time"
        >
          <CategoryPieChart data={dash.spendAllTime} size="large" />
        </Panel>
      </div>

      <div className="grid grid--budget">
        <Panel
          title="50/30/20 actual (month)"
          subtitle="Type: piechart — Necessities / Wants / Savings"
        >
          <CategoryPieChart data={dash.budgetActual} size="compact" />
        </Panel>
        <Panel
          title="50/30/20 target from income"
          subtitle="Type: bargauge — 50% / 30% / 20% of earnings"
        >
          <HorizontalBudgetBars data={dash.budgetTarget} compact />
        </Panel>
        <Panel className="panel--gauge" title="Savings" subtitle="Type: gauge">
          <GaugeDisplay value={dash.savings} compact />
        </Panel>
      </div>

      <div className="grid grid--2">
        <Panel
          title="Monthly expenditure by selected categories"
          subtitle="Type: timeseries"
        >
          <TimeSeriesChart
            series={categorySeriesFromMonthly(dash.selectedCategorySeries)}
          />
        </Panel>
        <Panel title="Daily expenditures" subtitle="Type: timeseries">
          <TimeSeriesChart series={dailySeries} />
        </Panel>
      </div>

      <div className="grid grid--2">
        <Panel title="Monthly salary income" subtitle="Type: barchart">
          <StackedBarChart
            rows={salaryBarData(dash.salary)}
            categories={['Salary']}
            stacked={false}
          />
        </Panel>
        <Panel
          title="Monthly earnings vs expenditures"
          subtitle="Type: barchart"
        >
          <ComparisonBarChart data={dash.earningsVsExp} />
        </Panel>
      </div>

      <Panel
        title="Spending by category by month"
        subtitle="Type: barchart (crosstab amounts)"
        className="span-full"
      >
        <StackedBarChart
          rows={pivotToRows(dash.pivotAmount)}
          categories={dash.pivotAmount?.categories}
        />
      </Panel>

      <Panel
        title="Times spent per category per month"
        subtitle="Type: barchart (crosstab counts)"
        className="span-full"
      >
        <StackedBarChart
          rows={pivotToRows(dash.pivotCount)}
          categories={dash.pivotCount?.categories}
          valueFormat="count"
        />
      </Panel>

      <div className="grid grid--2">
        <Panel
          title="Total spending per category per month"
          subtitle="Type: table"
        >
          <DataTable
            columns={[
              { key: 'month', label: 'Month', format: (v) => formatMonth(v) },
              { key: 'category', label: 'Category' },
              currencyColumn('totalAmount', 'Amount'),
              { key: 'count', label: 'Count' },
            ]}
            rows={dash.categoryTable}
          />
        </Panel>
        <Panel
          title="Total expenses vs earnings"
          subtitle="Type: piechart"
        >
          <CategoryPieChart data={totalsPie} />
          {dash.expensesVsEarnings && (
            <div className="totals-row">
              <span>Expenses: {formatCurrency(dash.expensesVsEarnings.totalExpenses)}</span>
              <span>Earnings: {formatCurrency(dash.expensesVsEarnings.totalEarnings)}</span>
            </div>
          )}
        </Panel>
      </div>

      <Panel title="Top merchants" subtitle="From analytics API" className="span-full">
        <DataTable
          columns={[
            { key: 'description', label: 'Description' },
            currencyColumn('totalAmount', 'Total'),
            { key: 'transactionCount', label: 'Times' },
          ]}
          rows={dash.merchants}
        />
      </Panel>

      <footer className="footer">
        Panel mapping documented in <code>docs/GRAFANA_PANELS.md</code> ·{' '}
        <a href="http://localhost:5000/swagger" target="_blank" rel="noreferrer">
          API Swagger
        </a>
      </footer>
    </div>
  );
}
