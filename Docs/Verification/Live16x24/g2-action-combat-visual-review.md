# G2 ordinary combat — bounded visual review

2026-10-24 UTC. Inspected all17 actual1280×800 PNGs in
`Docs/Verification/Live16x24/G2Action-20261024-034900-041/frame-00..16`
and their JSON metadata through read-only image/file tools. The index has no
recorded capture errors. No Unity, gameplay or artwork edits were made by this
review. Separate capture-helper robustness edits are not visual/gameplay changes.

## Supported observations

- **Approach:** frames00–08 show real player movement from(40,17) through(40,18)
  to(40,19),40HP throughout. Walk A/B and destination idle poses remain aligned
  with the recorded world/screen positions. No doubled static body, displaced
  head, blank step or visible baseline pop is apparent.
- **Actual NPC death:** Scrabbler2071 starts at2HP in00, reaches0HP and
  `dying=true` in01, uses hurt artwork while fading at alpha1→0.8200→0.6406→0.5694
  through04, and is absent from the actor receipts from05 onward. The right log
  reports its death and equipment drops. The visible body fades without leaving
  a second standing sprite. This is bounded death/fade evidence; the receipt
  does not enumerate or verify every dropped item's blueprint/portrait.
- **Player melee and enemy reaction:** at tick90, frames09–11 show the player's
  attack sprite at(40,19), Gleaner2074 hurt at(39,20) and HP16; the log records
  the player's3-damage fist hit and subsequent stun. Frame12 displays the player's
  idle sprite again. At tick100,13–16 repeat player attack→idle art, with enemy
  HP11 after the logged2-damage player hit and3-damage warden hit. The figures
  stay in their cells, and their silhouette changes are consistent with the
  existing restrained native pose sheet rather than painted weapon claims.
- **State versus displayed sprite:** player frames12/16 still report
  `state=Attack` around0.121 seconds despite displaying the idle sprite;
  Gleaner11/12/15/16 similarly reports Hurt with idle art. These samples establish
  sprite recovery, not the later full state-enum transition. They do not show
  player hurt: player HP remains40 throughout this sequence.
- **Scene and UI:** ground remains quiet behind combatants, existing foliage
  does not erase bodies, and the right log/bottom skill bar stay within their
  own regions. Actor pixels, palette and limb boundaries remain readable except
  where existing large floating feedback crosses them (below). No source-art
  redraw is justified by the captured poses themselves.

## Concrete visual issues to resolve or explicitly bound

1. **Adjacent numeric hits concatenate perceptually.** Frames01–05 display red
   values reading as “62” above neighboring combatants while the log records
   separate6-damage and2-damage hits. The earlier six-frame walk capture showed
   the same issue as “12.” This is repeatable native-size feedback ambiguity,
   not evidence that damage arithmetic is wrong. The planned numeric grouping/
   placement correction should be checked against these before frames.
2. **Long status text obscures another actor and weakens ownership.** In09–12,
   the wide red “Stunned” label over the Gleaner crosses the adjacent player's
   head/body area. The log identifies the Gleaner as stunned, but the world label
   alone can look associated with the player who is performing the attack.
   “Shaken” in01–05 similarly spans a broad cluster during movement. Preserve
   the actual effect/event; acceptance needs clearer placement/ownership or an
   explicit limitation, not a new sprite design. This is a UI-feedback concern,
   separate from the correctly aligned actor artwork.

## Acceptance bounds

The captures support native pose placement during approach, actual player melee,
Gleaner hurt, one NPC death/fade and idle-sprite recovery. They do not establish
continuous motion feel, sound synchrony, performance, player hurt/death,
all guard/parry outcomes, every state/lighting/material combination, later state
recovery after the final sample, physical Steam Deck controls or a finished G2
scene/release. EOF readback is selectively instrumented and cannot be used as
an uninstrumented frame-time measurement. No pixels were transformed for review.
