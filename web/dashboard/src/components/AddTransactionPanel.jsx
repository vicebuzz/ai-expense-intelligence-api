import { useState } from 'react';
import { api } from '../api';

export default function AddTransactionPanel({ categories, onAdded }) {
  const today = new Date().toISOString().slice(0, 10);
  const defaultMonth = today.slice(0, 7);

  const [date, setDate] = useState(today);
  const [month, setMonth] = useState(defaultMonth);
  const [description, setDescription] = useState('');
  const [amount, setAmount] = useState('');
  const [isExpense, setIsExpense] = useState(true);
  const [category, setCategory] = useState('');
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState(null);
  const [error, setError] = useState(null);

  const expenseCats = categories?.expense ?? [];
  const incomeCats = categories?.income ?? [];
  const catList = isExpense ? expenseCats : incomeCats;

  const onDateChange = (value) => {
    setDate(value);
    if (value) setMonth(value.slice(0, 7));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setMessage(null);

    try {
      const created = await api.createTransaction({
        date,
        month,
        description,
        amount: parseFloat(amount),
        isExpense,
        category: category || null,
      });
      setMessage(
        `Added: ${created.description} → ${created.category} (${created.month})`
      );
      setDescription('');
      setAmount('');
      setCategory('');
      onAdded?.();
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <form className="add-form" onSubmit={handleSubmit}>
      <div className="form-row">
        <label>
          Date
          <input
            type="date"
            value={date}
            onChange={(e) => onDateChange(e.target.value)}
            required
          />
        </label>
        <label>
          Month (YYYY-MM)
          <input
            type="month"
            value={month}
            onChange={(e) => setMonth(e.target.value)}
            required
          />
        </label>
      </div>

      <label>
        Description
        <input
          type="text"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="e.g. SAINSBURY'S S/MKT"
          required
        />
      </label>

      <div className="form-row">
        <label>
          Amount (£)
          <input
            type="number"
            min="0"
            step="0.01"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
          />
        </label>
        <label>
          Type
          <select
            value={isExpense ? 'expense' : 'income'}
            onChange={(e) => {
              setIsExpense(e.target.value === 'expense');
              setCategory('');
            }}
          >
            <option value="expense">Expense (debit)</option>
            <option value="income">Income (credit)</option>
          </select>
        </label>
      </div>

      <label>
        Category (optional)
        <select value={category} onChange={(e) => setCategory(e.target.value)}>
          <option value="">Auto (ML)</option>
          {catList.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </select>
      </label>

      <button type="submit" className="btn-primary" disabled={loading}>
        {loading ? 'Saving…' : 'Add to database'}
      </button>

      {error && <div className="error">{error}</div>}
      {message && <p className="success-msg">{message}</p>}
    </form>
  );
}
