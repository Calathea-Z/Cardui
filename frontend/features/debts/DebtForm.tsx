"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Form } from "@/components/ui/form";
import { DateField } from "@/components/ui/date-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import type { AccountDto } from "@/lib/api/types";
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
  onChange: (form: DebtFormState) => void;
  onPickerOpenChange: (open: boolean) => void;
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
};

/**
 * Form for one debt.
 * The type starts unset on a new debt. A blank term stays unknown and is not stored as zero.
 * The linked account can stay blank. A credit limit is asked for a revolving debt, and months left for an installment debt.
 * While following, the balance, its date, and the account stay as they are.
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
  onChange,
  onPickerOpenChange,
  onSubmit,
}: DebtFormProps) {
  const [showPromotion, setShowPromotion] = useState(
    form.promotionalApr !== "" || form.promotionalEndsOn !== "",
  );

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
            disabled={following}
          />
          <span className="font-normal text-muted-foreground">
            {following
              ? "This balance follows the connected account."
              : "What you owe. Leave blank if you don't know it."}
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
            disabled={following}
          />
          <span className="font-normal text-muted-foreground">
            The date that balance was true. Required when you enter a balance.
          </span>
        </div>

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
          APR
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
            />
            <span className="font-normal text-muted-foreground">
              How much of the limit is in use is shown when both the balance and
              the limit are filled in.
            </span>
          </label>
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
