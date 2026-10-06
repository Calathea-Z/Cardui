"use client";

import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type {
  DebtDto,
  DebtSummaryItemDto,
  DebtSummaryReportDto,
} from "@/lib/api/types";
import {
  formatApr,
  formatCalendarDate,
  formatUtilization,
} from "./debtDisplay";
import {
  balanceComparisonCopy,
  debtFactLine,
  debtSummaryLines,
  debtInterestLine,
  twoBalancesNote,
  twoBalancesTitle,
  utilizationMark,
} from "./debtSummaryCopy";
import { debtKindLabel } from "./debtOptions";

type DebtListProps = {
  debts: DebtDto[];
  summary: DebtSummaryReportDto | null;
  busyId: string | null;
  onAdd: (opener: HTMLElement) => void;
  onEdit: (debt: DebtDto, opener: HTMLElement) => void;
  onRemove: (debt: DebtDto) => void;
  onUseAccountBalance: (debt: DebtDto) => void;
};

/**
 * Lists the household's debts.
 * A missing term says unknown. It is not shown as zero.
 */
export function DebtList({
  debts,
  summary,
  busyId,
  onAdd,
  onEdit,
  onRemove,
  onUseAccountBalance,
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
              summary={summary}
              busy={busyId === debt.id}
              onEdit={(opener) => onEdit(debt, opener)}
              onRemove={() => onRemove(debt)}
              onUseAccountBalance={() => onUseAccountBalance(debt)}
            />
          ))}
        </ul>
      )}
    </section>
  );
}

type DebtRowProps = {
  debt: DebtDto;
  summary: DebtSummaryReportDto | null;
  busy: boolean;
  onEdit: (opener: HTMLElement) => void;
  onRemove: () => void;
  onUseAccountBalance: () => void;
};

/**
 * One debt.
 * Edit opens it in the form. Remove deletes the row after confirmation.
 * Summary notes use the recorded balance. A different account balance is a choice.
 */
function DebtRow({
  debt,
  summary,
  busy,
  onEdit,
  onRemove,
  onUseAccountBalance,
}: DebtRowProps) {
  const item = summary?.debts.find((entry) => entry.debtId === debt.id) ?? null;
  const comparison = item?.balanceComparison
    ? balanceComparisonCopy(
        debt.balance,
        debt.balanceAsOf,
        debt.currency,
        item.balanceComparison,
        formatCurrency,
        formatCalendarDate,
      )
    : null;
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
        {comparison ? null : (
          <div className="shrink-0 text-right">
            {debt.balance === null ? (
              <p className="text-sm text-muted-foreground">Unknown</p>
            ) : (
              <>
                <p className="ledger-amount text-lg">
                  {formatCurrency(debt.balance, debt.currency)}
                </p>
                <p className="mt-0.5 text-xs text-muted-foreground">
                  {formatCalendarDate(debt.balanceAsOf ?? "")}
                </p>
              </>
            )}
          </div>
        )}
      </div>
      {comparison && item?.balanceComparison ? (
        <BalanceComparison
          comparison={comparison}
          canUse={item.balanceComparison.canUseAccountBalance}
          busy={busy}
          onUseAccountBalance={onUseAccountBalance}
        />
      ) : null}
      <p className="text-sm text-muted-foreground">{factLine(debt)}</p>
      <DebtTerms
        debt={debt}
        utilizationNote={
          summary && item
            ? utilizationMark(
                item.utilizationReachesNotice,
                item.utilizationReachesLimitNotice,
                summary,
              )
            : null
        }
      />
      {item ? <DebtSummaryNotes debt={debt} item={item} /> : null}
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
  utilizationNote: string | null;
};

/**
 * Shows the term that belongs to this type, and a promotion only when one was recorded.
 * A revolving debt without a limit says the limit is unknown. Utilization appears only when it can be calculated.
 */
