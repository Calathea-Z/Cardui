type BrandMarkProps = {
  variant?: "sidebar" | "header";
};

/**
 * Tortoise wordmark. The sidebar names the product as a financial recovery plan.
 * The phone header shows the wordmark alone at a smaller size, so it stays quieter than the page's h1.
 */
export function BrandMark({ variant = "sidebar" }: BrandMarkProps) {
  if (variant === "header") {
    return (
      <p className="text-lg leading-none font-semibold tracking-[-0.02em] text-foreground">
        Tortoise
      </p>
    );
  }

  return (
    <div className="mb-10">
      <p className="text-[1.75rem] leading-none font-semibold tracking-[-0.02em] text-sidebar-foreground">
        Tortoise
      </p>
      <p className="mt-2 text-xs font-medium text-muted-foreground">
        Financial recovery plan
      </p>
    </div>
  );
}
