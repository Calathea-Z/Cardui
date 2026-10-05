# UI direction

Approved October 5, 2026. This is the local UI plan. The first increment is the light shell plus Home.

Section 1 describes the shell before the Income page. `/income` is now a real route, in the sidebar and the phone tab bar. See `docs/reviews/2026-10-04-016-income-sources.md`. The approved primary navigation is still Home, Accounts, and Activity. Keep the Income route. Do not redesign that page in the first increment, and do not treat it as a fourth primary item.

Tortoise should look like one light finance product. The phone layout is the same screens, stacked. The current UI is a dark journal theme wearing a phone shell on a laptop.

The signed-in app is branded **Tortoise** (`frontend/app/layout.tsx`). The repo and roadmap call it Cardui. This note is about the Next.js app in `frontend/` (Next.js 16, React 19, Tailwind 4). Zach approved this direction on October 5, 2026. It is the local UI plan, not a design-system spec and not an implementation.

I did not run Tortoise. The workspace has no env files, and every signed-in route calls Clerk `auth.protect()`. Starting it would need secrets. The layout notes below are from the components.

## 1. Where the UI is split

The break is the `md` breakpoint (768px). Below it, the app is a phone. At it and above, a sidebar appears, and the page underneath is still the phone column.

**Two navigations at once.** `AppShell` always mounts `DesktopSidebar` and `MobileShell`. The sidebar is `hidden ... md:flex` (`desktop-sidebar.tsx`). The phone header, drawer, and tab bar are `md:hidden` (`mobile-shell.tsx`). On a phone, primary destinations exist twice: `MobileBottomNav` shows Dashboard, Transactions, Accounts, and Budgets (`bottomNavItems` in `nav-items.ts`), and `MobileDrawer` lists all six items, including Institutions and Categories. Household is in neither. It sits only in `AccountMenu`, at the bottom of the sidebar and the drawer.

**Phone pages have a title. The main desktop pages do not.** The phone header centers the active nav label. `PageHeader` (handwritten title, back button) is `hidden md:inline-flex` for the back control, and the money pages do not use it. Dashboard, accounts, and transactions render a `max-w-6xl` stack with no `h1`. Categories, institutions, household, and the budgets placeholder draw their own titles. Desktop Home is a column of cards under a sidebar, with no page name.

**The dashboard is a swipe carousel at every width.** `DashboardAccountsSlider` is three snap panels (Net Worth, Assets, Liabilities) with `snap-x` and `touch-pan-x`. A laptop user swipes or hits the text tabs to see assets. That is a phone pattern in the widest content column.

**Detail UI is a phone sheet on the desktop too.** `fullScreenSheet.ts` says the sheet "fills the phone and becomes a bottom sheet on desktop." At `md` it is still anchored to the bottom, `max-h-[85vh]`, `rounded-t-2xl`. `Select` always opens a `BottomSheet`, including account and category filters. Transaction detail, add, import, and account detail all use that sheet.

**Actions move, the page does not.** Accounts puts Add / refresh into the phone header via `useSetMobileHeaderActions`, and repeats them in a row that is `hidden ... md:flex` (`AccountsView`). Transactions always shows Import and Add at the top of the column, under the phone header when the window is narrow.

**The theme is one dark craft treatment, on both sizes.** `layout.tsx` puts `class="dark"` on `<html>` and loads three faces: Shantell Sans (`font-brand`, the wordmark and page titles), Sora (UI), JetBrains Mono (money). `:root` and `.dark` in `globals.css` are the same forest green. The body paints radial glows and ruled lines. `.app-panel` adds a green left rail and an entrance animation. `.app-panel-header` adds a gradient and a growing rule. `.ledger-amount` paints a highlighter behind figures. None of that changes at `md`.

A few inner layouts do use width (`sm` for the income/spending/difference row, `lg` for category editor, institution cards, and transaction filters). The shell does not. A 1200px window is a 256px dark sidebar plus a centered 72rem phone stack.

### What is real

