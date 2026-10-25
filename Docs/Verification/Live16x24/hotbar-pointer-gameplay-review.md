# Hotbar pointer independent gameplay review

Reviewed 2026-10-25 from the actual 1280×800 PNGs and JSON receipts named
`hotbar-pointer-ready`, `hotbar-pointer-lunge-click`,
`hotbar-pointer-follow-through-click`, `hotbar-pointer-en-garde-click` and
`hotbar-pointer-cooldown-blocked`. The reviewer did not control Unity, change
source art or rerun native tests.

The supplied mouse acceptance evidence passes this bounded review. Selection,
aiming, self-cast status and cooldown refusal agree between the visible HUD and
recorded state. No visual or state inconsistency was found.

| Capture | Visible result and recorded state | Tick / HP |
| --- | --- | --- |
| Ready | Card 1 En Garde selected; all three powers ready; no pending action. | 10 / 40 |
| Lunge click | Card 2 selected with cyan AIM and `line 2`; pending `LongBlades_Lunge`. | 10 / 40 |
| Follow Through click | Card 3 selected with cyan AIM and `adjacent`; pending `LongBlades_FollowThrough`. The log records cancellation of Lunge first. | 10 / 40 |
| En Garde click | Card 1 selected, no pending action, En Garde status visible and cooldown 11. The log records cancellation of Follow Through first. | 20 / 40 |
| Cooldown blocked | Card 1 remains selected with cooldown 11 and no pending action. The log explicitly reports En Garde on cooldown. | 20 / 40 |

All receipts retain player 40,12 in `Overworld.11.10.0` and report native input
enabled. The repeated cooldown click does not advance the recorded tick, reduce
HP or reset the cooldown. The three established pictograms retain their source
classes, icon names, 16×16 atlas rectangles and transforms across these captures.
The icons remain distinct and clear of state labels, card borders and names;
this is confirmation of the existing art through pointer interaction, not a new
art acceptance claim.

The capture operator reports ordinary desktop mouse clicks on Lunge, then
Escape and Follow Through, then Escape and En Garde, followed by a repeated
En Garde click. The `nativePointer` and `legacyPointer` values were sampled in a
later Editor callback. They are **not exact click-frame coordinates** and cannot
establish which pointer position was consumed by the click handler.

This review covers the supplied post-click outcomes. It does not independently
demonstrate executed aimed attacks, every hotbar hit boundary, other display
scales, controller use or physical device performance. Native regression results
are maintained separately by the implementation owner.
