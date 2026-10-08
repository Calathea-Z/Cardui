export const planTabs = ["overview", "cash", "debt"] as const;

export type PlanTab = (typeof planTabs)[number];

export type PlanTabKey = "ArrowLeft" | "ArrowRight" | "Home" | "End";

/**
 * Moves the selected Plan tab for horizontal arrow, Home, and End keys.
 * Arrow movement wraps so keyboard navigation never stops at an edge.
 */
export function movePlanTab(current: PlanTab, key: PlanTabKey): PlanTab {
  const index = planTabs.indexOf(current);
  if (key === "Home") {
    return planTabs[0];
  }

  if (key === "End") {
    return planTabs[planTabs.length - 1];
  }

  const offset = key === "ArrowRight" ? 1 : -1;
  return planTabs[(index + offset + planTabs.length) % planTabs.length];
}
