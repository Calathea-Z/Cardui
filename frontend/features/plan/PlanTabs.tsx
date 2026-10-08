"use client";

import { useRef, type KeyboardEvent } from "react";
import { cn } from "@/lib/utils";
import { movePlanTab, planTabs, type PlanTab } from "./planNavigation";

type PlanTabsProps = {
  value: PlanTab;
  onChange: (value: PlanTab) => void;
};

const labels: Record<PlanTab, string> = {
  overview: "Overview",
  cash: "Cash outlook",
  debt: "Debt payoff",
};

/**
 * Switches among the three Plan views without changing the shared scenario.
 * Arrow keys wrap, while Home and End move to the first and last tab.
 */
export function PlanTabs({ value, onChange }: PlanTabsProps) {
  const buttons = useRef<Partial<Record<PlanTab, HTMLButtonElement | null>>>(
    {},
  );

  /**
   * Selects and focuses the tab requested by the standard horizontal-tab keys.
   */
  function handleKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
    if (
      event.key !== "ArrowLeft" &&
      event.key !== "ArrowRight" &&
      event.key !== "Home" &&
      event.key !== "End"
    ) {
      return;
    }

    event.preventDefault();
    const next = movePlanTab(value, event.key);
    onChange(next);
    buttons.current[next]?.focus();
  }

  return (
    <nav
      aria-label="Plan views"
      className="sticky top-[calc(3.75rem+env(safe-area-inset-top))] z-20 -mx-4 border-y border-border bg-background/95 px-4 py-2 backdrop-blur-sm md:static md:mx-0 md:border-0 md:bg-transparent md:px-0 md:py-0 md:backdrop-blur-none"
    >
      <div
        role="tablist"
        aria-label="Plan views"
        className="grid w-full grid-cols-3 rounded-lg border border-border bg-muted p-0.5 md:w-fit md:min-w-[28rem]"
      >
        {planTabs.map((tab) => {
          const selected = tab === value;
          return (
            <button
              key={tab}
              ref={(node) => {
                buttons.current[tab] = node;
              }}
              id={`plan-tab-${tab}`}
              type="button"
              role="tab"
              tabIndex={selected ? 0 : -1}
              aria-selected={selected}
              aria-controls="plan-panel"
              onClick={() => onChange(tab)}
              onKeyDown={handleKeyDown}
              className={cn(
                "min-h-11 rounded-md px-2 text-sm font-medium transition-colors motion-reduce:transition-none md:min-h-9 md:px-4",
                "focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none",
                selected
                  ? "border border-border bg-card text-foreground shadow-xs"
                  : "border border-transparent text-muted-foreground hover:text-foreground",
              )}
            >
              {labels[tab]}
            </button>
          );
        })}
      </div>
    </nav>
  );
}
