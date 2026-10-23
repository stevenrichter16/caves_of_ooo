# Native opening motion

This is the implementation record for the verified motion plan in `LIVE-16X24-ART-DIRECTION.md`. The task is presentation only: simulation remains turn-discrete, with no turn delay, new combat rule, or implicit release activation.

## Verified design

- Borrow the accepted base/cast atlas idles and the sixteen authored motion frames. The compact catalog owns at most twenty bottom-center sprites and four assets. It does not mutate or dispose borrowed textures. Missing groups retain the existing static foreground.
- Discover actors only inside the existing resolved-cell pass. Suppress a static foreground only after the actor renderer accepts its current visible top owner. Reconcile tracked actors without scanning all zone entities or dropping untouched actors during an incremental render.
- Flat actors are unlit white with no shadows, lunge, shake, or invented casting art. All four canonical poses face East; West mirrors horizontally and North/South retains the last horizontal orientation. Existing non-flat animation behavior stays unchanged.
- Walk lasts100ms, split between its two poses. Melee lasts160ms (120ms pose,40ms idle recovery); hurt lasts140ms (100ms pose,40ms recovery). One pending reaction preserves at least60ms of a pose during immediate retaliation, coalescing duplicates without restarting. It cannot become an unbounded event backlog.
- Consecutive movement begins at the displayed anchor. An action interrupting movement anchors at the authoritative destination. Adjacent forced movement slides idle art for80ms; nonadjacent displacement snaps.
- Validate tracked current owners even when no cell is dirty. Release hidden, removed, replaced, carried, foreign-part or otherwise ineligible owners and request only their bounded old/current cells to repaint. This closes the blank static-fallback gap when ownership changes without a previously scheduled render.
- Death is emitted before `Zone.RemoveEntity`, after HP/death bookkeeping. Capture a visible current owner's hurt pose despite its death marker, then allow only a short fade while the captured cell remains visible and has no replacement living owner. Ordinary removal has no fade.
- Pause, projection, mode, zone and disable transitions clear views and pending reactions. Re-entry uses ordinary idle admission. Release active and pooled objects before disposing their sprites/materials. Additive gameplay-hook subscribers survive renderer destruction.

## Test and review plan

Write failing native Unity tests before implementation. Catalog gates cover exact eligibility, rectangles/pivots, cache identity, optional-resource independence, adapter casting fallback and borrowed-resource disposal. Renderer gates cover actual submitted static suppression, incremental retention, no-op work, native move/blocked move/melee miss/veto/damage, fast reactions, visibility/removal/death, bounded fallback repaint and repeated lifecycle transitions. Preserve legacy catalog/state/projection and fullscreen ownership controls.

Synthetic tests establish behavior independently of the real atlas. The actual-resource test must fail while the resource is absent, then pass after the independently reviewed source is imported. The integration agent owns import after the real-resource RED. Root retains ordinary Play inspection, main living document and commit/push ownership. Native tests establish contracts, not visual feel or Deck performance. Real Play must still inspect movement, combat, fog and menu re-entry before acceptance.

## Evidence

The integration is implemented and imported. `opening-motion.png` is256×24, Point/Clamp, one mip, uncompressed; imported bytes equal the reviewed source, SHA256 `08bf046d5e70ae2f9466d0e3570c39933d46de4f991cf941607725ae0c64c176`. `Tools/Live16x24/import_opening_motion.cs` records the repeatable import. Original base/cast/state artwork was not edited.

