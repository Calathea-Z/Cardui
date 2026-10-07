# Linked manual debts

Status: Approved. Follow a balance is built. Later slices are not.
Date: October 6, 2026
Updated: 2026-10-07

Every design question is decided (see Decisions).

## Problem

A person records a debt by hand. When they also connect that card or loan,
they still have to retype the balance after every payment. The debt and the
connected account stay separate on purpose, so a sync can never overwrite
something the person typed.

This note designs an optional way for a debt to follow a connected
account's balance and credit limit. A sync still never overwrites what the person typed.

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
  still open in `docs/roadmap.md` and in every
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
- Following uses basic sync only. Plaid Liabilities is not adopted (see
  Decisions).
- Sync correctness comes first.

## Design

### Two kinds of link

Keep today's link and add a second mode. Do not change what an existing
link does.

| Mode | Meaning | Who sets the balance |
| --- | --- | --- |
| Reference | The debt names an account. Nothing is copied until the person chooses. When that account can be followed, "Use this balance" starts the follow. When it cannot, the balance is copied once. | The person, until they follow |
| Follow (new) | The debt uses the connected account's synced fields, except where the person set an override. | The connection, field by field |

- Every existing debt with an `AccountId` stays a reference link. The
  migration must not turn any existing link into a follow.
- Follow is allowed only for a connected credit card or loan: `Source` is
  `Plaid`, `PlaidItemId` is set, the account is active and not archived,
  `AccountLedger.IsLiability` is true, and the currency matches the debt.
  A manual account already holds a balance the person types, so following
  it adds nothing.
- One account backs one debt at most. Each account is separate. Two debts
  following one card would count that balance twice in the summary.

### Sync never writes a debt

The sync keeps writing only account-side rows: `Account` and
`AccountBalanceSnapshot`. It does not load or update `Debts`.

A followed debt reads its synced values when it is shown or planned, the
same way `LoadLinkedBalancesAsync` reads the latest snapshot today. That
keeps the rule from the debts review true by construction: a sync cannot
overwrite a debt's own columns, because it never touches them.

The debt's own columns keep holding the person's values. When the person
stops following, the last synced values are copied into those columns once,
by the person's action, not by sync.

### Field ownership

Following uses what the sync already fetches from `/accounts/get`. Nothing
else is synced.

| Field | Owner when following | Source |
| --- | --- | --- |
| Current balance and its date | Synced | Latest `AccountBalanceSnapshot` |
| Credit limit (revolving) | Synced | `balances.limit`. Not stored today. |
| APR | Person | Debt |
| Minimum payment | Person | Debt |
| Next due date | Person | Debt |
| Name, type | Person | Debt |
| Promotional APR and end date | Person | Debt |
| Months left (installment) | Person | Debt |
| Payoff priority, notes, target payment | Person | Not built yet. Owned by the person when added. |

There is no statement balance on a debt, and following does not add one.

A synced field resolves to one of three states. A pure rule in `Domain`
decides it from values, with no database, clock, or HTTP:

1. **Synced.** Following, no override, and the connection provides a usable
   value. Shown with its as-of date.
2. **Override.** Following, and the person set their own value. The person's
   value is used and labeled "Your value", with the synced value beside it
   and "Use synced value".
3. **Manual.** Not following, or the connection gives nothing for that
   field, such as a loan with no credit limit. The person's value is used
   without an override label. Typing a value here is not an override.

The person-owned fields stay as they are today on a followed debt. The card
says they are entered by the person, so it is clear the connection does not
keep them current.

When the synced balance cannot be used, the rule reuses
`DebtAccountBalanceBlock`. A blocked value (no date, other currency, too
large) is shown next to the reason. The debt keeps using its last usable
value and the field is marked stale.

A negative synced balance is different. It means the card owes the person
money. On a followed revolving debt the balance in use is $0, with a note
that names the credit, for example "The card shows a $42.10 credit. Counted
as $0." The negative amount is not stored on the debt.

### Editing a followed debt

`PUT /api/debts/{id}` sends every field, so a form that shows a synced
value would send it back as if the person typed it. A sync between opening
and saving would then look like an override. To avoid that:

- In the debt form, the balance and credit limit are read-only while
  following. Each one has "Enter my own value", which opens that one field.
  APR, minimum, due date, and the other person-owned fields stay editable
  as they are today.
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
     dismissal does not need to be stored.
3. The confirm step shows what will follow (balance, and credit limit on a
   card) and what stays theirs (APR, minimum, due date, and the rest).
