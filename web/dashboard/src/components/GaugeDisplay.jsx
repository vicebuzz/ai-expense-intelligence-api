import { formatCurrency } from '../utils/format';

export default function GaugeDisplay({ value, label = 'Savings', compact = false }) {
  if (value == null) {
    return (
      <div className={`gauge gauge--empty ${compact ? 'gauge--compact' : ''}`}>
        <p className="empty">No savings table configured</p>
        <small>Requires legacy <code>savings</code> table in PostgreSQL</small>
      </div>
    );
  }

  return (
    <div className={`gauge ${compact ? 'gauge--compact' : ''}`}>
      <div className="gauge-ring">
        <span className="gauge-value">{formatCurrency(value)}</span>
      </div>
      <span className="gauge-label">{label}</span>
    </div>
  );
}
