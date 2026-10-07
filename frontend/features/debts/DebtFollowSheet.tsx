"use client";

import { useState } from "react";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type { DebtDto, DebtFollowAccountDto } from "@/lib/api/types";
import { formatCalendarDate } from "./debtDisplay";
import {
  creditNote,
  followBlockCopy,
  followedAccountLabel,
} from "./debtFollowCopy";

type DebtFollowSheetProps = {
  debt: DebtDto | null;
  accounts: DebtFollowAccountDto[];
  loading: boolean;
  error: string | null;
  busy: boolean;
  onClose: () => void;
  onRetry: () => void;
  onFollow: (accountId: string, keepOwnBalance: boolean) => void;
};

/**
 * Nested steps for choosing an account and confirming the follow.
 * Back returns to the list. Escape closes the steps and leaves the debt page in place.
 */
export function DebtFollowSheet({
  debt,
  accounts,
  loading,
  error,
  busy,
  onClose,
  onRetry,
  onFollow,
}: DebtFollowSheetProps) {
  const [pickedId, setPickedId] = useState<string | null>(null);
  const [listRequested, setListRequested] = useState(false);
  const [stepDebtId, setStepDebtId] = useState<string | null>(debt?.id ?? null);
  if ((debt?.id ?? null) !== stepDebtId) {
    setStepDebtId(debt?.id ?? null);
    setPickedId(null);
    setListRequested(false);
  }

  const linked =
    !loading && debt
      ? (accounts.find((account) => account.accountId === debt.accountId) ??
        null)
      : null;
  const picked =
    accounts.find((account) => account.accountId === pickedId) ?? null;
  const confirming = listRequested ? picked : (picked ?? linked);

  /**
   * Returns to the account list, or closes when the list was skipped.
   */
  function back() {
    if (pickedId) {
      setPickedId(null);
      setListRequested(true);
      return;
    }

    onClose();
  }

  return (
    <BottomSheet
      open={debt !== null}
      onClose={onClose}
      onBack={back}
      title={confirming ? "Follow this account" : "Follow a connected account"}
      presentation="panel"
      headerAction="back"
    >
      {debt ? (
        <FollowBody
          debt={debt}
          accounts={accounts}
          confirming={confirming}
          loading={loading}
          error={error}
          busy={busy}
          onRetry={onRetry}
          onPick={setPickedId}
          onFollow={onFollow}
        />
      ) : null}
    </BottomSheet>
  );
}

type FollowBodyProps = {
  debt: DebtDto;
  accounts: DebtFollowAccountDto[];
  confirming: DebtFollowAccountDto | null;
  loading: boolean;
  error: string | null;
  busy: boolean;
  onRetry: () => void;
  onPick: (accountId: string) => void;
  onFollow: (accountId: string, keepOwnBalance: boolean) => void;
};

/**
 * The account list, or the confirm step for the account that was chosen.
 * A failed load offers retry. An empty list says what kind of account can be followed.
 */
function FollowBody({
  debt,
  accounts,
  confirming,
  loading,
  error,
  busy,
  onRetry,
  onPick,
  onFollow,
}: FollowBodyProps) {
  if (loading) {
    return <p className="text-sm text-muted-foreground">Loading accounts…</p>;
  }

  if (error) {
    return (
      <div className="flex flex-col gap-3">
        <p className="text-sm text-muted-foreground">{error}</p>
        <Button type="button" className="min-h-11" onClick={onRetry}>
          Try again
        </Button>
      </div>
    );
  }

  if (confirming) {
    return (
      <ConfirmFollow
        debt={debt}
        account={confirming}
        busy={busy}
        onFollow={onFollow}
      />
    );
  }

  if (accounts.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        No connected card or loan matches this debt. Following needs an active
        credit card or loan in the same currency.
      </p>
    );
  }

  return (
    <ul className="flex flex-col gap-2">
      {accounts.map((account) => {
        const label = followedAccountLabel(account.name, account.mask);
        const amount =
          account.balanceInUse === null
            ? "Balance unknown"
            : formatCurrency(account.balanceInUse, debt.currency);
        return (
          <li key={account.accountId}>
            <button
              type="button"
              className="flex min-h-11 w-full flex-col items-start justify-center rounded-lg border border-border bg-card px-3 py-2 text-left focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
              onClick={() => onPick(account.accountId)}
            >
              <span className="font-medium">{label}</span>
              <span className="text-sm text-muted-foreground">{amount}</span>
            </button>
          </li>
        );
      })}
    </ul>
  );
}

