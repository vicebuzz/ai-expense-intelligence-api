"""Rule-based transaction categorisation (keyword → category)."""

from __future__ import annotations

RULES: list[tuple[str, str]] = [
    # Groceries
    ("FRESHMART", "Groceries"),
    ("SUPERMARKET", "Groceries"),
    ("GROCER", "Groceries"),
    # Food
    ("CAFE", "Food"),
    ("PIZZA", "Food"),
    ("RESTAURANT", "Food"),
    ("PATISSER", "Food"),
    # Transport
    ("RIDESHARE", "Transport"),
    ("UBER", "Transport"),
    ("BUS", "Transport"),
    ("COACH", "Transport"),
    ("TRAIN", "Transport"),
    # Bills & subscriptions
    ("UTILITY", "Bills"),
    ("UTILITIES", "Bills"),
    ("ELECTRIC", "Bills"),
    ("STREAMFLIX", "Subscriptions"),
    ("SUBSCRIPTION", "Subscriptions"),
    ("APPLE.COM", "Subscriptions"),
    # Health, sport, education
    ("PHARMACY", "Health"),
    ("GYM", "Sports"),
    ("BOOKSHOP", "Education"),
    ("UNIVERSITY", "Education"),
    # Shopping & gifts
    ("BOUTIQUE", "Presents"),
    ("GIFT", "Presents"),
    ("MALL", "Shopping"),
    ("SHOP", "Shopping"),
    # Housing & travel
    ("LANDLORD", "Rent"),
    ("RENT", "Rent"),
    ("HOTEL", "Travel"),
    ("HOLIDAY", "Travel"),
    # Entertainment
    ("CINEMA", "Entertainment"),
    # Income
    ("PAYROLL", "Salary"),
    ("WAGES", "Wages"),
    ("REFUND", "Refund"),
    ("DEPOSIT", "Deposit"),
    # Savings
    ("SAVINGS", "Savings"),
]

INCOME_DEFAULT = "Other"
EXPENSE_DEFAULT = "Other"


def categorize(description: str, is_expense: bool) -> tuple[str, float]:
    text = description.upper()

    for keyword, category in RULES:
        if keyword in text:
            if not is_expense and category in {
                "Groceries", "Food", "Transport", "Bills", "Entertainment",
                "Education", "Shopping", "Health", "Sports", "Rent", "Subscriptions",
            }:
                continue
            return category, 0.92

    default = EXPENSE_DEFAULT if is_expense else INCOME_DEFAULT
    return default, 0.55
