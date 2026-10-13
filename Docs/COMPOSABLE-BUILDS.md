# Composable builds implementation

Status: milestones 1–6 implemented and reviewed; candidate decision and authored encounters next, source baseline `244177619cffae76fb452cce40e3c49cc5dfcb2f` (Unity 6000.3.4f1). Authoritative design: [supplied specification](COMPOSABLE-BUILDS-SPEC.md). User authorized milestone implementation, review and fixes. User explicitly waived legacy-save compatibility on 2026-10-12: version 8 will reject old saves; no migration/legacy fixtures. Current saves must round-trip and invalid loads must fail safely. Existing player files remain untouched by isolated tests.

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

### Milestone 4 verification and decisions (before implementation)
- Riposte issues one selected current long-blade strike, never the outer multiweapon action. Valid primary blade wins, then stable attached-body traversal; this query remains separate from attack scratch.
- Typed provenance includes action/root/parent/strike IDs, origin, depth, inherited reactive ancestry and follow-up suppression. Riposte descendants suppress extra attacks before RNG. Existing Rejoinder/Backswing behavior is retained absent Riposte; tagged Rejoinder ancestry cannot become eligible for Riposte by passing through Backswing.
- Actor-owned allowance survives skill removal/reacquisition, equipment changes, current-save load and travel. Failed chance/cancelled pre-issue leaves allowance; issued miss/armor stop/parry spends it. Reset only at a genuine scheduled owner completion, after EndTurn callbacks and immediately before energy payment; transient reentrancy guard prevents a callback from renewing it twice.
- Subsequent strikes in body/flurry/whirlwind sequences must recheck current actor/target/action/equipment/contact after an earlier counter. Existing standalone guaranteed damage keeps its exclusion while parent bonuses carry their parent provenance.


### Milestone 4: bounded counter reactions
- Added authored Riposte ranks I–III (45/65/80 conditional chance) and actor-owned saved allowance. One selected primary eligible blade, then stable body order; never a full multiweapon action. Failed probability leaves allowance, issued miss/parry/armor-zero spends it. Skill removal/reacquisition does not refill it.
- Added typed action/root/parent/strike IDs, origin, depth, reactive ancestry and follow-up suppression to damage, skill events and results. Paid multi-strikes share action identity. Riposte descendants cannot trigger Rejoinder/Backswing or another Riposte; ordinary Rejoinder/Backswing retains its legacy overlap and probability, with current actor/contact checks before issuance.
- TurnManager resets only genuine registered current-owner completions with scheduled energy, after EndTurn callbacks and before energy spending; reentrant calls return without repeating events. Save/autosave requests refuse unresolved strike/action scopes. Current-save round trips retain spent/unspent state without replaying acquisition.
- Subsequent body, Flurry and Whirlwind strikes revalidate current participants and original equipment. Natural/default weapons retain their separate ownership contract.
- RED: [missing counter API](Verification/ComposableBuilds/riposte-red.json). Cold review RED `329fde0e926049f4afc57ef562a54b49`: legacy Rejoinder issued attacks against dead/remote opponents or while stunned; three negative controls failed ([receipt](Verification/ComposableBuilds/riposte-review-red.json)). Fixed.
- GREEN: **237/237 native Unity EditMode, zero skipped**, job `66f8f90c12684eb19af2a33724b81c40` ([receipt](Verification/ComposableBuilds/riposte-green.json)). Includes 40 new counter cases, blade tests and neighboring multiweapon, active skills, event dispatch, elemental miss retorts and turn scheduler tests.
- 🟡 Fixed review: StrikeStarted now enters its own diagnostic cause scope before emission; parent/counter IDs and cause fields are asserted.
- 🟡 Fixed review: pending legacy Rejoinder after Backswing/Riposte now revalidates actors and contact; Backswing uses the same pre-issuance rule.
- 🧪 Discarded stale run `8e017dff8fd24e04bc355a762d53431c`: a test-only Newtonsoft reference failed compilation, but Unity ran previous assemblies. Removed that dependency, refreshed, confirmed clear console, and obtained the final 237-case run above. No stale result is acceptance evidence.
- Existing controlled native bench's guard case was updated from raw damage to actual strikes, and two cases added for rank boundaries and saved counter allowance/diagnostic lineage.
- **Native Play: complete**, run `673fd8e95be24ed6bc073c0219cba8af`, **11 driver checks; combat 17/17, preparation 18/18, exploration 21/21; zero errors/failures**, [raw report](Verification/ComposableBuilds/combat-native.json). Actual bootstrap/native input and detached controlled benches ran; caller graph/save isolation restored and Play stopped.
- Can verify: runtime parry boundaries, one counter over repeated parries, typed causal records and current-save spent allowance in an isolated game session. Cannot verify here: ordinary acquisition of these skills, their rendered combat feel, human enjoyment, or physical Deck behavior. The combat cases use explicit controlled grants/rolls; this is not a natural expedition.
- Prototype one-counter limit remains fixed in production; capped/uncapped comparison is still M6 work, not a balance conclusion.

