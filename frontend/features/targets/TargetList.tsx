"use client";

import { formatCurrency } from "@/features/accounts/formatCurrency";
import { cn } from "@/lib/utils";
import type { CategoryTargetLineDto } from "@/lib/api/types";
import {
  groupTargetLines,
  monthLabel,
  previousMonth,
  remainingText,
  rolloverDetail,
} from "./categoryTargetCopy";

type TargetListProps = {
  year: number;
  month: number;
  currency: string;
  lines: CategoryTargetLineDto[];
  busy: boolean;
  onEdit: (line: CategoryTargetLineDto, opener: HTMLElement) => void;
};

/**
 * Spending categories for the month, with spent and what is left.
 * A category with no target can still show spending. Uncategorized spending cannot be given a target.
 */
export function TargetList({
  year,
  month,
  currency,
  lines,
  busy,
  onEdit,
}: TargetListProps) {
  const format = (amount: number) => formatCurrency(amount, currency);
  const previous = previousMonth(year, month);
  const previousLabel = monthLabel(previous.year, previous.month);
  const groups = groupTargetLines(lines);

  if (lines.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        No spending categories yet.
      </p>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      {groups.map((group) => (
        <section key={group.name} className="flex flex-col gap-3">
          <h2 className="app-section-title">{group.name}</h2>
          <ul className="flex flex-col gap-3">
            {group.lines.map((line) => (
              <TargetRow
                key={line.categoryId ?? "uncategorized"}
                line={line}
                format={format}
                previousLabel={previousLabel}
                busy={busy}
                onEdit={onEdit}
              />
            ))}
          </ul>
        </section>
      ))}
    </div>
  );
}

type TargetRowProps = {
  line: CategoryTargetLineDto;
  format: (amount: number) => string;
  previousLabel: string;
  busy: boolean;
  onEdit: (line: CategoryTargetLineDto, opener: HTMLElement) => void;
};

/**
 * One category.
 * The row opens the target form when the category can hold one.
 */
function TargetRow({
  line,
  format,
  previousLabel,
  busy,
  onEdit,
}: TargetRowProps) {
  const detail = rolloverDetail(
    line.rolloverIn,
    line.rollover,
    previousLabel,
    format,
  );
  const remaining = remainingText(line.remaining, format);
  const over = line.remaining !== null && line.remaining < 0;

  const body = (
    <>
      <span
        className="mt-1.5 size-2.5 shrink-0 rounded-full"
        style={{ backgroundColor: line.color ?? "var(--muted-foreground)" }}
        aria-hidden
      />
      <span className="min-w-0 flex-1">
        <span className="block truncate font-medium">{line.name}</span>
        <span className="mt-0.5 block text-sm text-muted-foreground">
          Spent {format(line.spent)}
          {line.target !== null ? ` · Target ${format(line.target)}` : ""}
        </span>
        {detail ? (
          <span className="mt-0.5 block text-sm text-muted-foreground">
            {detail}
          </span>
        ) : null}
      </span>
      <span
        className={cn(
          "shrink-0 text-sm tabular-nums",
          over ? "text-destructive" : "text-foreground",
        )}
      >
        {remaining}
      </span>
    </>
  );

  return (
    <li>
      {line.canSetTarget ? (
        <button
          type="button"
          className="flex min-h-11 w-full items-start gap-3 rounded-lg border border-border/70 p-3 text-left transition hover:border-primary/45 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none disabled:opacity-50"
          disabled={busy}
          onClick={(event) => onEdit(line, event.currentTarget)}
        >
          {body}
        </button>
      ) : (
        <div className="flex items-start gap-3 rounded-lg border border-border/70 p-3">
          {body}
        </div>
      )}
    </li>
  );
}
