"use client";

import { ArrowLeft, X } from "lucide-react";
import { useEffect, useId, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

/**
 * How long the close animation runs before the sheet leaves the DOM.
 */
const SHEET_TRANSITION_MS = 320;

/**
 * How many sheets are holding page scroll closed.
 * Page scroll returns when the count reaches zero.
 */
let bodyScrollLockCount = 0;

/**
 * Hides page scroll and increments the shared lock count.
 * Nested callers keep scroll hidden until each one unlocks.
 */
function lockBodyScroll() {
  bodyScrollLockCount += 1;
  document.body.style.overflow = "hidden";
}

/**
 * Releases one page-scroll lock.
 * Restores scrolling when no sheet still holds a lock.
 */
function unlockBodyScroll() {
  bodyScrollLockCount = Math.max(0, bodyScrollLockCount - 1);
  if (bodyScrollLockCount === 0) {
    document.body.style.overflow = "";
  }
}

type BottomSheetProps = {
  open: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  className?: string;
  overlayClassName?: string;
  /** When false, Escape does not close this sheet (use for sheets under a stacked sheet). */
  closeOnEscape?: boolean;
  /**
   * `back` — left arrow, centered title.
   * `close` — title left, X right.
   * `close-leading` — X left, centered title, optional trailing action.
   */
  headerAction?: "close" | "back" | "close-leading";
  headerTrailing?: React.ReactNode;
};

/**
 * Panel that slides up from the bottom of the screen.
 * It stays mounted through the close animation, locks page scroll while shown, and closes on Escape when closeOnEscape is set.
 */
export function BottomSheet({
  open,
  onClose,
  title,
  children,
  className,
  overlayClassName,
  closeOnEscape = true,
  headerAction = "close",
  headerTrailing,
}: BottomSheetProps) {
  const titleId = useId();
  const panelRef = useRef<HTMLDivElement>(null);
  const [present, setPresent] = useState(open);

  if (open && !present) {
    setPresent(true);
  }

  useEffect(() => {
    if (open || !present) {
      return;
    }

    const timeout = window.setTimeout(() => {
      setPresent(false);
    }, SHEET_TRANSITION_MS);

    return () => window.clearTimeout(timeout);
  }, [open, present]);

  useEffect(() => {
    if (!present) {
      return;
    }

    lockBodyScroll();

    /**
     * Closes the sheet on Escape.
     * Escape applies when closeOnEscape is set and the sheet is still open.
     */
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape" && closeOnEscape && open) {
        onClose();
      }
    }

    document.addEventListener("keydown", handleKeyDown);

    return () => {
      unlockBodyScroll();
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [present, open, onClose, closeOnEscape]);

  useEffect(() => {
    if (open && present) {
      panelRef.current?.focus();
    }
  }, [open, present]);

  if (!present) {
    return null;
  }

  return (
    <div className={cn("fixed inset-0 z-60", overlayClassName)}>
      <button
        type="button"
        aria-label="Close"
        tabIndex={open ? 0 : -1}
        className={cn(
          "absolute inset-0 bg-black/50 motion-reduce:animate-none",
          open ? "animate-sheet-overlay-in" : "animate-sheet-overlay-out",
        )}
        onClick={onClose}
      />

      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
        className={cn(
          "absolute inset-x-0 bottom-0 flex max-h-[85vh] flex-col rounded-t-2xl border border-border bg-background shadow-2xl outline-none",
          "pb-[max(1rem,env(safe-area-inset-bottom))]",
          "motion-reduce:animate-none",
          open ? "animate-sheet-in" : "animate-sheet-out",
          className,
        )}
      >
        {headerAction === "back" ? (
          <div className="app-panel-header grid grid-cols-[2.25rem_1fr_2.25rem] items-center gap-2 px-4 py-4">
            <Button
              type="button"
              variant="ghost"
              size="icon-lg"
              aria-label="Go back"
              onClick={onClose}
              className="justify-self-start"
            >
              <ArrowLeft className="size-5" />
            </Button>
            <h2
              id={titleId}
              className="truncate text-center text-lg font-semibold text-foreground"
            >
              {title}
            </h2>
            <span aria-hidden="true" />
          </div>
        ) : headerAction === "close-leading" ? (
          <div className="app-panel-header grid grid-cols-[minmax(4.5rem,auto)_1fr_minmax(4.5rem,auto)] items-center gap-2 px-2 py-3 sm:px-4">
            <Button
              type="button"
              variant="ghost"
              size="icon-lg"
              aria-label="Close"
              onClick={onClose}
              className="justify-self-start"
            >
              <X className="size-5" />
            </Button>
            <h2
              id={titleId}
              className="truncate text-center text-lg font-semibold text-foreground"
            >
              {title}
            </h2>
            <div className="flex justify-end">{headerTrailing}</div>
          </div>
        ) : (
          <div className="app-panel-header flex items-center justify-between gap-4 px-4 py-4">
            <h2 id={titleId} className="text-lg font-semibold text-foreground">
              {title}
            </h2>
            <Button
              type="button"
              variant="ghost"
              size="icon-lg"
              aria-label="Close"
              onClick={onClose}
            >
              <X className="size-5" />
            </Button>
          </div>
        )}

        <div className="min-h-0 flex-1 overflow-y-auto px-4 py-4">
          {children}
        </div>
      </div>
    </div>
  );
}
