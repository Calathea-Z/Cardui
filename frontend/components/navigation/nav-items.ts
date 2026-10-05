/**
 * One destination: the visible label and its path.
 */
export type NavItem = {
  label: string;
  href: string;
};

/**
 * Primary destinations in sidebar and tab-bar order.
 * Home, Accounts, and Activity are the daily jobs. Income stays a route
 * under settings, not a fourth primary item.
 */
export const navItems: NavItem[] = [
  {
    label: "Home",
    href: "/",
  },
  {
    label: "Accounts",
    href: "/accounts",
  },
  {
    label: "Activity",
    href: "/transactions",
  },
];

/**
 * Destinations in the account block.
 * These stay off the primary nav, including the real Income route.
 */
export const settingsItems: NavItem[] = [
  {
    label: "Income",
    href: "/income",
  },
  {
    label: "Categories",
    href: "/categories",
  },
  {
    label: "Connections",
    href: "/institutions",
  },
  {
    label: "Household",
    href: "/household",
  },
];

/**
 * Phone tab-bar destinations.
 * The list is the three primary items.
 */
export const bottomNavItems = navItems;

/**
 * Nav item for the current path.
 * The path `/` matches the `/` item. Any other path matches the first primary
 * or settings item with a non-root href that pathname starts with.
 */
export function getActiveNavItem(pathname: string): NavItem | undefined {
  const items = [...navItems, ...settingsItems];

  if (pathname === "/") {
    return items.find((item) => item.href === "/");
  }

  return items.find(
    (item) => item.href !== "/" && pathname.startsWith(item.href),
  );
}
