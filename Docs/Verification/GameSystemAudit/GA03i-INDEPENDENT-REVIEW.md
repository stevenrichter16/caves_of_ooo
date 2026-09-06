# GA03i final independent review

Reviewed 2026-09-06 by system_inventory. Repository review was read-only; this report is the only new output, under /tmp. No Unity, tests, gameplay stimuli, repository edits, staging or commits were performed by this reviewer. The root agent owns all captured executions and implementation.

**Conclusion: the final source review is complete, with 0 additional production must-fix findings in the assigned scope.** The occupied secondary-hand guard is present and addresses the independently confirmed Greatsword regression. The earlier save-flag/shared-alias and unknown-recipe findings are also repaired. This conclusion does not substitute for the final focused/full-suite and functional native rerun gates, which were pending when this review closed.

## Reviewed source and final contracts

Repository paths below are relative to `/Users/steven/caves-of-ooo`.

| Surface | Source anchor | Final contract / result |
|---|---|---|
| Exactly 24 kits | `Assets/Resources/Content/Blueprints/Objects.json` | Semantic comparison with HEAD found exactly 24 changed records. Removing each record's newly appended Loadout returns that record exactly to HEAD; no other actor/item fields changed. Effective inheritance produces exactly those 24 Loadout actors, with no unintended descendants. |
| Trader ordering | Same JSON plus `EntityFactory` resolution | All nine affected Traders precede Loadout in the effective part order: Elder 15/16, Weaponsmith 13/14, Tinker 14/15, Merchant 13/14, Quartermaster 13/14, Warden 15/16, WellKeeper 13/15, Farmer 13/16, Scribe 15/16 (zero-based Trader/Loadout indices). Current Snapjaw descendants all override the inherited Loadout, including blank Carry/Pick values. |
| Quiet creation | `LoadoutPart.cs:186`; `InventorySystem.cs:167`; `AutoEquipCommand.cs:13,114,131`; `EquipCommand.cs:83,174` | Only the loadout caller opts out of the success sentence. The choice is immutable command-local state; public AutoEquip/direct commands/pickup remain audible. Both body and legacy paths forward it. Hooks, bonus application, claims/rollback and LoadoutEquipResult remain on their existing paths. There is no global suppression state or new saved field. |
| Final natural recipe before observers | `Data/Factories/EntityFactory.cs:498–513` | Existing anatomy is constructed, actor NaturalWeapon override is applied, then RegenerateDefaultEquipment materializes the final recipes before ObjectCreated/loadout observers. Preinitialized custom bodies and no-Body cases preserve their existing early-return behavior. No test-only manual maintenance masks ordinary factory results. |
| Conservative save repair | `Gameplay/Anatomy/Body.cs:65–104` | After graph reads, attached and detached roots are rebound and only null defaults with nonempty recognized recipes are filled. Every newly created natural is unique and gets its first flag. Existing natural references and all existing flags remain untouched, including aliases across attached/detached branches. No blanket per-root recomputation. |
| Unknown recipe policy | `NaturalWeaponFactory.cs:10–18,124` | Public Create retains its historical generic 1d2 fallback, including fresh custom-authoring calls. Internal CreateKnown uses the same switch but returns null for unknown recipes during save repair. Unknown missing saved defaults/flags remain untouched. Existing non-null custom payloads survive either policy. |
| Two-handed selection | `Gameplay/Combat/CombatSystem.cs:617–646` | An equipped non-first Hand is occupied and skips natural fallback. A first equipped melee slot contributes its weapon once. A free hand still contributes its natural. A primary/first non-melee item such as Buckler still permits the prior natural fallback. The guard adds no allocation, cache or saved field. |
| Save graph and loot boundaries | `Gameplay/Save/SaveSystem.cs:1655–1659,1692–1697`; `Body.cs:841`; `NaturalWeaponFactory.cs:128` | Natural references continue through the existing entity-reference graph. Natural entities have no Physics item and are not inventory/equipment/ground loot. Personal equipment cleanup remains the normal equipped union. No save-version change. Anonymous natural IDs may be assigned on their first read; this wave guarantees graph identity/aliases, not initial anonymous-ID stability. |

## Exact implemented kits

Only listed Carry/Pick entries are nonempty. Percentages below are the existing Loadout suffix syntax, not an added parser feature.

