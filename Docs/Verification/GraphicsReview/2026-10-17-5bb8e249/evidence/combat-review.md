# Native route visual review — fresh 2026-10-17 captures

These are actual native GameView PNGs, copied byte-for-byte from the fresh run directories. All 15 selected images were individually opened and inspected. Selected resolution is 1280×800, verified from every PNG header. These runs use queued input through production handlers; they are not a physical Steam Deck test. No repository source, scene or asset files were edited for this review.

## Source and capture conditions

Source commit is `5bb8e249d1fc69d9213f684bace7a8628e11b4ad`, Unity 6000.3.4f1 OSXEditor, seed64. ReferenceGlade routes open `Assets/Scenes/Main/ReferenceGlade.unity`, whose serialized gameplay zoom is0.82 and RevealEntire3DZone isfalse. The shared route checks both reveal flags false before the selected captures. Root selected the existing1280×800 GameView size after launch. Route reports do not emit per-frame graphics, camera, numeric light, exact UTC or a full visible-owner census. Unknowns remain null in the JSON entries; PNG modification times are explicitly file-write times, not asserted exact shutter timestamps. The source baseline is not a substitute for a per-frame settings receipt.

The throwing route instead opens SampleScene, whose serialized gameplay zoom is1.2 and RevealEntire3DZone istrue. It also replaces the visible world with a fixture. Its black surrounding area cannot demonstrate ordinary fog or world contrast. The per-frame reveal flag is not recorded.

Raw reports and all original PNGs are retained under `evidence/routes/<route>/<runId>/`; selected copies are in `screenshots/controlled` or `screenshots/staged`. The aggregate metadata fragment is `evidence/native-routes-entries.json`, with individual same-ID metadata JSONs alongside this note. Every raw report retains its original canVerify/cannotVerify prose; the actual completion fields and failed checks take precedence over those generic route descriptions.

## Combat — two selected controlled images

Run32439dfbf1074f539606b48754ef5eff, 10.98seconds. Ordinary starting actor/kit, no transfer, grants, AI suppression or direct damage calls. One original Rime Grip cast, no tonic. Contact and aftermath use the authored northern scrabbler encounter; the real scheduler and unrelated NPC fights remain active.

- CMB-01: readable full HUD, ordinary green native terrain and tiny player/enemy near the upper edge. The log primarily shows an unrelated NPC fight at this instant. It is a contact/composition frame, not proof of a strike animation.
- CMB-02: clear player-kill, freezing, XP and owned-item-drop log. Player HP32/40 and Rime cooldown visible. Native corpse/original drops are correlated by the route report.

The route is **incomplete**: its attack-pose/frame assertion failed, and no03-real-player-attack PNG exists. Actual attack, damage, retaliation, kill and original drop checks pass. Do not report animation verification or use a historical03 screenshot to fill the gap.

## Ordinary cave crop — two selected controlled travel-shortcut images

Run07b9269e886e43fe80e1849ca884d1e6, 5checks passed in7.38seconds. One explicit player transfer to unchanged generated connected caveOverworld.12.4.5 at67,18; source LampveinCrop21661 at66,18. No material, stage, light, HP or stat grant. All subsequent harvesting, pickup, stow and drop are ordinary key-driven actions.

- CAV-01: actual ripe crop examine. Native portrait, current-state copy, hand-harvest instructions,64moist rounds and portable-light use are readable. Dense copy fits the popup. The popup obscures much of the cave.
- CAV-02: actual harvested LampveinFan21666 lies at66,18 underneath the player after pickup, automatic equip, native unequip and drop. The local light pool, native actor and rock walls are visible. The report confirms the enabled LightSourcePart and that recomputed brightness at the cell is greater than while the fan is stowed; it does not store numeric brightness values.

This is a crop-to-ground-produce state change, **not** a matched before/after light pair: CAV-01 is an examine overlay with the actor on the adjacent cell, and the unlit stowed state was not photographed. Ground light is from the harvested dropped item, not an independently observed environmental lamp or artificial lighting override.

## Repair field and live danger — four selected controlled travel-shortcut images

Run023c82b599ed4893b17390cc482c3cf2, west-of-Morrowfast fieldOverworld.2.6.0, authored map biomeGrovelands. One actor transfer to the real generated field. Local gathering, repairs, drinking and gate operation are ordinary inputs; NPC scheduling is active. Two exact original tonics were consumed, no healing injection.

- WRK-01: visible beds, plants, two wells and damaged frame. The olive palette and large native objects contrast the dark green opening.
- WRK-02: real cracked clay-lined well5232 examine; masonry and two fire-clay cost are clear. Portrait and popup fit. Duplicate description in the right log is visible.
- WRK-03: restored gate5234 closed; focus offers open door, log confirms closing. Real rotling attack has reduced HP to11. Large red **Bleeding** floating text is legible, although it covers the immediate interaction area.
- WRK-04: same player cell63,13 and gate now open; focus offers close door, log confirms opening. Red **2** damage and HP9 visibly demonstrate ongoing combat. Vegetation/player obscure some gate detail.

