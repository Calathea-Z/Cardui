import Link from "next/link";
import { buttonVariants } from "@/components/ui/button";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { cn } from "@/lib/utils";
import { planSetupGroups, type PlanSetupInput } from "./planSetupSummary";

type PlanSetupClientProps = {
  input: PlanSetupInput;
  currency: string;
};

/**
 * Plan setup groups the existing editors by workflow.
 * Each group shows what is already saved and an edit link. Completed facts are not asked for again.
 */
export function PlanSetupClient({ input, currency }: PlanSetupClientProps) {
  const groups = planSetupGroups(input, (amount) =>
    formatCurrency(amount, currency),
  );

  return (
    <div className="flex flex-col gap-4">
      {groups.map((group) => (
        <section
          key={group.key}
          className="app-panel p-4"
          aria-labelledby={`setup-${group.key}`}
        >
          <h2 id={`setup-${group.key}`} className="app-section-title">
            {group.title}
          </h2>
          <ul className="mt-2 flex flex-col gap-1">
            {group.lines.map((line) => (
              <li key={line} className="text-sm text-foreground">
                {line}
              </li>
            ))}
          </ul>
          <div className="mt-3 flex flex-wrap gap-2">
            {group.actions.map((action) => (
              <Link
                key={action.href + action.label}
                href={action.href}
                className={cn(
                  buttonVariants({ variant: "outline" }),
                  "min-h-11 bg-card px-4",
                )}
              >
                {action.label}
              </Link>
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}
