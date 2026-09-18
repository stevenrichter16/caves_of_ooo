# R2 proposal: close the regional loop with truthful, durable outcomes

Status: accepted and promoted to local main, 18 September 2026. Original plan adopted 17 September; implementation followed R1 acceptance with confirmed RED before production edits. Sources below refer to the verified clone `/tmp/coo-regional-verification-20260917`; no implementation is implied. R104 now supplies a vulnerable native opening result (58/58, ending at 31 HP), as reported by root.

## Recommendation

Implement **state-aware regional field notes with an accurate delivery receipt and explicit next action** across the existing five finite bindings. This is the smallest high-value continuation: it makes the consequences of a journey comprehensible and persistent, rather than introducing another reward, another template, or a simulated service shortage. Do not expand the number of requests in this phase.

There is a concrete contradiction today. `RegionalSituationNotes.Describe` (lines 31–48) prepends `[completed]` but still emits “Bring…”, “Outside goods still fulfill the request”, prospective payment, and “Read, deliver, or release”. `RegionalRequestPart.CanAct` (66–76) correctly forbids all three once completed. Root's R104 screenshot exhibits this, so it is a player-observed mismatch rather than speculative polish. The detailed habitat outcome currently survives only in an AfterCommit message (219–225); the persistent note receives only the string state and source-availability snapshot. A returning player therefore cannot reliably see what their particular delivery accomplished.

The release vision's ten-minute loop explicitly asks the player to “solve and reassess” and recognizes someone remembering a decision. Its regional contract asks for lasting outcomes and explicit release. This proposal makes already-existing outcomes readable; it does not pretend to implement the cosmic closure ledger. `Lore/Design/V1-DramaticCore.md:113–114` keeps Concord trade at hub scale while deeper faction arcs remain separate.

## Options considered

1. **Recommended: outcome-aware notes/receipts.** Highest certainty, directly demonstrated mismatch, small transaction/UI surface, all five existing situations benefit. Makes actual supplied stock, optional preserved-bank result, payment and available follow-up understandable after save/reload.
2. **Separate inspection from undertaking a request.** Currently the C action called “read the regional request” sets Accepted=true (`RegionalRequestPart:91,113–123`). An explicit preview/accept choice would better express optional commitment. Valuable next UX slice, but changes interaction semantics, cues and native routes; it should receive its own RED/acceptance cycle rather than sneak into receipt work. No current evidence proves that accepting carries a cosmic obligation.
3. **Material-use preparation guidance.** Explain the choice between delivering ChoirIron and using its existing tinkering recipe (`Content/Data/Tinkering/Recipes_V1.json:86`), or buying goods instead of mining under Choir law. Potentially meaningful, but only after confirming recipe availability, required skills and actual UI access for that player. Avoid advertising an unavailable recipe or adding new crafting/economic benefits just to decorate a request.

## Concrete bounded behavior

- **Accepted:** retain exact goods, destination, source-as-last-observed, relevant hazard warning and genuine deliver/release instructions. Clearly distinguish alternative purchased supply from unique recovery cargo. No journal-open source generation or world scan.
- **Released:** say no delivery or payment occurred. Do not instruct delivery of an inactive request. Say that the player may inquire again; do not promise reacceptance after cargo/recipient destruction. Existing code allows reacquisition where context remains valid, so release is not permanent refusal.
- **Completed:** record a past-tense receipt: delivered goods/converted stock, recipient and address, actual drams including any preservation bonus, reward item, and the actual bank result if applicable. State that this request is settled and cannot pay again. Remove source-gathering instructions and prospective reward language.
- **Useful next action:** explain that the delivery joined that recipient's stock at delivery time and can be inspected through the existing trade conversation if they remain available. This must be historical wording, not a promise that the goods remain unsold, that the NPC is still alive, or that buying them back is required. `TraderPart` supplies native inventories and existing restocking; neither it nor the new note should promise a simulated shortage or production chain.
- **Habitat result:** distinguish preserved from not preserved at delivery. Missing, destroyed, damaged-and-healed or replaced owners cannot become a positive result. Keep the exact existing testable decision; do not add permanent ecological recovery or a moral score.
- Notes remain readable through Q → Tab with zero active quests, wrapping and pagination. A receipt must expose its result and next action, not only a clipped title.

