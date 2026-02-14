# Caves of Qud NPC System -- Deep Source Code Analysis

This document is a comprehensive analysis of how Caves of Qud implements NPCs, based on the decompiled source code at `/Users/steven/qud-decompiled-project/`.

---

## Table of Contents

1. [Brain Part -- Core NPC Intelligence](#1-brain-part----core-npc-intelligence)
2. [Goal Stack System (AI Goals)](#2-goal-stack-system-ai-goals)
3. [AI Behavior Parts](#3-ai-behavior-parts)
4. [Faction and Feeling System](#4-faction-and-feeling-system)
5. [Conversation System](#5-conversation-system)
6. [Water Ritual](#6-water-ritual)
7. [Quest System](#7-quest-system)
8. [Reputation System](#8-reputation-system)
9. [Secrets and Gossip](#9-secrets-and-gossip)
10. [XP and Leveling](#10-xp-and-leveling)
11. [NPC Generation: HeroMaker](#11-npc-generation-heromaker)
12. [Village Generation](#12-village-generation)
13. [Merchants and Trade](#13-merchants-and-trade)
14. [Named vs Generated NPCs](#14-named-vs-generated-npcs)

---

## 1. Brain Part -- Core NPC Intelligence

**File**: `XRL.World.Parts/Brain.cs` (~4400 lines)

The `Brain` is the single most important Part for NPC behavior. Every creature that thinks has one. It manages perception, hostility, targeting, the goal stack, and party membership.

### Key Fields

```csharp
public class Brain : IPart
{
    // Behavior flags (bitfield, default = 17792 = Mobile|Wanders|WandersRandomly|Passive)
    public int Flags;

    // Flag constants
    BRAIN_FLAG_HOSTILE = 1;
    BRAIN_FLAG_CALM = 2;
    BRAIN_FLAG_WANDERS = 4;
    BRAIN_FLAG_WANDERS_RANDOMLY = 8;
    BRAIN_FLAG_AQUATIC = 16;
    BRAIN_FLAG_LIVES_ON_WALLS = 32;
    BRAIN_FLAG_WALL_WALKER = 64;
    BRAIN_FLAG_MOBILE = 128;
    BRAIN_FLAG_HIBERNATING = 256;
    BRAIN_FLAG_STAYING = 4096;
    BRAIN_FLAG_PASSIVE = 8192;

    // Combat radii
    public int MinKillRadius = 5;
    public int MaxKillRadius = 15;
    public int HostileWalkRadius = 84;
    public int MaxWanderRadius = 12;
    public int MaxMissileRange = 80;

    // AI state
    public AllegianceSet Allegiance;              // faction membership + hostility/calm
    public StringMap<int> FactionFeelings;        // per-faction feelings (-100 to +100)
    public OpinionMap Opinions;                   // per-object opinions
    public PartyCollection PartyMembers;          // party members this entity leads
    public CleanStack<GoalHandler> Goals;         // THE GOAL STACK
    public GlobalLocation StartingCell;           // home position (for returning)
    private GameObjectReference LeaderReference;  // party leader
    public Dictionary<GameObject, int> FriendlyFire;
}
```

### Main AI Turn Loop

The Brain handles `CommandTakeActionEvent`. The core loop:

1. Fire `AITakingAction` event (lets AI behavior parts intercept)
2. Handle confused state (random movement)
3. Do re-equip if flagged
4. Pop any finished goals from the stack
5. If no target and has a party leader, try to help the leader's target
6. If no target, call `FindProspectiveTarget()` to scan for hostiles
7. If goals stack is empty, push a `Bored` goal
8. Fire `TakingAction` event
9. Pop any finished goals
10. **`Goals.Peek().TakeAction()`** -- execute the top goal

### Target Acquisition

```csharp
public GameObject FindProspectiveTarget(Cell FromCell = null, bool WantPlayer = false)
{
    // Uses FastCombatSquareVisibility with random radius between Min/MaxKillRadius
    // Filters by IsSuitableTarget (checks hostility + perception)
    // Sorts by TargetSort (prefers players, matching phase/flight)
    // 80% picks best, 10% random, 10% worst
}
```

---

## 2. Goal Stack System (AI Goals)

**File**: `XRL.World.AI/GoalHandler.cs`

All AI behavior runs through a **goal stack**. `GoalHandler` is the abstract base class:

```csharp
public class GoalHandler
{
    public int Age;
    public Brain ParentBrain;
    public GoalHandler ParentHandler;

    public virtual bool CanFight() => true;
    public virtual bool IsBusy() => true;
    public virtual void Create() {}
    public virtual void TakeAction() {}
    public virtual bool Finished() => true;

    public void PushChildGoal(GoalHandler Child);
    public void Pop();
    public void FailToParent();

    // Pathfinding helper used by many goals
    public bool MoveTowards(Cell targetCell, bool Global = false);
}
```

### All Goal Handlers

Located in `XRL.World.AI.GoalHandlers/`:

| Goal | Purpose |
|------|---------|
| **Bored** | Default idle state -- scans for targets, tries passive abilities, wanders |
| **Kill** | Chase and attack a target using melee/missile/abilities |
| **Flee** | Run away from a specific target |
| **Retreat** | Find a safe retreat point and path to it |
| **Guard** | Stand guard, find targets, follow party leader at distance |
| **Wander** | Pick a random reachable cell within MaxWanderRadius and path to it |
| **WanderRandomly** | Step in random directions for N turns |
| **WanderDuration** | Wander until a specific game turn |
| **MoveTo** / **MoveToGlobal** / **MoveToZone** | Path to specific coordinates |
| **Step** | Take a single step in a direction |
| **Wait** | Do nothing for N turns |
| **Dormant** | Inactive state |
| **ExtinguishSelf** | Try to put out fire on self |
| **Pet** | Pet behavior |
| **Reequip** | Re-evaluate equipment |
| **GoOnAPilgrimage** | Travel to a distant map location |
| **GoOnAShoppingSpree** | Visit merchants in town zones |
| **GoFetch** / **GoFetchGet** | Retrieve items |
| **DropOffStolenGoods** | Thief behavior |
| **TombPatrolGoal** | Patrol a tomb perimeter |
| **PlaceTurretGoal** / **LayMineGoal** | Tactical placement |
| **Confused** | Random movement |
| **Land** | Landing from flight |

### The Bored Goal -- Idle AI Decision Tree

**File**: `XRL.World.AI.GoalHandlers/Bored.cs`

`Bored` is the default "nothing to do" goal. Its `TakeAction()` decision tree:

1. If on fire: push `ExtinguishSelf`
2. Check `WhenBoredReturnToOnce` property
3. Fire `AIBoredEvent` -- lets behavior parts intercept (see AIShopper, AIPilgrim)
4. **If has a party leader**: call `TakeActionWithPartyLeader()`:
   - Try to join leader in another zone
   - Help leader's target
   - Find own hostile targets
   - Try movement/passive abilities/items
   - Path toward leader, maintaining follow distance
5. **If solo**: find hostile targets, try passive abilities
6. If no target and has `StartingCell` (and not Wanders): return to starting cell
7. If not in player's zone: push `Wait(1..20)`
8. If `Wanders` flag: push `Wander()` (10% chance)
9. If `WandersRandomly` flag: push `WanderRandomly(5)` (20% chance)
10. Check for idle behaviors via `IdleQueryEvent`
11. Fall through: `Wait(1..10)`

Key tags that affect wandering:
- `Restless` -- always wanders when possible (unless `Social` tag)
- `Social` -- suppresses restless wandering
- `AllowIdleBehavior` / `PreventIdleBehavior` -- idle interaction system

### Wander (Controlled)

```csharp
// Pick a random cell in the zone:
// - Not occluding, not solid
// - Within MaxWanderRadius of current cell
// - Reachable via pathfinding
// - No "WanderStopper" tag objects
// Then push MoveTo as child goal
```

### WanderRandomly (Drunk Walk)

```csharp
// Each turn:
// - Check adjacent cells, weight by navigation weight
// - Avoid WanderStopper objects, solid cells
// - Pick weighted random direction
// - Push Step child goal
```

---

## 3. AI Behavior Parts

These Parts modify Brain decisions by intercepting AI events. Located in `XRL.World.Parts/`:

### AISelfPreservation
```csharp
public int Threshold = 35;  // HP percentage to flee at
// On "AITakingAction": if HP below threshold, clear goals and push Retreat(30-50 turns)
```

### AIShopper
```csharp
// On AIBoredEvent: 25% chance to push GoOnAShoppingSpree
// Only if no party leader, can do independent behavior
```

### AIPilgrim
```csharp
public string StiltZoneID = "JoppaWorld.5.2.1.1.10";
public string TargetObject = "StiltWell";
public int Chance = 100;
// On AIBoredEvent: push GoOnAPilgrimage to travel to the Stilt
```

### Other AI Behavior Parts
- **AISitting** -- Apply Sitting effect on first cell entry
- **AIFlocks** -- Flocking behavior
- **AIJuker** -- Juking/dodging
- **AIShootAndScoot** / **AIThrowAndScoot** -- Kiting
- **AITryKeepDistance** / **AITryKeepSteadyDistance** -- Ranged positioning
- **AISeekHealingPool** -- Find healing pools when hurt
- **AIWallWalker** / **AIWallPhaser** -- Wall movement
- **AIShoreLounging** -- Sit by water
- **AIWorldMapTravel** -- World map travel behavior
- **AIVehiclePilot** -- Vehicle control

---

## 4. Faction and Feeling System

### Feeling Computation

```csharp
public enum FeelingLevel { Hostile, Neutral, Allied }

public const int FEELING_HOSTILE_THRESHOLD = -10;
public const int FEELING_ALLIED_THRESHOLD = 50;

public FeelingLevel GetFeelingLevel(GameObject Object)
{
    int feeling = GetFeeling(Object);
    if (feeling < -10) return FeelingLevel.Hostile;
    if (feeling >= 50) return FeelingLevel.Allied;
    return FeelingLevel.Neutral;
}
```

Feelings are composed from:
- Faction memberships (weighted `AllegianceSet`)
- Faction feelings (`FactionFeelings` dictionary)
- Per-object opinions (`OpinionMap`)

### AllegianceSet

```csharp
public sealed class AllegianceSet : StringMap<int>
{
    public int SourceID;
    public AllegianceSet Previous;  // linked list -- tracks allegiance history
    public IAllyReason Reason;      // why this allegiance exists
    public int Flags;               // Hostile + Calm bits
}
```

### Opinion System

**OpinionMap**: `Dictionary<int, OpinionList>` mapping object BaseID to opinions.

20+ opinion types explain WHY NPCs feel a certain way:

| Opinion Class | Trigger |
|---------------|---------|
| OpinionAttack | Being attacked directly |
| OpinionAttackAlly | Seeing an ally attacked |
| OpinionBeguile | Being beguiled |
| OpinionProselytize | Being proselytized |
| OpinionCoquetry | Flirtation |
| OpinionGoad | Being goaded |
| OpinionMollify | Being calmed |
| OpinionThief | Being stolen from |
| OpinionTrespass | Trespassing detected |
| OpinionFriendlyFire | Friendly fire incident |
| OpinionKilledAlly | Seeing ally killed |
| OpinionDominate | Being dominated |

### Ally Reason System

20+ ally reason types explaining WHY entities are allied:

| Class | Reason |
|-------|--------|
| AllyDefault | Base faction membership |
| AllyBeguile | Mental domination |
| AllyProselytize | Converted via Proselytize |
| AllyBond | Bond of fate |
| AllyRebuke | Rebuked into service |
| AllyPet | Domesticated pet |
| AllyPack | Pack animal |
| AllyClan | Clan membership |
| AllyRetinue | Part of retinue |
| AllyClone | Clone relationship |
| AllySummon | Summoned creature |

---

## 5. Conversation System

### Architecture Overview

| Namespace | Purpose |
|-----------|---------|
| `XRL.World.Conversations` | Core model (nodes, choices, events, delegates) |
| `XRL.World.Conversations.Parts` | Behavioral components on conversation elements |
| `XRL.World.Parts/ConversationScript.cs` | IPart on GameObjects to trigger conversations |
| `XRL.UI/ConversationUI.cs` | UI rendering and input loop |
| `Qud.API/ConversationsAPI.cs` | Programmatic API for building conversations |

### Dialogue Tree Structure

**Conversation -> Node(s) -> Choice(s)**, with optional **Text** elements and **IConversationPart** components at any level.

#### IConversationElement (abstract base)

```csharp
public string ID;
public string Text;
public int Priority;
public IConversationElement Parent;
public List<IConversationPart> Parts;          // Behavioral components
public List<IConversationElement> Elements;    // Child nodes/choices
public List<ConversationText> Texts;           // Text alternatives
public Dictionary<string, string> Predicates;  // Visibility conditions
public Dictionary<string, string> Actions;     // Side effects on entry
```

Has a full event system mirroring the Entity/Part pattern:
- `WantEvent(int ID)` / `HandleEvent(ConversationEvent E)` -- propagates through `Parent`
- `CheckPredicates()` -- checks all predicate delegates for visibility
- `Entered()` -- fires all action delegates, then sends `EnteredElementEvent`
- `IsVisible()` -- checks predicates AND fires `IsElementVisibleEvent`

#### Conversation (root)
```csharp
public List<Node> Starts;                          // Entry-point nodes
public Dictionary<string, object> State;           // Per-conversation state bag
public static GameObject Speaker, Listener;
```

#### Node
```csharp
public bool AllowEscape = true;    // Can the player ESC out?
// Extremely minimal -- behavior comes from IConversationElement + attached Parts
```

#### Choice
```csharp
public bool Transient;              // Don't track as "visited"
public static FixedHashSet Hashes;  // Global set of visited choice hashes
private string _Target;             // Target node ID ("End", "Start", or node ID)

public bool Visited => Hashes.Contains(Hash);  // Green vs dark green color
```

#### ConversationText
Text elements serve as **alternative text variants**. When `GetText()` is called, it collects all visible Text children, groups by highest priority, then picks a random one. This enables randomized NPC dialogue.

### ConversationScript Part (on GameObjects)

```csharp
public class ConversationScript : IPart
{
    public string ConversationID;
    public string Quest;
    public string PreQuestConversationID;
    public string InQuestConversationID;
    public string PostQuestConversationID;
}
```

Quest-aware conversation selection:
```csharp
public string GetActiveConversationID()
{
    if (!string.IsNullOrEmpty(Quest))
    {
        if (Game.FinishedQuest(Quest))  return PostQuestConversationID;
        if (!Game.HasQuest(Quest))       return PreQuestConversationID;
        if (Game.HasQuest(Quest))         return InQuestConversationID;
    }
    return ConversationID;
}
```

### The Delegate/Predicate System

This is one of the most elegant parts. XML attributes automatically wire up as predicates, actions, or part generators:

```csharp
// Three dictionaries populated by reflection at startup:
Dictionary<string, PredicateReceiver> Predicates;
Dictionary<string, ActionReceiver> Actions;
Dictionary<string, PartGeneratorReceiver> PartGenerators;
```

Any method marked with `[ConversationDelegate]` is registered:
- Returns `bool` -> Predicate (visibility)
- Returns `void` -> Action (executed on entry)
- Returns `IConversationPart` -> Part Generator

When XML attributes are loaded, they're resolved in order:
1. Is it a registered Predicate? -> Add to Predicates dict
2. Is it a registered Action? -> Add to Actions dict
3. Is it a registered PartGenerator? -> Call it, attach result as Part
4. Is it a field/property? -> Set via reflection
5. Otherwise -> Store in Attributes dict

So XML like `<Choice IfHaveQuest="What's Eating the Watervine?" Target="End">` automatically wires up visibility conditions.

#### Built-in Predicates (partial list)

| Key | What it checks |
|-----|----------------|
| `IfHaveQuest` / `IfNotHaveQuest` | Player has quest |
| `IfHaveActiveQuest` | Quest is unfinished |
| `IfFinishedQuest` / `IfFinishedQuestStep` | Quest completion |
| `IfHaveState` / `IfTestState` | Game state checks |
| `IfHavePart` / `IfSpeakerHavePart` | Part exists on player/speaker |
| `IfHaveItem` / `IfHaveBlueprint` | Inventory checks |
| `IfGenotype` / `IfTrueKin` / `IfMutant` | Character type |
| `IfReputationAtLeast` | Faction rep level |
| `IfIn100` | Random percentage check |
| `IfZoneName` / `IfZoneLevel` | Location checks |

#### Built-in Actions (partial list)

| Key | What it does |
|-----|--------------|
| `AwardXP` | Give XP to player |
| `FinishQuest` | Complete a quest |
| `SetStringState` / `SetIntState` | Modify game state |
| `FireEvent` | Fire a game event on target |
| `RevealMapNote` | Journal reveals |
| `SetLeader` | Set party leader |

### Conversation Event Chain

When the player talks to an NPC:

1. Player bumps NPC -> `CanSmartUseEvent` -> `ConversationScript` claims it
2. `CommandSmartUseEvent` -> `ConversationScript.AttemptConversation()`
3. `AttemptConversationEvent.Check()` -> validation chain
4. Physical/Mental conversation feasibility checks (not stunned/confused/hostile)
5. `ConversationUI.HaveConversation(blueprint, speaker, listener)`
6. Build `Conversation` from XML blueprint, `Awake()`, `Enter()`
7. Get `StartNode`, fire `BeginConversationEvent`
8. **UI Loop**: `Prepare()` -> collect visible choices -> `Render()` -> get input -> `Select()`
9. On `Select()`: fire actions, quest handlers, navigate to target node
10. On exit: `AfterConversationEvent`, `Dispose()`

---

## 6. Water Ritual

The Water Ritual is Qud's signature NPC interaction system, implemented as conversation parts.

### WaterRitualRecord (IPart on the NPC)

```csharp
public class WaterRitualRecord : IPart
{
    public int mySeed;
    public int secretsRemaining = Stat.Random(2, 3);
    public int totalFactionAvailable = 100;  // Reputation pool
    public int numBlueprints, numGifts, numItems, numFungusLeft;
    public List<string> attributes;
    public List<int> tinkerdata;
}
```

### Step-by-Step Flow

**1. Initiation (WaterRitualBegin)**: Costs 1 dram of ritual liquid (usually water) or play a Sifrah minigame for variable performance (0-200%).

**2. Performing the Ritual (WaterRitual.PerformRitual())**:
- Sets `WaterRitualed` flag on speaker (one-time per NPC)
- Adds journal accomplishment
- Modifies reputation: primary faction gets `GivesRep.repValue` (default 100) scaled by performance
- Allied factions get proportional rep changes

**3. The Conversation Menu** (IWaterRitualPart subclasses):

| Option | Rep Cost | What You Get |
|--------|----------|-------------|
| **Buy Secret** | -50 rep | NPC shares an unrevealed journal entry |
| **Sell Secret** | +50 rep | You share a revealed entry |
| **Learn Skill** | Skill cost | Learn a skill from the NPC's faction tree |
| **Gain Mutation** | Mutation cost * 100 | Gain a specific mutation |
| **Buy Item** | Varies | Get a faction-specific item |
| **Tinkering Recipe** | 50 * tier/3 | Learn a tinkering blueprint |
| **Cooking Recipe** | 50 rep | Learn a cooking recipe |
| **Join Party** | 200 + (NPC_level - player_level) * 12.5 | NPC joins your party |
| **Hermit Oath** | +N rep (free) | Gain rep, but penalty on re-visit |

The `totalFactionAvailable` pool caps how much rep can be earned from selling secrets (starts at 100, decremented per sale).

---

## 7. Quest System

### Core Classes

| Class | File | Purpose |
|-------|------|---------|
| `Quest` | `XRL.World/Quest.cs` | Data object with steps, rewards |
| `QuestStep` | `XRL.World/QuestStep.cs` | Individual step with flags |
| `QuestLoader` | `XRL.World/QuestLoader.cs` | Loads from XML |
| `IQuestSystem` | `XRL/IQuestSystem.cs` | Complex quest behavior |
| `QuestManager` | `XRL.World/QuestManager.cs` | Simple quest hooks (IPart) |
| `QuestStarter` | `XRL.World.Parts/QuestStarter.cs` | Part that triggers quests |
| `QuestStepFinisher` | `XRL.World.Parts/QuestStepFinisher.cs` | Part that completes steps |

### Quest Class

```csharp
public class Quest
{
    public string ID, Name;
    public int Level;
    public bool Finished;
    public Dictionary<string, QuestStep> StepsByID;
    public Type SystemType;              // Links to IQuestSystem subclass
    public QuestManager _Manager;        // Optional QuestManager (is IPart!)
    public DynamicQuestReward _dynamicReward;

    // Flavor/rewards
    public string Accomplishment, Achievement;
    public string Factions, Reputation;  // Rep rewards on completion
    public string QuestGiverName, QuestGiverLocationName;
}
```

### QuestStep Flags

```csharp
FLAG_FINISHED = 2;   // Completed (green checkmark)
FLAG_FAILED   = 4;   // Failed (red cross)
FLAG_HIDDEN   = 16;  // Hidden until revealed
FLAG_AWARDED  = 32;  // XP already awarded
FLAG_OPTIONAL = 64;  // Won't block quest completion
```

### Quest Lifecycle

**Starting** (`XRLGame.StartQuest()`):
1. Copy blueprint from `QuestLoader.QuestsByID`
2. Add to `Game.Quests` dictionary
3. Initialize `IQuestSystem` if declared
4. Show popup, fire `QuestStartedEvent`

**Step completion** (`XRLGame.FinishQuestStep()`):
1. Mark `Finished = true`, `Failed = false`
2. Award XP via `The.Player.AwardXP(step.XP)`
3. Fire `QuestStepFinishedEvent`
4. Auto-finish quest if all non-optional steps done

**Quest completion** (`XRLGame.FinishQuest()`):
1. Mark `Finished = true`, move to `FinishedQuests`
2. Award dynamic rewards
3. Record accomplishment in journal

### Quest Triggers on GameObjects

**QuestStarter** part triggers based on events:
- `Trigger = "Taken"` -- player picks up item
- `Trigger = "Created"` -- zone loads with player present
- `Trigger = "Seen"` -- when rendered
- Supports `IfFinishedQuestStep` prerequisite gating

**QuestStepFinisher** part completes steps using the same trigger system.

### QuestHandler (Conversation Part)

Attaches to conversation choices to start/finish quests:

```csharp
public class QuestHandler : IConversationPart
{
    public string QuestID, StepID;
    public int Type;  // 0=None, 1=Start, 2=Step, 3=Finish, 4=Complete

    public override bool HandleEvent(EnteredElementEvent E)
    {
        if (Type == 1) The.Game.StartQuest(QuestID);
        if (Type == 2) The.Game.FinishQuestStep(QuestID, StepID);
        if (Type == 3) The.Game.FinishQuest(QuestID);
    }
}
```

Renders tags like `"{{W|[Accept Quest]}}"` and `"{{W|[Complete Quest]}}"`.

---

## 8. Reputation System

### Reputation Thresholds

```csharp
REPUTATION_HATED    = -600   // Attitude -2 (even docile creatures attack)
REPUTATION_DISLIKED = -250   // Attitude -1
REPUTATION_LIKED    =  250   // Attitude +1 (aggressive won't attack)
REPUTATION_LOVED    =  600   // Attitude +2 (considered one of their own)
```

### How Reputation is Stored

`Reputation` holds `Dictionary<string, float> ReputationValues` keyed by faction name.

```csharp
public int Get(Faction Faction)
{
    int num = ReputationValues[Faction.Name] ?? 0;
    // Add part-based bonuses (having certain Parts on player)
    foreach (var (partName, bonus) in Faction.PartReputation)
        if (player.HasPart(partName)) num += bonus;
    return num;
}
```

### Modify Reputation

`Reputation.Modify(Faction, Amount, Because, ...)`:
1. Fire `ReputationChangeEvent` (can alter amount)
2. Update `ReputationValues[name] += adjustedAmount`
3. Sync faction feeling: `Faction.FactionFeeling["Player"] = GetFeeling(name)`
4. Detect threshold crossings, show colored messages
5. Fire `AfterReputationChangeEvent`

### GivesRep Part (on Legendary NPCs)

```csharp
public class GivesRep : IPart
{
    public bool wasParleyed;   // true if water ritual completed
    public int repValue = 100;
    public List<FriendorFoe> relatedFactions;  // 1d3 random relationships

    // On kill by player:
    //   If wasParleyed: -100 rep with ALL visible factions (covenant violation!)
    //   Loved factions: -repValue*2
    //   Friend factions: -repValue*2
    //   Dislike factions: +repValue
    //   Hate factions: +repValue*2
}
```

`FriendorFoe` entries have colorful procedural reasons:
- Hate: "insulting their kumquats", "releasing snakes into one of their camps"
- Like: "cooking them a splendid meal", "providing shelter during a glass storm"

---

## 9. Secrets and Gossip

### What Are Secrets?

Journal entries (`IBaseJournalEntry` subclasses):
1. **JournalSultanNote** -- Events from sultan history
2. **JournalMapNote** -- Location discoveries
3. **JournalObservation** -- Gossip and observations
4. **JournalRecipeNote** -- Cooking recipes

Each has: `Attributes` (tags), `Revealed` state, `Tradable` state.

### FactionInterest

Each `Faction` has `List<FactionInterest>` defining what secrets they care about:
- `Tags` -- comma-separated tag list (e.g., "ruins", "sultan", "gossip")
- `WillBuy` / `WillSell` -- direction of interest
- `Weight` -- priority bonus

### Gossip Generation

`Gossip` (static utility class) generates procedural gossip text:

```csharp
public static string GenerateGossip_TwoFactions(string actor, string actee)
{
    string text = HistoricStringExpander.ExpandString("<spice.gossip.twoFaction.!random>");
    // Replace @item references, faction names, random member names
    return text.Replace("*f1*", actor).Replace("*f2*", actee);
}
```

### SecretObject Part

Lives on world objects. When rendered (player enters zone), reveals a JournalMapNote.

---

## 10. XP and Leveling

### Experience Part

Handles `AwardXPEvent` with tier scaling:

```csharp
// Tier scaling: if player level/5 >> source tier, XP is reduced
int diff = levelTier - sourceTier;
if (diff > 2) xp = 0;        // No XP from much-lower-tier sources
else if (diff > 1) xp /= 10; // 10% XP
else if (diff > 0) xp /= 2;  // 50% XP
```

XP flows through party hierarchy: companions share XP up to leader, leader shares XP down to nearby companions (within 10 cells).

### Leveler Part (NPCs Level Up Too!)

`Leveler` is NOT player-exclusive. Any entity with both `Experience` and `Leveler` will level up.

**XP Curve**: `GetXPForLevel(L) = Floor(L^3 * 15) + 100`

| Level | XP Required |
|-------|------------|
| 2 | 220 |
| 5 | 1,975 |
| 10 | 15,100 |
| 20 | 120,100 |
| 30 | 405,100 |

**On Level Up**:
1. Increment Level stat
2. Roll HP gain: `max(BaseDice + Toughness_modifier, 1)`
3. Roll SP gain: `BaseDice + (Intelligence - 10) * 4`
4. Roll MP gain (mutants only)
5. Attribute points: 1 per 3 levels (but not every 6th)
6. Attribute bonus (+1 to ALL): every 6th level
7. Rapid Advancement (mutants): levels 5, 15, 25... gain 3 rapid mutation levels

NPCs level up identically to players (same code path, random choices instead of interactive prompts).

---

## 11. NPC Generation: HeroMaker

**File**: `XRL.World/HeroMaker.cs`

`HeroMaker.MakeHero()` transforms any creature into a legendary/hero NPC:

```csharp
public static GameObject MakeHero(GameObject BaseCreature,
    string[] AdditionalBaseTemplates = null,
    string[] AdditionalSpecializationTemplates = null,
    int TierOverride = -1, string SpecialType = "Hero")
{
    // 1. Set Hero=1, Role="Hero"
    // 2. Apply hero color scheme
    // 3. Boost all 6 stats via blueprint tags (HeroStrBoost, etc.)
    // 4. HP *= HeroHPBoost (default 2x)
    // 5. Level *= HeroLevelMultiplier (default 1.5x)
    // 6. Add skills from HeroSkills tag
    // 7. Set AISelfPreservation threshold
    // 8. Add ConversationScript if HeroConversation tag exists
    // 9. Add random mutations (0-2 mental + 0-2 physical)
    // 10. Generate full name via NameMaker:
    //     - MakeHonorific (e.g., "Warden")
    //     - MakeEpithet (e.g., "the Fearsome")
    //     - MakeTitle (e.g., "Scourge of the Jungles")
    //     - GiveProperName (culture-appropriate name)
    // 11. Equip from HeroInventory population table
    // 12. Add HasGuards/HasSlaves/HasThralls from tags
    // 13. Add GivesRep (enables water ritual)
    // 14. Maybe generate faction heirloom
    // 15. Bump render layer
    // 16. Fire "MadeHero" event
}
```

### NameMaker

Delegates to `NameStyles.Generate()` with rich parameters:

```csharp
NameMaker.MakeName(For, Genotype, Subtype, Species, Culture, Faction, Region,
    Gender, Mutations, Tag, Special, Type, NamingContext)
NameMaker.MakeHonorific(...)  // "Warden", "Elder"
NameMaker.MakeEpithet(...)    // "the Fearsome", "of Many Legs"
NameMaker.MakeTitle(...)      // "Scourge of ..."
```

### Relic Generation

`RelicGenerator.GenerateRelic()` creates historical artifacts:
1. Create base item from population table
2. Apply element bestowals (glass, jewels, ice, circuitry, etc.)
3. Add faction like/hate modifiers
4. Generate name from history + NameMaker

---

## 12. Village Generation

### Three-Layer Architecture

**Layer 1: History Generation** (`XRL.Annals/QudHistoryFactory.cs`)

Villages are first created as `HistoricEntity` objects. ~28 villages per world, distributed by biome.

```csharp
GenerateNewVillage(history, year, "DesertCanyon", null, 400, 900, 2, VillageZero: true);
```

Village events shape character:
| Event | Effect |
|-------|--------|
| BecomesKnownFor | Signature items/skills |
| PopulationInflux | Immigrants with specific roles |
| Worships | Sacred creature |
| Despises | Profane creature |
| SharedMutation | Villagers share a mutation (80% chance each) |
| NewGovernment | Government type changes |
| ImportedFoodorDrink | Signature liquid |
| Abandoned | 1-in-20 chance, village is ruins |

**Layer 2: World Builder Placement** (`XRL.World.WorldBuilders/JoppaWorldBuilder.cs`)

`AddVillages()` iterates village entities and places them on the world map:
- Creates a unique faction per village ("villagers of Kyakukya")
- Registers Village/VillageOutskirts zone builders

**Layer 3: Zone Building** (`XRL.World.ZoneBuilders/Village.cs`)

Uses **Wave Function Collapse** for building layouts and **Influence Maps** to partition zones into regions.

Building styles: "burrow", "aerie", "pond", "tent", "roundhut", "squarehut", "wfc,\<template\>"

### NPC Placement in Buildings

```
Building[0] -> Mayor (always)
Building[1] -> Merchant (30% chance, 100% for village zero)
Building[2] -> Tinker (25% chance, 100% for village zero)
Building[3] -> Apothecary (25% chance, 100% for village zero)
Town Square -> Warden
Scattered   -> 4-10 generic villagers
```

### Village Roles and Their Templates

| Role | Template | Key Features |
|------|----------|-------------|
| Mayor | SpecialVillagerHeroTemplate_Mayor | Leadership stats, ConversationScript |
| Warden | SpecialVillagerHeroTemplate_Warden | Combat-focused boosts |
| Merchant | SpecialVillagerHeroTemplate_Merchant | Commerce skills, yellow name |
| Tinker | SpecialVillagerHeroTemplate_Tinker | Tinkering skills, INT boost |
| Apothecary | SpecialVillagerHeroTemplate_Apothecary | Cooking/medicine skills |

### Population Influx (Immigrant NPCs)

The `PopulationInflux` history event adds named immigrants:

```csharp
// 40% chance: named immigrant with a specific role
// Role weights: Mayor=10, Warden=10, Merchant=10, Tinker=10,
//               Apothecary=10, Villager=70

gameObject = HeroMaker.MakeHero(
    GameObject.Create(blueprintName),
    "SpecialVillagerHeroTemplate_" + role, -1, role);
```

Immigrant data is stored in village history, then instantiated at zone build time.

### Shared Village Properties

From `VillageBase.preprocessVillager()`:
- **Shared mutations**: 80% of non-foreign villagers get the village mutation
- **Shared diseases**: Glotrot, Ironshank
- **Shared transformations**: Roboticized

### Government Types

| Type | Effect |
|------|--------|
| Default | Standard mayor + warden |
| Anarchism | No warden generated |
| Colonialism | Colonist-type NPCs replace some roles |
| Representative democracy | Multiple mayors (2-4 extra) |

---

## 13. Merchants and Trade

### GenericInventoryRestocker Part

The core restocking engine:

```csharp
public long RestockFrequency = 6000L;  // ~5 game days
public List<string> Tables;            // Population tables for stock
public List<string> HeroTables;        // Extra tables if NPC is Hero
```

**Restocking process**:
1. Remove all items marked `_stock` that don't have `norestock`
2. For each table, call `EquipFromPopulationTable(table, tier)`
3. If Hero, also roll from HeroTables
4. Mark new items with `_stock = 1`
5. Fire `StockedEvent`

Restocking is triggered on `TurnTick` (when timer expires and player in same zone) or on first trade (`StartTradeEvent`).

### Village Merchant Generation

```csharp
// 20% chance: non-dromad merchant from village species
//   -> HeroMaker.MakeHero with merchant template
// 80% chance: DromadTrader of appropriate tier
//   -> Remove DromadCaravan + ConversationScript parts

// Stock from tiered wares (tier 3 merchant gets Tier3+Tier2+Tier1 Wares)
for (int i = 0; i <= 2 && villageTier > i; i++)
    restocker.AddTable("Tier" + (villageTier - i) + "Wares");
```

### Dromad Caravan Merchants

Traveling merchants that spawn companions on first cell entry:
- 1-3 Great Saltbacks (pack animals)
- 2-4 Caravan Guards
- Reveals map note "A dromad caravan"

### Trade Conversation Part

`Trade` is an `IConversationPart` that:
- Shows `[begin trade]` tag on its choice
- Opens `TradeUI.ShowTradeScreen()` when entered
- Visibility: must be adjacent, phase-matched, no "NoTrade" tag

---

## 14. Named vs Generated NPCs

### The Spectrum of NPC Uniqueness

1. **Hardcoded unique** (Mehmet, Argyve, Warden Yrame):
   - XML blueprint with specific name, stats, parts, skills
   - Placed by specific zone builders at hardcoded positions
   - Referenced by quest systems
   - Hand-written conversation trees
   - `GivesRep` with dynamically generated faction relationships

2. **Generated unique with role** (village mayor, warden, merchant):
   - Runtime-created via `HeroMaker.MakeHero()` + role template
   - Procedural name via NameMaker (honorific + epithet + title + proper name)
   - Role-specific inventory/skills
   - Placed in specific buildings

3. **Generated unique villager** (generic hero):
   - HeroMaker with generic template
   - Procedural name, no specific role

4. **Generic villager**:
   - Base creature from faction
   - Colored `&y`, simple conversation, no name

5. **Immigrant NPC**:
   - Created during history generation (PopulationInflux event)
   - Stored in village entity, instantiated at zone build time
   - Has specific role and backstory explaining why they're there

### Key Properties That Distinguish Roles

| Property | Meaning |
|----------|---------|
| `Hero = 1` | Marked as a hero NPC |
| `NamedVillager = 1` | Has a proper name and role |
| `VillageMayor = 1` | Is the village mayor |
| `VillageMerchant = 1` | Is the village merchant |
| `VillageTinker = 1` | Is the village tinker |
| `VillageApothecary = 1` | Is the village apothecary |
| `VillageWarden = 1` | Is the village warden |
| `VillagePet = 1` | Is a village pet |
| `Merchant = 1` | Set by GenericInventoryRestocker |

---

## Key Architecture Patterns for Remake

1. **Goal Stack**: All NPC behavior is a stack of `GoalHandler` objects. Brain pops finished goals and calls `TakeAction()` on the top one. Goals push child goals. `Bored` is the default.

2. **AIBehaviorPart pattern**: Parts like `AISelfPreservation`, `AIShopper` hook into `AITakingAction` or `AIBoredEvent` to inject goals. They don't override Brain -- they intercept events.

3. **Faction-based hostility**: No hardcoded enemy lists. Hostility = weighted sum of faction memberships + faction feelings + per-object opinions. Threshold: `< -10 = hostile`, `>= 50 = allied`.

4. **WanderStopper tag**: Prevents NPCs from wandering into specific cells.

5. **Village vs wilderness NPCs**: Village NPCs have `Wanders=true` + `StartingCell` + low `MaxWanderRadius`. Wilderness creatures have `WandersRandomly=true` with larger radii.

6. **Party system**: Leader reference + PartyMembers collection. Bored goal has `TakeActionWithPartyLeader()` for following/helping.

7. **Conversation delegate system**: XML attributes auto-wire as predicates/actions via reflection + `[ConversationDelegate]` attribute. Extremely modular.

8. **Water Ritual as economy**: Reputation is the currency. Buy/sell secrets, learn skills/mutations, recruit NPCs -- all through the same rep pool system.

9. **Quest lifecycle**: Start -> FinishStep(s) -> FinishQuest. Triggered by Parts on world objects (`QuestStarter`, `QuestStepFinisher`) or conversation parts (`QuestHandler`).

10. **NPCs level up**: Same `Leveler` code as player, cubic XP curve, XP shared through party hierarchy.
