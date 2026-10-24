# G2 action visual review — six end-of-frame captures

2026-10-24 UTC. Read-only review of actual1280×800 PNGs and matching JSON in
`Docs/Verification/Live16x24/G2ActionWalk/frame-00..05`. No Unity, repo, art or
runtime changes. Only this scratch review was written. Root reports these are
WaitForEndOfFrame/ScreenCapture.CaptureScreenshotAsTexture captures with deferred
encoding. The rejected upside-down endFrameRendering prototype is not evidence
for this review.

## Observed sequence and alignment

| Frames | Observed art and supporting state |
| --- | --- |
|00|Idle baseline at tick20; player(40,13),40HP; Scrabbler2071 at14HP. This baseline is several seconds before the sampled step and is not part of a continuous timing measurement.|
|01|At tick30 player destination(40,14), Walk A; dog, other Scrabbler2077 and Gleaner also show Walk A. Their rendering still begins from the previous cell, as expected during interpolation. Scrabbler2071 attacks with Hurt queued and HP12.|
|02|Walk B at approximately0.0508 seconds; player halfway through the24-screen-pixel vertical move, other moving actors likewise partway to destination. Scrabbler2071 still displays attack.|
|03|Walk B at approximately0.0607 seconds; Scrabbler2071 now shows its actual hurt frame, with the queued reaction consumed.|
|04|Player/dog/moving Scrabbler/Gleaner have idle art at their destination. Scrabbler2071 retains hurt art at0.0414 seconds into that reaction.|
|05|The walkers remain idle; Scrabbler2071 displays its idle sprite again. Its recorded state enum is still Hurt at0.100706 seconds: this proves idle-art recovery, not full state-machine Idle recovery.|

The displayed head/torso/leg changes distinguish Walk A/B rather than merely
sliding one unchanged sprite. The player stays centered horizontally while
moving down one native cell; dog moves up one cell, and both walking marlbacks
move left. Recorded screen positions follow those same directions and
intermediate integer-pixel positions. No duplicate static body, detached pose,
obvious baseline pop, invisible frame or sprite overflow into HUD is apparent.
The attack-to-hurt sequence retains the same Scrabbler's location and anatomy.
These are actual NPC actions, not proof of a player melee or player hurt pose.

Ground detail remains subordinate to actors. Gold player silhouette stays
readable on green; the low dog and squat Scrabbler remain distinct from taller
Gleaner. Nearby herbs do not erase actor bodies. The pond/road/field and existing
masonry remain visually stable within these samples; the right HUD and bottom
skill bar retain their geometry and readable contrast. Source RGB identity is
not claimed: these are composited game captures, not raw atlas pixel checks.

## One bounded action-feedback concern

Adjacent red floating values in frames01–05 read visually as **“12”** above the
two neighboring combatants, while the log describes separate1-damage and
2-damage hits. This risks reading as a single12-damage event at native scale.
It is a placement/readability concern in existing floating feedback, not evidence
of incorrect damage calculation and not a reason to redraw ground or actors.
Check it in the next ordinary combat sequence; separate-owner values may need
more spatial or temporal separation if the ambiguity persists. No code/art
change was made by this review.

## Acceptance boundary

Accept these captures as bounded visual evidence for the four supported actors'
walk A/B and destination idle artwork, plus one real Scrabbler attack→hurt→idle
sprite sequence. They do not establish continuous motion feel, capture-free
frame pacing, sound synchrony, player attack/hurt, death, status/fog/pause
transitions, all material backgrounds, complete state-machine recovery or
physical Deck controls/performance. Full G2 action and scene acceptance remain
with the parent’s ongoing ordinary-play route.
