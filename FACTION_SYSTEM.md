# Faction System Analysis and Implementation Plan

## Part 1: Caves of Qud Faction System Analysis

### Overview

Qud's faction system is a deep, interconnected web of reputation tracking, NPC allegiance, ritual bonding, and dynamic relationship management. The core architecture spans 20+ files across multiple namespaces.

### Core Architecture

#### Faction Definition (`Faction.cs` - 2,075 lines)

Each faction is a data-rich object loaded from `Factions.xml`:

```
Faction
  |- Name, DisplayName, Visible, Old, HatesPlayer, Pettable, Plural
  |- InitialPlayerReputation (int, e.g. -140 for Joppa)
  |- FactionFeeling: Dict<string, int>     -- how this faction feels about others
  |- PartReputation: Dict<string, int>     -- reputation modifiers from body parts/mutations
  |- Interests: List<FactionInterest>      -- what secrets/items they buy/sell
  |- Ranks: List<string>                   -- rank titles (Initiate, Knight, etc.)
  |- RankStandings: Dict<string, int>      -- rep required for each rank
  |- Parent: string                        -- inherits feelings from parent faction
  |- Worshippables, HolyPlaces             -- worship/religion system
  |- WaterRitual*                          -- 30+ properties for ritual configuration
```

#### Faction Manager (`Factions.cs` - 1,192 lines)

Static manager that loads all factions from XML and provides queries:
- `Get(name)`, `GetIfExists(name)`, `GetByDisplayName(name)`
- `GetFeelingFactionToFaction(a, b)` -- cross-faction feeling
- `GetFeelingFactionToObject(faction, entity)` -- faction-to-entity feeling
- `GetRandomFaction()`, `GetMostLiked()`, `GetMostHated()`

#### Reputation System (`Reputation.cs` - 1,064 lines)

Tracks the **player's** reputation with every faction:

```
Dictionary<string, float> ReputationValues   -- raw reputation numbers
Dictionary<string, string> FactionRanks      -- player's rank in each faction
```

**Reputation Thresholds (from RuleSettings.cs):**

| Level | Threshold | Feeling | Color |
|-------|-----------|---------|-------|
| HATED | <= -600 | -100 | Bright Red |
| DISLIKED | <= -250 | -50 | Dark Red |
| INDIFFERENT | -250 to 250 | 0 | Cyan |
| LIKED | >= 250 | 50 | Green |
| LOVED | >= 600 | 100 | Bright Green |

`REPUTATION_BASE_UNIT = 50` -- the standard unit for all reputation changes.

The `GetFeeling()` method converts raw reputation into a feeling value (-100 to +100) used by the Brain AI:
- Rep <= HATED: feeling = -100
- Rep <= DISLIKED: feeling = -50
- Rep < LIKED: feeling = 0
- Rep < LOVED: feeling = 50
- Rep >= LOVED: feeling = 100

#### NPC Allegiance (`Brain.cs` - 4,426 lines)

Each NPC's Brain has:

```
AllegianceSet Allegiance     -- stack of faction memberships with weights
StringMap<int> FactionFeelings  -- per-NPC feeling overrides toward factions
Dictionary<string, int> Opinions -- personal feelings about specific entities
```

**Feeling Calculation (priority order):**
1. Personal opinion (from `Opinions` dict, if target has ID)
2. Faction feeling (from `Allegiance.GetBaseFeeling()` + `FactionFeelings` override)
3. Combat context (-25 if target attacked a friend)
4. Leader feelings (inherit from party leader)
5. Hostile flag override (if `Hostile=true` and feeling < 50, floor to -50)
6. Calm flag override (if `Calm=true` and -50 <= feeling < 0, set to 0)

**Hostility thresholds:**
- `FEELING_HOSTILE_THRESHOLD = -10` (feeling <= -10 is hostile)
- `FEELING_ALLIED_THRESHOLD = 50` (feeling >= 50 is allied)

#### AllegianceSet -- Faction Membership

Dictionary-like structure supporting weighted multi-faction membership:

