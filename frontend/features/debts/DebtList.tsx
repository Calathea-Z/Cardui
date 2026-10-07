"use client";

import Link from "next/link";
import { CircleAlert, CircleCheck, LoaderCircle } from "lucide-react";
import { Button, buttonVariants } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { cn } from "@/lib/utils";
import type {
  AccountDto,
  DebtDto,
  DebtSummaryItemDto,
  DebtSummaryReportDto,
} from "@/lib/api/types";
import {
  creditNote,
  freshnessLine,
  followBlockCopy,
  isFollowCandidate,
} from "./debtFollowCopy";
import {
  formatApr,
  formatCalendarDate,
  formatUtilization,
} from "./debtDisplay";
import {
  balanceComparisonCopy,
  debtSummaryLines,
  debtInterestLine,
  twoBalancesNote,
  twoBalancesTitle,
  utilizationMark,
} from "./debtSummaryCopy";
import { debtKindLabel } from "./debtOptions";

type DebtListProps = {
  debts: DebtDto[];
  accounts: AccountDto[];
  summary: DebtSummaryReportDto | null;
  busyId: string | null;
  onAdd: (opener: HTMLElement) => void;
  onEdit: (debt: DebtDto, opener: HTMLElement) => void;
  onRemove: (debt: DebtDto) => void;
  onUseAccountBalance: (debt: DebtDto, startsFollow: boolean) => void;
  onFollow: (debt: DebtDto, opener: HTMLElement) => void;
  onStopFollowing: (debt: DebtDto) => void;
  onRefresh: (debt: DebtDto) => void;
};

/**
 * Lists the household's debts.
 * A missing term says unknown. It is not shown as zero.
 */
export function DebtList({
  debts,
  accounts,
  summary,
  busyId,
  onAdd,
  onEdit,
  onRemove,
  onUseAccountBalance,
  onFollow,
  onStopFollowing,
  onRefresh,
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
              accounts={accounts}
              followedAccountIds={followedAccountIds(debts, debt.id)}
              summary={summary}
              busy={busyId === debt.id}
              onEdit={(opener) => onEdit(debt, opener)}
              onRemove={() => onRemove(debt)}
              onUseAccountBalance={(startsFollow) =>
                onUseAccountBalance(debt, startsFollow)
              }
              onFollow={(opener) => onFollow(debt, opener)}
              onStopFollowing={() => onStopFollowing(debt)}
              onRefresh={() => onRefresh(debt)}
            />
          ))}
        </ul>
      )}
    </section>
  );
}

type DebtRowProps = {
  debt: DebtDto;
  accounts: AccountDto[];
  followedAccountIds: Set<string>;
  summary: DebtSummaryReportDto | null;
  busy: boolean;
  onEdit: (opener: HTMLElement) => void;
  onRemove: () => void;
  onUseAccountBalance: (startsFollow: boolean) => void;
  onFollow: (opener: HTMLElement) => void;
  onStopFollowing: () => void;
  onRefresh: () => void;
};

/**
 * Account ids already followed by another debt.
 * The debt being shown can still follow its own reference link.
 */
function followedAccountIds(debts: DebtDto[], exceptId: string) {
  return new Set(
    debts
      .filter(
        (debt) => debt.following && debt.id !== exceptId && debt.accountId,
      )
      .map((debt) => debt.accountId as string),
  );
}

/**
 * One debt.
 * Edit opens it in the form. Remove deletes the row after confirmation.
 * Summary notes use the recorded balance. A different account balance is a choice.
 */
