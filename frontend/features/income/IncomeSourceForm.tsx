"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Form } from "@/components/ui/form";
import { DateField } from "@/components/ui/date-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import type { HouseholdContributorDto } from "@/lib/api/types";
import {
  emptyIncomeRaiseForm,
  type IncomeRaiseFormState,
  type IncomeSourceFormState,
} from "./incomeFormState";
import {
  incomeCadenceOptions,
  incomeReliabilityOptions,
  isIncomeCadence,
  isIncomeReliability,
} from "./incomeSourceOptions";

type IncomeSourceFormProps = {
  form: IncomeSourceFormState;
  contributors: HouseholdContributorDto[];
  planningCurrency: string;
  storedCurrency: string | null;
  isEditing: boolean;
  isSaving: boolean;
  onChange: (form: IncomeSourceFormState) => void;
  onPickerOpenChange: (open: boolean) => void;
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
};

/**
 * Form for one income source.
 * The payment fields come first. Gross pay, low, strong, and raises stay behind a disclosure.
 * Each amount is one payment. A raise is a later typical amount. Gross pay is not estimated.
 */
export function IncomeSourceForm({
  form,
  contributors,
  planningCurrency,
  storedCurrency,
  isEditing,
  isSaving,
  onChange,
  onPickerOpenChange,
  onSubmit,
}: IncomeSourceFormProps) {
  /**
   * Explains which currency the amount uses.
   * An edit keeps the currency from when the source was created.
   */
  function currencyNote() {
    if (!isEditing) {
      return `Amounts use ${planningCurrency}. Each amount is one payment.`;
    }

    if (storedCurrency && storedCurrency !== planningCurrency) {
      return `These amounts stay in ${storedCurrency}.`;
    }

    return "Each amount stays one payment.";
  }

  const hasOptionalDetails =
    form.grossPayAmount.trim() !== "" ||
    form.lowTakeHomeAmount.trim() !== "" ||
    form.strongTakeHomeAmount.trim() !== "" ||
    form.raises.length > 0;
  const [showMore, setShowMore] = useState(hasOptionalDetails);

  /**
   * Replaces one raise row.
   * The other rows stay as the user left them.
   */
  function updateRaise(index: number, patch: Partial<IncomeRaiseFormState>) {
    onChange({
      ...form,
      raises: form.raises.map((raise, raiseIndex) =>
        raiseIndex === index ? { ...raise, ...patch } : raise,
      ),
    });
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

        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Typical net pay
          <Input
            value={form.takeHomeAmount}
            onChange={(event) =>
              onChange({ ...form, takeHomeAmount: event.target.value })
            }
            inputMode="decimal"
            autoComplete="off"
          />
        </label>

        <div className="flex flex-col gap-1.5 text-sm font-medium">
          How often it is paid
          <Select
            title="How often it is paid"
            value={form.cadence}
            onChange={(value) => {
              if (isIncomeCadence(value)) {
                onChange({ ...form, cadence: value });
              }
            }}
            options={incomeCadenceOptions.map((option) => ({
              value: option.value,
              label: option.label,
            }))}
            placeholder="Select"
            onOpenChange={onPickerOpenChange}
          />
          {form.cadence === "Biweekly" ? (
            <span className="font-normal text-muted-foreground">
              Every 14 days from the next payment. Some months include three
              paychecks.
            </span>
          ) : null}
          {form.cadence === "Semimonthly" ? (
            <span className="font-normal text-muted-foreground">
              Two days each month, about fifteen days apart, based on the next
              payment date.
            </span>
          ) : null}
        </div>

        <div className="flex flex-col gap-1.5 text-sm font-medium">
          Next payment date
          <DateField
            title="Next payment date"
            value={form.nextPaymentDate}
            onChange={(nextPaymentDate) =>
              onChange({ ...form, nextPaymentDate })
            }
            min="2000-01-01"
            max="2100-12-31"
            onOpenChange={onPickerOpenChange}
          />
        </div>

        <div className="flex flex-col gap-1.5 text-sm font-medium">
          Contributor
          <Select
            title="Contributor"
            value={form.contributorId}
            onChange={(value) => onChange({ ...form, contributorId: value })}
            options={[
              { value: "", label: "No contributor" },
              ...contributors.map((contributor) => ({
                value: contributor.id,
                label: contributor.isVisible
                  ? contributor.name
                  : `${contributor.name} (hidden)`,
              })),
            ]}
            onOpenChange={onPickerOpenChange}
          />
        </div>

        <div className="flex flex-col gap-1.5 text-sm font-medium">
          Reliability
          <Select
            title="Reliability"
            value={form.reliability}
            onChange={(value) => {
              if (isIncomeReliability(value)) {
                onChange({ ...form, reliability: value });
              }
            }}
            options={incomeReliabilityOptions.map((option) => ({
              value: option.value,
              label: option.label,
            }))}
            placeholder="Select"
            onOpenChange={onPickerOpenChange}
          />
          <span className="font-normal text-muted-foreground">
            Steady is expected in full. Variable can change. Uncertain may not
            arrive.
          </span>
        </div>
      </div>

      <details
        className="rounded-lg border border-border"
        open={showMore}
        onToggle={(event) => setShowMore(event.currentTarget.open)}
      >
        <summary className="min-h-11 cursor-pointer px-3 py-3 text-sm font-medium focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/50">
          Gross, low, strong, and raises
        </summary>
        <div className="flex flex-col gap-4 border-t border-border px-3 py-3">
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Gross pay
            <Input
              value={form.grossPayAmount}
              onChange={(event) =>
                onChange({ ...form, grossPayAmount: event.target.value })
              }
              inputMode="decimal"
              autoComplete="off"
              placeholder="Optional"
            />
            <span className="font-normal text-muted-foreground">
              Before taxes and deductions. Leave blank when you only know net
              pay.
            </span>
          </label>

          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Low net pay
            <Input
              value={form.lowTakeHomeAmount}
              onChange={(event) =>
                onChange({ ...form, lowTakeHomeAmount: event.target.value })
              }
              inputMode="decimal"
              autoComplete="off"
              placeholder="Optional"
            />
            <span className="font-normal text-muted-foreground">
              A lean payment. It cannot be higher than typical.
            </span>
          </label>

          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Strong net pay
            <Input
              value={form.strongTakeHomeAmount}
              onChange={(event) =>
                onChange({
                  ...form,
                  strongTakeHomeAmount: event.target.value,
                })
              }
              inputMode="decimal"
              autoComplete="off"
              placeholder="Optional"
            />
            <span className="font-normal text-muted-foreground">
              A good payment. It cannot be lower than typical.
            </span>
          </label>

          <div className="flex flex-col gap-3">
            <div className="flex items-center justify-between gap-3">
              <span className="text-sm font-medium">Expected raises</span>
              <Button
                type="button"
                variant="outline"
                onClick={() =>
                  onChange({
                    ...form,
                    raises: [...form.raises, emptyIncomeRaiseForm()],
                  })
                }
              >
                Add raise
              </Button>
            </div>
            <span className="text-sm text-muted-foreground">
              The new typical net pay for one payment, from that date.
            </span>
            {form.raises.map((raise, index) => (
              <div
                key={raise.key}
                className="flex flex-col gap-2 sm:flex-row sm:items-end"
              >
                <div className="flex flex-1 flex-col gap-1.5 text-sm font-medium">
                  Raise date
                  <DateField
                    title={`Raise date ${index + 1}`}
                    value={raise.effectiveDate}
                    onChange={(effectiveDate) =>
                      updateRaise(index, { effectiveDate })
                    }
                    min={form.nextPaymentDate || "2000-01-01"}
                    max="2100-12-31"
                    onOpenChange={onPickerOpenChange}
                  />
                </div>
                <label className="flex flex-1 flex-col gap-1.5 text-sm font-medium">
                  New typical net pay
                  <Input
                    value={raise.takeHomeAmount}
                    onChange={(event) =>
                      updateRaise(index, { takeHomeAmount: event.target.value })
                    }
                    inputMode="decimal"
                    autoComplete="off"
                  />
                </label>
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() =>
                    onChange({
                      ...form,
                      raises: form.raises.filter(
                        (_, raiseIndex) => raiseIndex !== index,
                      ),
                    })
                  }
                >
                  Remove
                </Button>
              </div>
            ))}
          </div>
        </div>
      </details>

      <Button type="submit" className="min-h-11" disabled={isSaving}>
        {isSaving ? "Saving…" : isEditing ? "Save changes" : "Add income"}
      </Button>
    </Form>
  );
}
