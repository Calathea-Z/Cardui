import { MANUAL_ACCOUNT_TYPES, type ManualAccountType } from "@/lib/api/types";
import { moneyDigits } from "./formatCurrency";

/**
 * Labels for the manual account types, in the same order as the API list.
 * Every manual account type needs a label.
 */
const ACCOUNT_TYPE_LABELS: Record<ManualAccountType, string> = {
  depository: "Cash",
  investment: "Investment",
  credit: "Credit card",
  loan: "Loan",
};

/**
 * Account types a household can create by hand.
 * The labels are the words on the form. The values match the API.
 */
export const ACCOUNT_TYPE_OPTIONS = MANUAL_ACCOUNT_TYPES.map((value) => ({
  value,
  label: ACCOUNT_TYPE_LABELS[value],
}));

/**
 * True when the value is a type the manual account form can save.
 */
export function isManualAccountType(value: string): value is ManualAccountType {
  return MANUAL_ACCOUNT_TYPES.some((type) => type === value);
}

/**
 * Keeps a stored account type when the manual form offers it.
 * Any other stored type starts as cash so the type list still has a selection.
 */
export function toManualAccountType(value: string): ManualAccountType {
  return isManualAccountType(value) ? value : "depository";
}

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
 * A thousands comma is part of the amount, so 30,000 is 30000. Blank or other text is null.
 */
export function parseMoney(value: string) {
  const digits = moneyDigits(value);
  if (digits === null) {
    return null;
  }

  const parsed = Number(digits);
  if (!Number.isFinite(parsed)) {
    return null;
  }

  return Math.round(parsed * 100) / 100;
}

/**
 * Explains the opening balance for the selected account type.
 * Credit and loan balances are amounts already owed.
 */
export function openingBalanceHelp(type: ManualAccountType) {
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
