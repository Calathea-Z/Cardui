# UI review notes

Date: October 5, 2026

## Scope and evidence

Reviewed the latest UI reports, UI direction, and current Home, Activity,
sidebar, and detail-surface components. The local application redirected the
in-app browser to Clerk sign-in. These are source-grounded UX recommendations,
not a completed visual review of authenticated screens.

No application code or records changed. No tests ran; this is documentation only.

## Recommended improvements

1. **Contain and restore keyboard focus in detail panels.** `BottomSheet`
   focuses its dialog on opening but has no focus trap, background inertness,
   or return-to-trigger handling. Make keyboard navigation stay in the active
   surface and return to its opener on close, including nested pickers.
2. **Compact Activity filters on phones.** `TransactionsFilters` stacks search,
   account, category, and status below the title actions until the large
   breakpoint. Keep search visible and consider a Filters button with an active
   count and removable selections, giving transaction rows more initial space.
   Give search an explicit accessible name beyond its placeholder.
3. **Let Home explain its category totals.** Spending category rows are static.
   Link each to Activity with that category and the same reporting period.
   Activity currently has no date filter, so preserving the period requires
   deliberate implementation; a category-only link would show different totals.
4. **Offer manual onboarding on Home.** The net-worth empty state only offers
   bank linking despite manual accounts being supported. Offer Add account and
   Link bank choices with a short explanation of each.
5. **Clarify Difference.** Show a short explanation such as income minus spending
   beneath this monthly metric. Avoid wording that implies the amount is safe
   to spend or available cash, since that is not what this calculation measures.
6. **Check intermediate-width density before changing spacing.** The fixed
   256px sidebar and Home's two-column layout from 1024px leave less room for
   the chart, asset/liability figures, and three monthly metrics. Visually inspect
   around 1024px with long amounts; consider delaying the split or stacking the
   asset/liability figures if it feels cramped. This is an unverified layout risk.

Keep the approved three primary destinations, light palette, and desktop
panels/popovers. Prioritize interaction and information clarity over another
theme change.

## Manual checks awaiting Zach

- At phone width, check how much Activity is visible before scrolling past filters.
- Around 1024px, check that Home figures fit and the chart remains readable.
- Open a detail panel and a nested picker, navigate with Tab/Shift+Tab, then close.
  Desired result: focus stays in the active surface and returns to its opener.
  Current implementation does not supply all of this behavior.
- Review an empty household only if one is already available; verify that a
  manual-account path would be discoverable. Do not clear existing data.

These inspection steps require no application record changes.

## Pending decision

Zach to choose and approve the next implementation increment. Suggested first
increment: detail-panel keyboard focus behavior. No implementation started.
Continue this chat for clarification; a new implementation chat can read
`AGENTS.md`, `docs/reviews/README.md`, and this report.