Implementation should pass the already-computed transaction outcome into receipt formatting instead of recomputing habitat or stock after the transaction. Existing `RecordNote` snapshots source availability even for completion; completed receipts need not query that source. Reuse the persisted per-instance player note string for the smallest patch, or add a minimal explicit receipt payload only if required for formatting. No migrations are required, but do not reinterpret old narrative strings as trusted authoritative completion state. Keep Completed/player completion key as the existing authority.

## Tests first

1. Actual supply delivery creates a completed note without “Bring”, “Outside goods still fulfill”, prospective payment, or deliver/release instructions; active supply still has those valid instructions.
2. Recovery receipt names the actual resulting stock and its exact quantity, rather than claiming the marked cargo itself was stocked.
3. Preserved-bank delivery records the actual extra three drams; damaged, removed and healed/replaced-bank controls record no bonus. No reward changes.
4. Released receipt records no exchanged goods/currency; reaccepting a still-valid request replaces that same instance's note. Destroyed cargo cannot be reaccepted merely because the old note suggests inquiry.
5. Full native save/load preserves completed, released and active notes separately without resetting cues, source ownership or payout guards.
6. Completion callback/reentrancy, currency overflow and post-action exception restore the old note along with inventory, currency and completion state. Existing transaction rollback snapshots at `RegionalRequestPart:104–110` must encompass any new fields.
7. Journal inspection after completion does not generate a missing source zone or require the recipient to remain alive. After merchant stock changes, the receipt remains historical and makes no current-availability claim.
8. Real journal rendering exposes wrapped result and follow-up text; multiple instances remain separately paginated, and travel directions are retained.
9. Delivering again after reload neither duplicates stock nor changes the historical receipt, reward or completion ledger.

Retain the existing authority, cargo, habitat and inventory adversarial suites; do not rewrite them to match new prose. Assert semantic sections/values rather than every punctuation mark. Pair each completed-state omission with active/released positive controls to avoid simply deleting all guidance.

## Native acceptance route

Use the already proven ordinary seed-64 Morrowfast route, without F12 for its opening. Read Orrit's iron request, inspect active Q/Tab directions, obtain the supply through a real allowed route and deliver through C. Open Q/Tab immediately to capture the settled receipt, then enter the existing trade conversation to observe native stock (no forced purchase). Save/reload through native controls, reopen the receipt, and verify a second delivery is unavailable. Keep test-only stock additions and direct quest calls out of this route.

The longer iron expedition's combat balance was not established by the previous F12-assisted regional journey; record protection settings explicitly rather than claiming a new vulnerable full route until observed. Habitat preserved/disturbed pairs can be verified with real native dispatcher integration tests first; a separate Sumphold native route is useful only if the harness actually completes it, not mandatory fabricated evidence.

## Release boundary

Done means existing situations tell the truth about acceptance, release and completion; actual transactional results survive normal saves; and native UI evidence shows the result. It does not mean renewable jobs, new faction reputation, time simulation, service unlocks, habitat regeneration, a global refusal ledger, or regional economic balance. The proposal intentionally improves the first five situations before multiplying them.

## R2 implementation log — 18 September 2026

R1 is accepted on main at `519518b7`. Verification sweep confirms the existing per-instance note key is already captured by the transaction undo before dispatch; no new serialized fields or migration are needed. Delivery computes the exact preservation bonus before transfer, and that result can be passed directly to receipt formatting. Completed journal reads already consume stored strings, so no live-world query or new UI is needed. Classification: CoO-original usability repair; no Qud parity or cosmic closure claim.

**R109 RED:** 13 actual-native-path cases compiled cleanly; 11 failed on misleading completed/released instructions or missing historical outcome, and two existing rollback controls already passed. All five shipped bindings are represented, including preserved/not-preserved Sumphold and no-habitat Wellmeet. This proves a presentation defect while preserving authoritative payment/cargo logic.

