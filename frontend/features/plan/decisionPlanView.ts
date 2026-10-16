import type { DecisionPlanDto, PlanVersionDto } from "@/lib/api/types";

export const decisionPlanLabels = [
  "Draft",
  "Accepted",
  "Superseded",
  "Not saved",
] as const;

export type DecisionPlanLabel = (typeof decisionPlanLabels)[number];

export const decisionPlanChromeActions = [
  "save",
  "accept",
  "print",
  "back",
] as const;

export type DecisionPlanChromeAction =
  (typeof decisionPlanChromeActions)[number];

/**
 * What Your plan should show and which action leads.
 * `replacesDraftId` is the open draft a save must name. Null means there is no open draft.
 * `acceptId` is the draft an accept request names.
 */
export type DecisionPlanChrome = {
  plan: DecisionPlanDto | null;
  version: PlanVersionDto | null;
  label: DecisionPlanLabel;
  action: DecisionPlanChromeAction;
  note: string;
  replacesDraftId: string | null;
  acceptId: string | null;
};

/**
 * True when two decision plans are the same document.
 * Property order follows the API, so a preview and the draft it saved compare equal.
 */
export function sameDecisionPlan(
  left: DecisionPlanDto,
  right: DecisionPlanDto,
) {
  return JSON.stringify(left) === JSON.stringify(right);
}

/**
 * Chooses the plan on screen and the leading action.
 * A viewed version is that stored copy. Otherwise the live preview is shown, and it is a draft or accepted only when it matches one.
 */
export function decisionPlanChrome(
  preview: DecisionPlanDto | null,
  versions: PlanVersionDto[],
  viewedId: string | null,
): DecisionPlanChrome {
  const draft = versions.find((version) => version.status === "Draft") ?? null;
  const accepted =
    versions.find((version) => version.status === "Accepted") ?? null;
  const viewed = viewedId
    ? (versions.find((version) => version.id === viewedId) ?? null)
    : null;

  if (viewed) {
    return viewedChrome(viewed, preview, draft);
  }

  if (!preview) {
    return {
      plan: null,
      version: null,
      label: "Not saved",
      action: "save",
      note: "Nothing is saved yet.",
      replacesDraftId: null,
      acceptId: null,
    };
  }

  if (draft && sameDecisionPlan(preview, draft.plan)) {
    return {
      plan: preview,
      version: draft,
      label: "Draft",
      action: "accept",
      note: "This draft matches the preview. Accept it to make it the plan you are keeping.",
      replacesDraftId: draft.id,
      acceptId: draft.id,
    };
  }

  if (!draft && accepted && sameDecisionPlan(preview, accepted.plan)) {
    return {
      plan: preview,
      version: accepted,
      label: "Accepted",
      action: "print",
      note: "This is the accepted plan. Later edits do not change this version.",
      replacesDraftId: null,
      acceptId: null,
    };
  }

  return {
    plan: preview,
    version: null,
    label: "Not saved",
    action: "save",
    note: unsavedNote(draft !== null, accepted !== null),
    replacesDraftId: draft?.id ?? null,
    acceptId: null,
  };
}

/**
 * The leading action for a version opened from history.
 * Accept stays available on the open draft. An accepted plan that matches the preview can be printed.
 */
function viewedChrome(
  viewed: PlanVersionDto,
  preview: DecisionPlanDto | null,
  draft: PlanVersionDto | null,
): DecisionPlanChrome {
  if (viewed.status === "Draft") {
    const differs = preview !== null && !sameDecisionPlan(preview, viewed.plan);
    return {
      plan: viewed.plan,
      version: viewed,
      label: "Draft",
      action: "accept",
      note: differs
        ? "You are reading the open draft. Show the current preview to save a different one."
        : "This draft matches the preview. Accept it to make it the plan you are keeping.",
      replacesDraftId: draft?.id ?? null,
      acceptId: viewed.id,
    };
  }

  if (viewed.status === "Accepted") {
    const current =
      preview !== null &&
      draft === null &&
      sameDecisionPlan(preview, viewed.plan);
    return {
      plan: viewed.plan,
      version: viewed,
      label: "Accepted",
      action: current ? "print" : "back",
      note: "You are reading the accepted plan. Later edits do not change it.",
      replacesDraftId: null,
      acceptId: null,
    };
  }

  return {
    plan: viewed.plan,
    version: viewed,
    label: "Superseded",
    action: "back",
    note: "You are reading an earlier version. A later version replaced it.",
    replacesDraftId: null,
    acceptId: null,
  };
}

/**
 * Why the preview is not the kept plan yet.
 */
function unsavedNote(hasDraft: boolean, hasAccepted: boolean) {
  if (hasDraft) {
    return "This preview is not saved. Saving replaces the open draft. An accepted plan stays until you accept a new one.";
  }

  if (hasAccepted) {
    return "This preview is not saved. The accepted plan stays until you accept a new draft.";
  }

  return "Nothing is saved yet. A draft keeps this preview without changing your accounts.";
}