Files: new MeleeStrikeProvenance, RiposteAllowancePart, LongBlades_Riposte and two counter test fixtures/meta; combat/skill context and caller plumbing, TurnManager, save request guard, LongBlades data, expanded native combat bench; this doc and receipts.

### Milestone 5 verification and decisions (before implementation)
- The Field Manual already separates owned/upgradable state and snapshots reviewed rank/cost. Add live defense/allowance summaries, plus a visible nonblocking Riposte source warning in both details and review. Source warning must not fabricate a mandatory Deflect prerequisite.
- Existing controller audit starts with an explicitly granted 10-SP UI-only budget, buys Acrobatics/Vault for two, then can exercise the exact eight-SP sword package with the remaining eight. Preserve its cancel-first and no-turn-cost controls, existing slot assignment, and state-driven rendering.
- Validate at 1280x800 and 1280x720 through the existing isolated native launcher; capture actual rank/review/overview states. These are injected controller events, not physical Steam Deck tests or earned progression.


### Milestone 5: readable ranks and live defense
- Riposte acquisition and review warn when neither Deflect nor En Garde is known, without making either a prerequisite. The review still defaults to Cancel. Upgrade changes the existing part; active hotbar assignment remains separate.
- Details prioritize current/next rank values, current held-blade eligibility, guard state and available/spent counter allowance. Reading these never renews the allowance.
- RED: missing presentation API (`presentation-red.json`); review RED `ac71927a905f45a7a6bea1a7afe73b28` exposed a purchase price shown at maximum rank. Price hidden at cap, with an available-rank counter-control.
- GREEN: **65/65 native Unity EditMode**, zero skipped, `681dbf8e4d9e4232ad513a7b044b1f8b`, [receipt](Verification/ComposableBuilds/presentation-green.json). Two initial jobs timed out before starting tests and are not evidence.
- Native controller + keyboard: **23/23 checks at each viewport**, zero errors/failures. 1280x800 run `c519ec27991844d788b9a54a058de322`; 1280x720 run `37a0bdb5c56041198b54b46594e9649c`. [800p](Verification/ComposableBuilds/presentation-native-800.json), [720p](Verification/ComposableBuilds/presentation-native-720.json). Reviewed warning, current/next, maximum-rank, upgrade-review and overview pixels. Controls/text fit; no overlapping or clipped essential text observed.
- Native scenario buys the eight-SP full blade package after the existing two-SP Acrobatics/Vault UI exercise; it verifies exactly one part at maximum rank and unchanged Vault slot, position, turn/energy. Ten SP is an explicit fixture grant, not opening progression evidence.
- 🔵 Fixed cold-review evidence mismatch: native maximum-rank check now counts the authoritative skill instances instead of inferring uniqueness from rank text.
- Can verify real rendered UI, synthetic controller navigation/cancel/confirm and keyboard return at both supported small viewports. Cannot verify physical Deck buttons, novice discovery or build balance here.

Files: AbilityDetailsBuilder, SkillBrowserModel, SkillsScreenUI.View, two ranked skill descriptions; native skill driver and 720p menu; new ComposableBuildPresentationTests/meta; this doc and receipts. CoO-original presentation; no new Qud-parity claim.


