# G2 action readability and framing

Status: observation and acceptance in progress, 2026-10-24 UTC, after
`ee3c90abe`. This closes evidence gaps before designing any replacement art.
Root owns Unity. No new animation or gameplay change is assumed necessary.

## Plan and source sweep

Reuse ordinary SampleScene/GameBootstrap seed64, isolated saves and absent
presentation preference. Preserve current approved16×24 sprites and timing.
Walk into an actual encounter, perform ordinary bump attacks, observe damage,
retaliation/defence when they occur, and inspect menu/target return. Capture
multiple distinct rendered frames with matching owner/pose receipts, not
multiple Screenshot calls in one frame. Review at actual1280×800 and current
integer pixel framing. Only a named observed defect warrants changed pixels or
runtime code; such a change requires RED first, independent review and the
normal capture→imagegen→native authoring loop where artwork changes.

| Assumption | Source correction and decision |
| --- | --- |
| New motion is missing | Existing FlatPixelActorMotionCatalog and AnimatedEntityRenderer.FlatPixel already provide two poses sharing100ms of walking,160ms melee and140ms hurt with one bounded pending reaction. Close ordinary evidence first. |
| Prior hurt screenshot proves hurt | LIVE-16X24-MOTION explicitly says it still showed Attack, and only the last same-frame request was saved. Observe rendered frames separately. |
| Four directions are authored | Canonical East with West mirror; North/South retain last horizontal facing. Record this deliberate limit; do not label full directional art. |
| Capturing every frame is free | PNG readback/encoding can perturb real-time motion. Bound captures, record monotonic frame/time/state and keep capture cost out of performance claims. |
| A scripted pose is gameplay | Only ordinary user input may trigger actions. Observation reads existing views/events and saves screenshots; no hook emission, health/equipment injection, duration changes or artificial poses. |
| Native tests prove camera/feel | Existing tests establish projection/lifetime/queue contracts. Real appearance, actionable contrast and screen composition need live inspection. Physical Deck remains separate. |

## Acceptance steps

1. Actual normal launch; capture start and inspect current view/nearby threats.
2. Arm bounded read-only motion observation; ordinary movement captures walkA/B
   and idle recovery. One capture per rendered frame; no raw image editing.
3. Reach a real hostile, record actual melee/contact/hurt/retaliation and the
   observed event/log. Preserve failed/missed actions honestly.
4. Check Look/selection, inventory return and available framing control. Save
   exact receipts and identify the three largest actual defects, if any.
5. Fix significant verified issues using scoped RED/GREEN; do not invent code
   or art changes to increase activity. Independent visual/code review.
6. Stop/restore isolated session, record limits, update coverage and push owned
   changes after fetch/rebase. Continue six-zone opening acceptance.

## Current evidence and limits

Ordinary seed64 movement now captured both Player walk poses and genuine NPC
attack/hurt/recovery. A second ordinary approach/bump sequence captured Player
attack and Gleaner hurt. Existing435/629 motion regressions are historical
evidence, not rerun here. This is original CoO presentation; no Qud-engine parity
claim. The accepted frames preserve existing art; no sprite revision is needed.

## Observed defect and repair plan (before production changes)

Independent review of `G2ActionWalk/frame-02.png` found neighbouring damage
values1 and2 concatenate into a misleading red12. The actual combat log records
separate hits. This is a significant combat-readability defect, not a damage
calculation problem or a reason to redesign approved sprites.

| Source checked | Correction / implementation decision |
| --- | --- |
| CombatSystem.ApplyDamage | Already clamps actual damage and emits once before death handling. Preserve that authority; provide exact target and lethal receipt to presentation. |
| AsciiFxBus.EmitFloatingNumber | Splits each number into unrelated digit particles, losing group and owner. Replace only numeric emission with one whole immutable value; retain generic decorative particles. |
| AsciiFxRenderer.AcceptEventText | Existing lanes only avoid the same owner; adjacent words/numbers can overwrite or concatenate. Share bounded reservations across semantic readouts. |
| EventText duplicate suppression | Correct for repeated status labels, incorrect for two real1-damage hits. Numeric receipts never coalesce. |
| Queue admission after death | Exact target may already be removed. Explicit lethal-at-emission receipt may finish at its visible captured origin; a removed living target must not. |
| WorldFxCoordinator.IsStateOrReadout | FX Off must retain grouped damage, just as it retains status words. Keep nonblocking wall-clock readability. |
| Legacy particle-count tests | Representation pins will intentionally change; retain actual damage, resistance, overkill, trap and native font contracts. |

