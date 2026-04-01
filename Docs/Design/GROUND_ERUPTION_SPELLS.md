# Ground-Eruption Spells: Design Brainstorm

## Context

Every existing spell in the game originates from the caster and travels outward:

| Spell | Pattern | Direction |
|-------|---------|-----------|
| Fire Bolt | Projectile + trail | Caster -> target (line) |
| Ice Shard | Projectile + trail | Caster -> target (line) |
| Poison Spit | Projectile + trail | Caster -> target (line) |
| Prismatic Beam | Charge -> beam | Caster -> target (line, piercing) |
| Chain Lightning | Arc -> burst | Caster -> target -> bounces |
| Frost Nova | Charge -> ring wave | Caster -> outward (radial) |
| Flaming Hands | Burst | Caster -> adjacent cell |

**What's missing**: spells where the effect comes *from the ground up* -- erupting at a target location, rising from below, or emerging from the terrain itself. This is a fundamentally different visual rhythm: instead of watching something leave your hands, you watch something *happen to the world*.

Ground-eruption spells fit the Frieren/alchemy/infrastructure themes perfectly. The caster doesn't throw power *at* a target -- they awaken something *under* it. The ground remembers. The caves have layers. Magic rises.

---

## New FX Type Required: Column Rise

The existing FX types (projectile, burst, ring wave, beam, chain arc, charge orbit, aura, particle) don't naturally express upward vertical motion on a 2D plane. We need one new effect type:

### ColumnRise FX

A sequential cell-by-cell reveal along a vertical column on screen (Y-axis), animating from bottom to top within the zone's coordinate space.

```
ColumnRiseFxInstance
  |- X: int                    // column position
  |- StartY: int               // bottom of eruption (target Y + height)
  |- EndY: int                 // top of eruption (target Y)
  |- Height: int               // how many cells tall
  |- StepDuration: float       // time per cell revealed
  |- Theme: AsciiFxTheme
  |- Glyphs: char[]            // glyph sequence bottom-to-top
  |- LingerDuration: float     // how long full column stays visible after completing
```

**Rendering**: At each step, one more cell of the column becomes visible, from `StartY` upward to `EndY`. The glyph at the newest (topmost revealed) cell animates through the glyph array. Older cells hold their final glyph or fade.

On the ASCII grid, "rising from the ground" means cells illuminate *from the bottom of the affected area upward*. Since the game's Y=0 is the top of the screen, rising means decrementing Y over time.

**Emit API**:

```csharp
AsciiFxBus.EmitColumnRise(
    Zone zone,
    int x, int targetY,
    int height,
    float stepDuration,
    float lingerDuration,
    AsciiFxTheme theme,
    bool blocksTurnAdvance,
    float delay = 0f)
```

---

## Spell 1: Stone Spike

### Fantasy

You press your hand to the ground. At a distant point, the stone floor cracks and a jagged spike erupts upward, impaling whatever stands above it. The cave itself strikes.

### Mechanics

| Property | Value |
|----------|-------|
| Type | Targeted ground eruption (pick a cell within range) |
| Targeting | `TargetCell` -- select any visible cell within range |
| Range | 5 cells |
| Damage | 3d4 piercing |
| Secondary | Immobilized 2 turns (pinned by stone) |
| Cooldown | 10 turns |
| LearnCost | `BCC` (stone/structural bits) |
| ManaCost | 10 |
| AmplificationMaterials | `BC` |

### Amplification

| Materials Spent | Bonus |
|-----------------|-------|
| None (mana only) | Base damage, 2-turn pin |
| +1 C bit | +1d4 damage |
| Full `BCC` | +50% damage, pin extends to 3 turns, spike persists as terrain (1-cell solid obstacle for 10 turns) |

### Visual Sequence

