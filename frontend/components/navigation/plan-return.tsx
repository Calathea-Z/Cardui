"use client";

import { Suspense } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

/**
 * Shows a way back to Plan or Plan setup when an editor was opened from there.
 * A direct visit has no return query, so the editor stays a top-level page.
 */
export function PlanReturn() {
  return (
    <Suspense fallback={null}>
      <PlanReturnLink />
    </Suspense>
  );
}

/**
 * Reads `from=plan` or `from=setup` and links back to that page.
 * The working Plan preview is kept in this browser, so the return does not clear it.
 */
function PlanReturnLink() {
  const from = useSearchParams().get("from");
  if (from !== "plan" && from !== "setup") {
    return null;
  }

  const href = from === "plan" ? "/plan" : "/plan/setup";
  const label = from === "plan" ? "Back to Plan" : "Back to Plan setup";

  return (
    <Link
      href={href}
      className={cn(
        buttonVariants({ variant: "outline" }),
        "min-h-11 w-fit bg-card px-4",
      )}
    >
      {label}
    </Link>
  );
}