4. If the debt already has a balance or credit limit that differs from the
   synced one, the confirm step shows both with their dates. "Use connected
   value" is the primary choice. "Keep mine as my own value" saves it as an
   override.
5. Confirm saves. A toast says "Store card now follows Visa ending 4821".

An existing reference link to an eligible account shows "Follow this
account" in the same place, and skips the suggestion step.

### Freshness and broken connections

Each followed debt shows one freshness line under its balance. The state is
a pure rule over values already stored:

| State | When | What the card says | Action |
| --- | --- | --- | --- |
| Current | Latest snapshot is no more than two days old in the household time zone, and the last sync succeeded | "Synced Oct 6" | None |
| Stale | Latest snapshot is more than two days old in the household time zone | "Last synced Oct 2. A refresh has not run since." | Refresh |
| Sync failing | `LastSyncFailedAt` is after the last success | "Sync failed Oct 5. Showing the Oct 4 balance." | Reconnect, or Update balance |
| Disconnected | The account's `PlaidItemId` is null (bank link removed) | "Bank link removed. Showing the Oct 4 balance." | Reconnect, Stop following |
| Account missing | `IsActive` is false or the account is archived | "The bank no longer returns this account." | Stop following |

- The stale threshold is two days in the household time zone. The worker
  runs daily, so one missed run does not mark a debt stale.
- The last balance stays visible in every state. Stale data is never hidden
  or zeroed.
- Removing the bank link does not stop the follow. The debt stays followed
  and stale, showing the last balance, until the person reconnects, updates
  the balance, or stops following.
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

Description only. Each slice that changes the model updates the model and
`DbContext` configuration, then stops. Zach generates and applies the EF
Core migration himself (`.cursor/rules/migrations.mdc`). Names are
proposals.

On `Debt`:

- `AccountFollowedSince` (`DateTimeOffset?`). Null is a reference link or no
  link. A value means the debt follows `AccountId` and records when that
  started. It is set only together with `AccountId`.
- One override timestamp per synced field: `BalanceOverriddenAt` and
  `CreditLimitOverriddenAt` (`DateTimeOffset?`). Null means no override. A
  value means the debt's own column is the value in use, and when the
  person set it. The timestamp is the provenance shown as "Your value since
  Oct 3".
- No synced-value columns on `Debt`. Synced values stay on the account side.
- A filtered unique index on `AccountId` where `AccountFollowedSince` is not
  null, so one account backs at most one debt.
- `AccountId` keeps `SetNull` on delete. No API path deletes an account
  today. If one is added, it must stop the follow first, so the snapshot is
  copied before the link is lost.

On the account side:

- `Account.CreditLimit` (`decimal?`), copied from `balances.limit` by
  `PlaidAccountSyncService`.

In `Domain`, each type in its own file per `.cursor/rules/backend-type-files.mdc`
and `.cursor/rules/backend-enums.mdc`:

- `DebtFieldSource` enum: `Synced`, `Override`, `Manual`.
- `DebtLinkFreshness` enum: `Current`, `Stale`, `SyncFailing`,
  `Disconnected`, `AccountMissing`.
- `DebtSyncedField` enum for the override route: `Balance`, `CreditLimit`.
- A resolver that returns each synced field's value, source, and as-of
  date, and a matcher that ranks eligible accounts with reasons.

`DebtDto` gains the follow state, freshness, and, per synced field, its
source, synced value, and as-of date. The frontend types follow in
`frontend/lib/api/types/debts.ts`.

## Dependency: sync correctness first

A followed balance is only as right as the sync behind it. Slice 0 does not
wait for the linked-debt work. It is the next engineering increment. The
Plaid transaction reconciliation tests are already done
(`docs/reviews/2026-10-05-008-plaid-sync-reconciliation-tests.md`). Slice 0
covers:

- Add `PlaidAccountSyncService` tests: balances copied, today's snapshot
  replaced in the household time zone, an account Plaid stops returning
  marked inactive, an archived account left archived.
- Settle overlapping worker and manual sync for one item. Two syncs at once
  can both replace today's snapshot. That decides whether a debt reads a
  half-finished state.
- Freshness depends on `LastSyncCompletedAt` and `LastSyncFailedAt` being
  right when a sync is interrupted. Add a test for that.

`/accounts/get` returns the balance Plaid last refreshed, which can be up
to about a day old. A real-time balance call is a separate, billed request.
The daily worker does not need it, and this design does not use it.

## Slices

