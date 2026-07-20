import type { ComponentProps } from "react";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "@/lib/utils";

const alertVariants = cva("rounded-md text-sm", {
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