| Route | Status |
| --- | --- |
| `/` | Real. Net worth carousel, monthly income/spending/category bars, recent transactions. |
| `/transactions` | Real. Search, filters, date groups, detail sheet, add, CSV import. |
| `/accounts` | Real. Balance chart, metric and range controls, grouped list, manual add, Plaid, detail sheet. |
| `/institutions` | Real. Linked institutions. |
| `/categories` | Real. List and editor. |
| `/household` | Real, but only from the account block. Planning currency, time zone, contributors. |
| `/budgets` | Placeholder. "Budget tracking is coming soon." |
| `/sign-in`, `/sign-up` | Clerk. |

The October 2 roadmap points the product at debt recovery. The screens above are still a spending and net-worth ledger. This direction does not design the recovery flows.

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

Home, Accounts, and Activity are the objects. Categories, connections, and household are settings. A chart on Home answers one question, and the rows under it are the drill-down. Filters change that view. They do not open a second app.

The dark canvas goes away. Tortoise is dark today, and that theme is part of what is being retired. One light theme. No second theme in the same pass.

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
| Meta, tab label | 0.75rem | 500 | Phone tab labels are 10px today. 12px is the floor. |

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
| `--transfer` | `#3d5f8a` | Transfers only. |
| `--chart-1` | `#1e4d3a` | Net worth and the primary series. |
| `--chart-2` | `#c4b8a5` | The second series, when a chart compares two things. |
| `--radius` | `0.5rem` | Cards and fields. Sheets stop at `rounded-t-2xl` on desktop later. |

Green stays as the accent. It no longer fills the background. Category colors stay the household's colors, and they appear only on that category's bar.

Delete the body wallpaper (the three radial gradients and the 40px ruled lines). Delete the panel rail, the gradient header, the animated underline, the ink stroke under the wordmark, and the `.ledger-amount` highlight. The wordmark is "Tortoise" in Inter at the page-title size, with "Personal ledger" in meta type under it. No script, no SVG flourish.

### Density and surfaces

Page padding is 16px under 768px and 32px from there up. Reading pages (household, categories) can keep a `max-w-6xl` column. Home uses the content width: at `lg` (1024px) it becomes two columns, net worth on the left and this-month on the right, with recent activity full width underneath. Under `lg`, those stack. No horizontal scroller.

Cards are white, 1px border, 8px radius, no shadow, no entrance animation. Headers inside a card are a label and a figure, then a border. Rows are about 48px, name and meta on the left, amount on the right, hover `--muted`.

### Charts

Keep Recharts. Do not add a chart library.

Home's chart is one net-worth area: `--chart-1` stroke, a faint fill, no point markers, horizontal grid lines in `--border` only. The range control stays the existing ranges, drawn as text, with the active range in `--primary`. Assets and liabilities are two figures beside that chart, not extra swipe panels. The accounts page already has the metric switcher. Home should not invent a worse one.

This-month keeps the three numbers Tortoise already calculates (income, spending, difference) and the category rows with a 4px bar. Those bars use the category color on a `--muted` track. No donut, no gauge, no third palette.

## 4. Information architecture

Three primary destinations. Everything else is a setting.

| Label | Path | Job |
| --- | --- | --- |
| Home | `/` | Where the household stands. Net worth chart, this month, recent activity. |
| Accounts | `/accounts` | Balances, the balance chart, the grouped list. |
| Activity | `/transactions` | The ledger. Search, filters, the row, the detail. |

The path stays `/transactions` in the first pass. Only the label changes, to Activity. Renaming the route can wait.

**Plan** (`/budgets`) comes off the primary nav until the screen is real. A "coming soon" row in the sidebar, and a tab on the phone, advertise an empty room. Leave the route in place so the URL does not 404. When budgets, and later the recovery questions from the October 2 roadmap, have a real screen, Plan comes back as the fourth primary item. Do not design that screen now.

**Settings, in the account block, on both widths:**

