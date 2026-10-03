export const ACCOUNT_TYPE_OPTIONS = [
  { value: "depository", label: "Cash" },
  { value: "investment", label: "Investment" },
  { value: "credit", label: "Credit card" },
  { value: "loan", label: "Loan" },
] as const;

export function todayDateInput() {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${now.getFullYear()}-${month}-${day}`;
}

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

export function openingBalanceHelp(type: string) {
  if (type === "credit" || type === "loan") {
    return "The amount already owed. It is not a purchase, a payment, or income.";
  }

  return "The balance already in this account. It is not income.";
}

export function isManualAccount(account: { plaidItemId: string | null }) {
  return account.plaidItemId === null;
}
