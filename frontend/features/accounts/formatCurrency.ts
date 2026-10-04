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

function normalizeCurrencyCode(currency: string | null) {
  const code = currency?.trim().toUpperCase() ?? "";
  return /^[A-Z]{3}$/.test(code) ? code : "USD";
}
