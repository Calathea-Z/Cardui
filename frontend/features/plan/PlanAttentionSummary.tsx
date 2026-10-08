import Link from "next/link";
import { CheckCircle2, ChevronRight, CircleAlert } from "lucide-react";
import { cn } from "@/lib/utils";
import type { PlanFinishItem } from "./planCopy";
import type { PlanReadinessCopy } from "./planReadiness";

type PlanAttentionSummaryProps = {
  readiness: PlanReadinessCopy;
  debtItems: PlanFinishItem[];
};

/**
 * Summarizes Plan input quality without repeating the primary status warning.
 * Remaining issues, healthy inputs, and named debt blockers stay available in one closed disclosure.
 */
export function PlanAttentionSummary({
  readiness,
  debtItems,
}: PlanAttentionSummaryProps) {
  const warningCount = readiness.rows.filter((row) => row.warning).length;
  const healthyCount = readiness.rows.length - warningCount;
  const remainingRows = readiness.rows.filter(
    (row) => row.key !== readiness.next?.key,
  );

  return (
    <section
      className="app-panel self-start"
      aria-labelledby="plan-attention-title"
    >
      <div className="app-panel-header px-4 py-3">
        <div className="flex items-center justify-between gap-3">
          <h2 id="plan-attention-title" className="app-section-title">
            Attention
          </h2>
          <span
            className={cn(
              "rounded-full px-2 py-0.5 text-xs font-medium",
              warningCount > 0
                ? "bg-warning/10 text-warning"
                : "bg-muted text-foreground",
            )}
          >
            {warningCount > 0
              ? `${warningCount} ${warningCount === 1 ? "area" : "areas"}`
              : "Ready"}
          </span>
        </div>
        <p className="app-section-meta">
          {readiness.next
            ? "The most important action is shown with your plan status."
            : "The current inputs are ready for review."}
        </p>
      </div>
      <details className="group">
        <summary className="flex min-h-11 cursor-pointer items-center justify-between gap-3 px-4 py-3 text-sm font-medium text-foreground focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none focus-visible:ring-inset">
          <span>
            Review {Math.max(warningCount - (readiness.next ? 1 : 0), 0)} other{" "}
            {warningCount - (readiness.next ? 1 : 0) === 1 ? "issue" : "issues"}{" "}
            and {healthyCount} healthy {healthyCount === 1 ? "input" : "inputs"}
          </span>
          <ChevronRight
            aria-hidden
            className="size-4 shrink-0 transition-transform group-open:rotate-90"
          />
        </summary>
        <div className="border-t border-border">
          {debtItems.length > 0 ? (
            <div className="border-b border-border/70 px-4 py-3">
              <p className="text-xs font-semibold text-foreground">
                Debt details
              </p>
              <ul className="mt-2 flex flex-col gap-2">
                {debtItems.map((item) => (
                  <li key={item.key}>
                    <Link
                      href="/debts"
                      className="flex min-h-11 items-start gap-2 rounded-md py-2 text-sm focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
                    >
                      <CircleAlert
                        aria-hidden
                        className="mt-0.5 size-4 shrink-0 text-warning"
                      />
                      <span>
                        <span className="block font-medium text-foreground">
                          {item.name}
                        </span>
                        <span className="block text-xs text-muted-foreground">
                          {item.fix} {item.action}.
                        </span>
                      </span>
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ) : null}
          <ul className="divide-y divide-border/70">
            {remainingRows.map((row) => (
              <li
                key={row.key}
                className="flex flex-col gap-2 px-4 py-3 sm:flex-row sm:items-start sm:justify-between"
              >
                <div className="flex min-w-0 items-start gap-2">
                  {row.warning ? (
                    <CircleAlert
                      aria-hidden
                      className="mt-0.5 size-4 shrink-0 text-warning"
                    />
                  ) : (
                    <CheckCircle2
                      aria-hidden
                      className="mt-0.5 size-4 shrink-0 text-success"
                    />
                  )}
                  <div>
                    <p className="text-sm font-medium text-foreground">
                      {row.title}
                    </p>
                    <p className="mt-0.5 text-xs text-muted-foreground">
                      {row.detail}
                    </p>
                  </div>
                </div>
                <Link
                  href={row.href}
                  className="min-h-11 shrink-0 self-start py-2 text-sm font-medium text-primary underline sm:min-h-0 sm:py-0"
                >
                  {row.linkLabel}
                </Link>
              </li>
            ))}
          </ul>
        </div>
      </details>
    </section>
  );
}