Chosen bound: maximum32 active groups, three nearby lanes spaced two rows, one
blank glyph column between separate values/words sharing a row. Keep valid
existing lanes stable, recheck movement/rise, clamp entire strings to world
bounds and preserve source column. If no truthful nearby placement exists,
omit that transient readout; the log remains authoritative. No horizontal
shuffling over other actors, general label engine or new damage aggregation.

Both captured origin and current exact living owner must remain disclosed even
with reveal/clip settings changed. A finite lethal receipt must never reattach
to a same-ID replacement. Queue pool/reset and lifecycle cleanup retain no stale
owner. Root verifies real ApplyDamage→coordinator→submitted glyph RED before
production edits; contiguous true12 is the paired counter. Then test adjacent,
repeated, multi-digit, mixed status, fog, death, boundaries, lifecycle and FX
modes. Revisit the same ordinary encounter for1280×800 before/after evidence.

No new art is proposed for this fix, so the existing native font is reused;
imagegen reinterpretation is reserved for a named artwork defect. The observed
walk/attack/hurt art remains unchanged.

Independent17-frame review additionally found `Stunned` centered over the
Gleaner(39,20) crosses the Player(40,19). Before implementing this additional
branch, add a real submitted-word RED: a disclosed neighbouring actor must not
be painted over; a hidden actor must not influence placement. Reject candidate
rows occupied by a currently disclosed Creature/Player within the string's
width and choose another of the same three lanes. This is a bounded local
candidate check, not a zone scan, a new screen-space UI or hidden-entity probe.
The native actor sheet occupies one logical cell; smooth intermediate movement
and arbitrary larger legacy models are not claimed pixel-perfect occlusion.

## Ordinary observation receipts

Isolation `G2ActionLaunchAudit.` token `7c49ecf788964517b18ad7c91418b01e`,
ordinary SampleScene seed64, absent presentation preference, no forced art
mode. N started at(40,12),40HP,tick10. Keyboard Down reached(40,14),tick30;
subsequent ordinary Down reached(40,19),tick80. Southwest bump (`b`) attacked
the actual Gleaner at(39,20) at ticks90/100. No health, equipment, action,
clock, RNG, event or world-state injection.

- `G2ActionWalk/frame-00..05.png/json`: six upright1280×800 end-of-frame
  captures, Player and supported nearby walkers A/B; Scrabbler attack with
  pending hurt → hurt → idle artwork. Frame05 still has state Hurt during its
  idle-art tail; it is not a full Idle-state receipt.
- `G2Action-20261024-034900-041/frame-00..16.png/json`:17 ordinary movement/
  combat samples. `g2-action-combat-events.json` contains13 actual additive
  attack/damage observations, including the Player's3- and2-damage bumps and
  intervening NPC combat. Other targets died; the Player stayed40HP.
- `g2-action-combat-idle.json` separately confirms Player/Gleaner/Dog enum
  Idle after recovery. `g2-action-combat-rest.png` is the resting Game view.
- `g2-action-launch-cleanup.json` confirms Play stopped, original scene/
  settings restored, no borrowed view, save root or pending isolation.

The earlier endFrameRendering/AsyncGPUReadback prototype produced inverted
raw frames and was rejected, retained only under `/tmp/coo-g2-action-prototype-frames`.
It is not gameplay acceptance and was not edited into acceptable evidence.
The saved helper now uses WaitForEndOfFrame and untouched Screenshot texture
pixels, deferred PNG encoding and per-session paths. Selective pose sampling
and synchronous readback perturb pacing: these are neither continuous video
nor performance measurements. Existing art was not modified to match capture
artifacts.

Can verify: ordinary movement and real combat changed exact current owners,
their genuine native pose sprites and HP; the documented frames exist at the
actual Game target. Cannot verify from these samples: Player hurt/parry/guard,
audio synchrony, capture-free motion feel, all directional poses, all seed or
background combinations, packaged release or physical Deck input/performance.
Those remain separate gates, as do full scene framing and the six-zone route.

## Native verification and review

- Initial semantic RED:2 cases,1 passed genuine12 control; adjacent1/2 failed
  the submitted-glyph separation assertion. A first premature runner launch
  before assembly reload returned0 cases and was rejected, never counted.
- After grouped numbers:35 cases,34 passed; disclosed actor obstruction failed.
  Both hidden-neighbour placement controls passed before the actor guard.
- First broader run:247 cases,245 passed. Two old partial-resistance assertions
  still counted digit particles. Corrected those representation pins while
  retaining actual HP/resistance checks; the same existing deterministic melee
  fixture now also proves one9-damage receipt for a real hit and zero on a miss.