| Receipt in `Docs/Verification/Live16x24` | Native result / implication |
| --- | --- |
| `opening-motion-renderer-red.xml` |27/27 fail because the compact catalog is absent; renderer tests precede production. |
| `opening-motion-catalog-compile-red.txt` |Native C# compiler rejects the absent catalog type before its implementation. |
| `opening-motion-synthetic-first.xml` |Catalog synthetic and legacy controls pass; actual motion resource absent. Initial renderer fixture also exposed that an inactive Unity Grid produces zero CellToWorld coordinates; fixture was replaced with the actual Awake-created graph. Mixed item tests in this receipt belong to their separate slice. |
| `opening-motion-lifecycle-red.xml` |72 cases,66pass6fail: missing resource, two Cast interruption cases, component re-enable fallback, plus vector tolerance and movement-endpoint findings. |
| `opening-motion-ownership-red.xml` |37 cases,31pass6fail: two Cast interruptions, component re-enable, the float endpoint, and stale replaced/foreign cell ownership. |
| `opening-motion-resource-red.xml` |105 cases,104pass; the only failure is the genuinely missing motion resource. |
| `opening-motion-import.json` |Successful native import and exact source-byte comparison. |
| `opening-motion-green.xml` |**435/435 pass, zero skipped**,06:30:47–06:31:07 UTC. Includes all76 new catalog/renderer cases,29 existing actor controls and the previous330 opening/state/terrain/projection/fullscreen tests. |
| `opening-items-examine-green.xml` |**629/629 pass, zero skipped**,06:52:02–06:53:00 UTC. Rechecks those435 after loose-item and flat-examine integration, adding46 item,52 flat-examine and96 existing portrait/ledger controls. |

One mixed item run executed the prior compiled fixture after an unrelated new item test compilation error. Its receipt is explicitly named `opening-motion-import-items-stale-not-acceptance`; it is not acceptance evidence. The error was corrected and the fresh item RED was run separately. Final435 validation uses the current compiled tests and actual imported motion resource.

The later629-case run started after an MCP initialization timeout while Unity was finishing the preceding test cleanup. Its fresh native XML contains every requested fixture and complete passing results; the transport timeout itself is not test evidence. Post-run readiness probes also timed out, so root must inspect editor readiness before ordinary Play. No extra test job or forced refresh was started during that native run.

## Implementation review

- Fixed cast interruptions so an unauthored spell pose anchors idle at the actual cell and clears queued melee/hurt, rather than continuing an earlier action.
- Fixed quiet ownership release and component re-enable with bounded cell repaint requests. Removed or invalid owners no longer leave the prior static tile suppressed. Current cell identity and ParentZone are checked, not merely a cached entity index.
- Consecutive movement preserves the displayed anchor; duration completion tolerates floating-point addition at100ms. Native gameplay movement, misses, attack veto and HP loss were exercised, rather than only invoking mock hooks.
- The flat layer uses one pending reaction, exact pose/recovery phases, horizontal facing and white unlit bodies. Stable updates allocate zero managed objects in the targeted native test and discover no previously unpainted actors; rendering admission happens only in the existing cell pass. Both old full-zone synchronization sites bypass flat discovery.
- Destruction releases active/pool objects before the owned sprites/materials. Borrowed textures and additive external hook subscribers survive. Tests restore Interaction/SelfUse subscribers as well as the actor hooks.
- Independent cold review found no remaining actionable P1/P2 in this bounded slice after the concrete Cast, cell-identity and fixture-isolation corrections.

## Ordinary-input Play check — 2026-10-23

The isolated seed64 opening ran through fresh N, ordinary arrow movement, a player bump attack, existing NPC combat and repeated inventory open/close. Player and creatures returned visibly after fullscreen menus; walking changed current fog normally and no duplicate static body was observed. Observational subscribers listened to genuine events without emitting hooks or changing game state. `native-motion-player-step.png/txt` captures Player walk A; `native-motion-melee.txt` identifies the Scrabbler attack pose with West mirroring. The same-frame `native-motion-damage-reaction.png/txt` still shows that attack pose: it is **not** visual proof of hurt. Unity accepted only the final same-frame screenshot request, so there is no separate melee PNG. Do not infer complete walk-B, reaction sequence or timing from these stills.

Root and independent visual review accepted the captured silhouettes and alignment. The targeted native cases establish two-frame timing, reaction bounds, fog removal and lifecycle contracts; those assertions do not prove handheld feel. No gameplay timing, spawn placement or equipment rules changed for capture.

**Can verify:** native imported bindings, bottom-center projection, renderer ownership/static suppression, real gameplay hook contracts, bounded timing and lifecycle/fog counterchecks, legacy controls; actual keyboard movement/combat/menu return and captured walk-A/attack appearance. **Not yet accepted:** complete recorded reaction sequence, physical Deck/controller feel or performance, all-biome art or release activation. This remains opt-in development presentation.
