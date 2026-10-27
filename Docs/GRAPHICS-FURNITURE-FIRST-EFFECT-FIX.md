# Ordinary chair use before the player's first status effect

Status: native GREEN and ordinary Sit/Stand replay verified; exact source publication pending. This
is a narrow gameplay defect discovered during the shared-furniture before route,
not a save migration or new furniture system. Root owns Unity and production.

## Observed trigger and source correction

Root's ordinary Continue from the earned Sodden room fixture reached Bed12657
at47,19 and Chair12666 at55,19. Sleep worked, changing HP38→40 and advancing
the clock60 turns. Chair offered Sit but rejected `invalid_context`: the player
and chair were placed, the player tag and ChairPart existed, and the player's
StatusEffectsPart was absent. This is actual gameplay evidence supplied by root;
no missing part was fabricated into the live run.

The absence is legitimate. Player inherits Creature without an authored
StatusEffectsPart. `Entity.ApplyEffect` creates that part lazily through
`EnsureStatusEffectsPart` (Entity.cs341–351,416–424); StatusEffectsPart itself
documents first-effect creation. Save loading restores serialized parts and is
not responsible for preinstalling a container. `PlayerSeatService.TryAct`
instead rejects a null effects container before reaching ApplyEffect. Existing
chair tests preinstall the container in Setup, which hid the first-effect case.

This is original Caves of Ooo behavior; no external Qud implementation or parity
claim is needed.

## Bounded plan and verification

1. Run the new real-factory Player/Chair regression before production. It first
   proves adjacent refusal leaves health, position, reservation and absent
   status container unchanged; then same-cell Sit must create one container and
   the actual SittingEffect, and Stand must release the real reservation.
2. Remove the service's incorrect preexisting-container requirement and make
   its seated lookup nullable. Keep existing eligibility and transaction gates.
   Rollback must remove a newly created effect through the actor's effect
   facade, because the original local container reference may have been null.
   Do not preinstall parts in blueprints, bootstrap or save loading.
3. Run the focused chair suite and existing bed/ownership/transaction controls.
   Review veto and exception rollback, then retry the actual preserved chair
   through ordinary input before furniture sprite integration.

The proposed fix changes no health, movement, ownership, rest timing, art,
rendering or save format. A successfully attempted effect may leave an empty
lazy container after later rollback, as the existing effect facade already
does; the effect and chair reservation must still be rolled back exactly.

## Self-review and files

- Corrected premise: missing StatusEffectsPart is not a broken legacy save.
- Regression includes a real factory player and a flipped spatial precondition;
  it does not grant a part or seat state to make the positive pass.
- Native execution, runtime implementation and ordinary retry remain pending.
- Modified: `Assets/Tests/EditMode/Gameplay/AI/DensityPlayerChairTests.cs`.
- Added: this scoped record. No production file changed at this checkpoint.

## Native RED and minimum correction

Native job8876b31fab46453a8669f8d272f537a8 executed the real-player case and
failed on same-cell Sit returningfalse; preceding adjacent-refusal checks
passed. Raw receipt: shared-furniture-first-effect-red.json. Root removed only
the incorrect required-container gate, used a nullable seated lookup and made
rollback remove a newly created effect through the actor facade. Existing
stand rollback still uses its already-existing container. GREEN and actual
replay remain pending.


## Independent source and native-evidence review

Bounded cold review of the saved correction found no significant defect. The
nullable lookup preserves Stand refusal when the actor has no sitting effect;
any successful Stand already has the nonnull original effect container. Sit
rollback now resolves the actor's actual container after lazy creation and
removes only the exact newly created SittingEffect. Existing finally logic
restores the captured chair reservation and occupant. No blueprint, save-load
or migration change is needed.

The initial source review identified one test gap: old exception controls had
a preinstalled container. The added real-factory exception counter closes it:
an observer proves the new container, sitting effect and actual chair claim
exist before throwing; rollback removes the effect and claim, preserves HP and
position, and allows a subsequent successful Sit/Stand. Existing Stand rollback
continues to test restoring the exact previous effect. Test execution belongs
to root; this reviewer did not operate Unity.

Independently parsed evidence:

- RED job8876b31fab46453a8669f8d272f537a8 reports1 completed/1 failed,
  uncapped, at the new same-cell Sit assertion; its final Result isnull. Raw
  receipt SHA256`0f473d445775b829dcb12dd5fe43cf24cb947e2a309640e3960901014b5063ff`.
- `shared-furniture-first-effect-green.json` contains70 distinct Passed results,
  zero failures/skips:21 chair,42 bed and7 chair-ownership cases. SHA256
  `25f68d3d7765474af349c149ebc66a8268ed5b36914d8318ea42047b6c34a42c`.
- Reviewed service SHA256
  `982d4bda59c069a67c769f2871fe3f386392f6c0649d7bd9044dbd9fdca8efed`;
  fixture SHA256`4d2784dda33a9f8dee39a0b9fbf47413310f1c5637ce8b1c70ab0375716912c5`.

This clears the narrow source/native correction for publication review. Root
has reported an ordinary successful Sit on the preserved Chair12666 from an
initially absent status container; final actual Stand/save/restart evidence
is separately root-owned. This review does not claim furniture sprite
integration, packaged execution or area-polish acceptance.

## Ordinary replay and final bounds — 2026-10-27 22:35UTC

Root continued the unchanged earned108 fixture with player2695 at55,19,HP40,
no status container and the cave study disabled. Actual Look/Return/Sit created
the first SittingEffect and set Chair12666 Occupied=true/Occupant2695. Actual
Return/Stand released it; HP and location stayed unchanged. Filtered diagnostics
record PlayerSat atturn2440 and PlayerStood at2450. Two actual captures and raw
state/diagnostic evidence are in shared-furniture-first-effect-actual.json.
F5 saved earned109 atturn2460; the entire isolated session was copied and
verified before Finish. Root then observed Play=false, SaveRoot=null and a clean
scene. This is an actual replay of108, not a fresh reload of109 or packaged run.

Source fix is3 changed behavioral lines plus one explanatory comment. Native
70cases passed (21chair,42bed,7ownership), including new first-use success,
spatial refusal and actual-created-effect rollback. Independent review found
no significant issue. No art or save-format change; no new per-frame path or
performance claim. Furniture sprite integration and area polish remain open.
