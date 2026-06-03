/** Grafana-inspired light finance dashboard palette */

export const colors = {
  bg: '#F7F9FC',
  surface: '#FFFFFF',
  border: '#E5EAF2',
  borderSoft: '#EEF2F7',

  text: '#0F172A',
  textSecondary: '#475569',
  textMuted: '#94A3B8',

  brand: '#1E4F8F',
  interactive: '#2B6CB0',
  accent: '#4A90E2',

  success: '#10B981',
  warning: '#F59E0B',
  danger: '#EF4444',
  info: '#3B82F6',
};

/** Outflow — warm/discretionary vs blues/purples for recurring; gray = other */
export const outflowCategoryColors = {
  Groceries: '#84CC16',
  Food: '#F59E0B',
  Bills: '#6366F1',
  Transport: '#3B82F6',
  Entertainment: '#8B5CF6',
  Education: '#0EA5E9',
  Presents: '#EC4899',
  Other: '#94A3B8',
  Shopping: '#F43F5E',
  'Health&Hygiene': '#10B981',
  'Travel&Tourism': '#14B8A6',
  Savings: '#22C55E',
  'Sports&Activities': '#F97316',
  Tobacco: '#78716C',
  Rent: '#1E40AF',
  Subscriptions: '#A855F7',
};

/** Inflow — cooler greens/blues */
export const inflowCategoryColors = {
  Deposit: '#38BDF8',
  Wages: '#16A34A',
  University: '#0284C7',
  'Savings Account': '#059669',
  Other: '#64748B',
  'Help Funds': '#22C55E',
  Refund: '#4ADE80',
  Salary: '#15803D',
};

const { Other: _outOther, ...outflowWithoutOther } = outflowCategoryColors;
const { Other: _inOther, ...inflowWithoutOther } = inflowCategoryColors;

/** All categories + aggregates (Other resolved via colorForCategory) */
export const categoryColors = {
  ...outflowWithoutOther,
  ...inflowWithoutOther,
  Health: outflowCategoryColors['Health&Hygiene'],
  Travel: outflowCategoryColors['Travel&Tourism'],
  Expenditures: '#F43F5E',
  Earnings: '#15803D',
  Necessities: '#6366F1',
  Wants: '#F59E0B',
};

/** Distinct fallback order (outflow-first, no duplicates) */
export const chartPalette = [
  '#84CC16',
  '#F59E0B',
  '#6366F1',
  '#3B82F6',
  '#8B5CF6',
  '#0EA5E9',
  '#EC4899',
  '#F43F5E',
  '#10B981',
  '#14B8A6',
  '#F97316',
  '#1E40AF',
  '#A855F7',
  '#38BDF8',
  '#16A34A',
  '#15803D',
];

export const chartUi = {
  grid: colors.borderSoft,
  axis: colors.textMuted,
  tick: colors.textSecondary,
};

/**
 * @param {string} name
 * @param {number} [fallbackIndex]
 * @param {boolean | null} [isExpense] — disambiguates inflow vs outflow "Other"
 */
export function colorForCategory(name, fallbackIndex = 0, isExpense = null) {
  if (!name) return chartPalette[fallbackIndex % chartPalette.length];

  if (name === 'Other') {
    if (isExpense === false) return inflowCategoryColors.Other;
    return outflowCategoryColors.Other;
  }

  const exact = categoryColors[name];
  if (exact) return exact;

  const normalized = Object.keys(categoryColors).find(
    (k) => k.toLowerCase() === String(name).toLowerCase()
  );
  if (normalized) return categoryColors[normalized];

  let hash = 0;
  const s = String(name);
  for (let i = 0; i < s.length; i += 1) {
    hash = (hash + s.charCodeAt(i) * (i + 1)) % chartPalette.length;
  }
  return chartPalette[hash];
}
