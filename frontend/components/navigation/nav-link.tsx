"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { getNavIcon } from "./nav-icons";
import type { NavItem } from "./nav-items";

type NavLinkProps = {
  item: NavItem;
  className?: string;
  activeClassName?: string;
  inactiveClassName?: string;
  showIcon?: boolean;
  onNavigate?: () => void;
};

function isActiveRoute(pathname: string, href: string) {
  if (href === "/") {
    return pathname === "/";
  }

  return pathname.startsWith(href);
}

export function NavLink({
  item,
  className,
  activeClassName,
  inactiveClassName,
  showIcon = false,
  onNavigate,
}: NavLinkProps) {
  const pathname = usePathname();
  const isActive = isActiveRoute(pathname, item.href);
  const Icon = getNavIcon(item.href);

  return (
    <Link
      href={item.href}
      onClick={onNavigate}
      className={cn(
        className,
        isActive ? activeClassName : inactiveClassName,
      )}
    >
      {showIcon && <Icon className="size-5 shrink-0" aria-hidden />}
      <span>{item.label}</span>
    </Link>
  );
}
