"use client";

import { useEffect, useLayoutEffect, useRef, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { UserButton, useUser } from "@clerk/nextjs";
import { settingsDestinations } from "@/components/navigation/nav-items";
import { cn } from "@/lib/utils";

/**
 * Clerk user button for the signed-in account.
 * It renders UserButton with no extra props.
 */
export function AccountButton() {
  return <UserButton />;
}

/**
 * Phone account menu.
 * It opens Categories, Connections, and Household. Plan setup stays on Plan.
 * A press outside that list, or Escape, closes it.
 * Opening the list focuses the first destination so Tab moves through the rest.
 */
export function AccountMenu() {
  const { user } = useUser();
  const [isOpen, setIsOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const focusedOnOpen = useRef(false);
  const label =
    user?.primaryEmailAddress?.emailAddress ?? user?.fullName ?? "Account";

  useEffect(() => {
    if (!isOpen) {
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
  }, [isOpen]);

  useLayoutEffect(() => {
    if (!isOpen) {
      focusedOnOpen.current = false;
      return;
    }

    if (focusedOnOpen.current) {
      return;
    }

    focusedOnOpen.current = true;
    menuRef.current?.querySelector<HTMLElement>("a")?.focus();
  }, [isOpen]);

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

type SettingsLinksProps = {
  onNavigate?: () => void;
};

/**
 * Lists Categories, Connections, and Household.
 * Plan editors stay on Plan setup, so this menu does not repeat them.
 */
function SettingsLinks({ onNavigate }: SettingsLinksProps) {
  const pathname = usePathname();

  return (
    <nav className="space-y-1" aria-label="Settings">
      {settingsDestinations.map((item) => {
        const active =
          pathname === item.href || pathname.startsWith(`${item.href}/`);
        return (
          <Link
            key={item.href}
            href={item.href}
            role="menuitem"
            onClick={onNavigate}
            aria-current={active ? "page" : undefined}
            className={cn(
              "block min-h-11 rounded-lg px-3 py-2 text-sm font-medium text-foreground transition hover:bg-accent",
              "focus-visible:bg-muted focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/50",
              active && "text-primary",
            )}
          >
            {item.label}
          </Link>
        );
      })}
    </nav>
  );
}
