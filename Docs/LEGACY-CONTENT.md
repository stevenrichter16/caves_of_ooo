# Legacy content quarantine (Phase 11, M6)

The following shipped content predates the current canon and is
**quarantined as legacy**: it remains loadable and playable, but is
not part of the canonical world, will not be extended, and should be
re-skinned or retired in a future content pass.

## Adventure-Time-era quest content

`Assets/Resources/Content/Conversations/`:
`BMO_Quest.json`, `Marceline_Quest.json`, `CinnamonBun_Quest.json`,
`CandyCitizen.json`, `CandyTax_Quest.json`, `RootBeerGuy_Quest.json`,
plus AT-flavored NPCs referenced from other quest files
(`Baker_Quest`, `Crunchy_Quest`, `Strongman_Quest`, etc. — audit at
re-skin time).

These are the game's original prototype skin (the title's "Ooo").
They are mechanically useful test content. They are not canon.

**Why not moved or deleted:** files under `Resources/` are loaded by
path; relocating them without a Unity editor session to verify would
risk breaking content loading. Deleting them is a user decision, not
a revision decision. Hence: quarantine by declaration.

## Frozen engine identifiers (not legacy — deliberate)

Per `Lore/TERMS.md`, these engine-facing IDs are **frozen for
save-compat** and intentionally diverge from display names:

| Frozen ID | Player-facing name |
|---|---|
| Faction `Palimpsest` | the Recension |
| Blueprint `PalimpsestEcho` (+ conversation `PalimpsestEcho_1`, node IDs `AboutPalimpsest`, `FactionPalimpsest`, etc.) | Recension field scribe |
| Blueprints `Mogu`/`Grib`/`Nam`/`Sien`/`Sopp` (+ conversation IDs) | Solm / Vurn / Amai / Isk / Ketch |
| Property keys `MetPalimpsest`, `PalimpsestGaveGift`, `PalimpsestHope`, `SawPalimpsestMemory`, `KnowsThreeEndings` | (invisible to players) |
| Item `TemporalShard` | described in-world as memory-marble |

Do not "clean up" these IDs; renaming them breaks saves and code
references for zero player-facing gain.
