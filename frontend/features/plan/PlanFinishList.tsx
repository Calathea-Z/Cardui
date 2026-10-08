import Link from "next/link";
import { ChevronRight, CircleAlert } from "lucide-react";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { PlanFinishItem } from "./planCopy";

type PlanFinishListProps = {
  items: PlanFinishItem[];
};

/**
 * Finish your plan: one row for each debt or currency that keeps the plan from projecting a payoff.
 * Each row is a warning with an icon, names what is missing, and its action label says what to do on Debts.
 * Every action label has the same width, so the column lines up whatever the label says.
 */
export function PlanFinishList({ items }: PlanFinishListProps) {
  return (
    <section
      className="app-panel border-warning/40"
      aria-labelledby="plan-finish-title"
    >
      <div className="app-panel-header px-4 py-3">
        <h2
          id="plan-finish-title"
          className="app-section-title flex items-center gap-2"
        >
          <CircleAlert aria-hidden className="size-4 text-warning" />
          Finish your plan
        </h2>
        <p className="app-section-meta">
          Fix these on Debts and your plan can project a payoff.
        </p>
      </div>
      <ul className="divide-y divide-border/70">
        {items.map((item) => (
          <li key={item.key}>
            <Link
              href="/debts"
              className="group flex min-h-11 items-center justify-between gap-3 px-4 py-3 hover:bg-muted/50 focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none focus-visible:ring-inset"
            >
              <span className="flex min-w-0 items-start gap-2.5">
                <CircleAlert
                  aria-hidden
                  className="mt-0.5 size-4 shrink-0 text-warning"
                />
                <span className="min-w-0">
                  <span className="block text-sm text-foreground">
                    {item.name}
                  </span>
                  <span className="block text-xs text-muted-foreground">
                    {item.fix}
                  </span>
                </span>
              </span>
              <span
                className={cn(
                  buttonVariants({ variant: "outline" }),
                  "w-36 justify-between text-primary group-hover:border-primary/45",
                )}
              >
                {item.action}
                <ChevronRight aria-hidden />
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
