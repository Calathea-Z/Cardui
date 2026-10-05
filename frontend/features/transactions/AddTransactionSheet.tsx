"use client";

import { useState } from "react";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { DateField } from "@/components/ui/date-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { createManualTransaction } from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type { AccountDto, CategoryDto } from "@/lib/api/types";
import { todayDateInput, parseMoney } from "@/features/accounts/manualAccount";

type AddTransactionSheetProps = {
  open: boolean;
  accounts: AccountDto[];
  categories: CategoryDto[];
  onClose: () => void;
  onCreated: () => void;
};

/**
 * Collects a manual transaction and saves it as posted.
 * Money in is stored as a negative amount.
 */
function AddTransactionForm({
  accounts,
  categories,
  onClose,
  onCreated,
  onPickerOpenChange,
}: Omit<AddTransactionSheetProps, "open"> & {
  onPickerOpenChange: (open: boolean) => void;
}) {
  const [accountId, setAccountId] = useState(accounts[0]?.id ?? "");
  const [date, setDate] = useState(todayDateInput);
  const [name, setName] = useState("");
  const [amount, setAmount] = useState("");
  const [direction, setDirection] = useState<"out" | "in">("out");
  const [categoryId, setCategoryId] = useState("");
  const [notes, setNotes] = useState("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  /**
   * Saves the manual transaction when an account, name, amount, and date are present.
   * Money in is stored negative, the transaction is posted, and a successful save closes the sheet.
   */
  async function submit() {
    const parsed = parseMoney(amount);
    if (!accountId || !name.trim() || parsed === null || !date) {
      setErrorMessage("Choose an account and enter a name, amount, and date.");
      return;
    }

    const signed = direction === "in" ? -Math.abs(parsed) : Math.abs(parsed);
    setIsSaving(true);
    setErrorMessage(null);
    try {
      await createManualTransaction({
        accountId,
        date,
        name: name.trim(),
        amount: signed,
        categoryId: categoryId || null,
        notes: notes.trim() || null,
        pending: false,
      });
      onCreated();
      onClose();
    } catch (error) {
      setErrorMessage(
        getApiErrorMessage(error, "Could not add this transaction."),
      );
    } finally {
      setIsSaving(false);
    }
  }

  const selectedAccount = accounts.find((account) => account.id === accountId);

  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        void submit();
      }}
    >
      {accounts.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          Add an account before entering a transaction.
        </p>
      ) : (
        <>
          <div className="flex flex-col gap-1.5 text-sm font-medium">
            Account
            <Select
              title="Account"
              value={accountId}
              onChange={setAccountId}
              onOpenChange={onPickerOpenChange}
              className="h-9"
              options={accounts.map((account) => ({
                value: account.id,
                label: account.name,
              }))}
            />
          </div>

          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Name
            <Input
              value={name}
              onChange={(event) => setName(event.target.value)}
              maxLength={300}
              required
            />
          </label>

          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Amount
            <Input
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              inputMode="decimal"
              required
            />
          </label>

          <div className="flex gap-2">
            <Button
              type="button"
              variant={direction === "out" ? "default" : "outline"}
              className="flex-1"
              onClick={() => setDirection("out")}
            >
              Money out
            </Button>
            <Button
              type="button"
              variant={direction === "in" ? "default" : "outline"}
              className="flex-1"
              onClick={() => setDirection("in")}
            >
              Money in
            </Button>
          </div>
          <p className="text-xs text-muted-foreground">
            Money in counts as income only when you assign the Income category.
            Put an opening balance on the account instead.
          </p>
          {selectedAccount?.plaidItemId ? (
            <p className="text-xs text-muted-foreground">
              This account is linked. The bank still supplies its balance. The
              transaction counts in activity.
            </p>
          ) : null}

          <div className="flex flex-col gap-1.5 text-sm font-medium">
            Category
            <Select
              title="Category"
              value={categoryId}
              onChange={setCategoryId}
              onOpenChange={onPickerOpenChange}
              placeholder="Uncategorized"
              className="h-9"
              options={[
                { value: "", label: "Uncategorized" },
                ...categories.map((category) => ({
                  value: category.id,
                  label: category.name,
                })),
              ]}
            />
          </div>

          <div className="flex flex-col gap-1.5 text-sm font-medium">
            Date
            <DateField
              title="Date"
              value={date}
              onChange={setDate}
              onOpenChange={onPickerOpenChange}
              className="h-9"
            />
          </div>

          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Notes
            <Input
              value={notes}
              onChange={(event) => setNotes(event.target.value)}
              maxLength={1000}
            />
          </label>
        </>
      )}

      {errorMessage ? (
        <Alert variant="destructive">{errorMessage}</Alert>
      ) : null}

      {accounts.length > 0 ? (
        <Button type="submit" size="lg" disabled={isSaving} className="py-3">
          {isSaving ? "Saving" : "Add transaction"}
        </Button>
      ) : null}
    </form>
  );
}

/**
 * Opens the sheet for entering a transaction by hand.
 * Escape is ignored while the account, category, or date sheet is open.
 */
export function AddTransactionSheet({
  open,
  accounts,
  categories,
  onClose,
  onCreated,
}: AddTransactionSheetProps) {
  const [pickerOpen, setPickerOpen] = useState(false);

  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      title="Add transaction"
      closeOnEscape={!pickerOpen}
      presentation="panel"
    >
      {open ? (
        <AddTransactionForm
          accounts={accounts}
          categories={categories}
          onClose={onClose}
          onCreated={onCreated}
          onPickerOpenChange={setPickerOpen}
        />
      ) : null}
    </BottomSheet>
  );
}
