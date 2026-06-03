import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { categoryColors, chartUi } from '../theme';
import { formatCurrency } from '../utils/format';

export default function HorizontalBudgetBars({
  data,
  color = categoryColors.Savings,
  compact = false,
}) {
  if (!data?.length) return <p className="empty">No data</p>;

  const chartData = data.map((d) => ({
    group: d.group,
    total: Number(d.total),
  }));

  return (
    <ResponsiveContainer width="100%" height={compact ? 150 : 220}>
      <BarChart
        data={chartData}
        layout="vertical"
        margin={{ left: compact ? 8 : 24, top: 4, bottom: 4, right: 8 }}
      >
        <CartesianGrid strokeDasharray="3 3" stroke={chartUi.grid} />
        <XAxis
          type="number"
          stroke={chartUi.axis}
          tick={{ fill: chartUi.tick }}
          tickFormatter={(v) => `£${v}`}
        />
        <YAxis
          type="category"
          dataKey="group"
          width={compact ? 88 : 100}
          tick={{ fontSize: compact ? 11 : 12 }}
          stroke={chartUi.axis}
          tick={{ fill: chartUi.tick }}
        />
        <Tooltip formatter={(v) => formatCurrency(v)} />
        <Bar dataKey="total" fill={color} radius={[0, 4, 4, 0]} />
      </BarChart>
    </ResponsiveContainer>
  );
}
