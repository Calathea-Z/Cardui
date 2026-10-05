import type { TransactionDto } from "@/lib/api/types";
import { getCategoryEmoji } from "@/features/categories/categoryEmoji";
import { cn } from "@/lib/utils";
import {
  getTransactionAmountDisplay,
  isBalanceReconciliation,
  isTransferTransaction,
} from "./transactionAmountDisplay";
import {
  transactionListContext,
  transactionListTitle,
} from "./transactionListPresentation";

type TransactionRowProps = {
  transaction: TransactionDto;
  onSelect?: (transaction: TransactionDto) => void;
};

/**
 * Picks the color for a transaction amount.
 * Income uses success, a transfer uses the transfer color, and spending uses the foreground color.
 */
function amountClassName(kind: "income" | "spend" | "transfer") {
  return cn(
    "ledger-amount",
    kind === "income" && "text-success",
    kind === "transfer" && "text-transfer",
    kind === "spend" && "text-foreground",
  );
}

/**
 * Marks a transfer, balance adjustment, or pending transaction.
 * A posted spending or income row has no mark.
 */
function TransactionMarks({ transaction }: { transaction: TransactionDto }) {
  const isTransfer = isTransferTransaction(transaction);
  const isAdjustment = isBalanceReconciliation(transaction);

  if (!isTransfer && !isAdjustment && !transaction.pending) {
    return null;
  }

  return (
    <span className="flex shrink-0 flex-wrap items-center justify-end gap-1">
      {isTransfer ? (
        <span className="ledger-stamp bg-transfer-soft text-transfer">
          Transfer
        </span>
      ) : null}
      {isAdjustment ? (
        <span className="ledger-stamp bg-transfer-soft text-transfer">
          Adjustment
        </span>
      ) : null}
      {transaction.pending ? (
        <span className="ledger-stamp bg-muted text-muted-foreground">
          Pending
        </span>
      ) : null}
    </span>
  );
}

/**
 * Shows one transaction in the list.
 * The title prefers a merchant name, with the account and category underneath. A select handler turns the row into a button.
 */
export function TransactionRow({ transaction, onSelect }: TransactionRowProps) {
  const amount = getTransactionAmountDisplay(transaction);

  const content = (
    <>
      <div className="flex min-w-0 items-start gap-3">
        <span
          className="mt-1 shrink-0 text-base leading-none"
          aria-hidden="true"
        >
          {getCategoryEmoji(transaction.category)}
        </span>
        <div className="min-w-0 text-left">
          <p className="line-clamp-2 font-medium text-foreground">
            {transactionListTitle(transaction)}
          </p>
          <div className="mt-0.5 flex min-w-0 items-center gap-2">
            <p className="min-w-0 flex-1 truncate text-xs text-muted-foreground">
              {transactionListContext(transaction)}
            </p>
            <TransactionMarks transaction={transaction} />
          </div>
        </div>
      </div>

      <p className={cn("shrink-0", amountClassName(amount.kind))}>
        {amount.label}
      </p>
    </>
  );

  if (onSelect) {
    return (
      <button
        type="button"
        onClick={() => onSelect(transaction)}
        className="flex w-full cursor-pointer items-start justify-between gap-4 px-4 py-3 text-sm transition-colors hover:bg-accent/20"
      >
        {content}
      </button>
    );
  }

  return (
    <div className="flex items-start justify-between gap-4 px-4 py-3 text-sm">
      {content}
    </div>
  );
}
