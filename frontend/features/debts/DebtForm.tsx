"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Form } from "@/components/ui/form";
import { DateField } from "@/components/ui/date-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import type { AccountDto, DebtFieldSource } from "@/lib/api/types";
import type { DebtFormState } from "./debtFormState";
import { debtKindOptions, isDebtKind } from "./debtOptions";

type DebtFormProps = {
  form: DebtFormState;
  accounts: AccountDto[];
  savedAccountName: string | null;
  planningCurrency: string;
  storedCurrency: string | null;
  isEditing: boolean;
  isSaving: boolean;
  following: boolean;
  balanceSource: DebtFieldSource | null;
  creditLimitSource: DebtFieldSource | null;
  onChange: (form: DebtFormState) => void;
  onPickerOpenChange: (open: boolean) => void;
  onSaveOwnBalance: (balance: string, balanceAsOf: string) => Promise<boolean>;
  onSaveOwnCreditLimit: (creditLimit: string) => Promise<boolean>;
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
};

/**
 * Form for one debt.
 * The type starts unset on a new debt. A blank term stays unknown and is not stored as zero.
 * The linked account can stay blank. A credit limit is asked for a revolving debt, and months left for an installment debt.
 * While following, the balance and its date stay locked until the person enters their own.
 * A credit limit the connection provides stays locked the same way. A limit it does not provide stays editable.
 * Saving the other terms does not write a locked field. The account stays locked too.
 */
