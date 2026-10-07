"use client";

import { useRef, useState } from "react";
import { Tooltip as TooltipPrimitive } from "@base-ui/react/tooltip";
import { cn } from "@/lib/utils";

type InfoTipProps = {
  text: string;
  hint: string;
  className?: string;
};

/**
 * Shows a short explanation on hover, focus, or a tap.
 * A phone opens it with a tap and closes it with another tap or a tap outside.
 * The visible text stays the name. The hint says what that text leaves out.
 */
export function InfoTip({ text, hint, className }: InfoTipProps) {
  const [open, setOpen] = useState(false);
  const openedByTap = useRef(false);

  return (
    <TooltipPrimitive.Root
      open={open}
      onOpenChange={(next, details) => {
        if (!next && details.reason === "trigger-hover" && !canHover()) {
          return;
        }

        if (next && details.reason === "trigger-focus" && !canHover()) {
          openedByTap.current = true;
        }

        if (!next) {
          openedByTap.current = false;
        }

        setOpen(next);
      }}
    >
      <TooltipPrimitive.Trigger
        type="button"
        delay={200}
        closeOnClick={false}
        aria-expanded={open}
        onClick={() => {
          if (canHover()) {
            return;
          }

          if (openedByTap.current) {
            openedByTap.current = false;
            return;
          }

          setOpen((current) => !current);
        }}
        className={cn(
          "inline cursor-help border-0 bg-transparent p-0 text-left font-inherit text-inherit",
          "underline decoration-dotted decoration-muted-foreground/70 underline-offset-4",
          "max-md:inline-flex max-md:min-h-11 max-md:items-center",
          "focus-visible:rounded-sm focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
          className,
        )}
      >
        {text}
      </TooltipPrimitive.Trigger>
      <TooltipPrimitive.Portal>
        <TooltipPrimitive.Positioner
          side="bottom"
          sideOffset={6}
          className="z-[110]"
        >
          <TooltipPrimitive.Popup className="max-w-72 rounded-md border border-border bg-popover px-2.5 py-1.5 text-left text-sm text-popover-foreground">
            {hint}
          </TooltipPrimitive.Popup>
        </TooltipPrimitive.Positioner>
      </TooltipPrimitive.Portal>
    </TooltipPrimitive.Root>
  );
}

/**
 * True when the pointer can hover, so a tip can open without a tap.
 * A phone reports false. A mouse or trackpad reports true.
 */
function canHover() {
  return window.matchMedia("(hover: hover) and (pointer: fine)").matches;
}

type InfoTipProviderProps = {
  children: React.ReactNode;
};

/**
 * Shares one hover delay across the tips in a section.
 * Moving from one tip to the next opens the next one immediately.
 */
export function InfoTipProvider({ children }: InfoTipProviderProps) {
  return (
    <TooltipPrimitive.Provider delay={200}>
      {children}
    </TooltipPrimitive.Provider>
  );
}
