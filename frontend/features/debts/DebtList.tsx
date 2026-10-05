"use client";

import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type { DebtDto } from "@/lib/api/types";
import {
  formatApr,
  formatCalendarDate,
  formatUtilization,
} from "./debtDisplay";
import { debtKindLabel } from "./debtOptions";

type DebtListProps = {
  debts: DebtDto[];
  busyId: string | null;
  onAdd: (opener: HTMLElement) => void;
  onEdit: (debt: DebtDto, opener: HTMLElement) => void;
  onRemove: (debt: DebtDto) => void;
};

/**
 * Lists the household's debts.
 * A missing term says unknown. It is not shown as zero.
 */
export function DebtList({
  debts,
  busyId,
  onAdd,
  onEdit,
  onRemove,
}: DebtListProps) {
  return (
    <section className="flex flex-col gap-3">
      {debts.length === 0 ? (
        <EmptyState
          title="No debts yet"
          description="Add a card or loan with the balance you know. Leave the APR, minimum, or due date blank if you don't know them."
          action={
            <Button
              type="button"
              className="min-h-11"
              onClick={(event) => onAdd(event.currentTarget)}
            >
              Add debt
            </Button>
          }
        />
      ) : (
        <ul className="flex flex-col gap-3">
          {debts.map((debt) => (
            <DebtRow
              key={debt.id}
              debt={debt}
              busy={busyId === debt.id}
              onEdit={(opener) => onEdit(debt, opener)}
              onRemove={() => onRemove(debt)}
            />
          ))}
        </ul>
      )}
    </section>
  );
}

type DebtRowProps = {
  debt: DebtDto;
  busy: boolean;
  onEdit: (opener: HTMLElement) => void;
  onRemove: () => void;
};

/**
 * One debt.
 * Edit opens it in the form. Remove deletes the row after confirmation.
 */
function DebtRow({ debt, busy, onEdit, onRemove }: DebtRowProps) {
  return (
    <li className="flex flex-col gap-3 rounded-lg border border-border/70 p-3">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate font-medium">{debt.name}</p>
          <p className="mt-0.5 text-sm text-muted-foreground">
            {debtKindLabel(debt.kind)}
            {" · "}
            {debt.accountName ?? "No account"}
          </p>
        </div>
        <div className="shrink-0 text-right">
          {debt.balance === null ? (
            <p className="text-sm text-muted-foreground">Balance unknown</p>
          ) : (
            <>
              <p className="text-foreground tabular-nums">
                {formatCurrency(debt.balance, debt.currency)}
              </p>
              <p className="mt-0.5 text-xs text-muted-foreground">
                as of {formatCalendarDate(debt.balanceAsOf ?? "")}
              </p>
            </>
          )}
        </div>
      </div>
      <p className="text-sm text-muted-foreground">
        {debt.apr === null ? "APR unknown" : `APR ${formatApr(debt.apr)}`}
        {" · "}
        {debt.minimumPayment === null
          ? "Minimum unknown"
          : `Minimum ${formatCurrency(debt.minimumPayment, debt.currency)}`}
        {" · "}
        {debt.nextDueDate
          ? `Due ${formatCalendarDate(debt.nextDueDate)}`
          : "Due date unknown"}
      </p>
      <DebtTerms debt={debt} />
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
    </li>
  );
}

type DebtTermsProps = {
  debt: DebtDto;
};

/**
 * Shows the term that belongs to this type, and a promotion only when one was recorded.
 * A revolving debt without a limit says the limit is unknown. Utilization appears only when it can be calculated.
 */
function DebtTerms({ debt }: DebtTermsProps) {
  const promotion = promotionText(debt);

  return (
    <div className="flex flex-col gap-1 text-sm text-muted-foreground">
      {debt.kind === "Revolving" ? (
        <p>
          {debt.creditLimit === null
            ? "Credit limit unknown"
            : `Credit limit ${formatCurrency(debt.creditLimit, debt.currency)}`}
          {debt.utilization === null
            ? ""
            : ` · ${formatUtilization(debt.utilization)}`}
        </p>
      ) : (
        <p>
          {debt.remainingTermMonths === null
            ? "Term unknown"
            : debt.remainingTermMonths === 1
              ? "1 month left"
              : `${debt.remainingTermMonths} months left`}
        </p>
      )}
      {promotion ? <p>{promotion}</p> : null}
    </div>
  );
}

/**
 * Describes a recorded promotion.
 * Both fields blank means nothing was recorded, so the line is omitted.
 */
function promotionText(debt: DebtDto) {
  if (debt.promotionalApr === null && !debt.promotionalEndsOn) {
    return null;
  }

  if (debt.promotionalApr !== null && debt.promotionalEndsOn) {
    return `Promo ${formatApr(debt.promotionalApr)} until ${formatCalendarDate(debt.promotionalEndsOn)}`;
  }

  if (debt.promotionalApr !== null) {
    return `Promo ${formatApr(debt.promotionalApr)}, end date unknown`;
  }

  return `Promo rate unknown, ends ${formatCalendarDate(debt.promotionalEndsOn ?? "")}`;
}
