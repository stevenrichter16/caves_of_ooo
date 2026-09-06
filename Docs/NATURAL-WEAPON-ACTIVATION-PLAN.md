# Natural weapon activation — GA03i discovery

Status: COMPLETE,2026-09-06 (GA03i). Fresh creation, conservative attached/detached save repair and occupied secondary-hand exclusion are implemented and verified. Final full9599/9599GREEN,0CS;115new equipment/natural tests. Final native23/23 and both75s bounded combat profiles passed. Historical REDs and intermediate failures are retained below.

## Observable contract and bounded scope

A newly created actor must have the natural weapons declared by its anatomy and NaturalWeapon property before creation callbacks, equipment grants and its first attack. Existing held weapons take priority on their hand. Loading an older body with missing natural-weapon objects must restore declared defaults without replacing valid saved natural objects, changing missing-limb state, regranting personal equipment or emitting equipment messages. Handless opted-in body attacks and legacy no-Body fallback remain valid.

This is an explicit expansion of GA03i beyond 24 kits and quiet success messages. The kits exposed previously dead natural-weapon creation; masking the test with manual regeneration would falsely certify ordinary gameplay. Enabling authored attacks changes real combat difficulty, offhand participation and damage-class riders; this is not cosmetic or balance-neutral.

## Verification corrections before implementation

| Premise | Actual source read | Consequence |
| --- | --- | --- |
| Natural weapons are lazily initialized in ordinary turns | Body.HandleEvent's BeginTurn branch is empty. The only production caller of Body.UpdateBodyParts is InputHandler.TryDebugDismember (DevMode F8). That is the only caller of RegenerateDefaultEquipment | Do not add an explicit maintenance call only in tests/native fixtures. Repair a real creation/load path |
| Setting DefaultBehaviorBlueprint creates an entity | EntityFactory.InitializeAnatomy assigns strings after SetBody; Body.SetBody only propagates tree references. GatherMeleeWeapons reads _DefaultBehavior objects and falls back when none exist | Initialize after the actor-specific NaturalWeapon override, before ObjectCreated |
| Full regeneration is automatically safe on saved bodies | RegenerateDefaultEquipment currently clears a populated default when its declaration is empty. SaveSystem.LoadBodyPart restores both declaration and reference and invokes Body.OnAfterLoad only after graph parsing | Preserve valid saved custom references; use missing-only repair on load, with no schema change |
| Old actor saves should get new personal kits | Save load does not fire ObjectCreated | Repair missing natural anatomy objects only; never backfill Loadout/items |
| Every natural attack should become a hand weapon | Handless Avian/Frog content uses BodyNaturalAttack and entity MeleeWeapon; legacy no-Body uses its own path | Preserve these paths and test both controls |

Root read EntityFactory.CreateEntity/InitializeAnatomy, Body.SetBody/OnAfterLoad/HandleEvent/RegenerateDefaultEquipment/RegenerateLimb, CombatSystem.PerformMeleeAttack/GatherMeleeWeapons/PerformBodyPartAwareAttack, NaturalWeaponFactory, AnatomyFactory and SaveSystem graph loading/body serialization. Two independent readers confirmed the missing production caller. Their initial Qud/save/mutation reviews confirmed the producer gap and exposed the shared-detached alias repair issue. Final independent review covers the narrowed fix. Directly cited line locations may move with the fix; resolve symbols.

## Implementation proposal and gates

Smallest fresh-creation fix: invoke existing Body.RegenerateDefaultEquipment at the end of EntityFactory.InitializeAnatomy after the override. Keep handless anatomy and preinitialized custom bodies intact. Final reviewed load policy: populate only null defaults with recognized nonempty declarations, marking only each newly created unique default first. Preserve already serialized natural references AND flags, including shared aliases across attached/detached branches and custom blank-declaration objects. Unknown missing recipes remain missing; explicit public factory calls retain their existing fallback. The initial blanket-recalculation proposal was rejected by actual REDs. No per-frame/turn maintenance pass, no new equipment/loot unit and no save version bump.

