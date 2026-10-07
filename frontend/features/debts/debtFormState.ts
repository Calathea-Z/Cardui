import type {
  DebtDto,
  DebtKind,
  SetDebtBalanceOverrideDto,
  SetDebtCreditLimitOverrideDto,
  UpsertDebtDto,
} from "@/lib/api/types";

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
 * While following, the balance shown is the one in use. Saving does not write that balance.
 * A followed credit limit shows the one in use. A limit the connection did not provide shows the stored one.
 */
export function debtToForm(debt: DebtDto): DebtFormState {
  const followedLimit = debt.following && debt.creditLimitSource !== "Manual";
  return {
    name: debt.name,
    kind: debt.kind,
    accountId: debt.accountId ?? "",
    balance: optionalNumber(debt.following ? debt.balanceInUse : debt.balance),
    balanceAsOf: optionalDate(
      debt.following ? debt.balanceInUseAsOf : debt.balanceAsOf,
    ),
    apr: optionalNumber(debt.apr),
    minimumPayment: optionalNumber(debt.minimumPayment),
    nextDueDate: optionalDate(debt.nextDueDate),
    creditLimit: optionalNumber(
      followedLimit ? debt.creditLimitInUse : debt.creditLimit,
    ),
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
 * The balance override payload, or the reason it is not ready.
 * The amount is required. A blank date is rejected here. Update balance uses today instead.
 */
export type BalanceOverrideResult =
  { ok: true; dto: SetDebtBalanceOverrideDto } | { ok: false; error: string };

/**
 * Reads a balance the person is keeping, with the date they chose.
 * A blank amount or date is rejected. Zero is a known zero.
 */
export function toBalanceOverride(
  balance: string,
  balanceAsOf: string,
): BalanceOverrideResult {
  const amount = readRequiredBalance(balance);
  if (!amount.ok) {
    return amount;
  }

  if (!balanceAsOf) {
    return { ok: false, error: "Enter the date this balance was true." };
  }

  return { ok: true, dto: { balance: amount.amount, balanceAsOf } };
}

/**
 * The credit-limit override payload, or the reason it is not ready.
 * The amount is required. Zero is not a limit.
 */
export type CreditLimitOverrideResult =
  | { ok: true; dto: SetDebtCreditLimitOverrideDto }
  | { ok: false; error: string };

/**
 * Reads a credit limit the person is keeping.
 * Blank and zero are rejected. An amount equal to the synced limit is still valid.
 */
export function toCreditLimitOverride(
  creditLimit: string,
): CreditLimitOverrideResult {
  const amount = readOptionalMoney(
    creditLimit,
    false,
    "Enter a credit limit above zero.",
  );
  if (!amount.ok) {
    return amount;
  }

  if (amount.amount === null) {
    return { ok: false, error: "Enter the credit limit." };
  }

  return { ok: true, dto: { creditLimit: amount.amount } };
}

/**
 * Reads a balance dated today by the server.
 * The date is omitted so the household's today is used.
 */
export function toTodayBalanceOverride(balance: string): BalanceOverrideResult {
  const amount = readRequiredBalance(balance);
  if (!amount.ok) {
    return amount;
  }

  return { ok: true, dto: { balance: amount.amount, balanceAsOf: null } };
}

/**
 * Reads a required dollar amount for a balance override.
 * Blank is rejected. Zero is kept.
 */
function readRequiredBalance(
  value: string,
): { ok: true; amount: number } | { ok: false; error: string } {
  const amount = readOptionalMoney(
    value,
    true,
    "Enter the balance in dollars and cents.",
  );
  if (!amount.ok) {
    return amount;
  }

  if (amount.amount === null) {
    return { ok: false, error: "Enter the balance." };
  }

  return { ok: true, amount: amount.amount };
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
