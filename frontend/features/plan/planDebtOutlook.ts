import type { PayoffRolloverKind, PlanRecoveryPathDto } from "@/lib/api/types";

/**
 * Formats a `YYYY-MM-DD` date for on-screen text.
 */
type FormatDate = (date: string) => string;

/**
 * The small debt outlook under next-30-day safety.
 * Payment priority is who is paid first. The payoff line is the earliest projected end date.
 */
export type PlanDebtOutlookCopy = {
  priority: string;
  payoff: string;
  availability: string;
  conditionalNote: string | null;
};

/**
 * Builds the overview debt lines from the selected path.
 * A cash shortfall makes the payoff date conditional. The lowest-balance date is not reused as a payoff date.
 */
export function planDebtOutlook(
  path: PlanRecoveryPathDto,
  kind: PayoffRolloverKind,
  cashShort: boolean,
  date: FormatDate,
): PlanDebtOutlookCopy {
  const priority = path.debts[0];
  const first = earliestPayoff(path);
  return {
    priority: priority
      ? `${priority.name} is first in payment priority.`
      : "No payment priority yet.",
    payoff: first ? `${first.name} · ${date(first.endedOn)}` : "Not projected",
    availability: availabilityLine(first, kind),
    conditionalNote:
      cashShort && first
        ? "This payoff estimate assumes the cash shortfall is resolved. It is not a payment due date."
        : null,
  };
}

/**
 * The payoff step with the earliest end date.
 * Steps are not assumed to already be sorted.
 */
function earliestPayoff(path: PlanRecoveryPathDto) {
  return [...path.steps].sort((left, right) =>
    left.endedOn.localeCompare(right.endedOn),
  )[0];
}

/**
 * Says whether the first payoff's payment rolls forward or becomes available.
 * The wording follows the selected path and does not invent an amount.
 */
function availabilityLine(
  first: ReturnType<typeof earliestPayoff>,
  kind: PayoffRolloverKind,
) {
  if (!first) {
    return "Not projected until a debt can be paid off.";
  }

  if (kind === "Reclaim" && first.breathingRoom > 0) {
    return "Part of that payment becomes available after this payoff. The rest rolls to the next debt.";
  }

  if (kind === "Rollover" && first.breathingRoom <= 0) {
    return "That payment rolls to the next debt. It is not available cash yet.";
  }

  if (first.breathingRoom > 0) {
    return kind === "Rollover"
      ? "That payment becomes available only after no modeled debt can take it."
      : "That payment becomes available for other uses after this payoff.";
  }

  return "No additional breathing room is modeled after this payoff.";
}
