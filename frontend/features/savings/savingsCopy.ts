import {
  moneyCommaError,
  moneyDigits,
} from "@/features/accounts/formatCurrency";
import type { SavingsGoalKind, UpsertSavingsGoalDto } from "@/lib/api/types";

/**
 * The fields the savings form edits.
 * `reservedAmount` blank means nothing is available or set aside yet. `useAccountBalance` follows the selected account.
 * `monthlyAmount` and `readyDay` are for monthly living spending. `floorAmount` is the cash to always keep.
 */
export type SavingsFormState = {
  kind: SavingsGoalKind;
  name: string;
  targetAmount: string;
  targetDate: string;
  monthlyAmount: string;
  readyDay: string;
  floorAmount: string;
  reservedAmount: string;
  accountId: string;
  useAccountBalance: boolean;
};

/**
 * What the savings form can save, or the reason it cannot.
 * `ok` is false when a required fact is missing or an amount is not money.
 */
export type SavingsFormResult =
  { ok: true; dto: UpsertSavingsGoalDto } | { ok: false; error: string };

/**
 * The facts the progress sentence reads.
 * `amountNeededPerMonth` is null when there is no monthly schedule.
 */
export type SavingsProgress = {
  alreadyMet: boolean;
  datePassed: boolean;
  beyondHorizon: boolean;
  remaining: number;
  amountNeededPerMonth: number | null;
  finalAmountNeeded: number | null;
};

const maxAmount = 100_000_000;

/**
 * Reads an amount set aside.
 * Blank is zero. A negative amount or other text is rejected.
 */
export function reservedFromField(value: string): number | null {
  const trimmed = value.trim();
  if (!trimmed) {
    return 0;
  }

  const rounded = roundMoney(trimmed);
  if (rounded === null || rounded < 0 || rounded > maxAmount) {
    return null;
  }

  return rounded;
}

/**
 * Reads a target amount.
 * It has to be greater than zero. Blank is not stored as zero.
 */
export function targetFromField(value: string): number | null {
  const amount = reservedFromField(value);
  if (amount === null || amount <= 0) {
    return null;
  }

  return amount;
}

/**
 * The sentence under a goal: funded, due now, too far out, or the monthly amount.
 * The last month is named when it differs from the earlier months.
 */
export function goalProgress(
  goal: SavingsProgress,
  money: (amount: number) => string,
): string {
  if (goal.alreadyMet) {
    return "This is funded.";
  }

  if (goal.datePassed) {
    return `${money(goal.remaining)} is due now.`;
  }

  if (goal.beyondHorizon || goal.amountNeededPerMonth === null) {
    return "That date is too far out to schedule.";
  }

  const monthly = `Set aside ${money(goal.amountNeededPerMonth)} a month.`;
  if (
    goal.finalAmountNeeded !== null &&
    goal.finalAmountNeeded !== goal.amountNeededPerMonth
  ) {
    return `${monthly} The last month is ${money(goal.finalAmountNeeded)}.`;
  }

  return monthly;
}

/**
 * The line that says where the amount in use came from.
 * A goal with no account has nothing extra to say.
 */
export function followNote(goal: {
  following: boolean;
  reservedOverridden: boolean;
  accountUnavailable: boolean;
  negativeBalance: boolean;
  accountName: string | null;
}): string | null {
  if (goal.accountUnavailable) {
    return "This account can no longer be followed. The amount above is what the plan uses.";
  }

  if (!goal.following) {
    return null;
  }

  const name = goal.accountName ?? "This account";
  if (goal.negativeBalance) {
    return `${name} is below zero, so nothing is set aside from it.`;
  }

  if (goal.reservedOverridden) {
    return `Using your amount instead of the ${name} balance.`;
  }

  return `Follows ${name}.`;
}

/**
 * The day-of-month label, such as "The 1st of each month".
 * A 31st in a short month is that month's last day when the plan counts it.
 */
export function readyByLabel(day: number): string {
  return `The ${day}${daySuffix(day)} of each month`;
}

/**
 * The short day name, such as "the 1st".
 * The row uses this after the monthly amount.
 */
