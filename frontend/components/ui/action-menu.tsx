"use client";

import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { cn } from "@/lib/utils";
import { placeActionMenu } from "./action-menu-placement";
import type { PopoverPlacement } from "./select-popover";

/**
 * One row in an action menu.
 * A disabled item stays visible and skips onSelect.
 */
export type ActionMenuItem = {
  id: string;
  label: string;
  onSelect: () => void;
  disabled?: boolean;
};

type ActionMenuProps = {
  open: boolean;
  onClose: () => void;
  items: ActionMenuItem[];
  /** The control that opened the menu. The menu stays in the page beside this control. */
  anchorRef: { current: HTMLElement | null };
  className?: string;
};

/**
 * Menu of actions anchored to its trigger.
 * The menu is portaled so a sidebar or overflow clip cannot cover it. It closes on an outside press or Escape, and focus returns to the trigger.
 */
export function ActionMenu({
  open,
  onClose,
  items,
  anchorRef,
  className,
}: ActionMenuProps) {
  const menuRef = useRef<HTMLDivElement>(null);
  const [placement, setPlacement] = useState<PopoverPlacement | null>(null);
  const focusedOnOpen = useRef(false);
  const wasOpen = useRef(false);

  useLayoutEffect(() => {
    if (!open) {
      focusedOnOpen.current = false;
      return;
    }

    /**
     * Reads the trigger and the page column, then stores a position that stays on screen.
     */
    function updatePlacement() {
      const anchor = anchorRef.current;
      if (!anchor) {
        return;
      }

      const rect = anchor.getBoundingClientRect();
      const contentLeft =
        anchor.closest("main")?.getBoundingClientRect().left ?? 0;
      setPlacement(
        placeActionMenu(
          {
            top: rect.top,
            bottom: rect.bottom,
            left: rect.left,
            width: rect.width,
          },
          { width: window.innerWidth, height: window.innerHeight },
          contentLeft,
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
  }, [open, anchorRef]);

  useLayoutEffect(() => {
    if (!open || !placement || focusedOnOpen.current) {
      return;
    }

    focusedOnOpen.current = true;
    menuRef.current
      ?.querySelector<HTMLElement>("[role='menuitem']:not(:disabled)")
      ?.focus();
  }, [open, placement]);

  useEffect(() => {
    if (open) {
      wasOpen.current = true;
      return;
    }

    if (!wasOpen.current) {
      return;
    }

    wasOpen.current = false;
    anchorRef.current?.querySelector<HTMLElement>("button")?.focus();
  }, [open, anchorRef]);

  useEffect(() => {
    if (!open) {
      return;
    }

    /**
     * Closes the menu when the press lands outside the menu and the trigger.
     */
    function handlePointerDown(event: MouseEvent) {
      const target = event.target as Node;
      if (menuRef.current?.contains(target)) {
        return;
      }

      if (anchorRef.current?.contains(target)) {
        return;
      }

      onClose();
    }

    /**
     * Closes the menu when the key is Escape.
     * Closing returns focus to the trigger.
     */
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") {
        return;
      }

      event.stopPropagation();
      onClose();
    }

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [open, onClose, anchorRef]);

  if (!open || !placement) {
    return null;
  }

  return createPortal(
    <div
      ref={menuRef}
      role="menu"
      style={{
        top: placement.side === "below" ? placement.top : undefined,
        bottom: placement.side === "above" ? placement.bottom : undefined,
        left: placement.left,
        width: placement.width,
        maxHeight: placement.maxHeight,
      }}
      className={cn(
        "fixed z-[110] overflow-y-auto rounded-lg border border-border bg-popover py-1 shadow-xl",
        className,
      )}
    >
      {items.map((item) => (
        <button
          key={item.id}
          type="button"
          role="menuitem"
          disabled={item.disabled}
          onClick={() => {
            if (item.disabled) {
              return;
            }

            onClose();
            item.onSelect();
          }}
          className="flex min-h-11 w-full cursor-pointer px-4 py-2.5 text-left text-sm whitespace-nowrap text-foreground transition hover:bg-accent focus-visible:bg-muted focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {item.label}
        </button>
      ))}
    </div>,
    document.body,
  );
}