- Final native Unity run: **253/253 passed,0 failed,0 skipped**,
  `g2-action-readout-green.xml`,04:14:22–04:15:53 UTC. Includes42 new readout
  cases and fourteen fixtures covering combat, traps, event/status text,
  coordinator modes/lifecycle, native font/decorative FX and allocation.
- Six post-review hypotheses passed without additional production fixes:
  delayed cancellation, replaced captured cell, lethal same-ID replacement,
  foreign/carried custody, moving diagonal-owner collisions and the fourth-hit
  local lane cap. These are confirmed existing protections, not six new bugs.
- Independent final source review found no P1/P2. Saved reviews:
  `g2-action-walk-visual-review.md`, `g2-action-combat-visual-review.md` and
  `g2-action-readout-cold-review.md`. Actual post-fix appearance remains a
  separate gate; tests alone do not establish it.

### Environment recovery during the post-fix capture

At roughly04:18 UTC, Unity code execution explicitly failed with `Disk full`
and native screenshot creation failed. Stopped and restored the isolated
launch (`g2-action-fixed-aborted-launch-cleanup.json`) before restarting the
clean editor. Removed only generated `Library/BurstCache`/`Library/Bee` and two
superseded October10 non-current Steam Deck archives whose source commits are
ancestors of main and whose saved checksums matched. Kept all `-current`
builds, audit/checksum receipts, source, saves and editable art. Exact deletion
receipt: `g2-action-disk-recovery.json` (534,528,075 logical bytes); this is not
a claim of permanent equivalent free space because Unity regenerates caches.
Storage remained tight (about307 MiB at04:22); no large build/download is
appropriate until more headroom is available. This is an environment failure,
not failed gameplay acceptance or permission to invent an after screenshot.

After restart, the editor process waits in `NSAlert.runModal` from a native
decision dialog. CUA reports `noWindowsAvailable` for Unity, and MCP pings do
not complete. A bounded retry through Hub and front-window inspection did not
expose its text. Root requested that text from the user rather than guessing a
confirmation. No audit session or gameplay is left active. The next ordinary
capture must wait for this gate to clear; it has not passed.

## Implementation, divergences and in-phase self-review

`ApplyDamage` now emits a single `FloatingNumber` with exact owner, immutable
decimal value and captured Cell. Rendering reuses the existing font/readout
path; three vertical lanes and a one-column gap preserve distinct values.
Labels avoid disclosed top Creature/Player cells. A lethal-at-emission receipt
may finish at its origin; hidden/foreign/removed living owners and stale cells
retire permanently. FX Off keeps semantics, all remain nonblocking, and pooled
requests reset their owner/origin references immediately on release.

Deliberate presentation change: complete damage values rise one row at half
of their0.6s wall-time lifetime, instead of independently hopping each digit
every0.15s. Event words retain1.1s. Overcrowding omits a whole newest transient
after the three nearby lanes are exhausted; the combat log retains every hit.
Legacy/flat readouts retain full UI glyph size. No damage, RNG, action cost,
combat skill, spawn, sprite or animation timing was changed.

- 🟡 Fixed: neighbouring hits appeared as one larger value; actual submitted
  damage RED now passes with genuine multi-digit and repeated-hit counters.
- 🟡 Fixed: enemy status word obscured Player; disclosed-body RED now passes
  alongside hidden-neighbour controls and actual full-width submission.
- 🟡 Fixed in capture helper: repeat finish overwrote its index, finishing
  before arm threw, and event exports reused one path. Four stub-harness
  checks pass; these are helper evidence, separate from native/game tests.
- 🔵 Review complete: source cold-eye found no remaining significant issue;
  six new post-review cross-system hypotheses also pass. Frame reviewers did
  not request artwork changes. Source art remains stable.
- 🧪 Deferred acceptance: post-fix ordinary screenshot, Player defence/hurt,
  uninstrumented motion, full G2 scene/journey, packaged and physical Deck.
  The present disk/dialog blocker is recorded, not counted as acceptance.

Files changed: CombatSystem, AsciiFxBus, AsciiFxRenderer, WorldFxCoordinator;
new42-case GroupedDamageReadoutTests plus seven existing numeric/status/trap
fixture contracts; bounded ActionCapture snippets/README; this living record,
motion/art/completion records, release coverage ledger and exact evidence.
Scope divergence: the observation slice found two genuine feedback defects,
so it now includes those fixes; no new sprites were warranted. Runtime fixes
are ready for final appearance verification; the opening release is unfinished.
