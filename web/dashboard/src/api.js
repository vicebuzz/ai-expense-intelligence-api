const API_BASE = import.meta.env.VITE_API_URL || '';

async function fetchJson(path) {
  const res = await fetch(`${API_BASE}${path}`);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`API ${path} failed (${res.status}): ${text}`);
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
};
