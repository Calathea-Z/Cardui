# Linked manual debts

Status: proposed, for Zach's review. Not approved. Nothing here is built.

Date: October 6, 2026

## Problem

A person records a debt by hand. When they also connect that card or loan,
they still have to retype the balance after every payment. The debt and the
connected account stay separate on purpose, so a sync can never overwrite
something the person typed.

This note proposes an optional way for a debt to follow a connected
account's balance. A sync still never overwrites what the person typed.

## What exists today

All of this is on `main` as of October 6, 2026.

### Debt record

- `api/Models/Debt.cs` holds the debt: name, `Kind` (`DebtKind`: revolving
  or installment), an optional `AccountId`, `Balance` with `BalanceAsOf`,
  `Currency`, `Apr`, `MinimumPayment`, `NextDueDate`, `CreditLimit`,
  `RemainingTermMonths`, `PromotionalApr`, and `PromotionalEndsOn`. A null
  term is unknown. Zero is a known zero.
- `api/Data/Configurations/DebtConfiguration.cs` maps `AccountId` with
  `OnDelete(DeleteBehavior.SetNull)` and indexes it. The debt is deleted
  with its household.
- `api/Domain/DebtRules.cs` normalizes a request into `DebtDraft`. The API
  shapes are `api/Dtos/Debts/DebtDto.cs` and `UpsertDebtDto.cs`.
- There is no payoff priority, notes, or target payment field yet. The
  promotional rate and end date exist.

### The account link today

- `DebtsService.RequireAccountAsync` accepts any account in the household,
  including a manual or cash account. An archived account can stay linked.
- The form says the link is optional and that "the account balance is not
  copied or changed" (`frontend/features/debts/DebtForm.tsx`).
- The debt summary (`docs/reviews/2026-10-05-016-debt-summary.md`) loads the
  linked account's latest dated balance in
  `DebtsService.LoadLinkedBalancesAsync`. The latest
  `AccountBalanceSnapshot` is the dated figure. Without one, the current
  balance is shown with an unknown date.
- When that balance differs, `api/Domain/DebtSummary.cs` builds a
  `DebtBalanceComparison`. The card says "Two balances". The recorded
  balance stays in use. `api/Domain/DebtAccountBalanceBlock.cs` explains why
  an account balance cannot be copied: `DateUnknown`, `NegativeBalance`,
  `CurrencyDiffers`, or `AmountTooLarge`. A cash account is not offered.
- `POST /api/debts/{id}/use-account-balance` copies that one snapshot onto
  the debt after confirmation. It does not change the account, APR,
  minimum, or due date. It does not repeat after the next sync.

### Connected accounts and sync

- `api/Models/Account.cs` carries `PlaidItemId`, `PlaidAccountId`,
  `Source`, `Provenance`, `Type`, `Subtype`, `Mask`, `CurrentBalance`,
  `AvailableBalance`, `IsoCurrencyCode`, `IsActive`, and `ArchivedAt`.
- `api/Services/Plaid/PlaidAccountSyncService.cs` calls `/accounts/get`,
  copies the name, type, mask, current and available balance, and currency,
  replaces today's `AccountBalanceSnapshot` in the household time zone, and
  marks an account inactive when the bank stops returning it. It does not
  store the credit limit that `/accounts/get` returns.
- `PlaidService.CreateLinkTokenAsync` requests only `Products.Transactions`.
  There is no update-mode link token, so there is no reconnect flow today.
- `api/Models/PlaidItem.cs` records `LastSyncStartedAt`,
  `LastSyncCompletedAt`, `LastSyncFailedAt`, and `LastSyncError`. The
  Connections page (`frontend/features/institutions/InstitutionCard.tsx`)
  shows the last success or the failure text.
- `api/Services/Plaid/PlaidItemRemoval.cs` removes a bank link and detaches
  its accounts (`PlaidItemId = null`). Accounts, transactions, and snapshots
  stay. Accounts are archived, not deleted, through the API.
- `worker/Program.cs` runs one daily sync per item. A manual sync can run
  through `POST /api/plaid/{id}/sync`. Overlapping worker and manual sync is
  still open in `docs/Recovery-Application-Action-Plan.md` and in every
  review since `docs/reviews/2026-10-05-008-plaid-sync-reconciliation-tests.md`.
- Tests cover `PlaidTransactionReconciler`, `PlaidTransactionSyncService`,
  and `PlaidItemRemoval` (`tests/Cardui.Tests/Services`). There is no test
  file for `PlaidAccountSyncService`, which is the code that would feed a
  followed balance.

## Approved direction

These points were approved before this note. The note designs them.

- Linking is optional. Cardui suggests likely matches. The person confirms.
- Ownership is per field. Synced fields come from the connection. Other
  fields always belong to the person.
- Editing a synced field creates a labeled override. A sync never silently
  replaces it. "Use synced value" reverts it.
