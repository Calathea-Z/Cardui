import type { PayoffRolloverKind, PlanRecoveryPathDto } from "@/lib/api/types";

export type PlanDebtMilestones = {
  currentMinimums: string;
  firstPayoff: string;
  firstPaymentRemoved: string;
  breathingRoom: string;
  breathingRoomNote: string;
};

type FormatMoney = (amount: number) => string;
type FormatDate = (date: string) => string;

/**
 * Summarizes existing payoff milestones without deriving a new financial result.
 * The breathing-room qualification follows whether removed payments roll forward or return to cash.
 */
export function debtMilestones(
  path: PlanRecoveryPathDto,
  kind: PayoffRolloverKind,
  money: FormatMoney,
  date: FormatDate,
): PlanDebtMilestones {
  const first = path.steps[0];
  return {
    currentMinimums:
      path.startingObligation === null
        ? "Unknown"
        : `${money(path.startingObligation)} a month`,
    firstPayoff: first
      ? `${first.name} · ${date(first.endedOn)}`
      : "Not projected",
    firstPaymentRemoved: first
      ? `${money(first.minimum)} a month`
      : "Not projected",
    breathingRoom: `${money(path.recurringRoom)} a month`,
    breathingRoomNote:
      kind === "Rollover"
        ? "Available only after no modeled debt can take the rolled payment."
        : "Available for other uses as modeled payments end.",
  };
}
