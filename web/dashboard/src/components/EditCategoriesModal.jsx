import { useEffect, useState } from 'react';
import { api } from '../api';
import Modal from './Modal';

function CategorySection({ title, hint, items, onChange, newValue, setNewValue }) {
  const add = () => {
    const name = newValue.trim();
    if (!name) return;
    if (items.some((c) => c.toLowerCase() === name.toLowerCase())) {
      setNewValue('');
      return;
    }
    onChange([...items, name]);
    setNewValue('');
  };

  const remove = (name) => onChange(items.filter((c) => c !== name));

  return (
    <section className="category-editor-section">
      <h3>{title}</h3>
      {hint && <p className="category-editor-hint">{hint}</p>}
      <ul className="category-editor-list">
        {items.map((name) => (
          <li key={name}>
            <span>{name}</span>
            <button
              type="button"
              className="category-editor-remove"
              onClick={() => remove(name)}
              aria-label={`Remove ${name}`}
            >
              ×
            </button>
          </li>
        ))}
      </ul>
      <div className="category-editor-add">
        <input
          type="text"
          value={newValue}
          placeholder="New category name"
          onChange={(e) => setNewValue(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault();
              add();
            }
          }}
        />
        <button type="button" className="btn-header" onClick={add}>
          Add
        </button>
      </div>
    </section>
  );
}

export default function EditCategoriesModal({ open, catalog, onClose, onSaved }) {
  const [expense, setExpense] = useState([]);
  const [income, setIncome] = useState([]);
  const [newExpense, setNewExpense] = useState('');
  const [newIncome, setNewIncome] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [warnings, setWarnings] = useState([]);

  useEffect(() => {
    if (!open) return;
    setExpense([...(catalog?.expense ?? [])]);
    setIncome([...(catalog?.income ?? [])]);
    setError(null);
    setWarnings([]);
    setNewExpense('');
    setNewIncome('');
  }, [open, catalog]);

  const handleSave = async () => {
    setLoading(true);
    setError(null);
    setWarnings([]);

    try {
      const result = await api.updateCategories({ expense, income });
      const notes = [...(result.warnings ?? [])];
      if (result.mlRetrainedSamples > 0) {
        notes.push(`ML retrained on ${result.mlRetrainedSamples} labelled examples from your database.`);
      }
      setWarnings(notes);
      await onSaved?.(result);
      if (!result.warnings?.length) {
        onClose();
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      title="Edit categories"
      subtitle="Expense and income labels used for imports, manual entry, and filters. Saved to the database."
      open={open}
      onClose={onClose}
    >
      <div className="category-editor">
        <CategorySection
          title="Expense categories"
          hint="Used for debits and spending charts."
          items={expense}
          onChange={setExpense}
          newValue={newExpense}
          setNewValue={setNewExpense}
        />
        <CategorySection
          title="Income categories"
          hint="Used for credits and salary panels."
          items={income}
          onChange={setIncome}
          newValue={newIncome}
          setNewValue={setNewIncome}
        />

        {error && <div className="error">{error}</div>}

        {warnings.length > 0 && (
          <div className="import-result">
            <p><strong>Saved with notes:</strong></p>
            <ul>
              {warnings.map((w) => (
                <li key={w}>{w}</li>
              ))}
            </ul>
          </div>
        )}

        <div className="category-editor-actions">
          <button type="button" className="btn-header" onClick={onClose} disabled={loading}>
            Cancel
          </button>
          <button
            type="button"
            className="btn-primary"
            onClick={handleSave}
            disabled={loading || (expense.length === 0 && income.length === 0)}
          >
            {loading ? 'Saving…' : 'Save to database'}
          </button>
        </div>
      </div>
    </Modal>
  );
}
