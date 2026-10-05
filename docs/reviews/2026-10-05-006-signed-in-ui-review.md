# Signed-in UI review

Date: October 5, 2026

## Scope

Zach signed in and requested continuation of the UI review. Inspected Home,
Activity, Accounts, and an existing transaction detail panel in the local app.
Used read-only browser navigation and temporary viewport changes to resolve
the layout questions from report 005. No application records were edited.
Restored the normal browser viewport afterward. No screenshots or personal
financial values were saved into the repository.

## Findings and priorities

1. **Fix Home at 1024px first.** Visually confirmed that the three monthly
   amounts run together and the rightmost amount is clipped. The net-worth
   plot is squeezed into a very narrow strip beside the asset/liability figures.
   Delay the two-card split until there is enough content width, or adapt the
   inner layouts to their card widths. Keep amounts fully readable and move
   asset/liability figures below the chart when space is limited.
2. **Compact mobile Activity filters.** At 390px, the title/actions/search and
   three stacked choice controls take nearly half the viewport before the first
   transaction. Keep search visible and group other filters behind a Filters
   control with an active count. Preserve easy clearing and visible selections.
3. **Improve transaction row context.** Long statement descriptions dominate
   desktop rows and truncate heavily on phones. Show a readable display name
   when available with account/category metadata beneath it; preserve the full
   bank statement in detail. Avoid guessing merchant identities. Repeated
   transfer explanations and badges could be consolidated to reduce row height
   without losing the distinction from spending.
4. **Give Accounts more emphasis on its accounts.** The large chart repeats
   Home's leading picture and pushes much of the account list down. Consider a
   shorter or collapsible chart. Replace the bare plus action with a labeled
   Add account control on desktop for consistency with Activity.
5. **Polish desktop detail dismissal.** The right panel fits the light shell,
   but its back arrow implies navigation. Use a close icon for a top-level
   desktop panel and retain back for nested flows. A lighter backdrop is worth
   evaluating if retaining list context is a priority.
6. **Keep the earlier clarity improvements.** Link Home categories to Activity
   with the matching period, explain Difference as income minus spending, and
   offer manual-account onboarding. The panel focus concern from report 005
   remains source-grounded; this visit did not test keyboard containment.

The light palette, restrained green, simple navigation, aligned amounts, and
consistent surfaces work well together. No new visual direction is needed.

## Verification

- Visually inspected the signed-in desktop screens listed above.
- Visually confirmed mobile filter density at a 390 x 844 viewport.
- Visually confirmed Home clipping and chart compression at 1024 x 768.
- Opened and closed transaction detail without editing any field.
- Did not run automated tests, builds, write flows, or exhaustive browser QA.
- Documentation-only diff checked with `git diff --check`.

## Manual checks after an approved fix

Zach remains the manual QA owner. On Home at 1024px, expect all monthly
amounts to fit and the chart to remain readable. On a phone, expect Activity
to show more transactions before scrolling while still exposing active filters.
Opening and closing detail should retain list position. These checks do not
require changing records. Keyboard focus and save/error flows remain unverified.

## Pending decision and chat handoff

Suggested next increment: fix Home's intermediate-width layout, then review
before starting the other suggestions. No application implementation started.
A new implementation chat is reasonable because the findings are saved: read
`AGENTS.md`, `docs/reviews/README.md`, and this report first. Zach decides
whether to use a new chat and whether to approve the proposed scope.
