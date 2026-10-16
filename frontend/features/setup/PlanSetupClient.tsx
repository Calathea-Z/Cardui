import Link from "next/link";
import { CircleAlert } from "lucide-react";
import { buttonVariants } from "@/components/ui/button";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { cn } from "@/lib/utils";
import { planSetupGroups, type PlanSetupInput } from "./planSetupSummary";

type PlanSetupClientProps = {
  input: PlanSetupInput;
  currency: string;
};

/**
 * Plan setup lists the existing editors as compact rows.
 * Each row shows the saved summary, a specific edit, and a debt-terms warning when Plan already has one.
 */
export function PlanSetupClient({ input, currency }: PlanSetupClientProps) {
  const groups = planSetupGroups(input, (amount) =>
    formatCurrency(amount, currency),
  );

  return (
    <div className="divide-y divide-border overflow-hidden rounded-lg border border-border bg-card">
      {groups.map((group) => (
        <section
          key={group.key}
          className="flex flex-col gap-3 px-4 py-3 lg:flex-row lg:items-center lg:justify-between lg:gap-6"
          aria-labelledby={`setup-${group.key}`}
        >
          <div className="min-w-0 flex-1">
            <h2 id={`setup-${group.key}`} className="app-section-title">
              {group.title}
            </h2>
            <ul className="mt-1 flex flex-col gap-0.5">
              {group.lines.map((line) => (
                <li key={line} className="text-sm text-foreground">
                  {line}
                </li>
              ))}
            </ul>
            {group.issue ? (
              <p className="mt-2 flex items-start gap-2 text-sm text-warning">
                <CircleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
                <span>{group.issue}</span>
              </p>
            ) : null}
          </div>
          <div className="flex flex-wrap gap-2">
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
