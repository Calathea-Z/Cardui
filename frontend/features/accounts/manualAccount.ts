/**
 * Account types a household can create by hand.
 * The labels are the words on the form. The values match the API.
 */
export const ACCOUNT_TYPE_OPTIONS = [
  { value: "depository", label: "Cash" },
  { value: "investment", label: "Investment" },
  { value: "credit", label: "Credit card" },
  { value: "loan", label: "Loan" },
] as const;

/**
 * Today's date as a value for a date input, in the browser's local calendar.
 */
export function todayDateInput() {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${now.getFullYear()}-${month}-${day}`;
}

/**
 * Parses a money field into cents-rounded dollars.
 * Blank or non-numeric text is null so the form can ask for a real amount.
 */
export function parseMoney(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }

  const parsed = Number(trimmed);
  if (!Number.isFinite(parsed)) {
    return null;
  }

  return Math.round(parsed * 100) / 100;
}

/**
 * Explains the opening balance for the selected account type.
 * Credit and loan balances are amounts already owed.
 */
export function openingBalanceHelp(type: string) {
  if (type === "credit" || type === "loan") {
    return "The amount already owed. It is not a purchase, a payment, or income.";
  }

  return "The balance already in this account. It is not income.";
}

/**
 * True when the account was created in Cardui and is not linked to Plaid.
 */
export function isManualAccount(account: { plaidItemId: string | null }) {
  return account.plaidItemId === null;
}
