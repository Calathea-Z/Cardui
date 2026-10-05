"use client";

import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import {
  placeSelectPopover,
  type PopoverPlacement,
} from "@/components/ui/select-popover";
import { cn } from "@/lib/utils";

type AnchoredPopoverProps = {
  open: boolean;
  label: string;
  triggerRef: { current: HTMLElement | null };
  onClose: () => void;
  children: React.ReactNode;
  role?: "dialog" | "listbox";
  className?: string;
  /** Smallest width in pixels. Defaults to the choice-list minimum. */
  minWidth?: number;
  /** Tallest height in pixels before the viewport cuts it shorter. */
  maxHeight?: number;
  /** Keeps the popover at minWidth instead of growing with the trigger. */
  fixedWidth?: boolean;
  /** Moves focus to the selected option, or the first button, when the popover opens. */
  focusSelected?: boolean;
};

/**
 * Popover anchored to a trigger and portaled to document.body.
 * It closes on Escape or a press outside the popover and the trigger, and it moves with the trigger while the page scrolls.
 */
export function AnchoredPopover({
  open,
  label,
  triggerRef,
  onClose,
  children,
  role = "dialog",
  className,
  minWidth,
  maxHeight,
  fixedWidth = false,
  focusSelected = false,
}: AnchoredPopoverProps) {
  const popoverRef = useRef<HTMLDivElement>(null);
  const [placement, setPlacement] = useState<PopoverPlacement | null>(null);
  const focusedOnOpen = useRef(false);

  useLayoutEffect(() => {
    if (!open) {
      focusedOnOpen.current = false;
      return;
    }

    /**
     * Reads the trigger and stores the popover position.
     */
    function updatePlacement() {
      const trigger = triggerRef.current;
      if (!trigger) {
        return;
      }

      const rect = trigger.getBoundingClientRect();
      setPlacement(
        placeSelectPopover(
          {
            top: rect.top,
            bottom: rect.bottom,
            left: rect.left,
            width: rect.width,
          },
          { width: window.innerWidth, height: window.innerHeight },
          { minWidth, maxHeight, fixedWidth },
        ),
      );
    }

    updatePlacement();
    window.addEventListener("resize", updatePlacement);
    document.addEventListener("scroll", updatePlacement, true);

    return () => {
      window.removeEventListener("resize", updatePlacement);
      document.removeEventListener("scroll", updatePlacement, true);
    };
  }, [open, triggerRef, minWidth, maxHeight, fixedWidth]);

  useLayoutEffect(() => {
    if (!open || !placement || !focusSelected || focusedOnOpen.current) {
      return;
    }

    focusedOnOpen.current = true;
    const selected = popoverRef.current?.querySelector<HTMLElement>(
      "[aria-selected='true']",
    );
    (selected ?? popoverRef.current?.querySelector("button"))?.focus();
  }, [open, placement, focusSelected]);

  useEffect(() => {
    if (!open) {
      return;
    }

    /**
     * Closes the popover when the press lands outside it and the trigger.
     */
    function handlePointerDown(event: MouseEvent) {
      const target = event.target as Node;
      if (popoverRef.current?.contains(target)) {
        return;
      }

      if (triggerRef.current?.contains(target)) {
        return;
      }

      onClose();
    }

    /**
     * Closes the popover when the key is Escape.
     */
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        event.stopPropagation();
        onClose();
      }
    }

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [open, onClose, triggerRef]);

  if (!open || !placement) {
    return null;
  }

  return createPortal(
    <div
      ref={popoverRef}
      role={role}
      aria-label={label}
      style={{
        top: placement.side === "below" ? placement.top : undefined,
        bottom: placement.side === "above" ? placement.bottom : undefined,
        left: placement.left,
        width: placement.width,
        maxHeight: placement.maxHeight,
      }}
      className={cn(
        "fixed z-[110] overflow-y-auto rounded-lg border border-border bg-popover shadow-xl",
        className,
      )}
    >
      {children}
    </div>,
    document.body,
  );
}
