import { cn } from "@/lib/utils";
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
        "fixed inset-y-0 left-0 z-40 w-[85vw] max-w-sm border-r border-border bg-sidebar px-4 py-6 transition-transform duration-300 ease-in-out",
        isOpen ? "translate-x-0" : "-translate-x-full",
      )}
      aria-hidden={!isOpen}
    >
      <div className="mb-8">
        <p className="text-sm text-muted-foreground">Cardui</p>
        <h1 className="text-xl font-semibold text-foreground">Finance</h1>
      </div>

      <nav className="space-y-1">
        {navItems.map((item) => (
          <NavLink
            key={item.href}
            item={item}
            showIcon
            onNavigate={onClose}
            className="flex cursor-pointer items-center gap-3 rounded-md px-3 py-3 text-base transition"
            activeClassName="bg-accent text-primary"
            inactiveClassName="text-muted-foreground hover:bg-accent hover:text-foreground"
          />
        ))}
      </nav>
    </aside>
  );
}
