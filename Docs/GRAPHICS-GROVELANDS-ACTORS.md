# Grovelands — three living actor identities

2026-10-27. **Source preflight and fresh Mac baseline complete; current owned-graph
confirmation, native RED, production and acceptance remain pending.** Root reports Maw source
`751d9247cf05f55e0baf8f8f8cb02ded0eb931ae` pushed and verified on origin/main;
its package work is separate. Root obtained the new actor baseline through
ordinary controls in that fresh package; see the final checkpoint below. This document changes no runtime,
art, tests, Unity state, Git state or current coverage ledger.

The finite next slice is exact **Shambler, Mosshulk and ChoirTendril**. Reuse
approved Mosshulk and ChoirTendril16×16 source pixels unchanged unless the
actual comparison exposes a named defect. Plain Shambler needs one original
native16×24 body. Preserve real committed-melee intent, damage/status cues,
living merchant interactions and current world/reader authority. This is
original CoO presentation work; Qud parity is not claimed. No new AI, balance,
population, stock, anatomy or optional animation is proposed.

## Verified source corrections before implementation

| Premise / exact source anchor | Verified correction and implementation consequence |
|---|---|
| Old `grovelands-next-gap-plan.md` recommends authoring all three | That audit found no exact **flat catalog mapping**, not an absence of all suitable source images. `EnvironmentSpriteRenderer.cs:154,170` already identifies exact Mosshulk/ChoirTendril resources. Both are crisp16×16, binary alpha and five colors. Reuse two, author only plain Shambler. |
| `Shambler` and `SporeShambler` display “spore shambler” | They are separate registered identities (`Objects.json:36320` and `20574`). Plain Shambler is `z`, Beasts, inherited Flesh/Organic, HP16, DefaultTendril and CommittedMelee. SporeShambler is `f`, RotChoir, Fungal, HP22, SporeTouch, GasImmunity and BurnOffGas. Display name, corpse yield or fungal-looking shape cannot alias them. |
| Moss/tendrils imply a Fungal material or special anatomy | All three planned owners inherit Creature's Flesh/Organic Material, Thermal, Body and Inventory. None declares Anatomy or BodyCategory; `EntityFactory.cs:469–510` builds the default Humanoid/ANIMAL body. Require legitimate owned body semantics; do not invent Fungal/Frog anatomy or alter gameplay anatomy. |
| Mosshulk omits RenderLayer/Brain in its own block | Inheritance supplies layer10, solid Physics, Brain sight10/Wanders/WandersRandomly and DamageFlash. Its `M`/`&g`, HP55, Speed70, AV6 and MosshulkSlam are real (`Objects.json:19194–19290`). Omitted local fields are not absent parts. |
| ChoirTendril is a plant prop or guaranteed friendly stationary decoration | It is a solid living Creature, `F`/`&G`/layer10, HP80, Brain sight12/Wanders=false, ChoirLash, RotChoir, owned Conversation `ChoirTendril_1`, Trader `ChoirStock`/initial70drams and Inventory (`Objects.json:16232–16372`). `SpeaksToHostiles` intentionally permits conversation despite hostile relationship (`ConversationManager.cs:48–85`). No faction/friendliness, unchanged purse, empty/full stock or immobility gate belongs in native identity. |
| Plain Shambler can lose intent because this is a static-art pass | `CommittedMeleePart.cs:11–155` implements one windup action, fixed-ray strike of bounded reach2, then recovery. `CombatIntentRenderer.cs:45–75` and `CombatIntentReadout.cs:7–81` already expose visible amber ray chevrons and reader text. Preserve these gameplay-essential cues; optional pose polish is a separate matter. |
| Ordinary Grovelands only supplies these actors | Tier3 chooses a weighted Shambler/Mosshulk/Rotling encounter family, plus optional Choir and other owners; TendrilFen separately attempts1–3 residents. Saved generation/POIs and existing outcomes decide actual owners. Three identities do not cover every population or all areas. |
| Region authoring determines where a living body may display | Shambler/Mosshulk also appear in Grovelands lair guards; Mosshulk occurs in CaveTier3; Choir appears in JungleTier3, a living-underground stamp and cathedral builder. Keep globally portable exact live-body identity under current graph/selected-owner/reader guards. Do not broaden Grovelands ground/scenery admission. |

