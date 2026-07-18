# World Ingredients — filling the world with useful objects

> **Design catalogue** (2026-07-15), grounded in the canon (Phases
> 1–12) and written against the crafting systems on
> `claude/rpg-crafting-system-8gbflx`: emergent alchemy
> (`ReagentPart` — `PropertiesRaw` atoms, MAX-potency combining,
> `BrewRules.json`), modular weaponcraft (`WeaponComponentPart` —
> Blade/Haft/Binding slots, `NameFragment` composition), and
> tempering (quench-in-brew). Every entry is blueprint-ready: exact
> `PropertiesRaw` / component params included. **Author the JSON on
> or after the crafting branch merges** — the parts don't exist on
> other branches.
>
> Property atoms used: only the eleven that ship (`heat cold
> combustible corrosive conductive toxic binding vital sweet viscous
> volatile`), except §VI which proposes two new atoms + rules,
> flagged as needing effect support. FlavorText lines follow the
> hinted-discovery rule (name the properties in-fiction) and the
> voice cards (no design voice).

---

## I. Design principles

1. **Gathering is geography.** Each region's larder teaches its
   character: the bog preserves, the groves are generous and
   thinking, the wasteland is honest about the cold, the Overwrit
   should not be eaten from at all (and can be).
2. **The warning is the recipe.** Where canon has a folk-rule ("eat
   nothing red," "sweep toward the door"), an ingredient enforces it
   mechanically. Lore and alchemy should rhyme, not just coexist.
3. **Harvesting has owners.** A drosera ring is a village's Bloom
   defense (canon: strategic infrastructure); grove-fruit belongs to
   the grove. Stripping them works and costs standing. Gathering is
   the closure-ledger's little sibling: take, and something notices.
4. **Extend combos, don't fork them.** New reagents mostly recombine
   the existing eleven atoms at new potencies and pairings, so every
   new item multiplies against the shipped 13 instead of demanding
   new rules.

## II. Plants & fungi (forage)

| Blueprint | Display | Found | PropertiesRaw | Value | FlavorText (hinted discovery) |
|---|---|---|---|---|---|
| `DroseraDew` | drosera dew | bog fringes; village drosera rings (**owned** — see §I.3) | `corrosive:2, viscous:2` | 12 | "A bead of glassy mucilage. It clings to the jar, and the jar's label is dissolving." |
| `GroveRed` | grove-red | Choir grove edges (**the sign says eat nothing red**) | `vital:3, toxic:1` | 18 | "A fruit of astonishing health, grown from everyone the grove has ever loved. The red is thinking." |
| `PaleReed` | pale reed pith | Sill riverbanks | `vital:1, viscous:1` | 3 | "River pith, wet and mild. Grandmothers bind scraped knees with it." |
| `FestivalPetal` | festival petal | anywhere charms are cast; meadow-knot aftermath | `sweet:1, vital:1` | 4 | "A conjured petal that hasn't realized morning came. Sweet, and briefly very alive." |
| `PeatTar` | peat-tar | the bog; Drowned Ledger approaches | `combustible:2, viscous:2` | 8 | "Black bog-blood. Burns slow, sticks to everything, remembers being a forest." |
| `DarkwickCap` | darkwick cap | catacomb walls near the Sealed Dim | `cold:1, binding:1` | 10 | "A fungus that grows *dimness*. Villagers cultivate it for the nights when light is unwise." |
| `HearthPlumeCutting` | hearth-plume cutting | catacomb hearth-patches (**tended — never taken freely**) | `vital:2, binding:1` | 22 | "A pale stalk from a village hearth. It pulses faintly, like something far away breathing." |
| `SaltbriarSprig` | saltbriar sprig | the Beating; wasteland caravan routes | `binding:2, toxic:1` | 9 | "The wasteland's only hedge. Every part of it is preserved, including the parts that sting." |
| `ThornwoodBole` | thornwood bole | tepui slopes | *(weaponcraft — §V)* | 10 | — |
| `ResinWeep` | resin-weep | Bower-Folk installations' contested bog edge | `binding:3, viscous:2` | 20 | "Raw casting-resin. It wants, very gently, for you to hold still." |
| `SariThorn` | sari-thorn | Hush-edge scrub; the Overwrit's rim | `toxic:2, cold:1` | 14 | "A spine from a bush that grows where names are thin. The wound it leaves is hard to describe. Literally." |
| `EmberVeinMoss` | embervein moss | Cinderhold mine galleries | `heat:2, conductive:1` | 11 | "Mine-moss with hot metal in its veins. Miners read the galleries by where it glows angriest." |

*(Balance note: `GroveRed` is deliberately the best vital reagent in
the game AND ruins every `brew_mending`/`brew_snack` batch via its
`toxic:1` unless paired against rules that forbid nothing — the
folk-warning as brew-math. The Choir's generosity, in one item.)*

## III. Creature drops (butcher / harvest)

| Blueprint | Display | Source (bestiary canon) | PropertiesRaw | Value | FlavorText |
|---|---|---|---|---|---|
| `SariSnakeVenom` | sari-snake venom sac | Sari-Snake (the fer-de-lance of the simas) | `toxic:3` | 25 | "The grove-killer's gland. The villagers call the snake Urqu's signature; the venom doesn't care what anyone calls it." |
| `BandfrogSlime` | bandfrog slime | Bandfrogs, river + sima pools | `viscous:3, toxic:1` | 7 | "Ropes of frog-mucus. Slips through the fingers, then numbs them." |
| `StewardGulletOil` | steward gullet-oil | Sima-Steward (**sacred to villagers** — carrion-take only, or standing drops) | `combustible:3, vital:1` | 30 | "Rich orange fat from the seed-carrier bird. Burns bright and long. The deep villages will know how you got it." |
| `BroodJelly` | gin-frog brood jelly | Sima Gin Frog (back-brood shed, gathered not killed) | `vital:2, viscous:1, sweet:1` | 26 | "Left-behind brood cradle, faintly effervescent. Nothing about these frogs is explained, including this." |
| `LanternBeetleOil` | lantern-beetle oil | beetle culls, catacomb light-farms | `combustible:2, conductive:1` | 9 | "Presses out of the spent glands still warm. Light, waiting for a wick." |
| `GlassScorpionChitin` | glass scorpion chitin | glass scorpions (shipped creature) | `conductive:2, binding:1` | 16 | "Transparent plates that ring when struck. The desert makes its own glassware." |
| `ShamblerSporeSac` | shambler spore-sac | spore shamblers (shipped creature) | `toxic:2, volatile:1` | 13 | "A wheezing pouch of spores. Keep it away from flame, and from your face, and honestly from you." |
| `SnapjawFang` | snapjaw fang | snapjaws (shipped creature) | *(weaponcraft — §V)* | 6 | — |
| `WardlineScale` | wardline scale | the Wardline (protector bird-snake — **taking one is an omen**) | `binding:2, cold:1` | 28 | "A scale from the snake that guards. It refuses, mildly, to be anything else." |
| `EyelessSinew` | eyeless sinew | eyeless apex predators, deep simas | *(weaponcraft — §V)* | 20 | — |
| `BogTakenPeatHide` | bog-taken peat-hide | the Drowned Ledger (**funerary — Curation pays for intact recovery instead**) | `binding:2, toxic:1, cold:1` | 15 | "Preserved hide from something the flood kept. A thousand years old and not one day decayed. The salt-people would rather you hadn't." |

## IV. Minerals, liquids & strange matter

| Blueprint | Display | Found | PropertiesRaw | Value | FlavorText |
|---|---|---|---|---|---|
| *(existing item — add Reagent part)* `PaleSalt` | pale-salt measure | the Beating salt-flats; trade | `binding:3, cold:1` | (keep) | "The money you can eat or embalm with. Holds what it touches exactly where it is." |
| *(existing — add Reagent)* `GlowQuartz` | glow-quartz | Cinderhold; catacomb seams | `conductive:2, binding:1` | (keep) | "Light sleeps in it. Tap it and the light turns over." |
| *(existing — add Reagent)* `ChoirIron` | choir-iron | deep substrate seams | `binding:2, conductive:1, toxic:1` | (keep) | "Ore the mycelium grew through. It never fully stops being warm." |
| `TepuiboneShard` | tepuibone shard | tepui heights; the Felling-Site's approaches | `binding:3` | 40 | "Stone from the Tree's fossil anatomy. Of everything in the world, it is the surest of its own name." |
| `MuteStoneSliver` | mute-stone sliver | Hush-edge quarries | `binding:2, cold:2` | 35 | "A stone that says nothing so firmly that nearby things say less." |
| `BogAmber` | bog-amber | the bog; Drowned Ledger spoil | `combustible:1, binding:1` | 12 | "A drop of pre-Felling daylight with an insect still reading it." |
| `DewInkVial` | dew-ink vial | drosera processing; Recension trade | `corrosive:1, viscous:1` | 15 | "Scribes' ink from sundew mucilage. Glows while fresh. Bites what it's written on, gently, forever." |
| `GroveHoney` | grove honey | Choir-adjacent apiaries | `sweet:2, vital:1, viscous:1` | 10 | "Delicious. Ask no questions of the flowers." |
| `QuenchBrine` | quench-brine | Cinderhold smithies; Concord stock | `cold:2, binding:1` | 8 | "Smiths' quenching liquor, salt-hardened. The temper it gives outlasts the argument about how." |
| `BleedGlass` | bleed-glass | the Overwrit, after a bleed closes (**singular; never restocks**) | `volatile:2, binding:2` | 60 | "A pane of somewhere else's window. It is trying, quietly, to be a window again." |

*(`BleedGlass` is the loot-discipline exception that proves the rule:
one per closed bleed, per THE-OVERWRIT.md's singular-curio policy —
a late-game reagent whose `volatile+binding` pairing exists nowhere
else, enabling brews that are otherwise impossible. Do not add a
second source.)*

## V. Weapon components (`WeaponComponentPart`)

**Blades:**

| Blueprint | Display | Params | Source |
|---|---|---|---|
| `SnapjawFangPoint` | snapjaw fang point | Slot Blade, BaseDamage 1d4, PenBonus 1, Attributes Piercing, NameFragment "fang-tipped" | snapjaw drops, cheap and everywhere |
| `GlassStingerBlade` | glass stinger | Slot Blade, BaseDamage 1d6, Attributes Piercing, OnHitEffectSpec `Poison,15,1d2,10,0`, NameFragment "glass-stung" | glass scorpion rare drop |
| `ChoirIronEdge` | choir-iron edge | Slot Blade, BaseDamage 1d6, Attributes Cutting, NameFragment "choir-iron" | smithed from ChoirIron (build recipe) |
| `TepuiboneEdge` | tepuibone edge | Slot Blade, BaseDamage 1d4, HitBonus 1, Attributes Cutting, NameFragment "tepuibone" | knapped from TepuiboneShard; the name-sure stone — flavor seed for future anti-Unsaying content, no mechanics claimed yet |

**Hafts:**

| Blueprint | Display | Params | Source |
|---|---|---|---|
| `ThornwoodHaft` | thornwood haft | Slot Haft, MaxStrengthBonus 3, NameFragment "thornwood" | tepui slopes forage |
| `BogOakHaft` | bog-oak haft | Slot Haft, MaxStrengthBonus 4, HitBonus -1, NameFragment "bog-oak" | Drowned Ledger salvage — preserved millennium wood, heavy and sure |
| `ReedlashHaft` | reedlash haft | Slot Haft, MaxStrengthBonus 1, HitBonus 2, NameFragment "reedlash" | Sill rivercraft — light, quick, home-made |
| `BeetleChitinHaft` | beetle-chitin haft | Slot Haft, MaxStrengthBonus 2, DVBonus 1*, NameFragment "chitin-hafted" | catacomb beetle-culls (*if component DV is supported; else HitBonus 1) |

**Bindings:**

| Blueprint | Display | Params | Source |
|---|---|---|---|
| `EyelessSinewBinding` | eyeless sinew binding | Slot Binding, MaxStrengthBonus 1, HitBonus 1, NameFragment "sinew-bound" | deep-sima predator drops |
| `ResinWrapBinding` | resin-wrap binding | Slot Binding, HitBonus 1, OnHitEffectSpec `Wet,10,1,5,0`*, NameFragment "resin-wrapped" | Bower resin (*sticky-coating flavor; use whatever slow/wet effect exists) |
| `SaltCrustBinding` | salt-crust binding | Slot Binding, PenBonus 1 vs nothing special — flat PenBonus 1, NameFragment "salt-crusted" | Beating salt-work; the Choir dislikes being hit with it (flavor + faction log line, no special mechanics claimed) |
| `GuestclothStrip` | guest-cloth strip | Slot Binding, HitBonus 2, NameFragment "cloth-bound" | **only via Tent-Right gift or theft — binding a weapon in guest-cloth is a statement, and the Beating reads it** (reputation hook, content-time) |

## VI. Two proposed new atoms (flagged — need effect support)

Only add these if/when their effects exist; everything above works
with the shipped eleven.

1. **`luminous`** — carried by `LanternBeetleOil` (add `luminous:2`),
   `GlowQuartz` (`luminous:1`), `DewInkVial` (`luminous:1`),
   `DarkwickCap` (**`luminous:-…`** no — darkwick simply lacks it).
   Rule sketch: `brew_lamplight` — RequireAll `luminous`, ForbidAny
   `cold`, Form Coating, Effect *Glowing* (needs a light-emitting
   status effect on the coated item/actor). The catacomb economy in
   a bottle.
2. **`still`** — carried by `MuteStoneSliver` (`still:2`),
   `WardlineScale` (`still:1`), `DarkwickCap` (`still:1`). Rule
   sketch: `brew_quietus` — RequireAll `still, viscous`, Form Tonic,
   Effect *Calm* (CalmMutation's effect exists — verify it has a
   status-effect form). The Sealed Dim as a drinkable; Apatheia
   smiles.

## VII. Where-found matrix (region → larder)

| Region | Forage | Drops | Strange |
|---|---|---|---|
| Sill / river (T1) | PaleReed, FestivalPetal | Bandfrog slime, SnapjawFang | — |
| The bog / Sumphold / Drowned Ledger | PeatTar, DroseraDew | BogTakenPeatHide | BogAmber, BogOakHaft |
| The Beating (wasteland) | SaltbriarSprig | GlassScorpionChitin | PaleSalt, QuenchBrine, GuestclothStrip |
| Catacombs / Lampwell | DarkwickCap, HearthPlumeCutting | LanternBeetleOil, BeetleChitin | GlowQuartz |
| Choir groves | GroveRed, GroveHoney | ShamblerSporeSac | ChoirIron |
| Simas / tepui deeps | ThornwoodBole | SariSnakeVenom, BroodJelly, StewardGulletOil, WardlineScale, EyelessSinew | TepuiboneShard |
| Cinderhold / mines | EmberVeinMoss | — | GlowQuartz, QuenchBrine |
| Hush-edge / the Overwrit | SariThorn | — | MuteStoneSliver, BleedGlass |

## VIII. Combo seeds (what the new larder unlocks against shipped rules)

- `GroveRed` alone → ruined mending (toxic spoils vital) — the sign
  was right. `GroveRed + GlacierSalt`? Still ruined; nothing removes
  toxic — **which teaches the rule the fun way.**
- `StewardGulletOil + FireMoss` → Burning coating at combustible 3 —
  the best fire-quench in the game costs deep-village standing.
- `DroseraDew + SparkRoot` → the galvanic draught the shipped
  GlimmerBrine hints at, now reachable two ways.
- `ResinWeep + anything viscous` → premium slick coatings; resin +
  `QuenchBrine` tempering = the Bower's arrest, at forge-scale
  (flavor only until tempering rules read `binding`).
- `TepuiboneShard`/`MuteStoneSliver` → binding:3/binding+cold:
  Stoneskin tonics stop being a one-reagent trick (StoneburrSeed)
  and start being a mineral economy.

## IX. Harvesting ethics hooks (content-time, cheap)

- **Drosera rings are village Bloom-defense.** Harvesting dew from a
  ring: fine. Uprooting plants: village standing loss + a Tender's
  line. (One `BeforeTake`-style check + dialogue.)
- **Sima-Stewards are sacred seed-carriers.** Gullet-oil from a
  killed Steward marks the player to Listening-Tradition villages;
  carrion-gathered oil doesn't. (Provenance flag on the item.)
- **Hearth-plume cuttings are gifts, never takings.** Available only
  via village standing; stolen cuttings wilt to `vital:1` (the patch
  notices).
- **Guest-cloth as weapon-wrap** — the Beating reads it: +standing
  with some (the oath carried into battle), scandal with others.

## X. Authoring checklist (when the crafting branch lands)

1. Blueprints: ~30 reagent items + 12 components per the tables
   (copy `FireMoss` / `SteelBladeComponent` shapes; Reagent
   `PropertiesRaw` strings are exact above).
2. `FlavorText` params from the tables (hinted-discovery surface).
3. Zone loot/harvest tables per §VII (level-design).
4. The three ethics hooks (§IX) as content, not systems.
5. §VI atoms only after effect verification.
6. EditMode: extend the reagent-catalog content test to assert every
   `PropertiesRaw` parses and every property name is a known atom.
