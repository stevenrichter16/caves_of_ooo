# Original enemy replacement — census, design, and migration plan

**Status:** runtime migration implemented; art and final integrated acceptance in progress, 2026-09-26. Root applied the approved surgical Objects changes. Runtime identity migration is implemented and focused-tested; the art owner is replacing the actual silhouette and bindings. This work implements the user's new requirement to remove recognizable Caves of Qud enemies and replace them with original creatures; it does not claim Qud parity.

## 1. Verified result and scope

The resolved creature census contains **106 blueprints**, including the base Creature, Player, and friendly NPCs. Five form the explicitly named **Snapjaw** family. **GlowMoth** also overlaps a documented Qud creature name, although CoO's passive moth has materially different authored behavior and its own existing lore. These are the two confirmed families/names to replace. This is a census of the present roster and confirmed evidence, not a guarantee of the provenance of every generic fantasy word.

Primary public evidence: Freehold's [Deep Jungle update](https://freeholdgames.itch.io/cavesofqud/devlog/348340/the-deep-jungle-feature-arc-is-here) documents snapjaw settlements, lairs, scavengers, and equipment. Freehold's [February 3, 2023 update](https://freeholdgames.itch.io/cavesofqud/devlog/485523/feature-friday-february-3-2023) names glowmoths and their attacks; the [June 7, 2019 update](https://freeholdgames.itch.io/cavesofqud/devlog/84814/feature-friday-june-7-2019) discusses their natural ranged weapon. Those sources establish the names, not that CoO copied every behavior.

`Creature`, `CaveBat`, `CaveBear`, `CaveSlime`, `GiantSpider`, `Scorpion`, `Viper`, `JungleApe`, `StoneGolem`, `IceWight`, `SkeletalSentry`, `SleepingTroll`, and `MimicChest` are generic fantasy/animal names. No evidence found here supports categorizing those as distinctive Qud imports. Existing Ooo ecology such as GinFrog, SariSnake, Bandfrog, CascadeFather, and BrocchiniaSentinel has repository lore and should not be removed through word association. Glowmaw, PaleStalker, and Mosshulk likewise have no confirmed Qud-specific provenance in this sweep.

Evidence files:

- `Docs/Verification/DensityCompletion/OriginalEnemies/creature-audit.json`: resolved inherited-Creature census, display names, and narrow classifications.
- `replacement-reference-files.txt`: 253 matching text files under the searched active roots, comprising 69 runtime scripts, 150 tests, 14 Resources files, 1 Editor file, 17 art-source files, and 2 Lore files. Historical Docs are deliberately excluded from this count; binary assets also require a separate art inventory.
- `reference-counts.json`: machine-readable count by root.
- `snapjaw-active-reference-files.txt`: earlier narrower Snapjaw-only inventory, retained as audit history.

## 2. Pre-implementation verification sweep and corrections

| Initial assumption | Verified correction | Consequence |
|---|---|---|
| Only a few enemy display names need replacing. | Five creature IDs, a corpse, two natural-weapon tokens, faction data, population/lair rows, quests, conversations, renderer catalogs, art, tests, and saved bodies reference Snapjaw. | Implement one coordinated migration; do not leave an old ID spawning behind a new display name. |
| Existing Snapjaw artwork is a generic humanoid. | `ArtSource/SpawnRing3D/build_ring.py` constructs `Long_snapjaw_muzzle` and named eyes. | Original silhouette and newly authored art are part of completion, not an optional cleanup. |
| The Snapjaws faction contains only Snapjaws. | DesertBandit, AmbushBandit, SleepingTroll, and MimicChest also use it. | Do not blindly turn unrelated actors into the new species faction. Explicitly preserve their hostility and encounter relationships. |
| GlowMoth is only a generic luminous-moth phrase. | Official Freehold devlogs name a Qud creature Glowmoth. | Rename/design it independently while retaining CoO's existing passive ecology. |
| The CoO moth emits gameplay light. | Its concrete blueprint has Render, Physics, Brain, and Examinable; no LightSource. Brain is passive with a high flee threshold. | Preserve the current behavior; luminous prose is not an implementation of illumination. |
| Quest gremlins and gnomes are independent creatures. | `VillagePopulationBuilder` creates Snapjaw, then changes the display for the soot gremlin and dirt gnomes. The gnome path explicitly corrects its corpse, while the gremlin path does not. | Give these quest creatures explicit non-import identities and verify death/quest facts/corpse lifecycle. |
| Renaming blueprint data updates saved creatures. | `SaveGraphSerializer.LoadEntityBody` reads BlueprintName and serialized parts; it does not rebuild the creature from the current blueprint. Body parts also serialize DefaultBehaviorBlueprint and the actual fallback entity reference. | Choose and test a deliberate compatibility contract; never silently promise old saves are rewritten. |
| Every Snapjaw-named dialogue predicate has a producer. | `Villagers.json` has `KilledSnapjaw`; no active producer was found in the audited sources. | Preserve or deliberately repair the existing dormant predicate with a test; a broad string replacement is not a behavioral fix. |

References checked: `CLAUDE.md`; current Objects, Factions, EntityVisuals; PopulationTable; WorldGenerator; VillagePopulationBuilder; NaturalWeaponFactory; FactionManager; EnvironmentSpriteRenderer; SpawnRing3DCatalog; SaveSystem; SpawnRing3D generator/contracts; current C2 tactical tests; the current Lore faction documents and FELLING-WORLD-DESIGN references listed below.

## 3. Proposed original designs

### 3.1 Marlbacks — replacement for the Snapjaw family

**Design proposal, awaiting root's integrated choice:** small upright bank-burrowers with a broad, layered shale-and-mud back, a short blunt face, grasping forelimbs, and powerful digging rakes. Their silhouette is low and wide, without the long hyena muzzle, pointed mane, or toothy profile of the retired family. They collect discarded tools and use them to cut roots and defend narrow burrows. Their identity follows the Spread's banks, Sodden margins, and shallow excavations already occupied by these encounter rows. This is ordinary ecology and salvage behavior, not a new explanation of Urqu, Sari, the Felling, or the world's mysteries.

The replacements retain the current HP, damage, encounter weights, loot tier, and newly tested combat kits for the first migration. Original appearance, role names, examination prose, corpse, faction, and art change together; a later balance pass can adjust numbers with its own evidence. This avoids conflating a content-identity requirement with untested difficulty changes.

| Retired ID | Proposed ID | Display / role | Retained authored combat distinction |
|---|---|---|---|
| Snapjaw | MarlbackScrabbler | marlback scrabbler; an exposed-bank defender | Existing natural melee baseline. |
| SnapjawScavenger | MarlbackGleaner | marlback gleaner; carries discarded cutting tools in its back plates | Actual Dagger/ShortSword loadout, Shank/Lunge eligibility, high retreat threshold. |
| SnapjawHunter | MarlbackTunnelguard | marlback tunnelguard; braces a pole across a burrow mouth | Existing Spear and close Shank; preserve current resistance/stat behavior without inventing a new mechanic. |
| SnapjawChieftain | MarlbackWallkeeper | marlback wallkeeper; layered back with patched digging harness | Actual LongSword/Lunge, low retreat threshold, local assistance. |
| SnapjawWarlord | MarlbackBreacher | marlback breacher; heavy forward plates and a salvaged wedge-cleaver | Actual cleaver/Berserk, natural fallback, lowest retreat threshold. |
| SnapjawCorpse | MarlbackCorpse | marlback remains | Existing corpse yield/weight behavior, original examination text. |
| SnapjawClaw | MarlbackRake | digging rake | Preserve natural-weapon stats and body-slot behavior. |
| SnapjawHunterClaw | MarlbackGuardRake | hooked digging rake | Preserve the hunter's natural fallback profile. |
| WarlordCleaver | BreacherCleaver, if root chooses the coordinated item rename | wedge-cleaver | Keep the actual equipped item's Axe identity and natural fallback token semantics; replace warband/banner flavor. |

The design names are proposals, not claims of established canon. Before implementation, root should settle the exact short roster once so tests, art, and docs agree. Do not add lore about civilization, ancestry, or moral essence beyond the visible ecology.

### 3.2 Grove lantern-moth — separate passive fauna identity

Proposed ID `GroveLanternMoth`, display **grove lantern-moth**. Keep its hand-sized scale, pale column-colored dust, grove navigation, passive wandering, 3 HP, and fleeing behavior. Distinguish it visually with broad translucent wings bearing three soft bars of reflected grove light. Do not add Qud's ranged attack, hostile AI, or a new gameplay LightSource during this migration.

Existing CoO references are `Lore/Factions/05_BowerFolk.md` (installations and bio-light trade), `Lore/Factions/06_CatacombVillagers.md` (settlement displays), and `Docs/FELLING-WORLD-DESIGN.md` (fauna and installations). Update the living names without deleting that ecology. Exact runtime references found are Objects, PopulationTable, and GrovelandsBestiaryTests. Search all lore spelling variants, including Glow-moth/glow-moths, when implementing.

### 3.3 Unrelated actors and quest reskins

A species-specific **Marlbacks** faction is clearer than renaming the entire old catch-all faction. Proposed split: DesertBandit/AmbushBandit use a generic **Raiders** faction; SleepingTroll/MimicChest use an appropriate independent hostile-beast faction or explicit existing behavior. Preserve player/villager hostility and keep old mixed encounter members mutually non-hostile unless separately designed. Existing Beasts must be examined before assigning trolls/mimics: changing to a neutral faction would change encounters. Preserve the deliberate neutrality of GlassScorpion.

If this split would exceed the bounded migration, an explicitly generic hostile faction can retain the old relationship graph under a new neutral name. Document that as a scope choice rather than pretending every member is a marlback. In either case, test opt-in local assistance, because changing factions affects the new C2 assistance rules.

Create independent `SootGremlin` and `DirtGnome` blueprints (or another explicitly approved original quest identity) rather than giving them a hidden Marlback ID. Preserve `rbg_gremlin_routed`, `warren_gnomes_routed`, the number of quest creatures, kill-credit ordering, and generic remains. Their quest identity is independent of the hostile family replacement.

## 4. Migration surfaces that must move together

| Surface | Files / data | Required invariant |
|---|---|---|
| Blueprint graph | `Assets/Resources/Content/Blueprints/Objects.json` | Root makes surgical edits; parsed-object diff contains only approved targets. No spawnable retired family aliases. New inheritance, faction, corpse, descriptions, kits, real gear, and fallback weapons resolve. |
| Natural weapons | `Assets/Scripts/Gameplay/Anatomy/NaturalWeaponFactory.cs` | New tokens build the same functional weapons; exact natural-weapon activation roster updated rather than weakened. |
| Population | `Assets/Scripts/Data/Tables/PopulationTable.cs` | Every replaced row resolves; tiers, counts, selection weights, and anti-monoculture structure preserved. |
| Lairs / landmarks | `WorldGenerator.cs`, `LandmarkBuilder.cs` | Lair titles, biome boss selection, boss records, fort populations, and ambushes use original identities. |
| Factions | `Assets/Resources/Content/Data/Factions.json`, `Gameplay/AI/FactionManager.cs`, creature faction tags | JSON and fallback initialization agree; player/villager hostility, unrelated actors, neutrality, and assistance are deliberate. |
| Quests | `VillagePopulationBuilder.cs`, quest fixtures | No retired base underneath a gremlin/gnome display; correct corpse, fact, count, and before/after-acceptance behavior. |
| Dialogue | FriendlyNPCs, Hermits, RotChoir, Shopkeepers, Villagers, Wardens conversation JSON | Player-facing lines and node/condition identities consistent; old display names never leak in new games. Existing dormant kill flag gets an explicit decision. |
| Narrative / scenarios | Snapjaw-named witness scenarios, `ScenarioMenuItems.cs`, scenario fixtures | Root updates active diagnostic/scenario labels without altering historical receipts. Witness behavior remains covered. |
| Sprite rendering | `EntityVisuals.json`, `EnvironmentSpriteRenderer.cs`, actor and boss sprite resources | All new actors resolve, with original visual family. Remove StartsWith-Snapjaw assumptions; corpses and quest reskins route correctly. |
| 3D rendering | `SpawnRing3DCatalog.cs`, `SpawnRing3D/Library.asset`, SpellFx3DLibrary, ArtSource catalogs/contracts/generator | New family keys and original geometry reach the actual runtime library; no hidden long-muzzle model selected through a renamed key. GUID references survive or are deliberately replaced. |
| Tests | 150 matching fixtures in census, plus new dedicated adversarial fixture | Update exact IDs and semantic expectations without reducing coverage; preserve unaffected-creature controls. |
| Living lore/docs | Lore faction references, current density plan/kits/loot docs, FELLING-WORLD-DESIGN | Active design reflects the replacement; archival Qud comparisons and past receipts stay historical. |
| Save graph | `Gameplay/Save/SaveSystem.cs`, saved body and POI/faction records | Deliberate compatibility policy below; never recreate loaded actors wholesale or silently lose references. |

## 5. Save policy: accepted pre-release precedent and the remaining choice

Earlier density work explicitly accepted that existing saves retain old serialized blueprints/parts. A fresh-world-only replacement is consistent with that precedent, but the user must not be told that it removes the old enemies from existing saves. It also must not break loaded natural weapons or deferred corpse creation merely by deleting the old factory tokens.

Recommended coordinated policy is a **narrow load-time identity adapter** if root wants old saves to lose recognizable names as well. It should map retired blueprint IDs, faction IDs, natural-weapon tokens, saved lair boss IDs/titles, corpse references, and exact old default display names. Preserve entity IDs, inventories, equipment references, HP, cooldowns, effects, position, quest facts, and customized display names. Never regrant tactical skills or rebuild serialized parts from the new blueprint. Transfer existing reputation rather than initializing a new hostile faction over it. A renamed soot-gremlin/dirt-gnome must not be relabeled as a marlback based solely on its old base ID.

This adapter is a separate tested milestone, not a casual blanket string replacement across all serialized data. If root retains the accepted fresh-save scope instead, keep narrowly scoped legacy token resolution sufficient for load/death safety; do not keep ordinary spawn-table aliases that undermine removal. Document the remaining old-save visuals/names in the living doc and final report. Save format changes are unnecessary for a pure read-side mapping.

## 6. Concrete implementation and verification sequence

1. **Lock identity and compatibility scope.** Root selects exact family/moth/quest IDs and faction/save policy in the integrated living plan. Capture the current parsed Objects graph, population rows, loadout paths, visual catalogs, and representative save graph for before/after comparison.
2. **RED content contract first.** Add a dedicated original-enemy fixture that expects the new IDs and rejects retired IDs in live population/lair/quest/content/visual references. Counter-check unrelated creatures and historical docs. Run the failing test before changing production. Do not write a blanket `rg` assertion over the whole repo: compatibility tests and historical evidence legitimately mention retired names.
3. **Runtime identity migration.** Root edits Objects surgically; assigned agents update owned scripts/data after coordination. Keep stats/tier/weight changes out of this milestone. Update exact armed-creature and loadout pins. Validate every population/loot reference through the existing registries and factory construction.
4. **Faction and quest counter-checks.** Test hostile versus friendly/neutral targets, local assistance limits, mixed lair coexistence, retreat behavior, corpse blueprint on death, all three gnome deaths, gremlin kill before acceptance, and non-player kill attribution. A hostile-faction rename must not recruit GlassScorpion or turn bandits friendly.
5. **Save milestone.** Construct a serialized legacy graph before implementation, then RED the chosen adapter/compatibility invariants. Probe duplicate references, equipped versus fallback weapon identity, custom display names, corpse creation after load, saved lair bosses, faction reputation, round-trip idempotence, and no new skill/cooldown grant. Include a fresh-save control and unknown-ID preservation control.
6. **Original art milestone.** Author the new silhouette, sprites, and distinct boss forms; rebuild catalog artifacts with their existing tools. Verify import/library resolution and sprite/3D fallback at ordinary zoom. Retire old active art references without deleting unrelated assets. Root coordinates native screenshots and reviews readability; a JSON green test cannot establish original appearance.
7. **Integrated behavior and native acceptance.** Re-run the ordinary-stat C2 scheduler, equipped/natural fallback controls, population determinism, quest/witness scenarios, renderer contracts, and the dedicated adversarial suite. Native EditMode must compile against Unity's NUnit version. Native PlayMode should encounter a replacement in ordinary generation, inspect it, fight with ordinary stats, see assistance/retreat or the appropriate kit, observe its remains, and reload the save. Root records any travel/seed shortcuts; no invulnerability or stat boosts are balance evidence.
8. **Final census and regression.** Repeat the runtime identity reference census and parsed-object diff; explain every remaining old token as compatibility or history. Compare full private-runner results against the saved baseline, requiring no newly failing tests. Update the same-commit living docs and §2.3 commit message, then root fetches/rebases/pushes as authorized.

## 7. Self-review and honesty bounds

- 🟡 **Display-only replacement would be incomplete.** Actual muzzle geometry, hidden quest bases, corpse names, faction names, and generated lair labels can expose the retired family. All are included above; no completion claim until their active references are closed.
- 🟡 **Faction replacement can change combat without a stats diff.** Assistance and enemy selection depend on faction identity. Preserve behavior with explicit controls or document the intentional split.
- 🟡 **Old-save destructive rebuilding is unacceptable.** Serialized parts carry live state and reference identity. Only narrow migration or clearly documented retained legacy behavior is within the established contract.
- 🔵 **Original ecology proposal is new design.** Marlbacks and the moth's visual design are proposals grounded in the occupied biomes and existing moth lore, not facts discovered in the lore corpus. Root's implementation plan must distinguish the authored addition.
- 🧪 **Provenance limit.** Primary evidence confirms two names/families; this audit does not establish legal originality or prove every generic creature word is unused elsewhere.
- 🧪 **No native execution in this audit.** It can verify source references, factory/data shapes, and a bounded roster classification. It cannot verify imported artwork, actual native frame selection, combat readability, encounter feel, or old-save safety before the planned tests and editor run.

## 8. Implementation log / changed files

2026-09-26: read-only runtime/art/lore audit and primary-source check; 106-creature census, extended 253-file text-reference inventory; discovered GlowMoth name overlap, passive/no-LightSource distinction, actual muzzle geometry, mixed faction membership, quest reskins, and serialized identity migration surfaces. No Assets production changes made. New audit doc and verification manifests only.


## 9. Adopted implementation and review closure

Root approved the Marlback roster, GroveLanternMoth, and exact descriptions in
`Verification/DensityCompletion/OriginalEnemies/blueprint-replacement-spec.json`.
The faction choice is **OutlandRaiders**, preserving the former heterogeneous
hostile coalition rather than splitting combat relationships during an identity
change. This is not a species name. The four unrelated hostile blueprints retain
the same player/villager/coalition feelings. GlassScorpion stays neutral.
SootGremlin and DirtGnome are independent Creature blueprints with generic remains
and ScavengerClaw; root copied their older unarmored base behavior, so they do not
become an extra source of the newer combat loadouts.

Root's parsed Objects receipt records8 renames,4 faction-only updates,2 new quest
blueprints and480 unrelated unchanged objects. Non-art runtime migration changes
213 tracked/explicitly-owned live text files (plus follow-up source/test fixes),
with longest-first exact identities. Art ownership and all unrelated untracked
files are excluded. Historical docs and verification receipts are not rewritten.
The natural-weapon roster remains an exact list and now contains45 creatures,
including the two newly independent quest identities.

A read-side `OriginalEnemyIdentity` adapter normalizes exact retired blueprint,
faction, body-recipe, lair-boss/title, corpse and default display identities.
Player reputation transfers without reset; a current-key entry wins if both old
and new keys are present. Custom names/prose/IDs and unknown tokens survive. Saved
parts are not reconstructed and current blueprint kits are not granted on load.
The one known item description naming the retired family is replaced only on an
exact old-default match. This is a deliberate narrow exception to the earlier
pre-release acceptance of serialized old identities, not a general save upgrade.

Initial43 RED:42 missing-content/identity failures and1 unknown-ID control passed.
After implementation,43/43 GREEN. Dedicated18 adversarial cases then found5 RED
assertions across two real defects: the NaturalWeapon declaration is an entity
property, and a custom display name alone cannot identify a quest reskin. The
adapter now uses the property and recognizes explicit quest-fact parts. Combined
61/61 GREEN, followed by376/376 in the published-source selected regression.
The wider selected run caught one missed untracked test token and two family-prefix
assertions accidentally narrowed to the base role; corrected without weakening
those assertions.

Root's independent read produced6 more probes (24 dedicated cases total).
The exact old playable CinnamonBunFavor/drive_off_gremlin marker was not recognized,
and both quest reskins retained a species recipe in their properties and attached/
detached limbs:3 RED among67. The fix recognizes only that exact quest/objective,
changes only the old default recipe to ScavengerClaw, and leaves actual fallback
weapon references, stats, custom recipes, limb parent IDs and positions intact.
Wrong quest/objective, custom names and unknown recipe controls remain green.
**67/67 private GREEN** is recorded before publishing the verified change after
root's native-art RED window. The fresh playable scenario now spawns SootGremlin.

Artifacts: `roster-red.xml.gz`, `identity-adversarial-red.xml.gz`,
`parent-review-red.xml.gz`, `focused-green.xml.gz`; final expanded receipt follows
the checked-in-source run. Root/art owner hold native visual and imported geometry
acceptance. No Unity calls were made by this audit agent.
