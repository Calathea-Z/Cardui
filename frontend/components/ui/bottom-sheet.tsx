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

type SheetPresentation = "sheet" | "panel";

type BottomSheetProps = {
  open: boolean;
  onClose: () => void;
  /**
   * Called by the back arrow on a nested step.
   * Escape and the backdrop still call onClose. When this is omitted, the arrow closes too.
   */
  onBack?: () => void;
  title: string;
  children: React.ReactNode;
  className?: string;
  overlayClassName?: string;
  /**
   * `sheet` slides up from the bottom.
   * `panel` fills the screen under 768px and sits on the right from there up.
   */
  presentation?: SheetPresentation;
  /** When false, Escape does not close this sheet (use for sheets under a stacked sheet). */
  closeOnEscape?: boolean;
  /**
   * `back` — left arrow, centered title. For a nested step.
   * `close` — title left, X right. For dismissing a surface.
   * `close-leading` — X left, centered title, optional trailing action.
   * `panel` — back under 768px, close from there up. For a top-level detail panel.
   */
  headerAction?: "close" | "back" | "close-leading" | "panel";
  headerTrailing?: React.ReactNode;
};

/**
 * Detail surface for a form or a record.
 * `sheet` slides up from the bottom. `panel` fills the screen under 768px and slides in from the right from there up. It stays mounted through the close animation, locks page scroll while shown, and closes on Escape when closeOnEscape is set.
 */
export function BottomSheet({
  open,
  onClose,
  onBack,
  title,
  children,
  className,
  overlayClassName,
  presentation = "sheet",
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
    <div className={sheetOverlayClassName(presentation, overlayClassName)}>
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
          sheetSurfaceClassName(presentation),
          "motion-reduce:animate-none",
          sheetMotionClassName(presentation, open),
          className,
        )}
      >
        <SheetHeader
          action={headerAction}
          title={title}
          titleId={titleId}
          onClose={onClose}
          onBack={onBack}
          headerTrailing={headerTrailing}
        />

        <div className="min-h-0 flex-1 overflow-y-auto px-4 py-4">
          {children}
        </div>
      </div>
    </div>
  );
}

type SheetHeaderProps = {
  action: NonNullable<BottomSheetProps["headerAction"]>;
  title: string;
  titleId: string;
  onClose: () => void;
  onBack?: () => void;
  headerTrailing?: React.ReactNode;
};

/**
 * Header for the open surface.
 * A top-level panel uses a back arrow under 768px and a close icon from there up. A nested step keeps the back arrow.
 */
function SheetHeader({
  action,
  title,
  titleId,
  onClose,
  onBack,
  headerTrailing,
}: SheetHeaderProps) {
  if (action === "panel") {
    return (
      <>
        <h2 id={titleId} className="sr-only">
          {title}
        </h2>
        <div className="md:hidden">
          <BackHeader title={title} onClose={onClose} />
        </div>
        <div className="hidden md:block">
          <CloseHeader title={title} onClose={onClose} />
        </div>
      </>
    );
  }

  if (action === "back") {
    return (
      <BackHeader
        title={title}
        titleId={titleId}
        onClose={onBack ?? onClose}
        headerTrailing={headerTrailing}
      />
    );
  }

  if (action === "close-leading") {
    return (
      <CloseLeadingHeader
        title={title}
        titleId={titleId}
        onClose={onClose}
        headerTrailing={headerTrailing}
      />
    );
  }

  return <CloseHeader title={title} titleId={titleId} onClose={onClose} />;
}

/**
 * Centered title with a back arrow on the left.
 * The arrow returns from a nested step. A trailing action, such as Save, sits on the right when one is set.
 */
function BackHeader({
  title,
  titleId,
  onClose,
  headerTrailing,
}: {
  title: string;
  titleId?: string;
  onClose: () => void;
  headerTrailing?: React.ReactNode;
}) {
  return (
    <div
      className={cn(
        "app-panel-header grid items-center gap-2 px-4 py-4",
        headerTrailing
          ? "grid-cols-[minmax(4.5rem,auto)_1fr_minmax(4.5rem,auto)]"
          : "grid-cols-[2.25rem_1fr_2.25rem]",
      )}
    >
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
      <SheetHeading id={titleId} title={title} align="center" />
      <div className="flex justify-end">{headerTrailing}</div>
    </div>
  );
}

/**
 * Title on the left and a close icon on the right.
 * The icon dismisses the surface.
 */
function CloseHeader({
  title,
  titleId,
  onClose,
}: {
  title: string;
  titleId?: string;
  onClose: () => void;
}) {
  return (
    <div className="app-panel-header flex items-center justify-between gap-4 px-4 py-4">
      <SheetHeading id={titleId} title={title} align="left" />
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
  );
}

/**
 * Close icon on the left, centered title, and an optional trailing action.
 * The icon dismisses the surface.
 */
function CloseLeadingHeader({
  title,
  titleId,
  onClose,
  headerTrailing,
}: {
  title: string;
  titleId?: string;
  onClose: () => void;
  headerTrailing?: React.ReactNode;
}) {
  return (
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
      <SheetHeading id={titleId} title={title} align="center" />
      <div className="flex justify-end">{headerTrailing}</div>
    </div>
  );
}

/**
 * Visible title, or the accessible name when an id is set.
 * A second visual title omits the id so the dialog has one name.
 */
function SheetHeading({
  id,
  title,
  align,
}: {
  id?: string;
  title: string;
  align: "left" | "center";
}) {
  const className = cn(
    "min-w-0 truncate text-lg font-semibold text-foreground",
    align === "center" && "text-center",
  );

  if (!id) {
    return (
      <p className={className} aria-hidden="true">
        {title}
      </p>
    );
  }

  return (
    <h2 id={id} className={className}>
      {title}
    </h2>
  );
}

/**
 * Overlay stack for the surface.
 * A panel sits above the phone header. A later overlay class can raise a nested surface further.
 */
function sheetOverlayClassName(
  presentation: SheetPresentation,
  overlayClassName?: string,
) {
  return cn(
    "fixed inset-0",
    presentation === "panel" ? "z-[100]" : "z-60",
    overlayClassName,
  );
}

/**
 * Frame for the surface.
 * A panel is a white full-screen sheet under 768px and a right-hand column from there up.
 */
function sheetSurfaceClassName(presentation: SheetPresentation) {
  if (presentation === "panel") {
    return "absolute inset-0 flex max-h-none flex-col border-0 bg-card pt-[env(safe-area-inset-top)] pb-[max(1rem,env(safe-area-inset-bottom))] outline-none md:left-auto md:h-auto md:w-[min(28rem,100%)] md:border-l md:border-border md:pt-0";
  }

  return "absolute inset-x-0 bottom-0 flex max-h-[85vh] flex-col rounded-t-2xl border border-border bg-background pb-[max(1rem,env(safe-area-inset-bottom))] shadow-2xl outline-none";
}

/**
 * Enter and leave motion for the surface.
 * A panel rises on a phone and slides in from the right from 768px up.
 */
function sheetMotionClassName(presentation: SheetPresentation, open: boolean) {
  if (presentation === "panel") {
    return open
      ? "max-md:animate-sheet-in md:animate-panel-in"
      : "max-md:animate-sheet-out md:animate-panel-out";
  }

  return open ? "animate-sheet-in" : "animate-sheet-out";
}
