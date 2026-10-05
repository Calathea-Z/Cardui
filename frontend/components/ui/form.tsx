import type { ComponentProps } from "react";

/**
 * Form that keeps validation in the app.
 * The browser's tooltip stays off, so a missing field uses the screen's own message.
 */
export function Form({ className, ...props }: ComponentProps<"form">) {
  return <form {...props} className={className} noValidate />;
}
