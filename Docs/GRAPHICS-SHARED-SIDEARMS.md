# Shared Dagger, Short Sword and Mace coverage

**Delivery update — 2026-10-28:** Source0b888da80 is verified on origin/main. Fresh Mac/Linux packages include this family. Actual Mac identity captures, restart comparison, archive verification and preservation are recorded in [the delivery report](Verification/Live16x24/packaged-village-roles-delivery.md). Earlier pending-package statements below are historical.

Status: source integrated;108 native cases pass; bounded actual Editor verification complete; source a15ae3a95 published and remotely verified. Specialist source83e1e568f and delivery7f38536a4 are verified on main; current packages remain that verified batch until a later coherent delivery. Current packages exclude this pending source batch.

## Why this family

These common shop and equipment identities still use glyph fallback in the flat presentation, while distinct 16×16 sprites already exist in the legacy environment set. Actual Sill shop views expose all three and earned Weaponsmith fixture143 retains Dagger7359. Reusing correct approved pixels closes frequently encountered gaps without spending the visual budget on a blind NPC search, a cave packet or optional animation. This does not fix whole-chunk fullness.

## Source corrections before implementation

- Existing resources: `Sprites/Environment/item_dagger`, `item_sword`, `item_mace`, each16×16. Preserve their pixels and original import metadata; create complete square runtime views without stretching to16×24.
- FlatPixelIncidentalEquipmentCatalog currently covers LeatherArmor, Hatchet and Cudgel; LongSword has its own existing portable route. Add only exact Dagger, ShortSword and Mace, with independent optional resources and backwards-compatible injected constructors.
- Require genuine owned Render/Physics/MeleeWeapon/Equippable/Examinable parts, takeable non-solid Item meaning, Hand slot and current single-cell footprint. Reject foreign semantic owners and custom visual IDs/variants. World graph/disclosure and carried-reader custody remain caller-owned. Preserve mutable weapon, condition, repair, heat/effect and equipment state.
- ShortSword lacks Repairable/Composition parts; do not require them. Dagger/Mace have them, but neutral body art must not encode pristine condition.
- Battleaxe and Buckler need separate original art; no exact reusable sprite was found. Generic forge is TinkersForge, a passable Furniture/Weaponcraft owner already covered locally in two East zones; it is not part of this item batch.

## Gates and bounds

Collect actual Dagger carried/drop/examine and shop-authorized ShortSword/Mace readers before implementation. Reuse the existing artwork unchanged and document its provenance/critique. Record RED before runtime changes; verify positive routes and authoritative missing-resource, identity, custody, footprint, selected-owner and state counter-cases. Then play matching interactions, save/restart and compare actual pixels, obtain independent review, update living records and push exact owned changes. Batch package delivery rather than rebuilding after each small item.

No new services, loot, balance or geometry. Worldwide observation, remaining items/actors/regions and all55+5 area-polish scopes remain open. Normal saves, approved sources and49foreign paths remain preserved; storage inspection only. Root owns Unity and Git.

## Baseline correction and preserved route — 07:48 UTC

Actual Dagger7359 carried reader and dropped ground slash are recorded. Normal drop60,3→step61,3→ground reader→F5fixture149 (turn1550/HP38), pickup and F5fixture150 (turn1570/HP38) are preserved; full isolation archived at `/private/tmp/coo-sidearms-before-4335-preserved` before Finish. Shop F1 is a text-only trade notice, not a portrait route: ShortSword/Mace notes establish disclosed identity/availability only. A grimoire sale confirmation was cancelled, with no sale. Current99-case specialist evidence is unrelated to the upcoming sidearm RED.

The existing unknown-owner allocation test used Dagger as its sentinel. Since Dagger becomes deliberately known, change that sentinel to still-uncovered Spear after RED; preserve the allocation assertion. Current new23-case test fixture was authored before production. Constructor order is LeatherArmor/Hatchet/Cudgel (16×24), then Dagger/ShortSword/Mace (16×16). Optional resources stay independent. Immutable reuse exception and six protected source/import hashes are documented in `ArtSource/Live16x24/shared-sidearms`.

Native RED recorded23 cases:22 expected failures and1 legacy-control pass, before production edits. Root then extended the existing catalog with independent square sidearm resources and full frames, preserving old2/3input absence, exact identity, unique owned essential parts, foreign semantic refusal and existing caller authority. No approved asset/import bytes changed. Actual after and GREEN remain separate gates.

Independent pre-GREEN review corrected an invalid anatomical `BodyPart` guard to the actual entity `Body` Part, and found a refused Player-tagged Dagger could fall through to opening player art. Exact sidearm refusal is now authoritative in world and shared-reader routing. The first attempted combined run921ba3ff executed stale code while compilation was broken (108 cases,22 old failures); it is not GREEN evidence. Compile must clear before the next run.

