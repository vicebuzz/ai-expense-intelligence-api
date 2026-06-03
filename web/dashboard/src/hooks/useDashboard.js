import { useCallback, useEffect, useState } from 'react';
import { api } from '../api';

export function useDashboard(selectedMonth, selectedCategories) {
  const [health, setHealth] = useState(null);
  const [months, setMonths] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const [spendByMonth, setSpendByMonth] = useState([]);
  const [spendAllTime, setSpendAllTime] = useState([]);
  const [budgetActual, setBudgetActual] = useState([]);
  const [budgetTarget, setBudgetTarget] = useState([]);
  const [savings, setSavings] = useState(null);
  const [daily, setDaily] = useState([]);
  const [salary, setSalary] = useState([]);
  const [earningsVsExp, setEarningsVsExp] = useState([]);
  const [pivotAmount, setPivotAmount] = useState(null);
  const [pivotCount, setPivotCount] = useState(null);
  const [categoryTable, setCategoryTable] = useState([]);
  const [expensesVsEarnings, setExpensesVsEarnings] = useState(null);
  const [selectedCategorySeries, setSelectedCategorySeries] = useState([]);
  const [merchants, setMerchants] = useState([]);

  const [catalog, setCatalog] = useState({ expense: [], income: [] });

  const loadMeta = useCallback(async () => {
    const [h, m, cat] = await Promise.all([
      api.health(),
      api.months(),
      api.categories(),
    ]);
    setHealth(h);
    setMonths(m);
    setCatalog(cat);
    setCategories(cat.expense ?? []);
    return { months: m, categories: cat.expense ?? [], catalog: cat };
  }, []);

  const loadDashboard = useCallback(async () => {
    if (!selectedMonth) return;

    setLoading(true);
    setError(null);

    try {
      const [
        monthSpend,
        allSpend,
        actual,
        target,
        savingsRes,
        dailySpend,
        salaryRows,
        evE,
        pivotA,
        pivotC,
        table,
        totals,
        byCats,
        top,
      ] = await Promise.all([
        api.spendByCategory(selectedMonth),
        api.spendByCategory(),
        api.budget502030Actual(selectedMonth),
        api.budget502030Target(selectedMonth),
        api.latestSavings(),
        api.dailyExpenditures(),
        api.monthlySalary(),
        api.earningsVsExpenditures(),
        api.categoryPivot('amount'),
        api.categoryPivot('count'),
        api.categoryMonthTable(),
        api.expensesVsEarnings(),
        selectedCategories.length
          ? api.monthlyByCategories(selectedCategories)
          : Promise.resolve([]),
        api.topMerchants(15),
      ]);

      setSpendByMonth(monthSpend);
      setSpendAllTime(allSpend);
      setBudgetActual(actual);
      setBudgetTarget(target);
      setSavings(savingsRes.amount);
      setDaily(dailySpend);
      setSalary(salaryRows);
      setEarningsVsExp(evE);
      setPivotAmount(pivotA);
      setPivotCount(pivotC);
      setCategoryTable(table);
      setExpensesVsEarnings(totals);
      setSelectedCategorySeries(byCats);
      setMerchants(top);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, [selectedMonth, selectedCategories]);

  useEffect(() => {
    loadMeta().catch((e) => setError(e.message));
  }, [loadMeta]);

  useEffect(() => {
    loadDashboard();
  }, [loadDashboard]);

  return {
    health,
    months,
    categories,
    loading,
    error,
    spendByMonth,
    spendAllTime,
    budgetActual,
    budgetTarget,
    savings,
    daily,
    salary,
    earningsVsExp,
    pivotAmount,
    pivotCount,
    categoryTable,
    expensesVsEarnings,
    selectedCategorySeries,
    merchants,
    catalog,
    reload: async () => {
      await loadMeta();
      await loadDashboard();
    },
  };
}
