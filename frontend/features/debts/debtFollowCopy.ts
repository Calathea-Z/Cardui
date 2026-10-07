import type {
  AccountDto,
  DebtAccountBalanceBlock,
  DebtLinkFreshness,
} from "@/lib/api/types";

type MoneyText = (amount: number, currency: string) => string;

type DateText = (value: string) => string;

/**
 * Names a connected account with its last four digits when it has them.
 * A missing mask leaves the account name alone.
 */
export function followedAccountLabel(name: string, mask: string | null) {
  const digits = mask?.trim();
  return digits ? `${name} ending ${digits}` : name;
}

/**
 * True when this account is a connected credit card or loan in the debt's currency.
 * An account another debt already follows is left out. A manual account is left out.
 */
export function isFollowCandidate(
  account: AccountDto,
  debtCurrency: string,
  followedByAnother: boolean,
) {
  const type = account.type.toLowerCase();
  if (type !== "credit" && type !== "loan") {
    return false;
  }

  if (
    followedByAnother ||
    account.source !== "Plaid" ||
    !account.plaidItemId ||
    !account.isActive ||
    account.archivedAt
  ) {
    return false;
  }

  const accountCurrency = account.isoCurrencyCode?.trim().toLowerCase();
  const currency = debtCurrency.trim().toLowerCase();
  return Boolean(accountCurrency) && accountCurrency === currency;
}

/**
 * The freshness sentence under a followed balance.
 * A missing date leaves that part out. The last balance stays in the sentence when it has one.
 */
export function freshnessLine(
  freshness: DebtLinkFreshness,
  syncedAsOf: string | null,
  syncFailedOn: string | null,
  formatDate: DateText,
) {
  const synced = syncedAsOf ? formatDate(syncedAsOf) : null;
  const failed = syncFailedOn ? formatDate(syncFailedOn) : null;
  switch (freshness) {
    case "Current":
      return synced ? `Synced ${synced}` : "Synced";
    case "Stale":
      return synced
        ? `Last synced ${synced}. A refresh has not run since.`
        : "A refresh has not run since.";
    case "SyncFailing":
      if (failed && synced) {
        return `Sync failed ${failed}. Showing the ${synced} balance.`;
      }

      return failed ? `Sync failed ${failed}.` : "Sync failed.";
    case "Disconnected":
      return synced
        ? `Bank link removed. Showing the ${synced} balance.`
        : "Bank link removed.";
    case "AccountMissing":
      return "The bank no longer returns this account.";
  }
}

/**
 * The note for a followed card whose balance is a credit.
 * The amount is the positive credit. The debt counts zero.
 */
export function creditNote(amount: number, currency: string, money: MoneyText) {
  return `The card shows a ${money(amount, currency)} credit. Counted as $0.`;
}

/**
 * Why a connected balance cannot be used yet.
 * None returns null. The person's balance stays in use.
 */
export function followBlockCopy(block: DebtAccountBalanceBlock) {
  switch (block) {
    case "DateUnknown":
      return "That balance has no date, so yours stays in use.";
    case "NegativeBalance":
      return "That balance is below zero, so yours stays in use.";
    case "CurrencyDiffers":
      return "That account uses a different currency, so yours stays in use.";
    case "AmountTooLarge":
      return "That balance is too large to store, so yours stays in use.";
    default:
      return null;
  }
}

/**
 * The toast after a debt starts following an account.
 */
export function startedFollowingToast(debtName: string, accountLabel: string) {
  return `${debtName} now follows ${accountLabel}.`;
}

/**
 * True when a followed card can record today's balance.
 * A current card uses the form. A missing account is stopped, not updated.
 */
export function canUpdateBalance(freshness: DebtLinkFreshness) {
  return (
    freshness === "Stale" ||
    freshness === "SyncFailing" ||
    freshness === "Disconnected"
  );
}

/**
 * The toast after the person keeps their own balance.
 * A known amount names it and the date that was saved.
 */
export function ownBalanceToast(
  name: string,
  balance: string | null,
  asOf: string | null,
) {
  if (balance && asOf) {
    return `${name} is using your balance of ${balance} from ${asOf}.`;
  }

  return `${name} is using your balance.`;
}

/**
 * The toast after the person goes back to the synced balance.
 */
export function syncedBalanceToast(name: string) {
  return `${name} is using the synced balance again.`;
}

/**
 * The toast after a debt stops following.
 * A known balance names the amount and the date that was kept.
 */
export function stoppedFollowingToast(
  name: string,
  balance: string | null,
  asOf: string | null,
) {
  if (balance && asOf) {
    return `${name} is manual again. Balance kept as ${balance} from ${asOf}.`;
  }

  return `${name} is manual again.`;
}
