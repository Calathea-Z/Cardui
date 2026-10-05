const GAP = 4;
const MARGIN = 8;
const DEFAULT_MIN_WIDTH = 192;
const DEFAULT_MAX_HEIGHT = 320;
const PREFER_BELOW_IF_AT_LEAST = 160;

export type PopoverAnchor = {
  top: number;
  bottom: number;
  left: number;
  width: number;
};

export type PopoverViewport = {
  width: number;
  height: number;
};

/**
 * Where the choice list sits relative to its trigger.
 * `top` is the offset from the viewport top when `side` is below.
 * `bottom` is the offset from the viewport bottom when `side` is above.
 */
export type PopoverPlacement = {
  side: "above" | "below";
  top: number;
  bottom: number;
  left: number;
  width: number;
  maxHeight: number;
};

/**
 * Optional size for a popover that is wider or taller than a choice list.
 * Width and height still shrink to the room inside the viewport.
 */
export type PopoverSize = {
  minWidth?: number;
  maxHeight?: number;
  /** When true, the width stays at minWidth instead of growing with the trigger. */
  fixedWidth?: boolean;
};

/**
 * Places a popover under its trigger, or above it when the room below is small.
 * It stays inside the viewport. It matches the trigger width, and it is at least 12rem unless a larger minimum is requested. A fixed width stays at that minimum.
 */
export function placeSelectPopover(
  anchor: PopoverAnchor,
  viewport: PopoverViewport,
  size?: PopoverSize,
): PopoverPlacement {
  const minWidth = size?.minWidth ?? DEFAULT_MIN_WIDTH;
  const maxHeight = size?.maxHeight ?? DEFAULT_MAX_HEIGHT;
  const availableWidth = Math.max(0, viewport.width - MARGIN * 2);
  const preferredWidth = size?.fixedWidth
    ? minWidth
    : Math.max(anchor.width, minWidth);
  const width = Math.min(preferredWidth, availableWidth);
  let left = anchor.left;

  if (left + width > viewport.width - MARGIN) {
    left = viewport.width - MARGIN - width;
  }

  if (left < MARGIN) {
    left = MARGIN;
  }

  const spaceBelow = viewport.height - anchor.bottom - GAP - MARGIN;
  const spaceAbove = anchor.top - GAP - MARGIN;
  const side =
    spaceBelow >= PREFER_BELOW_IF_AT_LEAST || spaceBelow >= spaceAbove
      ? "below"
      : "above";
  const room = Math.max(0, side === "below" ? spaceBelow : spaceAbove);

  return {
    side,
    top: anchor.bottom + GAP,
    bottom: viewport.height - anchor.top + GAP,
    left,
    width,
    maxHeight: Math.min(maxHeight, room),
  };
}