Minimum implementation adds a historical completion writer from the computed paid amount and optional habitat result, and a separate released note. It reuses the exact transaction-owned note key. No source scan occurs for released/completed receipts. Native acceptance is extended with a real trade view and a reopened, rendered receipt after F5/F6. Focused verification follows before publication.

R110 focused gate: **88/88 pass**, zero compiler errors, including all13new receipt cases and the existing regional ownership/cargo/habitat/rollback/presentation fixtures. The hypotheses and control map are preserved in `Verification/RegionalOutcomes/coverage-review.md`.

Cold-eye caught a native acceptance weakness before launch: searching the full journal line buffer could claim a receipt was visible on a different page. The scenario now reconstructs actual rendered Tilemap glyphs, turns pages through native input, requires payment/stock/settled text on the captured page, and repeats after F5/F6. The trade assertion explicitly proves native UI stock-list membership; the capture still requires visual inspection. Production receipt/transaction logic had no material finding in independent review.

The central change is `RecordCompletion(actor, definition, expectedInstance, recipient, factory, RewardDrams + preservation, habitatResult)`, invoked inside the existing transaction before deferred currency commit. Undo restores the previous note if commit fails. No source, merchant inventory or habitat is consulted when the journal later reads the stored receipt. Formatting allocates only on interaction; no new per-frame path, source generation, save field or economy behavior.

R111 native acceptance: **62/62**, 918 queued native steps, nine walked borders, nineteen actual1920×1080 captures, zero compiler or unexpected errors; source freeze and complete private-state cleanup pass. Run `ab459547131d466f8baf556ee9398bf7`. Root inspected completed notes, the actual trade view (delivered choir-iron visible on the seventh stock row), and the reopened receipt after F5/F6. The captured receipt says delivered1choir-iron, paid8drams plus fireclay, historical stock and no second payment. The source stays mined, completion/stock/money/notes survive reload, and another delivery command is absent. Vulnerable opening again ends31HP; later regional verification protection remains explicitly separate and is restored.

Independent native review exposed an unrelated existing trade presentation issue: the bottom of the trade screen leaks terrain glyphs through empty rows. The delivered goods and prices remain readable. This is tracked as the next small UI repair rather than hidden by accepting the receipt. R2 changes no trade rendering behavior; its journal screenshots are clean. R112 full regression is running before publication.

Honesty bounds: these tests establish native outcomes, authority, rollback, persistence and actual UI glyphs/captures for the seeded route. They do not establish long-term balance, every route/monitor, permanent ecological change, renewable work, universal service availability or a cosmic obligation ledger. Existing saved text is retained; no legacy note migration is promised.

## R2 close-out

**R112 full:14,931/14,931 pass**, zero failed/skipped/inconclusive, zero compiler errors. This includes the complete current suite, the13new outcome cases and17R1benchmark contracts. Final R111 native62/62 covers the exact published source. Seven explicitly enumerated code/test/tool files were hash-checked against main and the tested candidate before publication; clone-only launch helpers and private project settings were not copied.

Self-review: 🟡 false completed/released instructions and missing durable habitat outcome fixed; 🟡 offscreen native journal assertion replaced with actual rendered glyph checks; 🔵 transaction/quantity/recipient-stock logic preserved; ⚪ old saved prose intentionally remains unchanged, per the user's no-migration preference; 🧪 gameplay balance and the later F12-protected regional route remain outside ordinary-opening evidence. All13new cases classify as11confirmed receipt defects and2already-correct rollback controls. The35-case existing adversarial sweep is green; the ten-hypothesis review maps its authority/cargo/overflow/death/instance cases and new receipt controls explicitly. Independent production review found no remaining material defect.

Next: repair the observed full-screen trade/faction layer leak, then explicit opt-in regional offers from [RELEASE-REGIONAL-OPT-IN](RELEASE-REGIONAL-OPT-IN.md). Neither is claimed implemented by this commit. No additional requests, rewards, factions, migrations or economy simulation were added by R2.