```
Phase 1 - Ground Crack (0.06s)
  Target cell flickers: '.' -> '_' -> '=' in &w (brown)
  Particles spawn at target cell: '.', ',' in &w

Phase 2 - Eruption (0.08s per cell, 3 cells tall)
  Column rise from target cell downward on screen (Y-2 to Y):
    Cell Y+2: '#' in &w (brown, base rubble)
    Cell Y+1: '^' in &y (gray, mid spike)  
    Cell Y+0: 'A' in &Y (white, spike tip -- sharp)

Phase 3 - Impact Burst (0.18s)
  Burst at target cell: '*', '+' in &w, &Y
  Particle scatter: '.', ',' in &w (debris)

Phase 4 - Linger (0.3s)
  Full spike visible, then fades top-to-bottom
  Tip 'A' fades first, base '#' fades last
```

**ASCII Snapshot (frame at peak)**:
```
  .   .        <- scattered debris particles
  A            <- spike tip (white)
  ^            <- mid spike (gray)  
  #            <- base rubble (brown)
~~+~~          <- ground crack pattern
```

### Lore Connection

This is a cave spell. It doesn't come from a spellbook -- it comes from understanding the stone. The Inkguard use something like this to seal breaches: spike the ground to block a passage. The Palimpsest view it as "asking the stone to remember when it was a wall."

---

## Spell 2: Geyser

### Fantasy

You sense the pressurized water trapped beneath the cave floor. You release it. A column of scalding water and steam erupts at a target point, launching creatures upward and drenching everything nearby. The infrastructure of the caves -- its hidden water table -- becomes your weapon.

### Mechanics

| Property | Value |
|----------|-------|
| Type | Targeted AOE ground eruption |
| Targeting | `TargetCell` -- select any visible cell within range |
| Range | 6 cells |
| Damage | 2d4 water/heat (center), 1d4 (adjacent 4 cardinal cells) |
| Secondary | Knockback 1 cell (center target pushed away from eruption) |
| Tertiary | Soaks target and adjacent cells (creates 1-dram water puddles) |
| Cooldown | 12 turns |
| LearnCost | `BBC` + 1 dram water |
| ManaCost | 12 |
| AmplificationMaterials | `BC` |

### Amplification

| Materials Spent | Bonus |
|-----------------|-------|
| None (mana only) | Base damage, small puddles (1 dram each) |
| +1 B bit | Puddles are 3 drams (wading depth in center) |
| Full `BBC` + water | Center puddle is 5 drams, knockback 2 cells, steam cloud persists 3 turns (obscures vision) |

### Visual Sequence

```
Phase 1 - Rumble (0.10s)
  Target cell vibrates: alternating '~' and '=' in &b (dark blue)
  Ring wave radius 1, very fast (0.03s), in &b -- the ground shaking

Phase 2 - Eruption Column (0.06s per cell, 4 cells tall)
  Column rise from target:
    Cell Y+3: '~' in &b (dark blue, base splash)
    Cell Y+2: '|' in &B (bright blue, water column)
    Cell Y+1: '|' in &C (cyan, upper column)
    Cell Y+0: '*' in &Y (white, steam burst at top)

Phase 3 - Splash Spread (0.08s)
  Burst at target cell: 'O', 'o', '.' in &B, &C, &b (blue cascade)
  Cardinal cells each get a burst: '~', '.' in &b
  Particle scatter around target (radius 2): '.' ',' in &b (droplets)

Phase 4 - Puddle Settle (0.15s)
  Column fades top-to-bottom
  Target cell and cardinals settle to '~' in &b (persistent puddle glyph)
  Steam particles rise (aura-style) from center: '.' '*' in &Y (white)
```

**ASCII Snapshot (frame at peak)**:
```
     *         <- steam burst (white)
     |         <- water column (cyan)
     |         <- water column (bright blue)
  .~~O~~.      <- splash spreading outward (blue)
     ~         <- base splash / puddle forming
```

### Lore Connection

This is alchemy meeting combat. The water table beneath the caves is infrastructure -- the same system that feeds wells. A geyser spell is a *violent* interaction with that infrastructure. The Rot Choir would approve: you're not preserving the water system, you're unleashing it. Villagers might be horrified: "You cracked the aquifer? That fed three wells!" Faction reputation could shift based on where you cast it.

