# Grovelands — three living actor identities

2026-10-27, 16:18 UTC. **Three native actor identities are implemented; native
tests and the bounded ordinary Editor comparison have passed. Source push and
fresh packages remain pending.** The current delivered build is still the Maw
package from source `751d9247cf05f55e0baf8f8f8cb02ded0eb931ae`. The final checkpoint
below records 44 genuine RED cases, 44 focused GREEN plus 363 related GREEN
cases (407 distinct passes), real Shambler windup/evasion, Choir talk/trade,
save/fresh Continue and the separate fatal Mosshulk route. No whole-region,
polish, packaged-runtime or physical Deck acceptance is implied.

The implemented scope is exact **Shambler, Mosshulk and ChoirTendril**: one
original native 16×24 Shambler body and two unchanged approved 16×16 bodies.
Three cached sprite/tile pairs serve portable living owners through shared
world and authorized reader routes. Existing committed-melee intent,
damage/status cues and living merchant services retain their actual mechanics.
This is original CoO presentation work; Qud parity is not claimed. AI, balance,
population, stock generation, anatomy and optional animation are unchanged.

The sections through **Native RED** preserve the historical preflight and
implementation contract. Their proposals, pending checks and preflight-only
file list describe that earlier checkpoint, not the current status. The final
implementation and evidence record at the end supersedes those pending gates.

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

## Exact owner and resource contract — historical preflight

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

## Actual spawn evidence and fresh baseline targets — historical preflight

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

## Minimal implementation and essential-state boundary — approved contract

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

## Proposed native RED and actual comparison gates — historical test plan

These checks had not executed when this plan was written. Execution and the
bounded actual comparison are recorded in the final checkpoint below.

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

## Pre-implementation self-review and continuation — historical

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


## Current owned-graph checkpoint —2026-10-27 15:43UTC

Fresh isolated Editor Continue of earned Fen80 confirmed the actual three
registered identities, followed by ordinary map travel to Grove4,7. Read-only
`Verification/Live16x24/grovelands-actors-before-census.json` records:

- Fen: Mosshulk51774 at13,2 HP55; ChoirTendril51741 at9,16 and51747 at19,17
  HP80, each with its own Conversation/Trader/Inventory.
- Grove: Shambler46964 at17,9 and46965 at18,6 HP16, each with its own
  CommittedMeleePart. Player40,12 HP23 in both current graphs.

No owner was spawned, relocated, faction-edited or granted currency. The
isolated session was stopped and its earned root retained as
`fd162865a20f465fa5d73196dc521157-grove-baseline-preserved` before restoration.
Normal save protection passed. This closes baseline identity ambiguity; it does
not establish native art, combat/trade acceptance or area polish.

Implementation contract: one independently optional cached pair per identity,
three-texture constructor ordered Shambler/Mosshulk/ChoirTendril. The existing
shared world/reader selector gets a lazy companion and authoritative known-role
refusal, plus the same opt-in single dispatched Render color cue as Stump fauna.
No hot-query allocation, repeated resource load, extra Render event or simulation
mutation is needed. Verify owned-pair disposal and borrowed texture survival.
Record bounded full/dirty/visibility behavior and actual route timing without
promoting Editor measurements to packaged or physical Deck performance.

Built-in imagegen scene and transparent parts references were inspected. Keep
the small drooped head, crooked former-person trunk and one attached tapered
tendril; reject the large reference clusters, soft alpha, cane-like hooked tip
and retouched terrain/UI. Production is original native integer-cell geometry,
not a scaled reference bitmap. The two approved square bodies remain unchanged.

## Native RED —2026-10-27 15:52UTC

All44 focused native cases were discovered and failed against the old
production: missing Shambler resource/companion and glyph world fallbacks for
the two existing square textures. Exact initial source is retained in
`grovelands-actors-tests-before-implementation.cs.txt`; raw XML is retained in
`grovelands-actors-red.xml`. Root imported the test explicitly and verified the
compiled type before execution. No production resource/catalog existed yet.
Independent review then found a test-isolation gap: actual committed melee can
invoke WorldAudioCombat.Callback; save/neutralize/restore that hook before
GREEN. This fixture-only correction does not alter the44 observed RED cases.

## Final implementation and bounded Editor acceptance — 2026-10-27 16:18 UTC

The three selected living owners now have native world and reader identities.
`FlatPixelGrovelandsActorsCatalog` caches one centered 16×24 Shambler pair and
two centered 16×16 pairs at 16 PPU. The new constructor takes independently
optional textures in Shambler/Mosshulk/ChoirTendril order. A failed resource or
unsupported owner retains truthful fallback without loading a substitute.
Only catalog-owned sprites/tiles are disposed; borrowed textures are unchanged.

