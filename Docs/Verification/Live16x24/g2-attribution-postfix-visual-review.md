# G2 attribution post-fix ordinary-capture review — 2026-10-24

Independent read-only review. No Unity actions or production edits. Original untouched1280×800 PNGs inspected: A01/A12/A17/A23 and B07/B14; read all41 indexed actor/readout samples and both event records.

- A: `Docs/Verification/Live16x24/G2Action-20261024-134107-270` —24frames,20attack/damage observations.
- B: `Docs/Verification/Live16x24/G2Action-20261024-134257-723` —17frames,**6**attack/damage observations.

## Result

**No new significant visual defect found in these bounded samples.** Ordinary gameplay still displays flat native actor/environment art, legible separated damage and valid nearby status text after the corridor refusal. This is useful integration/visual regression evidence; the exact original Bleeding/Player geometry did not recur and is established by the native RED→GREEN test instead.

- A12 is a useful representative screenshot: actual previous1 and new8 on Scrabbler2071 are separate at(40,18)/(40,17), not a fabricated18. PetDog2089's `Shaken` spans30..35,row19 immediately above its current cell32,20. The yellow player41,16 is intact. Metadata/log distinguish those actual recipients and damage values.
- A17 records clamped lethal6 at the Scrabbler's finite origin40,20 while `Shaken` follows the Dog32,19. A23 shows the old origin receipt finishing alongside a distinct new1 for Warden2078; metadata preserves those separate owners. No claim that every hit retains its own floating label: A's log/events include hits omitted by the conservative placement policy.
- B07 shows the player's actual lethal2 against Gleaner2074 alongside a separate earlier2 belonging to Warden2078, on different rows. Gleaner hurt/fade is recorded, and B14→B16 clears transient readouts, removes the dead Gleaner from tracked active actors, and returns Player/Dog to actual Idle state. Player HP stays40 throughout both captures.
- Across all41 indexed samples, no two readout spans touch/overlap on the same row and no readout covers the player's logical cell. The inspected PNGs also show the player clearly. These are bounded sampled checks, not proof of every possible sprite silhouette or intermediate animation frame.
- A12's combat log states the Scrabbler is stunned while no displaced `Stunned` word is drawn above the player. This is compatible with intentional truthful omission; it is not the same event/geometry as the earlier Bleeding defect and should not be presented as an exact controlled before/after.

## Acceptance and limits

Accept this as an ordinary-play regression sample for the narrow attribution correction, alongside root's native61/61 focused and211/211 prior-regression receipts (zero skipped). Native tests prove the exact competing-actor geometry; these actual frames show that ordinary damage/status/actor/death presentation continues to work under different genuine outcomes. No gameplay/RNG injection or edited screenshot is needed to force a matching frame.

Do not claim that original Bleeding was visibly reproduced-and-omitted in this run; it did not recur. Do not infer smooth animation, audio sync, performance, physical Steam Deck experience, every guard/parry/player-hurt state, full opening or six-zone release from41 selective EOF stills. New frames support observed Player/Dog/Marlback walk variants, Player attack, enemy hurt and death/fade; they are not video. The new rule deliberately favors omission when no truthful nearby attachment exists; the log remains the full outcome record.
