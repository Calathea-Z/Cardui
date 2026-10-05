"use client";

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
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
  onCancel: () => void;
};

/**
 * Form for one income source.
 * The payment comes first. Low, strong, and expected raises stay in an optional group.
 * Each amount is one net payment. A raise is a later typical amount.
 */
export function IncomeSourceForm({
  form,
  contributors,
  planningCurrency,
  storedCurrency,
  isEditing,
  isSaving,
  onChange,
  onSubmit,
  onCancel,
}: IncomeSourceFormProps) {
  /**
   * Explains which currency the amount uses.
   * An edit keeps the currency from when the source was created.
   */
  function currencyNote() {
    if (!isEditing) {
      return `New amounts use ${planningCurrency}. A biweekly paycheck stays on its own dates and is not turned into a monthly amount.`;
    }

    if (storedCurrency && storedCurrency !== planningCurrency) {
      return `These amounts stay in ${storedCurrency}.`;
    }

    return "Each amount stays one payment. It is not turned into a monthly average.";
  }

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
    <Form className="app-panel flex flex-col gap-4 p-4" onSubmit={onSubmit}>
      <div>
        <h2 className="app-section-title">
          {isEditing ? "Edit income source" : "Add income source"}
        </h2>
        <p className="app-section-meta">{currencyNote()}</p>
      </div>

      <fieldset className="min-w-0 border-0 p-0">
        <legend className="app-section-title float-none px-0">Payment</legend>
        <p className="app-section-meta mb-4">
          The net pay you expect for one payment, after taxes and deductions.
        </p>
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
            />
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
            />
            <span className="font-normal text-muted-foreground">
              Steady is expected in full. Variable can change. Uncertain may not
              arrive.
            </span>
          </div>
        </div>
      </fieldset>

      <fieldset className="min-w-0 rounded-lg border border-border bg-muted/40 p-4">
        <legend className="bg-card px-1 text-sm font-semibold text-foreground">
          Optional
        </legend>
        <div className="flex flex-col gap-4">
          <p className="text-sm text-muted-foreground">
            Low and strong pay, and expected raises. Leave these blank when this
            payment stays the same.
          </p>

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
              A lean payment. Leave this blank when pay does not drop. It cannot
              be higher than typical.
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
              A good payment. Leave this blank when pay does not rise. It cannot
              be lower than typical.
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
              The new typical net pay for one payment, starting on that date. It
              cannot be lower than the typical net pay, and it does not change
              the amounts above.
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
      </fieldset>

      <div className="flex gap-2">
        <Button type="submit" disabled={isSaving}>
          {isSaving ? "Saving…" : isEditing ? "Save changes" : "Add income"}
        </Button>
        {isEditing ? (
          <Button type="button" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
        ) : null}
      </div>
    </Form>
  );
}
