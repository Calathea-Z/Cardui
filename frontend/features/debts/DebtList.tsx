"use client";

import { useState } from "react";
import Link from "next/link";
import { CircleAlert, CircleCheck, LoaderCircle } from "lucide-react";
import { Alert } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
  canUpdateBalance,
  creditNote,
  freshnessLine,
  repairConnectionId,
  creditLimitSourceText,
  followBlockCopy,
  isFollowCandidate,
} from "./debtFollowCopy";
import {
  formatApr,
  formatCalendarDate,
  formatUtilization,
  utilizationFill,
} from "./debtDisplay";
import {
  balanceComparisonCopy,
  debtSummaryLines,
  debtInterestLine,
  twoBalancesNote,
  twoBalancesTitle,
  utilizationNotice,
  type UtilizationNotice,
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
  onReconnect: (debt: DebtDto) => void;
  reconnectingItemId: string | null;
  onUseSyncedValue: (debt: DebtDto) => void;
  onUseSyncedLimit: (debt: DebtDto) => void;
  onUpdateBalance: (debt: DebtDto, amount: string) => Promise<boolean>;
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
  onReconnect,
  reconnectingItemId,
  onUseSyncedValue,
  onUseSyncedLimit,
  onUpdateBalance,
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
              onReconnect={() => onReconnect(debt)}
              reconnectingItemId={reconnectingItemId}
              onUseSyncedValue={() => onUseSyncedValue(debt)}
              onUseSyncedLimit={() => onUseSyncedLimit(debt)}
              onUpdateBalance={(amount) => onUpdateBalance(debt, amount)}
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
  onReconnect: () => void;
  reconnectingItemId: string | null;
  onUseSyncedValue: () => void;
  onUseSyncedLimit: () => void;
  onUpdateBalance: (amount: string) => Promise<boolean>;
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
  onReconnect,
  reconnectingItemId,
  onUseSyncedValue,
  onUseSyncedLimit,
  onUpdateBalance,
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
        {comparison ? null : (
          <BalanceInUse
            debt={debt}
            busy={busy}
            onUseSyncedValue={onUseSyncedValue}
          />
        )}
      </div>
      {debt.following && debt.freshness && debt.freshness !== "Current" ? (
        <FollowStatus
          debt={debt}
          accounts={accounts}
          busy={busy}
          onRefresh={onRefresh}
          onReconnect={onReconnect}
          reconnecting={
            reconnectingItemId !== null &&
            accounts.some(
              (account) =>
                account.id === debt.accountId &&
                account.plaidItemId === reconnectingItemId,
            )
          }
          onUpdateBalance={onUpdateBalance}
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
      <CreditUse
        debt={debt}
        balanceShowsSync={balanceShowsSync(debt, comparison !== null)}
        busy={busy}
        utilizationNotice={
          summary && item
            ? utilizationNotice(
                item.utilizationReachesNotice,
                item.utilizationReachesLimitNotice,
                summary,
              )
            : null
        }
        onUseSyncedLimit={onUseSyncedLimit}
      />
      <DebtFacts debt={debt} />
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

type CreditUseProps = {
  debt: DebtDto;
  balanceShowsSync: boolean;
  busy: boolean;
  utilizationNotice: UtilizationNotice | null;
  onUseSyncedLimit: () => void;
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
 * How full the credit limit is, then the limit that scale uses.
 * A revolving debt with no limit in use stays in the terms row as unknown.
 */
function CreditUse({
  debt,
  balanceShowsSync,
  busy,
  utilizationNotice,
  onUseSyncedLimit,
}: CreditUseProps) {
  if (debt.kind !== "Revolving" || debt.creditLimitInUse === null) {
    return null;
  }

  const share =
    debt.utilization === null ? null : formatUtilization(debt.utilization);
  const fill =
    debt.utilization === null ? 0 : utilizationFill(debt.utilization);
  const showSynced =
    debt.creditLimitSource === "Override" && debt.syncedCreditLimit !== null;
  return (
    <div className="flex flex-col gap-3">
      {share && debt.utilization !== null ? (
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between sm:gap-8">
          <div className="min-w-0 flex-1">
            <p className="text-base font-medium text-foreground tabular-nums">
              {share}
            </p>
            <div
              className="mt-2 h-2 overflow-hidden rounded-full bg-muted"
              role="progressbar"
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={Math.round(fill)}
              aria-valuetext={share}
              aria-label="Credit used"
            >
              <div
                className={cn(
                  "h-full rounded-full",
                  utilizationNotice?.level === "high"
                    ? "bg-destructive"
                    : "bg-foreground",
                )}
                style={{ width: `${fill}%` }}
              />
            </div>
          </div>
          <LimitAmount
            amount={debt.creditLimitInUse}
            debt={debt}
            balanceShowsSync={balanceShowsSync}
            alignEnd
          />
        </div>
      ) : (
        <LimitAmount
          amount={debt.creditLimitInUse}
          debt={debt}
          balanceShowsSync={balanceShowsSync}
        />
      )}
      {utilizationNotice ? (
        <p
          className={cn(
            "flex items-start gap-2 text-sm font-medium",
            utilizationNotice.level === "high"
              ? "text-destructive"
              : "text-foreground",
          )}
        >
          <CircleAlert className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
          <span>{utilizationNotice.text}</span>
        </p>
      ) : null}
      {showSynced ? (
        <div className="flex flex-wrap items-center justify-between gap-2">
          <p className="text-sm text-muted-foreground">
            {`Synced limit ${formatCurrency(debt.syncedCreditLimit, debt.currency)}`}
          </p>
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            disabled={busy}
            onClick={onUseSyncedLimit}
          >
            Use synced limit
          </Button>
        </div>
      ) : null}
    </div>
  );
}

/**
 * The credit limit in use.
 * The sync date is omitted when the balance row already shows it. A limit the person kept still names that date.
 */
function LimitAmount({
  amount,
  debt,
  balanceShowsSync,
  alignEnd = false,
}: {
  amount: number;
  debt: DebtDto;
  balanceShowsSync: boolean;
  alignEnd?: boolean;
}) {
  const caption =
    balanceShowsSync && debt.creditLimitSource === "Synced"
      ? null
      : creditLimitSourceText(
          debt.creditLimitSource,
          debt.syncedCreditLimitAsOf,
          debt.creditLimitOverriddenOn,
          formatCalendarDate,
        );
  const current = limitIsCurrent(debt);
  return (
    <div className={cn("shrink-0", alignEnd && "sm:text-right")}>
      <p className="text-xs text-muted-foreground">Limit</p>
      <p className="ledger-amount mt-0.5 text-lg">
        {formatCurrency(amount, debt.currency)}
      </p>
      {caption ? (
        <p
          className={cn(
            "mt-1 flex items-center gap-1 text-xs",
            alignEnd && "sm:justify-end",
            current ? "text-success" : "text-muted-foreground",
          )}
        >
          <span>{caption}</span>
          {current ? (
            <CircleCheck className="size-3.5 shrink-0" aria-hidden="true" />
          ) : null}
        </p>
      ) : null}
    </div>
  );
}

/**
 * True when the balance row already shows the sync date.
 * The limit then omits that same line.
 */
function balanceShowsSync(debt: DebtDto, comparisonShown: boolean) {
  return (
    !comparisonShown &&
    debt.balanceInUse !== null &&
    debt.following &&
    debt.balanceSource === "Synced"
  );
}

/**
 * True when the followed limit is the bank's latest successful value.
 * A stale or overridden limit stays unmarked.
 */
function limitIsCurrent(debt: DebtDto) {
  return (
    debt.following &&
    debt.creditLimitSource === "Synced" &&
    debt.freshness === "Current"
  );
}

/**
 * The terms a person scans on one debt.
 * A label is quiet. A known value is the text they read. A blank term says Unknown.
 * A revolving limit that is in use is shown above these terms.
 */
function DebtFacts({ debt }: { debt: DebtDto }) {
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
  ];
  if (debt.kind === "Revolving") {
    if (debt.creditLimitInUse === null) {
      facts.push({ label: "Limit", value: null });
    }
  } else {
    facts.push(termFact(debt));
  }

  return (
    <div className="flex flex-col gap-3">
      <dl
        className={cn(
          "grid grid-cols-2 gap-x-4 gap-y-3",
          facts.length > 3 ? "sm:grid-cols-4" : "sm:grid-cols-3",
        )}
      >
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
};

/**
 * The months left on an installment debt.
 * A blank term stays unknown.
 */
function termFact(debt: DebtDto): DebtFact {
  if (debt.remainingTermMonths === null) {
    return { label: "Term", value: null };
  }

  const months =
    debt.remainingTermMonths === 1
      ? "1 month"
      : `${debt.remainingTermMonths} months`;
  return { label: "Term", value: months };
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
function BalanceInUse({
  debt,
  busy,
  onUseSyncedValue,
}: {
  debt: DebtDto;
  busy: boolean;
  onUseSyncedValue: () => void;
}) {
  const amount = debt.balanceInUse;
  const credit =
    debt.balanceSource === "Synced" && debt.balanceCredit !== null
      ? creditNote(debt.balanceCredit, debt.currency, formatCurrency)
      : null;
  const block = debt.following
    ? followBlockCopy(debt.syncedBalanceBlock)
    : null;
  const showSynced =
    debt.following &&
    debt.balanceSource === "Override" &&
    debt.syncedBalance !== null;
  const canUseSynced = showSynced && debt.syncedBalanceBlock === "None";
  return (
    <div className="flex shrink-0 flex-col items-end text-right">
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
      {showSynced && debt.syncedBalance !== null ? (
        <p className="mt-0.5 text-xs text-muted-foreground">
          {`Synced ${formatCurrency(debt.syncedBalance, debt.currency)}`}
        </p>
      ) : null}
      {canUseSynced ? (
        <Button
          type="button"
          variant="outline"
          className="mt-2 min-h-11"
          disabled={busy}
          onClick={onUseSyncedValue}
        >
          Use synced value
        </Button>
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
  onReconnect: () => void;
  reconnecting: boolean;
  onUpdateBalance: (amount: string) => Promise<boolean>;
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
 * A current balance is already named under the amount. An old balance uses the notice and a filled Refresh. A failed sync or a removed link uses the error alert.
 */
function FollowStatus({
  debt,
  accounts,
  busy,
  onRefresh,
  onReconnect,
  reconnecting,
  onUpdateBalance,
}: FollowStatusProps) {
  const [updating, setUpdating] = useState(false);
  const [amount, setAmount] = useState("");
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
  const repairId = repairConnectionId(debt.freshness, account?.plaidItemId);
  const openConnections =
    debt.freshness === "Disconnected" ||
    (debt.freshness === "SyncFailing" && !repairId);
  const canUpdate = canUpdateBalance(debt.freshness);
  const broken =
    debt.freshness === "SyncFailing" || debt.freshness === "Disconnected";
  const stale = debt.freshness === "Stale";

  /**
   * Saves today's balance and closes the amount field when it is kept.
   */
  async function saveUpdate() {
    const saved = await onUpdateBalance(amount);
    if (saved) {
      setUpdating(false);
      setAmount("");
    }
  }

  return (
    <div className="flex flex-col gap-2">
      {broken ? (
        <Alert variant="destructive" className="flex items-start gap-2">
          <CircleAlert className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
          <span>{line}</span>
        </Alert>
      ) : stale ? (
        <Alert variant="notice" className="flex items-start gap-2">
          <CircleAlert className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
          <span>{line}</span>
        </Alert>
      ) : (
        <p className="flex items-start gap-2 text-sm text-foreground">
          <CircleAlert className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
          <span>{line}</span>
        </p>
      )}
      {updating ? (
        <div className="flex max-w-xs flex-col gap-2">
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Balance
            <Input
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              inputMode="decimal"
              autoComplete="off"
              disabled={busy}
            />
            <span className="font-normal text-muted-foreground">
              Dated today. The connection does not replace it.
            </span>
          </label>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              className="min-h-11"
              disabled={busy}
              onClick={() => void saveUpdate()}
            >
              Save
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              disabled={busy}
              onClick={() => {
                setUpdating(false);
                setAmount("");
              }}
            >
              Cancel
            </Button>
          </div>
        </div>
      ) : null}
      {canRefresh || repairId || openConnections || canUpdate ? (
        <div className="flex flex-wrap gap-2">
          {canRefresh ? (
            <Button
              type="button"
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
          {repairId ? (
            <Button
              type="button"
              variant="destructive"
              className="min-h-11"
              disabled={busy || reconnecting}
              aria-busy={reconnecting}
              onClick={onReconnect}
            >
              {reconnecting ? (
                <LoaderCircle
                  className="size-4 animate-spin"
                  aria-hidden="true"
                />
              ) : null}
              Reconnect
            </Button>
          ) : null}
          {openConnections ? (
            <Link
              href="/connections"
              className={cn(
                buttonVariants({ variant: "destructive" }),
                "min-h-11",
              )}
            >
              Reconnect
            </Link>
          ) : null}
          {canUpdate && !updating ? (
            <Button
              type="button"
              variant="outline"
              className="min-h-11"
              disabled={busy}
              onClick={() => setUpdating(true)}
            >
              Update balance
            </Button>
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