The implemented branch retains the original contract:

```csharp
// Caller retains current graph, disclosure and selected-owner authority.
public FlatPixelGrovelandsActorsCatalog(Texture2D shamblerTexture,
    Texture2D mosshulkTexture, Texture2D choirTexture)
{
    shambler = Build(shamblerTexture, 24, "Shambler");
    mosshulk = Build(mosshulkTexture, 16, "Mosshulk");
    choirTendril = Build(choirTexture, 16, "ChoirTendril");
}
```

The Greybox companion uses exact known-role refusal, the existing authorized
reader path and the existing opt-in single Render-event color cue. Queries do
not dispatch additional Render events or advance combat, AI or trade. No region
ground gate or gameplay source was broadened. Surviving damage, commitment,
status, legitimate name/disposition changes and mutable merchant stock/purse
preserve the supported living body. Corpses remain separate owners.

The new Shambler was authored from the actual baseline and two retained
imagegen references as editable integer-cell Blender geometry: **16×24,
95 opaque pixels, seven colors**, with feet on row 22. The source PNG and runtime
PNG have SHA256
`e0d8be101e58460fb1a2e45503a2f7b1a729be838466a7468ec8d488a95e3287`.
The source verifier matches all 384 RGBA cells; a retained Blender inspection
matches the 38 named mesh clusters/95 unit quad faces. Mosshulk and ChoirTendril
reuse their approved PNGs unchanged, with no padded or enlarged derivatives.
The excluded SporeShambler image is also unchanged. See the
[source/native review](Verification/Live16x24/grovelands-actors-independent-review.md)
for exact hashes, protected inputs and the distinction between independent
source checks, test-author self-review and root-executed native tests.

### Test evidence and review corrections

| Execution | Result | Evidence |
| --- | --- | --- |
| Genuine pre-production RED | 44 discovered, 44 failed | [grovelands-actors-red.xml](Verification/Live16x24/grovelands-actors-red.xml), 15:51:57–15:52:01 UTC |
| Final focused GREEN | 44 passed | [grovelands-actors-green.xml](Verification/Live16x24/grovelands-actors-green.xml), 15:55:50–15:55:55 UTC |
| Related renderer/domain GREEN | 363 passed | [grovelands-actors-related-green.xml](Verification/Live16x24/grovelands-actors-related-green.xml), 15:56:17–15:56:41 UTC |

The final groups have **407 distinct test identities**, zero overlap and zero
failed, skipped or inconclusive cases. Earlier runs are not added. The original
RED fixture is retained as `grovelands-actors-tests-before-implementation.cs.txt`.
The only post-RED fixture correction snapshots, neutralizes and restores
`WorldAudioCombat.Callback`; no assertion or case count was weakened.

The focused cases exercise actual factory owners and the submitted painter,
authorized readers, resource independence/disposal, foreign/custom/dead/custody/
multicell/disclosure refusal, movement/removal over real nonnull ground, actual
damage and applied Wet, separate death-created corpses, real committed-melee
begin/resolution/recovery/cancellation, and real Choir conversation/trade state.
These test results do not themselves prove visual clarity or ordinary acquisition.

### Ordinary controls, changed state and restart

The [final actual receipt](Verification/Live16x24/grovelands-actors-after-receipt.json)
binds current owners, controls, purse/stock changes, saved fixture, capture hashes
and restoration. It records the working source base as
`6f85e167b73b751a80874de7f3d260b48589d4e7`; the new actor source commit is still
pending. The main comparison has **13 distinct readable views: 01–10 and 12–14**.
Capture 11 is fully readable and byte-identical to10, retaining the unchanged
trade screen after cancellation; it adds no distinct view or purchase. Capture 15 records the separate fatal extra route.

