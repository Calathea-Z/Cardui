Date: 2026-10-17
Status: Approved 2026-10-17
PR: None

# User journey, roadmap, and validation assessment

## Finding

The roadmap's first product outcome is sound, but the implementation sequence
has advanced past an unmet Phase 4 exit. Items 1–9 are approved building
blocks; a person still cannot complete guided planning. Phase 5 item 1 is
approved, but more follow-through features should wait for a usable first-plan
and return-session journey. This is a proposed sequencing correction, not an
approved scope change or a reason to discard completed work.

## Evidence

- `docs/roadmap.md`, Phase 4, requires starting with “I don't know where to
  begin,” returning another day, correcting a fact, reviewing a calculated
  plan, and accepting it. Its status explicitly records no chat screen or
  live call. No numbered item explicitly owns the complete integration.
- `docs/README.md` nevertheless schedules Phase 5 item 2 next.
- `api/Services/Planning/OpenAiPlanningModel.cs`, `CreateMessage`, sends text
  and instructions with no tools. The narrow-tools review confirms that
  `IPlanningTools` methods are not exposed to the model.
- `api/Services/Implementations/PlanningCalls.cs`, `AskAsync`, returns a
  reply but does not persist it or prepare consent-filtered context. A
  production entry point must compose those separate services; adding a
  screen and credentials alone is insufficient.
- The plan-revision review records no route or screen and no conversation
  linkage on accept. Fact proposals validate inputs without saving them;
  the narrow-tools review identifies unresolved create-versus-update behavior.
- Planning-call limits bound request size and retries, not cumulative dollar
  spend. Model selection and representative live quality/cost evaluation
  from Phase 4 item 1 remain unresolved.
- Phase 3 already had UX repairs and behavior QA. Its QA report lists limits,
  including empty households, actual fact edits after acceptance, and detail
  panel keyboard focus. Those are not reported failures and should not be
  described as if no testing occurred.
- Conversation, planning, and revision service tests use EF InMemory. They
  provide useful service evidence but do not establish PostgreSQL transaction,
  constraint, concurrency, or authenticated HTTP behavior.

## Holes in the user story

1. **Starting:** where a new person begins, whether to chat or enter facts,
   the minimum information needed for a useful provisional plan, and a clear
   path when they decline AI sharing. A full inventory cannot be the price
   of receiving the first useful answer.
2. **Confirming facts:** distinguish unknown, explicitly none, estimated,
   proposed, and confirmed. Phase 5 item 1 acknowledges that a household
   with no bills cannot save “none.” A correction needs to identify the
   existing fact, show the change, save once, and refresh the same data used
   by direct editing. Chat acknowledgments alone cannot count as saved facts.
3. **Keeping a plan:** explain working preview, draft, and accepted plan in
   user terms. Accepting a plan must say what is saved and whether budget
   settings change. “Accept into their budget” currently needs an explicit
   contract; it must not imply that payments or transfers occurred.
4. **Returning:** restore useful context, show what changed since acceptance,
   and let the person correct a fact without starting over. Live Overview
   and a historical accepted plan must not appear to be the same version.
5. **Recovering:** preserve a message/draft through timeout, retry, reload,
   duplicate submission, declined consent, and manual edits during chat.
   A fallback notice in a service is not yet a usable recovery flow.
6. **Knowing when to stop building:** item approval is being treated as a
   substitute for phase-exit evidence. The initial alpha boundary also
   contains enough scenarios and follow-through features to postpone learning
   whether the core experience is understandable.

## Proposed bounded sequence

Keep Phase 5 item 1. Pause items 2–6 while closing the earlier journey gap.
If work is in progress in Cursor, preserve it and agree a stopping point;
this assessment does not authorize interrupting or reverting that work.

1. **Agree one journey and screen flow.** Define the start point, minimal
   intake, confirmation, calculated plan, acceptance, and return. Reuse the
   existing visual system and editors. Choose chat placement deliberately;
   do not add another parallel planning workspace by default. Decide the
   acceptance/budget contract before implementation.
