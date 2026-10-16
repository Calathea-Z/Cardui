import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { PlanDebtOutlookCopy } from "./planDebtOutlook";

type PlanDebtOutlookPanelProps = {
  outlook: PlanDebtOutlookCopy;
  onReviewDebt: () => void;
};

/**
 * The short debt outlook under next-30-day safety.
 * Payment priority and the first payoff date stay separate. A conditional note appears only when cash is short.
 */
export function PlanDebtOutlookPanel({
  outlook,
  onReviewDebt,
}: PlanDebtOutlookPanelProps) {
  return (
    <section
      className="app-panel p-4"
      aria-labelledby="plan-debt-outlook-title"
    >
      <h2 id="plan-debt-outlook-title" className="app-section-title">
        Debt outlook
      </h2>
      <dl className="mt-3 grid gap-3 sm:grid-cols-3">
        <div>
          <dt className="text-xs text-muted-foreground">Payment priority</dt>
          <dd className="mt-1 text-sm font-medium text-foreground">
            {outlook.priority}
          </dd>
        </div>
        <div>
          <dt className="text-xs text-muted-foreground">
            First expected payoff
          </dt>
          <dd className="mt-1 text-sm font-medium text-foreground">
            {outlook.payoff}
          </dd>
        </div>
        <div>
          <dt className="text-xs text-muted-foreground">After that payoff</dt>
          <dd className="mt-1 text-sm text-foreground">
            {outlook.availability}
          </dd>
        </div>
      </dl>
      {outlook.conditionalNote ? (
        <p className="mt-3 text-sm text-warning">{outlook.conditionalNote}</p>
      ) : null}
      <button
        type="button"
        onClick={onReviewDebt}
        className={cn(
          buttonVariants({ variant: "outline" }),
          "mt-4 min-h-11 bg-card",
        )}
      >
        Review debt payoff
      </button>
    </section>
  );
}