| Observed action | Bounded result |
| --- | --- |
| Grove world and reader, captures 01–02 | Exact Shambler 46964/46965 appear as the new body; their source identity remains distinct from SporeShambler. |
| Real windups and ordinary Up sidestep, captures 03–05 | Both Shamblers entered Windup on fixed rays toward player 23,9. One Up moved the player to 23,8; both entered Recovery, the threat cleared and player HP remained23. This proves ordinary evasion and readable existing intent/recovery, not a landed Shambler hit or a new attack animation. |
| Fen and two readers, captures 06–08 | Exact Mosshulk 51774 and ChoirTendril 51741/51747 retain their separate native identities in the existing material/water scene. Mosshulk movement was observed. |
| Choir chat and trade, captures 09–10 and 12 | Neutral Choir 51747 at 19,17 provides its real conversation and stock. The first confirmation was cancelled by Escape; direct controls/state support that cancellation, and image11 repeats the unchanged50/100 trade screen byte-for-byte. A second confirmation with Y bought VenomGland for 18 drams: player 50→32, merchant 100→118, stock 3→2. No currency or faction grant was used. |
| F5 and fresh Continue, captures 13–14 | Earned `Fixtures/grovelands-actors-after81` saves turn 10290 in Fen0,8, player 20,16 HP23. Fresh Continue restores the same merchant, remaining stock and purses. All 639,200 RGB pixels in the 940×680 world rectangle match exactly between saved/restarted captures. This is a bounded world-view comparison, not equality of every UI pixel or all saved state. |
| Separate extra route, capture 15 | After the successful restart, Right4/Up2/Down2/Left4 ordinary keys led to Mosshulk combat. A left-hand miss was followed by a right-hand 23-damage hit that killed the isolated player at turn 10398. This is a real fatal encounter, not a successful safe walk or a combat-balance change. The living after81 fixture at 10290 was not overwritten. |

All three isolated roots—baseline, after and restart/death—were archived before
finishing the isolation. The final receipt reports clean Edit mode, no Play,
compile or pending isolation, no save-root override, unchanged normal Editor
and standalone saves, and the older 38 foreign files preserved. No cleanup or
deletion of those roots was performed.

The [bounded profile](Verification/Live16x24/grovelands-actors-profile.json)
covers 60.091109 seconds and 15,990 samples per listed recorder during the extra
route. It includes the combat/death transition, idle frames and MCP/CUA overhead.
There is no matched pre-change profile. It cannot establish FPS improvement,
sustained package performance, Linux execution or physical Steam Deck behavior.

### Files changed and remaining gates

| File or exact packet | Change |
| --- | --- |
| `Assets/Scripts/Presentation/Rendering/FlatPixelGrovelandsActorsCatalog.cs` and `.meta` | New finite three-body adapter, cached native geometry and owned-pair lifetime. |
| `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs` | Lazy companion, authoritative known-role decisions, shared world/reader/cue integration and disposal. |
| `Assets/Tests/EditMode/FlatPixelGrovelandsActorsTests.cs` and `.meta` | 44 focused behavioral cases and scoped audio-callback restoration. |
| `Assets/Resources/Sprites/Live16x24/shambler.png` and `.meta` | One original Point/Clamp native body; no edits to the two approved borrowed bodies. |
| `Tools/Live16x24/build_shambler.py` | Editable integer-cell native source and verification workflow. |
| `ArtSource/Live16x24/grovelands-actors/` | Exact prompts/tool results and references, scene/parts concepts, critique, `shambler.blend`, `source-cells.json`, native PNG/contact, manifest, protected-input and source/Blender verification receipts. |
| `Docs/GRAPHICS-GROVELANDS-ACTORS.md` | Historical preflight retained; final implementation, tests, actual outcomes, limits and continuation recorded here. |
| `Docs/Verification/Live16x24/grovelands-actors-*` and `Fixtures/grovelands-actors-{grove79,fen80,after81}` | Before/after receipts and captures, original failing fixture, RED/GREEN XML, review, bounded profile and earned fixture evidence. Related global coverage records are updated separately in the same source delivery. |

- 🟡 **Resolved:** wrong-body/anatomy/material/service premises were corrected
  before implementation; audio-callback test isolation was corrected before
  GREEN. Source/runtime cold review reported no remaining significant defect
  within this finite adapter scope.
- 🧪 **Evidence limits:** native tests establish damage/Wet/hostile-chat and
  actor-corpse behavior; this ordinary route did not separately visually accept
  every such cue. The actual Mosshulk victim was the player. Source/contact and
  test success do not accept every Grovelands owner or special-site appearance.
- ⚪ **Open visual debt:** broad quiet center intervals, remaining letter/glyph
  props and conversation/faction-label clipping remain named coverage or polish
  work. No full chunk, biome or area is promoted to polished by these bodies.
- ⚪ **Delivery pending:** review/stage/push the owned source and living records,
  then build and exercise a fresh Mac package and verify a fresh Linux archive.
  Until that delivery, the previous Maw package remains current. Physical Deck
  testing remains unverified. Continue broad missing-family coverage before
  optional nuanced animation; the next bounded recommendation is
  [ordinary shallow cave materials and travel](Verification/Live16x24/post-grovelands-next-gap-plan.md).

There is no gameplay scope expansion from the approved plan. The deliberate
production split remains one newly authored body plus two immutable source
reuses, with essential cues supplied by existing systems.
