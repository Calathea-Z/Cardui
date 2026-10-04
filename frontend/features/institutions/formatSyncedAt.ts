/**
 * Formats the last Plaid sync time for an institution card.
 * A missing time means the connection has never synced.
 */
export function formatSyncedAt(value: string | null) {
  if (!value) {
    return "Never synced";
  }

  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(value));
}