export function readyByShort(day: number): string {
  return `the ${day}${daySuffix(day)}`;
}

/**
 * The English suffix for a day of the month.
 * 11, 12, and 13 stay "th".
 */
function daySuffix(day: number): string {
  const mod = day % 100;
  if (mod >= 11 && mod <= 13) {
    return "th";
  }

  if (day % 10 === 1) {
    return "st";
  }

  if (day % 10 === 2) {
    return "nd";
  }

  if (day % 10 === 3) {
    return "rd";
  }

  return "th";
}

/**
 * Turns the form into the API body.
 * Monthly living spending needs a monthly amount and a day. Cash to keep needs an amount to keep. A named goal needs a name, a target, and a date.
 * An account that is not offered has to be cleared first.
 */
export function toSavingsPayload(
  form: SavingsFormState,
  accountOffered: boolean,
): SavingsFormResult {
  if (form.kind === "Sinking" && form.name.trim().length === 0) {
    return { ok: false, error: "Name what you are saving for." };
  }

  if (form.name.trim().length > 80) {
    return { ok: false, error: "Use 80 characters or fewer." };
  }

  const reservedAmount = reservedFromField(form.reservedAmount);
  if (reservedAmount === null) {
    return {
      ok: false,
      error: amountError(
        form.reservedAmount,
        form.kind === "Operating" || form.kind === "Floor"
          ? "Enter zero or a positive amount available now."
          : "Enter zero or a positive amount set aside.",
      ),
    };
  }

  if (form.accountId && !accountOffered) {
    return {
      ok: false,
      error: "Choose a cash account, or choose no account.",
    };
  }

  const accountId = form.accountId || null;
  const useAccountBalance = Boolean(form.accountId) && form.useAccountBalance;
  const name = form.kind === "Sinking" ? form.name.trim() : null;

  if (form.kind === "Operating") {
    const monthlyAmount = targetFromField(form.monthlyAmount);
    const readyDay = Number(form.readyDay);
    if (monthlyAmount === null) {
      return {
        ok: false,
        error: amountError(
          form.monthlyAmount,
          "Enter a monthly amount greater than zero.",
        ),
      };
    }

    if (!Number.isInteger(readyDay) || readyDay < 1 || readyDay > 31) {
      return {
        ok: false,
        error: "Choose the day of the month this spending counts.",
      };
    }

    return {
      ok: true,
      dto: {
        kind: form.kind,
        name,
        targetAmount: null,
        targetDate: null,
        monthlyAmount,
        readyDay,
        floorAmount: null,
        reservedAmount,
        accountId,
        useAccountBalance,
      },
    };
  }

  if (form.kind === "Floor") {
    const floorAmount = targetFromField(form.floorAmount);
    if (floorAmount === null) {
      return {
        ok: false,
        error: amountError(
          form.floorAmount,
          "Enter an amount to keep greater than zero.",
        ),
      };
    }

    return {
      ok: true,
      dto: {
        kind: form.kind,
        name,
        targetAmount: null,
        targetDate: null,
        monthlyAmount: null,
        readyDay: null,
        floorAmount,
        reservedAmount,
        accountId,
        useAccountBalance,
      },
    };
  }

  const targetAmount = targetFromField(form.targetAmount);
  if (targetAmount === null) {
    return {
      ok: false,
      error: amountError(
        form.targetAmount,
        "Enter a target greater than zero.",
      ),
    };
  }

  if (!/^\d{4}-\d{2}-\d{2}$/.test(form.targetDate)) {
    return { ok: false, error: "Choose a target date." };
  }

  return {
    ok: true,
    dto: {
      kind: form.kind,
      name,
      targetAmount,
      targetDate: form.targetDate,
      monthlyAmount: null,
      readyDay: null,
      floorAmount: null,
      reservedAmount,
      accountId,
      useAccountBalance,
    },
  };
}

/**
 * Rounds a numeric field to cents.
 * Anything that is not a finite number is rejected.
 */
/**
 * The comma message when the amount uses a comma that is not thousands.
 * Otherwise the caller's own message, such as a missing target.
 */
function amountError(value: string, fallback: string): string {
  return moneyCommaError(value) ?? fallback;
}

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