export function DebtForm({
  form,
  accounts,
  savedAccountName,
  planningCurrency,
  storedCurrency,
  isEditing,
  isSaving,
  following,
  balanceSource,
  creditLimitSource,
  onChange,
  onPickerOpenChange,
  onSaveOwnBalance,
  onSaveOwnCreditLimit,
  onSubmit,
}: DebtFormProps) {
  const [showPromotion, setShowPromotion] = useState(
    form.promotionalApr !== "" || form.promotionalEndsOn !== "",
  );
  const [editingOwnBalance, setEditingOwnBalance] = useState(false);
  const [editingOwnLimit, setEditingOwnLimit] = useState(false);
  const balanceLocked = following && !editingOwnBalance;
  const limitFollowed =
    following && creditLimitSource !== null && creditLimitSource !== "Manual";
  const limitLocked = limitFollowed && !editingOwnLimit;

  /**
   * Saves the open balance as the person's value, then locks the field again.
   * The rest of the form is left as it is.
   */
  async function saveOwnBalance() {
    const saved = await onSaveOwnBalance(form.balance, form.balanceAsOf);
    if (saved) {
      setEditingOwnBalance(false);
    }
  }

  /**
   * Saves the open credit limit as the person's value, then locks the field again.
   * The rest of the form is left as it is.
   */
  async function saveOwnLimit() {
    const saved = await onSaveOwnCreditLimit(form.creditLimit);
    if (saved) {
      setEditingOwnLimit(false);
    }
  }

  /**
   * Explains which currency the amounts use, and that a blank term stays unknown.
   * An edit keeps the currency from when the debt was created.
   */
  function currencyNote() {
    const currency =
      isEditing && storedCurrency ? storedCurrency : planningCurrency;
    return `Amounts use ${currency}. Leave a term blank when you don't know it. A blank is not stored as zero.`;
  }

  return (
    <Form className="flex flex-col gap-4" onSubmit={onSubmit}>
      <p className="text-sm text-muted-foreground">{currencyNote()}</p>
      <div className="flex flex-col gap-4">
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Name
          <Input
            value={form.name}
            onChange={(event) =>
              onChange({ ...form, name: event.target.value })
            }
            maxLength={80}
            autoComplete="off"
          />
        </label>

        <div className="flex flex-col gap-1.5 text-sm font-medium">
          Type
          <Select
            title="Type"
            value={form.kind}
            onChange={(value) => {
              if (isDebtKind(value)) {
                onChange({
                  ...form,
                  kind: value,
                  creditLimit: value === "Revolving" ? form.creditLimit : "",
                  remainingTermMonths:
                    value === "Installment" ? form.remainingTermMonths : "",
                });
              }
            }}
            options={debtKindOptions.map((option) => ({
              value: option.value,
              label: option.label,
            }))}
            placeholder="Select"
            onOpenChange={onPickerOpenChange}
          />
          <span className="font-normal text-muted-foreground">
            A card or line of credit is revolving. A loan with a set number of
            payments is installment.
          </span>
        </div>

        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Balance
          <Input
            value={form.balance}
            onChange={(event) =>
              onChange({ ...form, balance: event.target.value })
            }
            inputMode="decimal"
            autoComplete="off"
            placeholder="Optional"
            disabled={balanceLocked}
          />
          <span className="font-normal text-muted-foreground">
            {balanceNote(following, editingOwnBalance, balanceSource)}
          </span>
        </label>

        <div className="flex flex-col gap-1.5 text-sm font-medium">
          Balance as of
          <DateField
            title="Balance as of"
            value={form.balanceAsOf}
            onChange={(balanceAsOf) => onChange({ ...form, balanceAsOf })}
            min="2000-01-01"
            max="2100-12-31"
            onOpenChange={onPickerOpenChange}
            disabled={balanceLocked}
          />
          <span className="font-normal text-muted-foreground">
            The date that balance was true. Required when you enter a balance.
          </span>
        </div>

        {following ? (
          editingOwnBalance ? (
            <Button
              type="button"
              variant="outline"
              className="min-h-11"
              disabled={isSaving}
              onClick={() => void saveOwnBalance()}
            >
              Save my balance
            </Button>
          ) : (
            <Button
              type="button"
              variant="outline"
              className="min-h-11"
              disabled={isSaving}
              onClick={() => setEditingOwnBalance(true)}
            >
              Enter my own value
            </Button>
          )
        ) : null}

        <div className="flex flex-col gap-1.5 text-sm font-medium">
          Linked account
          <Select
            title="Linked account"
            value={form.accountId}
            onChange={(value) => onChange({ ...form, accountId: value })}
            options={accountChoices(accounts, form.accountId, savedAccountName)}
            onOpenChange={onPickerOpenChange}
            disabled={following}
          />
          <span className="font-normal text-muted-foreground">
            {following
              ? "Stop following before choosing a different account."
              : "Optional. The account balance is not copied or changed."}
          </span>
        </div>

        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Interest rate (APR)
          <Input
            value={form.apr}
            onChange={(event) => onChange({ ...form, apr: event.target.value })}
            inputMode="decimal"
            autoComplete="off"
            placeholder="Optional"
          />
          <span className="font-normal text-muted-foreground">
            The yearly rate, such as 19.99. Zero means a 0% rate.
          </span>
        </label>

        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Minimum
          <Input
            value={form.minimumPayment}
            onChange={(event) =>
              onChange({ ...form, minimumPayment: event.target.value })
            }
            inputMode="decimal"
            autoComplete="off"
            placeholder="Optional"
          />
          <span className="font-normal text-muted-foreground">
            The minimum due for one payment.
          </span>
        </label>

        <div className="flex flex-col gap-1.5 text-sm font-medium">
          Due date
          <DateField
            title="Due date"
            value={form.nextDueDate}
            onChange={(nextDueDate) => onChange({ ...form, nextDueDate })}
            min="2000-01-01"
            max="2100-12-31"
            onOpenChange={onPickerOpenChange}
          />
        </div>

        {form.kind === "Revolving" ? (
          <>
            <label className="flex flex-col gap-1.5 text-sm font-medium">
              Credit limit
              <Input
                value={form.creditLimit}
                onChange={(event) =>
                  onChange({ ...form, creditLimit: event.target.value })
                }
                inputMode="decimal"
                autoComplete="off"
                placeholder="Optional"
                disabled={limitLocked}
              />
              <span className="font-normal text-muted-foreground">
                {creditLimitNote(
                  following,
                  limitFollowed,
                  editingOwnLimit,
                  creditLimitSource,
                )}
              </span>
            </label>
            {limitFollowed ? (
              editingOwnLimit ? (
                <Button
                  type="button"
                  variant="outline"
                  className="min-h-11"
                  disabled={isSaving}
                  onClick={() => void saveOwnLimit()}
                >
                  Save my limit
                </Button>
              ) : (
                <Button
                  type="button"
                  variant="outline"
                  className="min-h-11"
                  disabled={isSaving}
                  onClick={() => setEditingOwnLimit(true)}
                >
                  Enter my own limit
                </Button>
              )
            ) : null}
          </>
        ) : null}

        {form.kind === "Installment" ? (
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Months left
            <Input
              value={form.remainingTermMonths}
              onChange={(event) =>
                onChange({ ...form, remainingTermMonths: event.target.value })
              }
              inputMode="numeric"
              autoComplete="off"
              placeholder="Optional"
            />
            <span className="font-normal text-muted-foreground">
              How many months are left. Leave blank if you don&apos;t know.
            </span>
          </label>
        ) : null}
      </div>

      <details
        className="rounded-lg border border-border"
        open={showPromotion}
        onToggle={(event) => setShowPromotion(event.currentTarget.open)}
      >
        <summary className="min-h-11 cursor-pointer px-3 py-3 text-sm font-medium focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/50">
          Promotional terms
        </summary>
        <div className="flex flex-col gap-4 border-t border-border px-3 py-3">
          <p className="text-sm text-muted-foreground">
            Fill in only what you know. A blank promo rate is not stored as 0%.
          </p>
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Promo APR
            <Input
              value={form.promotionalApr}
              onChange={(event) =>
                onChange({ ...form, promotionalApr: event.target.value })
              }
              inputMode="decimal"
              autoComplete="off"
              placeholder="Optional"
            />
          </label>
          <div className="flex flex-col gap-1.5 text-sm font-medium">
            Promo ends
            <DateField
              title="Promo ends"
              value={form.promotionalEndsOn}
              onChange={(promotionalEndsOn) =>
                onChange({ ...form, promotionalEndsOn })
              }
              min="2000-01-01"
              max="2100-12-31"
              onOpenChange={onPickerOpenChange}
            />
          </div>
        </div>
      </details>

      <Button type="submit" className="min-h-11" disabled={isSaving}>
        {isSaving ? "Saving…" : isEditing ? "Save changes" : "Add debt"}
      </Button>
    </Form>
  );
}