Merged inheritance was read from Objects.json against the actual factory
initialization source. The source-default HP/speed/stock numbers above describe
the owners; they are not immutable sprite predicates. Surviving damage,
relationship changes, a renamed display label and legitimate trade must retain
the same body. Explicit visual reskins, wrong blueprint/glyph, foreign or duplicate
essential parts, custody, invalid footprint, dead/removed/hidden owners retain
truthful existing fallback/refusal.

## Exact owner and resource contract

| Owner | Actual body and essential behavior | Finite art proposal |
|---|---|---|
| Shambler | `z`, `&m`, layer10, one-cell solid Creature; HP16/Speed80; sight8/Wanders; Beasts; DefaultTendril. Owns CommittedMelee `tendril lash`, Reach2. Corpse is separate CreatureCorpse; harvest ShamblerSporeSac1 at80%. | One new original16×24 static body after fresh capture → imagegen/reference critique → parts → editable integer-cell production. Slack former-person silhouette with one legible tendril; no copied SporeShambler or fictitious spore cloud. Existing intent chevrons/readout supply essential attack timing unless actual comparison identifies a specific unresolved readability defect. |
| Mosshulk | `M`, `&g`, layer10 inherited; HP55/Speed70/AV6; mobile Beasts creature, MosshulkSlam. Separate corpse yields two MendleafSprig at100%. | Borrow `Sprites/Environment/mosshulk`, exact Rect(0,0,16,16), centered pivot,16PPU, FullRect. Five-color broad moss mass; no scale-up or invented larger footprint. |
| ChoirTendril | `F`, `&G`, layer10, solid living merchant; HP80/AV3; RotChoir and SpeaksToHostiles. Conversation/Trader/Inventory remain real services; authored Wander=false does not constitute a new physical immobility promise. Separate CreatureCorpse has no authored harvest product. | Borrow `Sprites/Environment/choir_tendril`, same exact square geometry. Five-color upright branching silhouette. Do not reuse MycelialColumn, generic plant or hostile-only body. |

Read-only PNG inspection confirms Mosshulk146 occupied pixels and
ChoirTendril95; both16×16 with alpha only0/255. The original SporeShambler
source is16×16/seven colors, but is explicitly excluded from plain Shambler
selection. Source hashes at this preflight:

- `Assets/Resources/Sprites/Environment/mosshulk.png`:
  `b638b82cbcc405c27dbb470e61ed8597ecc32480daa21d09de354e527c4bc031`.
- `Assets/Resources/Sprites/Environment/choir_tendril.png`:
  `0e7f78d44986d35fd34c46e6c163664afb53c2fb72b492ae7677a1e0b9551fe6`.
- Excluded `Assets/Resources/Sprites/Environment/spore_shambler.png`:
  `42c502b18880355f39df513248b17ab26ad61cff36d42f65f6832a283d715e42`.

Direct cached square sprites inside the existing16×24 world grid avoid padded
PNG derivatives, extra atlases and source metadata edits. Their square reader
aspect is intentional, as in the delivered Stump square-body reuse. Existing
Blender/source art, all three old PNGs/metas and the legacy presentation remain
unchanged. Current source shapes look coherent for a trial; green foliage/Fen
water contrast, small-size recognition and merchant-vs-column distinction still
need real gameplay evidence. Source inspection cannot accept those gates.

## Actual spawn evidence and fresh baseline targets

The retained `Verification/Live16x24/grovelands-baseline-census.json` is an
**earlier** actual Editor census, not the forthcoming Maw-package baseline:

| Existing observed graph | Earlier visible owners / coordinates |
|---|---|
| Overworld.4.7.0 Grove | Shambler21,4 and14,6 |
| Overworld.4.8.0 CompostingField | Shambler67,21 and78,18 |
| Overworld.0.8.0 TendrilFen | Mosshulk14,2; ChoirTendril9,16 and19,17 |

Those seven observations motivate the finite kit; they do not promise the same
positions, health, relationships or all seven survivors on the current earned
save. Earlier census `keys[].id` stores a blueprint label, not a runtime owner
ID. Root must record fresh exact owner IDs/cells/HP/services/current graph and
capture all three body/readers before production. The old four-formation material
route remains reference evidence, not new actor acceptance.

Source spawn anchors:

- `PopulationTable.cs:641–674`: GrovelandsTier2/3. All37 ordinary authored
  Grovelands candidate addresses in the previous source preflight are tier3;
  the table is not a guaranteed list of simultaneous encounters. The actual
  surface comprises four formations: Grove, FruitingWall, CompostingField and
  TendrilFen (`GrovelandsCompositionPlan.cs`; prior material preflight).
