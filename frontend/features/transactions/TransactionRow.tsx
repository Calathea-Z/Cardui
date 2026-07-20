import type { TransactionDto } from "@/lib/api/types";
import { getCategoryEmoji } from "@/features/categories/categoryEmoji";
import { cn } from "@/lib/utils";
import {
  getTransactionAmountDisplay,
  isTransferTransaction,
} from "./transactionAmountDisplay";

type TransactionRowProps = {
  transaction: TransactionDto;
};

function amountClassName(kind: "income" | "spend" | "transfer") {
  return cn(
    "font-medium tabular-nums",
    kind === "income" && "text-success",
    kind === "transfer" && "text-transfer",
    kind === "spend" && "text-foreground",
  );
}

export function TransactionRow({ transaction }: TransactionRowProps) {
  const amount = getTransactionAmountDisplay(transaction);
  const isTransfer = isTransferTransaction(transaction);

  return (
    <div className="flex items-start justify-between gap-4 px-4 py-4 text-sm">
      <div className="flex min-w-0 items-start gap-3">
        <span
          className="mt-0.5 shrink-0 text-base leading-none"
          aria-hidden="true"
        >
          {getCategoryEmoji(transaction.category)}
        </span>
        <div className="min-w-0">
          <p className="truncate font-medium text-foreground">
            {transaction.name}
          </p>
          {isTransfer || transaction.pending ? (
            <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs">
              {isTransfer ? (
                <span className="bg-transfer-soft rounded-full px-2 py-0.5 text-transfer">
                  Transfer
                </span>
              ) : null}
              {transaction.pending ? (
                <span className="rounded-full bg-muted px-2 py-0.5 text-muted-foreground">
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
            Move between accounts
          </p>
        ) : null}
      </div>
    </div>
  );
}
