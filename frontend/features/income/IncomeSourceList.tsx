"use client";

import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type { IncomeRaiseDto, IncomeSourceDto } from "@/lib/api/types";
import { formatPayDay, formatPaymentDate } from "./incomeSourceDisplay";
import { isRaiseDue } from "./incomeRaiseReview";
import { averageMonthlyAmount, upcomingPaymentDates } from "./paycheckSchedule";
import {
  incomeCadenceLabel,
  incomeReliabilityLabel,
} from "./incomeSourceOptions";

type IncomeSourceListProps = {
  sources: IncomeSourceDto[];
  today: string;
  busyId: string | null;
  onAdd: (opener: HTMLElement) => void;
  onEdit: (source: IncomeSourceDto, opener: HTMLElement) => void;
  onRemove: (source: IncomeSourceDto) => void;
  onConfirmRaise: (source: IncomeSourceDto, raise: IncomeRaiseDto) => void;
  onRemoveRaise: (source: IncomeSourceDto, raise: IncomeRaiseDto) => void;
};

/**
 * Lists the household's income sources.
 * The saved sources are the page. The card shows the next payday and the one after it, so the anchor and the gap are visible.
 * A monthly figure is an average across the year.
 * A raise whose date has arrived asks for a decision.
 */
export function IncomeSourceList({
  sources,
  today,
  busyId,
  onAdd,
  onEdit,
  onRemove,
  onConfirmRaise,
  onRemoveRaise,
}: IncomeSourceListProps) {
  return (
    <section className="flex flex-col gap-3">
      {sources.length === 0 ? (
        <EmptyState
          title="No income yet"
          description="Add the first place money comes in. Enter net pay for one payment."
          action={
            <Button
              type="button"
              className="min-h-11"
              onClick={(event) => onAdd(event.currentTarget)}
            >
              Add income
            </Button>
          }
        />
      ) : (
        <ul className="flex flex-col gap-3">
          {sources.map((source) => (
            <IncomeSourceRow
              key={source.id}
              source={source}
              today={today}
              busy={busyId === source.id}
              onEdit={(opener) => onEdit(source, opener)}
              onRemove={() => onRemove(source)}
              onConfirmRaise={(raise) => onConfirmRaise(source, raise)}
              onRemoveRaise={(raise) => onRemoveRaise(source, raise)}
            />
          ))}
        </ul>
      )}
    </section>
  );
}

type IncomeSourceRowProps = {
  source: IncomeSourceDto;
  today: string;
  busy: boolean;
  onEdit: (opener: HTMLElement) => void;
  onRemove: () => void;
  onConfirmRaise: (raise: IncomeRaiseDto) => void;
  onRemoveRaise: (raise: IncomeRaiseDto) => void;
};

/**
 * One income source.
 * Edit opens it in the form. Remove deletes the row after confirmation.
 * A raise whose date has arrived asks whether that pay is what they receive now.
 */
function IncomeSourceRow({
  source,
  today,
  busy,
  onEdit,
  onRemove,
  onConfirmRaise,
  onRemoveRaise,
}: IncomeSourceRowProps) {
  const dueRaises = source.raises.filter((raise) =>
    isRaiseDue(raise.effectiveDate, today),
  );
  const laterRaises = source.raises.filter(
    (raise) => !isRaiseDue(raise.effectiveDate, today),
  );
  const payDates = upcomingPaymentDates(
    source.cadence,
    source.nextPaymentDate,
    today,
  ).slice(0, 2);
  const monthlyAverage = averageMonthlyAmount(
    source.takeHomeAmount,
    source.cadence,
  );
  const scenarioLine = [
    typeof source.grossPayAmount === "number"
      ? `Gross ${formatCurrency(source.grossPayAmount, source.currency)}`
      : null,
    source.lowTakeHomeAmount !== null
      ? `Low ${formatCurrency(source.lowTakeHomeAmount, source.currency)}`
      : null,
    source.strongTakeHomeAmount !== null
      ? `Strong ${formatCurrency(source.strongTakeHomeAmount, source.currency)}`
      : null,
  ]
    .filter((part) => part !== null)
    .join(" · ");

  return (
    <li className="flex flex-col gap-3 rounded-lg border border-border/70 p-3">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate font-medium">{source.name}</p>
          <p className="mt-0.5 text-sm text-muted-foreground">
            {incomeCadenceLabel(source.cadence)}
            {" · "}
            {source.contributorName ?? "No contributor"}
            {" · "}
            {incomeReliabilityLabel(source.reliability)}
          </p>
        </div>
        <div className="shrink-0 text-right">
          <p className="ledger-amount text-foreground">
            {formatCurrency(source.takeHomeAmount, source.currency)}
          </p>
          <p className="mt-0.5 text-xs text-muted-foreground">
            net each payment
          </p>
        </div>
      </div>
      {scenarioLine ? (
        <p className="text-xs text-muted-foreground">{scenarioLine}</p>
      ) : null}
      {laterRaises.map((raise) => (
        <p key={raise.id} className="text-xs text-muted-foreground">
          {formatCurrency(raise.takeHomeAmount, source.currency)} from{" "}
          {formatPaymentDate(raise.effectiveDate)}
        </p>
      ))}
      {payDates.length > 0 ? (
        <div className="flex flex-col gap-1.5">
          <p className="text-xs font-medium text-foreground">
            Upcoming pay dates
          </p>
          <ul className="flex flex-wrap gap-1.5">
            {payDates.map((date) => (
              <li
                key={date}
                className="rounded-md border border-border bg-card px-2 py-1 text-sm tabular-nums text-foreground"
              >
                {formatPayDay(date, today)}
              </li>
            ))}
          </ul>
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">
          Next payment {formatPaymentDate(source.nextPaymentDate)}
        </p>
      )}
      {dueRaises.map((raise) => (
        <div
          key={raise.id}
          className="flex flex-col gap-3 rounded-lg border border-border bg-muted/40 p-3"
        >
          <p className="text-sm">
            An expected raise of{" "}
            {formatCurrency(raise.takeHomeAmount, source.currency)} was dated{" "}
            {formatPaymentDate(raise.effectiveDate)}. Is this the pay you
            receive now?
          </p>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              disabled={busy}
              onClick={() => onConfirmRaise(raise)}
            >
              Update typical pay
            </Button>
            <Button
              type="button"
              variant="outline"
              disabled={busy}
              onClick={() => onRemoveRaise(raise)}
            >
              Remove raise
            </Button>
          </div>
        </div>
      ))}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        {monthlyAverage !== null ? (
          <p className="text-xs text-muted-foreground">
            Monthly average {formatCurrency(monthlyAverage, source.currency)}
          </p>
        ) : null}
        <div className="flex gap-2 sm:ml-auto">
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            onClick={(event) => onEdit(event.currentTarget)}
          >
            Edit
          </Button>
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            disabled={busy}
            onClick={onRemove}
          >
            Remove
          </Button>
        </div>
      </div>
    </li>
  );
}