type ConfirmFollowProps = {
  debt: DebtDto;
  account: DebtFollowAccountDto;
  busy: boolean;
  onFollow: (accountId: string, keepOwnBalance: boolean) => void;
};

/**
 * Shows what will follow and what stays the person's.
 * When the balances differ, the person chooses the connected amount or keeps theirs.
 */
function ConfirmFollow({ debt, account, busy, onFollow }: ConfirmFollowProps) {
  const label = followedAccountLabel(account.name, account.mask);
  const block = followBlockCopy(account.block);
  const choose =
    account.balancesDiffer && account.block === "None" && block === null;
  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm text-muted-foreground">
        {debt.name} will follow {label}. The balance follows that account. APR,
        minimum, and due date stay yours.
      </p>
      {choose ? (
        <BalanceChoice debt={debt} account={account} />
      ) : (
        <ConnectedBalance debt={debt} account={account} />
      )}
      {block ? <p className="text-sm text-muted-foreground">{block}</p> : null}
      {choose ? (
        <div className="flex flex-col gap-2">
          <Button
            type="button"
            className="min-h-11"
            disabled={busy}
            onClick={() => onFollow(account.accountId, false)}
          >
            Use connected value
          </Button>
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            disabled={busy}
            onClick={() => onFollow(account.accountId, true)}
          >
            Keep mine as my own value
          </Button>
        </div>
      ) : (
        <Button
          type="button"
          className="min-h-11"
          disabled={busy}
          onClick={() => onFollow(account.accountId, false)}
        >
          Follow
        </Button>
      )}
    </div>
  );
}

type BalanceChoiceProps = {
  debt: DebtDto;
  account: DebtFollowAccountDto;
};

/**
 * The person's balance beside the connected one.
 * The connected side is the amount following would use.
 */
function BalanceChoice({ debt, account }: BalanceChoiceProps) {
  return (
    <div className="overflow-hidden rounded-md border border-border bg-muted/60">
      <div className="grid grid-cols-1 sm:grid-cols-2 sm:divide-x sm:divide-border">
        <AmountSide
          label="Yours"
          amount={debt.balance}
          asOf={debt.balanceAsOf}
          currency={debt.currency}
          marked
        />
        <AmountSide
          label="Connected"
          amount={account.balanceInUse}
          asOf={account.balanceInUseAsOf}
          currency={debt.currency}
          note={
            account.balanceCredit === null
              ? null
              : creditNote(account.balanceCredit, debt.currency, formatCurrency)
          }
        />
      </div>
    </div>
  );
}

type ConnectedBalanceProps = {
  debt: DebtDto;
  account: DebtFollowAccountDto;
};

/**
 * The balance following would use when there is no second choice.
 */
function ConnectedBalance({ debt, account }: ConnectedBalanceProps) {
  return (
    <AmountSide
      label="Connected"
      amount={account.balanceInUse}
      asOf={account.balanceInUseAsOf}
      currency={debt.currency}
      note={
        account.balanceCredit === null
          ? null
          : creditNote(account.balanceCredit, debt.currency, formatCurrency)
      }
    />
  );
}

type AmountSideProps = {
  label: string;
  amount: number | null;
  asOf: string | null;
  currency: string;
  marked?: boolean;
  note?: string | null;
};

/**
 * One money amount and the date it was true.
 * A missing amount says unknown. A missing date says the date is unknown.
 */
function AmountSide({
  label,
  amount,
  asOf,
  currency,
  marked = false,
  note = null,
}: AmountSideProps) {
  return (
    <div
      className={
        marked
          ? "border-l-2 border-l-primary px-3 py-2.5"
          : "border-t border-border px-3 py-2.5 sm:border-t-0"
      }
    >
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="ledger-amount mt-1 text-lg">
        {amount === null ? "Unknown" : formatCurrency(amount, currency)}
      </p>
      <p className="mt-0.5 text-xs text-muted-foreground">
        {asOf ? formatCalendarDate(asOf) : "Date unknown"}
      </p>
      {note ? (
        <p className="mt-1 text-xs text-muted-foreground">{note}</p>
      ) : null}
    </div>
  );
}