```
AllegianceSet : StringMap<int>
  |- SourceID: int              -- who caused this allegiance
  |- Previous: AllegianceSet    -- previous allegiance (stack)
  |- Reason: IAllyReason        -- why they joined
  |- Flags: int                 -- Hostile/Calm flags

AllegianceLevel:
  None       = 0
  Associated = >0
  Affiliated = >=50
  Member     = >=75
```

The base feeling is a **weighted average** of all faction memberships' feelings toward the target.

#### GivesRep -- Kill Reputation

When an NPC with `GivesRep` dies:
- Factions that **love** the NPC's faction lose rep with the killer: `-repValue * 2`
- Factions that **hate** the NPC's faction gain rep with the killer: `+repValue`
- Related factions (friends/enemies) also affected
- Killing a water-ritual-bonded NPC: -100 rep with ALL visible non-hating factions

#### Water Ritual System

The primary relationship-building mechanic:
- Player shares liquid with NPC representative
- Grants reputation with the NPC's faction (base: 50-100 units)
- Can learn skills, mutations, recipes
- Can receive gifts, buy special items
- Can join the faction
- Performance multiplier based on NPC properties
- Violation (killing bonded NPC) gives massive penalty to ALL factions

#### Faction Data (Factions.xml)

```xml
<faction Name="Joppa"
         DisplayName="villagers of Joppa"
         Visible="true"
         InitialPlayerReputation="-140"
         HatesPlayer="false">

  <feeling About="Beasts" Value="-50" />
  <feeling About="Wardens" Value="100" />
  <feeling About="*" Value="0" />

  <partreputation About="ThickFur" Value="100" />

  <waterritual Liquid="water" Skill="CookingAndGathering_Harvestry"
               SkillCost="75" Join="true" Mutation="ThickFur" MutationCost="1" />

  <interests BuyTargetedSecrets="true" SellTargetedSecrets="true">
    <interest Tags="snapjaw,settlement" WillBuy="true" WillSell="false" />
  </interests>

  <ranks>
    <rank Name="Initiate" Standing="0" />
    <rank Name="Knight" Standing="10" />
  </ranks>
</faction>
```

#### Event System

Reputation changes flow through events that can be intercepted:
- `ReputationChangeEvent` -- fired before any reputation modification, allows parts to modify the amount
- `AfterReputationChangeEvent` -- fired after change is applied
- `GetFeelingEvent` -- fired when calculating NPC feelings, allows modification
- `GetFactionRankEvent` -- fired when querying player's faction rank

---

## Part 2: Current State of Caves of Ooo

### What Already Exists

| Feature | Status | Location |
|---------|--------|----------|
| Flat faction feeling table | Implemented | `FactionManager.cs` |
| Per-entity personal hostility | Implemented | `BrainPart.cs` (HashSet<Entity> PersonalEnemies) |
| Faction-based hostility in AI | Implemented | `AIHelpers.FindNearestHostile()` |
| Conversation faction checks | Implemented | `ConversationPredicates.cs` (IfFactionFeelingAtLeast, IfNotHostile) |
| Conversation faction actions | Implemented | `ConversationActions.cs` (ChangeFactionFeeling) |
| Conversation hostility blocking | Implemented | `ConversationManager.cs` |
| Trade system with drams | Implemented | `TradeSystem.cs` |
| 7 factions with NPC representatives | Implemented | `Objects.json` + conversation files |
| Line-of-sight hostility detection | Implemented | `AIHelpers.HasLineOfSight()` |
| Test coverage | Implemented | `FactionAITests.cs` (50+ tests) |

### Current Factions

| Faction | Feeling toward Player | NPCs |
|---------|----------------------|------|
| Snapjaws | -100 (hostile) | Snapjaw, SnapjawScavenger, SnapjawHunter |
| Villagers | +20 (friendly) | Villager, Elder, Tinker, Merchant |
| RotChoir | 0 (neutral) | ChoirTendril |
| Palimpsest | 0 (neutral) | PalimpsestEcho |
| SaccharineConcord | 0 (neutral) | SaccharineEnvoy |
| PaleCuration | 0 (neutral) | PaleCurator |
| GlassblownRemnant | 0 (neutral) | GlassblownDrifter |

### Current Limitations

