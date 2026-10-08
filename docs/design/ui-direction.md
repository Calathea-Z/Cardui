# UI direction

Status: Implemented. Approved October 5, 2026 and built the same day.
Updated: 2026-10-07

The enforceable conventions for new screens are in `.cursor/rules/ui-governance.mdc`. Where this note and that rule differ, the rule wins. This note keeps the reasoning behind the shell, the visual system, and the map.

Built in `docs/reviews/2026-10-05-002-light-shell-home.md` (light shell and Home), `docs/reviews/2026-10-05-003-detail-surface.md` (right-hand panel), `docs/reviews/2026-10-05-004-picker-popovers.md` (popovers), `docs/reviews/2026-10-05-005-accounts-chart-header.md`, and `docs/reviews/2026-10-05-007-signed-in-layout.md`.

The signed-in app is branded **Tortoise** (`frontend/app/layout.tsx`). The repo and roadmap call it Cardui. This note is about the Next.js app in `frontend/` (Next.js 16, React 19, Tailwind 4).

## 1. What this direction replaced (historical)

Before October 5, 2026, the app was a dark journal theme in a phone shell. At 768px and up it showed a dark sidebar beside the same phone column. It had two navigations on a phone (a four-item tab bar and a drawer with all six destinations), no page title on the main desktop pages, a swipe carousel on Home at every width, and bottom sheets for detail and choices on a laptop. It loaded three faces and painted wallpaper, a panel rail, and a highlighter behind figures. None of that is in the current app. The full description is in git history, in the version of this file from `dbe4c27`.

## 2. What the product should feel like

Tortoise is one light finance product. The picture of the money, the chrome, and the map are the same choices at every width.

### The picture

Home leads with one number and one chart: net worth over time, with the accounts gathered under it. The other picture is the one Tortoise already has data for: a category name, a thin bar, and the amount spent.

Home is that pair. One net-worth chart. One this-month block with money in, money out, and a short category list. Recent activity sits under both, as rows, not as a third product.

Leave these out: a serif display face, cream as a brand, a promo banner, a download-an-app path, and a second budgeting mode. Tortoise has no budget data yet. Do not invent one.

### The chrome

The page is a light canvas and a left sidebar. Active navigation uses one accent. There is no paper texture, no script wordmark, and no highlighter behind a number.

Money in, money out, and a short category list use that same light frame.

Leave these out: a second brand color that fills the room, a display face used only for headings, and a long catalog of products in the sidebar. Tortoise has three daily jobs. Extra destinations would make the split worse.

### The map

Home, Accounts, Activity, and Plan are the objects, in that order. Income, bills, debts, Plan budget, savings, spending targets, categories, connections, and household are settings. A chart on Home answers one question, and the rows under it are the drill-down. Filters change that view. They do not open a second app.

The dark canvas is gone. One light theme, and no second theme.

Leave these out: a custom dashboard builder, a pile of measures, marketing-size headlines, an AI prompt, a logo wall, and team or payables reporting. Clicking a chart later can open the rows underneath. It does not need a new viewer in this pass.

## 3. Visual system

Change tokens and type. Keep the Tailwind token names (`--background`, `--card`, `--primary`, and the rest) so screens do not need a class rewrite to pick up the palette. Drop the decorative layer.

### Type

Use **Inter**, loaded with `next/font/google` the same way Sora is loaded now. No new package. Inter is the one product sans for titles, navigation, and money.

Remove Shantell Sans and JetBrains Mono from `layout.tsx`. Page titles stop using `font-brand`. Money uses the same sans with `tabular-nums`. A code face on every balance is why the ledger feels like a terminal.

| Role | Size | Weight | Notes |
| --- | --- | --- | --- |
| Page title | 1.75rem | 600 | Tracking `-0.02em`. One per primary page. |
| Big figure | 2rem | 600 | Tabular numbers. One per card, not a highlighter. |
| Section title | 0.875rem | 600 | Sentence case. |
| Body, nav, rows | 0.875rem | 400 / 500 | Nav labels at 500. |
| Meta, tab label | 0.75rem | 500 | Phone tab labels were 10px before this direction. 12px is the floor. |