function DebtRow({
  debt,
  accounts,
  followedAccountIds,
  summary,
  busy,
  onEdit,
  onRemove,
  onUseAccountBalance,
  onFollow,
  onStopFollowing,
  onRefresh,
}: DebtRowProps) {
  const item = summary?.debts.find((entry) => entry.debtId === debt.id) ?? null;
  const linked = accounts.find((account) => account.id === debt.accountId);
  const startsFollow = linked
    ? isFollowCandidate(
        linked,
        debt.currency,
        followedAccountIds.has(linked.id),
      )
    : false;
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
    <li className="flex flex-col gap-4 rounded-lg border border-border/70 bg-card p-4">
      <div className="flex items-start justify-between gap-4">
        <div className="min-w-0 pt-0.5">
          <p className="truncate text-base font-medium">{debt.name}</p>
          <p className="mt-1 text-xs text-muted-foreground">
            {debtContext(debt)}
          </p>
        </div>
        {comparison ? null : <BalanceInUse debt={debt} />}
      </div>
      {debt.following && debt.freshness && debt.freshness !== "Current" ? (
        <FollowStatus
          debt={debt}
          accounts={accounts}
          busy={busy}
          onRefresh={onRefresh}
        />
      ) : null}
      {comparison && item?.balanceComparison ? (
        <BalanceComparison
          comparison={comparison}
          canUse={item.balanceComparison.canUseAccountBalance}
          startsFollow={startsFollow}
          busy={busy}
          onUseAccountBalance={() => onUseAccountBalance(startsFollow)}
        />
      ) : null}
      <DebtFacts
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
      <div className="flex flex-wrap gap-2 border-t border-border/70 pt-3 sm:justify-end">
        {debt.following ? (
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            disabled={busy}
            onClick={onStopFollowing}
          >
            Stop following
          </Button>
        ) : (
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            disabled={busy}
            onClick={(event) => onFollow(event.currentTarget)}
          >
            {followLabel(debt, accounts, followedAccountIds)}
          </Button>
        )}
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
 * The kind of debt, then how it relates to an account.
 * A follow whose account name matches the debt says Following, so the name is not repeated.
 */
function debtContext(debt: DebtDto) {
  const kind = debtKindLabel(debt.kind);
  if (!debt.following) {
    return `${kind} · ${debt.accountName ?? "No account"}`;
  }

  const account = debt.accountName?.trim();
  const sameName =
    account !== undefined &&
    account.localeCompare(debt.name.trim(), undefined, {
      sensitivity: "accent",
    }) === 0;
  return sameName
    ? `${kind} · Following`
    : `${kind} · Follows ${account || "a connected account"}`;
}

/**
 * The terms a person scans on one debt.
 * A label is quiet. A known value is the text they read. A blank term says Unknown.
 */
function DebtFacts({ debt, utilizationNote }: DebtTermsProps) {
  const promotion = promotionText(debt);
  const facts: DebtFact[] = [
    {
      label: "APR",
      value: debt.apr === null ? null : formatApr(debt.apr),
    },
    {
      label: "Minimum",
      value:
        debt.minimumPayment === null
          ? null
          : formatCurrency(debt.minimumPayment, debt.currency),
    },
    {
      label: "Due",
      value: debt.nextDueDate ? formatCalendarDate(debt.nextDueDate) : null,
    },
    debt.kind === "Revolving"
      ? limitFact(debt, utilizationNote)
      : termFact(debt),
  ];

  return (
    <div className="flex flex-col gap-3">
      <dl className="grid grid-cols-2 gap-x-4 gap-y-3 sm:grid-cols-4">
        {facts.map((fact) => (
          <Fact key={fact.label} fact={fact} />
        ))}
      </dl>
      {promotion ? (
        <p className="text-sm text-foreground">{promotion}</p>
      ) : null}
    </div>
  );
}

type DebtFact = {
  label: string;
  value: string | null;
  note?: string | null;
};

/**
 * The credit limit, with the share in use on the line under it.
 * A missing limit stays unknown. The share is not packed onto the amount.
 */
function limitFact(debt: DebtDto, utilizationNote: string | null): DebtFact {
  if (debt.creditLimit === null) {
    return { label: "Limit", value: null, note: utilizationNote };
  }

  const share =
    debt.utilization === null ? null : formatUtilization(debt.utilization);
  const note = [share, utilizationNote].filter((part) => part).join(" · ");
  return {
    label: "Limit",
    value: formatCurrency(debt.creditLimit, debt.currency),
    note: note || null,
  };
}

/**
 * The months left on an installment debt.
 * A blank term stays unknown.
 */
function termFact(debt: DebtDto): DebtFact {
  if (debt.remainingTermMonths === null) {
    return { label: "Term", value: null, note: null };
  }

  const months =
    debt.remainingTermMonths === 1
      ? "1 month"
      : `${debt.remainingTermMonths} months`;
  return { label: "Term", value: months, note: null };
}

/**
 * One labeled term.
 * Unknown is quieter than a known amount, so the eye stops on the values.
 */
function Fact({ fact }: { fact: DebtFact }) {
  return (
    <div className="min-w-0">
      <dt className="text-xs text-muted-foreground">{fact.label}</dt>
      <dd
        className={cn(
          "mt-0.5 text-sm tabular-nums",
          fact.value ? "font-medium text-foreground" : "text-muted-foreground",
        )}
      >
        {fact.value ?? "Unknown"}
      </dd>
      {fact.note ? (
        <p className="mt-0.5 text-xs text-muted-foreground">{fact.note}</p>
      ) : null}
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

type BalanceComparisonProps = {
  comparison: ReturnType<typeof balanceComparisonCopy>;
  canUse: boolean;
  startsFollow: boolean;
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
  startsFollow,
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
          {twoBalancesNote(canUse, startsFollow)}
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
    <div className="flex flex-col gap-1 text-sm text-foreground">
      {interest ? <p className="tabular-nums">{interest}</p> : null}
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

/**
 * The balance the plan uses, with a short source label when the debt is following.
 * A missing amount says unknown.
 */
function BalanceInUse({ debt }: { debt: DebtDto }) {
  const amount = debt.balanceInUse;
  const credit =
    debt.balanceSource === "Synced" && debt.balanceCredit !== null
      ? creditNote(debt.balanceCredit, debt.currency, formatCurrency)
      : null;
  const block = debt.following
    ? followBlockCopy(debt.syncedBalanceBlock)
    : null;
  return (
    <div className="shrink-0 text-right">
      {amount === null ? (
        <p className="text-sm text-muted-foreground">Unknown</p>
      ) : (
        <>
          <p className="ledger-amount text-xl">
            {formatCurrency(amount, debt.currency)}
          </p>
          <p
            className={cn(
              "mt-1 flex items-center justify-end gap-1 text-xs",
              isCurrentSync(debt) ? "text-success" : "text-muted-foreground",
            )}
          >
            <span>{balanceCaption(debt)}</span>
            {isCurrentSync(debt) ? (
              <CircleCheck className="size-3.5 shrink-0" aria-hidden="true" />
            ) : null}
          </p>
        </>
      )}
      {debt.following &&
      debt.balanceSource === "Override" &&
      debt.syncedBalance !== null ? (
        <p className="mt-0.5 text-xs text-muted-foreground">
          {`Synced ${formatCurrency(debt.syncedBalance, debt.currency)}`}
        </p>
      ) : null}
      {credit ? (
        <p className="mt-0.5 max-w-48 text-xs text-muted-foreground">
          {credit}
        </p>
      ) : null}
      {block ? (
        <p className="mt-0.5 max-w-48 text-xs text-muted-foreground">{block}</p>
      ) : null}
    </div>
  );
}

type FollowStatusProps = {
  debt: DebtDto;
  accounts: AccountDto[];
  busy: boolean;
  onRefresh: () => void;
};

/**
 * True when the followed balance is the bank's latest successful value.
 * A stale or overridden balance stays unmarked.
 */
function isCurrentSync(debt: DebtDto) {
  return (
    debt.following &&
    debt.balanceSource === "Synced" &&
    debt.freshness === "Current"
  );
}

/**
 * The date under a balance, with the source when the debt is following.
 * A current follow does not repeat that date in a second sentence.
 */
function balanceCaption(debt: DebtDto) {
  const date = debt.balanceInUseAsOf
    ? formatCalendarDate(debt.balanceInUseAsOf)
    : null;
  if (!debt.following) {
    return date ?? "Date unknown";
  }

  if (debt.balanceSource === "Override") {
    return date ? `Your value · ${date}` : "Your value";
  }

  if (debt.balanceSource === "Synced") {
    return date ? `Synced · ${date}` : "Synced";
  }

  return date ?? "Date unknown";
}

/**
 * How current a followed balance is, and the action for that state.
 * A current balance is already named under the amount. Refresh pulls the bank again.
 */
function FollowStatus({ debt, accounts, busy, onRefresh }: FollowStatusProps) {
  if (!debt.freshness || debt.freshness === "Current") {
    return null;
  }

  const account = accounts.find((item) => item.id === debt.accountId) ?? null;
  const line = freshnessLine(
    debt.freshness,
    debt.syncedBalanceAsOf,
    debt.syncFailedOn,
    formatCalendarDate,
  );
  const canRefresh =
    debt.freshness === "Stale" && Boolean(account?.plaidItemId);
  const canReconnect =
    debt.freshness === "SyncFailing" || debt.freshness === "Disconnected";
  return (
    <div className="flex flex-col gap-2">
      <p className="flex items-start gap-2 text-sm text-foreground">
        <CircleAlert className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
        <span>{line}</span>
      </p>
      {canRefresh || canReconnect ? (
        <div className="flex flex-wrap gap-2">
          {canRefresh ? (
            <Button
              type="button"
              variant="outline"
              className="min-h-11"
              disabled={busy}
              onClick={onRefresh}
            >
              {busy ? (
                <LoaderCircle
                  className="size-4 animate-spin"
                  aria-hidden="true"
                />
              ) : null}
              Refresh
            </Button>
          ) : null}
          {canReconnect ? (
            <Link
              href="/connections"
              className={cn(buttonVariants({ variant: "outline" }), "min-h-11")}
            >
              Reconnect
            </Link>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}

/**
 * The follow action label.
 * An eligible account the debt already names skips the wording for a new search.
 */
function followLabel(
  debt: DebtDto,
  accounts: AccountDto[],
  followedAccountIds: Set<string>,
) {
  const linked = accounts.find((account) => account.id === debt.accountId);
  if (
    linked &&
    isFollowCandidate(linked, debt.currency, followedAccountIds.has(linked.id))
  ) {
    return "Follow this account";
  }

  return "Follow a connected account";
}
