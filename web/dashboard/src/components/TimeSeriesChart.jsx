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
import { CHART_COLORS, formatCurrency } from '../utils/format';

/** @param {{ series: { key: string, label: string, data: { x: string, y: number }[] }[] }} props */
export default function TimeSeriesChart({ series, xLabel = 'Period' }) {
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
        <CartesianGrid strokeDasharray="3 3" stroke="#2a3548" />
        <XAxis dataKey="x" stroke="#8b9cb3" />
        <YAxis stroke="#8b9cb3" tickFormatter={(v) => `£${v}`} />
        <Tooltip formatter={(v) => formatCurrency(v)} />
        <Legend />
        {series.map((s, i) => (
          <Line
            key={s.key}
            type="monotone"
            dataKey={s.key}
            name={s.label}
            stroke={CHART_COLORS[i % CHART_COLORS.length]}
            dot={false}
            strokeWidth={2}
          />
        ))}
      </LineChart>
    </ResponsiveContainer>
  );
}
