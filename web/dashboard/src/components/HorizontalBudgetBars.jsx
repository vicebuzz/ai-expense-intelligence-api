import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { formatCurrency } from '../utils/format';

export default function HorizontalBudgetBars({ data, color = '#38bdf8' }) {
  if (!data?.length) return <p className="empty">No data</p>;

  const chartData = data.map((d) => ({
    group: d.group,
    total: Number(d.total),
  }));

  return (
    <ResponsiveContainer width="100%" height={220}>
      <BarChart data={chartData} layout="vertical" margin={{ left: 24 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="#2a3548" />
        <XAxis type="number" stroke="#8b9cb3" tickFormatter={(v) => `£${v}`} />
        <YAxis type="category" dataKey="group" width={100} stroke="#8b9cb3" />
        <Tooltip formatter={(v) => formatCurrency(v)} />
        <Bar dataKey="total" fill={color} radius={[0, 4, 4, 0]} />
      </BarChart>
    </ResponsiveContainer>
  );
}
