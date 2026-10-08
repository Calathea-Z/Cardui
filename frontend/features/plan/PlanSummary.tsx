import Link from "next/link";
import { TriangleAlert } from "lucide-react";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { PlanSummaryCopy } from "./planCopy";
import type { PlanNextAction } from "./planReadiness";

type PlanSummaryProps = {
  summary: PlanSummaryCopy;
  nextAction: PlanNextAction | null;
  onReviewPayoff: () => void;
};

/**
 * The answer at the top of Plan: one sentence, one big figure, one contextual action, and quieter supporting figures.
 * The big figure is the only 2rem number on the page.
 * A warning summary gets the warning border, an icon, and a spoken "Warning" so color is not the only signal.
 */
export function PlanSummary({
  summary,
  nextAction,
  onReviewPayoff,
}: PlanSummaryProps) {
  return (
    <section
      className={cn("app-panel p-4", summary.warning && "border-warning/40")}
      aria-labelledby="plan-summary-title"
    >
      <h2 id="plan-summary-title" className="sr-only">
        Summary
      </h2>
      {summary.warning ? (
        <p className="flex items-start gap-2 rounded-md bg-warning/10 px-3 py-2 text-sm font-medium text-warning">
          <TriangleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
          <span>
            <span className="sr-only">Warning: </span>
            {summary.sentence}
          </span>
        </p>
      ) : (
        <p className="text-sm text-foreground">{summary.sentence}</p>
      )}
      <div className="mt-4 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-xs text-muted-foreground">{summary.figureLabel}</p>
          <p className="text-[2rem] leading-tight font-semibold tracking-tight text-foreground tabular-nums">
            {summary.figure}
          </p>
        </div>
        {summary.details.length > 0 ? (
          <dl className="flex gap-6">
            {summary.details.map((detail) => (
              <div key={detail.label}>
                <dt className="text-xs text-muted-foreground">
                  {detail.label}
                </dt>
                <dd className="text-sm font-medium text-foreground tabular-nums">
                  {detail.value}
                </dd>
              </div>
            ))}
          </dl>
        ) : null}
      </div>
      <div className="mt-4 flex flex-col gap-2 border-t border-border/70 pt-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p className="text-xs font-medium text-muted-foreground">
            What to review next
          </p>
          <p className="text-sm font-medium text-foreground">
            {nextAction?.title ?? "Review how your debt falls over time"}
          </p>
          {nextAction && nextAction.key !== "budget" ? (
            <p className="mt-0.5 text-xs text-muted-foreground">
              {nextAction.detail}
            </p>
          ) : null}
        </div>
        {nextAction ? (
          <Link
            href={nextAction.href}
            className={cn(
              buttonVariants({ variant: "outline" }),
              "min-h-11 shrink-0 bg-card",
            )}
          >
            {nextAction.label}
          </Link>
        ) : (
          <button
            type="button"
            onClick={onReviewPayoff}
            className={cn(
              buttonVariants({ variant: "outline" }),
              "min-h-11 shrink-0 bg-card",
            )}
          >
            Review debt payoff
          </button>
        )}
      </div>
    </section>
  );
}
