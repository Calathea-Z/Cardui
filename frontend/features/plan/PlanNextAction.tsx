import Link from "next/link";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { PlanSafetyAction } from "./planSafetyAction";

type PlanNextActionProps = {
  action: PlanSafetyAction;
  onOpenShortfall: () => void;
  onTryChange: () => void;
};

/**
 * The one next step beside the 30-day safety answer.
 * A shortfall opens the steady-pay review. An editor link returns to Plan.
 */
export function PlanNextAction({
  action,
  onOpenShortfall,
  onTryChange,
}: PlanNextActionProps) {
  return (
    <section className="app-panel p-4" aria-labelledby="plan-next-action">
      <h2 id="plan-next-action" className="app-section-title">
        Next action
      </h2>
      <p className="mt-2 text-sm font-medium text-foreground">{action.title}</p>
      <p className="mt-1 text-sm text-muted-foreground">{action.detail}</p>
      {action.view === "shortfall" ? (
        <button
          type="button"
          onClick={onOpenShortfall}
          className={cn(
            buttonVariants({ variant: "outline" }),
            "mt-3 min-h-11 bg-card",
          )}
        >
          {action.label}
        </button>
      ) : action.view === "changes" ? (
        <button
          type="button"
          onClick={onTryChange}
          className={cn(
            buttonVariants({ variant: "outline" }),
            "mt-3 min-h-11 bg-card",
          )}
        >
          {action.label}
        </button>
      ) : (
        <Link
          href={action.href ?? "/plan/setup"}
          className={cn(
            buttonVariants({ variant: "outline" }),
            "mt-3 min-h-11 bg-card",
          )}
        >
          {action.label}
        </Link>
      )}
    </section>
  );
}
