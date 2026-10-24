# Working court: independent actual-image review

2026-10-24. Bounded review of the fresh seed65 kitchen court. The reviewer viewed the saved actual Unity captures and read the observational owner record, source art and exact court layout; did not operate Unity, alter art or change runtime code. This is not approval of the whole opening, every seed, a release build or physical Steam Deck performance.

## Evidence viewed

- `working-court-65-kitchen-entry.png`: ordinary chunk entry, 1280×800.
- `working-court-65-kitchen.png`: ordinary approach at native display scale, 1280×800.
- `working-court-65-kitchen-2x.png`: close 2× view, 1280×800; paired observational JSON reports player `(54,5)`, Cook `(51,6)`, 15 visible `court-floor` owners and 9 visible shelter walls.
- Earlier `working-landscape-65-kitchen-entry.png` and `working-landscape-65-kitchen-2x.png` establish the loose service-group baseline. The camera, player and fog states differ; this is a composition comparison, not a pixel-identical before/after diff.
- `ArtSource/Live16x24/concepts/working-court-v1.png` is a concept only. Its full paving, soft lighting, new rubble and redrawn presentation were rejected. The final restrained court should not match those unsupported details.
- The authored native contact/repetition renders, palette and source-cell file were previously inspected and verified. The source-art receipt is separate from actual runtime evidence.

## Assessment

**The local composition is meaningfully improved.** Previously, two wall cells and a row of services looked like separate objects floating over grass. The real nine-cell masonry edge now gives the group a back and one side. The connected sparse stones join the working spaces without turning the site into a closed room or decorative paved rectangle. This is a useful scene change, not merely another asset packet.

The warm Cook, pan, tray and pickup retain the focal colors. The grey/olive stones sit below them in contrast; visible dark-green gaps support the intended stepping-stones-in-grass reading. No floor gradient, glow, raised slab, shadow or fake road is visible. At 2× the rear walk row and open right/front access are legible. The cot, chair and oven remain distinct from the ground and the cultivated cell remains outside the work surface.

At entry, the larger real wall hides more of the pan and furniture than the old two-cell wall. The Cook, oven, crop and front stones remain visible, however, and the open approach still has a clear destination. The later normal approach reveals the services without suggesting a door or forced corridor. This observed occlusion is consistent with a real wall and does not justify rotating or extending the court on visual grounds alone. Root reports the ordinary route was walked; the images independently establish visible openness, not pathfinding correctness.

At 1× the material grouping is discernible but small. At 2×, three stone variants and their green joints are readable. The floor is intentionally quiet and does not compete with the useful owners. The approved Cook retains its familiar silhouette and colors in the actual view; preservation of exact accepted pixels is supported by the separate source hash checks, not inferred from this screenshot.

## Named observation requiring closure

**V1 — Tiny angular mark near the upper-right service/rear-aisle area.** A small green right-angle mark is visible in `working-court-65-kitchen-2x.png`, and faintly in `working-court-65-kitchen.png`. It is not present in any of the three authored stepping-stone frames. Root is investigating the candidate cell `(53,3)`; the observational record confirms visible `StoneFloor` id `7215` there, but that filtered record does not establish every selected foreground owner or visual overlay at that cell.

Do not repaint the stone or erase the mark speculatively. Check actual selected foreground, tile state, co-owners and overlays. A truthful unrelated current owner can retain its representation. If it is a redundant floor glyph or an unintended selection remnant, fix its selection/rendering path and recapture. No P1/P2 defect is confirmed from the screenshots alone; this cue remains open rather than silently accepted.

## Remaining weaknesses outside this slice

- The surrounding native-scale landscape still contains large uniform dark-green areas and strongly repeated hedge textures. The court improves the service destination; it does not finish the entire chunk or six-site opening.
- The existing wall is a clean repeated masonry edge, whereas the concept suggests broken individual stones. Repainting the accepted boundary atlas is an aesthetic follow-up, not required for this court's material truth or usefulness.
- The stone variants visibly repeat when inspected closely, especially in the rear/front rows. Within this small fifteen-cell surface, that repetition is quieter than the hedge texture and does not justify more variants now.

No new props, furniture, lights, crop rows, walls or HUD changes are recommended for this bounded pass. Preserve the accepted art and mask work. Close V1, then use root's gameplay/fog/revisit/save evidence to determine implementation acceptance. These seed65 captures support the intended local grouping; they do not establish all-seed placement, full fog-memory behavior, animation, controller usability, performance or release quality.

## Final independent visual supplement: rotated seed64 and seed65 state evidence

The following additional actual captures were viewed directly:

- `working-court-64-kitchen.png` and `working-court-64-kitchen-2x.png`, compared with `working-landscape-64-kitchen-2x.png`.
- `working-court-65-stones-reader.png`, `working-court-65-pan-menu.png`, `working-court-65-harvest.png`, `working-court-65-saved.png`, `working-court-65-loaded.png` and `working-court-65-returned.png`.

**Seed64 independently supports the rotated layout.** Its paired observational record reports player `(26,17)`, Cook `(27,17)`, oven `(27,15)`, pan `(29,17)`, cot `(27,19)`, and all 15 floor and 9 wall owners visible. The court's long wall now runs down its right side with a short upper return. The stone circulation space joins the vertically arranged services, while the broad left/bottom approach remains visually open. The same warm owner / quiet material hierarchy works in this orientation. The oven and crop remain outside the stones; the crop is readable as its own planted cell. The background hidden region meets the real masonry without visible fabricated structure leaking through it. No additional material layout or native-art defect is evident in these two views.

Root reports ordinary entry movement stopped at the real north wall, then reached the court through the open side, with real encounter damage still present. The screenshot log includes a stone-wall strike and HP23/40. These observations are consistent with a solid wall and an accessible court; this reviewer did not independently execute the route or use health/AI overrides.

**Seed65 material, owner and action wording agree.** The stone reader displays the native broad-stone portrait, title and FOCUS name “worn stepping stones.” Its prose explicitly describes grass in the open joints, intentional plantability and absence of a prepared bed. The pan menu still identifies a split clay batch pan with a two-fire-clay repair action. This is the damaged pan's current state; the menu capture alone does not prove the repair transaction. Harvest removes the crop while retaining the separate brown soil cell outside the court, and the saved/loaded/returned views retain the working area's recognizable wall-and-stone grouping. These are actual visible state checks, not evidence inferred from the concept image.

**V1 closure for this art slice:** the earlier angular mark is absent in the later seed65 pan, harvest and return views. No persistent atlas defect or false floor state is demonstrated. The mark's exact cause is not established by this review; no speculative artwork change is justified. Retain the original observation above as evidence rather than claiming a diagnosed runtime repair.

**Separate actor coverage follow-up:** the badge-like glyph over the seed65 cot is, according to root's live-owner inspection, an actual solid roaming Farmer. The screenshots show the glyph but do not by themselves establish the owner's identity. This is an existing NPC native-art coverage gap, not a manufactured court decoration, missing cot, extra floor state or reason to conceal a real actor. Record it for an actor coverage pass; no new Farmer art belongs in this court-only change. Seed64 also demonstrates ordinary occupants can overlap service positions; their current representation should follow real owner selection rather than forced scenery visibility.

**Bounded final assessment:** both observed orientations meaningfully improve local place recognition and retain readable service access and truthful sparse material. No confirmed P1/P2 visual defect remains in the reviewed court slice. No further stone variants, props or layout edits are recommended before root finishes its functional checks. The original wider-scene weaknesses and release limitations remain: this review does not establish every seed, all fog memory/state combinations, six-site graphics completion, animation, packaged-build performance or physical Deck quality.
