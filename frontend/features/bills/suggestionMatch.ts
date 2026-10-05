/**
 * True when a saved bill covers a suggestion.
 * The match is the stored pattern key, or the same name ignoring case and extra spaces.
 * A renamed bill still covers the suggestion it was opened from.
 */
export function suggestionIsCovered(
  suggestion: { key: string; name: string },
  billName: string,
  storedKey: string | null,
) {
  if (storedKey && suggestion.key === storedKey) {
    return true;
  }

  return (
    normalizeSuggestionKey(suggestion.name) === normalizeSuggestionKey(billName)
  );
}

/**
 * Collapses a bill or merchant name the same way the suggestion key does.
 * A blank name stays blank.
 */
function normalizeSuggestionKey(value: string) {
  return value.trim().replace(/\s+/g, " ").toLowerCase();
}
