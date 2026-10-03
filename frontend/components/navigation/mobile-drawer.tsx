import { AccountMenu } from "@/components/auth/account-menu";
import { cn } from "@/lib/utils";
import { BrandMark } from "./brand-mark";
import { NavLink } from "./nav-link";
import { navItems } from "./nav-items";

type MobileDrawerProps = {
  isOpen: boolean;
  onClose: () => void;
};

export function MobileDrawer({ isOpen, onClose }: MobileDrawerProps) {
  return (
    <>
      {isOpen ? (
        <button
          type="button"
          aria-label="Close navigation menu"
          className="fixed inset-0 z-[60] bg-black/40 md:hidden"
          onClick={onClose}
        />
      ) : null}
      <aside
        className={cn(
          "fixed top-0 left-0 z-[70] flex h-dvh max-h-dvh w-[85vw] max-w-sm flex-col overflow-hidden overscroll-none border-r border-sidebar-border bg-sidebar px-5 pb-7 pt-[calc(4.5rem+env(safe-area-inset-top))] text-sidebar-foreground transition-transform duration-300 ease-in-out md:hidden",
          isOpen ? "translate-x-0" : "-translate-x-full",
        )}
        aria-hidden={!isOpen}
      >
      <BrandMark />

      <nav className="min-h-0 flex-1 space-y-1 overflow-y-auto overscroll-none">
        {navItems.map((item) => (
          <NavLink
            key={item.href}
            item={item}
            showIcon
            onNavigate={onClose}
            className="flex cursor-pointer items-center gap-3 rounded-lg border-l-2 border-transparent px-3 py-3 text-base font-medium transition"
            activeClassName="border-l-sidebar-primary bg-sidebar-accent text-sidebar-primary"
            inactiveClassName="text-sidebar-foreground/65 hover:bg-sidebar-accent/70 hover:text-sidebar-foreground"
          />
        ))}
      </nav>
      <AccountMenu />
      </aside>
    </>
  );
}