**Alchemy tie-in**: If you've learned well-transmutation alchemy, a geyser inherits the liquid type of the nearest well. Cast it near a honey well and the geyser erupts with honey (sticky instead of knockback). Near an acid-transmuted well: acid geyser. This rewards the player for thinking about infrastructure spatially.

---

## Spell 3: Grave Salt

### Fantasy

You scatter salt across the ground and speak a word. The salt sinks, spreads beneath the surface, and erupts in a line of crystalline pillars that ward an area against the dead. Skeletons and ghosts caught in the eruption are seared. Living creatures feel nothing.

This is the Frieren "boring spell that saved the world" made real: a salt ward, cast as a line, that only affects undead. It's a maintenance spell weaponized.

### Mechanics

| Property | Value |
|----------|-------|
| Type | Targeted line eruption (pick a direction, pillars erupt in a wall) |
| Targeting | `DirectionLine` -- pick a cardinal/diagonal direction |
| Range | 5 cells (wall is 5 cells long, perpendicular to cast direction, centered on the 3rd cell) |
| Damage (undead) | 2d6 holy/salt to Skeletons, Ghosts (faction-tagged) |
| Damage (living) | 0 -- the salt does nothing to living creatures |
| Secondary | Creates salt-ward terrain line (undead cannot cross for 8 turns) |
| Cooldown | 14 turns |
| LearnCost | `BCC` + 1 dram salt |
| ManaCost | 14 |
| AmplificationMaterials | `CC` |

### Amplification

| Materials Spent | Bonus |
|-----------------|-------|
| None (mana only) | Ward lasts 8 turns, base damage to undead |
| +1 C bit | Ward lasts 12 turns |
| Full `BCC` + salt | Ward lasts 20 turns, damage becomes 3d6, ward also slows Demons (half movement for 3 turns) |

### Visual Sequence

The key visual distinction: the eruption happens *sequentially along a line*, not at a single point. Each pillar rises a beat after the previous one, creating a cascading wave of ground-eruptions.

```
Phase 1 - Salt Scatter (0.08s)
  Particles scatter from caster toward target line: '.' ',' ':' in &y (gray)
  These are the salt grains sinking into the ground

Phase 2 - Sequential Pillar Eruption (0.10s per pillar, 5 pillars)
  For each cell in the wall line (left to right or top to bottom):
    Ground crack: '=' in &y (gray)
    Column rise (2 cells tall):
      Cell Y+1: '#' in &Y (white, salt crystal base)
      Cell Y+0: '+' in &Y (white, crystal peak)
    Tiny burst at peak: '*' in &Y
    
  Each pillar starts 0.10s after the previous -- a visible cascade

Phase 3 - Ward Glow (0.20s)
  All 5 pillar cells pulse: '+' -> '*' -> '+' in &Y, &W (white/yellow alternating)
  This is the ward activating

Phase 4 - Settle (linger)
  Pillars fade to ':' in &y (gray, subtle terrain marker)
  Ward terrain persists invisibly for duration
```

**ASCII Snapshot (cascade in progress, pillar 3 of 5 erupting)**:
```
  +  +  *            <- crystal peaks (white), current eruption burst
  #  #  #  =   .     <- bases (white), next cracking, last one still settling
```

**ASCII Snapshot (ward complete)**:
```
  :  :  :  :  :      <- subtle salt line on ground (gray)
```

### Lore Connection

This is the deepest Inkbound/Frieren fusion. The Inkguard "keep the gates" -- Grave Salt is a gate made of salt. It's a defensive infrastructure spell, not a combat spell that happens to be defensive. You're building a temporary wall that only blocks specific threats.

The Salt Ward concept comes directly from folklore, but in-game it's something the Inkguard developed during the Siege. Archive stones in Inkguard territory might teach this spell, framing it as: "During the Siege, Captain [name] held the east passage for nine days with nothing but salt and will."

The Pale Curation, who curate the dead and maintain boundaries between living and not, would respect this spell. The Skeleton and Ghost factions would *hate* the caster for using it -- direct reputation penalty.

---

## New FX Requirements Summary