- `GrovelandsFormationBuilder.cs:263–281`: Fen resident placement occurs after
  reachability repair, tries1–3 ChoirTendrils, and keeps only placements that
  preserve routes. Do not add decoration or relocate residents for a screenshot.
- `PopulationTable.cs:860–875,918–932,1055–1066`: CaveTier3 Mosshulk,
  JungleTier3 ChoirTendril and GrovelandsLairGuards Shambler/Mosshulk. These are
  source placements only, not newly visited/accepted areas.
- `ChoirCathedralBuilder.cs:135–145` attempts up to3 Choir residents;
  `LandmarkBuilder.cs:198–219` maps a living-underground Ziggurat stamp to Choir.
  Special-site palettes/composition and all associated owners remain separate
  coverage obligations even when the same exact living sprite is portable.

From the last recorded Maw Grainfield world2,2, source-coordinate navigation to
Grove4,7 is2east/5south, or Fen0,8 is2west/6south. This is a candidate route,
not a claim that root already travelled it or that it is safe. Preserve the
actual new package checkpoint first; use normal map/local controls. Revisit
Grove/Compost for Shambler and Fen for the heavy creature and talk/trade owner.
Capture surrounding forest, paths, water and prop clutter at normal zoom, not
only enlarged portraits. Do not count fog as empty/bare content or invent extra
population to pass fullness.

## Minimal implementation and essential-state boundary

Propose one small `FlatPixelGrovelandsActorsCatalog` companion for these three
identities, following existing fauna cached Sprite/Tile ownership and independent
optional-resource injection. One original16×24 texture plus two borrowed16×16
textures is sufficient; no new generic framework or all-world admission switch.
Use current public world/authorized-subject routes, with known-role refusal
remaining authoritative so older loose selectors cannot bypass exact guards.
Reuse the existing single dispatched Render-event color cue. Body/portrait
queries must not advance AI, trade, effects or commitment, nor dispatch another
Render event. Cache only the required pairs; hot queries should not allocate.

Require exact live Creature identity, canonical glyph and owned current body,
render/physics/brain/positive HP, one registered physical cell and no carried or
equipped custody. Preserve existing reskin/variant and current graph/disclosure/
selected-owner gates; these three are portable actors, not region-floor admission.
For Shambler retain the owned CommittedMelee part and real intent service; for
Choir retain owned Conversation/Trader/Inventory. Do not require original
HP/maxHP, disposition, stock count or starting70drams. Avoid a fake “neutral
Fungal” material predicate: all three inherit Flesh. Essential damaged/Wet cues
must remain visible and the same living identity must not cover a corpse.

CommittedMelee is a gameplay state machine, not a request for new animation:

1. Real `TryBegin` requires a current living hostile first impact on an eight-way
   ray, no active conversation/no-fight/action block, and records origin/direction.
2. Existing amber cues expose only currently disclosed threatened cells and stop
   at physical barriers/first creature. Hidden intermediates cannot reveal a
   farther cell (`CombatIntentReadout.cs:37–52`). `ZoneRenderer.cs:1782–1786`
   calls the shared intent renderer under the real input-state permission gate.
3. Real resolution follows the saved ray rather than retargeting; recovery has
   a real DV−2 exposure. Movement, death or applied blocking effects cancel/clear
   through established events (`CommittedMeleePart.cs:68–124`). Keep all behavior.
4. Native Shambler body remains identifiable through idle/windup/recovery while
   the existing line/readout conveys intent. Only if an actual comparison shows
   those cues are insufficient should a bounded essential pose be considered;
   do not start optional directional or breathing animation first.

Choir's talk/trade identity is equally essential. Use real conversation data
`Assets/Resources/Content/Conversations/RotChoir.json` (`ChoirTendril_1`) and
`LootTables.json:3015` (`ChoirStock`) in meaningful integration tests. TraderPart
wires stock/purse at ObjectCreated and existing restocking uses a mutable shelf;
rendering must not reroll stock or change relationships. Capture normal dialogue
and the trade screen if naturally reachable; a refusal or unavailable service
must be recorded as observed, not fixed by granting currency or changing faction.

## Proposed native RED and actual comparison gates — not executed

