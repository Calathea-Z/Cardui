"use client";

import { useSyncExternalStore } from "react";

/** Matches the `md` breakpoint. A choice opens as a popover from this width up. */
const DESKTOP_CHOICE_LIST_QUERY = "(min-width: 768px)";

/**
 * Subscribes to the desktop choice breakpoint.
 */
function subscribeDesktopChoiceList(onChange: () => void) {
  const media = window.matchMedia(DESKTOP_CHOICE_LIST_QUERY);
  media.addEventListener("change", onChange);
  return () => media.removeEventListener("change", onChange);
}

/**
 * Reports whether a choice should open as a popover.
 * The server render is the phone sheet. The client switches at 768px.
 */
export function useDesktopChoiceList() {
  return useSyncExternalStore(
    subscribeDesktopChoiceList,
    () => window.matchMedia(DESKTOP_CHOICE_LIST_QUERY).matches,
    () => false,
  );
}
