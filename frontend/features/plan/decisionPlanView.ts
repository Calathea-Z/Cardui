import type {
  DecisionPlanActionKind,
  DecisionPlanDto,
  PlanVersionDto,
} from "@/lib/api/types";

export const decisionPlanLabels = [
  "Current facts",
  "Trying changes — not saved",
  "Saved draft",
  "Accepted plan",
  "History",
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
 * The print heading for the version on screen.
 * Status, version, and date name that copy. A stored version also names the day it was recorded.
 */
export type PlanVersionPrintLines = {
  status: string;
  version: string;
  date: string;
  recorded: string | null;
};

/**
 * A next step that opens an editor or a Plan detail.
 * The href returns to Plan. A view stays on this page.
 */
export type DecisionActionTarget = {
  label: string;
  href?: string;
  view?: "cash" | "debt";
};

/**
 * Chooses the plan on screen and the leading action.
 * A viewed version is that stored copy. Otherwise the live preview is shown, and it is a draft or accepted only when it matches one.
 * `matchesCurrentFacts` is true when that preview has no scenario change.
 */
export function decisionPlanChrome(
  preview: DecisionPlanDto | null,
  versions: PlanVersionDto[],
  viewedId: string | null,
  matchesCurrentFacts = false,
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
      label: workingLabel(matchesCurrentFacts),
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
      label: "Saved draft",
      action: "accept",
      note: acceptNote(),
      replacesDraftId: draft.id,
      acceptId: draft.id,
    };
  }

  if (!draft && accepted && sameDecisionPlan(preview, accepted.plan)) {
    return {
      plan: preview,
      version: accepted,
      label: "Accepted plan",
      action: "print",
      note: "This is the accepted plan. Later edits do not change this version.",
      replacesDraftId: null,
      acceptId: null,
    };
  }

  return {
    plan: preview,
    version: null,
    label: workingLabel(matchesCurrentFacts),
    action: "save",
    note: unsavedNote(draft !== null, accepted !== null),
    replacesDraftId: draft?.id ?? null,
    acceptId: null,
  };
}

/**
 * Names the working preview when it is not a stored version.
 */
function workingLabel(matchesCurrentFacts: boolean): DecisionPlanLabel {
  return matchesCurrentFacts ? "Current facts" : "Trying changes — not saved";
}

/**
 * What accept does before the person confirms it.
 * The older accepted plan stays in history. Neither save nor accept moves money.
 */
function acceptNote() {
  return "Accepting replaces the accepted plan and keeps the older copy in history. Saving and accepting only record this decision. Cardui does not schedule a payment or change an account.";
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
      label: "Saved draft",
      action: "accept",
      note: differs
        ? "You are reading the saved draft. Return to the working preview to save a different one."
        : acceptNote(),
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
      label: "Accepted plan",
      action: current ? "print" : "back",
      note: "You are reading the accepted plan. Later edits do not change it.",
      replacesDraftId: null,
      acceptId: null,
    };
  }

  return {
    plan: viewed.plan,
    version: viewed,
    label: "History",
    action: "back",
    note: "You are reading an earlier version. A later version replaced it.",
    replacesDraftId: null,
    acceptId: null,
  };
}

/**
 * The status, version, and date printed for the plan on screen.
 * A working preview has no recorded day. A stored version uses the day it was accepted, or the day it was saved.
 */
export function planVersionPrintLines(
  chrome: DecisionPlanChrome,
  formatDate: (value: string) => string,
): PlanVersionPrintLines {
  const recordedSource = chrome.version
    ? (chrome.version.acceptedAt ?? chrome.version.createdAt)
    : null;
  const asOf = chrome.plan?.asOf ?? null;

  return {
    status: chrome.label,
    version: chrome.version
      ? `Version ${chrome.version.number}`
      : "Working preview",
    date: asOf ? `As of ${formatDate(asOf.slice(0, 10))}` : "Date unavailable",
    recorded: recordedSource
      ? `Recorded ${formatDate(recordedSource.slice(0, 10))}`
      : null,
  };
}

/**
 * Where one ranked action can be reviewed or edited.
 * The scenario bar already edits extra payments and windfalls, so those actions have no second control.
 */
export function decisionActionTarget(
  kind: DecisionPlanActionKind,
): DecisionActionTarget | null {
  switch (kind) {
    case "CoverCashShortfall":
      return { label: "Review cash shortfall", view: "cash" };
    case "AddStartingCash":
    case "RefreshBalances":
      return { label: "Review accounts", href: editorHref("/accounts") };
    case "AddDebt":
    case "FinishDebtTerms":
      return { label: "Edit debts", href: editorHref("/debts") };
    case "AddIncome":
    case "HoldIncomeLoss":
      return { label: "Edit income", href: editorHref("/income") };
    case "PayFirstDebt":
    case "UseFreedPayment":
      return { label: "Review debt payoff", view: "debt" };
    case "ConfirmBills":
      return { label: "Edit bills", href: editorHref("/bills") };
    case "SetFlexibleSpending":
      return { label: "Edit living costs", href: editorHref("/living") };
    case "ChooseCashToKeep":
      return { label: "Edit savings", href: editorHref("/savings") };
    default:
      return null;
  }
}

/**
 * An editor route that returns to Plan.
 * This matches the plan return query without importing the helper into this module.
 */
function editorHref(path: string) {
  return `${path}?from=plan`;
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