Before implementing: add public melee-dispatch REDs and saved-missing/default-preservation controls. The dedicated equipment gate remains 40 cases; add a separate natural activation gate covering fresh attacks, ordering, repeat identity, save repair, equipped precedence, handless and no-Body controls, injury/regeneration and unsupported declarations. Run targeted tests, full suite, independent cold-eye/hypothesis review and native first-attack checks. Profile first-creation cost if a hotpath is changed; do not claim speedup.

Body/mutation lifecycle expansion will be driven only by additional real RED cases. A returned dismembered branch already containing its saved natural objects is not automatically a missing-default bug. Do not add unrelated movement, scheduler, equipment or global callback changes.

## Implementation log

- Baseline is GA03h ab7809b1 /9484 green; GA03i 24 kits + quiet grant code is uncommitted and its initial34 tests pass.
- GA03i dedicated40 added: focused74 returned66PASS/8FAIL, zero CS. Failures: two armor-only natural gather cases (0 instead of2), four equipped+offhand cases (1 instead of2), two Scavenger choice cases (no remaining natural hand). Production call-site search confirmed the gap. No expectation was weakened and no manual default initialization was inserted into fixtures.


### Review corrections and implementation progress

- Root directly read Qud Anatomy.ApplyTo and Body.UpdateBodyParts/RegenerateDefaultEquipment: completed anatomy construction performs default maintenance. CoO uses a finite factory-end call after the final recipe override, not a per-turn pass; no exact port claim.
- Independent census reports40 effective named natural declarations plus default humanoid fists. This restores offhand opportunities and existing on-hit payloads, changing gameplay beyond the24 kits. Fresh natural entities historically have null ID strings; save reference-token identity is distinct from ID-string stability. No new identity scheme is claimed.
- Existing limb detach/reattach retains natural objects. Old missing detached defaults are tested and included in post-load repair. Arbitrary dynamic AddPartByManager and changing an already-materialized recipe remain outside this concrete producer fix.
- First implementation: factory-end existing regeneration; post-load null-only fill on attached/detached roots. Focused84GREEN. First9568full:9567PASS/1FAIL, field pin changed0x10→0x10010. Independent alias test then confirmed detached shared-natural recomputation can invent a second attack after healing. Global per-root recomputation must be removed from the load helper; preserve retained objects AND their first flags, mark only newly created unique defaults first.
- The existing field pin uses an unknown custom recipe. Legacy repair must only restore known recipes, keeping unknown missing declarations/flags untouched. Preserve the existing public NaturalWeaponFactory.Create unknown fallback for explicit callers via a shared-switch known-only internal path. Added explicit unknown-save/public-fallback controls before this change.
- Additional hypothesis awaiting RED: a two-handed weapon's occupied secondary slot falls through to a fist now that defaults exist. Preserve free offhand behavior and historical primary non-melee fallback; reject only an occupied non-first slot if confirmed. The per-turn guard will require a75-second native before/after profile with workload/call-count differences stated explicitly.

### Final close-out

The26-case natural adversarial gate confirmed the occupied secondary-hand defect (25PASS/1RED). Minimal first-slot guard then passed121focused checks and the complete9599suite. Final native d09ad6e27f4047efbf55756584560551 passed23/23 with0unexpected errors and isolated save teardown. Two75s native profiles use identical natural activation and differ only by the guard; both preserve real free-hand attacks, while the fixed two-handed actor performs one swing. Source/provenance, independent taxonomy/Qud/hypothesis review, full measurements and limits are retained in Verification/GameSystemAudit/GA03i-REPORT.md. All introduced notable findings are resolved.

Files: EntityFactory, Body, NaturalWeaponFactory, owned CombatSystem supporting-hand guard; natural initial/alias/adversarial tests; shared GA03i equipment/native scenario and evidence. No new sprite, save version or universal rider/balance claim. A12 scheduling, A44 gas IDs, delayed source credit and arbitrary live anatomy mutation remain distinct recorded work.
