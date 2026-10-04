import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

type PaginationProps = {
  page: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  isLoading?: boolean;
  onPrevious: () => void;
  onNext: () => void;
  className?: string;
};

/**
 * Previous and Next controls for a paged list.
 * Each button stays disabled while isLoading is set or when that direction's flag is false.
 */
function Pagination({
  page,
  totalPages,
  hasPreviousPage,
  hasNextPage,
  isLoading = false,
  onPrevious,
  onNext,
  className,
}: PaginationProps) {
  return (
    <div
      data-slot="pagination"
      className={cn(
        "flex items-center justify-between border-t border-border px-4 py-3 text-sm text-muted-foreground",
        className,
      )}
    >
      <span>
        Page {page} of {totalPages}
      </span>

      <div className="flex gap-2">
        <Button
          type="button"
          variant="outline"
          size="sm"
          disabled={!hasPreviousPage || isLoading}
          onClick={onPrevious}
        >
          Previous
        </Button>

        <Button
          type="button"
          variant="outline"
          size="sm"
          disabled={!hasNextPage || isLoading}
          onClick={onNext}
        >
          Next
        </Button>
      </div>
    </div>
  );
}

export { Pagination };
export type { PaginationProps };
