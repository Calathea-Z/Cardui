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
    <aside
      className={cn(
        "fixed inset-y-0 left-0 z-40 w-[85vw] max-w-sm border-r border-sidebar-border bg-sidebar px-5 py-7 text-sidebar-foreground transition-transform duration-300 ease-in-out",
        isOpen ? "translate-x-0" : "-translate-x-full",
      )}
      aria-hidden={!isOpen}
    >
      <BrandMark />

      <nav className="space-y-1">
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
    </aside>
  );
}