### Milestone 6 verification and pre-registered comparison plan
- Read the actual scheduler, skill routing, movement, tonic, physical manual, commitment, factory and native benchmark seams. Existing combat witness deliberately boosts HP/accuracy; it is unsuitable for balance comparisons. Create a separate isolated laboratory with HP40, ordinary attributes18, Speed100, LongSword/Buckler/LeatherArmor, two real HealingTonics, six SP and no unrelated skills.
- Four exact six-SP spec builds: Counterfighter, Hybrid, Rank-heavy and existing Control. Buy through normal transactions. Vault arms read a physically supplied DitchkeepersFootwork; it never grants a free Acrobatics root. This is fixture acquisition, not natural discovery.
- Pre-register 32 seeds for four builds × three encounters × Vault/no-Vault (768 trials): open actual Shambler with safe walking; held doorway with two advancing Scrabblers; caster/Scrabbler pair and real opaque cover. Cap pilot: three blade builds × cap/uncapped × Speed50/100/200 ×32 on the crowd fixture (576 trials). Same starting seeds, divergent combat draws allowed; no favorable rerolls.
- Shared finite policy prioritizes healing at HP14, retreat at HP8 after supplies, visible commitment avoidance, situational owned skills, then ordinary melee/approach. Held-door policy explicitly values remaining on its tile. Limit60 paid choices and120 scheduler cycles; report timeout separately from victory/death/withdrawal. Record policy branches, HP/damage/consumables, elapsed turns, parries, counters and displacements.
- Actual NPC AI/scheduler executes between paid actions. Player gets an inert Brain for the scheduler's CurrentZone lookup. Material and coating decay mirror InputHandler after the scheduler. Never synthesize owner EndTurn events or force misses/hits.
- Cap experiment uses an internal, nonserialized actor-local flag that ignores only the spent gate; default production remains capped, reservation/ancestry/same-weapon safety remains. Explicit tests cover default, uncapped, recursion and current-save exclusion.
- Performance: no new production frame loop or cache. The finite laboratory may allocate report rows; batches are outside ordinary gameplay. Reuse existing detached scope to restore active scheduler, visual hooks and message/FX state. Diagnostics are read incrementally without clearing shared history; detect ring overflow. Results characterize this policy/sample, not optimal players or enjoyment.


### Milestone 6: equal-budget controlled comparison
- Built four real six-SP purchased loadouts from the authored Player (including Flesh/Thermal), common ordinary equipment, finite tonics and an optional physically read Vault manual. Forty-two arms, seeds 1–32: **1,344 trials**, no rerolls or forced combat results. Cap experiment changes only an actor-local, nonserialized spent-gate override.
- RED evidence: missing comparison API; isolated-world reputation leak; spell visual-wait handling; missing committed-damage metric; missing actual Player material and unreachable Tumble branch; invalid arguments. Receipts named `comparison-*-red.json` in Verification/ComposableBuilds.
- GREEN: **141/141 native Unity EditMode**, zero skipped, job `df8c222205334467ba0d3a0b1f6348f3` ([receipt](Verification/ComposableBuilds/comparison-green.json)). Includes 45 new comparison/adversarial cases, the full pilot and neighboring defense/counter/native-bench checks.
- Native Play: run `801a4e5dee414fa79b3d52e1d4c598c2`, **13/13 driver checks**, combat17/preparation18/exploration21/comparison42 all pass, zero errors/failures ([raw native](Verification/ComposableBuilds/comparison-native.json)). Full accepted pilot `55589ee1c0c446149b57b927beb975b8` retained with all trials and summaries ([raw pilot](Verification/ComposableBuilds/comparison-pilot.json)). Play stopped, SampleScene and caller state restored.
- Policy witnesses across all arms: walk off ray213, Jet Blast524, Tumble64, Undertow10, Vault attack165/retreat3, tonic473. Geometry was revised before acceptance because the original mixed-lane placement never admitted Tumble. Every build faces that same revised geometry; earlier zero-Tumble reports are not acceptance evidence.
- 🟡 Fixed review: detached trials now restore reputation; spell presentation waits are drained before one paid turn; damage counts committed HP loss rather than overkill; actual Player material restores fire behavior. Factory/RNG, message/FX, scheduler and diagnostics settings are restored. Production audio rejects detached zones synchronously, with no global queue.
- 🟡 Fixed adversarial: invalid speed/profile refuses before creating entities; override cannot defeat reservation or reactive ancestry and does not persist. Counters per eligible proc, per full parry and parries per defense draw have explicit denominators.
- Can verify deterministic execution, finite item use, real scheduler/skill/AI outcomes and cap differences under this fixed policy. Cannot verify optimal strategy, natural acquisition, novelty, enjoyment, or physical Deck behavior. Wilson/mean intervals describe this small pilot; repeated opportunities within a trial are not independent balance evidence. Open-commitment victories with zero damage show ordinary avoidance remains useful, not that every encounter is balanced.

Files: new ComposableBuildComparisonBench/Policy and two comparison test fixtures/meta; RiposteAllowancePart/LongBlades_Riposte test-only override; additive CombatSystem ActualDamage diagnostic; FiftySecondCombatBench isolation and native driver integration; this doc and receipts. CoO-original experiment, no Qud parity claim.
