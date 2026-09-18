# Regional guidance and travel notes

Status: implemented and verified. Focused/adversarial checks pass, with actual
Vennit dialogue, Q/Tab notes and saved-note restoration verified in CGN06.

Scribes, innkeepers and the actual Morrowfast clerk Vennit can offer the four nearest actual surface Village POIs, excluding their current town. Ordering is Manhattan map distance, then Y/X; generation is never forced by a conversation. Leads name the destination, coordinates and directions from the current town. They do not promise a god audience, particular stock, safety or an unexplored destination's surviving quest giver.

An attached live zone graph, living player and supported living speaker are required. Hostile contacts are rejected unless explicitly permitted to speak to hostiles. An already cached town with no living conversational resident is omitted. Canonical work is suggested only while neither active nor complete; if its target graph is cached, its living giver must still exist. Selecting a previously displayed choice recomputes eligibility, so a removed POI or dead contact cannot record stale offered directions.

Selected notes live in the player's ordinary Properties, at most 17 distinct destinations, and update rather than duplicate. They retain the original departure coordinates; their compass bearings do not pretend to follow the player. Quest availability text is reevaluated when notes are read. Notes are remembered advice, not a live simulation of subsequent destination changes.

Q opens the existing journal. Tab switches between quests and travel notes; PageUp/PageDown or left/right paginate notes; Q/Escape closes normally. Notes work with zero quests. Objective text now wraps within the quest panel so the early expedition's return destination is not silently clipped off one long line. The existing quest list still has finite screen height; full quest-list pagination is outside this slice.

## Verification

- CG06 captured missing-guidance/UI-type compile RED before production; no assertions claimed.
- CG07 initial 11 guidance cases passed, including nearest-map selection, no forced generation, dynamic work suppression, stale-choice rejection, properties persistence, real conversation action routing and journal control state.
- CG09 added five adversarial cases; exactly three failed as predicted. They drove hostility/death guards and objective wrapping. The other cases pinned existing stale-zone protection and real entity-body save serialization.
- CG10/CG11 revealed an actor-transfer error in the Vennit fixture before it reached the topic assertion: the actor still belonged to its setup zone. The fixture now removes that actor before placing it in Morrowfast. This is not evidence of a missing-topic assertion RED. The new branch accepts only the live `east-robed-resident` native owner; an impostor with the same conversation ID is rejected. Initial guidance, guard, save-body and objective-render cases passed.

- CG12 removed only the Vennit topic in the isolated validation copy: the corrected native fixture failed at its missing-topic assertion. Restoring the exact JSON bytes yielded a passing Vennit case in CG13; all guidance cases passed that full run. Other phase regressions remain tracked separately.

Counterchecks distinguish valid/dead/detached/hostile contacts, active/completed versus available work, removed versus surviving POIs, live versus replaced zone graphs and known ruined versus unvisited towns. An actual Tilemap render checks that a 250-character objective includes its final return destination and journal footer. Headless tests do not certify visual typography or comfortable gameplay-camera reading; native acceptance must inspect those.

Qud reference: not applicable. These are CoO map and conversation systems. No save migrations, remote world simulation or new quest instances are introduced. Existing village quests remain six canonical stories (see CANONICAL-VILLAGE-QUESTS.md).

### Final full-suite comparison

CG14 passed all 354 targeted cases. CG15 completed with 14,726 passed and
32 failed, zero C# errors. Its exact 32 failure names match the pre-change
CG01 baseline; no new failures remain. See the central implementation report
for rendered acceptance and its limits.

Final acceptance: CG17 356/356 focused, CG18 14,728 passed with the same 32
pre-existing failures, zero C# errors. CGN06 passed all 28 live checks and
60-second aggregate editor profiling; see the central implementation/native
reports for exact scope, visual evidence and performance bounds.
