/**
 * The line under a rule while its percents are valid.
 * The number is the share that stays available.
 */
export function staysAvailableSentence(cash: number): string {
  return `${cash} percent stays available.`;
}

/**
 * Reviews one variable surplus using amounts the page has already formatted.
 * The sentence says the money is not moved.
 */
export function variableSplitSentence(
  name: string,
  surplus: string,
  debt: string,
  savings: string,
  cash: string,
): string {
  return `${name} is ${surplus} above typical. ${debt} would be extra debt, ${savings} would be savings, and ${cash} would stay available. Tortoise does not move this money.`;
}

/**
 * Names a variable source the rule does not split.
 * Another currency and a missing strong surplus use different sentences.
 */
export function leftOutSentence(name: string, leftOut: string): string {
  if (leftOut === "Another currency") {
    return `${name} uses another currency, so this rule leaves it out.`;
  }

  return `${name} has no strong amount above typical, so there is nothing to split.`;
}

/**
 * Reviews the windfall percents without inventing an amount.
 * The Plan preview still keeps a windfall in cash.
 */
export function windfallSentence(
  debt: number,
  savings: number,
  cash: number,
): string {
  return `${debt} percent would be extra debt, ${savings} percent would be savings, and ${cash} percent would stay available. A windfall tried on Plan still stays in cash. Tortoise does not pay a debt or change a balance.`;
}

/**
 * A labeled example of one split.
 * The amount is synthetic. It is not the household's pay or windfall.
 */
export function allocationExampleSentence(
  amount: string,
  debt: string,
  savings: string,
  cash: string,
): string {
  return `Example, not your money: ${amount} would be ${debt} debt, ${savings} savings, and ${cash} stays available.`;
}

/**
 * Says whether the fields match the stored preference.
 * Unsaved edits stay distinct from a saved rule.
 */
export function allocationEditSentence(
  isSaved: boolean,
  dirty: boolean,
): string {
  if (dirty) {
    return isSaved
      ? "Unsaved changes. The saved preference stays until you save."
      : "Unsaved changes. No preference is saved yet.";
  }

  return allocationStatusSentence(isSaved);
}

/**
 * Says whether a rule is stored.
 * An unsaved page still shows zeros, and those zeros are not a saved choice.
 */
export function allocationStatusSentence(isSaved: boolean): string {
  return isSaved
    ? "Saved. Tortoise does not move money, pay a debt, or change an account balance."
    : "No rule is saved yet. Extra variable pay and a windfall stay available until you save one.";
}
