import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { SpendableCopy } from "./spendableCopy";

type PlanShortfallReviewProps = {
  spendable: SpendableCopy;
  onTypicalPay: () => void;
};

/**
 * Explains the steady-pay figure from Overview before offering typical pay.
 * The amount, lowest-balance date, and money set aside are the same lines Overview shows.
 * Typical pay is a later view and is not this review.
 */
export function PlanShortfallReview({
  spendable,
  onTypicalPay,
}: PlanShortfallReviewProps) {
  const short = spendable.status === "short";

  return (
    <div className="flex flex-col gap-4">
      <section
        className={cn("app-panel p-4", short && "border-warning/40")}
        aria-labelledby="plan-shortfall-title"
      >
        <h2 id="plan-shortfall-title" className="app-section-title">
          {short ? "Steady-pay shortfall" : "Steady pay"}
        </h2>
        <p className="mt-2 text-sm text-foreground">
          This is the next-30-day figure from Overview. It counts steady pay
          only, before extra debt payments.
        </p>
        <p className="mt-4 text-xs text-muted-foreground">
          {spendable.figureLabel}
        </p>
        <p className="text-[2rem] leading-tight font-semibold tracking-tight text-foreground tabular-nums">
          {spendable.figure}
        </p>
        <p className="mt-2 text-sm text-muted-foreground">
          Lowest balance:{" "}
          <span className="font-medium text-foreground tabular-nums">
            {spendable.lowestBalance}
          </span>
        </p>
        <p className="mt-1 text-sm text-muted-foreground">
          {spendable.protectedMoney}
        </p>
        <p className="mt-2 text-sm text-foreground">{spendable.basis}</p>
      </section>
      <section
        className="app-panel p-4"
        aria-labelledby="plan-typical-pay-offer"
      >
        <h2 id="plan-typical-pay-offer" className="app-section-title">
          Typical pay
        </h2>
        <p className="mt-2 text-sm text-foreground">
          Typical pay counts every included paycheck. It is a different view,
          and its lowest day is not this steady-pay figure.
        </p>
        <button
          type="button"
          onClick={onTypicalPay}
          className={cn(
            buttonVariants({ variant: "outline" }),
            "mt-4 min-h-11 bg-card",
          )}
        >
          Look at typical pay
        </button>
      </section>
    </div>
  );
}
