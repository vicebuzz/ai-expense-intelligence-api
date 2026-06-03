import { useState } from 'react';
import { api } from '../api';

export default function ImportPanel({ onImported }) {
  const [file, setFile] = useState(null);
  const [autoCategorize, setAutoCategorize] = useState(true);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);

  const handleUpload = async (e) => {
    e.preventDefault();
    if (!file) {
      setError('Choose a CSV file first.');
      return;
    }

    setLoading(true);
    setError(null);
    setResult(null);

    try {
      const data = await api.uploadCsv(file, autoCategorize);
      setResult(data);
      onImported?.();
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="import-panel">
      <p className="import-hint">
        Expected columns: <code>Transaction Date</code>, <code>Transaction Type</code>,{' '}
        <code>Sort Code</code>, <code>Account Number</code>,{' '}
        <code>Transaction Description</code>, <code>Debit Amount</code>,{' '}
        <code>Credit Amount</code>, <code>Balance</code>, <code>Category</code> (optional).
        Each row gets <strong>month</strong> set as YYYY-MM from the transaction date.
        Rows labelled <code>Other</code> are skipped.
      </p>

      <a className="btn-link" href="/api/import/template" download>
        Download empty template
      </a>

      <form onSubmit={handleUpload} className="import-form">
        <label className="file-label">
          <span>CSV file</span>
          <input
            type="file"
            accept=".csv,text/csv"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          />
        </label>

        <label className="checkbox-label">
          <input
            type="checkbox"
            checked={autoCategorize}
            onChange={(e) => setAutoCategorize(e.target.checked)}
          />
          Auto-categorise empty Category cells (transformer ML)
        </label>

        <button type="submit" className="btn-primary" disabled={loading}>
          {loading ? 'Importing…' : 'Upload & import'}
        </button>
      </form>

      {error && <div className="error">{error}</div>}

      {result && (
        <div className="import-result">
          <p>
            <strong>{result.imported}</strong> imported ·{' '}
            <strong>{result.skipped}</strong> skipped
          </p>
          <p>
            ML categorised: {result.categorizedByMl} · From CSV column:{' '}
            {result.usedCsvCategory}
          </p>
          {result.errors?.length > 0 && (
            <details>
              <summary>{result.errors.length} warnings</summary>
              <ul>
                {result.errors.slice(0, 10).map((msg, i) => (
                  <li key={i}>{msg}</li>
                ))}
              </ul>
            </details>
          )}
        </div>
      )}
    </div>
  );
}
