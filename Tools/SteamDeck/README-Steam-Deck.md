# Caves of Ooo — Steam Deck current build

This native Linux build includes the flat **16×24 pixel presentation** as the
normal default for new graphics preferences. The opening glade and its connected
cellar, north works, Nella's plot, east shelter and Orven's kitchen have native
sprite kits. The same flat grid remains active during travel; objects without
finished native artwork keep their actual glyph and color. This is a playable
opening pass, not a claim that every biome or item has finished new artwork.

The previous presentation and its assets remain available under
**View → Graphics → World view**. A saved explicit presentation choice
is respected; toggle it to **World view: 16x24** if this installation still shows the
previous view. The build also includes the more readable examine reader and the
connected repair/introduction/harvest/replant journey. Keyboard gameplay was
checked in the editor and a standalone Mac build; physical Steam Deck operation
and performance still need device verification.

Licensed music, environmental recordings, built-in controller controls and the
Vulkan world-view fix remain included. Outdoor ambience follows daylight and
shelter, with sparse calls from nearby visible wildlife and local water, fire,
wood and stone details. Melee uses distinct blade, blunt and natural swings and
multiple contact sounds. Existing saves and the previous art are preserved.

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
5. Open **View → Graphics** if you need to change the presentation or performance
   settings, then press B twice to return to play. **Handheld preset** reduces
   world resolution, shadows and detail/effects where applicable. Interface text
   remains at display resolution, and your choices are saved.

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
  skills, abilities, quests, factions, controls and **World log**. **View** pauses for save/load,
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

## Full-screen skills

Open **Menu -> Skills** (keyboard **X**). The Field Manual fills the screen with
disciplines, powers and persistent details. **Y** switches to the optional
prerequisite overview while keeping the selected power. Connections show actual
requirements, never an invented progression chain.

- **LB/RB** changes discipline; **D-pad/left stick** moves between panes and powers.
- **A** reviews a purchase. Review starts on Cancel; choose Learn with Right and
  press A to spend the displayed SP. **B** cancels review before closing the screen.
- **X** assigns an owned active ability to one of ten hotbar slots; A commits and
  B cancels. Assignment moves an existing binding and replaces that slot's occupant.
- **Right stick up/down** pages the selected skill's full details. Browsing,
  purchasing and assignment do not spend a world turn or activate an ability.
- Keyboard: arrows navigate, **Q/E** change discipline, **Tab** switches view,
  **Enter** reviews/confirms, **I** assigns, **Page Up/Down** reads, **Esc** backs out.
  Mouse clicks select rows/cards/buttons; the wheel browses the pane under it.

Names, ownership, SP costs, actual/current attribute requirements, range and
cooldown stay readable at handheld resolution. Skills learned from books remain
owned even when their parent discipline has not been purchased.

## Skills and world log

The compact bar keeps the same ten ability slots and controls. It shows each slot's
name and whether it is ready, cooling down or being aimed. The selected ability and
current targeting hint appear above the row; a **>** marks clipped text. Open the
existing ability reader for the full name and mechanics. **Ready** means
the cooldown is over; the ability's other requirements still apply when you use it.

The recent sidebar log groups adjacent identical messages only when they have the
same recorded tick. It keeps the underlying messages unchanged. Open **Menu →
World log** to read the full retained history, including messages older than the
sidebar window. **LB/RB** pages and **B** returns to play or Look. Reading history
is free and does not consume queued announcements or play physical-book sounds.

With a keyboard, **F2** opens World log from play or Look. Use **Page Up/Page Down**
or the arrow keys to page, **Home/End** for oldest/latest, and **Escape** to close.
F2 keeps its existing meaning inside other screens. History labels such as
**Tick 120** show the simulation clock, not a count of your actions.

## Music, ambience and sound effects

Open **View → Audio**. Select Music, Ambience or Sound effects with D-pad up/down.
**D-pad left/right** lowers/raises that level by ten percentage points; **A** also
raises it. **0% mutes that channel**. **B** returns one menu level. These independent
settings are saved and do not spend a turn. Music continues while you read menus.
The sound-effects level also scales the existing spell sounds; it does not change
spell animation speed or the music/ambience levels.

Music follows the current region, including world-map travel, and interrupted
compositions retain their place during the current session. Calm listening can
move to another complete piece after a quiet gap; local environmental sounds
continue through that gap. Morrowfast has its own musical setting. Changing Music
or Ambience to a nonzero level fades to that level; 0% mutes immediately.
Combat briefly lowers the background mix so actions remain clear.

Local details follow the actual place and visible sources: sheltered rooms,
windblown plants, flowing water and wildlife can sound different. Frozen or
empty water sources and resting animals do not keep sounding as if active.
Water handling, cooking, brewing, item handling and mineral gathering have quiet
feedback tied to the completed action. Brewing sounds indicate preparation,
not a guarantee of a perfect result. Physical documents make paper sounds on
real page changes; ordinary messages and the World log do not.

Choose **Audio credits** for the full paginated attribution in the game. The same
text is supplied in `AUDIO-CREDITS.txt`, with complete license texts in
`Audio-Licenses`. The seven full compositions are independently licensed Scott
Buckley works; no Caves of Qud music is included. Environmental and action recordings
are CC0, with their authors and modifications retained in the credits. Separate
place, atmosphere, utility, defense and regional-music credit and provenance files are
included alongside the combined `AUDIO-CREDITS.txt`. Quiet gaps are intentional:
indoor spaces suppress outdoor wildlife, and empty workshops do not play
imaginary workers.

For videos or livestream recordings, Scott Buckley uses YouTube Content ID. Put
these complete credit lines in your video description, or set **Music to 0%** before
recording. In-game credits alone do not guarantee that uploads avoid claims:

- 'Shadows and Dust' by Scott Buckley - released under CC-BY 4.0. www.scottbuckley.com.au
- 'Decoherence' by Scott Buckley - released under CC-BY 4.0. www.scottbuckley.com.au
- 'Ephemera' by Scott Buckley - released under CC-BY 4.0. www.scottbuckley.com.au
- 'Signal to Noise' by Scott Buckley - released under CC-BY 4.0. www.scottbuckley.com.au
- 'Cirrus' by Scott Buckley - released under CC-BY 4.0. www.scottbuckley.com.au
- 'Meanwhile' by Scott Buckley - released under CC-BY 4.0. www.scottbuckley.com.au
- 'Hiraeth' by Scott Buckley - released under CC-BY 4.0. www.scottbuckley.com.au

See the [composer's usage instructions](https://www.scottbuckley.com.au/library/using-this-music/)
and [CC-BY 4.0 license](https://creativecommons.org/licenses/by/4.0/).

## Verification limits

This package is built from the source identified in `build-info.json`. Its Linux
build and archive are checked locally. The actual standalone screenshots are
from macOS/Metal, not Linux/Vulkan or Steam Deck. Current reader tests and the
ordinary six-site journey do not certify all sprites, every service, all seeds,
physical controls, or complete regional art. Older performance and audio
measurements below remain historical and are not a fresh benchmark of this build.

`build-info.json` identifies the source revision and the checks run for this package.
The optimizations were tested and profiled in native Unity on a Mac, and this package
was built for Linux x86_64. These measurements are not Steam Deck frame-rate,
thermal, battery, audio-latency or physical-controller certification. The audio and
HUD update makes no percentage performance-improvement claim. Audio source
counters, file checks and nonzero mixer samples do not establish subjective musical
quality or speaker/headphone balance. Large explored-world
saves still capture their live state on the main thread before background compression.
