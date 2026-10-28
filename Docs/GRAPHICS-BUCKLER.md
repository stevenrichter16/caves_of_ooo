# Portable Buckler — original body candidate

**Preflight only, 2026-10-28.** Root may select this after the current [portable seed slice](GRAPHICS-PORTABLE-SEEDS.md). No Buckler art, runtime, tests or acceptance is created by this plan. Existing source audit is reused rather than repeated. Root owns Unity, native execution and publication.

## Value and exact scope

Buckler2700 is already legitimately equipped on Player2695 in earned157/163 and appears in the actual inventory/equipment baseline. No purchase, funding, hidden-owner hunt or new spawn is needed. A shield shape would replace a visible equipment identity gap in its authorized reader and ordinary unequip/drop views. This is one compact item, not an armor-family completion or a way to fill sparse mire terrain.

The two carried seeds were the cheaper immediate reuse win and remain first. Unlike those seeds, Buckler has no approved flat PNG/body route. LeatherArmor already has native coverage; other shields/armor or a new shared prop would require their own verified owner baseline. Existing evidence therefore favors one Buckler over speculative family expansion, while leaving broader scene composition as separate work. Do not include IronBuckler or GroundwireScreen merely because they are also shields.

## Verified source and corrections

Source: `Assets/Resources/Content/Blueprints/Objects.json`, Buckler at10478, inheriting ArmorItem→Item→PhysicalObject.

| Contract or earlier assumption | Source-grounded boundary |
| --- | --- |
| Physical identity | Exact `Buckler`, Item/Armor tags; visible Render baseline`)`, `&w`, layer5. Takeable, non-solid, weight5. Examinable describes a small shield with a raised centre and grip behind. |
| Equipment/gameplay | Owned Equippable.Slot=`Hand`, owned Armor baselineAV1/DV1, Hitpoints10/10, Commerce20. Equipment systems own bonuses/custody; artwork must not equip, block attacks, change stats or spend turns. |
| A generic ShieldPart or mandatory Wood/Metal material is required | None is authored. Buckler has no explicit Material, Composition, Thermal, Repairable or dedicated shield-block part. Do not invent such admission requirements or gameplay promises. |
| Equipment health is an actor-alive predicate | Do not copy Creature/HP>0 guards. Safe condition, stat tuning, commerce, effects and equipment modifications remain gameplay-owned; actual removal/custody determines whether the old presentation is current. |
| Existing shield art can be reused unchanged | No flat shield/buckler resource or FlatPixel catalog mapping was found. `ArtSource/World3D/item-candidates/models/item-Buckler.fbx` is explicitly `candidate-not-integrated`; Village3D `equipment-shield.fbx` is an attachment model. Neither is approved native sprite provenance. |
| Legacy fallback already supplies a body | `EnvironmentSpriteRenderer.ResolveItemBody` has no Buckler mapping. `FlatPixelIncidentalEquipmentCatalog` recognizes armor/hatchet/cudgel and three sidearms, not Buckler. Current flat-reader/ground acceptance still needs actual baseline captures; inventory text alone is not a native-body proof. |

## Small original-art pass

First capture the actual equipped Buckler reader, then normal unequip/drop/step-aside and ground reader on one preserved earned branch. OpenMire has a real Bandfrog threat: do not label it safe or disable AI. Use the current player/approved item art as scale references for one concept and saved parts critique, then author original editable integer-pixel Blender geometry; never shrink/trace generated reference pixels or repurpose the3D candidate as accepted art.

Proposed resource: `Sprites/Live16x24/buckler`, one transparent16×24 cell, centered pivot/16PPU, binary alpha, Point/Clamp/no mipmaps/uncompressed, no collider. Keep a compact round shield face, readable rim and raised central boss conveyed by restrained flat highlights; leave the unseen rear grip unseen. A roughly12–14pixel-wide silhouette with baseline22 is a trial target, not a pre-approved final shape. Muted face/rim/boss values should distinguish it from a coin, plate or tower shield without asserting an unprovided material. No glow, heraldry, permanent damage, straps implying a second item, attack pose or animation. Preserve all approved art. Save named cells/palette/editable source, native/contact verification and exact relevant published source pins; no foreign metadata baseline or duplicate archive tree.

## Minimal integration and tests-first gate

Extend the existing incidental-equipment catalog with one independent optional Buckler texture. Preserve existing2/3/6-input constructors by forwarding null; a new seven-input overload appends Buckler. Default construction loads the new resource; explicit null/malformed input must not reload it or disable previous equipment. Cache/own only the Sprite/Tile views and borrow the texture.

Use a distinct exact-Buckler branch: genuine Item, unique owned Render/Physics/Examinable/Armor/Equippable, Hand slot, valid optional positive stack and single-cell/current footprint. Exclude actor/corpse/terrain/crop/service/container meanings and other blueprint aliases and explicit VisualID/VisualVariant/GlyphVariants reskins. Preserve the existing equipment Render-event cue policy rather than pinning runtime tint or original AV/DV/HP values. **Do not send Buckler through `ForeignSidearmMeaning`: that weapon-only check correctly rejects ArmorPart.**

Give exact Buckler authoritative handling in all four existing Greybox world/subject seams, following the sidearm precedence pattern. Missing art or semantic refusal must not regain native art through local fallback. Caller-owned current graph, disclosure, top selection, fog/memory and authorized reader custody remain unchanged. An equipped Buckler has an authorized subject portrait, not a second ground owner or a new player attachment.

Write a small focused native RED fixture before production: exact equipped/carried/ground portrait and rect; real unequip/drop/pickup/equip with stale-document/dirty-paint retirement; independent null/malformed input and old-constructor compatibility; wrong/foreign essential parts, reskin and unrelated IronBuckler controls; mutable equipment condition/stat/price and one existing Render cue without a second event; borrowed texture lifetime. Reuse existing sidearm/incidental/global custody, fog and unrelated-role regression coverage instead of copying it wholesale. No new shield combat system or damage experiment is needed.

After GREEN, root compares actual reader/world scale, ordinary custody changes, F5/fresh Continue and exact owner2700 persistence. Independent source/art and actual-image review remain separate gates. No package, platform, FPS, whole-area fullness or55+5polish claim follows from this plan; group delivery sensibly with other verified small work.