Keep the tests bounded and use the existing real factory/fixture/painter/reader
patterns. Run genuine behavioral RED before any production extension. Minimum
paired invariants:

- Three exact bodies/current selected world tiles and authorized readers agree;
  one ordinary and one nonlocal current graph positive. Counter shared display
  name/SporeShambler/MycelialColumn/corpse provenance, explicit reskin, duplicate/
  foreign essential part, hidden/memory/unexplored/stale graph, custody and real
  multicell footprint. Mutating legitimate HP/name/stock/disposition retains art.
- Two original square resources and one native cell keep exact rect/pivot/PPU,
  independent null/malformed fallback, immutable borrowed textures and owned
  pair-only disposal. Missing one resource must not disable either other owner.
- Submitted full/dirty movement/removal retains **nonnull real underlying ground**
  and clears old body/reader claims. Damage and successfully applied Wet retain
  the established one-event cue and neutral recovery; reader/neighbor queries
  do not consume flash frames. Restore all scoped hooks/factories/FX/save state.
- Real Shambler `TryBegin` → fixed-ray resolution/recovery with native body plus
  visible intent text/chevrons; counter hidden actor/intermediate cell, blocked
  ray, cancelled/moved origin and idle/recovered state. Do not merely assign
  `Stage` then claim actual AI/combat acceptance, or run simulation in queries.
- Actual Choir Chat/trade identity survives native body selection and a bounded
  trade/stock mutation. Test existing SpeaksToHostiles behavior as applicable,
  not a new friendliness guarantee. Missing/foreign service ownership must not
  silently masquerade as the valid living merchant; exact default purse/stock
  is not a body test. Snapshot conversation/trader state in teardown.
- Separate real death-created corpse never inherits the living body. Corpse
  harvest product art remains independently scoped; no corpse/combat success
  claim follows solely from a catalog lookup.

After GREEN and independent review, replay the fresh matched baseline through
normal controls: all three readers/world views, at least one real Shambler
commitment with clear line/recovery, approach/movement/status where reached,
Choir interaction, ordinary travel, save and fresh Continue. Preserve current
material/water/ground consistency and readable traversal; record remaining
unmapped props/actors and full-view composition honestly. Profile a bounded
actual route without claiming instrumented Editor timings as packaged FPS.
Push reviewed exact owned source plus same-commit living records; fresh Mac
ordinary smoke/restart and Linux archive verification follow. Linux execution,
physical Deck and broader area polish remain separately unverified until done.

## Self-review and continuation

- 🟡 Corrected false reuse/anatomy/material/merchant premises above before code.
  Fresh baseline and genuine native RED remain required; no production started.
- 🧪 Essential committed intent and living dialogue/trade need actual ordinary
  verification. Source/read-only test outlines are not executed proof.
- ⚪ Rotling, HelmwoodFrog/GroveLanternMoth, named shrine actors, containers,
  pipes/thermal props, corpses/products, crops, special sites and other biomes
  remain queued; this is three-body coverage, not a complete region census.
- ⚪ Separate later serious biome-by-biome and area-by-area polish remains
  required. Whole-chunk fullness, routes, palette and landmark/interior/service/
  hazard clarity need their own actual review; do not promote sprite counts.

Files changed by this preflight: **this new document only**. Tests run: none.
No new source image, runtime, test, build, Unity operation or Git operation.
Next: root-owned fresh ordinary baseline from the forthcoming Maw package;
then Shambler concept/native production and immutable two-source reuse, RED,
minimal integration, actual comparison and delivery. Optional animation stays
behind broad all-world sprite coverage.


## Fresh ordinary Mac baseline —2026-10-27 15:36UTC

Maw source751d9247c fresh package was played and restarted before normal travel to Grove4,7 and Fen0,8. Five actual captures and `Verification/Live16x24/grovelands-actors-before-receipt.json` retain Shambler18,6 HP16 z/Flesh/Hostile; Mosshulk13,2 HP55 M/Flesh/Hostile; Choir19,17 HP80 F/Flesh/Neutral. All three body/portraits remain letters. Earned fixture79turn9700 and fixture80turn9820 retain player40,12 HP23/40. No combat/trade acceptance. Exact registered identity inference is grounded in glyph/material/description/source; runtime owner IDs/current graph must still be confirmed from these earned fixtures in Editor. Native RED and production remain pending. Scene-level empty intervals, glyph-backed props and blocked/fog states remain distinct from actor replacement.
