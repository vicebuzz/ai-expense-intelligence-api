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
import { CHART_COLORS, formatCurrency, formatMonth } from '../utils/format';

export default function StackedBarChart({ rows, categories, stacked = true }) {
  if (!rows?.length) return <p className="empty">No data</p>;

  const chartData = rows.map((r) => ({
    month: formatMonth(r.month),
    ...r.values,
  }));

  const keys =
    categories?.length > 0
      ? categories
      : Object.keys(rows[0]?.values || {});

  return (
    <ResponsiveContainer width="100%" height={320}>
      <BarChart data={chartData}>
        <CartesianGrid strokeDasharray="3 3" stroke="#2a3548" />
        <XAxis dataKey="month" stroke="#8b9cb3" angle={-20} textAnchor="end" height={60} />
        <YAxis stroke="#8b9cb3" tickFormatter={(v) => `£${v}`} />
        <Tooltip formatter={(v) => formatCurrency(v)} />
        <Legend />
        {keys.slice(0, 12).map((key, i) => (
          <Bar
            key={key}
            dataKey={key}
            stackId={stacked ? 'a' : undefined}
            fill={CHART_COLORS[i % CHART_COLORS.length]}
          />
        ))}
      </BarChart>
    </ResponsiveContainer>
  );
}
