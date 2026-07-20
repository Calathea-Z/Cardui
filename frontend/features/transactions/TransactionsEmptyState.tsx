import { Search } from "lucide-react";

type TransactionsEmptyStateProps = {
  trimmedSearch: string;
  hasActiveFilters: boolean;
};

export function TransactionsEmptyState({
  trimmedSearch,
  hasActiveFilters,
}: TransactionsEmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 px-4 py-14 text-center">
      <Search className="size-12 text-muted-foreground/70" />
      <p className="text-base font-semibold text-foreground">
        No transactions found
      </p>
      <p className="text-sm text-muted-foreground">
        {trimmedSearch
          ? `We couldn't find any transactions matching your search of "${trimmedSearch}".`
          : hasActiveFilters
            ? "Try adjusting your filters."
            : "We couldn't find any transactions."}
      </p>
    </div>
  );
}
