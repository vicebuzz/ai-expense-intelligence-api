import { formatCurrency } from '../utils/format';

export default function DataTable({ columns, rows, maxHeight = 320 }) {
  if (!rows?.length) return <p className="empty">No data</p>;

  return (
    <div className="table-wrap" style={{ maxHeight }}>
      <table>
        <thead>
          <tr>
            {columns.map((col) => (
              <th key={col.key}>{col.label}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, i) => (
            <tr key={i}>
              {columns.map((col) => (
                <td key={col.key}>
                  {col.format ? col.format(row[col.key], row) : row[col.key]}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export const currencyColumn = (key, label) => ({
  key,
  label,
  format: (v) => formatCurrency(v),
});