- Each followed debt shows how fresh its numbers are. A broken connection
  keeps the last balance, marks it stale, and offers reconnect or a manual
  update.
- Stopping the follow keeps the last values and returns the debt to manual.
  Nothing is lost.
- APR, minimum payment, and due date need the Plaid Liabilities product.
  That is an open decision.
- Sync correctness comes first.

## Proposed design

### Two kinds of link

Keep today's link and add a second mode. Do not change what an existing
link does.

| Mode | Meaning | Who sets the balance |
| --- | --- | --- |
| Reference (today) | The debt names an account. Nothing is copied. "Two balances" and "Use this balance" work as they do now. | The person |
| Follow (new) | The debt uses the connected account's synced fields, except where the person set an override. | The connection, field by field |

- Every existing debt with an `AccountId` stays a reference link. The
  migration must not turn any existing link into a follow.
- Follow is allowed only for a connected credit card or loan: `Source` is
  `Plaid`, `PlaidItemId` is set, the account is active and not archived,
  `AccountLedger.IsLiability` is true, and the currency matches the debt.
  A manual account already holds a balance the person types, so following
  it adds nothing.
- One account can be followed by one debt at most. Two debts following one
  card would count that balance twice in the summary.

### Sync never writes a debt

The sync keeps writing only account-side rows: `Account`,
`AccountBalanceSnapshot`, and, if Liabilities is approved, one new
account-side terms row. It does not load or update `Debts`.

A followed debt reads its synced values when it is shown or planned, the
same way `LoadLinkedBalancesAsync` reads the latest snapshot today. That
keeps the rule from the debts review true by construction: a sync cannot
overwrite a debt's own columns, because it never touches them.

The debt's own columns keep holding the person's values. When the person
stops following, the last synced values are copied into those columns once,
by the person's action, not by sync.

### Field ownership

| Field | Owner when following | Source |
| --- | --- | --- |
| Current balance and its date | Synced | Latest `AccountBalanceSnapshot` (`/accounts/get`) |
| Credit limit (revolving) | Synced | `balances.limit` from `/accounts/get`. Not stored today. |
| Statement balance and date | Synced, shown only | Liabilities. No debt column exists. |
| Minimum payment | Synced | Liabilities |
| Next due date | Synced | Liabilities |
| APR | Synced | Liabilities. Purchase APR only (see questions). |
| Name, type | Person | Debt |
| Promotional APR and end date | Person | Debt |
| Months left (installment) | Person | Debt |
| Payoff priority, notes, target payment | Person | Not built yet. Owned by the person when added. |

A synced field resolves to one of four states. A pure rule in `Domain`
decides it from values, with no database, clock, or HTTP:

1. **Synced.** Following, no override, and the connection provides a usable
   value. Shown with its as-of date.
2. **Override.** Following, and the person set their own value. The person's
   value is used and labeled "Your value", with the synced value beside it
   and "Use synced value".
3. **Not provided.** Following, but the connection gives nothing for that
   field, for example APR without Liabilities. The person's value is used
   without an override label, and the field says the connection does not
   provide it. Typing a value here is not an override.
4. **Manual.** Not following. Today's behavior.

When the synced value cannot be used, the rule reuses
`DebtAccountBalanceBlock`. A blocked value (no date, negative, other
currency, too large) is shown next to the reason. The debt keeps using its
last usable value and the field is marked stale.

### Editing a followed debt

`PUT /api/debts/{id}` sends every field, so a form that shows a synced
value would send it back as if the person typed it. A sync between opening
and saving would then look like an override. To avoid that:

- In the debt form, synced fields are read-only while following. Each one
  has "Enter my own value", which opens that one field.
- Setting and clearing an override are their own actions, for example
  `PUT /api/debts/{id}/overrides/{field}` and
  `DELETE /api/debts/{id}/overrides/{field}`. `{field}` is a closed set and
  becomes an enum. Upsert keeps ignoring synced fields while following.
- An override equal to the current synced value is still saved as an
  override. The person asked to own that field.

### Linking flow

1. On a debt's detail panel, a quiet secondary action says "Follow a
   connected account". The page's primary action stays Add debt.
2. Cardui lists up to three suggestions, each with plain reasons. A match is
   a pure `Domain` rule over values the service loads once:
   - Eligible accounts only (see Two kinds of link).
   - Revolving matches a credit account. Installment matches a loan.
   - Signals: the last four digits (`Mask`) appear in the debt name; words
     from the account name, official name, or institution name appear in
     the debt name; the latest balance is close to the debt's dated
     balance.
   - Reasons read like "Ends in 4821" or "Balance within $38 of yours". No
     score is shown.
   - "Choose another account" lists every eligible account. "None of these"
     closes the step. Suggestions show only when the person asks, so a
     dismissal does not need to be stored in the first slice.
