import { PageHeader } from "@/components/navigation/page-header";
import { EmptyState } from "@/components/ui/empty-state";

/**
 * Plan page.
 * The destination is first in the main nav. Payoff dates and breathing room are not on this screen yet.
 */
export function PlanPageClient() {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Plan"
        description="When each payoff removes a monthly obligation, and the breathing room that follows."
      />
      <div className="rounded-lg border border-border bg-card">
        <EmptyState
          title="No plan yet"
          description="This page will show each payoff, the minimum it removes, and the monthly breathing room that follows."
        />
      </div>
    </div>
  );
}