Eyebrows in 11px with `0.18em` tracking go away. They are the journal voice.

### Color

Light only. Stop forcing `class="dark"` on `<html>`.

| Token | Value | Use |
| --- | --- | --- |
| `--background`, `--sidebar` | `#f3f2ee` | Page and sidebar. Same color, so the chrome is one surface. |
| `--card`, `--popover`, `--input` | `#ffffff` | Cards, sheets, fields. |
| `--foreground` | `#1c1b19` | Text and figures. |
| `--muted` | `#eeede8` | Bar tracks, hover. |
| `--muted-foreground` | `#6d6a64` | Meta. |
| `--border`, `--sidebar-border` | `#e3e0d8` | 1px rules. |
| `--primary` | `#1e4d3a` | Active nav, links, the main chart stroke. A mark, not the room. |
| `--success` | `#1f7a4a` | Money in, positive difference. |
| `--destructive` | `#9f3a32` | Money out when the sign is the point, and destructive actions. |
| `--warning` | `#8f5a00` | Something to finish or fix that is not a failure, always with a warning icon. 5.78:1 on white, 5.16:1 on the canvas, 5.05:1 on its 10% tint. Added October 7, 2026; see `docs/reviews/2026-10-07-014-plan-recovery.md`. |
| `--transfer` | `#3d5f8a` | Transfers only. |
| `--chart-1` | `#1e4d3a` | Net worth and the primary series. |
| `--chart-2` | `#c4b8a5` | The second series, when a chart compares two things. |
| `--series-1` to `--series-8` | emerald `#00806e`, sapphire `#2b5fb3`, copper `#b8642e`, amethyst `#7b4fb5`, teal `#127c99`, magenta `#a83f74`, amber `#a8740c`, indigo `#4a4fb0` | A chart that compares several things, such as one band per debt on Plan. Each is at least 3:1 on white (lowest: amber, 4.06:1). Emerald and teal moved from the starting `#0f7b5f` and `#1a7f8c` so emerald does not read as `--success`. Added October 7, 2026; see `docs/design/plan-page.md` and `docs/reviews/2026-10-07-014-plan-recovery.md`. |
| `--radius` | `0.5rem` | Cards and fields. From 768px up, a record opens as a right-hand panel, not a bottom sheet. |

Green stays as the accent. It no longer fills the background. Category colors stay the household's colors, and they appear only on that category's bar.

Delete the body wallpaper (the three radial gradients and the 40px ruled lines). Delete the panel rail, the gradient header, the animated underline, the ink stroke under the wordmark, and the `.ledger-amount` highlight. The wordmark is "Tortoise" in Inter at the page-title size, with "Personal ledger" in meta type under it. No script, no SVG flourish.

### Density and surfaces

Page padding is 16px under 768px and 32px from there up. Reading pages (household, categories) can keep a `max-w-6xl` column. Home uses the content width: at `lg` (1024px) it becomes two columns, net worth on the left and this-month on the right, with recent activity full width underneath. Under `lg`, those stack. No horizontal scroller.

Cards are white, 1px border, 8px radius, no shadow, no entrance animation. Headers inside a card are a label and a figure, then a border. Rows are about 48px, name and meta on the left, amount on the right, hover `--muted`.

### Charts

Keep Recharts. Do not add a chart library.

Home's chart is one net-worth area: `--chart-1` stroke, a faint fill, no point markers, horizontal grid lines in `--border` only. The range control stays the existing ranges, drawn as text, with the active range in `--primary`. Assets and liabilities are two figures beside that chart, not extra swipe panels. The accounts page already has the metric switcher. Home should not invent a worse one.

A chart that compares several things, such as Plan's balance bands, uses the series tokens in order. Each thing keeps one color everywhere on that page, and a name in the tooltip or a nearby row, so color is not the only signal. No entrance animation.