The gate pair shows a genuine paid open/closed change under danger, not a clean static art comparison. The route is **incomplete**: the next crop approach stopped at the >10HP safety check. It did not perform its intended knotflax crop harvest or final save/load checkpoint. Generic report canVerify wording includes those planned actions; the actual failed receipt must not be described as completing them.

## Connected Spread lair — two selected controlled travel-shortcut images

Run71cf42abd0f444f3b9fa99db3fe03d08. Four transfers across its whole sequence; two original Calm casts earlier; no grants, removed threats or forced models. The lair source and first floor are generated for a labelled source preflight. The two selected frames showOverworld.8.12.0 surface at44,12/tick1280, thenOverworld.8.12.1 at36,12/tick1290 reached through ordinary Shift+Period.

- LAI-01: real surface stair, green wall enclosure, brown native creatures. Ordinary occlusion leaves much of the frame black.
- LAI-02: real paired up stair under the player, chest and native MarlbackWallkeeper two tiles away. Underground and surface use similar green materials; the HUD saysBright underground, which must not be interpreted as outdoor sunlight. This is a location pair, not a bossfight.

The entire route is **incomplete** at its final east-cell safety precondition during a movement profile. All earlier real door, stair descent/ascent and current-owner profile checks pass. No performance conclusion. The village door pair remains raw evidence only because the selected work gate pair already covers that state change. No genuinely matched berry-before/after shot exists, so it was not invented from later images.

## Throwing — four selected staged images

Runaa21cb6ddf2441589a5338987e8c0942,40checks passed,49.46seconds. This is an explicit controlled fixture, not ordinary acquisition or a natural enemy encounter. The original zone is cleared except the player; bounded stone floor16..32×8..18 is created. Player moves to20,12, Strength/Agility become30, inventory capacity500. Spawned MarlbackGleaner25,12 has Brain removed,500HP, natural AV/DV0. ShortSword, three daggers, Warhammer, WaterTonic, a separate floor dagger22,12 and five throwing skills are provided; command RNG123. After setup, interactions, save/load and target selectors use production keyboard/synthetic-gamepad routes.

- THR-01/02: Body versus Ground mode on the same occupied target cell. Labels and changed preview text are readable. Ground aiming still correctly reports first contact as the occupying gleaner. Small overlapping yellow/red endpoint outlines on stone do not prove visibility over vegetation or particles.
- THR-03: actual prepared state retains dagger stack3 and target; overlay says2/2 READY TO THROW, confirm again/no wait needed, Enter/click to throw, Esc to cancel.
- THR-04: synthetic controller preparation retains stack2 and target; overlay and focus change to A/RT and B. Same scene, different later attempt, not an identical state with only device changed.

**Visible issue retained:** the right focus pane cuts the prepared-throw benefits text off at the log boundary. The dedicated lower overlay still supplies the actionable next step. Physical Deck input, Steam Input behavior, mouse mode buttons and handheld readability are not established by these four screenshots.

## Remaining capture gaps from these routes

No captured strike animation frame; no matched unlit-versus-portable-light pair; no work-route ripe crop harvest or save checkpoint; no natural acquisition or busy-terrain throwing comparison; no new generated fog/visibility override introduced to make those gaps disappear. Static visual findings are observations, not source fixes. Raw failed receipts are retained.

## Additional shop capture — one selected full-reveal/staged-context image

TRD-01 uses fresh native liquid run3dc02a360ac6431d970eeb451c1bc7dc. This1280×800 image was individually opened. The selected liquid flask costs23 drams, Sella Kettle has200, and the player retains the original50 before purchase. Buy/sell stock, weight, prices, visible chevron and F1/Enter/Tab/Esc hints are legible. No clipping observed. The real generated vendor is at Morrowfast, Overworld.3.6.0; the actor was transferred to48,20. There are no granted goods or currency. The screenshot is before purchase; subsequent native report verifies the exact flask4744 moved to player ownership and coins fell to27.

Conservatively placed under staged because the SampleScene baseline inherits full reveal; the full-screen trade UI itself obscures the world. This is not the synthetic throwing fixture. No per-frame reveal/camera/light receipt or controller shop hints were captured.

The liquid route is **incomplete**: after buying flask and waterskin and completing finite water operations, its inventory `examine_item` lookup failed. Oil, acid and the save/load checkpoint were not reached. Keep its2failures/1unexpected-error receipt; do not claim successful hazard capture. No additional cave/crop image was needed: CAV-01/02 already cover ripe-crop examine to harvested ground produce with their mismatch disclosed.
