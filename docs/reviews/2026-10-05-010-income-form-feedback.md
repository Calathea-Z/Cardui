# Income form feedback

Date: October 5, 2026

## Increment

A failed save says what you were trying to do. Validation text stays as
written. In development, a server exception is a second line. The income
form leads with the payment, and low, strong, and raises sit in an
optional group.

## Changes

- A 500 uses the sentence the screen already had for that action, such as
  "That income source could not be saved." Validation and any other status
  keep the server text. In development, the exception is a second line.
  Outside development, that exception stays off the screen. The generic
  "An unexpected error occurred." is not repeated as that second line.
- On Income, a missing field, a validation failure, and a 500 are Sonner
  toasts. The validation sentence stays as written. A 500 uses the
  sentence about the action, and in development the exception is the
  toast's second line. Confirming or removing a raise uses a success
  toast. The page no longer places that message under the form.
- Alerts and the page API banner keep a line break, so a 500 shown there
  also puts the exception on the next line in development.
- The form's first group is the payment: name, typical net pay, how often,
  next payment date, contributor, and reliability. Low net pay, strong net
  pay, and expected raises are an optional group under that. A raise cannot
  be lower than the typical net pay.
- Sonner 2.0.8 is the toast package. The toaster uses the app colors and
  sits below the phone header.
- Forms use the app alert for a missing field. The browser tooltip, such
  as "Please fill out this field.", stays off. Income, add account, edit
  account, add transaction, categories, add category, and household all
  use that form.
- Removing an income source and deleting a category ask in an app dialog.
  Cancel leaves the record. The browser confirm box stays off.

## Data changes

None. Saving or removing an income source still changes that household's
income rows. This pass does not add a migration.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~IncomeSourceRulesTests"`:
  11 passed. A raise below the typical net pay is rejected.
- `node ./scripts/run-api-error-tests.mjs` in `frontend`: 5 passed. A 500
  uses the action sentence. In development the exception is the next line.
  Outside development it is omitted. A generic server sentence is not a
  second line. A 400 keeps the validation text. A thrown error with no
  status keeps its message.
- `pnpm exec tsc --noEmit` in `frontend` passed, including the shared form and the confirm dialog.
- eslint on the edited frontend files passed, including the shared form and the confirm dialog.
- Prettier was run on those files.
- Did not click through the app. Did not force a live 500.

## Manual verification

Restart the frontend and the API. The API enforces the raise rule. No
records change unless you save or remove an income source.

1. Open Income.
   Expected: Payment comes first, with name, typical net pay, how often,
   next payment date, contributor, and reliability. Low net pay, strong
   net pay, and expected raises are in a box labeled Optional.
2. Add a source with only the payment filled in.
   Expected: it saves. The optional fields can stay blank.
3. Leave the name blank and save. Then enter a low amount higher than
   typical and save.
   Expected: both messages are a toast at the top of the page. The blank
   name says to enter a name, the typical net pay, how often it is paid,
   the next date, and how reliable it is. The low amount says it cannot be
   higher than typical. Nothing sits under the form. The browser box that
   says "Please fill out this field." does not appear. A raise below the
   typical net pay says a raise cannot be lower than the typical net pay.
4. Add an account and a transaction with the name left blank.
   Expected: each shows the app alert for the missing fields. The same
   browser box stays off.
5. Remove an income source, and delete a category you added for this
   check.
   Expected: a card in the app asks you to confirm, with Cancel and
   Remove or Delete. Cancel leaves the record. The browser box titled
   with the site address stays off.
6. On a raise whose date has arrived, update typical pay or remove the
   raise.
   Expected: a toast at the top states what happened. That sentence is
   not also an alert under the form.
7. If a save fails with a 500, the toast says what you were doing, such
   as "That income source could not be saved." With the frontend in
   development, the exception is the second line of that toast.

## Approval

Zach approved this increment on October 5, 2026.

## Pending decision

Overlapping worker and manual sync remains open. Phase 2 item 2 is the
next income roadmap item: paycheck schedules and gross income, without
turning a biweekly payment into a monthly amount. Which of those to do
next is the decision.
