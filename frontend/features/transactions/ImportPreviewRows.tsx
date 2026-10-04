"use client";

import type { TransactionImportPreviewRowDto } from "@/lib/api/types";
import { formatImportAmount } from "./importCsvFormat";

type ImportPreviewRowsProps = {
  rows: TransactionImportPreviewRowDto[];
  included: Set<number>;
  currency: string | null;
  isWorking: boolean;
  onToggle: (lineNumber: number) => void;
};

/**
 * Lists preview rows so the household can include or skip each line.
 * A row that needs a fix cannot be checked.
 */
export function ImportPreviewRows({
  rows,
  included,
  currency,
  isWorking,
  onToggle,
}: ImportPreviewRowsProps) {
  return (
    <ul className="flex flex-col">
      {rows.map((row) => {
        const disabled = row.status === "Error";
        return (
          <li key={row.lineNumber} className="border-b border-border py-3">
            <label className="flex gap-3">
              <input
                type="checkbox"
                className="mt-1 size-4 accent-primary"
                checked={included.has(row.lineNumber)}
                disabled={disabled || isWorking}
                aria-label={`Include line ${row.lineNumber} ${row.name ?? ""}`}
                onChange={() => onToggle(row.lineNumber)}
              />
              <span className="min-w-0 flex-1">
                <span className="flex flex-wrap items-baseline justify-between gap-2">
                  <span className="font-medium">{row.name || "Untitled"}</span>
                  <span className="text-sm">
                    {formatImportAmount(row.amount, currency)}
                  </span>
                </span>
                <span className="mt-1 block text-xs text-muted-foreground">
                  Line {row.lineNumber}
                  {row.date ? ` · ${row.date}` : ""}
                  {row.categoryName ? ` · ${row.categoryName}` : ""}
                  {row.status === "Duplicate" ? " · Duplicate" : ""}
                  {row.status === "Error" ? " · Needs a fix" : ""}
                </span>
                {row.message ? (
                  <span className="mt-1 block text-xs text-muted-foreground">
                    {row.message}
                  </span>
                ) : null}
              </span>
            </label>
          </li>
        );
      })}
    </ul>
  );
}
