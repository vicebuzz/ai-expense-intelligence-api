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
import { categoryColors, chartUi } from '../theme';
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
        <CartesianGrid strokeDasharray="3 3" stroke={chartUi.grid} />
        <XAxis
          dataKey="month"
          stroke={chartUi.axis}
          tick={{ fill: chartUi.tick }}
          angle={-20}
          textAnchor="end"
          height={60}
        />
        <YAxis
          stroke={chartUi.axis}
          tick={{ fill: chartUi.tick }}
          tickFormatter={(v) => `£${v}`}
        />
        <Tooltip formatter={(v) => formatCurrency(v)} />
        <Legend />
        <Bar dataKey="Earnings" fill={categoryColors.Earnings} />
        <Bar dataKey="Expenditures" fill={categoryColors.Expenditures} />
      </BarChart>
    </ResponsiveContainer>
  );
}
