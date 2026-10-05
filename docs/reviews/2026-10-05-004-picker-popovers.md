# Picker popovers

Date: October 5, 2026

## Increment

The date calendar and the emoji, color, group, subgroup, and chart-range
pickers open as popovers from 768px up. Under 768px they stay bottom
sheets. Group, subgroup, and chart range use `Select`. The date, emoji,
and color pickers use the same placement as `Select`.

## Changes

- `placeSelectPopover` accepts a minimum width, a maximum height, and a
  fixed width. A fixed width stays at that minimum instead of stretching
  to the trigger. It still shrinks to the viewport.
- `AnchoredPopover` is the shared desktop popover: placement, scroll
  tracking, Escape, and a press outside the popover and the trigger.
  `ChoiceSurface` uses it from 768px up and a bottom sheet under that.
- `DateField` opens the month calendar through `ChoiceSurface`. The
  calendar keeps a fixed width so a wide row does not stretch the grid.
- Add category uses `Select` for group and subgroup. Emoji and color open
  through `ChoiceSurface`, above the add-category panel. Choosing a group
  still clears the subgroup.
- The Categories page form uses those same pickers for Color and Icon.
  Those fields were plain text, so the page had no visible way to choose
  either one.
- Custom color uses `react-colorful` inside the popover. The twelve
  swatches still close the picker. Dragging the saturation square or
  editing the hex field updates the color and leaves the picker open.
  The icon list stays the 37 category glyphs.
- Merchant history replaces the chart-settings sheet with a Chart range
  row. That row is `Select`. Escape closes the list and leaves the
  history open.

## Data changes

None.

## Agent verification

- `pnpm exec tsc --noEmit` and `pnpm lint` in `frontend/` passed.
- `pnpm test` in `frontend/` passed, including the new popover size cases.
- Prettier was run on the files this increment changed.
- Did not click through the app. Signed-in routes need Clerk.

## Manual verification

Use the household you already have. No database changes.

1. On a laptop, open Add on Activity and open the date field.
   Expected: the month calendar sits next to the field. It does not
   slide up from the bottom. Choosing a day, Clear, or Today closes it.
2. Narrow the window below 768px and open that date field.
   Expected: the calendar is a bottom sheet.
3. On a laptop, open Add category and use Emoji, Color, Group, and
   Sub-group.
   Expected: each opens next to its row. Choosing a value closes it.
   Choosing a group clears the sub-group. The form stays open.
4. On a laptop, open Categories. Use Color and Icon on New category.
   Expected: Color opens swatches, a saturation square, and a hex field
   next to the control. A swatch fills the field and closes the popover.
   Dragging the square updates the color and leaves the popover open.
   Icon opens the emoji grid. Neither field is a plain text box.
5. Narrow the window and open those same four controls.
   Expected: each is a bottom sheet.
6. On a laptop, open a merchant's history and use Chart range.
   Expected: Monthly, Quarterly, and Yearly open next to the row. There
   is no settings gear. Choosing a range updates the chart and leaves
   the history open.
7. On a laptop, open a date or a choice list and press Escape.
   Expected: the popover closes. The panel behind it stays open.

## Approval

Zach approved this increment on October 4, 2026.

## Pending decision

These were the remaining desktop bottom sheets for choices. Plaid sync
reconciliation tests stay paused while the UI plan is the active local
work. Whether that engineering increment resumes is the next decision.