2. **Complete Phase 4 integration in reviewable increments.** Add authenticated
   conversation endpoints and orchestration for persistence, consent,
   scoped context, bounded tools, calculations, fact confirmation, and plan
   review/acceptance. Connect one usable conversation UI. Ensure numeric
   claims agree with calculated results; a calculation service alone does
   not prevent free-text model invention. Preserve manual use.
3. **Run a bounded live pilot and immediate UX repair.** Choose a model and
   authorize a small evaluation budget and synthetic-data use before live
   calls. Measure cost, latency, correctness, and completion of the journey.
   Finish these checks before declaring Phase 4's exit met.
4. **Bring Phase 5 item 7 forward as an initial validation gate.** Start with
   the five journeys below; expand to the remaining listed alpha cases
   before claiming the full alpha exit. Only then choose the smallest
   follow-through increment justified by observed friction.

Defer richer action dependencies, automatic actual-versus-expected proposals,
additional recovery indicators, and behavioral supports until this gate.
Phase 7 expansion and billing stay later. Core mobile, keyboard, empty-state,
and interruption usability move into this gate; broader operational launch
hardening stays in Phase 6. No rewrite, new analytics platform, new scenario
engine, or visual rebrand is proposed.

## Where UX and heavy testing are most needed

Highest UX priority is the complete path across Home, setup, chat, and Plan,
especially first use, correcting facts, and understanding what is saved.
Next is Plan comprehension: can a person identify the next action, its
reason, the cash constraint, and whether they are viewing current facts or
an accepted historical version? Review phone and keyboard use along those
same paths now, including focus return from detail panels. Do not wait for
Phase 6 to discover that the primary journey cannot be completed.

Highest engineering-test priority is the newly connected boundary: signed-in
HTTP requests through household scoping, consent, persistence, model/tool
dispatch, and confirmed writes. Use focused PostgreSQL integration tests for
duplicate sends/accepts, concurrent edits, stale reviews, and rollback. Keep
existing calculation fixtures and add cross-path assertions that the same
facts yield the same plan through chat and direct editing. Test provider
failure, malformed tool arguments, foreign IDs, untrusted text, unsupported
requests, and cumulative usage controls. Live synthetic evaluations establish
model behavior that mocked provider tests cannot.

Do not rerun every old test or automate every screen as a substitute for
these checks. Zach remains the manual UX and complete-workflow tester.

## Manual acceptance proposed for Zach

Use an isolated synthetic household; these future checks create and change
test facts, messages, consent choices, and plan versions. Live chat consumes
the separately approved API budget. No real money should move.

1. Start with no bank connection and incomplete facts. Reach a useful
   provisional answer, with unknowns visible and one clear next step.
2. Use a household with a shortfall and unknown debt terms. Get an honest
   shortfall explanation without invented surplus, dates, or terms.
3. Confirm facts and accept a calculated plan. Reload and identify what was
   saved and the next action without developer explanation.
4. Return, correct income manually, and resume chat. Reject the old proposal,
   explain the revised plan, and keep the previous accepted version intact.
5. Decline consent or interrupt a reply, then continue manually or retry.
   Preserve work and avoid duplicate facts, messages, or accepted versions.

Repeat the core path at phone width and with keyboard navigation. Record
hesitations and interventions as usability findings even if every button works.

## Verification and limits

Repository/document assessment only. Read the roadmap, current status, recent
Phase 4 and Phase 5 reports, prior UX/QA records, and relevant implementation
and test setup. No live UI inspection, test execution, model call, credential
access, database change, or application-code change occurred. Existing test
counts are historical evidence, not newly verified results. Current visual
usability findings require Zach's walkthrough; earlier repaired defects are
not asserted to remain present.

## Approval

Approved by Zach on October 17, 2026 as a finding to incorporate. The checks
under "Manual acceptance proposed for Zach" were not run, and this approval
does not say they passed. The revised sequence is in
[`2026-10-17-004-core-journey-sequence.md`](2026-10-17-004-core-journey-sequence.md)
and awaits review before implementation.
