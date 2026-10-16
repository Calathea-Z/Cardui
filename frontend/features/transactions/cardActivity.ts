/**
 * Shown on a card payment. Purchases are spending. The payment itself is not spending again.
 * A debt balance kept by hand is not changed by this label.
 */
export const cardPaymentExplanation =
  "Purchases count as spending. Paying the card is not spending again.";

const cardPaymentPhrases = [
  "credit card payment",
  "credit card pymt",
  "credit crd",
  "crcardpmt",
  "crcard pmt",
  "card payment",
  "card pymt",
  "card pmt",
  "card autopay",
  "card online payment",
  "cc payment",
  "cc pymt",
  "cc pmt",
  "payment thank you",
  "pymt thank you",
  "pmt thank you",
  "thank you for your payment",
  "e-payment",
] as const;

const cardMarkers = ["credit card", "credit crd", "crcard"] as const;

const paymentMarkers = ["payment", "pymt", "pmt"] as const;

const paymentPhrases = [
  "autopay",
  "auto pay",
  "auto-pay",
  "e-payment",
] as const;

/**
 * The fields that decide whether a row is a card payment.
 * `amount` uses the stored sign: positive leaves the account, negative enters it.
 * `accountType` is the stored account type, such as "credit" or "depository".
 */
export type CardActivityFields = {
  accountType: string;
  amount: number;
  merchantName?: string | null;
  name?: string | null;
};

/**
 * True when this row settles a credit card instead of buying something.
 * The same phrases are recognized in CardActivityRules. A bill autopay stays spending.
 * A credit on a credit card counts when it reads as a payment, so it is not shown as income.
 */
export function isCardPaymentActivity(activity: CardActivityFields) {
  const text = `${activity.merchantName ?? ""} ${activity.name ?? ""}`
    .trim()
    .toLowerCase();
  if (text.length === 0 || activity.amount === 0) {
    return false;
  }

  if (looksLikeCardPayment(text)) {
    return true;
  }

  return (
    activity.accountType.toLowerCase() === "credit" &&
    activity.amount < 0 &&
    (hasPaymentMarker(text) || text.includes("thank you"))
  );
}

/**
 * True when the description names a card payment on any account.
 */
function looksLikeCardPayment(text: string) {
  if (
    cardPaymentPhrases.some((phrase) => text.includes(phrase)) ||
    containsTerm(text, "epayment")
  ) {
    return true;
  }

  return hasCardMarker(text) && hasPaymentMarker(text);
}

/**
 * True when the text names a card rather than a generic purchase.
 */
function hasCardMarker(text: string) {
  return (
    cardMarkers.some((marker) => text.includes(marker)) ||
    containsTerm(text, "crd")
  );
}

/**
 * True when the text names a payment rather than a fee or a purchase.
 */
function hasPaymentMarker(text: string) {
  return (
    paymentMarkers.some((marker) => containsTerm(text, marker)) ||
    containsTerm(text, "epayment") ||
    paymentPhrases.some((phrase) => text.includes(phrase))
  );
}

/**
 * True when the term stands alone, not as part of a longer word.
 */
function containsTerm(text: string, term: string) {
  let index = 0;
  while ((index = text.indexOf(term, index)) >= 0) {
    const before = index === 0 || !isLetter(text[index - 1] ?? "");
    const after =
      index + term.length >= text.length ||
      !isLetter(text[index + term.length] ?? "");
    if (before && after) {
      return true;
    }

    index += term.length;
  }

  return false;
}

/**
 * True for a character a payment word can sit inside.
 */
function isLetter(character: string) {
  return /[a-z]/i.test(character);
}
