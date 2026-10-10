# Caves of Ooo — Steam Deck performance update

This native Linux build includes built-in controller controls, the Vulkan world-view
fix, reduced drawing and interface work, faster save capture, and background save
compression. It preserves the 3D scene and existing save format.

## Replace the old copy

1. Exit the game on the Deck, transfer the new archive, and extract it.
2. Replace the entire old `CavesOfOoo` folder at the same location. Keep the executable,
   `CavesOfOoo_Data`, and the other included files together.
3. Launch the existing non-Steam shortcut pointing at `CavesOfOoo.x86_64`.
   If you move the folder, update the shortcut's target and Start In paths.
4. Leave forced Proton compatibility off. Remove old graphics-forcing launch options;
   this build selects Vulkan itself.
5. Open **Menu → Graphics → Handheld preset**, then press B twice to return to play.
   The preset uses 75% world resolution, no shadows, and reduced detail/effects.
   Interface text stays at the display's resolution. Your choices are saved.

The included `Launch-CavesOfOoo.sh` also selects handheld defaults when no graphics
preferences exist. Saved preferences take priority. A shortcut pointing directly
at the executable can choose the same preset from the menu.

Saved games are stored separately under
`~/.config/unity3d/DefaultCompany/caves-of-ooo/Saves`.

## Built-in controls

- D-pad or left stick: move, aim and navigate; diagonals are supported.
- A: confirm / cast selected hotbar ability. B: back.
- X: inventory. Y: interact. LB: look. RB: pick up. RT: wait / underfoot.
- Menu: pause, save/load, controls, graphics and quit. View: help / item details.
- Left stick click: skills. Right stick click: abilities.
- Right stick left/right: choose hotbar slot. Up/down: page readers.
- Hold LT + A/B/X/Y/LB/RB/Menu/View/left stick click/right stick click:
  activate ability slots 1/2/3/4/5/6/7/8/9/0.
- Hold LT + up/down: stairs / world-map travel.
  Hold LT + left/right: quests / factions. Release between actions.
- At save/death prompts: A continues/loads; X starts a new character/restarts.
- At character selection: D-pad browses; A begins.

Use Steam Input's normal **Gamepad** output. Custom keyboard-only mappings can hide
gamepad buttons from the game. Keyboard/mouse controls still work; use the Steam
keyboard for text entry.

## Verification limits

`build-info.json` identifies the source revision and the checks run for this package.
The optimizations were tested and profiled in native Unity on a Mac, and this package
was built for Linux x86_64. These measurements are not Steam Deck frame-rate,
thermal, battery, audio or physical-controller certification. Large explored-world
saves still capture their live state on the main thread before background compression.