Final native combined job46ad6979447e49eaa22366d8013d4049:108/108 pass after current six-input constructor and no compilation errors were observed. Includes23 new sidearm, existing incidental equipment and LongSword positive/adversarial controls. Independent re-review cleared both prior blockers. Actual Dagger7359 now renders item_dagger on ground and both reader routes; F5fixture151 turn1580HP38 is preserved.93.79s actual Editor profile has54592 samples, no wrapping, ZoneRenderer peak25.84ms/MainThread2641.33ms including tools/screenshots; no baseline speedup or Deck claim.

Actual after sequence: Dagger inventory02,ground03/reader04,F5saved05/freshContinue06,pickupreader07. All564000world pixels match. Independent Mace branch reloads earned142(player50,trader300): actual grimoire sale11→player61/trader289; buysMace7361 for55→player6/trader344. Inventory10,ground11/reader12; normalpickup and F5fixture152turn1530HP38. Capture10a is an explicitly retained LongSword control after initially selecting inventory row0. Shop stack values are whole-stack offers; no speculative additional seed/tonic sales occurred. Only these branch trades are claimed.

## Final bounded acceptance and reproduction

ShortSword7360 was purchased normally for41drams from an independent142 branch (player50→9/trader300→341). Captures13 inventory,14 ground and15 ground reader show its own longer guarded blade. Ordinary pickup and F5fixture153 preserve turn1530HP38. This branch and Mace152 are separate alternatives, never a claim that the player owned both in one run. Dagger150→151 is the separate restart route. All actual branches are archived under `/private/tmp/coo-sidearms-{before-4335,after-1a59,restart-d165,mace-1eae,shortsword-c851}-preserved`. Finish verified Play=false, SaveRootOverride=null and scene clean.

Implementation: `FlatPixelIncidentalEquipmentCatalog` resolves exact three identities from independent borrowed16×16 textures, caches complete square sprites/tiles, validates unique essential owned parts and refuses semantic aliases. `FlatPixelGreyboxCatalog` preserves exact sidearm refusal before older opening art in world/reader routes. Existing caller current-graph, fog/disclosure, selected-owner and reader custody remain authoritative. No gameplay commands, content, drop tables, services, geometry, collision or approved pixels changed. Legacy presentation remains available.

| Reference | Classification / bounds |
| --- | --- |
| Existing EnvironmentSpriteRenderer mappings and immutable PNGs | Reused unchanged; source/import hashes pinned. Original editable provenance not located. |
| Qud | CoO presentation extension; no Qud visual or gameplay parity claimed. |
| Authoring pipeline | Existing approved-art reuse exception; no new imagegen or fabricated editable source. |
| Runtime/acceptance |108 native cases and actual Sill gameplay; not all-biome observation, package execution or full area polish. |

Reproduction: launch real GameBootstrap through the existing isolation tool, place the exact Quick pair from150/151/142 under that isolated marker, Continue normally. Root recorded actual keyboard i/Tab/Down/F1, d/drop, a/d/move, c/direction/x/examine, g/pickup, ordinary trade and F5. Never inject player currency, inventory, position or hidden-map queries. Independent review is `shared-sidearms-independent-review.md`; manifest lists all raw evidence with hashes.

Publication gate: exact owned source/tests/docs/reuse manifest/evidence only; foreign49 hashes and all6immutable pins verified. Four generated PNG.meta whitespace rewrites were backed up under `/private/tmp/coo-sidearms-generated-metadata-preserved` and restored byte-exactly. No cleanup. Current Mac/Linux packages remain specialist source83e1e568f; batch these item changes with the next coherent visual delivery. Missing Battleaxe/Buckler/Spear and other item/actor/surface families remain open, as do all55+5 later area-polish scopes. Prefer next work that changes ordinary scene appearance substantially, guided by actual disclosed owners; avoid another cave or optional-motion detour.

Post-play console retained two memoryless-depth capture messages, with no compilation error; this is not a clean-log claim. Root inspected every claimed reader/body and kept mistaken/mid-typewriter baseline captures explicitly labelled.

Storage inspection at08:16UTC showed364GiB available on the project volume. Zero bytes removed; caches and build outputs preserved.

Independent final review found no P1/P2 blocker in source or nine actual item/body-reader views; independently rechecked108 distinct passes,23-case RED, six source pins and0/564000restart differences. This is bounded source/Editor acceptance only.

Publication receipt: `a15ae3a95608aa8114d8d71ab112743957d92d26` equals origin/main at 2026-10-28T08:18:55.155297+00:00. Fetch found no divergence; no rebase was needed.56 reviewed owned files were staged; all49foreign hashes remain preserved. Current packages exclude sidearms. Next compare actual surface terrain/vegetation and shared settlement-role gaps against remaining equipment, favoring the largest ordinary-scene benefit rather than extending the convenient shop queue indefinitely.
