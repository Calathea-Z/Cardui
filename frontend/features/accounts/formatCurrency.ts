/**
 * The digits of a money field, without a dollar sign, spaces, or thousands commas.
 * `30,000` and `$30,000.50` are accepted. A comma that does not group thousands is rejected.
 * Blank is null.
 */
export function moneyDigits(value: string): string | null {
  const compact = value.trim().replace(/[$\s]/g, "");
  if (!compact) {
    return null;
  }

  if (
    compact.includes(",") &&
    !/^-?\d{1,3}(,\d{3})+(\.\d+)?$/.test(compact)
  ) {
    return null;
  }

  const plain = compact.replace(/,/g, "");
  if (!/^-?\d+(\.\d+)?$/.test(plain)) {
    return null;
  }

  return plain;
}

/**
 * The message when a comma is not grouping thousands.
 * Cents use a period. A comma is only for thousands.
 */
export function moneyCommaError(value: string): string | null {
  const compact = value.trim().replace(/[$\s]/g, "");
  if (
    !compact.includes(",") ||
    /^-?\d{1,3}(,\d{3})+(\.\d+)?$/.test(compact)
  ) {
    return null;
  }

  return "Use a period for cents, like 30.44. A comma is only for thousands, like 30,000.";
}

/**
 * Formats a money amount for on-screen text.
 * A missing amount is a dash. An unrecognized currency code uses USD.
 */
export function formatCurrency(
  value: number | null,
  currency: string | null = "USD",
) {
  if (value === null) {
    return "-";
  }

  const code = normalizeCurrencyCode(currency);

  try {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: code,
    }).format(value);
  } catch {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: "USD",
    }).format(value);
  }
}

/**
 * Keeps a three-letter currency code.
 * Anything else becomes USD so the formatter does not throw.
 */
function normalizeCurrencyCode(currency: string | null) {
  const code = currency?.trim().toUpperCase() ?? "";
  return /^[A-Z]{3}$/.test(code) ? code : "USD";
}
