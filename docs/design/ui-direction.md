# UI direction

Status: Current. The light shell is implemented and approved; the October 8
governance clarification is awaiting review in
[`2026-10-08-009-responsive-ui-governance.md`](../reviews/2026-10-08-009-responsive-ui-governance.md).
Updated: 2026-10-08

The enforceable conventions for new screens and substantial UI changes are in
[`.cursor/rules/ui-governance.mdc`](../../.cursor/rules/ui-governance.mdc).
Where this rationale differs from that rule, the rule wins. Page-specific
choices belong in page-specific design documents.

The signed-in app is branded **Tortoise** in `frontend/app/layout.tsx`. The
repository and roadmap call the product Cardui. This document explains the
shared visual and interaction direction for the Next.js frontend.

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

### Width, density, and surfaces

Content width follows the task:

- prose and forms stay comfortably bounded;
- lists use enough width for labels, dates, status, and values to remain
  readable; and
- analytical layouts may use a wider bounded container when comparison is
  easier with related information together.

The standard page padding is 16px below `md` and 32px from `md` up. White
cards have a 1px border, 8px radius, and no shadow or entrance animation.
Those cards represent meaningful groups. They are not wrappers required
around every metric, message, or control.

Home's current two-column arrangement at sufficiently wide sizes and Plan's
current wider analysis container are page-specific examples, not universal
width or card-count requirements.

## 3. Intentional responsive design

Desktop and mobile are two arrangements of the same product, not a desktop
page followed by an automatic stack. Before implementation, decide how each
arrangement presents the answer, actions, controls, and supporting detail.

They share routes, data, behavior, state ownership, formatters, and reusable
components. Grouping can change, controls can wrap or stack, and secondary
detail can move behind disclosure when space is limited. Relevant input and
selection state should survive resizing and local view changes.

On a phone:

- the answer and relevant action remain ahead of supporting analysis;
- controls and labels fit without horizontal page scrolling;
- long amounts, dates, warnings, and text remain readable with text zoom;
- chart values have touch and keyboard access or a readable data alternative;
- sticky headers, tabs, and bottom navigation do not cover content or focus;
  and
- the page avoids nested scrolling and duplicate phone-only implementations.

The shell still changes at `md`: a sidebar from 768px up and the four-item tab
bar below it. Record forms still become full-screen sheets below `md`, and
choice popovers become bottom sheets. Those are shared responsive patterns,
not a separate mobile information architecture.

A native client or PWA is not a layout remedy. Neither is part of the current
direction. A later product need can propose one separately.

## 4. Navigation and progressive disclosure

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

The rule against a second navigation prevents a competing app-level map. It
does not ban controls that clarify one destination:

- **Local tabs** separate distinct tasks, such as Overview and a focused
  analysis.
- **Filters** change the same dataset or view.
- **Disclosures** reveal supporting explanation or optional analysis.
- **Panels** keep inspection or editing focused without losing page context.

Critical errors, required actions, and missing inputs that invalidate a result
cannot disappear inside collapsed detail. Local view changes should preserve
the state that remains relevant to the user's task.

## 5. Warning hierarchy

Warnings are part of the result hierarchy, not a second dashboard.

The issue that most affects the current task or result reliability gets the
primary presentation. Additional issues can be summarized by count with
access to details. Healthy checks stay quiet. Repetition is useful only when
the nearby result or action would otherwise be misunderstood.

This reduction in visual noise does not justify hiding a material financial
qualification. A cash shortfall, stale balance, missing term, excluded
currency, or temporary scenario remains visible wherever it changes the
meaning of the answer.

Warnings use the warning color and icon convention. Failure and destructive
actions use destructive treatment. Color is always paired with text or an
icon.

## 6. Charts and metrics

A visualization earns its space by answering a question that a short summary
cannot answer as clearly. A prominent metric follows the same test. Units,
timeframe, and material qualifications sit with the result rather than in a
distant explanation.

Actual history, a forecast, and an unsaved scenario are different kinds of
evidence and should be named that way. The interface does not fabricate
intermediate points, false decimal precision, or certainty the underlying
contract does not provide.

Related charts and lists keep the same identity for each series. Color is not
the only identifier. Essential values remain available without hover, and
touch and keyboard users can inspect them or use an equivalent readable
view.

Recharts is already in the application. A new chart dependency needs the
normal dependency decision described in
[`.cursor/rules/ui-primitives.mdc`](../../.cursor/rules/ui-primitives.mdc).

## 7. Actions and states

An actionable view has one clearly dominant action. Other actions are quieter.
A read-only view does not need a manufactured button merely to satisfy a
layout pattern.

Screen-level actions stay associated with the title or primary result.
Contextual actions can sit beside the result they resolve. On a narrow screen,
that relationship matters more than keeping every action on the same physical
line: controls can wrap or stack without moving into a different navigation
bar.

Loading, empty, error, and stale states preserve the same hierarchy. An empty
state explains what is missing and offers the action that fills it. A stale or
partially failed view keeps the last useful data visible and clearly explains
its reliability.

## 8. Current implementation and historical notes

The light shell, Home direction, right-hand detail surface, choice popovers,
Accounts chart header, and signed-in responsive layout were implemented and
approved on October 5, 2026:

- [`2026-10-05-002-light-shell-home.md`](../reviews/2026-10-05-002-light-shell-home.md)
- [`2026-10-05-003-detail-surface.md`](../reviews/2026-10-05-003-detail-surface.md)
- [`2026-10-05-004-picker-popovers.md`](../reviews/2026-10-05-004-picker-popovers.md)
- [`2026-10-05-005-accounts-chart-header.md`](../reviews/2026-10-05-005-accounts-chart-header.md)
- [`2026-10-05-007-signed-in-layout.md`](../reviews/2026-10-05-007-signed-in-layout.md)

Before that work, the app used a dark journal theme, multiple typefaces, a
phone carousel, duplicated phone navigation, decorative wallpaper, and bottom
sheets at laptop widths. That description is historical, not a source for new
requirements.

Several statements in the original October 5 brief also described that moment
only: Tortoise now has budget data, Plan is a real destination rather than a
placeholder, and the product map is not limited to "three daily jobs." The
old "design desktop, then stack it" instruction is retired in favor of the
intentional responsive approach above.

The current Plan page is documented separately in
[`plan-page.md`](plan-page.md). Its implemented three-view refinement is still
awaiting review, and the broader Planning UX assessment remains a proposal.
Neither this shared direction nor the governance rule approves a further Plan
redesign.
