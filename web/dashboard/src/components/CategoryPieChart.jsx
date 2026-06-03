import {
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  Legend,
} from 'recharts';
import { CHART_COLORS, formatCurrency } from '../utils/format';

export default function CategoryPieChart({ data, emptyMessage = 'No data' }) {
  if (!data?.length) {
    return <p className="empty">{emptyMessage}</p>;
  }

  const chartData = data.map((d) => ({
    name: d.category ?? d.group,
    value: Number(d.total),
  }));

  return (
    <ResponsiveContainer width="100%" height={280}>
      <PieChart>
        <Pie
          data={chartData}
          dataKey="value"
          nameKey="name"
          cx="40%"
          cy="50%"
          outerRadius={90}
          label={({ name, percent }) =>
            `${name} (${(percent * 100).toFixed(0)}%)`
          }
        >
          {chartData.map((_, i) => (
            <Cell key={i} fill={CHART_COLORS[i % CHART_COLORS.length]} />
          ))}
        </Pie>
        <Tooltip formatter={(v) => formatCurrency(v)} />
        <Legend layout="vertical" align="right" verticalAlign="middle" />
      </PieChart>
    </ResponsiveContainer>
  );
}
