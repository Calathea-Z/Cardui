"use client";

import { ArrowLeft, X } from "lucide-react";
import { useEffect, useId, useRef } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

type BottomSheetProps = {
  open: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  className?: string;
  overlayClassName?: string;
  /** When false, Escape does not close this sheet (use for sheets under a stacked sheet). */
  closeOnEscape?: boolean;
  /** `back` shows a left arrow and centered title; `close` shows an X on the right. */
  headerAction?: "close" | "back";
};

export function BottomSheet({
  open,
  onClose,
  title,
  children,
  className,
  overlayClassName,
  closeOnEscape = true,
  headerAction = "close",
}: BottomSheetProps) {
  const titleId = useId();
  const panelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    document.body.style.overflow = "hidden";

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape" && closeOnEscape) {
        onClose();
      }
    }

    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.body.style.overflow = "";
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [open, onClose, closeOnEscape]);

  useEffect(() => {
    if (open) {
      panelRef.current?.focus();
    }
  }, [open]);

  if (!open) {
    return null;
  }

  return (
    <div className={cn("fixed inset-0 z-60", overlayClassName)}>
      <button
        type="button"
        aria-label="Close"
        className="absolute inset-0 bg-black/50"
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

        <div className="min-h-0 flex-1 overflow-y-auto px-4 py-4">{children}</div>
      </div>
    </div>
  );
}
