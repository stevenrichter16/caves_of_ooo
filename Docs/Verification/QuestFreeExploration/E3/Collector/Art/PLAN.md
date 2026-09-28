# F6 Tatterjay presentation — verified private plan

Status: root authorized original model/animation work. Source audit and API agreement precede art/renderer implementation. No shared Assets or Unity work; root owns blueprint publication, imports and native execution. CoO-original design, no parity claim. Existing Magpie gameplay/art remains unchanged.

## Frozen owners and visual intent

Root's exact live blueprint is Tatterjay, inheriting Creature (not Magpie), Props Anatomy=Avian, Render j/&c, passive Villagers, HP8/Strength6/Agility14/Toughness6, XP0, Inventory MaxWeight20. No explicit natural weapon or BodyNaturalAttack tag. TatterjayCorpse inherits CreatureCorpse, %/&c; normal CorpsePart writes actual SourceID/SourceBlueprint. The root still owns corpse chance/data. Visual: broader blue-green/dark-teal chest/body, pale bill/chest, asymmetric ragged tail. Original Avian silhouette and retained feet/wings/head, not a renamed magpie or a generic scrap-bearing bird.

Use the exact approved 24-color palette for body/remains, with no extra palette assets. One independent optional resource pack, SpreadCollector3D/Library, owning spread-tatterjay and spread-tatterjay-remains, avoids touching existing three-species source/importer. The model has an authored Head-owned Collector.Bill socket. Existing Magpie source has EIGHT named bones (Root, Body, Head, Wing.L/R, Leg.L/R, Tail), not nine, and no sockets. New bird may retain that coherent eight-bone anatomy, with a nondeforming owned bill socket.

## Pre-implementation correction log

| Premise | Verified source and implication |
|---|---|
| An avian can use humanoid hand attachment | AnatomyFactory.cs:298–315 has head/face/back, paired feet/wings, tail, thrown/floating slots. Village3DEquipmentViews validates actual equipped slots; collector items are carried. Use a separate exact bill attachment, never synthetic equipped gear. |
| Existing Magpie art can simply gain a socket | ArtSource/SpreadBiome3D/build_actors.py:14–33 authors eight bones/no socket; SpreadBiomeActorBuilder expects the exact existing socket-free three-pack. New independent pack preserves those contracts. |
| Inventory edits always notify equipment version | InventoryPart.AddObject/RemoveObject owns inventory changes, not EquipmentChangeBus. A carry view must requery the tiny exact CurrentCarriedItem property, not rely only on equipment events. |
| Pickup gesture can use EmitInteraction | EntityVisualHooks.IsCurrentInteraction requires both participants in current ground cells. A committed picked item is in inventory. Observe this role's saved committed phase transition; do not send fake ground interaction/attack. |
| A stopped trip is an empty beak | Home full/locked/destroyed may stop the trip while leaving the real target carried. CurrentCarriedItem, not Phase alone, owns presence. |
| One generic scrap mesh can stand for all goods | Actual rolled targets are whole Hatchet/Cudgel/LeatherBoots owners. SpreadPortable3DLibrary.TryRecipe+Find supplies their exact approved persistent mesh/material. Attach that actual source with an explicit per-form bill grip. No currency, generic scrap, new loot or ownership fiction. |
| A loaded Carrying phase proves a new pickup event | First bind/load shows current carried state only. Pickup/deposit gestures require a later observed live committed transition on the same exact actor/part. |
| All child renderers belong to body style proof | Presenter AddView captures body renderers and collider bounds before equipment. Keep carried child separate and expose its own proof. Do not broaden the approved body mesh count or actor picking to the carried inventory owner. |

## Owned behavior / APIs

Standalone owns SpreadCollectorPart.Configure(Zone,Entity home,Entity target), saved exact Home/Target and phase Seeking/Carrying/Deposited/Stopped. CurrentCarriedItem must return only actual Target in this live current actor's Inventory, proper Physics.InInventory, never equipped/stale/dead. It remains valid after Stopped, recruitment, foreign receiving-zone travel, passive changes and legitimate positive quantity/metadata/blueprint mutation while the exact item is still carried. Duty collection/deposit authorization is separate from this observed ownership. Root owns receipt budget/placement. No new general source search, hoarder behavior or gameplay event is needed.