| Actor | Equip | Carry / Pick |
|---|---|---|
| Snapjaw | Dagger;LeatherCap:20 | — |
| SnapjawScavenger | LeatherGloves:35 | Pick `1;Dagger;ShortSword` |
| SnapjawHunter | Spear;LeatherBoots:35 | — |
| SnapjawChieftain | ShortSword;LeatherArmor;LeatherCap | — |
| SnapjawWarlord | LeatherArmor;IronHelmet;IronshodBoots | — |
| DesertBandit | ShortSword;LeatherCap:30 | — |
| RuinScavenger | Dagger;LeatherGloves:35 | — |
| SkeletalSentry | IronHelmet | — |
| AmbushBandit | ShortSword;LeatherArmor:35 | — |
| RuneCultist | Dagger;LeatherGloves:35 | — |
| Warden | LongSword;LeatherArmor;LeatherBoots | — |
| Quartermaster | Spear;LeatherArmor;LeatherBoots | — |
| Weaponsmith | LeatherGloves;LeatherBoots | — |
| Tinker | Dagger;LeatherGloves | — |
| Farmer | LeatherBoots;LeatherGloves | — |
| WellKeeper | LeatherBoots;LeatherCap | — |
| PeatCutter | LeatherBoots;LeatherGloves | Carry Dagger |
| Elder | LeatherCap;LeatherBoots | — |
| Scribe | LeatherGloves;LeatherCap | — |
| Merchant | ShortSword;LeatherBoots | — |
| TentRightHost | LeatherBoots;LeatherCap | Carry Dagger |
| SaltMaster | LeatherGloves;LeatherBoots | — |
| RecensionScribe | LeatherGloves;LeatherBoots | — |
| CurationSorter | LeatherGloves;LeatherCap | — |

Successful equipped grants leave inventory Objects, so they do not count as later shelf stock or merge with subsequent Objects-only restocking. The nine maximum-stock controls are stronger than merely observing nonempty native stock. Armorer remains excluded because its existing shelf can collide with intended personal equipment before successful equip. Future Trader descendants require the same ordering/collision review.

These are deliberate gameplay changes: worn weapons replace that hand's natural attack, armor applies to the struck body location, DV applies through existing global armor behavior, and IronshodBoots add the existing five-point slowdown. Warlord and Sentry retain their stronger natural attacks by receiving armor-only kits. Natural activation also enables preexisting declared riders/free-hand attacks. Adding Loadout affects the existing derived Humanoid loot classification where no explicit LootClass overrides it. This is not balance-neutral, and no whole-game balance certification is claimed.

## Twelve attempted-break hypotheses and evidence

`NaturalAdv` below is `Assets/Tests/EditMode/Gameplay/Anatomy/GameAuditNaturalWeaponActivationAdversarialTests.cs`; `AliasAdv` is the adjacent `GameAuditNaturalAliasRepairAdversarialTests.cs`; `EquipmentAdv` is `Assets/Tests/EditMode/Gameplay/Entities/GameAuditEntityEquipmentAdversarialTests.cs`.

