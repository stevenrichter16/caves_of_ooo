# Awning and sounding rod: native observations, November 21

Main SampleScene/GameBootstrap, seed64, authored scenarios and ordinary keyboard
input. No direct gameplay actions, HP grants or clock jumps replaced player
actions. The awning fixture itself starts at Height, as authored. This record is
bounded gameplay evidence, not complete-machine acceptance or Mac delivery.

## Awning: initial partial check

CampAwningShowcase started on surface Beating at40,12, tick300, HP40. Ten ordinary
wait actions reached400: no shade, one Parched stack and exposure0. A scorpion
attacked during this waiting, leavingHP28. Two north steps reached40,10 at420,
HP20. RoadStone41,10 offered no placement; sand40,9 offered "set down north
[shade awning]". Choosing it cost420→430. At430 the actor was covered by shade,
exposure0 and still one Parched stack, HP15. Shade prevents further glare; it does
not cure an existing stack. No head covering or water substituted for the awning.

The scorpion interrupted the longer experiment. This does NOT establish ten more
protected turns, renewed exposure after leaving, take-up removal of shade,
travel/reload persistence or an alternative hat/water comparison. Do not mark the
initial experiment fully hand-tested; the follow-up below closes some of these
gaps. Earned F5 save is archived in
`/private/tmp/coo-awning-earned-20261121/547789184890420f89518528a85fb9c6`.

### Follow-up: standing reload, ten protected actions and take-up

Fresh Play/Continue from that earned430 save retained the standing awning and
shade=true, one Parched stack, HP15 and exposure0. Ordinary inventory input equipped
the starting dagger. A starting healing tonic cost430→440 and removed the one
Parched stack; shade had not cured it. The actor fought the adjacent scorpion
through a dagger attack and the ordinary Ember Spit directional ability, then
drank the remaining healing tonic. At470 HP27, shade=true, Parched0, exposure0.
The enemy burned out during this sequence; no direct kill or immunity was used.

Seven ordinary waits reached540, HP24. Together with the intervening combat/drink
turns, the actor spent ten consecutive paid actions under shade from440→540,
without regaining Parched; exposure remained0. This is a full exposure interval
at Height, not a claim that all ten actions were waiting. No hat or roof supplied
the protection and no drink was used after470.

C north on the awning offered take up. It was the same entity2225 before and after
taking it into the pack. Take-up cost540→550. Shade immediately became false and
exposure1. Four ordinary waits reached590 with exposure5, no new Parched yet and
HP24. Carried ShadePart.Shading was false: carrying the awning does not provide
shade. The day was about to leave Height, so another full unshaded ten-turn
interval was not claimed. The earlier open ten-turn check already produced a
Parched stack. Leaving the ring while the awning remains installed, alternative
hat/water comparisons, and travel/carried-save reload are still unverified.

F5 saved the earned590 state, archived separately without replacing the earlier
checkpoint: `/private/tmp/coo-awning-takeup-earned-20261121/547789184890420f89518528a85fb9c6`.
The follow-up used a new owned save root; cleanup returned main to idle/clean with
null override and removed the temporary keyboard/settings. Native readbacks are
in `awning-followup-observations.json`. This adds actual protection/take-up and
standing reload evidence, not complete-machine acceptance.
Bounded independent source/artifact critique found zero P1/P2. It confirms that
the disclosed tonic removes Parched but does not reset glare exposure, so it does
not invalidate the protected full-interval comparison. This was not independent
game operation. Fresh post-follow-up hashes matched84 foreign tracked files and
861 normal game files, excluding analytics/test output.

## Sounding: two paid soundings, free log and fresh Continue

SoundingRodShowcase started on ordinary cave Overworld.10.11.1 at40,12, tick10,
HP40. C on the actor's own cell offered "sound the ground (carries about12 paces)
[sounding rod]". Choosing it cost10→20. Rod3457 recorded tick10 and "Open ground
lies east, close." No coordinates or object identities were given for the lead.

The operator walked north four cells and back, then east five cells to45,12;
the actor could traverse the eastward ground. This is a coarse consistency check,
not identification of the particular unseen six-cell hollow selected internally.
A second C-menu sounding cost150→160 and recorded another coarse east/close
reading with the new origin. The pack's "read the survey log (2 soundings)"
printed both entries, oldest first, with origin and relative time; tick stayed160.

The actor returned to40,12 at210. Attempting ascent was refused "Not with company
this close." The surface was not reached, so surface refusal is still unverified.
Enemy response to the sound, protected authored-site refusal and other geological
floor reaches were not played here. No full-map or protected-mystery claim follows.

F5 saved at210. A fresh Play/Continue retained the same rod3457, both complete log
entries, LastTick150, LastReading and zone/tick210. Archive:
`/private/tmp/coo-sounding-earned-20261121/741e3cb41b084fc69e0dcda33fef22b0`.

Both scenarios and the fresh Continue used NativeSaveIsolation. Owned keyboard
callbacks were drained, virtual devices removed and original InputSettings
restored before stopping Play. Main was left idle with a clean scene and null save
override; seed/last-scenario settings were restored. Normal saves were not targets.
Observations JSON records the exact readbacks; intermediate operator actions and
cleanup are reported by the operator, not an independent second Unity operator.
Bounded independent source/artifact review found zero P1/P2 and confirmed these
limits; it did not independently operate the game. After all checks, fresh hashes
matched all84 tracked foreign files and861 normal game files from the retained
baseline, excluding Unity analytics and test output. This is not a byte-exact
claim for the entire persistent-data tree or preferences file.