3. The confirm step shows what will follow, and what stays theirs.
4. If the debt already has a balance that differs from the synced one, the
   confirm step shows both with their dates. "Use connected balance" is the
   primary choice. "Keep mine as my own value" saves it as an override. The
   same applies to any Liabilities field the person already filled.
5. Confirm saves. A toast says "Store card now follows Visa ending 4821".

An existing reference link to an eligible account shows "Follow this
account" in the same place, and skips the suggestion step.

### Freshness and broken connections

Each followed debt shows one freshness line under its balance. The state is
a pure rule over values already stored:

| State | When | What the card says | Action |
| --- | --- | --- | --- |
| Current | Latest snapshot is within the threshold and the last sync succeeded | "Synced Oct 6" | None |
| Stale | Latest snapshot is older than the threshold | "Last synced Oct 2. A refresh has not run since." | Refresh |
| Sync failing | `LastSyncFailedAt` is after the last success | "Sync failed Oct 5. Showing the Oct 4 balance." | Reconnect, or Update balance |
| Disconnected | The account's `PlaidItemId` is null (bank link removed) | "Bank link removed. Showing the Oct 4 balance." | Reconnect, Stop following |
| Account missing | `IsActive` is false or the account is archived | "The bank no longer returns this account." | Stop following |

- The last balance stays visible in every state. Stale data is never hidden
  or zeroed.
- "Update balance" sets a balance override with today's date. When the
  connection recovers, the card shows both values and offers "Use synced
  value". Sync does not clear the override on its own.
- Reconnect needs a Plaid update-mode link token for the existing item. That
  does not exist today and is its own slice. Until then, the action opens
  the Connections page.
- The summary keeps using the debt's resolved values. A stale value still
  counts, and the summary names how many debts are stale, the same way it
  names missing inputs.

### Stop following

"Stop following" sits on the detail panel with the other link actions. It is
not destructive, so it does not ask first, but the toast says what happened:
"Store card is manual again. Balance kept as $1,240.18 from Oct 6."

- Each synced field without an override copies its last synced value into
  the debt's own column. The balance takes the snapshot date as
  `BalanceAsOf`, the same way `use-account-balance` does today.
- Overrides stay as the person's values. The override labels go away.
- The link drops back to a reference link. The account stays named on the
  debt, and the person can clear it in the form.
- The account, its snapshots, and its transactions are not changed.

### Data model sketch

Description only. Zach generates and applies the migration himself after
review. Names are proposals.

On `Debt`:

- `AccountFollowedSince` (`DateTimeOffset?`). Null is a reference link or no
  link. A value means the debt follows `AccountId` and records when that
  started. It is set only together with `AccountId`.
- One override timestamp per synced field the person can own:
  `BalanceOverriddenAt`, `CreditLimitOverriddenAt`,
  `MinimumPaymentOverriddenAt`, `NextDueDateOverriddenAt`,
  `AprOverriddenAt` (`DateTimeOffset?`). Null means no override. A value
  means the debt's own column is the value in use, and when the person set
  it. The timestamp is the provenance shown as "Your value since Oct 3".
- No synced-value columns on `Debt`. Synced values stay on the account side.
- A filtered unique index on `AccountId` where `AccountFollowedSince` is not
  null, so one account has at most one following debt.
- `AccountId` keeps `SetNull` on delete. No API path deletes an account
  today. If one is added, it must stop the follow first, so the snapshot is
  copied before the link is lost.

On the account side:

- `Account.CreditLimit` (`decimal?`), copied from `balances.limit` by
  `PlaidAccountSyncService`.
- Only if Liabilities is approved: one row per account, for example
  `AccountLiabilityTerms` with `AccountId`, `StatementBalance`,
  `StatementDate`, `MinimumPayment`, `NextDueDate`, `PurchaseApr`, and
  `SyncedAt`. Sync writes it. A debt only reads it.

In `Domain`, each type in its own file per `.cursor/rules/backend-type-files.mdc`
and `.cursor/rules/backend-enums.mdc`:

- `DebtFieldSource` enum: `Synced`, `Override`, `NotProvided`, `Manual`.
- `DebtLinkFreshness` enum: `Current`, `Stale`, `SyncFailing`,
  `Disconnected`, `AccountMissing`.
- `DebtSyncedField` enum for the override route: `Balance`, `CreditLimit`,
  `MinimumPayment`, `NextDueDate`, `Apr`.
- A resolver that returns each field's value, source, and as-of date, and a
  matcher that ranks eligible accounts with reasons.

`DebtDto` gains the follow state, freshness, and, per synced field, its
source, synced value, and as-of date. The frontend types follow in
`frontend/lib/api/types/debts.ts`.

## Dependency: sync correctness first

A followed balance is only as right as the sync behind it. Before or as the
first slice:

