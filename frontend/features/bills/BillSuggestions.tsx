"use client";

import { Button } from "@/components/ui/button";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type { ObligationSuggestionDto } from "@/lib/api/types";
import { formatDueDate } from "./billDisplay";
import { obligationCadenceLabel } from "./billOptions";

type BillSuggestionsProps = {
  suggestions: ObligationSuggestionDto[];
  busyKey: string | null;
  onAdd: (suggestion: ObligationSuggestionDto, opener: HTMLElement) => void;
  onDismiss: (suggestion: ObligationSuggestionDto) => void;
};

/**
 * Lists recurring payments noticed in activity.
 * None of these are bills. Add one to record it, or leave it out.
 */
export function BillSuggestions({
  suggestions,
  busyKey,
  onAdd,
  onDismiss,
}: BillSuggestionsProps) {
  if (suggestions.length === 0) {
    return null;
  }

  return (
    <section className="flex flex-col gap-3">
      <div>
        <h2 className="app-section-title">Suggested from activity</h2>
        <p className="app-section-meta">
          These are not bills yet. Add one to record it, or leave it out.
        </p>
      </div>
      <ul className="flex flex-col gap-3">
        {suggestions.map((suggestion) => (
          <SuggestionRow
            key={suggestion.key}
            suggestion={suggestion}
            busy={busyKey === suggestion.key}
            onAdd={(opener) => onAdd(suggestion, opener)}
            onDismiss={() => onDismiss(suggestion)}
          />
        ))}
      </ul>
    </section>
  );
}

type SuggestionRowProps = {
  suggestion: ObligationSuggestionDto;
  busy: boolean;
  onAdd: (opener: HTMLElement) => void;
  onDismiss: () => void;
};

/**
 * One suggested payment.
 * The muted card marks it as unconfirmed. Add as bill opens the form. Not a bill leaves the pattern out.
 */
function SuggestionRow({
  suggestion,
  busy,
  onAdd,
  onDismiss,
}: SuggestionRowProps) {
  return (
    <li className="flex flex-col gap-3 rounded-lg border border-border bg-muted/40 p-3">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate font-medium">{suggestion.name}</p>
          <p className="mt-0.5 text-sm text-muted-foreground">
            Suggestion
            {" · "}
            {obligationCadenceLabel(suggestion.cadence)}
            {" · "}
            {suggestion.accountName ?? "No account"}
          </p>
        </div>
        <div className="shrink-0 text-right">
          <p className="text-foreground tabular-nums">
            {formatCurrency(suggestion.amount, suggestion.currency)}
          </p>
          <p className="mt-0.5 text-xs text-muted-foreground">each payment</p>
        </div>
      </div>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm text-muted-foreground">
          Next due {formatDueDate(suggestion.nextDueDate)}
        </p>
        <div className="flex gap-2 sm:ml-auto">
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            disabled={busy}
            onClick={(event) => onAdd(event.currentTarget)}
          >
            Add as bill
          </Button>
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            disabled={busy}
            onClick={onDismiss}
          >
            Not a bill
          </Button>
        </div>
      </div>
    </li>
  );
}
