export const followPanels = ["suggestions", "accounts", "confirm"] as const;

export type FollowPanel = (typeof followPanels)[number];

export const followBackTargets = ["suggestions", "accounts", "close"] as const;

export type FollowBackTarget = (typeof followBackTargets)[number];

type FollowStep = {
  linked: boolean;
  picked: boolean;
  listRequested: boolean;
  suggestionCount: number;
};

/**
 * Which follow step is showing.
 * A debt that already names an eligible account confirms immediately.
 * Suggestions show until the person asks for the full list. No suggestions means that list.
 */
export function followPanel(step: FollowStep): FollowPanel {
  if (step.linked || step.picked) {
    return "confirm";
  }

  if (!step.listRequested && step.suggestionCount > 0) {
    return "suggestions";
  }

  return "accounts";
}

/**
 * Where Back goes from the step that is open.
 * Confirm returns to the list that was just used. That list returns to suggestions when there are any.
 * A linked account, and a list with nothing behind it, close the steps.
 */
export function followBackTarget(step: FollowStep): FollowBackTarget {
  if (step.picked) {
    if (step.suggestionCount > 0 && !step.listRequested) {
      return "suggestions";
    }

    return "accounts";
  }

  if (!step.linked && step.listRequested && step.suggestionCount > 0) {
    return "suggestions";
  }

  return "close";
}

/**
 * The suggested accounts in rank order.
 * An account without an order stays in the full list only.
 */
export function suggestedAccounts<T extends { suggestionOrder: number | null }>(
  accounts: T[],
) {
  return accounts
    .filter((account) => account.suggestionOrder != null)
    .sort(
      (left, right) =>
        (left.suggestionOrder ?? 0) - (right.suggestionOrder ?? 0),
    );
}
