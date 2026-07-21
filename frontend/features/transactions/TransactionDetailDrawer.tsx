"use client";

import { useEffect, useRef, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import type { CategoryDto, TransactionDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import {
  getTransactionAmountDisplay,
  isTransferTransaction,
} from "./transactionAmountDisplay";
import { FULL_SCREEN_SHEET_CLASSNAME } from "./fullScreenSheet";
import { TransactionCategorySelect } from "./TransactionCategorySelect";
import { TransactionDateField } from "./TransactionDateField";
import { TransactionNotesField } from "./TransactionNotesField";
import { useTransactionDetails } from "./useTransactionDetails";

type TransactionDetailDrawerProps = {
  transaction: TransactionDto | null;
  categories: CategoryDto[];
  onClose: () => void;
  onSaved: (transaction: TransactionDto) => void;
  onCategoryCreated: (category: CategoryDto) => void;
};

function amountClassName(kind: "income" | "spend" | "transfer") {
  return cn(
    "ledger-amount text-lg",
    kind === "income" && "text-success",
    kind === "transfer" && "text-transfer",
    kind === "spend" && "text-foreground",
  );
}

function TransactionDetailDrawerContent({
  transaction,
  categories,
  onSaved,
  onCategoryCreated,
  onCategoryPickerOpenChange,
}: {
  transaction: TransactionDto;
  categories: CategoryDto[];
  onSaved: (transaction: TransactionDto) => void;
  onCategoryCreated: (category: CategoryDto) => void;
  onCategoryPickerOpenChange: (open: boolean) => void;
}) {
  const {
    form,
    errorMessage,
    clearError,
    setDate,
    setCategoryId,
    setNotes,
    registerCategory,
    flushPendingSave,
  } = useTransactionDetails({
    transaction,
    categories,
    onSaved,
  });

  const flushPendingSaveRef = useRef(flushPendingSave);
  flushPendingSaveRef.current = flushPendingSave;

  useEffect(() => {
    return () => {
      flushPendingSaveRef.current();
    };
  }, []);

  const amount = getTransactionAmountDisplay(transaction);
  const isTransfer = isTransferTransaction(transaction);

  function handleCategoryCreated(category: CategoryDto) {
    registerCategory(category);
    onCategoryCreated(category);
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col items-center gap-1 text-center">
        <p className={amountClassName(amount.kind)}>{amount.label}</p>
        <p className="text-sm text-muted-foreground">
          {transaction.account.name}
        </p>
        {isTransfer || transaction.pending ? (
          <div className="flex flex-wrap items-center justify-center gap-2 pt-1 text-xs">
            {isTransfer ? (
              <span className="ledger-stamp bg-transfer-soft text-transfer">
                Transfer
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

      <div className="divide-y divide-border/70 border-y border-border/70">
        <TransactionCategorySelect
          categories={categories}
          categoryId={form.categoryId}
          onCategoryIdChange={setCategoryId}
          onCategoryCreated={handleCategoryCreated}
          onOpenChange={onCategoryPickerOpenChange}
        />

        <TransactionDateField value={form.date} onChange={setDate} />

        <TransactionNotesField value={form.notes} onChange={setNotes} />
      </div>

      {errorMessage ? (
        <Alert variant="destructive">
          {errorMessage}{" "}
          <button
            type="button"
            onClick={clearError}
            className="cursor-pointer underline underline-offset-2"
          >
            Dismiss
          </button>
        </Alert>
      ) : null}
    </div>
  );
}

export function TransactionDetailDrawer({
  transaction,
  categories,
  onClose,
  onSaved,
  onCategoryCreated,
}: TransactionDetailDrawerProps) {
  const [isCategoryPickerOpen, setIsCategoryPickerOpen] = useState(false);
  const title = transaction
    ? transaction.merchantName || transaction.name
    : "Transaction";

  useEffect(() => {
    if (transaction === null) {
      setIsCategoryPickerOpen(false);
    }
  }, [transaction]);

  return (
    <BottomSheet
      open={transaction !== null}
      onClose={onClose}
      title={title}
      headerAction="back"
      closeOnEscape={!isCategoryPickerOpen}
      className={FULL_SCREEN_SHEET_CLASSNAME}
    >
      {transaction ? (
        <TransactionDetailDrawerContent
          transaction={transaction}
          categories={categories}
          onSaved={onSaved}
          onCategoryCreated={onCategoryCreated}
          onCategoryPickerOpenChange={setIsCategoryPickerOpen}
        />
      ) : null}
    </BottomSheet>
  );
}
