"use client";

import type { CSSProperties } from "react";
import { Toaster as SonnerToaster } from "sonner";

/**
 * Mounts Sonner so an action can show a toast.
 * The toast uses the app colors and sits below the phone header.
 */
export function Toaster() {
  return (
    <SonnerToaster
      theme="light"
      position="top-center"
      closeButton
      richColors
      duration={8000}
      offset={24}
      mobileOffset={{
        top: "calc(3.75rem + env(safe-area-inset-top) + 12px)",
      }}
      style={toasterStyle}
    />
  );
}

const toasterStyle = {
  fontFamily: "inherit",
  "--normal-bg": "var(--card)",
  "--normal-border": "var(--border)",
  "--normal-text": "var(--foreground)",
  "--success-bg": "color-mix(in oklch, var(--success) 12%, var(--card))",
  "--success-border": "color-mix(in oklch, var(--success) 35%, var(--border))",
  "--success-text": "var(--success)",
  "--error-bg": "color-mix(in oklch, var(--destructive) 12%, var(--card))",
  "--error-border":
    "color-mix(in oklch, var(--destructive) 40%, var(--border))",
  "--error-text": "var(--destructive)",
} as CSSProperties;
