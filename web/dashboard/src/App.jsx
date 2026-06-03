import { useEffect, useMemo, useState } from 'react';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { api } from './api';

export default function App() {
  const [spending, setSpending] = useState([]);
  const [averages, setAverages] = useState([]);
  const [merchants, setMerchants] = useState([]);
  const [error, setError] = useState(null);

  useEffect(() => {
    Promise.all([
      api.spendingByCategory(),
      api.averageMonthly(),
      api.topMerchants(8),
    ])
      .then(([s, a, m]) => {
        setSpending(s);
        setAverages(a);
        setMerchants(m);
      })
      .catch((e) => setError(e.message));
  }, []);

  const chartData = useMemo(() => {
    const byMonth = {};
    for (const row of spending) {
      if (!byMonth[row.month]) byMonth[row.month] = { month: row.month };
      byMonth[row.month][row.category] = Number(row.total);
    }
    return Object.values(byMonth).sort((a, b) =>
      a.month.localeCompare(b.month)
    );
  }, [spending]);

  const totalSpend = useMemo(
    () => spending.reduce((sum, r) => sum + Number(r.total), 0),
    [spending]
  );

  const topCategory = useMemo(() => {
    if (!averages.length) return '—';
    return [...averages].sort(
      (a, b) => b.averageAmount - a.averageAmount
    )[0].category;
  }, [averages]);

  return (
    <>
      <h1>Expense Intelligence</h1>
      <p className="subtitle">
        Spending analytics powered by ASP.NET Core and PostgreSQL
      </p>

      {error && <div className="error">{error}</div>}

      <div className="stat-row">
        <div className="stat">
          <label>Total spend (period)</label>
          <strong>£{totalSpend.toFixed(2)}</strong>
        </div>
        <div className="stat">
          <label>Highest avg category</label>
          <strong>{topCategory}</strong>
        </div>
        <div className="stat">
          <label>Data points</label>
          <strong>{spending.length}</strong>
        </div>
      </div>

      <div className="grid">
        <div className="card" style={{ gridColumn: '1 / -1' }}>
          <h2>Monthly spend by category</h2>
          <ResponsiveContainer width="100%" height={320}>
            <BarChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#2a3548" />
              <XAxis dataKey="month" stroke="#8b9cb3" />
              <YAxis stroke="#8b9cb3" />
              <Tooltip
                contentStyle={{
                  background: '#1a2332',
                  border: '1px solid #2a3548',
                }}
              />
              <Legend />
              <Bar dataKey="Groceries" stackId="a" fill="#38bdf8" />
              <Bar dataKey="Food" stackId="a" fill="#818cf8" />
              <Bar dataKey="Transport" stackId="a" fill="#34d399" />
              <Bar dataKey="Bills" stackId="a" fill="#fbbf24" />
              <Bar dataKey="Rent" stackId="a" fill="#f87171" />
              <Bar dataKey="Other" stackId="a" fill="#94a3b8" />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="card">
          <h2>Average monthly by category</h2>
          <ul style={{ margin: 0, paddingLeft: '1.2rem' }}>
            {averages.map((row) => (
              <li key={row.category} style={{ marginBottom: '0.35rem' }}>
                {row.category}: £{Number(row.averageAmount).toFixed(2)}
              </li>
            ))}
          </ul>
        </div>

        <div className="card">
          <h2>Top merchants</h2>
          <ul style={{ margin: 0, paddingLeft: '1.2rem' }}>
            {merchants.map((m) => (
              <li key={m.description} style={{ marginBottom: '0.35rem' }}>
                {m.description} — £{Number(m.totalAmount).toFixed(2)} (
                {m.transactionCount}x)
              </li>
            ))}
          </ul>
        </div>
      </div>
    </>
  );
}