| # | Attempt / distinguishing control | Evidence and scoped result |
|---|---|---|
| 1 | Inherited Snapjaw Loadout runs in addition to a child's kit or leaves a parent Pick/Carry value | Static effective-inheritance sweep: exactly 24 targets. EquipmentAdv:69 pins both Scavenger choices and absence of parent Dagger/Cap. No duplicate/inherited grant found. |
| 2 | Trader stock swallows the personal grant or worn gear consumes later shelf capacity | All nine effective ordering checks above; EquipmentAdv:85 maximum opening shelf; :229 later restock; native actual stock/owner signatures. Current kits avoid collisions. Future combinations remain an authoring constraint. |
| 3 | Quiet initialization suppresses player or nested independent commands, or skips equip hooks | Command-local flag has public defaults true; only success prose is gated after bonuses and before AfterEquip. Existing initial tests cover normal/nested/pickup controls; native ordinary re-equip announces and restores slowdown. No global message suppression or hook bypass found. |
| 4 | Naturals initialize before the final actor override, too late for Loadout, or share cached mutable objects | NaturalAdv:62 sweeps all 40 resolved authored declarations; :85 checks cross-hand/actor independence; :169/:185 inspect ObjectCreated/BeforeEquip/nested creation. Factory call is after override and each recipe constructs a new Entity. No manual materialization in positive factory cases. |
| 5 | Restored naturals become carried/dropped items, or save repair regrants current personal kits | NaturalAdv:108, :251, :265 plus EquipmentAdv:180. Factory naturals have no Physics; load repair never fires ObjectCreated. Native exact personal death drops and healthy F6 recovery agree. No natural-loot or kit-regrant path introduced. |
| 6 | Loading a detached non-first alias grants it a second attack when healed | AliasAdv:26 reproduced the earlier defect; :50 attached-shared and :63 unique-detached are controls. Current Body repair preserves existing refs/flags and sets first only for newly created unique defaults. Archived alias minimum 95/95 passed. |
| 7 | Load repair rewrites unrelated serialized flag bits or fabricates an unsupported custom weapon | Existing Tier1 raw Flags 16 control and unknown missing-recipe RED exposed real defects. Current known-only null repair preserves both; NaturalAdv:304/:315/:326 distinguish empty, retained custom and explicit fresh fallback. The 95-case archive includes full Tier1BodyRoundTripTests. |
| 8 | Two-handed equipment still contributes an extra supporting-hand fist | NaturalAdv:206 Greatsword was the sole failure in 26 cases; Dagger and Buckler passed as controls. Final guard is now source-verified. Native AFTER workload additionally observes one hit roll/no offhand roll per two-handed request, while one-handed remains two/one. Final functional test rerun is root-owned/pending here. |
| 9 | Removing the extra two-hand attack also disables an ordinary free offhand or shield fallback | Guard skips only occupied non-first slots, not every occupied slot. NaturalAdv:206 Dagger/Buckler controls and exact natural references after unequip; one-handed performance counter remains two miss swings and one offhand roll per request. No overbroad occupied-slot rule found. |
| 10 | Sever/veto/regrowth recreates naturals, leaks equipment bonuses or changes unrelated live actors | NaturalAdv:222 veto and :235 real sever/regrow pin exact refs; native living veto, living nonmortal loss, unrelated survivor, repeated death and F6 controls passed. Drop/refund comes from existing equipment lifecycle, not a new natural disposal path. |
| 11 | Declared effect/gas recipes disappear or all naturals silently become plain damage | NaturalAdv:126 has six actual literal effect/parser cases; :143 SporeShambler gas; :160 plain BoneBlade control. These verify materialized configuration and parser data, not complete future proc/tick/duration semantics. Source dispatch remains connected to actual damage and existing gates. |
| 12 | Handless/no-Body/empty/preinitialized bodies throw, or repeated load invents a stronger maintenance contract | Initial handless cases plus NaturalAdv:97/:251/:294/:304 preserve defaults/fallback and repeat behavior. Factory keeps preinitialized-body return; Body BeginTurn remains empty. This wave does not promise automatic regeneration after arbitrary custom anatomy insertion or changing an already materialized recipe. |

## Qud reference angle and divergences

Read the actual local reference under `/Users/steven/qud-decompiled-project` again:

- `XRL.World.Anatomy/Anatomy.cs:31–63` completes anatomy then calls Body.UpdateBodyParts; `XRL.World.Parts/Body.cs:2353–2363` regenerates defaults, first flags and other body maintenance. CoO now completes its declared defaults at the finite factory boundary after its actor override. This matches the initialization obligation but is not a port of Qud's broader maintenance/events.
- `XRL.World.Parts/Combat.cs:711–738` selects candidate weapons and deduplicates exact object references before strikes. CoO relies on its body first flags and now excludes the occupied secondary slot. These are compatible one-object/one-selection intentions, not identical algorithms.
- `XRL.World.Anatomy/BodyPart.cs:2852` considers natural and equipped melee weapons through AttackFromPart and PreferDefaultBehaviorEvent. Qud's selection, shield and preference rules are richer. The Buckler fallback control intentionally preserves CoO policy; do not claim exact Qud shield parity.
- CoO known-only legacy repair preserves serialized references and all existing flags. This conservative graph policy is a deliberate CoO load contract, not a reason to replay Qud's whole current-body recomputation over detached subtrees.
- The finite NaturalWeaponFactory recipes, no new natural inventory units, and current delayed-effect attribution remain CoO-specific. Existing delayed Poison/Bleeding source behavior and gas-at-already-removed-target behavior are not repaired or newly certified here.

