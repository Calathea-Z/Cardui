import type { ComponentProps } from "react";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "@/lib/utils";

/**
 * Class names for an alert variant.
 * An omitted variant uses the muted default panel.
 */
const alertVariants = cva("rounded-lg text-sm whitespace-pre-line", {
  variants: {
    variant: {
      default: "app-panel p-4 text-muted-foreground",
      destructive:
        "border border-destructive/40 bg-destructive/10 px-3 py-2 text-destructive",
      panel: "app-panel border-destructive/40 p-4 text-destructive",
    },
  },
  defaultVariants: {
    variant: "default",
  },
});

/**
 * Status message announced as an alert.
 * variant default is a muted panel, destructive is inline danger text, and panel is a danger panel.
 * A line break in the text stays on its own line.
 */
function Alert({
  className,
  variant = "default",
  ...props
}: ComponentProps<"div"> & VariantProps<typeof alertVariants>) {
  return (
    <div
      data-slot="alert"
      role="alert"
      className={cn(alertVariants({ variant }), className)}
      {...props}
    />
  );
}

export { Alert, alertVariants };