/**
 * The note under a followed balance.
 * Saving the form does not write that balance. Enter my own value does.
 */
function balanceNote(
  following: boolean,
  editingOwnBalance: boolean,
  balanceSource: DebtFieldSource | null,
) {
  if (!following) {
    return "What you owe. Leave blank if you don't know it.";
  }

  if (editingOwnBalance) {
    return "Save my balance keeps this amount. Save changes keeps the other terms.";
  }

  if (balanceSource === "Override") {
    return "This is your balance. The connection does not replace it.";
  }

  return "This balance follows the connected account.";
}

/**
 * The note under a credit limit.
 * A followed limit is saved on its own. A limit the connection did not provide saves with the other terms.
 */
function creditLimitNote(
  following: boolean,
  limitFollowed: boolean,
  editingOwnLimit: boolean,
  creditLimitSource: DebtFieldSource | null,
) {
  if (!following || !limitFollowed) {
    if (following) {
      return "The connection did not provide a credit limit. Yours stays in use.";
    }

    return "How much of the limit is in use is shown when both the balance and the limit are filled in.";
  }

  if (editingOwnLimit) {
    return "Save my limit keeps this amount. Save changes keeps the other terms.";
  }

  if (creditLimitSource === "Override") {
    return "This is your credit limit. The connection does not replace it.";
  }

  return "This credit limit follows the connected account.";
}

/**
 * Account choices for the debt form.
 * Open accounts are listed. A saved account that is no longer open stays selectable and is marked archived.
 */
function accountChoices(
  accounts: AccountDto[],
  accountId: string,
  savedAccountName: string | null,
) {
  const open = accounts
    .filter((account) => account.archivedAt === null)
    .map((account) => ({
      value: account.id,
      label: account.name,
    }));

  const choices = [{ value: "", label: "No account" }, ...open];
  if (accountId && !open.some((option) => option.value === accountId)) {
    choices.push({
      value: accountId,
      label: savedAccountName
        ? `${savedAccountName} (archived)`
        : "Archived account",
    });
  }

  return choices;
}
