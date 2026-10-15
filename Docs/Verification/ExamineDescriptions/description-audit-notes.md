# Authored examine coverage — ledger content pass

**Status:** 124 leaf descriptions added; offline RED/GREEN and parsed mutation
checks complete. Root owns `Docs/EXAMINE-LEDGER.md` and native verification.
This is original Caves of Ooo prose, not a Qud text port or parity claim.

## Scope and measured coverage

The census reads all 712 blueprints, follows parent-first part/parameter merging,
and resolves the final `Text`/`Description` alias assignment in insertion order.
704 blueprints have a display name. Before this pass, 259 of those had no authored
examine prose; afterward, 135 do. Named authored coverage rises from 445/704 to
569/704. Dynamic item/creature/condition readouts are not counted as authored prose.

The bounded source scan finds exact quoted blueprint IDs in runtime World C# and
content Data JSON, excluding entity-visual and faction registries. Among those
references, named entries without prose fall from 198 to 74. This is **source
evidence, not a live world-generation census**: references can include placement
constraints, special scenarios and inactive paths. No acquisition rates or
guaranteed spawn frequency are inferred. `cohort-source-evidence.json` supplies a
concrete loot, stock, terrain, container or authored-placement reference for each
of the 124 selected entries; the full reports retain every found reference.

| Cohort | New descriptions |
|---|---:|
| Terrain, depth geology and ground markers | 25 |
| Workstations and containers | 16 |
| Components, reagents and gathered materials | 27 |
| Crops, plants and food | 10 |
| Found and loaned weapons | 19 |
| Fixed authored tonics | 14 |
| Liquids, lamps, gas grenades and traps | 13 |
| **Total** | **124** |

Only leaf blueprints with no authored children are edited. General templates
(`PhysicalObject`, `Terrain`, `Item`, `Creature`, `MeleeWeapon`, `TonicItem`) remain
blank. Existing descriptions, quest text and inherited descriptions are preserved.
Every addition is a short ASCII paragraph, between 25 and 240 characters. The
manifest records the reviewed text and cohort for every change.

## Verification sweep and corrections

| Premise | Evidence / correction | Consequence |
|---|---|---|
| Description absence can be determined from a local part | `BlueprintLoader.Bake` merges parent parameters; `ExaminablePart.Description` aliases `Text`; factory applies parameters in order | Resolve inheritance and both aliases before selecting targets |
| `Docs/Lore/v2` is the current canon | Its README explicitly calls it a parallel redesign; `Lore/README.md` establishes the current Bible, Second Spine and Mystery Ledger | Do not transplant the old redesign's people, dates or explanations |
| A name proves a mechanical material | `Material`, repair `Composition`, display names and component roles are separate fields | Change no material, composition or capability data |
| Every steel component repairs every object | `Tier1Repairs.json` authorizes metal gear for the steel blade and leather gear for binding | Narrow those descriptions to the actual material-specific gear uses |
| A sporeblade emits spores | Its authored weapon has Cutting/LongBlades and fungal material tags, no spore payload | Describe the surface, promise no emission/disease |
| Glassblown/Palimpsest item names justify an origin story | `Lore/MYSTERY-LEDGER.md` protects the Remnant and under-writing; ordinary decoration need not be cosmic evidence | Describe glass and worn markings without identifying origins or creating a new bleed-site |
| Fixed prose can report current fuel, lock or durability state | Those values change during play | Prose describes housings/structure; runtime ledger remains authoritative |

Sources reviewed: current `Lore/README.md`, `Lore/10_Bible.md` canon lock,
`Lore/MYSTERY-LEDGER.md`, aligned material-culture history, Objects parts,
`EntityFactory.ApplyParameters`, `BlueprintLoader.Bake`, existing examination
composition, the repair registry, forge/still verbs, mineral preparation,
pressure traps, container pools and underground depth materials.

## RED, implementation and counter-checks

1. Before editing Objects, the executable audit's `--expect-authored` gate failed
   for exactly 124 missing target texts. No target was missing, nonleaf, malformed,
   oversized or without a production source reference. Receipt:
   `authored-red.json` and `authored-red.stdout.txt`.
2. The raw JSON object spans were edited in place. No whole-file reserialization:
   the initial diff is 1,116 added lines, zero removed lines, adding one local
   `Examinable.Description` block per selected leaf. Later prose review adjusts
   four newly inserted strings, not unrelated content.
3. Parsed before/after comparison proves exactly **124 changed blueprints and
   588 identical blueprints**. Removing only allowed examine-prose fields leaves
   every changed object otherwise identical. This covers stats, tags, material,
   composition, drops, actions, effects and part data. The GREEN receipt includes
   baseline and resulting file SHA-256 hashes.
4. All 124 selected descriptions pass the final exact-copy/ASCII/length/leaf/source
   checks. Seven offline unittest methods pass, with per-blueprint subchecks.
   Counter-checks prove inherited text works, a later empty alias clears text,
   cycles reject, a material mutation rejects, an unplanned blueprint edit rejects,
   blank base templates stay blank, and existing authored controls retain prose.

Reproduction from repository root:

```sh
python3 Tools/ContentAudit/examine_descriptions.py --expect-authored --report /tmp/examine-current.json
python3 -m unittest discover -s Tools/ContentAudit -p 'test_examine_descriptions.py' -v
```

For a fresh parsed diff, provide `--compare-before` with the original Objects JSON
from the pre-change revision. The audit makes no Unity, game-state or content
mutations. Its report is offline evidence, not a replacement for native factory
or UI tests.

## Self-review

- 🔵 Narrowed steel-blade and leather-binding repair wording to their actual
  material families; removed an implied manufactured origin from ChoirSpine;
  made PalimpsestBlade's worn decoration ordinary and ambiguous. All fixed in the
  final manifest and data.
- ⚪ New tonic markings, vessel construction, wear and texture are authored visual
  flavor, not assertions that each small detail is drawn on its model. No new
  actions, status immunities, guaranteed outcomes or damage values are promised.
- ⚪ Some runtime clones may inherit the parent's ordinary description; this pass
  avoids authored JSON children but does not claim to census all dynamic clones.
- 🧪 Remaining missing entries include NPCs, spellbooks, runtime templates and
  special pools. They need their own role/spell/runtime-state review; the remaining
  number is not itself proof that all those entries need static prose.

**Can verify:** exact JSON shape, selected source references, modeled inheritance,
authored prose coverage, untouched mechanics and counter-check behavior.

**Cannot verify here:** native factory reflection/order, final ledger wrapping,
visual correspondence with every model, or frequency/discovery in actual generated
worlds. Root owns native checks. No Unity editor/test tooling was run by this pass.

## Files

- `Assets/Resources/Content/Blueprints/Objects.json`: 124 descriptions only.
- `Tools/ContentAudit/examine_descriptions.py`: reproducible census and strict
  parsed mutation receipt.
- `Tools/ContentAudit/test_examine_descriptions.py`: seven offline audit/contract
  counter-check methods.
- This directory: final manifest, source evidence, before/after reports and
  stdout receipts.
