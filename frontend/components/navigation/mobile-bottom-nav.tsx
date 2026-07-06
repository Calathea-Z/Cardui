"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { getNavIcon } from "./nav-icons";
import { navItems } from "./nav-items";

function isActiveRoute(pathname: string, href: string) {
  if (href === "/") {
    return pathname === "/";
  }

  return pathname.startsWith(href);
}

export function MobileBottomNav() {
  const pathname = usePathname();

  return (
    <nav
      className="fixed inset-x-0 bottom-0 z-30 border-t border-slate-800 bg-slate-950 pb-[env(safe-area-inset-bottom)]"
      aria-label="Main navigation"
    >
      <div className="grid grid-cols-5">
        {navItems.map((item) => {
          const isActive = isActiveRoute(pathname, item.href);
          const Icon = getNavIcon(item.href);

          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex min-h-16 flex-col items-center justify-center gap-1 px-1 py-2 text-[10px] transition",
                isActive
                  ? "text-emerald-400"
                  : "text-slate-400 hover:text-slate-200",
              )}
            >
              <Icon className="size-5 shrink-0" aria-hidden />
              <span className="truncate">{item.label}</span>
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
