"use client";

import { useState } from "react";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type {
  CategoryDto,
  GroupDto,
  SubGroupDto,
  TransactionDto,
} from "@/lib/api/types";
import { cn } from "@/lib/utils";
import {
  getTransactionAmountDisplay,
  isBalanceReconciliation,
  isTransferTransaction,
} from "./transactionAmountDisplay";
import { TransactionAccountField } from "./TransactionAccountField";
import { TransactionCategorySelect } from "./TransactionCategorySelect";
import { TransactionDateField } from "./TransactionDateField";
import { TransactionHistoryField } from "./TransactionHistoryField";
import { TransactionNotesField } from "./TransactionNotesField";
import { TransactionOriginalStatementField } from "./TransactionOriginalStatementField";
import { canEditTransactionEntry } from "./transactionEntry";
import { useTransactionDetails } from "./useTransactionDetails";

type TransactionDetailDrawerProps = {
  transaction: TransactionDto | null;
  categories: CategoryDto[];
  groups: GroupDto[];
  subGroups: SubGroupDto[];
  onClose: () => void;
  onSaved: (transaction: TransactionDto) => void;
  onCategoryCreated: (category: CategoryDto) => void;
  onSelectTransaction?: (transaction: TransactionDto) => void;
  onArchived: (transactionId: string) => void;
  onRestored: () => void;
};

/**
 * Picks the color for the amount at the top of the detail.
 * Income uses success, a transfer uses the transfer color, and spending uses the foreground color.
 */
function amountClassName(kind: "income" | "spend" | "transfer") {
  return cn(
    "ledger-amount text-lg",
    kind === "income" && "text-success",
    kind === "transfer" && "text-transfer",
    kind === "spend" && "text-foreground",
  );
}

/**
 * Shows and edits one transaction, and can archive or restore it.
 * Name and amount stay editable for a manual entry or a CSV import, archive asks before removing a live transaction, and a waiting text save is sent when the detail unmounts.
 */
