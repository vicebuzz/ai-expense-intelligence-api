const API_BASE = import.meta.env.VITE_API_URL || '';

async function fetchJson(path, options = {}) {
  const res = await fetch(`${API_BASE}${path}`, options);
  if (!res.ok) {
    const text = await res.text();
    let message = text;
    try {
      const json = JSON.parse(text);
      message = json.error ?? json.message ?? text;
    } catch {
      /* plain text */
    }
    throw new Error(message || `API ${path} failed (${res.status})`);
  }
  return res.json();
}

export const api = {
  health: () => fetchJson('/api/health'),
  months: () => fetchJson('/api/analytics/months'),
  expenseCategories: () => fetchJson('/api/analytics/expense-categories'),
  spendByCategory: (month) =>
    fetchJson(`/api/analytics/spend-by-category${month ? `?month=${month}` : ''}`),
  budget502030Actual: (month) =>
    fetchJson(`/api/analytics/budget-502030/actual?month=${month}`),
  budget502030Target: (month) =>
    fetchJson(`/api/analytics/budget-502030/target?month=${month}`),
  latestSavings: () => fetchJson('/api/analytics/savings/latest'),
  dailyExpenditures: () => fetchJson('/api/analytics/daily-expenditures'),
  monthlySalary: () => fetchJson('/api/analytics/monthly-salary'),
  earningsVsExpenditures: () => fetchJson('/api/analytics/earnings-vs-expenditures'),
  categoryPivot: (metric = 'amount') =>
    fetchJson(`/api/analytics/category-pivot?metric=${metric}`),
  categoryMonthTable: () => fetchJson('/api/analytics/category-month-table'),
  expensesVsEarnings: () => fetchJson('/api/analytics/expenses-vs-earnings'),
  monthlyByCategories: (categories) =>
    fetchJson(
      `/api/analytics/monthly-by-categories?categories=${encodeURIComponent(categories.join(','))}`
    ),
  topMerchants: (limit = 15) =>
    fetchJson(`/api/analytics/top-merchants?limit=${limit}`),
  categories: () => fetchJson('/api/categories'),
  updateCategories: (body) =>
    fetchJson('/api/categories', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        expense: body.expense,
        income: body.income,
      }),
    }),
  uploadCsv: async (file, autoCategorize = true) => {
    const form = new FormData();
    form.append('file', file);
    const res = await fetch(
      `${API_BASE}/api/import/csv?autoCategorize=${autoCategorize}`,
      { method: 'POST', body: form }
    );
    if (!res.ok) {
      const text = await res.text();
      throw new Error(`Upload failed (${res.status}): ${text}`);
    }
    return res.json();
  },
  createTransaction: async (body) => {
    const res = await fetch(`${API_BASE}/api/transactions`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        date: body.date,
        month: body.month,
        description: body.description,
        amount: body.amount,
        isExpense: body.isExpense,
        category: body.category,
      }),
    });
    if (!res.ok) {
      const text = await res.text();
      throw new Error(`Create failed (${res.status}): ${text}`);
    }
    return res.json();
  },
  syncMlTraining: () =>
    fetch(`${API_BASE}/api/training/sync-ml`, { method: 'POST' }).then((r) => {
      if (!r.ok) throw new Error('ML sync failed');
      return r.json();
    }),
};
