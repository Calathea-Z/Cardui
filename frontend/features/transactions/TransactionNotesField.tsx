"use client";

import { useLayoutEffect, useRef } from "react";
import { cn } from "@/lib/utils";

type TransactionNotesFieldProps = {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
};

const MAX_NOTES_LENGTH = 1000;

function resizeTextarea(element: HTMLTextAreaElement) {
  element.style.height = "auto";
  element.style.height = `${element.scrollHeight}px`;
}

export function TransactionNotesField({
  value,
  onChange,
  disabled = false,
}: TransactionNotesFieldProps) {
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  useLayoutEffect(() => {
    if (textareaRef.current) {
      resizeTextarea(textareaRef.current);
    }
  }, [value]);

  return (
    <label
      className={cn(
        "flex min-h-12 items-start gap-3 py-3",
        disabled && "opacity-50",
      )}
    >
      <span className="shrink-0 pt-0.5 text-sm font-medium text-foreground">
        Notes
      </span>
      <textarea
        ref={textareaRef}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        maxLength={MAX_NOTES_LENGTH}
        disabled={disabled}
        placeholder="Add a note"
        rows={1}
        className={cn(
          "min-h-5 min-w-0 flex-1 resize-none overflow-hidden border-0 bg-transparent py-0 text-right text-sm leading-5 text-muted-foreground outline-none",
          "placeholder:text-muted-foreground/70",
          "disabled:cursor-not-allowed",
          "break-all whitespace-pre-wrap",
        )}
        aria-label="Notes"
      />
    </label>
  );
}
