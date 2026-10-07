import type {
  AccountDto,
  DebtAccountBalanceBlock,
  DebtFieldSource,
  DebtLinkFreshness,
  DebtMatchReasonDto,
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
 * The confirm sentence for what will follow.
 * A revolving debt with a usable limit names the credit limit. A loan does not.
 */
export function followConfirmLead(
  debtName: string,
  accountLabel: string,
  followsLimit: boolean,
) {
  const followed = followsLimit
    ? "The balance and credit limit follow that account."
    : "The balance follows that account.";
  return `${debtName} will follow ${accountLabel}. ${followed} APR, minimum, and due date stay yours.`;
}

/**
 * The short source under a followed credit limit.
 * Manual returns null, because a missing bank limit is not labeled as an override.
 */
export function creditLimitSourceText(
  source: DebtFieldSource,
  syncedAsOf: string | null,
  overriddenOn: string | null,
  formatDate: (value: string) => string,
) {
  if (source === "Synced") {
    return syncedAsOf ? `Synced · ${formatDate(syncedAsOf)}` : "Synced";
  }

  if (source === "Override") {
    return overriddenOn
      ? `Your value since ${formatDate(overriddenOn)}`
      : "Your value";
  }

  return null;
}

/**
 * The toast after a debt starts following an account.
 */
export function startedFollowingToast(debtName: string, accountLabel: string) {
  return `${debtName} now follows ${accountLabel}.`;
}

/**
 * The bank connection Reconnect can repair, or null when it cannot.
 * A failing sync still has an item. A removed link does not, so it cannot be repaired in place.
 */
export function repairConnectionId(
  freshness: DebtLinkFreshness,
  plaidItemId: string | null | undefined,
) {
  if (freshness !== "SyncFailing" || !plaidItemId) {
    return null;
  }

  return plaidItemId;
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
 * The toast after the person keeps their own credit limit.
 * A known amount names it.
 */
export function ownCreditLimitToast(name: string, limit: string | null) {
  if (limit) {
    return `${name} is using your credit limit of ${limit}.`;
  }

  return `${name} is using your credit limit.`;
}

/**
 * The toast after the person goes back to the synced credit limit.
 */
export function syncedCreditLimitToast(name: string) {
  return `${name} is using the synced credit limit again.`;
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

/**
 * The sentence for one suggestion reason.
 * A mask names the digits. A name lists the shared words. A balance names the gap, or says the amounts match.
 */
export function matchReasonText(
  reason: DebtMatchReasonDto,
  currency: string,
  money: MoneyText,
) {
  switch (reason.kind) {
    case "Mask":
      return reason.mask ? `Ends in ${reason.mask}` : null;
    case "Name":
      return nameReason(reason.words);
    case "Balance":
      if (reason.difference === null) {
        return null;
      }

      return reason.difference === 0
        ? "Same balance as yours"
        : `Balance within ${money(reason.difference, currency)} of yours`;
  }
}

/**
 * The accessible name for a suggested account.
 * The account comes first, then each reason.
 */
export function suggestionLabel(
  accountName: string,
  mask: string | null,
  reasons: string[],
) {
  const detail = reasons.filter((reason) => reason.length > 0);
  return [followedAccountLabel(accountName, mask), ...detail].join(", ");
}

/**
 * A shared-name reason.
 * One word stands alone. Two use "and". More use a list that ends with "and".
 */
function nameReason(words: string[]) {
  const names = words.filter((word) => word.trim().length > 0);
  if (names.length === 0) {
    return null;
  }

  if (names.length === 1) {
    return `Name includes ${names[0]}`;
  }

  if (names.length === 2) {
    return `Name includes ${names[0]} and ${names[1]}`;
  }

  const last = names[names.length - 1];
  return `Name includes ${names.slice(0, -1).join(", ")}, and ${last}`;
}