- Add `PlaidAccountSyncService` tests: balances copied, today's snapshot
  replaced in the household time zone, an account Plaid stops returning
  marked inactive, an archived account left archived.
- Settle overlapping worker and manual sync for one item. Two syncs at once
  can both replace today's snapshot. That decides whether a debt reads a
  half-finished state.
- Freshness depends on `LastSyncCompletedAt` and `LastSyncFailedAt` being
  right when a sync is interrupted. Add a test for that.

## Plaid Liabilities: open decision

`/accounts/get`, which the sync already calls, gives the current balance,
the available balance, and the credit limit. It does not give APR, minimum
payment, statement balance, or due date. Those come from the Liabilities
product.

Before Zach decides, confirm these against the current Plaid documentation
and the Plaid plan in use. Do not assume them:

- **Coverage.** Liabilities is documented for credit cards, student loans,
  and mortgages. Auto loans and personal loans are generally not covered,
  and support varies by institution. An installment debt may get only a
  balance.
- **Cost.** Liabilities is billed separately from Transactions. The amount
  depends on the plan. Record it next to the other bank-link costs the
  action plan asks to measure.
- **Consent.** The link token asks only for Transactions today. Existing
  connections would need the person to consent again, through update mode
  or an added product, before Liabilities data arrives.
- **Freshness.** `/accounts/get` returns the balance Plaid last refreshed,
  which can be up to about a day old. A real-time balance call is a
  separate, billed request. The daily worker does not need it.

Without Liabilities, this design still works. The balance and credit limit
follow. APR, minimum, and due date stay person-owned and show "Not provided
by this connection".

## Proposed slices

Each slice is one review. Each one leaves the app working.

0. **Sync correctness.** `PlaidAccountSyncService` tests, a decision and
   guard for overlapping worker and manual sync, and interrupted-sync
   timestamps. No schema change.
1. **Follow a balance.** `AccountFollowedSince` and `BalanceOverriddenAt`,
   the follow and stop-following actions, the resolver for the balance
   only, the freshness line, and the confirm step when balances differ.
   Pick the account from a list. No suggestions yet. One migration.
2. **Overrides.** The override and "Use synced value" actions for the
   balance, the read-only synced field in the form, and "Update balance"
   from a stale card. No schema change beyond slice 1.
3. **Suggestions.** The matcher and the suggestion step. No schema change.
4. **Credit limit.** Store `balances.limit` on the account, follow it, and
   allow its override. One migration.
5. **Reconnect.** An update-mode link token and a reconnect action from the
   card and the Connections page. No schema change expected.
6. **Liabilities, only if approved.** The account terms row, sync for it,
   consent for existing connections, and the minimum, due date, statement
   balance, and APR fields. One migration.

## UI notes

These follow `.cursor/rules/ui-governance.mdc`.

- Debts stays in the account menu. Following adds no page, tab, or second
  navigation.
- The follow, suggestion, and confirm steps are nested steps in the debt's
  right-hand panel from 768px up and in the full-screen sheet under it. They
  use back at both widths. Escape closes the open step and returns focus to
  "Follow a connected account".
- Suggestions are a list of buttons. Each has an accessible name with the
  account and its reasons, for example "Visa ending 4821, balance within
  $38 of yours". Choosing one is a separate confirm, not an instant link.
- A synced field shows a small "Synced" label in text, not only an icon or
  color. An override shows "Your value" in text, the synced value beside it,
  and a "Use synced value" button.
- The freshness line is text with an icon. Stale and failing states do not
  rely on color alone. The balance stays visible and readable.
- Every action says what happened: following, overriding, reverting,
  updating, and stopping. A failure says what failed and how to retry.
- The debt list keeps its loading, empty, and error states. Following adds
  a stale state per card, and a count in the summary.
- On a phone, link actions and "Use synced value" have a 44px target.
- The confirm step reuses the two-balance layout from the debt summary, so
  people see one comparison pattern.

## Questions for Zach

1. Should Liabilities be in scope? If yes, after the balance slices, or
   only once pricing and coverage are confirmed?
2. Is the stale threshold two days in the household time zone? The worker
   runs daily, so one missed run should not alarm.
3. A negative synced balance means the card owes the person money. Should a
   followed revolving debt show $0 with a note, or keep the last usable
   balance as it does for other blocked values?
4. When the bank link is removed, should following stop on its own with the
   snapshot kept, or stay followed and stale until the person chooses?
   This note proposes stale until they choose.
5. Should one account ever back two debts, for example a card split into
   two plans? This note proposes no.
6. APR from Liabilities lists several rates per card. Is the purchase APR
   enough, or should the debt note a balance-transfer or cash-advance APR
   too?
7. Should a statement balance ever be overridable? This note proposes it is
   shown only.
8. Should slice 0 land before this design is approved, as the next
   engineering increment, regardless of the decision on following?
