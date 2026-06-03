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
import { chartUi, colorForCategory } from '../theme';
import {
  formatCount,
  formatCurrency,
  formatMonth,
  formatTimes,
} from '../utils/format';

export default function StackedBarChart({
  rows,
  categories,
  stacked = true,
  valueFormat = 'currency',
}) {
  const isCount = valueFormat === 'count';
  const formatValue = isCount ? formatTimes : formatCurrency;
  const formatAxis = isCount ? formatCount : (v) => `£${v}`;
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
          tickFormatter={formatAxis}
          allowDecimals={!isCount}
        />
        <Tooltip formatter={(v) => formatValue(v)} />
        <Legend />
        {keys.slice(0, 12).map((key, i) => (
          <Bar
            key={key}
            dataKey={key}
            stackId={stacked ? 'a' : undefined}
            fill={colorForCategory(key, i)}
          />
        ))}
      </BarChart>
    </ResponsiveContainer>
  );
}