## Independently read execution evidence

All paths are under `Docs/Verification/GameSystemAudit`. These are root-generated archives, read by this reviewer; no new test execution occurred here.

| Artifact | Directly observed result / meaning |
|---|---|
| GA03i-red.xml.gz | 34 total, 7 passed / 27 failed: original content/quiet RED. |
| GA03i-adversarial-first.xml.gz | 74 total, 66 passed / 8 failed: actual factory natural activation gaps. |
| GA03i-natural-minimum.xml.gz | 84/84 passed after initial natural fix. |
| GA03i-first-full.xml.gz | 9568 total, 9567 passed / 1 failed: preexisting raw Body flag assertion exposed introduced load recomputation. |
| GA03i-natural-alias-red.xml.gz; GA03i-natural-unknown-red.xml.gz | Retain the actual alias/raw-flag/unknown-policy RED evidence. |
| GA03i-natural-alias-minimum.xml.gz | 95/95 passed, including Tier1 body neighbors. |
| GA03i-natural-adversarial-first.xml.gz | 26 total, 25 passed / 1 failed: actual Greatsword extra natural attack, before final guard. |
| GA03i-native-before-twohand-review.json + log.gz | Run 16a132ba899c4d77b953f458fb14774e: 23/23, 0 unexpected errors, 5.804119 s, exit 0. All shutdown ownership/unregistered flags true; cleanup exit 0; owned temporary save directory confirmed absent. |
| GA03i-before-perf.json + native/log/provenance | Run aa9ab41e94194f5eab0c61ce5a973c0e: true before guard with natural activation present; valid 75.1736779 s, 72287 frames, 492 accepted requests, no overflow/errors. |
| GA03i-after-perf.json + native/log/provenance | Run ea9e69e041ea4069b14e0452a56a2648: guard present; valid 75.1522081 s, 71654 frames, 491 accepted requests, no overflow/errors. Exit 0 and cleanup exit 0; owned temporary save directory confirmed absent. |

Native content honesty: real live factory and village/named-profile producers, actual 24 kits, actual trader stock and owner graphs, natural full melee dispatch, actual N/F5/F6, ordinary walk/G/rendered-choice pickup, and exact equipment cleanup/recovery were checked. Combat, injury and death are explicit scenario APIs with controlled RNG/HP/arena settings. The report does not prove keyboard combat, ordinary encounter frequency, enemy AI, art quality, feel or full balance. Six named-profile actor observations include both real PeatCutters; the producer fixture has a bounded search, not an all-world-seed guarantee.

Performance honesty: both variants include natural activation, and provenance records differ only in CombatSystem among the measured owned sources. BEFORE two-hand 243 requests produced 486 hit rolls/243 offhand rolls; AFTER 249 requests produced 249/0. One-hand stays two hit rolls/one offhand roll per request. This is intentionally different gameplay work. Full public melee timing includes RNG/events/messages/diagnostics/FX, while frame samples include editor/renderer overhead. It does not measure isolated guard cost, whole-wave creation cost, landed-hit/rider/death cost, zero allocation, statistical equivalence, or a general speedup. Each performance branch explicitly reports zero functional content cases.

## Remaining closure and honesty notes

1. Root must finish and record the after-guard focused/full suite and functional native rerun. The prior 23-group content run is valid evidence for its actual variant, not retroactive final-guard verification.
2. Living-doc current headers now correctly say implemented/final verification in progress and state the conservative save policy. Preserve historical RED entries as history; mark the older “two-hand hypothesis awaiting RED” entry historical when writing final results.
3. Source and literal tests establish newly reachable effect/gas configuration. Do not restate payload duration fields as actual effect runtime duration or imply a new exhaustive natural-rider live matrix.
4. The existing CombatSystem diff also contains protected attack-facing/EntityVisualHooks/SpellFxCapture changes unrelated to this wave. GA03i owns only the secondary-hand selection hunk there; preserve the other work and stage narrowly.
5. Sprites are a separate next phase. Existing equipment integration is implemented; no new sprite family, accepted image-generation concept, renderer mapping, visual quality or sprite performance completion is claimed.

No additional gameplay fix is requested by this review. Final source review has covered the assigned taxonomy, source/reference comparison and concrete counterchecks; execution completion remains accurately bounded above.
