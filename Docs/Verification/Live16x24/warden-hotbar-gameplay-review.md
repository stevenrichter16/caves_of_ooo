# Warden hotbar independent gameplay review

Reviewed 2026-10-25 from saved actual 1280×800 PNGs and JSON receipts:
`warden-hotbar-ready`, `warden-hotbar-en-garde`, `warden-hotbar-lunge-aim`
and `warden-hotbar-follow-through-aim`. The reviewer did not operate Unity,
modify source art or rerun native tests.

The three icons are visually acceptable in the captured ready, cooldown and
targeting states. No further art or layout adjustment is recommended.

## Visual findings

- En Garde reads as an upright sword with an open defensive brace. It does not
  resemble a shield or enclosing magic barrier. Its narrow silhouette remains
  recognizable in the selected ready state and while `CD11` is displayed.
- Lunge reads as a horizontal blade thrusting from its hilt. The short accents
  support reach without introducing a running figure or a dash arrow.
- Follow Through reads as a diagonal blade with a small asymmetric boot. The
  projecting toe distinguishes the step mark from a pedestal. The diagonal
  silhouette is distinct from both other powers.
- Icons remain inside their existing bands. No icon touches the ready/cooldown/
  aim labels, card outlines or ability names. The two-line Follow Through name
  stays legible. Selection and cyan targeting indicators remain distinguishable
  from the icon accents, and empty slots retain their quiet presentation.

## Receipt checks

The first three slots map the actual `LongBlades_EnGarde`, `LongBlades_Lunge`
and `LongBlades_FollowThrough` sources to `HudSkill_EnGarde`, `HudSkill_Lunge`
and `HudSkill_FollowThrough`. Their source rectangles are respectively
112, 128 and 144 pixels from the atlas left edge, each 16×16. These identities
and rectangles remain stable across all four captures.

Ready records player 40,12 at tick 10 with 40 HP. After ordinary En Garde
activation, the receipt records tick 20, 40 HP and cooldown 11; the visible
status text identifies En Garde. Lunge targeting selects slot 1 and records
pending `LongBlades_Lunge`; after cancellation, Follow Through targeting selects
slot 2 and records pending `LongBlades_FollowThrough`. Both aiming receipts
remain at player 40,12, tick 20 and 40 HP. The visible summary bands distinguish
Lunge's `line 2` targeting from Follow Through's `adjacent` targeting.

## Bounds

This is a visual identity and HUD state review. These captures do not demonstrate
an executed Lunge hit, Follow Through's conditional movement after a kill,
controller comfort or physical Deck performance. The small pictograms identify
the actions; they do not independently explain every combat condition. Native
regression results are maintained separately by the implementation owner.
