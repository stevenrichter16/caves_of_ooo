# Composable builds implementation

Status: milestones 1–3 implemented and reviewed; milestone 4 next, source baseline `244177619cffae76fb452cce40e3c49cc5dfcb2f` (Unity 6000.3.4f1). Authoritative design: [supplied specification](COMPOSABLE-BUILDS-SPEC.md). User authorized milestone implementation, review and fixes. User explicitly waived legacy-save compatibility on 2026-10-12: version 8 will reject old saves; no migration/legacy fixtures. Current saves must round-trip and invalid loads must fail safely. Existing player files remain untouched by isolated tests.

## Ordered work
1. Baseline/contracts and failing tests.
2. One-part ranks, atomic upgrades, current saves and browser.
3. Single would-hit defense and guarded fallback.
4. Source-tagged Riposte and durable per-owner allowance.
5. Player descriptions, feedback and native UI.
6. Equal-budget controlled comparisons.
7. Existing displacement comparison; candidate moves only if distinct value proven.
8. Source-bound authored encounter roles.
9. Optional starter/weapon access and ordinary opening journeys.
10. Exact-revision regression and balance/release evidence.

Every milestone needs a red-before-green test, counter-checks, cold review, fixes and evidence here. Human blind-player sessions and physical Deck verification cannot be substituted with agent simulation. Proposed balance values remain prototype values. No new build distribution is implied by this task; prior authorization for main commits/pushes remains.

## Verification sweep (before edits)
| Source | Finding / decision |
|---|---|
| SaveSystem | Strict v7 header, named public fields cannot skip unknown values. Use strict v8 per user waiver. Validate malformed ranks during graph decode before global activation, not post-load hooks. |
| SkillsPart | One instance per class already authoritative, but public SkillList cache is redundantly serialized. Mark cache NonSerialized in v8; reconstruct from Parts. Grants remain rank I and duplicates remain no-ops. |
| SkillPurchaseEligibility / BuySkillAction | Ownership currently blocks all purchases. Separate owned rank from next-rank eligibility; use existing authority for both acquisition and upgrade. |
| Field Manual | Ownership inferred from purchase failure in several paths. Keep owned state independent; preserve hidden/book powers and assignment, add expected-rank/cost review check. |
| Spec | Legacy requirements superseded by user; provisional percentages and candidate movement are not measured balance. |

## Design decisions
Ranks live on the sole BaseSkillPart. Definitions carry maximum rank (default one) and optional per-rank costs; existing unranked content stays one purchase. Rank upgrades do not run acquisition callbacks, allocate another part/ability, change cooldown or reset reaction state. Purchases recheck current rank, cost, SP and requirements. Source grant/read cannot upgrade. Save v8 validates ranks before publication. Encounter generation and player balance stay unchanged during infrastructure work.

## Milestone evidence / self-review
Existing unrelated changes: UnityMCP logs and Packages manifest/lock, excluded from feature commits.

### Milestones 1–2: ranks and transactions
- RED: initial compile errors for missing rank API (ranks-red.json). 10 initial checks passed. Cold review added removal RED `746abf687fef4672a03a83b8da6e58f6` and acquisition/metadata RED `0375c20eea574988af2618b6671151cd` (both new cases failed).
- GREEN: **172/172 native Unity EditMode**, no skipped tests; `9d9e5a084e674350bed6b45566c141a8`, [receipt](Verification/ComposableBuilds/ranks-green.json). Includes 13 new rank cases and neighboring registry, ownership, purchase, full-screen browser/input, current-save decode, hotbar and ability fixtures.
- 🟡 Fixed: acquisition-only rank validation leaked into removal. Ranked removal now tears down the one part/ability while retaining unrelated ownership.
- 🟡 Fixed: setup exceptions could spend SP and retain a partial skill. Purchase refunds; failed attachment cleanup removes tracked shifts, ability and skill/cache entry.
- 🟡 Fixed: invalid rank metadata masqueraded as discovery-only skills. Registry warns and skips invalid maxima/cost arrays; live mutation safely refuses purchases.
- 🟡 Fixed by design: rank validation runs in LoadPart before session activation; SkillList no longer redundantly persists.
- Existing hard-coded v7 hotbar test pin updated to current format; its nonzero-slot/cooldown assertions retained. Strict old-save rejection is the user-directed scope change.
- Current rank fixture temporarily declares Vault ranked in an isolated registry only to prove active GUID/slot/cooldown preservation. Production Vault stays unranked. No Deflect/Riposte runtime content added in this milestone.
- Native pixels for actual new ranked powers will be checked in milestone 5. These unit/UI tests are not natural-acquisition or fun evidence.

