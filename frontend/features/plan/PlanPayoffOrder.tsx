import { formatCurrency } from "@/features/accounts/formatCurrency";
import { formatCalendarDate } from "@/features/debts/debtDisplay";
import type { PayoffRolloverKind } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import type { PlanPayoffRow } from "./planChartSeries";

type PlanPayoffOrderProps = {
  rows: PlanPayoffRow[];
  currency: string;
  kind: PayoffRolloverKind;
  pinnedDebtId: string | null;
  onHover: (debtId: string | null) => void;
  onTogglePin: (debtId: string) => void;
};

/**
 * Payoff order: one row per debt that pays off, with its chart swatch, payoff date, and the payment it removes.
 * Each row distinguishes money rolled to another debt from breathing room available for other uses.
 * Hovering or focusing a row highlights that debt's band. Pressing it keeps the highlight until pressed again.
 */
export function PlanPayoffOrder({
  rows,
  currency,
  kind,
  pinnedDebtId,
  onHover,
  onTogglePin,
}: PlanPayoffOrderProps) {
  return (
    <section className="app-panel" aria-labelledby="plan-order-title">
      <div className="app-panel-header px-4 py-3">
        <h2 id="plan-order-title" className="app-section-title">
          Payoff order
        </h2>
        <p className="app-section-meta">
          Select a debt to highlight its band in the balance chart.
        </p>
      </div>
      <ol className="divide-y divide-border/70">
        {rows.map((row, index) => (
          <li key={row.debtId}>
            <button
              type="button"
              aria-pressed={pinnedDebtId === row.debtId}
              onClick={() => onTogglePin(row.debtId)}
              onMouseEnter={() => onHover(row.debtId)}
              onMouseLeave={() => onHover(null)}
              onFocus={() => onHover(row.debtId)}
              onBlur={() => onHover(null)}
              className={cn(
                "flex min-h-11 w-full items-start gap-3 px-4 py-3 text-left hover:bg-muted/50",
                "focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none focus-visible:ring-inset",
                pinnedDebtId === row.debtId && "bg-muted/70",
              )}
            >
              <span className="w-4 shrink-0 text-xs text-muted-foreground tabular-nums">
                {index + 1}
              </span>
              <span
                aria-hidden
                className="size-3 shrink-0 rounded-sm"
                style={{ backgroundColor: row.color }}
              />
              <span className="min-w-0 flex-1">
                <span className="block text-sm font-medium break-words text-foreground">
                  {row.name}
                </span>
                <span className="mt-1 block text-xs text-muted-foreground">
                  Payoff date: {formatCalendarDate(row.paidOffOn)}
                </span>
                <span className="block text-xs text-muted-foreground">
                  Payment removed: {formatCurrency(row.minimum, currency)} a
                  month
                </span>
                <span className="block text-xs text-muted-foreground">
                  {row.breathingRoom > 0
                    ? `Available for other uses after this payoff: ${formatCurrency(row.breathingRoom, currency)} a month`
                    : kind === "Rollover"
                      ? "This payment rolls to the next debt; it is not available cash yet."
                      : "No additional breathing room is modeled after this payoff."}
                </span>
              </span>
            </button>
          </li>
        ))}
      </ol>
    </section>
  );
}
