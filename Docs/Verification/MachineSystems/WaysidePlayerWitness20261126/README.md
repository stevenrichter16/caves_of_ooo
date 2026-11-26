# A player can report a firsthand Wayside sighting

An exact visible porter corpse can be noted through the ordinary interact
menu. The note is free and private. While the actual cook is available and
searching personally, the player can give him the remembered location in
one action. A moved body leaves the historical note valid. The cook follows
the reported cell within the existing search budget. An empty or blocked
waypoint leads back to the ordinary loading-point search. The cook records
firsthand evidence only when his normal line of sight confirms it. The
delivery and recovery rules still govern when the original case closes.

The connected test run passed 194 cases. A compiling baseline had three
missing-action failures and an unchanged no-report control. Tests also
exercise a stale body quote, wrong corpse, unavailable or moved cook,
duplicates, historical-location fallback and exact saved search progress.
The independent source review identified a hidden-recipient case. Both the
note and report now require a visible owned render when they commit.

## Native input in the saved Wayside case

The private run used the earlier seed 64 archive at tick 370. It includes
the earlier controlled lateness and player-caused porter death. No body,
searcher, clock, shipment, money or status was changed by the harness.

- The player noted body `6899` from the pile at `24,16`. The clock stayed at
  370. The note stayed private. Six ordinary steps reached `22,13`.
- At tick 430, the player reported the note in person to Orven `6853`.
  The command spent one action. Fresh Continue at tick 440 kept both the
  report and the absence of any firsthand cook sighting. The free cook
  reader called it a witness report.
- In the first branch, the fresh load exposed an older pacing timer reset.
  After the extended pacing ended, the cook used three ordinary outbound
  steps from `30,13` to `27,13`, taking his existing attempt count from
  seven to ten. His own sighting of body `6899` was recorded at tick 670.
  The shelf reader at 780 reported his own finding after he returned.
  Original parcel `6896`, its two clay measures, purses and the open case
  remained intact.

## Pacing across a fresh load

The inherited timed wander stored its completed-step count privately. A
native full-session case reproduced a load from three of five steps back to
zero of five. The existing counter now saves with its goal. Saves from
before this change begin with the previous default. In the repaired Editor,
three ordinary waits from the already reported case reached three of twenty
steps at tick 470. F5 and fresh Continue kept that count, the original search
budget and the witness record. At tick 480 the next ordinary wait advanced
the same goal to four of twenty.

Independent review matched all four captures and 20 files across five
archived checkpoints. The editor stopped with
the original save root and reload state. All 861 selected normal files and
86 selected foreign workspace files matched their prior hashes.
[Timelines and archive hashes](observations.json)

[Note the body](coo-witness-live-note.png) · [Tell Orven](coo-witness-live-report.png) ·
[Witness report reader](coo-witness-live-remembered.png) · [Orven's own finding](coo-witness-live-confirmed.png)
