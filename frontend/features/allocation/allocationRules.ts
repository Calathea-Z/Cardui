import type { UpsertAllocationPreferenceDto } from "@/lib/api/types/allocation";

/**
 * The percent fields while they are being edited.
 * Empty text is not a stored percent.
 */
export type AllocationFormState = {
  variableIncomeDebtPercent: string;
  variableIncomeSavingsPercent: string;
  windfallDebtPercent: string;
  windfallSavingsPercent: string;
};

/**
 * Reads one whole percent from a field.
 * Blank text and values outside 0 to 100 are rejected.
 */
export function parsePercent(
  value: string,
): { ok: true; percent: number } | { ok: false; error: string } {
  const trimmed = value.trim();
  if (!/^\d{1,3}$/.test(trimmed)) {
    return { ok: false, error: "Enter a whole percent from 0 to 100." };
  }

  const percent = Number(trimmed);
  if (percent > 100) {
    return { ok: false, error: "Enter a whole percent from 0 to 100." };
  }

  return { ok: true, percent };
}

/**
 * The share that stays available after debt and savings.
 * Returns null when the two percents are not a valid pair.
 */
export function cashPercent(debt: number, savings: number): number | null {
  if (
    !Number.isInteger(debt) ||
    !Number.isInteger(savings) ||
    debt < 0 ||
    savings < 0 ||
    debt > 100 ||
    savings > 100 ||
    debt + savings > 100
  ) {
    return null;
  }

  return 100 - debt - savings;
}

/**
 * Rounds money to cents, away from zero.
 * A negative amount keeps its sign.
 */
function roundCents(amount: number): number {
  const sign = amount < 0 ? -1 : 1;
  return (sign * Math.round(Math.abs(amount) * 100)) / 100;
}

/**
 * Splits one amount into extra debt, savings, and what stays available.
 * Cents round away from zero. When rounding would pass the amount, savings is reduced first, then debt.
 * A zero or negative amount stays all zeros. This does not move money.
 */
export function splitAllocation(
  amount: number,
  debtPercent: number,
  savingsPercent: number,
): { debt: number; savings: number; cash: number } {
  const rounded = roundCents(amount);
  if (rounded <= 0) {
    return { debt: 0, savings: 0, cash: 0 };
  }

  let debt = roundCents((rounded * debtPercent) / 100);
  let savings = roundCents((rounded * savingsPercent) / 100);
  let overflow = roundCents(debt + savings - rounded);
  if (overflow > 0) {
    const fromSavings = overflow > savings ? savings : overflow;
    savings = roundCents(savings - fromSavings);
    overflow = roundCents(overflow - fromSavings);
    if (overflow > 0) {
      debt = roundCents(debt - (overflow > debt ? debt : overflow));
    }
  }

  return {
    debt,
    savings,
    cash: roundCents(rounded - debt - savings),
  };
}

/**
 * Reads both rules from the fields.
 * The first invalid field stops the save. Debt and savings that add past 100 are rejected.
 */
export function readAllocationForm(
  form: AllocationFormState,
):
  | { ok: true; dto: UpsertAllocationPreferenceDto }
  | { ok: false; error: string } {
  const variableDebt = parsePercent(form.variableIncomeDebtPercent);
  if (!variableDebt.ok) {
    return {
      ok: false,
      error: `Variable income extra debt: ${variableDebt.error}`,
    };
  }

  const variableSavings = parsePercent(form.variableIncomeSavingsPercent);
  if (!variableSavings.ok) {
    return {
      ok: false,
      error: `Variable income savings: ${variableSavings.error}`,
    };
  }

  if (variableDebt.percent + variableSavings.percent > 100) {
    return {
      ok: false,
      error:
        "Variable income: debt and savings together can't be more than 100 percent. The rest stays available.",
    };
  }

  const windfallDebt = parsePercent(form.windfallDebtPercent);
  if (!windfallDebt.ok) {
    return { ok: false, error: `Windfall extra debt: ${windfallDebt.error}` };
  }

  const windfallSavings = parsePercent(form.windfallSavingsPercent);
  if (!windfallSavings.ok) {
    return { ok: false, error: `Windfall savings: ${windfallSavings.error}` };
  }

  if (windfallDebt.percent + windfallSavings.percent > 100) {
    return {
      ok: false,
      error:
        "Windfall: debt and savings together can't be more than 100 percent. The rest stays available.",
    };
  }

  return {
    ok: true,
    dto: {
      variableIncomeDebtPercent: variableDebt.percent,
      variableIncomeSavingsPercent: variableSavings.percent,
      windfallDebtPercent: windfallDebt.percent,
      windfallSavingsPercent: windfallSavings.percent,
    },
  };
}

/**
 * Starts the fields from the stored percents.
 * An unsaved page still shows zero, which leaves the whole amount available.
 */
export function allocationFormFromPage(page: {
  variableIncome: { debtPercent: number; savingsPercent: number };
  windfall: { debtPercent: number; savingsPercent: number };
}): AllocationFormState {
  return {
    variableIncomeDebtPercent: String(page.variableIncome.debtPercent),
    variableIncomeSavingsPercent: String(page.variableIncome.savingsPercent),
    windfallDebtPercent: String(page.windfall.debtPercent),
    windfallSavingsPercent: String(page.windfall.savingsPercent),
  };
}
