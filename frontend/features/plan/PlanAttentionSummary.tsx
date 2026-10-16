import Link from "next/link";
import { ChevronRight, CircleAlert } from "lucide-react";
import type { PlanFinishItem } from "./planCopy";
import { planReturnHref } from "./planReturn";
import type { PlanReadinessCopy } from "./planReadiness";

type PlanAttentionSummaryProps = {
  readiness: PlanReadinessCopy;
  debtItems: PlanFinishItem[];
  primaryKey: string;
};

/**
 * Lists the setup issues that are not already the safety action.
 * Healthy checks stay out of this list. A view with no remaining issues renders nothing.
 */
export function PlanAttentionSummary({
  readiness,
  debtItems,
  primaryKey,
}: PlanAttentionSummaryProps) {
  const issues = readiness.rows.filter(
    (row) => row.warning && row.key !== primaryKey,
  );
  if (issues.length === 0 && debtItems.length === 0) {
    return null;
  }

  return (
    <section
      className="app-panel self-start"
      aria-labelledby="plan-attention-title"
    >
      <div className="app-panel-header px-4 py-3">
        <div className="flex items-center justify-between gap-3">
          <h2 id="plan-attention-title" className="app-section-title">
            Other issues
          </h2>
          <span className="rounded-full bg-warning/10 px-2 py-0.5 text-xs font-medium text-warning">
            {issues.length + debtItems.length}
          </span>
        </div>
        <p className="app-section-meta">
          The next action above is the one to take first.
        </p>
      </div>
      <details className="group">
        <summary className="flex min-h-11 cursor-pointer items-center justify-between gap-3 px-4 py-3 text-sm font-medium text-foreground focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none focus-visible:ring-inset">
          <span>
            Review {issues.length + debtItems.length}{" "}
            {issues.length + debtItems.length === 1 ? "issue" : "issues"}
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
                      href={planReturnHref("/debts")}
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
            {issues.map((row) => (
              <li
                key={row.key}
                className="flex flex-col gap-2 px-4 py-3 sm:flex-row sm:items-start sm:justify-between"
              >
                <div className="flex min-w-0 items-start gap-2">
                  <CircleAlert
                    aria-hidden
                    className="mt-0.5 size-4 shrink-0 text-warning"
                  />
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