function DebtTerms({ debt, utilizationNote }: DebtTermsProps) {
  const promotion = promotionText(debt);

  return (
    <div className="flex flex-col gap-1 text-sm text-muted-foreground">
      {debt.kind === "Revolving" ? (
        <p>
          {debt.creditLimit === null
            ? "Credit limit unknown"
            : `Limit ${formatCurrency(debt.creditLimit, debt.currency)}`}
          {debt.utilization === null
            ? ""
            : ` · ${formatUtilization(debt.utilization)}`}
          {utilizationNote ? ` · ${utilizationNote}` : ""}
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
 * One line of APR, minimum, and due date.
 * Blank terms are named together. A known value stays in the line.
 */
function factLine(debt: DebtDto) {
  const known: string[] = [];
  const missing: string[] = [];
  if (debt.apr === null) {
    missing.push("APR");
  } else {
    known.push(formatApr(debt.apr));
  }

  if (debt.minimumPayment === null) {
    missing.push("minimum");
  } else {
    known.push(`Min ${formatCurrency(debt.minimumPayment, debt.currency)}`);
  }

  if (debt.nextDueDate) {
    known.push(`Due ${formatCalendarDate(debt.nextDueDate)}`);
  } else {
    missing.push("due date");
  }

  return debtFactLine(known, missing);
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

type BalanceComparisonProps = {
  comparison: ReturnType<typeof balanceComparisonCopy>;
  canUse: boolean;
  busy: boolean;
  onUseAccountBalance: () => void;
};

/**
 * Shows the recorded balance and the linked account balance as a pair.
 * The recorded side is marked in use. The account side is marked not in use, with the choice beside it.
 */
function BalanceComparison({
  comparison,
  canUse,
  busy,
  onUseAccountBalance,
}: BalanceComparisonProps) {
  return (
    <div className="overflow-hidden rounded-md border border-border bg-muted/60">
      <div className="border-b border-border px-3 py-2.5">
        <p className="text-sm font-medium text-foreground">
          {twoBalancesTitle}
        </p>
        <p className="mt-0.5 text-xs text-muted-foreground">
          {twoBalancesNote(canUse)}
        </p>
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 sm:divide-x sm:divide-border">
        <div className="border-l-2 border-l-primary px-3 py-2.5">
          <p className="text-xs text-muted-foreground">
            Recorded · {comparison.recordedStatus}
          </p>
          <p className="ledger-amount mt-1 text-lg">
            {comparison.recordedAmount}
          </p>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {comparison.recordedAsOf ?? "Date unknown"}
          </p>
        </div>
        <div className="flex flex-col gap-2 border-t border-border px-3 py-2.5 sm:border-t-0">
          <div>
            <p className="text-xs text-muted-foreground">
              Account · {comparison.accountStatus}
            </p>
            <p className="ledger-amount mt-1 text-lg">
              {comparison.accountAmount}
            </p>
            <p className="mt-0.5 text-xs text-muted-foreground">
              {comparison.accountAsOf ?? "Date unknown"}
            </p>
          </div>
          {canUse ? (
            <Button
              type="button"
              variant="outline"
              className="min-h-11 bg-card"
              disabled={busy}
              onClick={onUseAccountBalance}
            >
              Use this balance
            </Button>
          ) : null}
        </div>
      </div>
    </div>
  );
}

type DebtSummaryNotesProps = {
  debt: DebtDto;
  item: DebtSummaryItemDto;
};

/**
 * The interest amount, a passed due date, and a promotion consequence.
 * An unknown interest line is omitted. A differing account balance is shown above this, not here.
 */
function DebtSummaryNotes({ debt, item }: DebtSummaryNotesProps) {
  const lines = debtSummaryLines(
    item,
    debt.apr === null ? null : formatApr(debt.apr),
    formatCalendarDate,
  );
  const interest = debtInterestLine(
    item.monthlyInterest,
    item.rateIsPromotional,
    debt.currency,
    formatCurrency,
  );

  if (!interest && lines.length === 0) {
    return null;
  }

  return (
    <div className="flex flex-col gap-1 text-sm text-muted-foreground">
      {interest ? (
        <p className="tabular-nums text-foreground">{interest}</p>
      ) : null}
      {lines.length > 0 ? (
        <ul className="flex flex-col gap-1">
          {lines.map((line) => (
            <li key={line}>{line}</li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
