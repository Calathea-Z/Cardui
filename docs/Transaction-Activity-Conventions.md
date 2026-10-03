# Transaction activity conventions

The dashboard's monthly activity is a categorized, posted-transaction view. It
is not a complete cash-flow statement.

## Included period

- The period starts on the first day of the current local financial month and
  ends on the current local financial date.
- Transactions outside that inclusive date range do not affect monthly
  activity.
- Pending transactions remain visible in recent activity but do not affect
  income, spending, or spending by category until they post.

## Classification and signs

Plaid uses positive amounts for money leaving an account and negative amounts
for money entering an account.

- **Income:** Only transactions assigned to the Income group or system Income
  category affect income. Incoming amounts increase income; positive
  corrections or reversals reduce it. Net monthly income is floored at zero.
- **Spending:** Posted transactions outside the Income and Transfers groups
  affect spending. Positive amounts increase their category's spending.
- **Refunds:** A negative amount outside Income and Transfers is a refund or
  credit, not income. It reduces spending in its assigned category.
  Category spending is floored at zero, so a refund larger than the month's
  purchases does not create negative spending or offset another category.
- **Transfers:** Transactions in the Transfers group or system Transfers
  category do not affect income, spending, or category spending, regardless of
  amount direction.
- **Opening balances:** An opening balance is stored on the account. It sets
  the starting balance and is not a transaction, so it is not income, spending,
  or a transfer.
- **Balance reconciliation:** A statement match that differs from the calculated
  balance is saved as its own adjustment. That adjustment is excluded from
  income, spending, and category spending. Archiving a transaction removes it
  from those totals. Archiving an account removes its balance from net worth
  and leaves its transactions in activity until those transactions are archived.
- **Uncategorized activity:** Positive amounts count as Uncategorized
  spending. Negative amounts reduce Uncategorized spending and never become
  income solely because of their sign.

Monthly spending is the sum of the resulting non-negative category totals.
The dashboard difference is income minus spending under these conventions.

## Automatic categorization and existing data

New imports infer Income only from recognizable income descriptions such as
payroll, direct deposit, salary, pension, interest earned, or dividends.
Merchant refunds retain the matching expense category, and an unrecognized
negative amount defaults to Other instead of Income.

This improves new imports but does not rewrite stored category assignments. A
refund already assigned to Income cannot be distinguished from earned income
by the dashboard alone. Safely reviewing old assignments requires separate
work because transactions do not currently record whether a category was
selected by a user or an importer.
