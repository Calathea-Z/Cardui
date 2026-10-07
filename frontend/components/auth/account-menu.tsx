"use client";

import { useEffect, useLayoutEffect, useRef, useState } from "react";
import Link from "next/link";
import { UserButton, useUser } from "@clerk/nextjs";
import { settingsItems } from "@/components/navigation/nav-items";
import { cn } from "@/lib/utils";

type AccountMenuProps = {
  variant?: "sidebar" | "header";
};

/**
 * Clerk user button for the signed-in account.
 * It renders UserButton with no extra props.
 */
export function AccountButton() {
  return <UserButton />;
}

/**
 * Account block with settings destinations and the Clerk button.
 * The sidebar lists those links. The header opens the same list from the account control.
 * A press outside that list, or Escape, closes it.
 * Opening the list focuses the first destination so Tab moves through the rest.
 */
export function AccountMenu({ variant = "sidebar" }: AccountMenuProps) {
  const { user } = useUser();
  const [isOpen, setIsOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const focusedOnOpen = useRef(false);
  const label =
    user?.primaryEmailAddress?.emailAddress ?? user?.fullName ?? "Account";

  useEffect(() => {
    if (variant !== "header" || !isOpen) {
      return;
    }

    /**
     * Closes the account menu on Escape, and moves Tab into the list from the email.
     * The Clerk account card handles its own Escape while that card is open.
     */
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Tab") {
        const items = [
          ...(menuRef.current?.querySelectorAll<HTMLElement>("a") ?? []),
        ];
        const first = items[0];
        const last = items[items.length - 1];
        if (!first || !last) {
          return;
        }

        if (document.activeElement === triggerRef.current) {
          event.preventDefault();
          (event.shiftKey ? last : first).focus();
        }

        return;
      }

      if (event.key !== "Escape") {
        return;
      }

      if (document.querySelector(".cl-userButtonPopoverCard")) {
        return;
      }

      event.stopPropagation();
      setIsOpen(false);
      triggerRef.current?.focus();
    }

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [variant, isOpen]);

  useLayoutEffect(() => {
    if (variant !== "header" || !isOpen) {
      focusedOnOpen.current = false;
      return;
    }

    if (focusedOnOpen.current) {
      return;
    }

    focusedOnOpen.current = true;
    menuRef.current?.querySelector<HTMLElement>("a")?.focus();
  }, [variant, isOpen]);

  if (variant === "header") {
    return (
      <div className="relative">
        <button
          ref={triggerRef}
          type="button"
          aria-expanded={isOpen}
          aria-haspopup="menu"
          onClick={() => setIsOpen((open) => !open)}
          className={cn(
            "max-w-40 truncate rounded-lg px-2 py-1.5 text-sm font-medium text-foreground",
            isOpen && "relative z-50",
          )}
        >
          {label}
        </button>
        {isOpen ? (
          <>
            <button
              type="button"
              aria-label="Close account menu"
              tabIndex={-1}
              className="fixed inset-0 z-40 cursor-default bg-transparent"
              onClick={() => {
                setIsOpen(false);
                triggerRef.current?.focus();
              }}
            />
            <div
              ref={menuRef}
              role="menu"
              className="absolute top-full right-0 z-50 mt-2 min-w-52 rounded-lg border border-border bg-popover py-2"
            >
              <SettingsLinks onNavigate={() => setIsOpen(false)} />
              <div className="mt-2 flex items-center gap-3 border-t border-border px-3 pt-2">
                <AccountButton />
                <span className="min-w-0 truncate text-sm font-medium">
                  {label}
                </span>
              </div>
            </div>
          </>
        ) : null}
      </div>
    );
  }

  return (
    <div className="mt-auto shrink-0 border-t border-sidebar-border pt-4">
      <SettingsLinks />
      <div className="mt-2 flex min-w-0 items-center gap-3 rounded-lg bg-sidebar-accent px-3 py-2.5">
        <AccountButton />
        <span className="min-w-0 truncate text-sm font-medium text-sidebar-foreground">
          {label}
        </span>
      </div>
    </div>
  );
}

type SettingsLinksProps = {
  onNavigate?: () => void;
};

/**
 * Lists settings destinations in the account block.
 * Income, Bills, Debts, Targets, Categories, Connections, and Household stay off the primary nav.
 */
function SettingsLinks({ onNavigate }: SettingsLinksProps) {
  return (
    <nav className="space-y-1" aria-label="Settings">
      {settingsItems.map((item) => (
        <Link
          key={item.href}
          href={item.href}
          onClick={onNavigate}
          className={cn(
            "block min-h-11 rounded-lg px-3 py-2 text-sm font-medium text-sidebar-foreground/80 transition hover:bg-sidebar-accent hover:text-sidebar-foreground",
            "focus-visible:bg-muted focus-visible:text-foreground focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/50",
          )}
        >
          {item.label}
        </Link>
      ))}
    </nav>
  );
}
