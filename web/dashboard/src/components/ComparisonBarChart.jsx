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
import { formatCurrency, formatMonth } from '../utils/format';

export default function ComparisonBarChart({ data }) {
  if (!data?.length) return <p className="empty">No data</p>;

  const chartData = data.map((d) => ({
    month: formatMonth(d.month),
    Earnings: Number(d.earnings),
    Expenditures: Number(d.expenditures),
  }));

  return (
    <ResponsiveContainer width="100%" height={300}>
      <BarChart data={chartData}>
        <CartesianGrid strokeDasharray="3 3" stroke="#2a3548" />
        <XAxis dataKey="month" stroke="#8b9cb3" angle={-20} textAnchor="end" height={60} />
        <YAxis stroke="#8b9cb3" tickFormatter={(v) => `£${v}`} />
        <Tooltip formatter={(v) => formatCurrency(v)} />
        <Legend />
        <Bar dataKey="Earnings" fill="#34d399" />
        <Bar dataKey="Expenditures" fill="#f87171" />
      </BarChart>
    </ResponsiveContainer>
  );
}
