import {
  moneyCommaError,
  moneyDigits,
} from "@/features/accounts/formatCurrency";
import type { LivingContributionDto, LivingGapDto } from "@/lib/api/types";

/**
 * The amount a contribution field should show.
 * A null stored amount stays blank, so it is not shown as zero.
 */
export function contributionField(amount: number | null): string {
  return amount === null ? "" : String(amount);
}

/**
 * Reads a contribution field.
 * Blank clears it. Zero is a known zero.
 */
export function contributionAmount(
  value: string,
): { ok: true; amount: number | null } | { ok: false; error: string } {
  if (value.trim() === "") {
    return { ok: true, amount: null };
  }

  const amount = roundMoney(value);
  if (amount === null || amount < 0) {
    return {
      ok: false,
      error: moneyCommaError(value) ?? "Enter an amount of zero or more.",
    };
  }

  return { ok: true, amount };
}

/**
 * The sentence under one contribution.
 * A blank amount keeps their full pay in the plan. A set amount names today's shared amount.
 */
export function contributionNote(
  person: LivingContributionDto,
  money: (amount: number) => string,
): string {
  const unscheduled = person.hasUnscheduledPay
    ? " A paycheck without a schedule still arrives on its date."
    : "";
  if (person.limit === "Unplaced") {
    return `This amount can't be placed on a scheduled paycheck yet. The plan does not invent a deposit.${unscheduled}`;
  }

  if (person.limit === "AllRecordedPay" && person.recordedMonthly !== null) {
    return `The shared plan uses all of ${person.name}'s recorded pay, ${money(person.recordedMonthly)} a month.${unscheduled}`;
  }

  if (person.limit === "Shared") {
    const benchmark = person.monthlyAmount ?? person.sharedMonthly;
    const average =
      benchmark === person.sharedMonthly
        ? ""
        : ` Applied to dated paychecks, the current monthly average is ${money(person.sharedMonthly)}.`;
    return `The saved monthly benchmark is ${money(benchmark)}.${average} ${person.name} keeps about ${money(person.keptMonthly)} at today's pay. Low pay and future raises keep this same share.${unscheduled}`;
  }

  if (person.recordedMonthly === null) {
    return `No scheduled paycheck is assigned to ${person.name} yet. Their full recorded pay stays in the shared plan.`;
  }

  return `${person.name}'s full recorded pay, ${money(person.recordedMonthly)} a month, stays in the shared plan.${unscheduled}`;
}

/**
 * The monthly affordability sentence.
 * Zero shortfall means the average is covered; missing inputs remain listed separately.
 */
export function gapSentence(
  gap: LivingGapDto,
  money: (amount: number) => string,
): { warning: boolean; text: string } | null {
  const pictured =
    gap.sharedMonthly > 0 ||
    gap.billsMonthly > 0 ||
    gap.minimumsMonthly > 0 ||
    gap.livingSpendingMonthly > 0;
  if (!pictured) {
    return null;
  }

  if (gap.shortfall > 0) {
    return {
      warning: true,
      text: `The shared plan is short ${money(gap.shortfall)} a month for bills, debt minimums, and monthly living spending.`,
    };
  }

  return {
    warning: false,
    text:
      `This monthly average is covered, including ${money(gap.livingSpendingMonthly)} for living spending. ` +
      "Plan shows whether the dated cash flow also works.",
  };
}

/**
 * Rounds a numeric field to cents.
 * A comma that is not thousands grouping is rejected by the caller.
 */
function roundMoney(value: string): number | null {
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
