# Caves of Qud: Quests & Journal System — Deep Dive

A comprehensive analysis of the quest, journal, accomplishment, and lore systems based on decompiled source code.

---

## Table of Contents

1. [Quest System Architecture](#1-quest-system-architecture)
2. [Quest Data Model](#2-quest-data-model)
3. [Quest Lifecycle](#3-quest-lifecycle)
4. [Quest Events](#4-quest-events)
5. [Quest Givers & Conversation Integration](#5-quest-givers--conversation-integration)
6. [Quest Completion Triggers (Parts)](#6-quest-completion-triggers-parts)
7. [IQuestSystem — Scripted Quest Logic](#7-iquestsystem--scripted-quest-logic)
8. [QuestManager — Per-Quest IPart](#8-questmanager--per-quest-ipart)
9. [Dynamic Quest Generation](#9-dynamic-quest-generation)
10. [Journal System Architecture](#10-journal-system-architecture)
11. [Journal Entry Types](#11-journal-entry-types)
12. [Accomplishments](#12-accomplishments)
13. [Gossip & Lore Generation](#13-gossip--lore-generation)
14. [Sultan History & Procedural Lore](#14-sultan-history--procedural-lore)
15. [Secret Trading (Water Ritual)](#15-secret-trading-water-ritual)
16. [Quest Reward System](#16-quest-reward-system)
17. [Quest Difficulty & Consider](#17-quest-difficulty--consider)
18. [Key Source Files Reference](#18-key-source-files-reference)

---

## 1. Quest System Architecture

The quest system is a **two-tier architecture**:

1. **Static quests** — Defined in `Quests.xml`, loaded by `QuestLoader`. These are hand-authored story quests (e.g., "A Signal in the Noise", "More Than a Willing Spirit").
2. **Dynamic quests** — Procedurally generated at worldgen time by `DynamicQuestFactory` + `BaseDynamicQuestTemplate` subclasses. These are village fetch/find/interact quests.

Both types share the same `Quest` + `QuestStep` data model and use the same `XRLGame` API for lifecycle management.

### Storage

```
XRLGame.Quests           : Dictionary<string, Quest>  — active quests
XRLGame.FinishedQuests   : Dictionary<string, Quest>  — completed quests
QuestLoader.QuestsByID   : Dictionary<string, Quest>  — blueprint templates from XML
DynamicQuestsGameState.Quests : Dictionary<string, Quest> — dynamic quest blueprints
```

---

## 2. Quest Data Model

### Quest (`XRL.World.Quest`)

```csharp
[Serializable]
public class Quest
{
    public string ID;                    // Unique identifier (XML ID or GUID for dynamic)
    public string Name;                  // Display name
    public Type SystemType;              // Optional IQuestSystem class for scripted logic
    public string Accomplishment;        // Text logged in journal on completion
    public string Achievement;           // Steam/platform achievement ID
    public string BonusAtLevel;          // Level-based bonus reward indicator
    public int Level;                    // Recommended level (for difficulty display)
    public string Factions;              // Comma-delimited faction names for rep reward
    public string Reputation;            // Reputation amount string (parsed to int)
    public string QuestGiverName;        // Who gave the quest
    public string QuestGiverLocationName;
    public string QuestGiverLocationZoneID;
    public string Hagiograph;            // Mural text for golem quest
    public string HagiographCategory;    // MuralCategory string
    public string Gospel;                // Gospel text for history
    public bool Finished;
    public DynamicQuestReward _dynamicReward;
    public Dictionary<string, object> Properties;   // String-keyed bag
    public Dictionary<string, int> IntProperties;    // Numeric properties
    public Dictionary<string, QuestStep> StepsByID;
    public QuestManager _Manager;         // Optional IPart-derived manager
    public IQuestSystem _System;          // Optional game system (NonSerialized, lazy)
}
```

Key methods:
- `ReadyToTurnIn()` — returns true when all steps are `Finished`
- `Finish()` — marks finished, calls System.Finish(), awards dynamic rewards
- `FinishPost()` — awards post-rewards (reputation, etc.)
- `Copy()` — deep-copies blueprint into active quest (steps get `Finished=false`)
- `Consider(Quest)` — returns difficulty string based on player level delta
- `GetProperty<T>()` / `SetProperty()` — generic property bag access

### QuestStep (`XRL.World.QuestStep`)

```csharp
[Serializable]
public class QuestStep
{
    public string ID;        // Step identifier
    public string Name;      // Display name
    public string Text;      // Description body
    public string Value;     // Flexible string data (e.g., population table name)
    public int XP;           // XP reward for this step
    public int Ordinal;      // Display order
    public int Flags;        // Bitfield:

    // Flag constants:
    FLAG_BASE     = 1   // Blueprint-only step, not copied to active quest
    FLAG_FINISHED = 2   // Step complete (green checkmark)
    FLAG_FAILED   = 4   // Step failed (red cross)
    FLAG_COLLAPSE = 8   // Collapse text when finished (default ON)
    FLAG_HIDDEN   = 16  // Hidden until revealed
    FLAG_AWARDED  = 32  // XP already awarded (prevents double-awarding)
    FLAG_OPTIONAL = 64  // Won't block quest completion
}
```

Steps support:
- **Base** steps — exist only in blueprint, not inherited by active quest
- **Hidden** steps — can be revealed at runtime (e.g., Nephal step in Reclamation)
- **Optional** steps — quest can finish without them
- **Failed** steps — rendered with red cross, distinct from unfinished

---

## 3. Quest Lifecycle

### Starting a Quest

```
XRLGame.StartQuest(string QuestID) →
  1. Check if already active (early return if so)
  2. Find blueprint in QuestLoader.QuestsByID or DynamicQuestsGameState.Quests
  3. Deep-copy blueprint via Quest.Copy() (resets step Finished flags)
  4. Play "sfx_quest_gain" sound
  5. Add to XRLGame.Quests dictionary
  6. Initialize IQuestSystem via Quest.InitializeSystem()
  7. Set QuestGiverName/Location from params or current zone
  8. Show start popup
  9. Call System.Start()
  10. Call Manager.OnQuestAdded()
  11. Fire QuestStartedEvent (dispatched to player and game)
  12. Call Manager.AfterQuestAdded()
```

### Finishing a Quest Step

```
XRLGame.FinishQuestStep(Quest, stepList, XP, CanFinishQuest) →
  1. Parse stepList by '~' delimiter (supports multi-step completion)
  2. For each step:
     a. Skip if already finished
     b. Play "sfx_quest_step_complete"
     c. Set Finished=true, Failed=false
     d. If not already Awarded:
        - Show popup with step name + XP
        - AwardXP to player (with influencer for item naming)
        - Set Awarded=true
     e. Call Quest.FinishStep(step) → System.FinishStep()
     f. Call Manager.OnStepComplete(stepID)
     g. Fire QuestStepFinishedEvent
  3. If CanFinishQuest, call CheckQuestFinishState()
```

### Finishing a Quest

```
XRLGame.FinishQuest(Quest) →
  1. Skip if already finished
  2. Play "sfx_quest_total_complete"
  3. Set Quest.Finished = true
  4. Move to FinishedQuests dictionary
  5. Record finish time as game state
  6. Show finish popup
  7. Call Quest.Finish() → System.Finish(), award dynamic rewards
  8. Call Manager.OnQuestComplete()
  9. ItemNaming opportunity (player may name an item)
  10. Call Quest.FinishPost() → post-award dynamic rewards
  11. Add accomplishment to journal (from Quest.Accomplishment field)
  12. Award faction reputation (Quest.Factions + Quest.Reputation)
  13. Unlock achievement (Quest.Achievement)
  14. Fire QuestFinishedEvent
```

### CheckQuestFinishState

Automatically completes a quest when **all non-optional steps are finished**:

```csharp
public void CheckQuestFinishState(Quest Quest) {
    foreach (var (_, step) in Quest.StepsByID) {
        if (!step.Finished && !step.Optional)
            return;  // Still have unfinished required steps
    }
    FinishQuest(Quest);
}
```

### Failing

```
FailQuestStep(Quest, StepID) →
  - Sets Finished=false, Failed=true
  - Shows fail popup
  - Calls Quest.FailStep()

FailQuest(QuestID) →
  - Shows fail popup
  - Calls Quest.Fail() → System.Fail()
  - Removes system if RemoveWithQuest
```

### CompleteQuest (Force)

```csharp
public void CompleteQuest(string questID) {
    // Starts quest if not started, then finishes ALL steps
    if (!HasQuest(questID)) StartQuest(questID);
    foreach (step in quest.StepsByID.Keys)
        FinishQuestStep(questID, step);
}
```

---

## 4. Quest Events

All derive from `IQuestEvent` (which extends `MinEvent`):

| Event | Fired When | Contains |
|-------|-----------|----------|
| `QuestStartedEvent` | Quest is started | Quest |
| `QuestStepFinishedEvent` | Step completed | Quest, Step |
| `QuestFinishedEvent` | Entire quest completed | Quest |

Events are dispatched to `The.Game` (game systems) and also to the player object. This allows:
- IQuestSystem subclasses to register for game events and check quest state
- IPart components on the player to react to quest changes
- Any game system to observe quest progression

---

## 5. Quest Givers & Conversation Integration

### QuestHandler (Conversation Part)

`XRL.World.Conversations.Parts.QuestHandler` is the primary bridge between the conversation system and quest system. It's an `IConversationPart` attached to conversation choices.

```csharp
public class QuestHandler : IConversationPart
{
    public string QuestID;
    public string StepID;
    public string Text;     // Custom tag text
    public int XP = -1;
    public int Type;         // 0=none, 1=start, 2=step, 3=finish, 4=complete

    // On entering the dialogue choice:
    Type == 1 → The.Game.StartQuest(QuestID, The.Speaker.DisplayName)
    Type == 2 → The.Game.FinishQuestStep(QuestID, StepID, XP)
    Type == 3 → The.Game.FinishQuest(QuestID)
    Type == 4 → The.Game.CompleteQuest(QuestID)  // forces all steps done
}
```

It also provides UI tags for choices: `[Accept Quest]`, `[Complete Quest Step]`, `[Complete Quest]`, with optional `[level-based reward]` suffix.

### Conversation Predicates

Conversations use XML attributes to conditionally show choices based on quest state:
- `IfNotHaveQuest="QuestID"` — show only if quest not started
- `IfHaveQuest="QuestID"` — show only if quest active
- `IfFinishedQuest="QuestID"` — show only if quest completed
- `IfNotFinishedQuest="QuestID"` — show only if quest not yet completed

### QuestSignpost (Conversation Part)

`QuestSignpost` is used by NPC villagers to point the player toward quest givers:

```csharp
public class QuestSignpost : IConversationPart {
    // Handles PrepareTextEvent
    // Finds all NPCs in the zone who have unaccepted quests
    // Replaces "=questgivers=" token in conversation text with their
    // names + directions (e.g., "Mehmet, to the north, or Warden Ualraig, south")
}
```

### DynamicQuestSignpostConversation (Part)

Attached to village NPCs, this part dynamically adds a conversation choice that directs the player to nearby quest givers for dynamic quests. It:
1. Scans the zone for objects with `GivesDynamicQuest` property
2. Adds a conversation node listing quest givers + directional descriptions
3. Uses `<spice.quests.intro.!random>` for randomized greeting text

### GetQuestGiverState

```csharp
public int GetQuestGiverState(GameObject Object)
    // Returns: -1 = not a quest giver
    //           0 = has unaccepted quest
    //           1 = quest in progress
    //           2 = quest completed
    // Checks both "QuestGiver" tag and "GivesDynamicQuest" property
```

This is used by the rendering system to show quest-giver indicators (icons above heads).

---

## 6. Quest Completion Triggers (Parts)

These are `IPart` components that live on GameObjects and trigger quest progression when certain events occur:

### QuestStarter

Starts a quest when triggered. Supports multiple trigger types:
- **"Taken"** (default) — When player picks up or equips the item
- **"Seen"** — When the object is rendered (player sees it)
- **"OnScreen"** — When player is in same zone (end of turn check)
- **"Created"** — When object enters player's zone (before render)

Has optional `IfFinishedQuestStep` prerequisite.

### QuestStepFinisher

Completes a quest step when triggered. Same trigger types as QuestStarter. Removes itself after activation.

### FinishQuestStepWhenSlain

Completes a quest step when the object dies (`AfterDieEvent`):

```csharp
public class FinishQuestStepWhenSlain : IPart {
    public string Quest;
    public string Step;
    public string GameState;  // Optional game state to set
    public bool RequireQuest;  // If true, starts quest if not active

    // AfterDieEvent → Trigger()
    // Also handles ReplaceInContextEvent (transfers to replacement object)
    // Cleans up on ZoneActivatedEvent if step already finished
}
```

This is used extensively — e.g., warleader enemies in the Reclamation quest, boss creatures in various storylines.

### CompleteQuestOnTaken

Finishes a quest step when an item is taken, equipped, dropped, or unequipped by the player. Also listens for `QuestStartedEvent` to auto-complete if player already has the item.

### DynamicQuestTarget

Marks items as quest items: sets `NoAIEquip`, `QuestItem` property, changes physics category to "Quest Items", doubles value.

### InteractQuestTarget

Completes a quest step when the player interacts with the object (via a specific event like "Prayed" or "Desecrated").

---

## 7. IQuestSystem — Scripted Quest Logic

`IQuestSystem` (extends `IPlayerSystem`) provides a class-based approach for complex quest logic that needs to respond to game events over time.

```csharp
public abstract class IQuestSystem : IPlayerSystem
{
    public string QuestID;
    public Quest Quest { get; set; }       // The active quest
    public Quest Blueprint { get; }         // The XML template
    public virtual bool RemoveWithQuest => true;

    // Lifecycle
    virtual void Start()
    virtual void Finish()
    virtual void Fail()
    virtual void FinishStep(QuestStep)
    virtual void FailStep(QuestStep)

    // Popups (customizable)
    virtual void ShowStartPopup()
    virtual void ShowFinishPopup()
    virtual void ShowFinishStepPopup(QuestStep)
    virtual void ShowFailPopup()
    virtual void ShowFailStepPopup(QuestStep)

    virtual GameObject GetInfluencer()  // NPC who influenced this quest (for item naming)

    // Property access delegates to Quest
    GetProperty<T>(), SetProperty(), GetList(), GetDictionary()
}
```

### How Systems Register for Events

IQuestSystem extends `IPlayerSystem`, which can register for game-level and player-level events:

```csharp
// Register for game events
public override void Register(XRLGame Game, IEventRegistrar Registrar) {
    Registrar.Register(ZoneActivatedEvent.ID);
    Registrar.Register(EndTurnEvent.ID);
}

// Register for player events
public override void RegisterPlayer(GameObject Player, IEventRegistrar Registrar) {
    Registrar.Register(TookEvent.ID);
    Registrar.Register(EnteringZoneEvent.ID);
}
```

### Example: ArgyveKnicknackSystem

```csharp
public class ArgyveKnicknackSystem : IQuestSystem {
    // Registers for TookEvent on the player
    // When player picks up a TinkerItem with Complexity > 0 → complete step
    // On Start(), checks existing inventory for valid items
    // GetInfluencer() → returns Argyve NPC for item naming
}
```

### Example: WillingSpiritSystem

```csharp
public class WillingSpiritSystem : IQuestSystem {
    // Registers for TookEvent
    // Checks if item is "Scrapped Waydroid" or "Dormant Waydroid"
    // Progressively completes multiple steps based on which item found
    // On Start(), checks existing inventory
}
```

### Example: ReclamationSystem (Complex)

The most complex quest system — manages a large-scale battle:
- Registers for `ZoneActivatedEvent`, `EndTurnEvent`, `EnteringZoneEvent`
- Manages a perimeter of zones around the battle area
- Spawns waves of enemies on timers
- Creates warleader enemies with `FinishQuestStepWhenSlain` parts
- Handles reinforcement waves after failures
- Displaces/restores fauna during battles
- Controls music and camera shakes
- Tracks attempt numbers with quest name updates

---

## 8. QuestManager — Per-Quest IPart

`QuestManager` is an `IPart`-derived class serialized directly with the Quest. Unlike IQuestSystem (which is a game system), QuestManager travels with the quest data.

```csharp
public class QuestManager : IPart
{
    public string MyQuestID;
    public virtual void AfterQuestAdded()     // Called after quest start + system init
    public virtual void OnQuestAdded()        // Called during quest start
    public virtual void OnStepComplete(string StepName)
    public virtual void OnQuestComplete()
    public virtual GameObject GetQuestInfluencer()
    public virtual string GetQuestZoneID()    // Used for XP zone attribution
}
```

The QuestManager is specified in the XML: `<quest Manager="FrayingFavorites">` resolves to `XRL.World.QuestManagers.FrayingFavorites`.

Dynamic quests also use QuestManagers (e.g., `FindASiteDynamicQuestManager`, `FindASpecificItemDynamicQuestManager`, `InteractWithAnObjectDynamicQuestManager`) to track quest-specific state like item IDs and zone IDs.

---

## 9. Dynamic Quest Generation

Dynamic quests are procedurally generated during worldgen and assigned to village NPCs.

### Pipeline

```
World Generation
  → For each village:
    → VillageDynamicQuestContext created with village snapshot
    → DynamicQuestFactory.fabricateQuestTemplate(type, context)
      → Picks random template: FindASite, FindASpecificItem, InteractWithAnObject
      → Template.init(context):
        - Gets delivery target (nearby undiscovered location)
        - Gets quest item (from village signature items or random)
        - Registers ZoneBuilders to fabricate quest giver and quest item
    → Quest blueprint stored in DynamicQuestsGameState
    → ZoneBuilder places quest giver NPC with conversation at zone build time
```

### DynamicQuestContext (Abstract)

```csharp
public abstract class DynamicQuestContext {
    int questNumber;       // Which quest in the chain (0, 1, 2...)
    int questChainNumber;
    int tier;              // Village tier (affects rewards)

    abstract HistoricEntity originEntity();
    abstract string originZoneId();
    abstract string questTargetZone(int min, int max);
    abstract GeneratedLocationInfo getNearbyUndiscoveredLocation();
    abstract DynamicQuestReward getQuestReward();
    abstract Func<GameObject, bool> getQuestGiverFilter();
    abstract Func<GameObject, bool> getQuestActorFilter();
    abstract List<string> getSacredThings();
    abstract DynamicQuestDeliveryTarget getQuestDeliveryTarget();
    abstract GameObject getQuestRemoteInteractable();
    abstract GameObject getQuestDeliveryItem();
    abstract string getQuestItemNameMutation(string input);
}
```

### VillageDynamicQuestContext

The concrete implementation for village quests:
- **Quest Giver Filter**: Must be a `ParticipantVillager` + `NamedVillager` who doesn't already `GivesDynamicQuest`
- **Item Generation**: Uses village `signatureItem`, `signatureHistoricObjectType`, or random tier-appropriate items. Colors items with village palette.
- **Name Mutation**: Generates evocative item names like "the sacred chalice of renewal" using spice tables + village adjective roots
- **Interactables**: Creates shrines, statues, desecrated objects based on village beliefs (worshipped/despised creatures, factions, sultans)
- **Location Finding**: Searches for undiscovered locations within 12-18 zone distance, avoiding used locations and high-tier zones
- **Tier Validation**: Ensures quest destinations don't exceed village tier + filter

### Three Dynamic Quest Templates

#### FindASiteDynamicQuestTemplate
- Player must travel to and discover a specific location
- ZoneBuilder fabricates a quest giver NPC
- Quest story types provide narrative framing

#### FindASpecificItemDynamicQuestTemplate
- Player must find and retrieve a specific item from a remote location
- Creates a quest item with `DynamicQuestTarget` part
- Registers ZoneBuilders for both quest giver zone and item zone
- Item gets custom name via `getQuestItemNameMutation()`

#### InteractWithAnObjectDynamicQuestTemplate
- Player must travel to a location and interact with an object (pray at shrine, desecrate statue, use device)
- Object gets `InteractQuestTarget` part with custom event ID
- Uses `QuestableVerb` / `QuestableEvent` tags from blueprints
- Objects can be shrines to worshipped/despised creatures, sultan statues, or tech items

### DynamicQuestConversationHelper

Builds conversation trees for dynamic quests programmatically:

```csharp
static void appendQuestCompletionSequence(conversation, quest, ...) {
    // Creates conversation nodes for:
    // 1. Quest intro (with accept/reject choices)
    // 2. Quest complete (with reward dialogue)
    // 3. Quest incomplete (reminder dialogue)
    // Uses IfHaveQuest/IfNotHaveQuest/IfFinishedQuest predicates
    // Supports "Choice" reward type (pick from stockpile) or
    //   "VillageZeroMainQuest" (leads into Barathrumites storyline)
}
```

Uses `<spice.quests.intro.!random>`, `<spice.quests.thanks.!random>` for randomized dialogue.

---

## 10. Journal System Architecture

The journal is managed by `JournalAPI` — a static class with game-based cache that stores all journal entries.

### Storage

```csharp
public static class JournalAPI {
    static StringMap<IBaseJournalEntry> NotesByID;      // All entries by ID
    static List<JournalAccomplishment> Accomplishments;  // Player deeds
    static List<JournalObservation> Observations;        // Gossip, lore bits
    static List<JournalMapNote> MapNotes;                // Location discoveries
    static List<JournalRecipeNote> RecipeNotes;          // Cooking recipes
    static List<JournalGeneralNote> GeneralNotes;        // Misc notes
    static List<JournalSultanNote> SultanNotes;          // Sultan history entries
    static List<JournalVillageNote> VillageNotes;        // Village lore
}
```

### Key Operations

- **AddAccomplishment()** — Records a player deed with optional mural text, gospel text, and screenshot
- **AddObservation()** — Adds a piece of gossip/lore (can be revealed/unrevealed)
- **AddMapNote()** — Notes a location with zone coordinates, category, attributes
- **AddSultanNote()** — Records sultan history entry linked to a HistoricEvent
- **AddVillageNote()** — Records village lore entry
- **AddRecipeNote()** — Records a cooking recipe
- **AddGeneralNote()** — Adds a miscellaneous note
- **TryRevealNote(ID)** — Reveals a note by its secret ID
- **InitializeGossip()** — Creates 60 random gossip entries at game start
- **InitializeSultanEntries()** — Creates sultan lore from history
- **InitializeVillageEntries()** — Creates village lore from history + hardcoded entries

### Reveal/Forget Mechanism

All journal entries support a **Revealed/Unrevealed** state:
- Unrevealed entries exist in the data but aren't shown to the player
- Entries are revealed through: exploration, water ritual trading, conversation, quest completion
- Each entry type has a custom `Reveal()` method that shows an appropriate message
- `Forgettable()` determines if an entry can be un-revealed (MapNotes are NOT forgettable)
- `SecretVisibilityChangedEvent` fires when reveal state changes

### Tradable Secrets

Entries have a `Tradable` flag:
- `CanSell()` — Tradable + Revealed (you know it and can share)
- `CanBuy()` — Tradable + NOT Revealed (you can learn it)
- MapNotes cannot be sold if you're currently in that zone
- Accomplishments are never tradable

---

## 11. Journal Entry Types

### IBaseJournalEntry (Base Class)

```csharp
public class IBaseJournalEntry {
    string ID;                    // Secret/unique identifier
    string History;               // Appended history lines
    string Text;                  // Main display text
    string LearnedFrom;           // Who/what revealed this
    int Weight = 100;             // For weighted random selection
    bool Revealed;                // Player knows this
    bool Tradable = true;         // Can be traded in water ritual
    List<string> Attributes;      // Tag-like attributes for filtering
}
```

### JournalAccomplishment

Player deeds — things the player has done.

```csharp
public class JournalAccomplishment : IBaseJournalEntry {
    long Time;                     // Game tick when accomplished
    string Category;               // "general" etc.
    string MuralText;              // Text for golem quest mural selection
    string GospelText;             // Third-person version for history/gospel
    string AggregateWith;          // Group similar accomplishments
    MuralCategory MuralCategory;   // Category for golem quest
    MuralWeight MuralWeight;       // Weight for golem quest selection
    SnapshotRenderable[] Screenshot; // 9x5 grid screenshot at moment of accomplishment
}
```

Special features:
- **Screenshot capture** — 9x5 tile snapshot of the area around the player at the moment of accomplishment
- **Lovesick modification** — If player has Lovesick effect, accomplishment text gets modified with lovesick language
- **On-fire/frozen prefix** — "While on fire, ..." / "While frozen solid, ..."
- **MuralCategory** — Used in the Golem Quest to select accomplishments for the golem's "incantation"
- **GospelText** — Third-person version used in procedural history generation

### JournalMapNote

Location discoveries with world coordinates.

```csharp
public class JournalMapNote : IBaseJournalEntry {
    string WorldID;     // "JoppaWorld" etc.
    int ParasangX, ParasangY;  // World map coordinates
    int ZoneX, ZoneY, ZoneZ;   // Zone within parasang
    string Category;    // "Settlements", "Ruins", "Artifacts", "Historic Sites",
                        // "Lairs", "Merchants", "Natural Features", "Oddities",
                        // "Baetyls", "Named Locations", "Ruins with Becoming Nooks"
    long Time;
    bool Tracked;       // Whether tracked on map
}
```

Features:
- **ZoneID** computed property: `WorldID.ParasangX.ParasangY.ZoneX.ZoneY.ZoneZ`
- **Visited** / **LastVisit** — checks ZoneManager visited time
- **Direction display** — `GetDisplayText()` includes cardinal directions from player's location
- **Not forgettable** — Once discovered, can't be un-discovered
- **Category-specific reveal messages** — "You note the location of X in the Locations > Y section"

### JournalObservation

Gossip, rumors, and discovered facts.

```csharp
public class JournalObservation : IBaseJournalEntry {
    long Time;
    string Category;      // "general", "Gossip"
    string RevealText;    // Alternative text shown on reveal
    bool Rumor;           // If true, treated as a rumor fragment
}
```

### JournalSultanNote

Sultan history entries linked to procedurally generated historic events.

```csharp
public class JournalSultanNote : IBaseJournalEntry {
    string SultanID;    // ID of the historic sultan entity
    long EventID;       // ID of the historic event
}
```

Special features:
- **Forgettable** check: NOT forgettable if event has `revealsRegion`, `revealsItem`, or `revealsItemLocation`
- **Achievement**: When all notes for a sultan are revealed → `LEARN_ONE_SULTAN_HISTORY` achievement
- Attributes include `sultan`, `include:faction`, `rebekah`, `gyreplagues`, `sultanTombPropaganda`

### JournalVillageNote

Village lore and gospels.

```csharp
public class JournalVillageNote : IBaseJournalEntry {
    string VillageID;    // Historic entity ID of the village
    long EventID;        // Associated historic event
}
```

Text uses `|` delimiter — `GetShortText()` returns text before the last `|`.

### JournalRecipeNote

Cooking recipes.

```csharp
public class JournalRecipeNote : IBaseJournalEntry {
    CookingRecipe Recipe;  // The actual recipe object
}
```

On reveal: adds recipe to `CookingGameState.instance.knownRecipies`.

### JournalGeneralNote

Simple timestamped notes.

```csharp
public class JournalGeneralNote : IBaseJournalEntry {
    long Time;
}
```

---

## 12. Accomplishments

### How Accomplishments Are Created

The `JournalAPI.AddAccomplishment()` method is called from many places:

1. **Quest completion** — `XRLGame.FinishQuest()` adds accomplishment from `Quest.Accomplishment` field
2. **Parts** — `TakenAccomplishment` (picking up items), `EatenAccomplishment` (eating items)
3. **Quest systems** — Custom IQuestSystem implementations
4. **Direct calls** — Various game systems call `JournalAPI.AddAccomplishment()` directly

### Accomplishment Properties

```csharp
AddAccomplishment(
    string text,           // "You did X"
    string muralText,      // Third-person for mural (auto-generated if null)
    string gospelText,     // Third-person for gospel
    string aggregateWith,  // Group key for similar accomplishments
    string category,       // "general"
    MuralCategory,         // Used in Golem Quest
    MuralWeight,           // Weight for selection probability
    string secretId,       // Optional secret ID
    long time              // -1 for current time
)
```

### MuralCategory (31 categories)

Used in the Golem Quest's incantation selection to categorize player deeds:

```
Generic, IsBorn, HasInspiringExperience, Treats, CreatesSomething,
CommitsFolly, WeirdThingHappens, EnduresHardship, BodyExperienceBad,
BodyExperienceGood, BodyExperienceNeutral, Trysts, VisitsLocation,
DoesBureaucracy, LearnsSecret, FindsObject, DoesSomethingRad,
DoesSomethingHumble, DoesSomethingDestructive, BecomesLoved, Slays,
Resists, AppeasesBaetyl, WieldsItemInBattle, MeetsWithCounselors,
CrownedSultan, Dies
```

### MuralWeight

Controls selection probability in the Golem Quest:
```
Nil = 0, VeryLow = 1, Low = 2, Medium = 10, High = 50, VeryHigh = 100
```

### Gospel Text

Each accomplishment can have a "gospel text" — a third-person version suitable for inclusion in procedural history. The `GospelText` field uses `HistoricStringExpander` and forced third-person form.

### AccomplishmentAdded Event

When an accomplishment is added, an `AccomplishmentAdded` event is fired on the player's current zone, allowing zone-level parts to react (e.g., for murals, statues, or other environmental storytelling).

### TakenAccomplishment / EatenAccomplishment Parts

These Parts live on specific items and fire accomplishments when the player takes/eats them:

```csharp
public class TakenAccomplishment : IPart {
    string Text = "You got it!";
    string Hagiograph;           // Mural version
    string HagiographCategory;   // MuralCategory
    string HagiographWeight;     // MuralWeight
    string Gospel;               // Gospel version
    bool Triggered;              // One-shot
    // Fires on TakenEvent or EquippedEvent
}
```

---

## 13. Gossip & Lore Generation

### Gossip System

`Gossip` (static class in `XRL.World.Parts`) generates procedural faction gossip:

```csharp
public static class Gossip {
    // One-faction gossip: "some <group> <gossip about faction>"
    static string GenerateGossip_OneFaction(string faction)

    // Two-faction gossip using spice templates
    static string GenerateGossip_TwoFactions(string actor, string actee)
        // Uses: <spice.gossip.twoFaction.!random>
        // Replaces *f1* and *f2* with faction names or random member names
        // 40% chance to use a specific creature name instead of faction name
}
```

### Gossip Initialization

`JournalAPI.InitializeGossip()` creates **60 gossip entries** at game start:
- 75% are one-faction gossip
- 25% are two-faction gossip (random pair of different factions)
- All start unrevealed with "gossip" and "gossip:FactionName" attributes
- Can be revealed through water ritual or other means

### Static Observations

`JournalAPI.InitializeObservations()` adds hardcoded lore:
- "Qud was once called Salum."
- "The shomer Rainwater claims that Brightsheol is the dream of a seraph..."
- "The Palladium Reef was once called Maqqom Yd..."

---

## 14. Sultan History & Procedural Lore

### History System

The game generates a procedural history of sultans using `HistoryKit`:
- `History` — Contains `HistoricEntity` objects (sultans, villages)
- `HistoricEntity` — Has snapshots at different time periods with properties
- `HistoricEvent` — Individual events in a sultan's life with properties like "gospel", "tombInscription"

### Sultan Note Generation

`JournalAPI.InitializeSultanEntries()`:
1. Iterates all sultans from `HistoryAPI.GetSultans()`
2. Gets liked/hated factions for each sultan
3. For each event with a gospel → creates `JournalSultanNote` with attributes:
   - `include:faction` for each liked/hated faction
   - `sultan` attribute
   - Optional: `rebekah`, `rebekahWasHealer`, `gyreplagues`
4. For each event with a tomb inscription → creates note with `sultanTombPropaganda` attribute

### Village Lore Generation

`JournalAPI.InitializeVillageEntries()`:
1. Iterates all villages from `HistoryAPI.GetVillages()`
2. Calls `AddVillageGospels()` for each — extracts "Gospels" list from village snapshot
3. Each gospel is split by `|` into text + optional event ID
4. Adds hardcoded village notes for Joppa, Kyakukya, Yd Freehold

### HistoryAPI Key Methods

```csharp
public static class HistoryAPI {
    static List<HistoricEntity> GetSultans()
    static List<HistoricEntity> GetVillages()
    static List<HistoricEntity> GetKnownSultans()   // Only those with revealed notes
    static HistoricEntitySnapshot GetSultanForPeriod(int period)
    static List<string> GetSultanLikedFactions(HistoricEntity)
    static List<string> GetSultanHatedFactions(HistoricEntity)
    static HistoricEntity GetResheph()   // The special sultan
    static void ExpandVillageText(StringBuilder, faction, snapshot)
        // Replaces =village.name=, =village.sacred=, =village.profane=, =village.activity=
}
```

---

## 15. Secret Trading (Water Ritual)

The water ritual is the primary mechanism for trading journal entries:

- `IBaseJournalEntry.CanSell()` — Tradable + Revealed
- `IBaseJournalEntry.CanBuy()` — Tradable + NOT Revealed
- `HistoryAPI.OnWaterRitualShuffleSecrets()` — Hook for shuffling available secrets
- `HistoryAPI.OnWaterRitualBuySecret()` — Hook for buying a secret
- `HistoryAPI.OnWaterRitualSellSecret()` — Hook for selling secrets

Secret attributes control what NPCs are interested in:
- `gossip:FactionName` — Faction-relevant gossip
- `include:FactionName` — Sultan lore relevant to faction
- `onlySellIfTargetedAndInterested` — Only offered to interested factions
- `sultanTombPropaganda` — Tomb inscriptions
- `old` — Historical observations

---

## 16. Quest Reward System

### Static Rewards (XML Quests)

Defined in the Quest XML and applied on completion:
- **Accomplishment** — `Quest.Accomplishment` text → journal entry
- **Faction Reputation** — `Quest.Factions` (comma-delimited) + `Quest.Reputation` (amount)
- **Achievement** — `Quest.Achievement` → platform achievement
- **XP** — Per-step via `QuestStep.XP`
- **Item Naming** — `ItemNaming.Opportunity()` on quest completion (player can name an item)

### Dynamic Rewards

`DynamicQuestReward` contains:
```csharp
public class DynamicQuestReward {
    int StepXP;                              // Divided evenly among steps
    List<DynamicQuestRewardElement> rewards;       // Awarded on quest finish
    List<DynamicQuestRewardElement> postrewards;   // Awarded after finish
}
```

### Reward Elements

| Class | Effect |
|-------|--------|
| `DynamicQuestRewardElement_Reputation` | Modifies faction reputation |
| `DynamicQuestRewardElement_GameObject` | Gives player a cached game object |
| `DynamicQuestRewardElement_ChoiceFromPopulation` | Player picks from N random item sets |
| `DynamicQuestRewardElement_Quest` | Starts another quest as reward |
| `DynamicQuestRewardElement_VillageZeroLoot` | Special Village Zero loot |
| `DynamicQuestRewardElement_VillageZeroMainQuestHook` | Hooks into main quest line |

### Village Zero Rewards

Special reward scaling for the starting village:
- Quest 0: 750 XP/step + village recoiler + VillageZeroMainQuestHook + 50 rep
- Quest 1: 1000 XP/step + choice from "VillageZero_Reward" population (3 choices) + VillageZeroLoot + 100 rep
- Later quests: `tier * 1000` XP/step + 100 rep + choice from tier-appropriate population

### Level-Based Bonus

Quests with `BonusAtLevel` get displayed as `[Accept Quest - level-based reward]` in the conversation UI, indicating the reward scales with player level.

---

## 17. Quest Difficulty & Consider

```csharp
public static string Consider(Quest Q) {
    int delta = player.Level - Q.Level;
    if (delta <= -15) return "[Impossible]";    // Red
    if (delta <= -10) return "[Very Tough]";    // Dark red
    if (delta <= -5)  return "[Tough]";         // White
    if (delta < 5)    return "[Average]";       // Gray
    if (delta <= 10)  return "[Easy]";          // Green
    return "[Trivial]";                          // Bright green
}
```

---

## 18. Key Source Files Reference

### Core Quest System
| File | Description |
|------|-------------|
| `XRL.World/Quest.cs` | Quest data model |
| `XRL.World/QuestStep.cs` | Quest step with flags |
| `XRL.World/QuestManager.cs` | Per-quest IPart manager |
| `XRL.World/QuestLoader.cs` | Loads Quests.xml |
| `XRL/IQuestSystem.cs` | Abstract scripted quest logic |
| `XRL/XRLGame.cs` (lines 530-930) | StartQuest, FinishQuest, FinishQuestStep, etc. |
| `Qud.API/QuestsAPI.cs` | Simple quest enumeration API |

### Quest Events
| File | Description |
|------|-------------|
| `XRL.World/IQuestEvent.cs` | Base quest event |
| `XRL.World/QuestStartedEvent.cs` | Quest started |
| `XRL.World/QuestStepFinishedEvent.cs` | Step completed |
| `XRL.World/QuestFinishedEvent.cs` | Quest completed |

### Quest Parts (Triggers)
| File | Description |
|------|-------------|
| `XRL.World.Parts/QuestStarter.cs` | Start quest on trigger |
| `XRL.World.Parts/QuestStepFinisher.cs` | Complete step on trigger |
| `XRL.World.Parts/FinishQuestStepWhenSlain.cs` | Complete step on death |
| `XRL.World.Parts/CompleteQuestOnTaken.cs` | Complete step when item taken |
| `XRL.World.Parts/DynamicQuestTarget.cs` | Mark items as quest items |
| `XRL.World.Parts/InteractQuestTarget.cs` | Complete step on interaction |

### Conversation Integration
| File | Description |
|------|-------------|
| `XRL.World.Conversations.Parts/QuestHandler.cs` | Start/finish quests from dialogue |
| `XRL.World.Conversations.Parts/QuestSignpost.cs` | Direct player to quest givers |
| `XRL.World.Parts/DynamicQuestSignpostConversation.cs` | Dynamic quest giver directions |
| `XRL.World/DynamicQuestConversationHelper.cs` | Build dynamic quest conversations |

### Dynamic Quests
| File | Description |
|------|-------------|
| `XRL.World/DynamicQuestFactory.cs` | Creates dynamic quest templates |
| `XRL.World/BaseDynamicQuestTemplate.cs` | Abstract template |
| `XRL.World/DynamicQuestContext.cs` | Abstract quest generation context |
| `XRL.World/VillageDynamicQuestContext.cs` | Village quest context |
| `XRL.World/DynamicQuestsGameState.cs` | Stores generated quest blueprints |
| `XRL.World/FindASiteDynamicQuestTemplate.cs` | "Find a location" template |
| `XRL.World/FindASpecificItemDynamicQuestTemplate.cs` | "Find an item" template |
| `XRL.World/InteractWithAnObjectDynamicQuestTemplate.cs` | "Interact with object" template |
| `XRL.World/DynamicQuestDeliveryTarget.cs` | Target location data |

### Dynamic Quest Rewards
| File | Description |
|------|-------------|
| `XRL.World/DynamicQuestReward.cs` | Reward container |
| `XRL.World/DynamicQuestRewardElement.cs` | Abstract reward element |
| `XRL.World/DynamicQuestRewardElement_Reputation.cs` | Faction reputation reward |
| `XRL.World/DynamicQuestRewardElement_GameObject.cs` | Item reward |
| `XRL.World/DynamicQuestRewardElement_ChoiceFromPopulation.cs` | Pick-from-list reward |
| `XRL.World/DynamicQuestRewardElement_Quest.cs` | Quest chain reward |

### Specific Quest Systems
| File | Description |
|------|-------------|
| `XRL.World.Quests/ArgyveKnicknackSystem.cs` | Find a tinker item |
| `XRL.World.Quests/WillingSpiritSystem.cs` | Find/repair waydroid |
| `XRL.World.Quests/ReclamationSystem.cs` | Large-scale battle quest |
| `XRL.World.Quests/AscensionSystem.cs` | Endgame quest |
| `XRL.World.Quests/PaxKlanqIPresumeSystem.cs` | Pax Klanq quest |
| `XRL.World.Quests/GolemQuestSystem.cs` | Golem construction quest |

### Journal System
| File | Description |
|------|-------------|
| `Qud.API/JournalAPI.cs` | Central journal management |
| `Qud.API/IBaseJournalEntry.cs` | Base entry with reveal/trade |
| `Qud.API/JournalAccomplishment.cs` | Player deeds |
| `Qud.API/JournalMapNote.cs` | Location discoveries |
| `Qud.API/JournalObservation.cs` | Gossip and facts |
| `Qud.API/JournalSultanNote.cs` | Sultan history |
| `Qud.API/JournalVillageNote.cs` | Village lore |
| `Qud.API/JournalRecipeNote.cs` | Cooking recipes |
| `Qud.API/JournalGeneralNote.cs` | General notes |
| `Qud.API/MuralCategory.cs` | 31 accomplishment categories |
| `Qud.API/MuralWeight.cs` | Accomplishment weights |

### Gossip & History
| File | Description |
|------|-------------|
| `XRL.World.Parts/Gossip.cs` | Procedural gossip generation |
| `Qud.API/HistoryAPI.cs` | Sultan/village history access |

### Accomplishment Parts
| File | Description |
|------|-------------|
| `XRL.World.Parts/TakenAccomplishment.cs` | Accomplishment on pickup |
| `XRL.World.Parts/EatenAccomplishment.cs` | Accomplishment on eating |
