import type { TransactionDto } from "@/lib/api/types";
import { getCategoryEmoji } from "@/features/categories/categoryEmoji";
import { cn } from "@/lib/utils";
import {
  getTransactionAmountDisplay,
  isBalanceReconciliation,
  isTransferTransaction,
} from "./transactionAmountDisplay";

type TransactionRowProps = {
  transaction: TransactionDto;
  onSelect?: (transaction: TransactionDto) => void;
};

function amountClassName(kind: "income" | "spend" | "transfer") {
  return cn(
    "ledger-amount",
    kind === "income" && "text-success",
    kind === "transfer" && "text-transfer",
    kind === "spend" && "text-foreground",
  );
}

export function TransactionRow({ transaction, onSelect }: TransactionRowProps) {
  const amount = getTransactionAmountDisplay(transaction);
  const isTransfer = isTransferTransaction(transaction);
  const isAdjustment = isBalanceReconciliation(transaction);

  const content = (
    <>
      <div className="flex min-w-0 items-start gap-3">
        <span
          className="mt-0.5 shrink-0 text-base leading-none"
          aria-hidden="true"
        >
          {getCategoryEmoji(transaction.category)}
        </span>
        <div className="min-w-0 text-left">
          <p className="truncate font-medium text-foreground">
            {transaction.name}
          </p>
          {isTransfer || transaction.pending || isAdjustment ? (
            <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs">
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
            </div>
          ) : null}
        </div>
      </div>

      <div className="shrink-0 text-right">
        <p className={amountClassName(amount.kind)}>{amount.label}</p>
        {isTransfer ? (
          <p className="mt-0.5 text-[11px] text-transfer">
            Moved between accounts
          </p>
        ) : isAdjustment ? (
          <p className="mt-0.5 text-[11px] text-transfer">
            Not income or spending
          </p>
        ) : null}
      </div>
    </>
  );

  if (onSelect) {
    return (
      <button
        type="button"
        onClick={() => onSelect(transaction)}
        className="flex w-full cursor-pointer items-start justify-between gap-4 px-4 py-4 text-sm transition-colors hover:bg-accent/20"
      >
        {content}
      </button>
    );
  }

  return (
    <div className="flex items-start justify-between gap-4 px-4 py-4 text-sm">
      {content}
    </div>
  );
}
