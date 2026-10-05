"use client";

import { Check, ChevronDown } from "lucide-react";
import {
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
  useSyncExternalStore,
} from "react";
import { createPortal } from "react-dom";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import {
  placeSelectPopover,
  type PopoverPlacement,
} from "@/components/ui/select-popover";
import { cn } from "@/lib/utils";

export type SelectOption = {
  value: string;
  label: string;
};

type SelectProps = {
  value: string;
  options: SelectOption[];
  onChange: (value: string) => void;
  title: string;
  placeholder?: string;
  disabled?: boolean;
  /** `row` matches transaction detail rows; `field` matches form controls. */
  variant?: "row" | "field";
  label?: string;
  overlayClassName?: string;
  className?: string;
  onOpenChange?: (open: boolean) => void;
};

/** Matches the `md` breakpoint. The choice list is a popover from this width up. */
const DESKTOP_CHOICE_LIST_QUERY = "(min-width: 768px)";

/**
 * Subscribes to the desktop choice-list breakpoint.
 */
function subscribeDesktopChoiceList(onChange: () => void) {
  const media = window.matchMedia(DESKTOP_CHOICE_LIST_QUERY);
  media.addEventListener("change", onChange);
  return () => media.removeEventListener("change", onChange);
}

/**
 * Reports whether the choice list should open as a popover.
 * The server render is the phone sheet. The client switches at 768px.
 */
function useDesktopChoiceList() {
  return useSyncExternalStore(
    subscribeDesktopChoiceList,
    () => window.matchMedia(DESKTOP_CHOICE_LIST_QUERY).matches,
    () => false,
  );
}

/**
 * Choice list for a form or a detail row.
 * Under 768px it opens in a bottom sheet. From 768px up it opens in a popover anchored to the trigger. The list is portaled to document.body after the client mounts.
 */
export function Select({
  value,
  options,
  onChange,
  title,
  placeholder = "Select",
  disabled = false,
  variant = "field",
  label,
  overlayClassName,
  className,
  onOpenChange,
}: SelectProps) {
  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const desktop = useDesktopChoiceList();
  const mounted = useSyncExternalStore(
    () => () => {},
    () => true,
    () => false,
  );
  const selected = options.find((option) => option.value === value);
  const displayValue = selected?.label ?? placeholder;

  function setOpenState(next: boolean) {
    setOpen(next);
    onOpenChange?.(next);
  }

  /**
   * Applies the chosen value and closes the list.
   */
  function handleSelect(nextValue: string) {
    onChange(nextValue);
    setOpenState(false);
  }

  const triggerClassName =
    variant === "row"
      ? cn(
          "flex min-h-12 w-full items-center gap-3 text-left",
          disabled && "opacity-50",
          className,
        )
      : cn(
          "flex h-10 w-full min-w-0 items-center justify-between gap-2 rounded-lg border border-input bg-transparent px-2.5 text-left text-sm transition-colors outline-none",
          "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50",
          "disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50",
          className,
        );

  return (
    <>
      <button
        ref={triggerRef}
        type="button"
        disabled={disabled}
        onClick={() => setOpenState(!open)}
        aria-label={variant === "field" ? title : undefined}
        aria-expanded={open}
        aria-haspopup="listbox"
        className={triggerClassName}
      >
        {variant === "row" && label ? (
          <span className="shrink-0 text-sm font-medium text-foreground">
            {label}
          </span>
        ) : null}
        <span
          className={cn(
            "flex min-w-0 items-center gap-1.5",
            variant === "row"
              ? "flex-1 justify-end text-sm text-muted-foreground"
              : "flex-1 justify-between",
          )}
        >
          <span
            className={cn(
              "min-w-0 truncate",
              variant === "field" &&
                (selected ? "text-foreground" : "text-muted-foreground"),
            )}
          >
            {displayValue}
          </span>
          <ChevronDown
            className={cn(
              "size-4 shrink-0",
              variant === "field" && "text-muted-foreground",
            )}
            aria-hidden="true"
          />
        </span>
      </button>

      {mounted && desktop ? (
        <SelectPopover
          open={open}
          title={title}
          value={value}
          options={options}
          triggerRef={triggerRef}
          className={overlayClassName}
          onClose={() => setOpenState(false)}
          onSelect={handleSelect}
        />
      ) : null}

      {mounted && !desktop
        ? createPortal(
            <BottomSheet
              open={open}
              onClose={() => setOpenState(false)}
              title={title}
              headerAction="close"
              overlayClassName={cn("z-[110]", overlayClassName)}
              className="z-[110] max-h-[70vh]"
            >
              <SelectOptions
                options={options}
                value={value}
                layout="sheet"
                onSelect={handleSelect}
              />
            </BottomSheet>,
            document.body,
          )
        : null}
    </>
  );
}

