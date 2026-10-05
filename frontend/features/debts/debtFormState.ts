import type { DebtDto, DebtKind, UpsertDebtDto } from "@/lib/api/types";

/**
 * Fields the debt form edits.
 * Kind stays empty until the user chooses it.
 * A blank term stays blank so a save does not store it as zero.
 * `accountId` is empty when the debt is not linked to a household account.
 */
export type DebtFormState = {
  name: string;
  kind: DebtKind | "";
  accountId: string;
  balance: string;
  balanceAsOf: string;
  apr: string;
  minimumPayment: string;
  nextDueDate: string;
  creditLimit: string;
  remainingTermMonths: string;
  promotionalApr: string;
  promotionalEndsOn: string;
};

/**
 * The save payload, or the reason the form is not ready.
 * `ok` is false when the name or type is missing, or a filled term is not usable.
 */
export type DebtUpsertResult =
  { ok: true; dto: UpsertDebtDto } | { ok: false; error: string };

/**
 * Blank form for a new debt.
 * The type starts unset so a save cannot silently pick revolving or installment.
 */
export function emptyDebtForm(): DebtFormState {
  return {
    name: "",
    kind: "",
    accountId: "",
    balance: "",
    balanceAsOf: "",
    apr: "",
    minimumPayment: "",
    nextDueDate: "",
    creditLimit: "",
    remainingTermMonths: "",
    promotionalApr: "",
    promotionalEndsOn: "",
  };
}

/**
 * Copies a stored debt into the form.
 * A null term becomes a blank field. Dates keep the calendar day.
 */
export function debtToForm(debt: DebtDto): DebtFormState {
  return {
    name: debt.name,
    kind: debt.kind,
    accountId: debt.accountId ?? "",
    balance: optionalNumber(debt.balance),
    balanceAsOf: optionalDate(debt.balanceAsOf),
    apr: optionalNumber(debt.apr),
    minimumPayment: optionalNumber(debt.minimumPayment),
    nextDueDate: optionalDate(debt.nextDueDate),
    creditLimit: optionalNumber(debt.creditLimit),
    remainingTermMonths: optionalNumber(debt.remainingTermMonths),
    promotionalApr: optionalNumber(debt.promotionalApr),
    promotionalEndsOn: optionalDate(debt.promotionalEndsOn),
  };
}

/**
 * Builds the save payload from the form.
 * A blank term is sent as null. A credit limit is sent only for revolving, and a remaining term only for installment.
 */
export function toDebtUpsert(form: DebtFormState): DebtUpsertResult {
  const name = form.name.trim();
  if (!name) {
    return { ok: false, error: "A debt name is required." };
  }

  if (name.length > 80) {
    return { ok: false, error: "A debt name must be 80 characters or fewer." };
  }

  if (!form.kind) {
    return { ok: false, error: "Choose revolving or installment." };
  }

  const balance = readOptionalMoney(
    form.balance,
    true,
    "Enter the balance in dollars and cents, or leave it blank.",
  );
  if (!balance.ok) {
    return balance;
  }

  if (balance.amount === null && form.balanceAsOf) {
    return { ok: false, error: "Enter the balance, or clear the date." };
  }

  if (balance.amount !== null && !form.balanceAsOf) {
    return { ok: false, error: "Enter the date this balance was true." };
  }

  const apr = readOptionalApr(form.apr, "APR");
  if (!apr.ok) {
    return apr;
  }

  const minimum = readOptionalMoney(
    form.minimumPayment,
    true,
    "Enter the minimum in dollars and cents, or leave it blank.",
  );
  if (!minimum.ok) {
    return minimum;
  }

  const creditLimit =
    form.kind === "Revolving"
      ? readOptionalMoney(
          form.creditLimit,
          false,
          "Enter the credit limit, or leave it blank.",
        )
      : { ok: true as const, amount: null };
  if (!creditLimit.ok) {
    return creditLimit;
  }

  const term =
    form.kind === "Installment"
      ? readRemainingTerm(form.remainingTermMonths)
      : { ok: true as const, months: null };
  if (!term.ok) {
    return term;
  }

  const promotionalApr = readOptionalApr(
    form.promotionalApr,
    "promotional APR",
  );
  if (!promotionalApr.ok) {
    return promotionalApr;
  }

  return {
    ok: true,
    dto: {
      name,
      kind: form.kind,
      accountId: form.accountId || null,
      balance: balance.amount,
      balanceAsOf: balance.amount === null ? null : form.balanceAsOf,
      apr: apr.rate,
      minimumPayment: minimum.amount,
      nextDueDate: form.nextDueDate || null,
      creditLimit: creditLimit.amount,
      remainingTermMonths: term.months,
      promotionalApr: promotionalApr.rate,
      promotionalEndsOn: form.promotionalEndsOn || null,
    },
  };
}

