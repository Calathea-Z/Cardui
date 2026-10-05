"use client";

import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type { IncomeSourceDto } from "@/lib/api/types";
import { formatPaymentDate } from "./incomeSourceDisplay";
import {
  incomeCadenceLabel,
  incomeReliabilityLabel,
} from "./incomeSourceOptions";

type IncomeSourceListProps = {
  sources: IncomeSourceDto[];
  busyId: string | null;
  onEdit: (source: IncomeSourceDto) => void;
  onRemove: (source: IncomeSourceDto) => void;
};

/**
 * Lists the household's income sources.
 * Each amount is net pay for one payment.
 */
export function IncomeSourceList({
  sources,
  busyId,
  onEdit,
  onRemove,
}: IncomeSourceListProps) {
  return (
    <section className="flex flex-col gap-3">
      <h2 className="app-section-title">Income sources</h2>
      {sources.length === 0 ? (
        <EmptyState
          title="No income sources yet"
          description="Add each place money comes in. Enter net pay for one payment, after taxes and deductions."
        />
      ) : (
        <ul className="flex flex-col gap-3">
          {sources.map((source) => (
            <IncomeSourceRow
              key={source.id}
              source={source}
              busy={busyId === source.id}
              onEdit={() => onEdit(source)}
              onRemove={() => onRemove(source)}
            />
          ))}
        </ul>
      )}
    </section>
  );
}

type IncomeSourceRowProps = {
  source: IncomeSourceDto;
  busy: boolean;
  onEdit: () => void;
  onRemove: () => void;
};

/**
 * One income source.
 * Edit opens it in the form. Remove deletes the row after confirmation.
 */
function IncomeSourceRow({
  source,
  busy,
  onEdit,
  onRemove,
}: IncomeSourceRowProps) {
  return (
    <li className="flex flex-col gap-3 rounded-lg border border-border/70 p-3 sm:flex-row sm:items-start sm:justify-between">
      <div>
        <p className="font-medium">{source.name}</p>
        <p className="text-sm text-muted-foreground">
          {formatCurrency(source.takeHomeAmount, source.currency)} net each
          payment
          {" · "}
          {incomeCadenceLabel(source.cadence)}
        </p>
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
    </li>
  );
}
