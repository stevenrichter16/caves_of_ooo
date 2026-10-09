# Equipment and illumination passes 30–35

Status: six equipment/light passes implemented; 58/58 dedicated native EditMode cases pass, including adversarial review fixes. Parent owns final live demonstration and the wider tranche verification. Original CoO interactions, guided by situational inventory utility rather than a Qud implementation claim.

## Design

| Pass | Concrete action | Cost, duration and limits | Ordinary source |
|---|---|---|---|
| 30 Torch | Touch a held lit torch to one adjacent combustible object or actual oil film. | One action and 1 FuelMass. Uses the same 600-joule meaningful ignition as FireMoss; rejects wet/unlit/frozen/stowed tools, cold targets the dose cannot ignite, creatures and dry empty ground. Existing fire spread can harm the player. | Morrowfast mender, cave/camp/provisioner stock and authored dungeon supplies. |
| 31 LampveinFan | Fan one adjacent transient gas entity, dispersing up to 5 density. | One action and occupied hand. A small cloud can disappear; stable/anchored gas cannot be fanned. Does not remove independent smoke/veil tile clouds or imply gas immunity. | Renewable Cave Lampvein crop and corresponding seed. |
| 32 GroundwireScreen | Discharge the holder's active Electrified effect into a chosen adjacent passable tile. | One action and occupied hand. Requires the holder to be capable of acting, removes that exact charge effect and writes coarse tile charge 1–2 (ceil, capped); refuses already-full charge. Resolves real tile reactions after commit, potentially harming wet neighbors. Does not cleanse stun or confer new resistance. | Sodden works finite equipment locker; current held resistance remains. |
| 33 GripfrondWrap | Brace with a nearby intact wall handhold. | One action; blocks one otherwise-valid physical push/pull during the next 3 owner turns. Requires exact worn handwear and the selected wall still adjacent. Movement releases the stance. Attacks/spells remain possible. No stack of multiple braces. | Renewable Stump Gripfrond crop. |
| 34 IronshodBoots | Plant feet on stable ground. | Same one-push/pull, 3-turn stance, but requires exact worn boots and actual nonslippery, pool-free terrain under every occupied foot. Existing speed penalty remains. No wall required; icy/oily contact invalidates it. | Ordinary ArmorerStock/FindArmorT2/FindArmorT3 and MarlbackBreacher equipment as verified in the current item catalog. |
| 35 GlowQuartz | Crack one mineral into a temporary light placed on adjacent open ground. | One mineral and action; nonrecoverable radius-4, intensity-.6 light for 30 material turns. Existing LifespanPart saves/ages it; no new timer driver or hearing/stealth mechanic. | Arcanist/ore-cache/death-construct tables and finite GlowQuartz veins. |

## Source verification corrections

| Initial assumption | Verified API/behavior | Consequence |
|---|---|---|
| A new timer Part is needed for a dropped light. | `LifespanPart` already counts EndTurn and `MaterialSimSystem.TickMaterialEntities` explicitly ticks it; public TurnsRemaining saves. | Reuse LightSource + Lifespan blueprint, radius4/intensity.6/lifetime30. No new generic timer. |
| Gas density is a plain field. | `GasPoolPart.Density` fires a change event; `_density` is the public save backing field. Stable gas is anchored and nondecaying. | Reduce density transactionally and retain creator/type/level; preserve stable gases. |
| All forced moves are physical pushes. | `ForceMoveTo` also handles slips and general relocation. `SkillCombatHelpers.DragAlong`, `Cudgel_Slam` and `HookedEffect` are explicit push/pull call sites. | Place brace checks at these three physical sites, not globally in ForceMoveTo. |
| A brace can monitor movement through an Effect callback. | Effect has no arbitrary AfterMove hook; `StatusEffectsPart.HandleEvent` already processes AfterMove. | Add one explicit cancellation call there; also validate saved position/equipment/ground at consumption. |
| Equipment can be inferred from its slot string. | Native Body bindings plus inventory equipment cache and Physics.Equipped must agree; held objects are absent from carried Objects. | Require actual current equipped ownership, singleton state, matching required slot and valid body binding. |
| A charged actor can always ground immediately. | Electrified application also applies a 2-turn stun; grounded action must not bypass it. | Only an actionable charged holder can discharge; early stun recovery or wet charge duration provides a real window. |
| A lit torch can ignite any nominally flammable material with a tiny dose. | Ignition requires hundreds of joules; MaterialFieldActions is introducing verified 600J ignition gates for FireMoss. | Share target/query and ignition helpers with that group, charge real torch fuel, never call a tiny warmup an ignition. |
| Any 'solid thing' makes a fixed handhold. | Solid movable loads can be hauled; Walls have explicit Wall/Solid tags and destructible live state. | Grip stance selects intact solid Wall-tagged ground owners only. |

