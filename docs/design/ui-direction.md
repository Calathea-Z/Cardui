# UI direction

Status: Current. The light shell is approved. The October 8 governance
clarification is awaiting review in
[`2026-10-08-009-responsive-ui-governance.md`](../reviews/2026-10-08-009-responsive-ui-governance.md).
Updated: 2026-10-08

The enforceable conventions for new screens and substantial UI changes are in
[`.cursor/rules/ui-governance.mdc`](../../.cursor/rules/ui-governance.mdc).
Where this note differs from that rule, the rule wins. Page-specific choices
belong in page-specific design documents.

The signed-in app is branded **Tortoise** in `frontend/app/layout.tsx`. The
repository and roadmap call the product Cardui.

## 1. Product direction

Tortoise should make a financial answer and the next useful decision easier to
find than the analysis behind them. A screen begins with one question:

- What does the user need to know here?
- What can they do next, if the view is actionable?
- Which details help them trust, inspect, or change that answer?

That order is the intended hierarchy. It is not a fixed page template. A
read-only history view may have no action. An editor may lead with the form.
An analytical view may need related results side by side. The shared rule is
to organize around the task instead of mirroring an API response.

Adding a useful fact should not automatically add another equal-weight card.
The designer should reconsider the whole page: the fact may belong in the
main answer, an existing group, a local task tab, a disclosure, or a focused
detail surface. Useful information remains reachable, but healthy checks and
long explanations do not compete with the decision.

## 2. Visual foundation

The product is one light interface. Inter, restrained color, flat surfaces,
and strong spacing should make financial values readable without making every
value a headline.

### Type

Inter is the product face for titles, navigation, body copy, and money. Money
uses `tabular-nums`; it does not use a terminal-style face.

| Role                   | Typical treatment            | Rationale                                       |
| ---------------------- | ---------------------------- | ----------------------------------------------- |
| Page title             | 1.75rem, 600, tight tracking | One clear destination heading                   |
| Prominent result       | 2rem, 600, tabular numbers   | A result, not decorative display type           |
| Section title          | 0.875rem, 600, sentence case | Clear hierarchy without competing with the page |
| Body, navigation, rows | 0.875rem, 400/500            | Comfortable scanning                            |
| Meta and tab labels    | 0.75rem, 500                 | Secondary, but still readable                   |

These sizes describe the implemented system, not a reason to force every
screen into the same density. Reduce repeated and secondary content before
making text or spacing smaller.

### Color

| Token                            | Value                                                             | Use                                                         |
| -------------------------------- | ----------------------------------------------------------------- | ----------------------------------------------------------- |
| `--background`, `--sidebar`      | `#f3f2ee`                                                         | Page and sidebar                                            |
| `--card`, `--popover`, `--input` | `#ffffff`                                                         | Cards, sheets, fields                                       |
| `--foreground`                   | `#1c1b19`                                                         | Text and figures                                            |
| `--muted`                        | `#eeede8`                                                         | Bar tracks and hover                                        |
| `--muted-foreground`             | `#6d6a64`                                                         | Secondary information                                       |
| `--border`, `--sidebar-border`   | `#e3e0d8`                                                         | Rules and group boundaries                                  |
| `--primary`                      | `#1e4d3a`                                                         | Active navigation, links, main chart stroke                 |
| `--success`                      | `#1f7a4a`                                                         | Positive result or money in                                 |
| `--destructive`                  | `#9f3a32`                                                         | Failure, destructive action, or negative sign when material |
| `--warning`                      | `#8f5a00`                                                         | Something to finish or fix that is not a failure            |
| `--transfer`                     | `#3d5f8a`                                                         | Transfers                                                   |
| `--chart-1`                      | `#1e4d3a`                                                         | Primary series                                              |
| `--chart-2`                      | `#c4b8a5`                                                         | A comparison series                                         |
| `--series-1` to `--series-8`     | Emerald, sapphire, copper, amethyst, teal, magenta, amber, indigo | Several named items in one analysis                         |

The warning token has at least 5:1 contrast on its current white, canvas, and
10% tint backgrounds. The eight jewel series are each at least 3:1 on white;
their exact values and contrast check are recorded in
[`2026-10-07-014-plan-recovery.md`](../reviews/2026-10-07-014-plan-recovery.md).
Green remains an accent, not a page fill. Household category colors appear
only with their categories.

### Width and surfaces

Content width follows the task. Prose and forms stay comfortably bounded.
Lists stay wide enough for labels, dates, status, and values. An analytical
layout may use a wider bounded container when comparison is easier with
related information together.

The standard page padding is 16px below `md` and 32px from `md` up. White
cards have a 1px border, 8px radius, and no shadow or entrance animation.
Those cards represent meaningful groups.

Home's current two-column arrangement at sufficiently wide sizes and Plan's
current wider analysis container are page-specific examples, not universal
width or card-count requirements.

## 3. Screen map

Four primary destinations remain in this order:

| Label    | Path        | Current job                                                    |
| -------- | ----------- | -------------------------------------------------------------- |
| Home     | `/`         | Historical orientation: net worth, this month, recent activity |
| Accounts | `/accounts` | Balances, history, and the account inventory                   |
| Activity | `/activity` | Search, filter, inspect, and maintain transactions             |
| Plan     | `/plan`     | Forward-looking affordability and debt-recovery analysis       |

The account menu remains the secondary map on desktop and phone:

| Label            | Path           | Current job                                    |
| ---------------- | -------------- | ---------------------------------------------- |
| Income           | `/income`      | Planning income inputs                         |
| Bills            | `/bills`       | Scheduled non-debt obligations                 |
| Debts            | `/debts`       | Debt inventory and maintained terms            |
| Plan budget      | `/living`      | Household contribution and flexible spending   |
| Savings          | `/savings`     | Cash to keep, emergency, and named dated goals |
| Spending targets | `/targets`     | Activity tracking against monthly intentions   |
| Categories       | `/categories`  | Activity taxonomy                              |
| Connections      | `/connections` | Linked-account maintenance                     |
| Household        | `/household`   | Currency, time zone, and contributors          |

How local tabs, filters, disclosures, and panels differ is in the UI
governance rule.

## 4. Shared behavior

Responsive layout, warnings, charts, actions, states, and accessibility are
in [`.cursor/rules/ui-governance.mdc`](../../.cursor/rules/ui-governance.mdc).
This note does not restate them.

Recharts is already in the application. A new chart dependency needs the
decision in
[`.cursor/rules/ui-primitives.mdc`](../../.cursor/rules/ui-primitives.mdc).

## 5. Earlier shell

Before October 5, 2026, the app used a dark journal theme, more than one
typeface, a phone carousel, and bottom sheets at laptop widths. That shell
is not a source for new screens. The light shell was approved on October 5,
2026:

- [`2026-10-05-002-light-shell-home.md`](../reviews/2026-10-05-002-light-shell-home.md)
- [`2026-10-05-003-detail-surface.md`](../reviews/2026-10-05-003-detail-surface.md)
- [`2026-10-05-004-picker-popovers.md`](../reviews/2026-10-05-004-picker-popovers.md)
- [`2026-10-05-005-accounts-chart-header.md`](../reviews/2026-10-05-005-accounts-chart-header.md)
- [`2026-10-05-007-signed-in-layout.md`](../reviews/2026-10-05-007-signed-in-layout.md)

The current Plan page is in [`plan-page.md`](plan-page.md). Its three-view
refinement is awaiting review. The Planning UX assessment is a proposal.
This direction does not approve a further Plan redesign.
