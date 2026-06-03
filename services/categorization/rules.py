"""Rule-based transaction categorisation (keyword → category)."""

from __future__ import annotations

RULES: list[tuple[str, str]] = [
    ("FRESHMART", "Groceries"),
    ("SUPERMARKET", "Groceries"),
    ("GROCER", "Groceries"),
    ("SAINSBURY", "Groceries"),
    ("TESCO", "Groceries"),
    ("LIDL", "Groceries"),
    ("ALDI", "Groceries"),
    ("MORRISONS", "Groceries"),
    ("CO-OP", "Groceries"),
    ("CAFE", "Food"),
    ("PIZZA", "Food"),
    ("RESTAURANT", "Food"),
    ("PATISSER", "Food"),
    ("STARBUCKS", "Food"),
    ("COSTA", "Food"),
    ("MCDONALDS", "Food"),
    ("RIDESHARE", "Transport"),
    ("UBER", "Transport"),
    ("BUS", "Transport"),
    ("COACH", "Transport"),
    ("TRAIN", "Transport"),
    ("UTILITY", "Bills"),
    ("UTILITIES", "Bills"),
    ("ELECTRIC", "Bills"),
    ("STREAMFLIX", "Subscriptions"),
    ("SUBSCRIPTION", "Subscriptions"),
    ("APPLE.COM", "Subscriptions"),
    ("PHARMACY", "Health&Hygiene"),
    ("GYM", "Sports&Activities"),
    ("BOOKSHOP", "Education"),
    ("UNIVERSITY", "Education"),
    ("BOUTIQUE", "Presents"),
    ("GIFT", "Presents"),
    ("MALL", "Shopping"),
    ("SHOP", "Shopping"),
    ("LANDLORD", "Rent"),
    ("RENT", "Rent"),
    ("HOTEL", "Travel&Tourism"),
    ("HOLIDAY", "Travel&Tourism"),
    ("CINEMA", "Entertainment"),
    ("PAYROLL", "Salary"),
    ("WAGES", "Wages"),
    ("REFUND", "Refund"),
    ("DEPOSIT", "Deposit"),
    ("SAVINGS", "Savings"),
]

EXPENSE_ONLY = {
    "Groceries", "Food", "Transport", "Bills", "Entertainment",
    "Education", "Shopping", "Health&Hygiene", "Sports&Activities",
    "Rent", "Subscriptions", "Presents", "Travel&Tourism", "Tobacco",
}

INCOME_DEFAULT = "Deposit"
EXPENSE_DEFAULT = "Groceries"


def categorize(
    description: str,
    is_expense: bool,
    allowed: list[str] | None = None,
) -> tuple[str, float]:
    text = description.upper()
    allowed_set = {a.strip() for a in allowed} if allowed else None

    for keyword, category in RULES:
        if keyword not in text:
            continue
        if not is_expense and category in EXPENSE_ONLY:
            continue
        if allowed_set is not None and category not in allowed_set:
            continue
        return category, 0.92

    if allowed_set:
        return next(iter(allowed_set)), 0.55

    default = EXPENSE_DEFAULT if is_expense else INCOME_DEFAULT
    return default, 0.55