type SelectPopoverProps = {
  open: boolean;
  title: string;
  value: string;
  options: SelectOption[];
  triggerRef: React.RefObject<HTMLButtonElement | null>;
  className?: string;
  onClose: () => void;
  onSelect: (value: string) => void;
};

/**
 * Choice list anchored to its trigger.
 * It closes on Escape or a press outside the list and the trigger, and it moves with the trigger while the page scrolls.
 */
function SelectPopover({
  open,
  title,
  value,
  options,
  triggerRef,
  className,
  onClose,
  onSelect,
}: SelectPopoverProps) {
  const popoverRef = useRef<HTMLDivElement>(null);
  const [placement, setPlacement] = useState<PopoverPlacement | null>(null);
  const focusedOnOpen = useRef(false);

  useLayoutEffect(() => {
    if (!open) {
      focusedOnOpen.current = false;
      return;
    }

    /**
     * Reads the trigger and stores the list position.
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
  }, [open, triggerRef]);

  useLayoutEffect(() => {
    if (!open || !placement || focusedOnOpen.current) {
      return;
    }

    focusedOnOpen.current = true;
    const selected = popoverRef.current?.querySelector<HTMLElement>(
      "[aria-selected='true']",
    );
    (selected ?? popoverRef.current?.querySelector("button"))?.focus();
  }, [open, placement]);

  useEffect(() => {
    if (!open) {
      return;
    }

    /**
     * Closes the list when the press lands outside the list and the trigger.
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
     * Closes the list when the key is Escape.
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
      role="listbox"
      aria-label={title}
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
      <SelectOptions
        options={options}
        value={value}
        layout="popover"
        onSelect={onSelect}
      />
    </div>,
    document.body,
  );
}

type SelectOptionsProps = {
  options: SelectOption[];
  value: string;
  layout: "sheet" | "popover";
  onSelect: (value: string) => void;
};

/**
 * Renders one choice per option and marks the current value.
 * The sheet layout uses full-width rows. The popover layout uses a shorter row with its own padding.
 */
function SelectOptions({
  options,
  value,
  layout,
  onSelect,
}: SelectOptionsProps) {
  return (
    <div
      className={
        layout === "sheet"
          ? "divide-y divide-border/70 border-y border-border/70"
          : "flex flex-col"
      }
    >
      {options.map((option) => {
        const isSelected = option.value === value;

        return (
          <button
            key={option.value || "__empty__"}
            type="button"
            role="option"
            aria-selected={isSelected}
            onClick={() => onSelect(option.value)}
            className={cn(
              "flex w-full items-center gap-3 text-left",
              layout === "sheet"
                ? "min-h-12 py-3"
                : "min-h-10 rounded-md px-3 py-2",
              "hover:bg-muted focus-visible:bg-muted focus-visible:outline-none",
            )}
          >
            <span className="min-w-0 flex-1 truncate text-sm font-medium text-foreground">
              {option.label}
            </span>
            {isSelected ? (
              <Check
                className="size-4 shrink-0 text-primary"
                aria-hidden="true"
              />
            ) : null}
          </button>
        );
      })}
    </div>
  );
}
