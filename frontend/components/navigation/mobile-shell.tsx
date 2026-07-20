"use client";

import { Menu, X } from "lucide-react";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { getActiveNavItem } from "./nav-items";
import { MobileBottomNav } from "./mobile-bottom-nav";
import { MobileDrawer } from "./mobile-drawer";
import { MobileHeaderActionsSlot, MobileHeaderLeadingSlot } from "./mobile-header-actions";

type MobileShellProps = {
  children: React.ReactNode;
};

export function MobileShell({ children }: MobileShellProps) {
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const pathname = usePathname();
  const activeItem = getActiveNavItem(pathname);

  useEffect(() => {
    if (isDrawerOpen) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "";
    }

    return () => {
      document.body.style.overflow = "";
    };
  }, [isDrawerOpen]);

  return (
    <div className="relative flex min-w-0 flex-1 flex-col">
      <div className="md:hidden">
        <MobileDrawer
          isOpen={isDrawerOpen}
          onClose={() => setIsDrawerOpen(false)}
        />
      </div>

      <div
        className={cn(
          "relative z-50 flex min-h-screen flex-col bg-background transition-transform duration-300 ease-in-out md:min-h-0 md:translate-x-0",
          isDrawerOpen && "translate-x-[85vw]",
        )}
      >
        {isDrawerOpen && (
          <button
            type="button"
            aria-label="Close navigation menu"
            className="fixed inset-0 z-45 bg-black/40 md:hidden"
            onClick={() => setIsDrawerOpen(false)}
          />
        )}

        <header className="fixed top-0 inset-x-0 z-50 border-b border-border bg-background px-4 py-3 pt-[max(0.75rem,env(safe-area-inset-top))] md:hidden">
          <div className="grid grid-cols-[1fr_auto_1fr] items-center gap-2">
            <div className="flex justify-start">
              <MobileHeaderLeadingSlot
                fallback={
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon-lg"
                    aria-label={isDrawerOpen ? "Close menu" : "Open menu"}
                    aria-expanded={isDrawerOpen}
                    onClick={() => setIsDrawerOpen((open) => !open)}
                    className="shrink-0"
                  >
                    {isDrawerOpen ? (
                      <X className="size-5" />
                    ) : (
                      <Menu className="size-5" />
                    )}
                  </Button>
                }
              />
            </div>

            <p className="truncate text-center font-semibold">
              {activeItem?.label ?? "Finance"}
            </p>

            <div className="flex justify-end">
              <MobileHeaderActionsSlot />
            </div>
          </div>
        </header>

        <main className="min-w-0 flex-1 pt-[calc(4.5rem+env(safe-area-inset-top))] pb-[calc(4rem+env(safe-area-inset-bottom))] md:pb-0 md:pt-0">
          {children}
        </main>

        <div className="md:hidden">
          <MobileBottomNav />
        </div>
      </div>
    </div>
  );
}
