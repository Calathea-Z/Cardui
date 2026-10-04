"use client";

import {
  amountSignFromSelect,
  columnChoices,
  dateOrderFromSelect,
  type ImportColumnState,
} from "./importCsv";
import { ImportLabeledSelect } from "./ImportLabeledSelect";

type ImportColumnMappingProps = {
  headers: string[];
  columns: ImportColumnState;
  onChange: (next: ImportColumnState) => void;
  onOpenChange: (open: boolean) => void;
};

/**
 * Lets the household map columns and say how dates and amounts are written.
 * Debit is money out and credit is money in, and the positive-amount choice applies to a single amount column.
 */
export function ImportColumnMapping({
  headers,
  columns,
  onChange,
  onOpenChange,
}: ImportColumnMappingProps) {
  const requiredColumns = columnChoices(headers, false);
  const optionalColumns = columnChoices(headers, true);

  return (
    <div className="flex flex-col gap-5">
      <section className="flex flex-col gap-3">
        <div className="flex flex-col gap-1">
          <h3 className="text-sm font-medium">Which column is which</h3>
          <p className="text-xs font-normal text-muted-foreground">
            Choose the column from the sample that holds each part of the
            transaction.
          </p>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <ImportLabeledSelect
            label="Date is in"
            title="Date column"
            value={columns.dateColumn}
            onChange={(value) => onChange({ ...columns, dateColumn: value })}
            onOpenChange={onOpenChange}
            placeholder="Choose a column"
            options={requiredColumns}
          />
          <ImportLabeledSelect
            label="Name is in"
            title="Name column"
            value={columns.nameColumn}
            onChange={(value) => onChange({ ...columns, nameColumn: value })}
            onOpenChange={onOpenChange}
            placeholder="Choose a column"
            options={requiredColumns}
          />
          {columns.amountMode === "amount" ? (
            <ImportLabeledSelect
              label="Amount is in"
              title="Amount column"
              value={columns.amountColumn}
              onChange={(value) =>
                onChange({ ...columns, amountColumn: value })
              }
              onOpenChange={onOpenChange}
              placeholder="Choose a column"
              options={requiredColumns}
            />
          ) : (
            <>
              <ImportLabeledSelect
                label="Debit is in"
                title="Debit column"
                value={columns.debitColumn}
                onChange={(value) =>
                  onChange({ ...columns, debitColumn: value })
                }
                onOpenChange={onOpenChange}
                placeholder="Not used"
                options={optionalColumns}
              />
              <ImportLabeledSelect
                label="Credit is in"
                title="Credit column"
                value={columns.creditColumn}
                onChange={(value) =>
                  onChange({ ...columns, creditColumn: value })
                }
                onOpenChange={onOpenChange}
                placeholder="Not used"
                options={optionalColumns}
              />
            </>
          )}
          <ImportLabeledSelect
            label="Category is in"
            title="Category column"
            value={columns.categoryColumn}
            onChange={(value) =>
              onChange({ ...columns, categoryColumn: value })
            }
            onOpenChange={onOpenChange}
            placeholder="Not used"
            options={optionalColumns}
          />
          <ImportLabeledSelect
            label="Notes are in"
            title="Notes column"
            value={columns.notesColumn}
            onChange={(value) => onChange({ ...columns, notesColumn: value })}
            onOpenChange={onOpenChange}
            placeholder="Not used"
            options={optionalColumns}
          />
        </div>
      </section>
      <section className="flex flex-col gap-3">
        <div className="flex flex-col gap-1">
          <h3 className="text-sm font-medium">How to read the values</h3>
          <p className="text-xs font-normal text-muted-foreground">
            These choices describe how a date or an amount is written.
          </p>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <ImportLabeledSelect
            label="Amounts are written as"
            title="How amounts are written"
            value={columns.amountMode}
            onChange={(value) =>
              onChange({
                ...columns,
                amountMode: value === "split" ? "split" : "amount",
              })
            }
            onOpenChange={onOpenChange}
            options={[
              { value: "amount", label: "One amount column" },
              { value: "split", label: "Debit and credit columns" },
            ]}
          />
          <ImportLabeledSelect
            label="Dates are written"
            title="How dates are written"
            value={columns.dateOrder}
            onChange={(value) =>
              onChange({ ...columns, dateOrder: dateOrderFromSelect(value) })
            }
            onOpenChange={onOpenChange}
            hint="Month first reads 01/02/2026 as January 2. Day first reads it as February 1. A date like 2026-10-01 is the same either way."
            options={[
              { value: "MonthFirst", label: "Month first" },
              { value: "DayFirst", label: "Day first" },
            ]}
          />
          {columns.amountMode === "amount" ? (
            <ImportLabeledSelect
              label="A positive amount is"
              title="What a positive amount means"
              value={columns.amountSign}
              onChange={(value) =>
                onChange({
                  ...columns,
                  amountSign: amountSignFromSelect(value),
                })
              }
              onOpenChange={onOpenChange}
              hint="Money out was spent. Money in was received."
              options={[
                { value: "PositiveOut", label: "Money out" },
                { value: "PositiveIn", label: "Money in" },
              ]}
            />
          ) : null}
        </div>
        <p className="text-xs text-muted-foreground">
          Debit is money out and credit is money in. A CR or DR marker sets the
          direction on its own. Money in counts as income only when the category
          is Income.
        </p>
      </section>
    </div>
  );
}
