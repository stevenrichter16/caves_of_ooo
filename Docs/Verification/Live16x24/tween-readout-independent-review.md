# Ordinary tween/readout replay review

Independent read-only review by projection_cold_review; root also inspected
ordinary UI and source. Reviewed actual EOF frames02/10/13/15/16/18/19/23
in G2Action-20261025-011948-031, frame metadata and both recorded events.
No new significant visual issue was found. En garde is readable; Attack,
enemy Hurt/fade and following Walk A/B remain intact. Player stays42HP.

Only two events: ordinary Player2695 attack and lethal15 damage to
MarlbackScrabbler11284 at tick2050, Origin0/CounterDepth0. Frames15–23 have
no readouts. Player is still logically3,19 at lethal damage; the old logical
occupancy rule already obstructs that lane. Move to4,19 occurs at2060.
This does not exercise the new moving-body numeric suppression.

Ordinary post-fix combat and movement sanity passed. The exact departing-player
plus periodic-lethal overlap did not recur because this replay killed the NPC
in one15-point strike. Native behavioral RED→GREEN establishes that geometry;
exact live reproduction remains unobserved. No Riposte, periodic-status or
hit-number visibility claim. Selective EOF captures are not continuous video,
input-latency proof or physical Steam Deck acceptance.