## Tests and review obligations

Write native menu tests for all six items and matched invalid/missing-equipped controls. Test fuel/no-op gates, stable gas, exact density changes, charge/stun separation, one physical shove absorbed, later shove allowed, wall/ground invalidation, voluntary movement and generic ForceMoveTo cancellation without interception. Save/load both the brace and finite light. Rollback must restore payment/effect/fuel/density and remove only newly staged light. New transaction BeforeCommit guards will reject mutations in outer callbacks. Actual ordinary sources and inventory routes need evidence, not merely presence in a blueprint file.

## Implementation record

Native first RED: `ItemUtilityEquipmentTests` 19 cases (8 pass / 11 fail); `ItemUtilityEquipmentAdversarialTests` 25 cases (6 pass / 19 fail). The failing positive cases expose absent menus and actions, while invalid-equipment countercases already pass. Evidence: `native-three-groups-red.xml` beside this document.

Parent owns dispatcher, player help, blueprint surgery and light model. This group owns EquipmentUtilityActions, EquipmentBraceEffect, the three explicit physical displacement integrations, the status AfterMove cancellation hook, tests and this evidence document.

Implementation uses shared MaterialFieldActions ignition queries and real heat application. Gas and charge effects publish only after inventory commit, guarded against changed outer callbacks. Quartz is staged dark until its payment commits. A braced stance is never a global forced-movement veto: only validated physical pushes/pulls consult it.

### In-phase review

- 🟡 Root found the first quartz draft captured actor origin after consumption callbacks instead of binding the original selection. Original selection and exact staged object are now guarded. Fixture correction: Stat.Penalty is a plain field and consumption does not fire StatChanged. The initial proposed callback was vacuous; replacement uses a registered ObjectCreated hook on the real beacon factory, asserts that it ran, and moves the actor during creation. The real factory-hook case passes in the final equipment run.
- 🟡 Root found the first brace draft applied after commit, allowing an effect veto to spend an action without granting protection. Confirmed native veto RED; implementation now stages a nonarmed effect, requires acceptance, removes only its own effect on rollback, and arms it after commit.
- 🟡 Root found the stance's maintenance gate reused action availability, causing a stun to discard an already-prepared stance. Confirmed native RED; creation still requires an actionable owner while maintenance permits stun. Source correction: Slam itself pushes before applying stun, so the important countercase is a stance prepared before another incoming stun.
- 🔵 Initial equipment test fixture did not initialize tile reactions. Corrected fixture with isolated setup/restore of the real reaction table, avoiding order-dependent success.
- 🟡 Additional review checks hidden adjacency: item menus must not disclose invisible clouds or handholds. Five paired visible/hidden and stale-menu cases failed in `native-review-red.xml`; the implementation now requires visible occupied contact for objects and a visible selected ground tile.
- 🟡 Hooked runs during reverse-index effect ticking. Removing an earlier brace can shift Hooked and tick it twice. The duration/position countercase failed in `native-review-red.xml`; Hooked now marks the stance expired and defers removal until normal end-turn cleanup. Other physical push sites still remove the consumed stance immediately.
- 🔵 The finite-light test initially assumed a dark player; the Player blueprint supplies its own glow. The corrected assertion compares the post-expiry map to the measured pre-placement light, preserving actual production light sources.

These are original equipment interactions; no exact Caves of Qud mechanics parity is claimed. Native logic can verify menu eligibility, costs, effect lifecycle, actual physical movement/light consumers, and save state. It cannot establish visual readability or input feel; parent handles the in-editor scenario and model checks.

