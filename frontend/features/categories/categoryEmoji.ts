import type { TransactionCategoryDto } from "@/lib/api";

/** Default emojis for system category keys. */
const CATEGORY_KEY_EMOJIS: Record<string, string> = {
  income: "💰",
  groceries: "🛒",
  dining: "🍽️",
  bills: "🧾",
  transport: "🚗",
  shopping: "🛍️",
  entertainment: "🎬",
  transfers: "↔️",
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
  category: Pick<TransactionCategoryDto, "key" | "icon" | "name"> | null | undefined,
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
  if (name.includes("grocer")) return CATEGORY_KEY_EMOJIS.groceries;
  if (name.includes("dining") || name.includes("food")) return CATEGORY_KEY_EMOJIS.dining;
  if (name.includes("bill")) return CATEGORY_KEY_EMOJIS.bills;
  if (name.includes("transport") || name.includes("travel")) {
    return CATEGORY_KEY_EMOJIS.transport;
  }
  if (name.includes("shop")) return CATEGORY_KEY_EMOJIS.shopping;
  if (name.includes("entertain")) return CATEGORY_KEY_EMOJIS.entertainment;

  return DEFAULT_CATEGORY_EMOJI;
}
