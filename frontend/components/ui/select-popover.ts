const GAP = 4;
const MARGIN = 8;
const MIN_WIDTH = 192;
const MAX_LIST_HEIGHT = 320;
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
 * Places a choice list under its trigger, or above it when the room below is small.
 * The list stays inside the viewport. It matches the trigger width, and it is at least 12rem when the viewport allows.
 */
export function placeSelectPopover(
  anchor: PopoverAnchor,
  viewport: PopoverViewport,
): PopoverPlacement {
  const availableWidth = Math.max(0, viewport.width - MARGIN * 2);
  const width = Math.min(Math.max(anchor.width, MIN_WIDTH), availableWidth);
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
    maxHeight: Math.min(MAX_LIST_HEIGHT, room),
  };
}
