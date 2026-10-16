export const planTabs = ["overview", "changes", "decision"] as const;

export type PlanTab = (typeof planTabs)[number];

export const planDetails = ["shortfall", "cash", "debt", "offer"] as const;

export type PlanDetail = (typeof planDetails)[number];

/**
 * A primary Plan view or one supporting detail opened from it.
 * Detail views stay in the same page so the working scenario is not reset.
 */
export type PlanView = PlanTab | PlanDetail;

export type PlanTabKey = "ArrowLeft" | "ArrowRight" | "Home" | "End";

/**
 * True when the view is the shortfall review, cash, debt, or the offer tool rather than a primary tab.
 */
export function isPlanDetail(view: PlanView): view is PlanDetail {
  return (planDetails as readonly string[]).includes(view);
}

/**
 * Moves among Overview, Try changes, and Your plan.
 * Arrow movement wraps. Home and End move to the first and last primary view.
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
