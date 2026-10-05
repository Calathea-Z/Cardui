import {
  placeSelectPopover,
  type PopoverAnchor,
  type PopoverPlacement,
  type PopoverViewport,
} from "./select-popover";

const MENU_WIDTH = 192;
const MARGIN = 8;

/**
 * Places an action menu so the whole menu stays in the page.
 * It lines up with the trigger's right edge. When that runs off the left, including under the sidebar, the menu shifts right until it fits.
 * `contentLeft` is the left edge of the page column. Zero means the column starts at the viewport.
 */
export function placeActionMenu(
  anchor: PopoverAnchor,
  viewport: PopoverViewport,
  contentLeft = 0,
): PopoverPlacement {
  const placement = placeSelectPopover(anchor, viewport, {
    minWidth: MENU_WIDTH,
    fixedWidth: true,
  });
  const minLeft = Math.max(MARGIN, contentLeft);
  const maxRight = viewport.width - MARGIN;
  const width = Math.min(placement.width, Math.max(0, maxRight - minLeft));
  let left = anchor.left + anchor.width - width;

  if (left < minLeft) {
    left = minLeft;
  }

  if (left + width > maxRight) {
    left = Math.max(minLeft, maxRight - width);
  }

  return { ...placement, left, width };
}
