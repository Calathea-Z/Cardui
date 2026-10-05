/**
 * A raise the Income page can ask about once its date has arrived.
 * `effectiveDate` is a calendar day, `YYYY-MM-DD`.
 */
export type IncomeRaiseReview = {
  id: string;
  effectiveDate: string;
  takeHomeAmount: number;
};

/**
 * The payment amounts on a source before a raise is confirmed.
 * Low and strong are null when that scenario is not recorded.
 */
export type IncomeRaiseReviewAmounts = {
  takeHomeAmount: number;
  lowTakeHomeAmount: number | null;
  strongTakeHomeAmount: number | null;
};

/**
 * The amounts to store after the user says the raise is the pay they receive now.
 * A cleared low or strong no longer fits the new typical amount.
 */
export type ConfirmedRaiseAmounts = IncomeRaiseReviewAmounts & {
  clearedLow: boolean;
  clearedStrong: boolean;
};

/**
 * The household's calendar day for a clock time.
 * An unrecognized time zone uses the UTC date so the page can still render.
 */
export function calendarDateInTimeZone(timeZoneId: string, now: Date) {
  try {
    const parts = new Intl.DateTimeFormat("en-US", {
      timeZone: timeZoneId,
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
    }).formatToParts(now);
    const year = parts.find((part) => part.type === "year")?.value;
    const month = parts.find((part) => part.type === "month")?.value;
    const day = parts.find((part) => part.type === "day")?.value;
    if (!year || !month || !day) {
      return utcDate(now);
    }

    return `${year}-${month}-${day}`;
  } catch {
    return utcDate(now);
  }
}

/**
 * True when the raise date is today or earlier in the household calendar.
 * A later date stays an expectation and does not ask for a decision.
 */
export function isRaiseDue(effectiveDate: string, today: string) {
  return effectiveDate.slice(0, 10) <= today;
}

/**
 * Applies a confirmed raise to the typical amount.
 * Low is cleared when it would be above the new amount. Strong is cleared when it would be below it.
 */
export function amountsAfterConfirmingRaise(
  current: IncomeRaiseReviewAmounts,
  raiseAmount: number,
): ConfirmedRaiseAmounts {
  const clearedLow =
    current.lowTakeHomeAmount !== null &&
    current.lowTakeHomeAmount > raiseAmount;
  const clearedStrong =
    current.strongTakeHomeAmount !== null &&
    current.strongTakeHomeAmount < raiseAmount;

  return {
    takeHomeAmount: raiseAmount,
    lowTakeHomeAmount: clearedLow ? null : current.lowTakeHomeAmount,
    strongTakeHomeAmount: clearedStrong ? null : current.strongTakeHomeAmount,
    clearedLow,
    clearedStrong,
  };
}

/**
 * States the typical amount now stored after the user confirms a raise.
 * A cleared scenario is named so the change is visible.
 */
export function typicalPayUpdatedMessage(
  name: string,
  amount: string,
  clearedLow: boolean,
  clearedStrong: boolean,
) {
  const base = `Typical pay for ${name} is now ${amount}.`;
  if (clearedLow && clearedStrong) {
    return `${base} Low and strong pay were cleared because they no longer fit that amount.`;
  }

  if (clearedLow) {
    return `${base} Low pay was cleared because it was higher than that amount.`;
  }

  if (clearedStrong) {
    return `${base} Strong pay was cleared because it was lower than that amount.`;
  }

  return base;
}

/**
 * States that an expected raise was removed and the current typical pay is unchanged.
 */
export function raiseRemovedMessage(name: string, amount: string) {
  return `The expected raise was removed. Typical pay for ${name} is still ${amount}.`;
}

/**
 * UTC calendar day for a clock time.
 * Used when the household time zone cannot be read.
 */
function utcDate(now: Date) {
  return now.toISOString().slice(0, 10);
}
