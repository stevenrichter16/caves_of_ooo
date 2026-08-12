# Legacy content quarantine (Phase 11, M6)

> **Status update (2026-08-11): the re-skin happened.** The
> Adventure-Time *names* are retired from every player-visible surface
> — see "The retirement pass" below. The **files and IDs keep their
> old names**, which is the point of the frozen-ID rule: quarantine
> was never about the filenames, it was about what the player reads.

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

`Assets/Resources/Content/Data/Storylets/`: `RootBeerGuyCase.json`,
`BmoCartridge.json`, `TheCandyTax.json`, `CrunchyLocket.json`,
`StrongestInOoo.json`, `CinnamonBunFavor.json`, **`EnchiridionQuest.json`**
(this last one was missing from the original list — the Enchiridion is
AT-era too), `IronKeyQuest.json`.

These are the game's original prototype skin (the title's "Ooo").
They are mechanically useful test content. They are not canon.

**Why not moved or deleted:** files under `Resources/` are loaded by
path; relocating them without a Unity editor session to verify would
risk breaking content loading. Deleting them is a user decision, not
a revision decision. Hence: quarantine by declaration.

## The retirement pass (2026-08-11)

Every player-visible Adventure-Time name is gone. Every internal ID
that carried one stays exactly as it was. The register:

| Was (player-visible) | Is now | Frozen ID that did NOT change |
|---|---|---|
| Root Beer Guy, detective | Hallun, the village's witness-keeper | conversation `RootBeerGuy_Quest`, quest `RootBeerGuyCase` |
| detective notebook | witness-book | — |
| BMO | Ellun (a village child) | conversation `BMO_Quest`, quest `BmoCartridge`, fact `bmo_stump_reached` |
| game cartridge | the pith of an old tell | — |
| Peppermint Butler | Clerk Padok | conversation `CandyTax_Quest` |
| candy citizen(s) | villager(s) | conversation `CandyCitizen`, facts `candy_taxed`, `candy_taxes_collected` |
| the candy tax | the outstanding accounts / the levy | quest `TheCandyTax` |
| Crunchy | Belis | conversation `Crunchy_Quest`, quest+item `CrunchyLocket` |
| silver locket | carved name-token | — |
| Cinnamon Bun | Rullok | conversation `CinnamonBun_Quest`, quest `CinnamonBunFavor` |
| Marceline | Ondis | blueprint `Marceline`, conversation `Marceline_Quest`, table `MarcelineStock`, sprite key `marceline` |
| the Enchiridion | the Sealed Hand | item blueprint `Enchiridion`, quest `EnchiridionQuest` |
| gumdrop boulder (off candy cliffs) | the fallen mill-stone (at the old mill) | quest `StrongestInOoo` |
| candy-heart root | heartroot | blueprint `CandyHeartRoot` |
| candy carrot / seed / crop | gladroot / gladroot seed / gladroot crop | blueprints `CandyCarrot`, `CandyCarrotSeed`, `CandyCarrotCrop`; `CropSpriteKind.CandyCarrot` |

Quests also gained real titles (see `QuestData.Name`) — the quest log
used to print the raw ID, so "RootBeerGuyCase" was literally what the
player read.

**Two things deliberately NOT touched:**

1. **The Saccharine Concord.** Its name contains the target register
   and is nonetheless live canon (`Lore/10_Bible.md`). The Concord's
   material culture has no confection in it at all
   (`Lore/History/08_MaterialCulture.md`) — the name is retained
   irony, not a candy licence. Do not read it as permission to
   re-import sweetness vocabulary.
2. **The game's title.** `Lore/TERMS.md` retires "Ooo" as *in-world*
   vocabulary — and no in-world text says it any more — but the
   product name is a separate decision and is out of scope here.

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