### Ordinary-source witnesses

- The existing `BiomeCropUtilityTests.ActualHarvestCanPerformItsAdvertisedNonCookingUse` harvests the species, picks up its real yield and exercises the resulting native equipment. `BiomeCrops.json` lists Gripfrond in Stump and Lampvein in Cave, rather than adding a new dev-only source for these verbs.
- `SoddenDistrictGenerationTests` checks the generated, unlocked works locker contains GroundwireScreen among its exact supplies; SoddenDistrictBuilder validates its native Hand equipment and armor. The new active discharge adds value to equipment already found during this expedition.
- The ordinary loot tables contain Torch in provisioning supplies, IronshodBoots in ArmorerStock/FindArmorT2/FindArmorT3, and GlowQuartz in arcanist/ore/death tables. `GlowQuartzVein` also exists in live landmark stamps and is a finite Harvestable source.
- This pass does not inject equipment into the starting inventory or grant NPC decision-making for the new menus. Any actionable entity can use the command service through a real inventory; existing AI has not been taught to select these tactics automatically.

### Final native verification

The second native pass verified all 19 happy-path equipment cases, including real fire, gas changes, physical pushes, saved stance, finite saved light and rollback. Its equipment adversarial fixture passed 28/35; six failures demonstrated the new visibility/Hooked findings now fixed, and one was the mis-specified StatChanged fixture now replaced by an actual factory hook. The final native pass also verifies destroyed-handhold counters and a natural screen-use window: after two actual effect EndTurns with failed stun saves, wet Electrified retains one turn and permits grounding, while dry charge has expired. This does not manually remove stun.

⚪ Deferred: hypothetical external callbacks renaming BlueprintName/ID in place during the same inventory action are not separately guarded beyond current ownership/Part/reach checks. No current inventory callback doing this was found; root chose to finish the concrete gameplay/visual verification rather than grow another speculative adversarial branch.

Independent review of CompanionCareActions found no new ordinary healing/cure/payment blocker. Known-party assistance without a new visibility requirement was discussed as an intentional rescue option in cover; no change made by this group.

Final equipment evidence: `native-final-meal-review-red.xml` contains **21/21 ItemUtilityEquipmentTests** and **37/37 ItemUtilityEquipmentAdversarialTests**, all passed. The file is named for two unrelated companion-meal review failures in that combined 260-case run; it is not an all-tranche green claim. The equipment group has no remaining known medium/high severity findings.

Verified cases include exact current worn equipment; dry/lit/fueled torch ignition of actual scenery and oil; stable versus transient gas and independent haze; charge/stun separation and the natural wet-recovery window; one physical push or pull absorbed; blocked pushes preserving stance; generic relocation not vetoed; voluntary movement and equipment/wall/ground invalidation; native Hooked turn bookkeeping; brace veto/rollback; saved equipment binding and duration; finite saved beacon aging and actual light-map removal; initial visible versus hidden selections and stale visibility; real factory callback movement; and exact supply/outer-failure behavior.

Files owned by this group:

- NEW `Assets/Scripts/Gameplay/Items/EquipmentUtilityActions.cs` + `.meta`: six guarded equipment/mineral action families and their final commit guards.
- NEW `Assets/Scripts/Gameplay/Effects/Concrete/EquipmentBraceEffect.cs` + `.meta`: saved one-force stance, movement release and narrow deferred Hooked cleanup.
- MOD `Assets/Scripts/Gameplay/Skills/SkillCombatHelpers.cs`, `Skills/Cudgel_Slam.cs`, `Effects/Concrete/HookedEffect.cs`: consult the stance only after a physical destination passes its guards.
- MOD `Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs`: this group adds only the AfterMove cancellation call; companion receipt lifecycle changes belong to the companion group.
- NEW `Assets/Tests/EditMode/Gameplay/Items/ItemUtilityEquipmentTests.cs` and `ItemUtilityEquipmentAdversarialTests.cs`, each with `.meta`: 58 native tests and countercases.

All four new C# assets have fresh handwritten 32-hex GUID metadata. Modified physical integration files pass whitespace diff checks. No save-system change, generic ForceMoveTo interception, extra AI planner or automatic item grant was introduced.
