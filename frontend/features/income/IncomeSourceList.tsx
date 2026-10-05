"use client";

import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type { IncomeRaiseDto, IncomeSourceDto } from "@/lib/api/types";
import { formatPaymentDate } from "./incomeSourceDisplay";
import { isRaiseDue } from "./incomeRaiseReview";
import {
  incomeCadenceLabel,
  incomeReliabilityLabel,
} from "./incomeSourceOptions";

type IncomeSourceListProps = {
  sources: IncomeSourceDto[];
  today: string;
  busyId: string | null;
  onEdit: (source: IncomeSourceDto) => void;
  onRemove: (source: IncomeSourceDto) => void;
  onConfirmRaise: (source: IncomeSourceDto, raise: IncomeRaiseDto) => void;
  onRemoveRaise: (source: IncomeSourceDto, raise: IncomeRaiseDto) => void;
};

/**
 * Lists the household's income sources.
 * Each amount is net pay for one payment. A raise whose date has arrived asks for a decision.
 */
export function IncomeSourceList({
  sources,
  today,
  busyId,
  onEdit,
  onRemove,
  onConfirmRaise,
  onRemoveRaise,
}: IncomeSourceListProps) {
  return (
    <section className="flex flex-col gap-3">
      <h2 className="app-section-title">Income sources</h2>
      {sources.length === 0 ? (
        <EmptyState
          title="No income sources yet"
          description="Add each place money comes in. Enter the typical net pay for one payment, after taxes and deductions."
        />
      ) : (
        <ul className="flex flex-col gap-3">
          {sources.map((source) => (
            <IncomeSourceRow
              key={source.id}
              source={source}
              today={today}
              busy={busyId === source.id}
              onEdit={() => onEdit(source)}
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
  onEdit: () => void;
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

  return (
    <li className="flex flex-col gap-3 rounded-lg border border-border/70 p-3">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="font-medium">{source.name}</p>
          <p className="text-sm text-muted-foreground">
            {formatCurrency(source.takeHomeAmount, source.currency)} typical net
            each payment
            {" · "}
            {incomeCadenceLabel(source.cadence)}
          </p>
          {source.lowTakeHomeAmount !== null ||
          source.strongTakeHomeAmount !== null ? (
            <p className="text-sm text-muted-foreground">
              {source.lowTakeHomeAmount !== null
                ? `Low ${formatCurrency(source.lowTakeHomeAmount, source.currency)}`
                : null}
              {source.lowTakeHomeAmount !== null &&
              source.strongTakeHomeAmount !== null
                ? " · "
                : null}
              {source.strongTakeHomeAmount !== null
                ? `Strong ${formatCurrency(source.strongTakeHomeAmount, source.currency)}`
                : null}
            </p>
          ) : null}
          {laterRaises.map((raise) => (
            <p key={raise.id} className="text-sm text-muted-foreground">
              From {formatPaymentDate(raise.effectiveDate)},{" "}
              {formatCurrency(raise.takeHomeAmount, source.currency)} typical
              net each payment
            </p>
          ))}
          <p className="text-sm text-muted-foreground">
            Next payment {formatPaymentDate(source.nextPaymentDate)}
            {" · "}
            {source.contributorName ?? "No contributor"}
            {" · "}
            {incomeReliabilityLabel(source.reliability)}
          </p>
        </div>
        <div className="flex gap-2">
          <Button type="button" variant="outline" onClick={onEdit}>
            Edit
          </Button>
          <Button
            type="button"
            variant="ghost"
            disabled={busy}
            onClick={onRemove}
          >
            Remove
          </Button>
        </div>
      </div>
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
    </li>
  );
}