| Feature | Issue |
|---------|-------|
| No persistence | Faction feelings reset on game restart |
| No player reputation tracking | Only faction-to-faction feelings, no per-player reputation value |
| No kill reputation | Killing faction members has no reputation consequence |
| No trade price modifiers | Prices use only Ego stat, not faction standing |
| No faction data file | Factions hard-coded in `FactionManager.Initialize()` |
| No reputation UI | Player cannot see faction standings |
| No inter-faction dynamics | Fixed relationships, no wars/alliances |
| No faction rewards | No items/skills unlocked at reputation thresholds |
| Non-Villager traders have no stock | SaccharineEnvoy has 300 drams but nothing to sell |

---

## Part 3: Prerequisite Dependencies

Before implementing the full faction system, the following must exist or be enhanced:

### P0 -- Hard Prerequisites (must exist first)

#### 1. Save/Load System
- **Why**: Faction reputation must persist across sessions
- **Current state**: Unknown -- needs investigation
- **Required**: Serialize `FactionManager` state (faction feelings) and per-entity properties (personal hostility, conversation flags like `MetChoir`)
- **Blocks**: Everything about persistent reputation

#### 2. Faction Data File (`Factions.json`)
- **Why**: Factions are currently hard-coded in `FactionManager.Initialize()`. A data-driven approach is needed for extensibility
- **Current state**: Factions only exist as tags on NPC blueprints + hardcoded Initialize()
- **Required**: JSON file defining all factions with their properties, initial feelings, display names
- **Blocks**: Faction ranks, water ritual config, interest tags, initial reputations

### P1 -- Soft Prerequisites (can be built alongside)

#### 3. Death Event / Kill Tracking
- **Why**: Reputation changes on kill require intercepting entity death
- **Current state**: Needs investigation -- does `BeforeDeathRemoval` or similar event exist?
- **Required**: An event or hook that fires when an entity is killed, providing both the victim and the killer
- **Blocks**: Kill reputation system

#### 4. Player Reputation Object
- **Why**: Current system only tracks faction-to-faction feelings (-100 to +100). Qud uses a separate `Reputation` object with float values per faction (-600 to +600+) mapped to feeling levels
- **Current state**: No per-player reputation tracking beyond faction feelings
- **Required**: A `Reputation` component/object on the player entity storing `Dict<string, int>` of faction reputation values
- **Blocks**: Reputation thresholds, ranks, UI display

#### 5. Trade Stock for Non-Villager Merchants
- **Why**: SaccharineEnvoy, PaleCurator etc. have drams but no items to sell
- **Current state**: `TradeStockBuilder` only stocks "Villagers" faction
- **Required**: Extend to stock any NPC with Commerce tag, or add faction-specific stock lists
- **Blocks**: Meaningful trade with faction NPCs

### P2 -- Nice to Have Before Implementation

#### 6. Message Log Enhancements
- **Why**: Reputation changes need clear player feedback ("Your reputation with the Rot Choir improves.")
- **Current state**: Basic `MessageLog.Add()` exists, `ChangeFactionFeeling` already logs
- **Required**: Color-coded messages, threshold notifications ("The Rot Choir now considers you an ally!")

#### 7. UI Screen Framework
- **Why**: Player needs to see faction standings
- **Current state**: InventoryUI and TradeUI exist as 80x45 tilemap screens
- **Required**: Same framework used for a FactionUI screen

---

## Part 4: Proposed Implementation

### Phase 1: Data-Driven Factions

**Create `Assets/Resources/Content/Data/Factions.json`:**

