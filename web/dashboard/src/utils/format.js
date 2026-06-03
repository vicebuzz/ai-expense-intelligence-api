export function formatCurrency(value) {
  const n = Number(value) || 0;
  return `£${n.toLocaleString('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export function formatMonth(yyyyMm) {
  if (!yyyyMm) return '';
  const [y, m] = yyyyMm.split('-');
  const d = new Date(Number(y), Number(m) - 1, 1);
  return d.toLocaleDateString('en-GB', { month: 'long', year: 'numeric' });
}

export const CHART_COLORS = [
  '#38bdf8',
  '#818cf8',
  '#34d399',
  '#fbbf24',
  '#f87171',
  '#fb923c',
  '#a78bfa',
  '#2dd4bf',
  '#f472b6',
  '#94a3b8',
  '#4ade80',
  '#60a5fa',
];
