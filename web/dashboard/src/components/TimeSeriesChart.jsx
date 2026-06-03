import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { chartUi, colorForCategory } from '../theme';
import { formatCurrency } from '../utils/format';

/** @param {{ series: { key: string, label: string, data: { x: string, y: number }[] }[] }} props */
export default function TimeSeriesChart({ series }) {
  if (!series?.length) return <p className="empty">No data</p>;

  const keys = new Set();
  series.forEach((s) => s.data.forEach((p) => keys.add(p.x)));
  const merged = [...keys]
    .sort()
    .map((x) => {
      const row = { x };
      series.forEach((s) => {
        const point = s.data.find((p) => p.x === x);
        row[s.key] = point ? point.y : 0;
      });
      return row;
    });

  return (
    <ResponsiveContainer width="100%" height={300}>
      <LineChart data={merged}>
        <CartesianGrid strokeDasharray="3 3" stroke={chartUi.grid} />
        <XAxis
          dataKey="x"
          stroke={chartUi.axis}
          tick={{ fill: chartUi.tick }}
        />
        <YAxis
          stroke={chartUi.axis}
          tick={{ fill: chartUi.tick }}
          tickFormatter={(v) => `£${v}`}
        />
        <Tooltip formatter={(v) => formatCurrency(v)} />
        <Legend />
        {series.map((s, i) => (
          <Line
            key={s.key}
            type="monotone"
            dataKey={s.key}
            name={s.label}
            stroke={colorForCategory(s.label, i)}
            dot={false}
            strokeWidth={2}
          />
        ))}
      </LineChart>
    </ResponsiveContainer>
  );
}