```json
{
  "Factions": [
    {
      "Name": "Snapjaws",
      "DisplayName": "the Snapjaws",
      "Visible": true,
      "InitialPlayerReputation": -100,
      "Feelings": {
        "Villagers": -100,
        "Player": -100
      }
    },
    {
      "Name": "Villagers",
      "DisplayName": "the Villagers",
      "Visible": true,
      "InitialPlayerReputation": 50,
      "Feelings": {
        "Snapjaws": -100,
        "Player": 20
      }
    },
    {
      "Name": "RotChoir",
      "DisplayName": "the Rot Choir",
      "Visible": true,
      "InitialPlayerReputation": 0,
      "Feelings": {
        "Palimpsest": -25
      }
    },
    {
      "Name": "Palimpsest",
      "DisplayName": "the Palimpsest",
      "Visible": true,
      "InitialPlayerReputation": 0,
      "Feelings": {
        "RotChoir": -25
      }
    },
    {
      "Name": "SaccharineConcord",
      "DisplayName": "the Saccharine Concord",
      "Visible": true,
      "InitialPlayerReputation": 0,
      "Feelings": {}
    },
    {
      "Name": "PaleCuration",
      "DisplayName": "the Pale Curation",
      "Visible": true,
      "InitialPlayerReputation": 0,
      "Feelings": {
        "RotChoir": -15
      }
    },
    {
      "Name": "GlassblownRemnant",
      "DisplayName": "the Glassblown Remnant",
      "Visible": true,
      "InitialPlayerReputation": 0,
      "Feelings": {}
    }
  ]
}
```

**Modify `FactionManager.cs`:**
- Load factions from JSON instead of hardcoding
- Store a `FactionData` object per faction (DisplayName, Visible, etc.)
- Keep the flat feeling table but populate from JSON

### Phase 2: Player Reputation System

**Create `PlayerReputation.cs`:**

```csharp
public class PlayerReputation
{
    // Raw reputation values per faction
    private Dictionary<string, int> _reputation = new Dictionary<string, int>();

    // Thresholds (adapted from Qud, scaled down)
    public const int HATED_THRESHOLD = -150;
    public const int DISLIKED_THRESHOLD = -50;
    public const int LIKED_THRESHOLD = 50;
    public const int LOVED_THRESHOLD = 150;

    public enum Attitude { Hated = -2, Disliked = -1, Neutral = 0, Liked = 1, Loved = 2 }

    public int Get(string faction);
    public void Set(string faction, int value);
    public void Modify(string faction, int delta);  // with message + threshold checks
    public Attitude GetAttitude(string faction);
    public int GetFeeling(string faction);  // maps attitude to feeling for FactionManager
}
```

**Integrate with `FactionManager`:**
- `GetFeeling(player, npc)` now checks `PlayerReputation.GetFeeling(npc.faction)` instead of the flat table
- Faction-to-faction feelings remain in the flat table
- Player reputation modifiable by conversations, kills, quests

**Reputation-to-Feeling Mapping:**

| Attitude | Reputation Range | Feeling Value | Behavior |
|----------|-----------------|---------------|----------|
| Hated | <= -150 | -100 | Hostile, attacks on sight |
| Disliked | -150 to -50 | -50 | Hostile (below -10 threshold) |
| Neutral | -50 to 50 | 0 | Indifferent |
| Liked | 50 to 150 | 50 | Allied, trade bonuses |
| Loved | >= 150 | 100 | Allied, best prices, special rewards |

### Phase 3: Kill Reputation

**Add `GivesRepPart` to NPC blueprints:**

```json
{
  "Name": "ChoirTendril",
  "Parts": {
    "GivesRep": { "Value": 25 }
  }
}
```

**On entity death:**
1. Get the victim's faction and `GivesRep.Value`
2. Decrease killer's reputation with victim's faction by `Value`
3. For each faction that is hostile to victim's faction (feeling <= -50): increase killer's rep by `Value / 2`
4. Log messages: "Your reputation with the Rot Choir decreases." / "Your reputation with the Palimpsest improves."

### Phase 4: Trade Price Modifiers

**Modify `TradeSystem.GetBuyPrice` and `GetSellPrice`:**

```
Faction bonus = PlayerReputation.GetAttitude(traderFaction):
  Liked:  5% discount on buys, 5% bonus on sells
  Loved: 15% discount on buys, 15% bonus on sells
  Disliked: 10% markup on buys, 10% penalty on sells
  Hated: refuse to trade (already blocked by conversation hostility)
```

### Phase 5: Faction Reputation UI

**Create `FactionUI.cs`** (80x45 tilemap screen, opened with `F` key):