| FX Type | Needed For | Extends Existing? |
|---------|-----------|-------------------|
| **ColumnRise** | All 3 spells | New type in AsciiFxRenderer |
| **SequentialColumnRise** | Grave Salt (cascading pillars) | ColumnRise + delay staggering (same pattern as ChainArc hops) |
| Ground Crack particle | Stone Spike, Geyser | Existing particle system, new glyphs |
| Puddle placement | Geyser | Gameplay effect, not FX (creates LiquidVolume) |
| Ward terrain | Grave Salt | Gameplay effect (temporary solid/blocking terrain) |

### New Theme Configs Needed

**Earth Theme** (Stone Spike, Grave Salt):
```csharp
EarthConfig = new FxThemeConfig
{
    ProjectileGlyphs = Array.Empty<char>(),        // no projectiles
    ProjectileColors = Array.Empty<string>(),
    TrailGlyph = '.',
    TrailColor = "&w",
    BurstGlyphs = new[] { '*', '+', '.' },
    BurstColors = new[] { "&Y", "&w", "&y" },
    ChargeGlyphs = new[] { '.', '=', '_' },        // ground cracking
    ChargeColors = new[] { "&w", "&y" },
    RingGlyphs = new[] { '.', '=' },               // ground rumble ring
    RingColors = new[] { "&w", "&y" },
    // New: ColumnRise glyphs (bottom to top)
    ColumnGlyphs = new[] { '#', '^', 'A' },        // Stone Spike
    ColumnColors = new[] { "&w", "&y", "&Y" },
    // Salt variant
    SaltColumnGlyphs = new[] { '#', '+' },          // Grave Salt pillars
    SaltColumnColors = new[] { "&Y", "&Y" },
    AuraGlyphs = new[] { '.', ',' },               // debris particles
    AuraColors = new[] { "&w", "&y" },
    AuraInterval = 0.20f,
    ProjectileStepTime = 0f,
    BeamColors = Array.Empty<string>(),
    ChainGlyphs = Array.Empty<char>(),
    ChainColors = Array.Empty<string>()
};
```

**Water/Geyser Theme**:
```csharp
WaterConfig = new FxThemeConfig
{
    ProjectileGlyphs = Array.Empty<char>(),
    ProjectileColors = Array.Empty<string>(),
    TrailGlyph = '.',
    TrailColor = "&b",
    BurstGlyphs = new[] { 'O', 'o', '.' },
    BurstColors = new[] { "&B", "&C", "&b" },
    ChargeGlyphs = new[] { '~', '=' },             // ground rumble
    ChargeColors = new[] { "&b", "&B" },
    RingGlyphs = new[] { '~', '.' },               // splash ring
    RingColors = new[] { "&b", "&B" },
    ColumnGlyphs = new[] { '~', '|', '|', '*' },   // water column
    ColumnColors = new[] { "&b", "&B", "&C", "&Y" },
    AuraGlyphs = new[] { '.', '*' },               // steam particles
    AuraColors = new[] { "&Y", "&W" },
    AuraInterval = 0.12f,
    ProjectileStepTime = 0f,
    BeamColors = Array.Empty<string>(),
    ChainGlyphs = Array.Empty<char>(),
    ChainColors = Array.Empty<string>()
};
```

---

## How These Spells Fit the Broader Design

| Design Theme | Stone Spike | Geyser | Grave Salt |
|-------------|------------|--------|------------|
| Frieren "everyday magic" | Understanding stone | Understanding water infrastructure | A salt ward, perfected |
| Inkbound lore | Inkguard breach-sealing | Aquifer as weapon | Siege-era defensive spell |
| Alchemy connection | Earth manipulation | Liquid eruption (inherits well transmutation) | Salt as magical material |
| Spell economics | Materials amplify: spike persists as terrain | Materials amplify: bigger puddles, steam | Materials amplify: longer ward, more damage |
| Ground-up visual | Stone spike rises from floor | Water column erupts upward | Salt pillars cascade along a line |
| Faction implications | Neutral | Rot Choir approves, Villagers wary | Inkguard approves, Undead factions hate |
