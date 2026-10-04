"use client";

import { useEffect, useRef } from "react";
import { cn } from "@/lib/utils";

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
  align?: "start" | "end";
  className?: string;
};

/**
 * Menu of actions anchored to its trigger.
 * Closes on an outside click or Escape. align end sits on the right edge, and a disabled item skips onSelect.
 */
export function ActionMenu({
  open,
  onClose,
  items,
  align = "end",
  className,
}: ActionMenuProps) {
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    /**
     * Closes the menu when the press lands outside the menu element.
     */
    function handlePointerDown(event: MouseEvent) {
      if (!menuRef.current?.contains(event.target as Node)) {
        onClose();
      }
    }

    /**
     * Closes the menu when the key is Escape.
     */
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        onClose();
      }
    }

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [open, onClose]);

  if (!open) {
    return null;
  }

  return (
    <div
      ref={menuRef}
      role="menu"
      className={cn(
        "absolute top-full z-50 mt-2 min-w-48 overflow-hidden rounded-lg border border-border bg-popover py-1 shadow-xl",
        align === "end" ? "right-0" : "left-0",
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
          className="flex w-full cursor-pointer px-4 py-2.5 text-left text-sm text-foreground transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50"
        >
          {item.label}
        </button>
      ))}
    </div>
  );
}