This-month keeps the three numbers Tortoise already calculates (income, spending, difference) and the category rows with a 4px bar. Those bars use the category color on a `--muted` track. No donut, no gauge, no third palette.

## 4. Information architecture

Four primary destinations. Plan is fourth. Everything else is a setting.

| Label | Path | Job |
| --- | --- | --- |
| Home | `/` | Where the household stands. Net worth chart, this month, recent activity. |
| Accounts | `/accounts` | Balances, the balance chart, the grouped list. |
| Activity | `/activity` | The ledger. Search, filters, the row, the detail. |
| Plan | `/plan` | When each payoff removes a monthly obligation, and the breathing room that follows. |

The address matches the label.

**Spending targets** (`/targets`) is a setting in the account menu. It is not a primary destination.

**Settings, in the account menu, on both widths:**

| Label | Path | Why it is not primary |
| --- | --- | --- |
| Income | `/income` | Planning inputs, edited now and then. |
| Bills | `/bills` | Planning inputs, edited now and then. |
| Debts | `/debts` | The debt inventory, edited now and then. |
| Plan budget | `/living` | The share of household pay available to Plan and one flexible monthly-spending amount. |
| Savings | `/savings` | Cash to keep, Emergency, and named dated goals. |
| Spending targets | `/targets` | Activity tracking against a monthly intention; it does not add another amount to Plan. |
| Categories | `/categories` | A taxonomy for Activity. People open it to fix a name or a color. |
| Connections | `/connections` | How accounts get linked. |
| Household | `/household` | Planning currency, time zone, contributors. Already the right kind of page. It is just hidden. |

Desktop: these nine under the account email. Phone: the same nine in one menu opened from the account button. There is no hamburger drawer. The phone tab bar is Home, Accounts, Activity, Plan.

Primary pages get a title row and no back button. Back is for a nested flow, not for Home. Page actions (Add on Accounts, Import and Add on Activity) sit in that title row on desktop and in the same row on the phone, wrapping under the title. They stop teleporting into the phone header.

Drill-down stays what the data already supports. A category row on Home can later filter Activity. A chart point does not need a new viewer. The first pass does not have to wire those links. The layout should leave a row that can become one.

## 5. Mobile strategy

**Stay on this Next.js app. Design the desktop layout, then stack it under 768px. Do not start a native app or a PWA.**

The product is already one web app: App Router, Clerk on the server, Plaid Link in the browser, Recharts, and a shell that switches at `md`. The problem is that the switch replaces the navigation model and still leaves phone sheets and a phone carousel in the wide layout. A second client would duplicate that before Plan exists.

A PWA would not fix it. There is no web manifest and no service worker (the only `webmanifest` mention is a Clerk matcher exclusion in `proxy.ts`). Home-screen install, offline, and push are a wrapper around whatever layout we have. Wrap it after the layout is one product.

**Now**

- One component tree. `md` remains the shell breakpoint.
- From 768px up: light sidebar, title row, Home in two columns from 1024px.
- Under 768px: the same primary pages, stacked, with the tab bar and safe-area padding that already exists.
- Same routes, same hooks, same formatters.

**Later**

- After Home, Accounts, and Activity are comfortable at a phone width, a manifest and icons are enough if a home-screen bookmark is wanted.
- Native is a later decision, and only for a capability the browser cannot do well (a receipt camera, a system widget, a lock-screen alert). Not for layout, and not while Plan is still a placeholder.

**Do not build yet**

- React Native, Expo, or a second repo.
- A service worker, an offline ledger, or an install prompt.
- A phone-only information architecture.
- Dark mode. Light replaces the forest theme. Maintaining both now would freeze the split.

## 6. First increment (done)

The first increment was the light shell plus Home: tokens, Inter, no dark class, three primary items, settings in the account menu, a title row on Home, Accounts, and Activity, and Home without the carousel. It was approved on October 5, 2026 in `docs/reviews/2026-10-05-002-light-shell-home.md`. The detail surface and picker popovers followed in `docs/reviews/2026-10-05-003-detail-surface.md` and `docs/reviews/2026-10-05-004-picker-popovers.md`.
