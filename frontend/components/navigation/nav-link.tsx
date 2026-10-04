"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { NavIcon } from "./nav-icons";
import type { NavItem } from "./nav-items";

type NavLinkProps = {
  item: NavItem;
  className?: string;
  activeClassName?: string;
  inactiveClassName?: string;
  showIcon?: boolean;
  onNavigate?: () => void;
};

/**
 * Reports whether pathname belongs to a nav href.
 * The href `/` matches only that exact path; every other href matches the path and routes under it.
 */
function isActiveRoute(pathname: string, href: string) {
  if (href === "/") {
    return pathname === "/";
  }

  return pathname.startsWith(href);
}

/**
 * Link to one primary destination.
 * Active and inactive classes follow isActiveRoute for the current path, and onNavigate runs when the link is chosen.
 */
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

  return (
    <Link
      href={item.href}
      onClick={onNavigate}
      className={cn(className, isActive ? activeClassName : inactiveClassName)}
    >
      {showIcon && <NavIcon href={item.href} />}
      <span>{item.label}</span>
    </Link>
  );
}
