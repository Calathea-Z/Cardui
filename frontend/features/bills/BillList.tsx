"use client";

import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type { ObligationDto } from "@/lib/api/types";
import { formatDueDate } from "./billDisplay";
import {
  obligationCadenceLabel,
  obligationFlexibilityLabel,
} from "./billOptions";

type BillListProps = {
  obligations: ObligationDto[];
  hasSuggestions: boolean;
  busyId: string | null;
  onAdd: (opener: HTMLElement) => void;
  onEdit: (obligation: ObligationDto, opener: HTMLElement) => void;
  onRemove: (obligation: ObligationDto) => void;
};

/**
 * Lists the household's bills.
 * The saved bills are the page. Each amount is one payment.
 */
export function BillList({
  obligations,
  hasSuggestions,
  busyId,
  onAdd,
  onEdit,
  onRemove,
}: BillListProps) {
  return (
    <section className="flex flex-col gap-3">
      {hasSuggestions && obligations.length > 0 ? (
        <h2 className="app-section-title">Saved bills</h2>
      ) : null}
      {obligations.length === 0 ? (
        <EmptyState
          title="No bills yet"
          description={
            hasSuggestions
              ? "None of the suggestions above are bills until you add them."
              : "Add a bill or other payment you make on a schedule. Enter the amount of one payment."
          }
          action={
            <Button
              type="button"
              className="min-h-11"
              onClick={(event) => onAdd(event.currentTarget)}
            >
              Add bill
            </Button>
          }
        />
      ) : (
        <ul className="flex flex-col gap-3">
          {obligations.map((obligation) => (
            <BillRow
              key={obligation.id}
              obligation={obligation}
              busy={busyId === obligation.id}
              onEdit={(opener) => onEdit(obligation, opener)}
              onRemove={() => onRemove(obligation)}
            />
          ))}
        </ul>
      )}
    </section>
  );
}

type BillRowProps = {
  obligation: ObligationDto;
  busy: boolean;
  onEdit: (opener: HTMLElement) => void;
  onRemove: () => void;
};

/**
 * One bill.
 * Edit opens it in the form. Remove deletes the row after confirmation.
 */
function BillRow({ obligation, busy, onEdit, onRemove }: BillRowProps) {
  return (
    <li className="flex flex-col gap-3 rounded-lg border border-border/70 p-3">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate font-medium">{obligation.name}</p>
          <p className="mt-0.5 text-sm text-muted-foreground">
            {obligationCadenceLabel(obligation.cadence)}
            {" · "}
            {obligationFlexibilityLabel(obligation.flexibility)}
            {" · "}
            {obligation.accountName ?? "No account"}
          </p>
        </div>
        <div className="shrink-0 text-right">
          <p className="text-foreground tabular-nums">
            {formatCurrency(obligation.amount, obligation.currency)}
          </p>
          <p className="mt-0.5 text-xs text-muted-foreground">each payment</p>
        </div>
      </div>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm text-muted-foreground">
          Next due {formatDueDate(obligation.nextDueDate)}
        </p>
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
