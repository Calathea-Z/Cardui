import type { TransactionCategoryDto } from "@/lib/api/types";

/** Default emojis for system category keys. */
const CATEGORY_KEY_EMOJIS: Record<string, string> = {
  income: "💰",
  transfers: "↔️",
  "food-dining": "🍽️",
  "auto-transport": "🚗",
  "bills-utilities": "🧾",
  shopping: "🛍️",
  "travel-lifestyle": "🎬",
  other: "📦",
  "gifts-donations": "🎁",
  housing: "🏠",
  children: "🧒",
  education: "📚",
  "health-wellness": "💊",
  financial: "🏦",
  business: "💼",
  // Legacy keys (pre Groups/SubGroups cutover)
  groceries: "🛒",
  dining: "🍽️",
  bills: "🧾",
  transport: "🚗",
  entertainment: "🎬",
  uncategorized: "❔",
};

/** Map legacy lucide-style icon slugs (and emoji values) to display emojis. */
const CATEGORY_ICON_EMOJIS: Record<string, string> = {
  banknote: "💰",
  "shopping-basket": "🛒",
  utensils: "🍽️",
  receipt: "🧾",
  car: "🚗",
  "shopping-bag": "🛍️",
  ticket: "🎬",
  repeat: "↔️",
  "circle-help": "❔",
  ...CATEGORY_KEY_EMOJIS,
};

const DEFAULT_CATEGORY_EMOJI = "❔";

export function getCategoryEmoji(
  category:
    Pick<TransactionCategoryDto, "key" | "icon" | "name"> | null | undefined,
): string {
  if (!category) {
    return DEFAULT_CATEGORY_EMOJI;
  }

  const key = category.key?.toLowerCase().trim();
  if (key && CATEGORY_KEY_EMOJIS[key]) {
    return CATEGORY_KEY_EMOJIS[key];
  }

  const icon = category.icon?.trim();
  if (icon) {
    // Already an emoji (or other short glyph stored directly).
    if ([...icon].length <= 3 && !/^[a-z0-9-]+$/i.test(icon)) {
      return icon;
    }

    const fromIcon = CATEGORY_ICON_EMOJIS[icon.toLowerCase()];
    if (fromIcon) {
      return fromIcon;
    }
  }

  const name = category.name?.toLowerCase() ?? "";
  if (name.includes("transfer")) return CATEGORY_KEY_EMOJIS.transfers;
  if (name.includes("income")) return CATEGORY_KEY_EMOJIS.income;
  if (name.includes("gift") || name.includes("donation")) {
    return CATEGORY_KEY_EMOJIS["gifts-donations"];
  }
  if (
    name.includes("grocer") ||
    name.includes("dining") ||
    name.includes("food")
  ) {
    return CATEGORY_KEY_EMOJIS["food-dining"];
  }
  if (name.includes("bill") || name.includes("utilit")) {
    return CATEGORY_KEY_EMOJIS["bills-utilities"];
  }
  if (name.includes("auto") || name.includes("transport")) {
    return CATEGORY_KEY_EMOJIS["auto-transport"];
  }
  if (
    name.includes("hous") ||
    name.includes("rent") ||
    name.includes("mortgage")
  ) {
    return CATEGORY_KEY_EMOJIS.housing;
  }
  if (
    name.includes("travel") ||
    name.includes("lifestyle") ||
    name.includes("entertain")
  ) {
    return CATEGORY_KEY_EMOJIS["travel-lifestyle"];
  }
  if (name.includes("shop")) return CATEGORY_KEY_EMOJIS.shopping;
  if (name.includes("child")) return CATEGORY_KEY_EMOJIS.children;
  if (name.includes("educat") || name.includes("school")) {
    return CATEGORY_KEY_EMOJIS.education;
  }
  if (name.includes("health") || name.includes("wellness")) {
    return CATEGORY_KEY_EMOJIS["health-wellness"];
  }
  if (name.includes("financ")) return CATEGORY_KEY_EMOJIS.financial;
  if (name.includes("business")) return CATEGORY_KEY_EMOJIS.business;
  if (name.includes("other")) return CATEGORY_KEY_EMOJIS.other;

  return DEFAULT_CATEGORY_EMOJI;
}