Files: BaseSkillPart, SkillsPart, SkillData, PowerData, new SkillRanks, SkillRegistry, SkillPurchaseEligibility, BuySkillAction, SaveSystem; SkillsScreenSnapshot/StateBuilder, SkillBrowserModel, AbilityDetailsBuilder, SkillsScreenUI.View; new ComposableRankTests and adjusted GameAuditHotbarSelectionTests; this living doc, supplied spec and rank verification receipts.

### Milestone 3 verification and decisions (before implementation)
- Would-hit seam is after normal accuracy miss dispatch and before hit location/penetration. A parry gets a separate structured outcome and never emits miss hooks.
- Conk and HookAndDrag append effects outside CombatSystem; gate on actual outcome. ChargingStrike/Backstab bonuses must use the parent strike damage, not total HP loss spanning recursive reactions. Follow Through must require its own resolved lethal strike.
- Retire EnGarde's broad damage hook. Per spec, standalone guaranteed Slam/GroundPound and raw Melee-tagged damage are outside parry/guard; this intentionally narrows the old guard semantics. Failed guard halves aggregate strike damage before normal mutation/resistance.
- Perception uses current body-aware AI sight/LOS/radius, independent of player fog. No existing blindness/invisibility effect was found; zero sight radius represents inability to perceive. Adjacent contact in darkness/mist endpoints follows the existing AI rule and remains perceivable; do not invent a player-camera exception.
- New defense weapon query must use exact LongBlades token, attached usable Hand grips, required slot count, equipment owner and live item state. It must not reuse the outer attack's shared scratch list. Frozen/stunned/paralyzed block reactions through the existing read-only IsActionBlocked; Rooted does not.
- Deflect uses one roll per eligible otherwise-hit strike: 15/22/28; guard adds30, alone30. Natural20 can be parried. Neither passive nor guard means no defense draw. Guard consumes before rolling even if later armor prevents damage. Lifetime retains existing owner EndTurn/JustApplied behavior.


### Milestone 3: single defense resolution
- Implemented ranked Deflect and authored its three one-SP ranks. Would-hit gate precedes hit location, penetration, critical/contact effects. Guard alone works and consumes before resolution; fallback halves aggregate damage before mutation/resistance.
- Equipment uses attached supported Hand grips, exact token, effective required grip count and owner/location/destruction checks. Broken gear retains its existing penalty-only behavior. Normal body traversal and explicit slot parsing add no scratch collection allocations.
- Structured results distinguish natural miss, parry, armor stop and hit. Committed parent HP damage is captured at the actual damage write, excluding nested damage; an attacker killed by reflection still reports its landed damage.
- Conk/Hook cannot bypass a full parry, but retain natural-miss contact riders. ChargingStrike/Backstab use parent damage for bonuses and do not issue bonuses after attacker death/removal. Follow Through recognizes the landed strike's own contact-effect kill; full parry/miss cannot manufacture an advance.
- RED: [missing API](Verification/ComposableBuilds/blade-red.json). Cold-review RED: Follow Through failed to advance after Charsplit kill, job `feb5ff43494247e09d901f3491356ee9` ([receipt](Verification/ComposableBuilds/blade-review-red.json)). Fixed before commit.
- GREEN: **202/202 native Unity EditMode, zero skipped**, job `daa2ef8cd02642cf8795491a1489a544` ([receipt](Verification/ComposableBuilds/blade-green.json)). Includes 55 new defense/adversarial cases plus neighboring damage, contact-proc and combat-skill regressions. Two old guard pins updated for the explicit standalone-damage exclusion.
- 🟡 Cold-review fix: initial kill receipt excluded lethal Charsplit contact damage. Added positive/contact-kill and full-parry counter-control.
- 🟡 Review coverage fix: actual Cudgel critical rider and explicit miss-dispatch probes now have paired controls; an empty effects list alone was insufficient evidence.
- ⚪ Intentional divergence: old broad raw-Melee guard removed per supplied design. This is a CoO-original defense composition, not a claim of exact Qud implementation parity.
- 🧪 Native rendered combat/Field Manual and performance evidence remain milestone 5/10 gates; passing isolated tests is not visual, balance, human-player or Deck verification. No release build produced.

Files: new BladeDefense, MeleeStrikeResult, LongBlades_Deflect and two defense test fixtures (with meta); CombatSystem, EnGardeEffect/description, EnGarde/Conk/Hook/ChargingStrike/Backstab/FollowThrough, LongBlades content; two existing guard pins, this doc and receipts.
