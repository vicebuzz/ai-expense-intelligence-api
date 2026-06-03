const API_BASE = import.meta.env.VITE_API_URL || '';

async function fetchJson(path) {
  const res = await fetch(`${API_BASE}${path}`);
  if (!res.ok) {
    throw new Error(`API ${path} failed: ${res.status}`);
  }
  return res.json();
}

export const api = {
  health: () => fetchJson('/api/health'),
  spendingByCategory: (from, to) => {
    const params = new URLSearchParams();
    if (from) params.set('from', from);
    if (to) params.set('to', to);
    const q = params.toString();
    return fetchJson(`/api/analytics/spending-by-category${q ? `?${q}` : ''}`);
  },
  averageMonthly: () => fetchJson('/api/analytics/average-monthly-by-category'),
  topMerchants: (limit = 10) =>
    fetchJson(`/api/analytics/top-merchants?limit=${limit}`),
  transactions: () => fetchJson('/api/transactions?isExpense=true'),
};
