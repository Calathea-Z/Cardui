"use client";

import { useSyncExternalStore } from "react";
import { createPortal } from "react-dom";
import { AnchoredPopover } from "@/components/ui/anchored-popover";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { useDesktopChoiceList } from "@/components/ui/use-desktop-choice-list";
import { cn } from "@/lib/utils";

type ChoiceSurfaceProps = {
  open: boolean;
  title: string;
  triggerRef: { current: HTMLElement | null };
  onClose: () => void;
  children: React.ReactNode;
  overlayClassName?: string;
  /** Smallest popover width in pixels. The phone sheet ignores this. */
  minWidth?: number;
  /** Tallest popover height in pixels. The phone sheet ignores this. */
  maxHeight?: number;
  /** Keeps the popover at minWidth. The phone sheet ignores this. */
  fixedWidth?: boolean;
};

/**
 * Shows picker content in a bottom sheet under 768px and in a popover from 768px up.
 * The popover is anchored to the trigger and uses the same placement as Select.
 */
export function ChoiceSurface({
  open,
  title,
  triggerRef,
  onClose,
  children,
  overlayClassName,
  minWidth,
  maxHeight,
  fixedWidth,
}: ChoiceSurfaceProps) {
  const desktop = useDesktopChoiceList();
  const mounted = useSyncExternalStore(
    () => () => {},
    () => true,
    () => false,
  );

  if (!mounted) {
    return null;
  }

  if (desktop) {
    return (
      <AnchoredPopover
        open={open}
        label={title}
        triggerRef={triggerRef}
        onClose={onClose}
        minWidth={minWidth}
        maxHeight={maxHeight}
        fixedWidth={fixedWidth}
        className={cn("p-3", overlayClassName)}
      >
        {children}
      </AnchoredPopover>
    );
  }

  return createPortal(
    <BottomSheet
      open={open}
      onClose={onClose}
      title={title}
      headerAction="close"
      overlayClassName={cn("z-[110]", overlayClassName)}
      className="z-[110] max-h-[85vh]"
    >
      {children}
    </BottomSheet>,
    document.body,
  );
}