```
+------ Faction Standings ------------------------------------+
|                                                              |
|  the Rot Choir              50  [====------]  Liked          |
|  the Palimpsest             12  [=---------]  Neutral        |
|  the Saccharine Concord      0  [----------]  Neutral        |
|  the Pale Curation          -8  [----------]  Neutral        |
|  the Glassblown Remnant      0  [----------]  Neutral        |
|  the Villagers              75  [=====-----]  Liked          |
|  the Snapjaws             -100  [----------]  Disliked       |
|                                                              |
+--------------------------------------------------------------+
|  [Esc] close                                                 |
+--------------------------------------------------------------+
```

Color-coded: Hated=red, Disliked=dark red, Neutral=gray, Liked=green, Loved=bright green.

### Phase 6: Conversation Reputation Gates

**Add new predicate `IfReputationAtLeast`:**

```csharp
Register("IfReputationAtLeast", (speaker, listener, arg) =>
{
    // arg = "FactionName:Level" e.g. "RotChoir:Liked"
    var parts = arg.Split(':');
    if (parts.Length < 2) return false;
    var attitude = PlayerReputation.GetAttitude(parts[0]);
    var required = Enum.Parse<Attitude>(parts[1]);
    return (int)attitude >= (int)required;
});
```

This allows dialogue options gated by reputation:

```json
{
  "Text": "The Choir recognizes you as a friend. We will share deeper truths.",
  "Target": "DeepLore",
  "Predicates": [{ "Key": "IfReputationAtLeast", "Value": "RotChoir:Liked" }]
}
```

### Phase 7: Faction Stock and Rewards

**Extend `TradeStockBuilder` to support faction-specific stock:**

| Faction | Stock Theme | Items |
|---------|-------------|-------|
| SaccharineConcord | Trade goods, information | HealingTonic, various weapons, food |
| PaleCuration | Old-world artifacts | OldWorldPipe, SeveranceEdge, tech items |
| RotChoir | Organic weapons/food | ChoirSpine, Sporeblade, SporeLoaf |
| GlassblownRemnant | Raw materials, glass items | GlassblownStiletto, basic supplies |

**Reputation rewards at thresholds:**
- **Liked**: NPC offers a one-time gift item via conversation
- **Loved**: NPC teaches a skill or grants a unique item, unlock special dialogue branches

---

## Implementation Priority

| Phase | Effort | Dependencies | Value |
|-------|--------|--------------|-------|
| Phase 1: Data-driven factions | Low | None | Foundation for everything |
| Phase 2: Player reputation | Medium | Phase 1, Save system | Core mechanic |
| Phase 3: Kill reputation | Low | Phase 2, Death events | Makes combat meaningful |
| Phase 4: Trade price modifiers | Low | Phase 2 | Rewards faction investment |
| Phase 5: Faction UI | Medium | Phase 2 | Player visibility |
| Phase 6: Conversation gates | Low | Phase 2 | Enriches dialogue |
| Phase 7: Faction stock/rewards | Medium | Phase 2, Phase 4 | Content richness |

**Recommended order**: 1 -> 2 -> 3 -> 6 -> 4 -> 5 -> 7

---

## Key Design Decisions

### Simplifications from Qud

| Qud Feature | Our Approach | Rationale |
|-------------|--------------|-----------|
| Float reputation values (-600 to +600) | Int values (-200 to +200) | Simpler math, fewer thresholds needed |
| 5 attitude levels | 5 levels (same) | Good granularity without complexity |
| Water ritual with 30+ properties | Skip for now | Complex; can add later as unique mechanic |
| Allegiance stack (multi-faction NPCs) | Single faction per NPC | Sufficient for current faction count |
| Part-based reputation modifiers | Skip | No mutation system yet |
| Worship/blasphemy tracking | Skip | No religion system |
| Faction interests (secret trading) | Skip | No journal/secret system |
| Faction ranks | Defer to Phase 7+ | Nice-to-have, not essential |

### What We Keep from Qud

- Reputation thresholds with clear attitude labels
- Kill reputation affecting multiple factions
- Conversation gating by reputation level
- Faction-to-faction feelings (inter-faction politics)
- Trade price modifiers based on standing
- Visible reputation UI with color coding
- Data-driven faction definitions (JSON instead of XML)
