# Caves of Ooo — Steam Deck current build

This native Linux build adds licensed ambient music, environmental recordings and
short sounds for movement and committed actions. It also includes Qud-style
built-in controller controls, the Vulkan world-view fix, reduced drawing and
interface work, faster save capture, and background save compression. It preserves
the 3D scene and existing save format.

This update also completes companion pack transfers, deliberate equipment
changes and safe step-aside orders. Adjacent recruits offer these choices through
their interaction menu; pack inspection and gear comparison are free. Following
companions can assist after your direct spell or weapon-skill damage. Stay orders
still prevent offensive assistance.

Prismreed pith can wick oil, pitch or honey from you or a willing nearby ally.
Successful food and tonic use now costs an action. Coins taken from containers
go directly to your purse, armor comparisons explain which body slots receive
protection, and ditch-cutter mates can shove with their cudgels. Tinkering remains
unavailable in normal play.

## Replace the old copy

1. Exit the game on the Deck, transfer the new archive, and extract it.
2. Keep the new `CavesOfOoo-current` folder together. You may keep the old copy
   separately. The executable, `CavesOfOoo_Data` and other included files are all needed.
3. Point your non-Steam shortcut at `CavesOfOoo-current/CavesOfOoo.x86_64`.
   Set Start In to the `CavesOfOoo-current` folder; an old shortcut must be updated.
4. Leave forced Proton compatibility off. Remove old graphics-forcing launch options;
   this build selects Vulkan itself.
5. Open **View → Graphics → Handheld preset**, then press B twice to return to play.
   The preset uses 75% world resolution, no shadows, and reduced detail/effects.
   Interface text stays at the display's resolution. Your choices are saved.

A shortcut pointing directly at the executable uses handheld defaults when Steam
provides `SteamDeck=1` and graphics preferences have not been saved. The included
`Launch-CavesOfOoo.sh` also selects handheld defaults. Saved preferences always
take priority. An explicit `COO_HANDHELD=1` requests handheld defaults;
`COO_HANDHELD=0` disables that automatic choice. The Graphics menu remains available
if Steam does not provide its device hint.

Saved games are stored separately under
`~/.config/unity3d/DefaultCompany/caves-of-ooo/Saves`.

## Built-in controls

The layout follows Qud's gamepad button positions, adapted to this game's actions.
Menu is the right small button; View is the left small button. L3/R3 mean pressing
the left/right stick.

- **Left stick selects a direction; RT steps in that direction.** Diagonals work.
  With the stick centered, RT waits one turn. Holding RT repeats until danger stops
  it; release RT before stepping again. Selecting a direction alone is free.
- **A** uses the indicated cell, or a contextual action underfoot/nearby with the
  stick centered. **LT + A** opens nearby interactions with the stick centered,
  or acts in the indicated direction. These actions include pickup and talking.
- **B** opens a nearby bed/campfire's recovery actions when injured; at full health
  it does nothing. Recovery needs an available rest service or healing item.
  **LT + B** offers waiting for 1, 10 or 100 turns, stopping for danger. Waiting
  does not heal.
- **D-pad left/right** selects a hotbar ability; **X** uses it. **LT + X** opens all
  abilities. In that screen, select an ability and press **Y** to choose its hotbar
  slot, then **A** to assign it or **B** to cancel. There is one page of ten slots;
  **LT + D-pad up/down** explains that limit instead of changing pages.
- **LB** attacks the nearest adjacent hostile. **LT + LB** attacks in a chosen
  direction. **LT + RB** opens a picker for carried throwable items.
- **Y** walks toward a local edge; **LT + Y** explores safe local frontiers;
  **L3** lists visible known points of interest. Travel stops for danger or new
  input, stays in the current zone and does not loot automatically.
- **Right stick** looks around. In targeting, sticks/D-pad choose a cell,
  **A or RT** confirms and **B** cancels.
- **D-pad up/down** uses stairs or world-map travel. Release between actions.
  **LT + D-pad left/right** zooms in/out. **LT + RT** highlights visible points
  of interest.
- **Menu** opens character screens: inventory/equipment, attributes/effects,
  skills, abilities, quests, factions and controls. **View** pauses for save/load,
  controls, graphics, audio and quit. **LT + Menu** opens controls directly.
- In menus, **D-pad/left stick** navigates, **A** confirms and **B** goes back.
  **Y** switches tabs and **LB/RB** changes pages where that screen supports them.
  **Right stick up/down** pages readers; **LT + RT** opens item details where
  available. At save/death prompts, **A** continues/loads and **X** starts a new
  character/restarts. At character selection, D-pad browses and A begins.

**RB fire, R3 reload and LT + R3 replace cell are reserved:** missile weapons,
reload and energy cells are unavailable in this build. Pressing them explains
the limitation. This is not full Qud gameplay or menu parity.

Use Steam Input's normal **Gamepad** output. Custom keyboard-only mappings can hide
gamepad buttons from the game. Keyboard/mouse controls still work; use the Steam
keyboard for text entry.

## Music, ambience and sound effects

Open **View → Audio**. Select Music, Ambience or Sound effects with D-pad up/down.
**D-pad left/right** lowers/raises that level by ten percentage points; **A** also
raises it. **0% mutes that channel**. **B** returns one menu level. These independent
settings are saved and do not spend a turn. Music continues while you read menus.
The sound-effects level also scales the existing spell sounds; it does not change
spell animation speed or the music/ambience levels.

Choose **Audio credits** for the full paginated attribution in the game. The same
text is supplied in `AUDIO-CREDITS.txt`, with complete license texts in
`Audio-Licenses`. The three full compositions are independently licensed Scott
Buckley works; no Caves of Qud music is included. Environmental and action recordings
are CC0, with their authors and modifications retained in the credits.

For videos or livestream recordings, Scott Buckley uses YouTube Content ID. Put
these complete credit lines in your video description, or set **Music to 0%** before
recording. In-game credits alone do not guarantee that uploads avoid claims:

- 'Shadows and Dust' by Scott Buckley - released under CC-BY 4.0. www.scottbuckley.com.au
- 'Decoherence' by Scott Buckley - released under CC-BY 4.0. www.scottbuckley.com.au
- 'Ephemera' by Scott Buckley - released under CC-BY 4.0. www.scottbuckley.com.au

See the [composer's usage instructions](https://www.scottbuckley.com.au/library/using-this-music/)
and [CC-BY 4.0 license](https://creativecommons.org/licenses/by/4.0/).

## Verification limits

`build-info.json` identifies the source revision and the checks run for this package.
The optimizations were tested and profiled in native Unity on a Mac, and this package
was built for Linux x86_64. These measurements are not Steam Deck frame-rate,
thermal, battery, audio-latency or physical-controller certification. Audio source
counters, file checks and nonzero mixer samples do not establish subjective musical
quality or speaker/headphone balance. Large explored-world
saves still capture their live state on the main thread before background compression.