function TransactionDetailDrawerContent({
  transaction,
  categories,
  groups,
  subGroups,
  onSaved,
  onCategoryCreated,
  onSelectTransaction,
  onNestedOpenChange,
  onArchived,
  onRestored,
}: {
  transaction: TransactionDto;
  categories: CategoryDto[];
  groups: GroupDto[];
  subGroups: SubGroupDto[];
  onSaved: (transaction: TransactionDto) => void;
  onCategoryCreated: (category: CategoryDto) => void;
  onSelectTransaction?: (transaction: TransactionDto) => void;
  onNestedOpenChange: (open: boolean) => void;
  onArchived: (transactionId: string) => void;
  onRestored: () => void;
}) {
  const {
    form,
    errorMessage,
    clearError,
    setDate,
    setCategoryId,
    setNotes,
    setName,
    setAmount,
    setDirection,
    registerCategory,
    archiveError,
    isArchiving,
    confirmArchive,
    beginArchive,
    cancelArchive,
    archiveOrRestore,
  } = useTransactionDetails({
    transaction,
    categories,
    onSaved,
    onArchived,
    onRestored,
  });

  const amount = getTransactionAmountDisplay(transaction);
  const isTransfer = isTransferTransaction(transaction);
  const isAdjustment = isBalanceReconciliation(transaction);
  const canEditEntry = canEditTransactionEntry(transaction);

  /**
   * Adds a new category to this transaction and to the page list.
   */
  function handleCategoryCreated(category: CategoryDto) {
    registerCategory(category);
    onCategoryCreated(category);
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col items-center gap-1 text-center">
        <p className={amountClassName(amount.kind)}>{amount.label}</p>
        {isTransfer || transaction.pending || isAdjustment ? (
          <div className="flex flex-wrap items-center justify-center gap-2 pt-1 text-xs">
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

      <div className="divide-y divide-border/70 border-y border-border/70">
        <TransactionAccountField accountName={transaction.account.name} />

        {canEditEntry ? (
          <label className="flex min-h-12 items-center gap-3 py-3">
            <span className="shrink-0 text-sm font-medium text-foreground">
              Name
            </span>
            <Input
              value={form.name}
              onChange={(event) => setName(event.target.value)}
              aria-label="Name"
              className="h-8 border-0 bg-transparent text-right shadow-none"
            />
          </label>
        ) : (
          <TransactionOriginalStatementField statement={transaction.name} />
        )}

        {canEditEntry ? (
          <div className="flex flex-col gap-3 py-3">
            <label className="flex min-h-12 items-center gap-3">
              <span className="shrink-0 text-sm font-medium text-foreground">
                Amount
              </span>
              <Input
                value={form.amount}
                onChange={(event) => setAmount(event.target.value)}
                inputMode="decimal"
                aria-label="Amount"
                className="h-8 border-0 bg-transparent text-right shadow-none"
              />
            </label>
            <div className="flex gap-2">
              <Button
                type="button"
                variant={form.direction === "out" ? "default" : "outline"}
                className="flex-1"
                onClick={() => setDirection("out")}
              >
                Money out
              </Button>
              <Button
                type="button"
                variant={form.direction === "in" ? "default" : "outline"}
                className="flex-1"
                onClick={() => setDirection("in")}
              >
                Money in
              </Button>
            </div>
            <p className="text-xs text-muted-foreground">
              Money in counts as income only when you assign the Income
              category. An opening balance belongs on the account.
            </p>
          </div>
        ) : null}

        {isAdjustment ? (
          <p className="py-3 text-sm text-muted-foreground">
            This adjustment makes the account match a statement. It is not
            income or spending.
          </p>
        ) : null}

        <TransactionHistoryField
          transactionId={transaction.id}
          onOpenChange={onNestedOpenChange}
          onSelectTransaction={onSelectTransaction}
        />

        {isAdjustment ? null : (
          <TransactionCategorySelect
            categories={categories}
            groups={groups}
            subGroups={subGroups}
            categoryId={form.categoryId}
            onCategoryIdChange={setCategoryId}
            onCategoryCreated={handleCategoryCreated}
            onOpenChange={onNestedOpenChange}
          />
        )}

        <TransactionDateField
          value={form.date}
          onChange={setDate}
          onOpenChange={onNestedOpenChange}
        />

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

      {archiveError ? (
        <Alert variant="destructive">{archiveError}</Alert>
      ) : null}

      {confirmArchive ? (
        <div className="flex flex-col gap-2">
          <p className="text-sm text-muted-foreground">
            Archiving removes this transaction from activity and from a manual
            account balance. You can restore it from the Archived filter.
          </p>
          <div className="flex gap-2">
            <Button
              type="button"
              variant="outline"
              className="flex-1"
              onClick={cancelArchive}
            >
              Cancel
            </Button>
            <Button
              type="button"
              variant="destructive"
              className="flex-1"
              disabled={isArchiving}
              onClick={() => void archiveOrRestore()}
            >
              {isArchiving ? "Archiving" : "Archive"}
            </Button>
          </div>
        </div>
      ) : (
        <Button
          type="button"
          variant={transaction.archivedAt ? "outline" : "destructive"}
          disabled={isArchiving}
          onClick={beginArchive}
        >
          {transaction.archivedAt
            ? "Restore transaction"
            : "Archive transaction"}
        </Button>
      )}
    </div>
  );
}

/**
 * Opens a transaction's detail sheet.
 * Escape is ignored while a nested sheet is open, and the last transaction stays available while the sheet closes.
 */
export function TransactionDetailDrawer({
  transaction,
  categories,
  groups,
  subGroups,
  onClose,
  onSaved,
  onCategoryCreated,
  onSelectTransaction,
  onArchived,
  onRestored,
}: TransactionDetailDrawerProps) {
  const [isNestedOpen, setIsNestedOpen] = useState(false);
  const [displayedTransaction, setDisplayedTransaction] = useState(transaction);

  if (transaction !== null && transaction !== displayedTransaction) {
    setDisplayedTransaction(transaction);
  }

  const activeTransaction = transaction ?? displayedTransaction;
  const title = activeTransaction
    ? activeTransaction.merchantName?.trim() || activeTransaction.name
    : "Transaction";

  /**
   * Clears the nested-sheet flag and closes the detail.
   */
  function handleClose() {
    setIsNestedOpen(false);
    onClose();
  }

  return (
    <BottomSheet
      open={transaction !== null}
      onClose={handleClose}
      title={title}
      headerAction="back"
      closeOnEscape={!isNestedOpen}
      presentation="panel"
    >
      {activeTransaction ? (
        <TransactionDetailDrawerContent
          key={activeTransaction.id}
          transaction={activeTransaction}
          categories={categories}
          groups={groups}
          subGroups={subGroups}
          onSaved={onSaved}
          onCategoryCreated={onCategoryCreated}
          onSelectTransaction={onSelectTransaction}
          onNestedOpenChange={setIsNestedOpen}
          onArchived={onArchived}
          onRestored={onRestored}
        />
      ) : null}
    </BottomSheet>
  );
}