Proposed optional SpreadCollectorArtLibrary follows current exact Spread owner/recipe contract, rejecting custom appearance, hidden/stale/foreign/same-ID clones and malformed current part/backlinks. Remains require actual source blueprint and nonempty SourceID. Catalog/library/style/Voxel lookup adds only the two owned namespaces. Missing optional pack retains honest fallback rather than breaking ordinary scenes.

SpreadCollectorCarryView owns only a child instance under the actual cloned bill socket. Mesh/material are borrowed persistent portable assets; actor/item/source prefabs, native inventory and geometry remain untouched. It caches exact actor/part/item references and last committed phase. Sync reads those references with zero zone scans, no Resources.Load or new arrays every frame. Only changed identity/state creates/destroys an instance. Current scope/actor visibility owns draw state; removing source, losing actual carried item, death, rebind/load replacement, RemoveView, Release and component disable clear owned presentation references. No new carried-item collider or independent selection; world actions remain on the native actor.

Animations: Idle, Walk, Interact, Attack, Hit remain available for native presentation symmetry; Attack is fallback animation, not a new weapon/AI behavior. Additional CarryIdle/CarryWalk hold a sensible bill pitch; Pickup and Deposit provide distinct committed transfer gestures. Play maps only Idle/Walk for the exact current collector carry. Gesture expiry returns through mapped Idle. Hidden/removed transitions do not replay when the actor becomes visible. First/reloaded carrying does not fabricate Pickup. Avoid gesture queues or a new global animation system.

Three grip transforms explicitly anchor the actual Hatchet/Cudgel/LeatherBoots model to its natural handle/upper region. The entire one-owner whole-stack remains one view; rendered geometry does not imply a new quantity. Scaling is view-only, bounded and measured in native camera. All grip variants must be readable and visibly attached to bill under movement, pickup/deposit and carry poses. Ground/drop presentation remains the ordinary exact portable resolver.

## Test-first implementation sequence

1. Original source tests before generator: closed two-source IDs, exact palette, distinct bird anatomy, connected parts/no exposed coplanar overlaps, finite bounded boxes, real head-owned socket, named animation transforms and low avian remains. Author source, then export isolated FBXs and review front/side/three-quarter images. These are source studies, not native acceptance.
2. Compile-compatible native missing-library tests after root publishes exact blueprints/role: current owner/body/corpse selection, five ordinary plus four carry/transfer clip bindings, actual deformation/head motion, owned socket, shared palette and foreign/hidden/spoof controls. Root records actual RED before shared production.
3. Exact carried view tests use role-owned real pickup/deposit, not invented CurrentCarriedItem overrides. Counter-pairs cover three actual goods, equipped/other-inventory/stale/unknown/currency refusal; mutation/loss/death; Stopped-but-carried; fresh replacement load; initial Carrying with no pickup; live phase gestures; source buffers unchanged; pooling/rebind cleanup. Independent body style proof remains unchanged, carry proof requires exact actual mesh/material/current item.
4. Root imports and runs native tests, then a bounded actual world-camera pickup/carry/deposit sequence on a generated opted-in collector where available. An unavailable source stays unverified. Gallery stages are clearly disclosed. Inspect actual body silhouette/bill contact/item orientation under all gestures and full field visibility. No ordinary discovery or uninformed player awareness claim from autonomous observers.

## Performance and acceptance limits

One optional per-view handle, exact direct references and at most one attached renderer. No zone scan per frame, no repeated resource loads, no world-recipe cache or inventory mutation. Frame-time claim requires measured native window; source reasoning alone only establishes bounded operations. Reuse existing actor/carry evidence and root-controlled camera cleanup. New art availability does not extend receiving biome authority.

Q1–Q4 pending implementation/native acceptance. Concrete risks to test: phase changes during callback removal, loaded stale same-ID item, full home retains real carried item, array/body evidence accidentally includes carry child, and animation source socket import/local basis. Root generation owns finite source budget and save behavior; renderer must not repair malformed graphs or mint cargo.
