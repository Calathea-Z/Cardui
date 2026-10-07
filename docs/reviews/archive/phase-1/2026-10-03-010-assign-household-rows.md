# Assign existing rows to the household

Date: October 3, 2026

## Increment

Assigned the existing unscoped financial rows in the local database to the
only household. No schema change and no new EF Core migration. System
categories and subgroups stay shared.

## Changes

- Backed up `cardui` from the `cardui-postgres` container before changing
  rows. 
  It contains bank access tokens and financial rows. It is outside the
  repository and must not be committed or shared.
- Set `HouseholdId` on the 5 unscoped Plaid items, the 3 custom categories,
  and the 1 custom subgroup. Accounts, transactions, and balance snapshots
  have no household column; they follow the Plaid item.
- Left 15 system categories, 15 system subgroups, and 3 groups shared.
  Their household id stays null.
- Did not change category or subgroup names or keys. See the key review
  below.

Row totals were the same before and after. Nothing was inserted or deleted.

| Rows | Before | After |
| --- | ---: | ---: |
| Households | 1 | 1 |
| Plaid items on that household | 0 | 5 |
| Plaid items with no household | 5 | 0 |
| Accounts following that household | 0 | 13 |
| Transactions following that household | 0 | 1,271 |
| Balance snapshots following that household | 0 | 69 |
| Custom categories on that household | 0 | 3 |
| Custom subgroups on that household | 0 | 1 |
| System categories still shared | 15 | 15 |
| System subgroups still shared | 15 | 15 |

Two transactions use custom categories. One custom category sits on the
custom subgroup, and the other two sit on system subgroups.

## Category and subgroup keys

Reviewed the live unique indexes and the service checks. No migration.

The database still enforces these case-sensitive unique indexes:

- `Categories.Key`
- `Categories.Name`
- `SubGroups.Key`
- `SubGroups (GroupId, Name)`
- `Groups.Key` and `Groups.Name` (groups have no household column)

There were no case-insensitive duplicate names or keys, and no custom name
or key matched a system row. Assigning household ids did not depend on
changing those indexes.

The category and subgroup services also reject a name or key that already
exists anywhere in the table, using a case-insensitive name check. That is
stricter than the indexes, which are case-sensitive. A second household
still cannot create a custom category or subgroup whose name or key matches
the shared catalog or this household's custom rows.

A later change could allow the same custom name in different households.
That needs partial unique indexes, because a normal unique index on
`(HouseholdId, Name)` does not treat null household ids as the same value,
so it would not keep the shared catalog unique. The service checks would
have to match. That is a new migration and was not generated.

## Recovery

Stop the API and worker before either recovery path so nothing writes during
the restore.

Targeted undo, which clears the household id only on the rows this
assignment changed:

```powershell
Get-Content "$env:LOCALAPPDATA\Cardui\backups\2026-10-03-before-household-assign-undo.sql" |
  docker exec -i cardui-postgres psql -U cardui_user -d cardui -v ON_ERROR_STOP=1
```

The script refuses to commit unless it clears exactly 5 Plaid items, 3
custom categories, and 1 custom subgroup. It does not drop tables and does
not change rows created after the assignment.

Full restore, which replaces the current database with the backup, including
later edits made after the dump:

```powershell
docker cp "$env:LOCALAPPDATA\Cardui\backups\2026-10-03-before-household-assign.dump" cardui-postgres:/tmp/cardui-restore.dump
docker exec cardui-postgres pg_restore -U cardui_user -d cardui --clean --if-exists --no-owner /tmp/cardui-restore.dump
docker exec cardui-postgres rm -f /tmp/cardui-restore.dump
```

`--clean` drops existing objects before recreating them. Use it only to
return to the pre-assignment backup.

After either path, the expected unscoped counts are 5 Plaid items, 13
accounts, 1,271 transactions, 69 snapshots, 3 custom categories, and 1
custom subgroup.

## Agent verification

- Confirmed `ScopeHouseholdData` is the latest applied migration.
- Confirmed the backup archive lists table data for households, Plaid items,
  accounts, transactions, snapshots, categories, subgroups, groups, and the
  migration history.
- The assignment ran in one transaction and committed only after the
  expected update counts and follow-through counts matched.
- Recounted after commit. Totals match the table above. System rows stayed
  shared.
- No application code changed, so the test suite was not re-run. Did not
  commit.

## Manual verification

Zach confirmed on October 3, 2026 that account details are showing again
for his profile.

A second household was not signed in. Isolation between two households
remains covered by the tests in the household-scope review.

Do not commit `frontend/.env` or `frontend/.env.local`. Do not commit the
backup or the undo script.

Zach approved this increment on October 3, 2026.

## Remaining considerations

- System category edits still change the shared catalog.
- Custom category and subgroup names and keys remain unique across every
  household until a separate migration.
- Hobby keeps a 7-day session, has no multifactor authentication, and shows
  Clerk branding.