/**
 * A stored optional number as form text.
 * Null stays blank, so the field is not filled with the word "undefined".
 */
function optionalNumber(value: number | null) {
  if (value === null) {
    return "";
  }

  return String(value);
}

/**
 * A stored optional date as form text.
 * Null stays blank. A date-time value keeps the calendar day.
 */
function optionalDate(value: string | null) {
  return value ? value.slice(0, 10) : "";
}

/**
 * Reads an optional dollar amount.
 * Blank stays null. Zero is kept when allowZero is true. Extra fraction digits are rejected.
 */
function readOptionalMoney(
  value: string,
  allowZero: boolean,
  invalidMessage: string,
): { ok: true; amount: number | null } | { ok: false; error: string } {
  const trimmed = value.trim();
  if (!trimmed) {
    return { ok: true, amount: null };
  }

  if (!/^\d+(\.\d{1,2})?$/.test(trimmed)) {
    return { ok: false, error: "Enter the amount in dollars and cents." };
  }

  const amount = Number(trimmed);
  if (!Number.isFinite(amount) || amount < 0 || (!allowZero && amount === 0)) {
    return { ok: false, error: invalidMessage };
  }

  if (amount > 100_000_000) {
    return { ok: false, error: "That amount is too large." };
  }

  return { ok: true, amount };
}

/**
 * Reads an optional percent.
 * Blank stays null. Zero is a known 0% rate. More than three decimal places is rejected.
 */
function readOptionalApr(
  value: string,
  label: string,
): { ok: true; rate: number | null } | { ok: false; error: string } {
  const trimmed = value.trim();
  if (!trimmed) {
    return { ok: true, rate: null };
  }

  if (/^\d+\.\d{4,}$/.test(trimmed)) {
    return {
      ok: false,
      error: `Enter the ${label} with up to three decimal places.`,
    };
  }

  if (!/^\d+(\.\d{1,3})?$/.test(trimmed)) {
    return {
      ok: false,
      error: `Enter the ${label} as a percent, or leave it blank.`,
    };
  }

  const rate = Number(trimmed);
  if (!Number.isFinite(rate) || rate < 0) {
    return {
      ok: false,
      error: `Enter the ${label} as a percent, or leave it blank.`,
    };
  }

  if (rate > 999.999) {
    return { ok: false, error: `That ${label} is too large.` };
  }

  return { ok: true, rate };
}

/**
 * Reads the months left on an installment debt.
 * Blank stays null. Zero and a term longer than 50 years are rejected.
 */
function readRemainingTerm(
  value: string,
): { ok: true; months: number | null } | { ok: false; error: string } {
  const trimmed = value.trim();
  if (!trimmed) {
    return { ok: true, months: null };
  }

  if (!/^\d+$/.test(trimmed)) {
    return {
      ok: false,
      error: "Enter the months left, or leave the term blank.",
    };
  }

  const months = Number(trimmed);
  if (!Number.isInteger(months) || months < 1 || months > 600) {
    return {
      ok: false,
      error: "Enter the months left, or leave the term blank.",
    };
  }

  return { ok: true, months };
}
