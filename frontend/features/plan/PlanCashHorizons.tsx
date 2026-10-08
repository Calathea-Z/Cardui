import { CircleAlert } from "lucide-react";
import { cn } from "@/lib/utils";
import type { PlanCashHorizonCopy } from "./planCopy";

type PlanCashHorizonsProps = {
  cards: PlanCashHorizonCopy[];
};

/**
 * The 6, 12, and 18 month cards: ending cash, the lowest point, and the minimums still due.
 * A card whose cash goes below zero gets the warning border, and its lowest point an icon and a spoken "Warning".
 */
export function PlanCashHorizons({ cards }: PlanCashHorizonsProps) {
  return (
    <ul className="grid gap-3 sm:grid-cols-3">
      {cards.map((card) => (
        <li
          key={card.key}
          className={cn(
            "rounded-md border border-border p-3",
            card.warning && "border-warning/40",
          )}
        >
          <p className="text-sm font-medium text-foreground">{card.title}</p>
          <p className="text-xs text-muted-foreground">{card.through}</p>
          <dl className="mt-2 flex flex-col gap-1.5">
            <div>
              <dt className="text-xs text-muted-foreground">Ending cash</dt>
              <dd className="text-base font-semibold text-foreground tabular-nums">
                {card.ending}
              </dd>
            </div>
            <div>
              <dt className="text-xs text-muted-foreground">Lowest</dt>
              <dd
                className={cn(
                  "flex items-center gap-1.5 text-sm tabular-nums",
                  card.warning ? "text-warning" : "text-foreground",
                )}
              >
                {card.warning ? (
                  <>
                    <CircleAlert aria-hidden className="size-4 shrink-0" />
                    <span className="sr-only">Warning: </span>
                  </>
                ) : null}
                {card.lowest}
              </dd>
            </div>
            <div>
              <dt className="text-xs text-muted-foreground">
                Minimums still due
              </dt>
              <dd className="text-sm text-foreground tabular-nums">
                {card.minimums}
              </dd>
            </div>
          </dl>
        </li>
      ))}
    </ul>
  );
}