Each slice is one review. Each one leaves the app working. The roadmap
lists slices 0 to 5 as items 1 to 6 of "Sync correctness and linked debts",
between Phase 2 items 5 and 6.

0. **Sync correctness.** `PlaidAccountSyncService` tests, a decision and
   guard for overlapping worker and manual sync, and interrupted-sync
   timestamps. No schema change.
1. **Follow a balance.** `AccountFollowedSince` and `BalanceOverriddenAt`,
   the follow and stop-following actions, the resolver for the balance
   only, the freshness line, the $0 rule for a negative balance, and the
   confirm step when balances differ. Pick the account from a list. No
   suggestions yet. Model change; Zach generates the migration.
2. **Overrides.** The override and "Use synced value" actions for the
   balance, the read-only synced field in the form, and "Update balance"
   from a stale card. No schema change beyond slice 1.
3. **Suggestions.** The matcher and the suggestion step. No schema change.
4. **Credit limit.** Store `balances.limit` on the account, follow it, and
   allow its override. Model change; Zach generates the migration.
5. **Reconnect.** An update-mode link token and a reconnect action from the
   card and the Connections page. No schema change expected.

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

## Decisions

Zach decided these on October 6, 2026.

1. **Plaid Liabilities is not adopted.** Following uses basic sync only:
   the balance and the credit limit. APR, minimum payment, due date, and
   statement balance stay entered by the person. Liabilities may be looked
   at far in the future. The notes for that case are kept below.
2. **Stale threshold.** Two days in the household time zone.
3. **Negative balance.** A negative synced balance on a followed revolving
   debt is counted as $0, with a note that names the credit.
4. **Bank link removed.** The debt stays followed and stale, with the last
   balance, until the person reconnects, updates the balance, or stops
   following.
5. **One account, one debt.** An account backs at most one debt. Each
   account is separate.
6. **APRs.** Recorded under the future section. If Liabilities is ever
   adopted, keep every APR the connection returns, not only the purchase
   APR.
7. **Statement balance.** Recorded under the future section. If Liabilities
   is ever adopted, a synced statement balance is display only and cannot
   be overridden.
8. **Slice 0 goes first.** Account sync tests and overlapping worker and
   manual sync do not wait for the linked-debt work. They are the next
   engineering increment, with interrupted-sync timestamps.

## Future: if Liabilities is ever adopted

Not planned. This section keeps the earlier analysis and Zach's answers so
a later decision can start from them. Nothing here is part of the slices
above.

### What it would add

`/accounts/get` gives the current balance, the available balance, and the
credit limit. Liabilities would add APRs, minimum payment, statement
balance and date, and next due date. Those fields would move from
person-owned to synced, with the same override rules as the balance.

### Rules already decided for that case

- Keep every APR the connection returns (purchase, balance transfer, cash
  advance, special, and any other type), each with its type, so the person
  sees as much as the bank gives. The purchase APR would be the one the
  summary uses for interest, and the only one the person can override. A
  promotional rate the person entered would still win through its end
  date.
- A synced statement balance is display only on a followed debt. It cannot
  be overridden.
- The confirm step would also compare any of those fields the person
  already filled, with the same "Use connected value" and "Keep mine"
  choices.

### Model sketch for that case

- Override timestamps on `Debt` for `MinimumPayment`, `NextDueDate`, and
  `Apr`, and those members added to `DebtSyncedField`. A `NotProvided`
  source for a field the connection does not cover.
- One row per account, for example `AccountLiabilityTerms` with
  `AccountId`, `StatementBalance`, `StatementDate`, `MinimumPayment`,
  `NextDueDate`, and `SyncedAt`. Sync writes it. A debt only reads it.
- One row per APR per account, for example `AccountLiabilityApr` with
  `AccountId`, `AprType`, `Percentage`, and the balance and interest charge
  the connection reports for that rate when it gives them. `AprType` stays
  a string, because the connection defines that set and can extend it
  (`.cursor/rules/backend-enums.mdc`). Sync replaces an account's APR rows
  on each refresh.

### What to confirm first

Confirm these against the current Plaid documentation and the Plaid plan in
use. Do not assume them:

- **Coverage.** Liabilities is documented for credit cards, student loans,
  and mortgages. Auto loans and personal loans are generally not covered,
  and support varies by institution.
- **Cost.** Liabilities is billed separately from Transactions. The amount
  depends on the plan. Record it next to the other bank-link costs the
  roadmap asks to measure.
- **Consent.** The link token asks only for Transactions today. Existing
  connections would need the person to consent again, through update mode
  or an added product, before Liabilities data arrives.
