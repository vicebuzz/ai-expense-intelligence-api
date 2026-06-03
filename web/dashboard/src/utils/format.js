import { chartPalette, colorForCategory } from '../theme';

export function formatCurrency(value) {
  const n = Number(value) || 0;
  return `£${n.toLocaleString('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export function formatCount(value) {
  const n = Math.round(Number(value) || 0);
  return String(n);
}

export function formatTimes(value) {
  const n = Math.round(Number(value) || 0);
  return n === 1 ? '1 time' : `${n} times`;
}

export function formatMonth(yyyyMm) {
  if (!yyyyMm) return '';
  const [y, m] = yyyyMm.split('-');
  const d = new Date(Number(y), Number(m) - 1, 1);
  return d.toLocaleDateString('en-GB', { month: 'long', year: 'numeric' });
}

export const CHART_COLORS = chartPalette;

export { colorForCategory };