| Label | Path | Why it is not primary |
| --- | --- | --- |
| Categories | `/categories` | A taxonomy for Activity. People open it to fix a name or a color. |
| Connections | `/institutions` | How accounts get linked. The path stays `/institutions`. |
| Household | `/household` | Planning currency, time zone, contributors. Already the right kind of page. It is just hidden. |

Desktop: those three under the account email, where Household already is. Phone: the same three in one menu opened from the account button. The hamburger drawer that repeats Home, Accounts, and Activity goes away. The phone tab bar is Home, Accounts, Activity. Three tabs, not four, until Plan exists.

Primary pages get a title row and no back button. Back is for a nested flow, not for Home. Page actions (Add on Accounts, Import and Add on Activity) sit in that title row on desktop and in the same row on the phone, wrapping under the title. They stop teleporting into the phone header.

Drill-down stays what the data already supports. A category row on Home can later filter Activity. A chart point does not need a new viewer. The first pass does not have to wire those links. The layout should leave a row that can become one.

## 5. Mobile strategy

**Stay on this Next.js app. Design the desktop layout, then stack it under 768px. Do not start a native app or a PWA.**

The product is already one web app: App Router, Clerk on the server, Plaid Link in the browser, Recharts, and a shell that switches at `md`. The problem is that the switch replaces the navigation model and still leaves phone sheets and a phone carousel in the wide layout. A second client would duplicate that before Plan exists.

A PWA would not fix it. There is no web manifest and no service worker (the only `webmanifest` mention is a Clerk matcher exclusion in `proxy.ts`). Home-screen install, offline, and push are a wrapper around whatever layout we have. Wrap it after the layout is one product.

**Now**

- One component tree. `md` remains the shell breakpoint.
- From 768px up: light sidebar, title row, Home in two columns from 1024px.
- Under 768px: the same three pages, stacked, with a three-item tab bar and safe-area padding that already exists.
- Same routes, same hooks, same formatters.

**Later**

- After Home, Accounts, and Activity are comfortable at a phone width, a manifest and icons are enough if a home-screen bookmark is wanted.
- Native is a later decision, and only for a capability the browser cannot do well (a receipt camera, a system widget, a lock-screen alert). Not for layout, and not while Plan is still a placeholder.

**Do not build yet**

- React Native, Expo, or a second repo.
- A service worker, an offline ledger, or an install prompt.
- A phone-only information architecture.
- Desktop bottom sheets as a permanent pattern. Fixing `Select` and the detail sheets is the UI pass after this one.
- Dark mode. Light replaces the forest theme. Maintaining both now would freeze the split.

## 6. First increment

**The light shell, plus Home.** One pass. Zach can look at Home, Accounts, and Activity and see the same chrome. Only Home's layout changes inside the page.

In scope:

- `frontend/app/globals.css` and `frontend/app/layout.tsx`: the tokens, Inter, no `dark` class, no wallpaper, no ink utilities on the surfaces this pass touches.
- `frontend/components/navigation/`: three primary items, settings in the account block, phone tabs reduced to those three, drawer no longer a second primary nav, a title row on Home, Accounts, and Activity.
- `frontend/features/dashboard/`: remove the snap carousel. Net worth chart with asset and liability figures, this-month beside it from `lg` up, recent activity underneath. Keep `dashboardMonthlyActivity.ts`, `dashboardAccountGroups.ts`, and the existing summary data. This is a layout change.

Leave alone:

- API, worker, and database.
- CSV import, transaction detail fields, filters, and grouping.
- Account chart math, metric selector, and the accounts list.
- Category editor, institutions, and household forms.
- `BottomSheet` and `Select`. They will look lighter because the tokens change, and they will still slide up from the bottom. That is the next UI pass.
- The budgets route body. Remove it from the nav. Do not design Plan.
- New fonts beyond Inter, and any new chart or component library.

After this increment, the next UI decision is the detail surface: a right-hand panel from 768px up, a full-screen sheet under it, and `Select` as a popover on desktop. Not before Home looks like one product.
