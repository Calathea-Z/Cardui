/**
 * One destination: the visible label and its path.
 */
export type NavItem = {
  label: string;
  href: string;
};

/**
 * Primary destinations in sidebar and tab-bar order.
 * Home, Accounts, and Activity come first; Plan is fourth.
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
    href: "/activity",
  },
  {
    label: "Plan",
    href: "/plan",
  },
];

/**
 * Compact secondary destinations.
 * Plan setup groups the existing editors. Settings keeps the remaining household tools.
 */
export const secondaryNavItems: NavItem[] = [
  {
    label: "Plan setup",
    href: "/plan/setup",
  },
  {
    label: "Settings",
    href: "/settings",
  },
];

/**
 * Tools that stay in Settings.
 * Spending targets is reached from Activity. Connections is also reached from Accounts.
 */
export const settingsDestinations: NavItem[] = [
  {
    label: "Categories",
    href: "/categories",
  },
  {
    label: "Connections",
    href: "/connections",
  },
  {
    label: "Household",
    href: "/household",
  },
];

/**
 * Existing editor routes grouped by Plan setup.
 * The routes stay. This list does not merge their data.
 */
export const setupEditorItems: NavItem[] = [
  {
    label: "Income",
    href: "/income",
  },
  {
    label: "Bills",
    href: "/bills",
  },
  {
    label: "Debts",
    href: "/debts",
  },
  {
    label: "Plan budget",
    href: "/living",
  },
  {
    label: "Savings",
    href: "/savings",
  },
  {
    label: "Allocation",
    href: "/allocation",
  },
];

/**
 * Phone tab-bar destinations.
 * The list uses the same Home, Accounts, Activity, Plan order.
 */
export const bottomNavItems = navItems;

/**
 * Nav item for the current path.
 * `/plan` matches only that page. `/plan/setup` matches Plan setup.
 * Editor routes match their setup item. Settings tools match Settings destinations.
 */
export function getActiveNavItem(pathname: string): NavItem | undefined {
  if (pathname === "/") {
    return navItems.find((item) => item.href === "/");
  }

  if (pathname === "/plan" || pathname.startsWith("/plan?")) {
    return navItems.find((item) => item.href === "/plan");
  }

  const specific = [
    ...secondaryNavItems,
    ...settingsDestinations,
    ...setupEditorItems,
    ...navItems.filter((item) => item.href !== "/"),
  ];

  return specific.find(
    (item) => pathname === item.href || pathname.startsWith(`${item.href}/`),
  );
}
