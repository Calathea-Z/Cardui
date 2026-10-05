"use client";

import { AccountMenu } from "@/components/auth/account-menu";
import {
  MobileHeaderActionsSlot,
  MobileHeaderLeadingSlot,
} from "./mobile-header-actions";
import { MobileBottomNav } from "./mobile-bottom-nav";

type MobileShellProps = {
  children: React.ReactNode;
};

/**
 * Page column with a thin account bar and tab bar on small screens.
 * That chrome hides from the md breakpoint up. The hamburger drawer is gone;
 * settings open from the account menu.
 */
export function MobileShell({ children }: MobileShellProps) {
  return (
    <div className="relative flex min-w-0 flex-1 flex-col">
      <div className="flex min-h-screen flex-col bg-background md:min-h-0">
        <header className="fixed top-0 inset-x-0 z-[80] border-b border-border bg-background px-4 py-3 pt-[max(0.75rem,env(safe-area-inset-top))] md:hidden">
          <div className="flex items-center justify-between gap-2">
            <div className="flex justify-start">
              <MobileHeaderLeadingSlot fallback={null} />
            </div>

            <div className="flex items-center justify-end gap-1">
              <MobileHeaderActionsSlot />
              <AccountMenu variant="header" />
            </div>
          </div>
        </header>

        <main className="min-w-0 flex-1 pt-[calc(3.75rem+env(safe-area-inset-top))] pb-[calc(4rem+env(safe-area-inset-bottom))] md:pb-0 md:pt-0">
          {children}
        </main>

        <div className="md:hidden">
          <MobileBottomNav />
        </div>
      </div>
    </div>
  );
}
