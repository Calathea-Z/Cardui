export const PLANNING_CURRENCIES = [
  { code: "USD", label: "USD — US dollar" },
  { code: "CAD", label: "CAD — Canadian dollar" },
  { code: "EUR", label: "EUR — Euro" },
  { code: "GBP", label: "GBP — British pound" },
  { code: "AUD", label: "AUD — Australian dollar" },
  { code: "MXN", label: "MXN — Mexican peso" },
  { code: "JPY", label: "JPY — Japanese yen" },
] as const;

export const HOUSEHOLD_TIME_ZONES = [
  { id: "America/New_York", label: "Eastern — New York" },
  { id: "America/Chicago", label: "Central — Chicago" },
  { id: "America/Denver", label: "Mountain — Denver" },
  { id: "America/Phoenix", label: "Arizona — Phoenix" },
  { id: "America/Los_Angeles", label: "Pacific — Los Angeles" },
  { id: "America/Anchorage", label: "Alaska — Anchorage" },
  { id: "Pacific/Honolulu", label: "Hawaii — Honolulu" },
  { id: "UTC", label: "UTC" },
] as const;

export function currencyChoices(current: string) {
  if (PLANNING_CURRENCIES.some((option) => option.code === current)) {
    return [...PLANNING_CURRENCIES];
  }

  return [{ code: current, label: current }, ...PLANNING_CURRENCIES];
}

export function timeZoneChoices(current: string) {
  if (HOUSEHOLD_TIME_ZONES.some((option) => option.id === current)) {
    return [...HOUSEHOLD_TIME_ZONES];
  }

  return [{ id: current, label: current }, ...HOUSEHOLD_TIME_ZONES];
}
