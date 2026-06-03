import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts';
import { colorForCategory } from '../theme';
import { formatCurrency } from '../utils/format';

function PieLegend({ items, twoColumn }) {
  const total = items.reduce((sum, d) => sum + d.value, 0) || 1;

  return (
    <ul
      className={`pie-legend ${twoColumn ? 'pie-legend--two-col' : 'pie-legend--one-col'}`}
      aria-label="Chart legend"
    >
      {items.map((entry, i) => {
        const pct = ((entry.value / total) * 100).toFixed(0);
        const color = colorForCategory(entry.name, i);
        return (
          <li key={entry.name} className="pie-legend__item">
            <span
              className="pie-legend__swatch"
              style={{ backgroundColor: color }}
              aria-hidden
            />
            <span className="pie-legend__text">
              <span className="pie-legend__name" title={entry.name}>
                {entry.name}
              </span>
              <span className="pie-legend__meta">
                {pct}% · {formatCurrency(entry.value)}
              </span>
            </span>
          </li>
        );
      })}
    </ul>
  );
}

export default function CategoryPieChart({
  data,
  emptyMessage = 'No data',
  size = 'default',
}) {
  if (!data?.length) {
    return <p className="empty">{emptyMessage}</p>;
  }

  const isLarge = size === 'large';
  const isCompact = size === 'compact';
  const chartData = data
    .map((d) => ({
      name: d.category ?? d.group,
      value: Number(d.total),
    }))
    .filter((d) => d.value > 0)
    .sort((a, b) => b.value - a.value);

  if (!chartData.length) {
    return <p className="empty">{emptyMessage}</p>;
  }

  const height = isLarge ? 380 : isCompact ? 168 : 260;
  const outerRadius = isLarge ? '78%' : isCompact ? '72%' : 88;

  const layoutClass = isLarge
    ? 'pie-chart--large'
    : isCompact
      ? 'pie-chart--compact'
      : '';

  return (
    <div className={`pie-chart ${layoutClass}`}>
      <div className="pie-chart__canvas">
        <ResponsiveContainer width="100%" height={height}>
          <PieChart>
            <Pie
              data={chartData}
              dataKey="value"
              nameKey="name"
              cx="50%"
              cy="50%"
              outerRadius={outerRadius}
              label={false}
            >
              {chartData.map((entry, i) => (
                <Cell
                  key={entry.name}
                  fill={colorForCategory(entry.name, i)}
                />
              ))}
            </Pie>
            <Tooltip formatter={(v) => formatCurrency(v)} />
          </PieChart>
        </ResponsiveContainer>
      </div>
      <PieLegend items={chartData} twoColumn={isLarge && !isCompact} />
    </div>
  );
}
