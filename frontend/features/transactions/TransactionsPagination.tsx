type TransactionsPaginationProps = {
  page: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  isLoading: boolean;
  onPrevious: () => void;
  onNext: () => void;
};

export function TransactionsPagination({
  page,
  totalPages,
  hasPreviousPage,
  hasNextPage,
  isLoading,
  onPrevious,
  onNext,
}: TransactionsPaginationProps) {
  return (
    <div className="flex items-center justify-between border-t border-border px-4 py-3 text-sm text-muted-foreground">
      <span>
        Page {page} of {totalPages}
      </span>

      <div className="flex gap-2">
        <button
          type="button"
          disabled={!hasPreviousPage || isLoading}
          onClick={onPrevious}
          className="cursor-pointer rounded-md border border-border px-3 py-1.5 transition hover:border-primary/50 hover:text-foreground disabled:cursor-not-allowed disabled:opacity-50"
        >
          Previous
        </button>

        <button
          type="button"
          disabled={!hasNextPage || isLoading}
          onClick={onNext}
          className="cursor-pointer rounded-md border border-border px-3 py-1.5 transition hover:border-primary/50 hover:text-foreground disabled:cursor-not-allowed disabled:opacity-50"
        >
          Next
        </button>
      </div>
    </div>
  );
}
