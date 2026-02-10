# Caves of Qud — XP, Leveling, Skills & Mutations: Deep Dive

> Compiled from decompiled source code at `/Users/steven/qud-decompiled-project/`
> Core files: `Leveler.cs`, `Experience.cs`, `Skills.cs`, `Mutations.cs`, `BaseMutation.cs`

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [The XP Stat and AwardXP Pipeline](#2-the-xp-stat-and-awardxp-pipeline)
3. [XP from Kills](#3-xp-from-kills)
4. [Tier Scaling (Level Difference Reduction)](#4-tier-scaling)
5. [XP from Exploration and Quests](#5-xp-from-exploration-and-quests)
6. [XP from Sifrah Games (Tinkering, Social, etc.)](#6-xp-from-sifrah-games)
7. [XP from Other Sources](#7-xp-from-other-sources)
8. [XP Party Sharing](#8-xp-party-sharing)
9. [Wander Mode XP System](#9-wander-mode-xp-system)
10. [XP Blockers and Modifiers](#10-xp-blockers-and-modifiers)
11. [Level-Up: XP Thresholds](#11-level-up-xp-thresholds)
12. [Level-Up: The Leveler Part](#12-level-up-the-leveler-part)
13. [Level-Up Rewards Breakdown](#13-level-up-rewards-breakdown)
14. [Hitpoints Per Level](#14-hitpoints-per-level)
15. [Skill Points (SP) System](#15-skill-points-sp-system)
16. [Skill Tree Structure](#16-skill-tree-structure)
17. [Skill Purchase Mechanics](#17-skill-purchase-mechanics)
18. [Skill Implementation Patterns](#18-skill-implementation-patterns)
19. [Complete Skill Catalog](#19-complete-skill-catalog)
20. [Mutation Points (MP) System](#20-mutation-points-mp-system)
21. [Mutation Advancement](#21-mutation-advancement)
22. [Mutation Cap and Level Scaling](#22-mutation-cap-and-level-scaling)
23. [Buying New Mutations](#23-buying-new-mutations)
24. [Rapid Advancement](#24-rapid-advancement)
25. [Chimera vs Esper Paths](#25-chimera-vs-esper-paths)
26. [Mutation Defects](#26-mutation-defects)
27. [Attribute Points](#27-attribute-points)
28. [Creature Level and Tier](#28-creature-level-and-tier)
29. [Genotype Differences (True Kin vs Mutant)](#29-genotype-differences)
30. [Complete XP Source Reference](#30-complete-xp-source-reference)

---

## 1. Architecture Overview

The progression system in Qud has four interconnected pillars:

| System | Stat Key | Earned Per Level | Spent On |
|--------|----------|-----------------|----------|
| **XP** | `"XP"` | N/A (earned from gameplay) | Triggers level-ups |
| **Skill Points** | `"SP"` | 50 + (Int-10)*4 | Purchasing skills (binary) |
| **Mutation Points** | `"MP"` | 1 (Mutants only) | Advancing mutation ranks, buying new mutations |
| **Attribute Points** | `"AP"` | 1 every 3rd level (not 6th) | Raising any attribute by 1 |

All four are stored as standard `Statistic` objects on the GameObject. Spending increments `Penalty` (not decrementing `BaseValue`), so lifetime earned totals are preserved.

### Core Files

| File | Purpose |
|------|---------|
| `XRL.World.Parts/Leveler.cs` | Level-up logic, XP thresholds, reward distribution |
| `XRL.World.Parts/Experience.cs` | XP tier scaling, XP multiplier, party sharing |
| `XRL.World.Parts/Skills.cs` | Skill container, add/remove skills |
| `XRL.World.Parts/Mutations.cs` | Mutation container, level/add/remove mutations |
| `XRL.World.Parts.Mutation/BaseMutation.cs` | Mutation level calculation, cap enforcement |
| `XRL.World/GameObject.cs` | AwardXP, AwardXPTo, HasSkill, AddSkill |
| `XRL.Rules/Stat.cs` | Score modifier formula |
| `XRL.World.Skills/SkillFactory.cs` | Skill tree loaded from Skills.xml |

---

## 2. The XP Stat and AwardXP Pipeline

XP is stored as a `Statistic` keyed `"XP"`. Creatures also have `"XPValue"` defining how much XP they're worth when killed.

### Event Pipeline (3 stages)

All three events inherit from `IXPEvent`:

```csharp
public abstract class IXPEvent : MinEvent
{
    public GameObject Actor;          // who receives XP
    public GameObject Kill;           // what was killed (null if not a kill)
    public GameObject InfluencedBy;   // who influenced the XP gain
    public GameObject PassedUpFrom;   // follower who passed XP up
    public GameObject PassedDownFrom; // leader who passed XP down
    public string ZoneID;             // zone context
    public int Amount;                // XP amount
    public int AmountBefore;          // XP before award
    public int Tier;                  // tier for scaling (-1 = no scaling)
    public int Minimum;               // floor clamp
    public int Maximum;               // ceiling clamp
    public string Deed;               // description
    public bool TierScaling = true;   // whether tier reduction applies
}
```

| Stage | Event | Purpose |
|-------|-------|---------|
| 1 | `AwardingXPEvent` | Pre-award check. Returning false blocks XP entirely. |
| 2 | `AwardXPEvent` | Main award. `Experience` part handles tier scaling, XPMul, stat update, party propagation. |
| 3 | `AwardedXPEvent` | Post-award. `Leveler` part checks for level-ups. |

### Entry Point

```csharp
// GameObject.cs line 13213
public int AwardXP(int Amount, int Tier = -1, int Minimum = 0, int Maximum = int.MaxValue,
    GameObject Kill = null, GameObject InfluencedBy = null, ...)
{
    return AwardXPEvent.Send(this, Amount, Tier, Minimum, Maximum, Kill, ...);
}
```

---

## 3. XP from Kills

When a creature dies, `AwardXPTo` is called on the killer:

```csharp
// GameObject.cs line 13218
public int AwardXPTo(GameObject Subject, bool ForKill = true, ...)
{
    if (!HasTagOrProperty("NoXP") && Statistics.TryGetValue("XPValue", out var value))
    {
        int xpValue = GetIntProperty("*XPValue", value.Value);
        int tier = -1;
        if (Statistics.TryGetValue("Level", out var level))
            tier = level.Value / 5;   // Tier = victim's Level / 5

        Subject.AwardXP(xpValue, tier, 0, int.MaxValue, ForKill ? this : null, ...);
        Statistics.Remove("XPValue");  // Consumed — only awards ONCE
    }
}
```

Key details:
- **`XPValue` stat** defines base XP. Consumed after awarding (removed from Statistics).
- **`"NoXP"` tag** prevents any XP from being awarded for killing this creature.
- **Tier** for scaling = `victimLevel / 5` (integer division).
- **HeroMaker** sets XPValue: `Level/2 * 200` for heroes, `Level/2 * 100` for minions.

---

## 4. Tier Scaling

The `Experience` part applies level-difference reduction to kill XP:

```csharp
// Experience.cs lines 33-48
if (E.TierScaling && E.Tier >= 0)
{
    int tierDiff = (ParentObject.Stat("Level") / 5) - E.Tier;
    if (tierDiff > 2)      num = 0;       // 3+ tiers above: NO XP
    else if (tierDiff > 1)  num /= 10;    // 2 tiers above: 10% XP
    else if (tierDiff > 0)  num /= 2;     // 1 tier above: 50% XP
    // 0 or negative: 100% (full XP)
}
```

Where tier = `Level / 5` (integer division):

| Player Level | Player Tier | Enemy Level | Enemy Tier | Tier Diff | XP % |
|-------------|-------------|-------------|------------|-----------|------|
| 1-4 | 0 | 1-4 | 0 | 0 | 100% |
| 5-9 | 1 | 1-4 | 0 | 1 | 50% |
| 10-14 | 2 | 1-4 | 0 | 2 | 10% |
| 15-19 | 3 | 1-4 | 0 | 3 | 0% |
| 10-14 | 2 | 5-9 | 1 | 1 | 50% |
| 10-14 | 2 | 10-14 | 2 | 0 | 100% |

---

## 5. XP from Exploration and Quests

### Location Discovery

```csharp
// LocationFinder.cs line 85
if (!mapNote.Revealed)
{
    Popup.Show("You discover " + text + "!");
    The.Player.AwardXP(Value, -1, ...);  // Tier = -1, NO tier scaling
    JournalAPI.RevealMapNote(mapNote);
}
```

The `Value` field is per-location in blueprint data. **No tier scaling** (Tier = -1).

### Quest Completion

```csharp
// XRLGame.cs line 796
if (value.XP > 0)
    The.Player.AwardXP(value.XP, -1, ...);  // No tier scaling
```

Each quest step has an `XP` field from `Quests.xml`. Dynamic village quests divide `StepXP` equally across steps.

Special quests:
- **Golem Quest**: Flat 40,000 XP
- **Reclamation**: Awards enough XP to reach level 37: `Leveler.GetXPForLevel(37) - currentXP`

---

## 6. XP from Sifrah Games

All Sifrah (mini-game) activities follow the same formula pattern:

**Base formula**: `(Tokens² - Slots) × Difficulty × Multiplier`

With incomplete solution reduction: `xp = xp × (100 - (100 - PercentSolved) × 3) / 100`

| Sifrah Type | Success Multiplier | Critical Success Multiplier |
|------------|-------------------|---------------------------|
| **Reverse Engineering** | ×(Complexity+Difficulty) | ×(Complexity+Difficulty)×5 |
| **Examine Artifact** | ×(Complexity+Difficulty) | ×(Complexity+Difficulty)×3 |
| **Hacking** | ×(Complexity+Difficulty) | ×(Complexity+Difficulty)×2 |
| **Item Modding** | ×(Complexity+Difficulty)×2 | ×(Complexity+Difficulty)×10 |
| **Repairing** | ×(Complexity+Difficulty) | ×(Complexity+Difficulty)×3 |
| **Disarming** | ×Difficulty | ×Difficulty×3 |
| **Water Ritual** | ×Difficulty/3 | ×Difficulty |
| **Beguiling** | ×Difficulty/3 | ×Difficulty |
| **Proselytization** | ×Difficulty/3 | ×Difficulty |
| **Rebuking** | ×Difficulty/3 | ×Difficulty |
| **Haggling** | ×Difficulty/3 | ×Difficulty |
| **Psychic Combat** | ×Difficulty/3 | ×Difficulty |
| **Reality Distortion** | ×Difficulty/3 | ×Difficulty |
| **Baetyl Offering** | ×Difficulty/3 | ×Difficulty |
| **Item Naming** | ×Difficulty/3 | ×Difficulty |

All Sifrah XP has Tier = -1 (no tier scaling).

---

## 7. XP from Other Sources

| Source | Amount/Formula | Notes |
|--------|---------------|-------|
| **Cooking effect** | 1,000 flat | `ProceduralCookingEffectUnit_XP` |
| **Librarian book donation** | `(bookValue)² / 25` per book | `LibrarianGiveBook.cs` |
| **Resheph secrets** | Escalating: 250, 500, 1000, 2000, 3000, 4000, 5000, 6000, 7500, 10000, 12500, 15000, 20000, 25000, 30000, 40000+ | Per secret shared |
| **Baetyl altar offering** | `RewardAmount × (50 + Level)` | `RandomAltarBaetyl.cs` |
| **Waking Dream (pleasant)** | 15,000 flat | `WakingDream.cs` |
| **Conversation delegate** | Arbitrary (XML-defined) | Any conversation node |
| **MutationPointsOnEat** | MP, not XP (for Mutants) | True Kin get poisoned instead |

---

## 8. XP Party Sharing

The `Experience` part propagates XP bidirectionally:

```
Companion kills enemy
  → Companion gets full XP (tier-scaled)
  → XP passed UP to party leader (player)
  → Player gets full XP (tier-scaled again from their perspective)

Player kills enemy
  → Player gets full XP
  → XP passed DOWN to all player-led companions within 10 cells
  → Each companion gets full XP (tier-scaled from their perspective)
```

From `Experience.cs` lines 75-96:
```csharp
// Pass UP to party leader
GameObject partyLeader = ParentObject.PartyLeader;
if (partyLeader != null)
    partyLeader.AwardXP(E.Amount, E.Tier, ...PassedUpFrom: ParentObject);

// Pass DOWN to nearby companions (player only)
if (ParentObject.IsPlayer())
    foreach (companion in zone.FastSquareSearch(x, y, 10, "Brain"))
        if (companion.IsPlayerLed())
            companion.AwardXP(E.Amount, E.Tier, ...PassedDownFrom: ParentObject);
```

The `PassedUpFrom` / `PassedDownFrom` fields prevent infinite loops.

---

## 9. Wander Mode XP System

In Wander mode, kill XP is completely disabled:

```csharp
// WanderSystem.cs
public override bool HandleEvent(AwardingXPEvent E)
{
    if (WanderEnabled() && E.Kill != null)
    {
        E.Amount = 0;
        return false;  // blocks kill XP
    }
}
```

Instead, XP is awarded through **WXU** (Wander XP Units):

```csharp
public static int WXU
{
    get
    {
        int level = The.Player.Level;
        int xpToNext = GetXPForLevel(level + 1) - GetXPForLevel(level);
        int divisor = level <= 2 ? 2 : level <= 5 ? 3 : level <= 10 ? 5
                    : level <= 20 ? 6 : level <= 30 ? 8 : 10;
        return Math.Max(10, (int)Math.Round(xpToNext / divisor / 10.0) * 10);
    }
}
```

WXU sources: Quest completion (2 WXU), Secret revealed (1 WXU), First water ritual (1 WXU).

---

## 10. XP Blockers and Modifiers

| Mechanism | Effect |
|-----------|--------|
| `NoXPGain` part | Blocks ALL XP gain (returns false from AwardingXPEvent) |
| `"NoXPGain"` tag/property | Silently skips XP processing in Experience part |
| `"NoXP"` tag on victim | Prevents kill XP from being awarded |
| `XPMul` game state | Global multiplier (default 1.0, debug/wish only) |
| Tier scaling | Reduces kill XP based on level difference |
| Wander mode | Disables kill XP, substitutes WXU |

---

## 11. Level-Up: XP Thresholds

```csharp
// Leveler.cs line 98
public static int GetXPForLevel(int L)
{
    if (L <= 1) return 0;
    return (int)(Math.Floor(Math.Pow(L, 3.0) * 15.0) + 100.0);
}
```

**Formula: `XP(L) = floor(L³ × 15) + 100`**

| Level | Total XP Required | XP to Next Level |
|-------|-------------------|-----------------|
| 1 | 0 | 220 |
| 2 | 220 | 285 |
| 3 | 505 | 430 |
| 4 | 1,060 | 775 |
| 5 | 1,975 | 1,210 |
| 6 | 3,340 | 1,755 |
| 7 | 5,245 | 2,420 |
| 8 | 7,780 | 3,215 |
| 9 | 10,935 | 4,165 |
| 10 | 15,100 | 5,275 |
| 15 | 50,725 | 11,390 |
| 20 | 120,100 | 21,215 |
| 25 | 234,475 | 35,410 |
| 30 | 405,100 | 54,815 |
| 35 | 643,225 | 80,285 |
| 40 | 960,100 | 112,665 |

---

## 12. Level-Up: The Leveler Part

The `Leveler` part handles the `AwardedXPEvent` — after XP is awarded, it checks if the threshold for the next level has been crossed:

```csharp
// Leveler.cs line 30
public override bool HandleEvent(AwardedXPEvent E)
{
    int newValue = E.Actor.Stat("XP");
    while (valuePassed(E.AmountBefore, newValue, GetXPForLevel(ParentObject.Stat("Level") + 1))
           && LevelUp(E.Kill, E.InfluencedBy, E.ZoneID))
    { }  // Loop handles multi-level jumps
    return base.HandleEvent(E);
}
```

The `while` loop ensures that if a single XP award crosses multiple thresholds (e.g., a massive quest reward), the player levels up multiple times.

---

## 13. Level-Up Rewards Breakdown

```csharp
// Leveler.LevelUp() lines 244-270
int num = ParentObject.Stat("Level") + 1;  // new level

GetEntryDice(out BaseHPGain, out BaseSPGain, out BaseMPGain);

bool isMutant = ParentObject.IsMutant();
int HitPoints       = RollHP(BaseHPGain);      // dice + Toughness mod, min 1
int SkillPoints     = RollSP(BaseSPGain);       // dice + (Int-10)*4
int MutationPoints  = isMutant ? RollMP(BaseMPGain) : 0;  // 1 for mutants, 0 for True Kin
int AttributePoints = (num % 3 == 0 && num % 6 != 0) ? 1 : 0;  // every 3rd, not 6th
int AttributeBonus  = (num % 3 == 0 && num % 6 == 0) ? 1 : 0;  // every 6th: +1 ALL stats
int RapidAdvancement = (isMutant && (num + 5) % 10 == 0 && !ParentObject.IsEsper()) ? 3 : 0;

// All values can be modified by GetLevelUpPointsEvent
```

### Summary Table

| Reward | Default | Formula | Who Gets It |
|--------|---------|---------|-------------|
| **HP** | "1-4" | Roll + Toughness modifier (min 1) | Everyone |
| **SP** | "50" | Roll + (BaseIntelligence - 10) × 4 | Everyone |
| **MP** | "1" | Roll | Mutants only |
| **AP** | 1 | Every 3rd level (not divisible by 6) | Everyone |
| **+1 All Stats** | 1 | Every 6th level (6, 12, 18, 24, 30...) | Everyone |
| **Rapid Advancement** | 3 ranks | Every 10th level starting at 5 (5, 15, 25...) | Mutants only (not Espers) |

### Level-by-Level Reward Schedule (first 30 levels)

| Level | HP | SP | MP | AP | All Stats +1 | Rapid Adv |
|-------|----|----|----|----|-------------|-----------|
| 2 | Yes | Yes | M | — | — | — |
| 3 | Yes | Yes | M | 1 AP | — | — |
| 4 | Yes | Yes | M | — | — | — |
| 5 | Yes | Yes | M | — | — | **3 ranks** |
| 6 | Yes | Yes | M | — | **+1 all** | — |
| 7-8 | Yes | Yes | M | — | — | — |
| 9 | Yes | Yes | M | 1 AP | — | — |
| 10-11 | Yes | Yes | M | — | — | — |
| 12 | Yes | Yes | M | — | **+1 all** | — |
| 15 | Yes | Yes | M | 1 AP | — | **3 ranks** |
| 18 | Yes | Yes | M | — | **+1 all** | — |
| 21 | Yes | Yes | M | 1 AP | — | — |
| 24 | Yes | Yes | M | — | **+1 all** | — |
| 25 | Yes | Yes | M | — | — | **3 ranks** |
| 27 | Yes | Yes | M | 1 AP | — | — |
| 30 | Yes | Yes | M | — | **+1 all** | — |

(M = Mutants only)

---

## 14. Hitpoints Per Level

```csharp
public int RollHP(string BaseHPGain)
{
    return Math.Max(Stat.RollLevelupChoice(BaseHPGain) + ParentObject.StatMod("Toughness"), 1);
}
```

Default `BaseHPGain` = "1-4" (roll 1 to 4, uniform). Toughness modifier added (can be negative). Minimum 1 HP per level.

**Retroactive Toughness bonus**: When Toughness changes, HP is retroactively adjusted:
```csharp
// Leveler.HandleEvent(StatChangeEvent) for Toughness
int hpChange = (newToughness - oldToughness) + (newToughMod - oldToughMod) * (Level - 1);
stat.BaseValue += hpChange;
```

This means raising Toughness at level 20 retroactively adds `(newMod - oldMod) × 19` HP.

---

## 15. Skill Points (SP) System

### SP Gain Per Level

```csharp
public int RollSP(string BaseSPGain)
{
    int num = Stat.RollLevelupChoice(BaseSPGain);     // Default "50" = flat 50
    num += (ParentObject.BaseStat("Intelligence") - 10) * 4;
    return GetLevelUpSkillPointsEvent.GetFor(ParentObject, num);
}
```

**Formula: SP per level = 50 + (BaseIntelligence - 10) × 4**

| Intelligence | SP Per Level |
|-------------|-------------|
| 10 | 50 |
| 14 | 66 |
| 16 | 74 |
| 18 | 82 |
| 20 | 90 |
| 24 | 106 |

**Retroactive Intelligence bonus**: When base Intelligence increases beyond previous peak:
```csharp
int bonusSP = (newInt - peakInt) * 4 * (Level - 1);
```

At level 20 with Intelligence going from 16 to 18: `(18 - 16) × 4 × 19 = 152` bonus SP retroactively.

### SP Spending

SP is spent by incrementing `Penalty` on the SP stat (BaseValue is never reduced):
```csharp
GO.GetStat("SP").Penalty += cost;
```

This preserves the total lifetime earned while tracking spent points.

---

## 16. Skill Tree Structure

Skills use a **two-tier hierarchy**: **Skill Categories** contain **Powers** (individual abilities).

### Class Hierarchy

```
IPartEntry                    (base: Name, Class, Attribute, Cost, Flags)
  └── IBaseSkillEntry         (adds: Tile, Description, Generic BaseSkill ref)
       ├── SkillEntry         (= category: has PowerList, Powers dict, Initiatory flag)
       └── PowerEntry         (= individual skill: Minimum, Requires, Exclusion, ParentSkill)
```

### Key Architectural Insight

**Skills ARE Parts.** `HasSkill(name)` literally calls `HasPart(name)`. Each skill class in `XRL.World.Parts.Skill` is a full `IPart` subclass added to the entity's part list. The class name IS the skill identifier (e.g., `"Axe_Dismember"` → `XRL.World.Parts.Skill.Axe_Dismember`).

The `Skills` container part maintains a `List<BaseSkill>` for bookkeeping, but each skill is independently registered as a Part.

**Skills are binary** — you either have them or you don't. There is no skill leveling, ranks, or proficiency. Individual skills may reference character level or stats for their effects, but the skill itself doesn't "level up."

---

## 17. Skill Purchase Mechanics

### From Skills & Powers Screen (player UI)

```
1. Check if already owned
2. Check if initiatory (cannot buy directly)
3. Check SP >= cost
4. Check attribute requirements (PowerEntryRequirement)
5. Check prerequisite skills (Requires field)
6. Check exclusion skills (Exclusion field)
7. Confirmation popup
8. Instantiate via reflection: Activator.CreateInstance(type)
9. Skills.AddSkill(newSkill)
10. SP.Penalty += cost
```

### Attribute Requirements

Each power can have `Attribute` (e.g., "Agility") and `Minimum` (e.g., "17"). Pipe-delimited for OR alternatives:
```
Attribute="Strength|Agility" Minimum="17|19"
// Needs Strength >= 17 OR Agility >= 19
```

### Prerequisites (`Requires` field)

Comma-separated list of skill classes that must be owned first.

### Exclusions (`Exclusion` field)

Comma-separated list of skills that prevent purchase (mutually exclusive skills).

### Cost-0 Powers

Powers with `Cost = 0` are **auto-granted** when you purchase their parent category skill.

### Other Acquisition Methods

| Method | Notes |
|--------|-------|
| **Water Ritual** | Trade faction reputation to learn from NPCs |
| **Training Books** | Learn by reading |
| **Cybernetic Skillsofts** | Implant grants skills (True Kin) |
| **Wish system** | Debug command |
| **Auto-inclusion** | Cost-0 powers with parent purchase |

---

## 18. Skill Implementation Patterns

### Pattern A: Passive Stat Modifier
```csharp
// e.g., Acrobatics_Dodge
public override bool AddSkill(GameObject GO)
{
    base.StatShifter.SetStatShift("DV", 2);
    return base.AddSkill(GO);
}
public override bool RemoveSkill(GameObject GO)
{
    base.StatShifter.RemoveStatShifts();
    return base.RemoveSkill(GO);
}
```

### Pattern B: Activated Ability
```csharp
// e.g., Axe_Dismember, Tactics_Charge
public Guid ActivatedAbilityID;

public override bool AddSkill(GameObject GO)
{
    ActivatedAbilityID = AddMyActivatedAbility("Dismember", "CommandDismember", "Skills", ...);
    return true;
}
```

### Pattern C: Event Handler (passive proc)
```csharp
// e.g., Axe_Dismember's passive 3% dismember chance
public override void Register(GameObject Object, IEventRegistrar Registrar)
{
    Registrar.Register("AttackerAfterDamage");
}
public override bool FireEvent(Event E)
{
    if (E.ID == "AttackerAfterDamage") { /* 3% dismember chance */ }
}
```

### Pattern D: Empty Category
```csharp
// e.g., Axe, Survival — just a container
public class Axe : BaseSkill
{
    public override int Priority => int.MinValue;
}
```

### Pattern E: Initiatory Skill
Cannot be bought with SP. Must be learned through Water Ritual or special means. Extends `BaseInitiatorySkill`.

---

## 19. Complete Skill Catalog

### Combat Skills

**Axe**: Cleave, Dismember, Berserk, Decapitate, Expertise, HookAndDrag

**Long Blades**: DuelingStance, Swipe, Lunge, Proficiency, Deathblow, ImprovedAggressiveStance, ImprovedDefensiveStance, ImprovedDuelistStance

**Short Blades**: Jab, Shank, Expertise, Hobble, Puncture, Bloodletter, Rejoinder, PointedCircle

**Cudgel**: Bludgeon, ChargingStrike, Conk, Backswing, Slam, SmashUp, Expertise, Hammer, ShatteringBlows

**Pistol**: SteadyHands, DeadShot, DisarmingShot, SlingAndRun, Akimbo, EmptyTheClips, FastestGun, WeakSpotter

**Rifle**: SteadyHands, DrawABead, SureFire, BeaconFire, DisorientingFire, FlatteningFire, SuppressiveFire, WoundingFire, OneShot

**Heavy Weapons**: Sweep, Tank, StrappingShoulders

**Shield**: Block, DeftBlocking, SwiftBlocking, StaggeringBlock, ShieldWall, Slam

**Single Weapon Fighting**: WeaponExpertise, PenetratingStrikes, OpportuneAttacks, WeaponMastery

**Multiweapon Fighting**: Proficiency, Expertise, Flurry, Mastery

### Non-Combat Skills

**Acrobatics**: Dodge, Jump, SwiftReflexes, Tumble

**Tactics**: Charge, Juke, Run, Hurdle, Throwing, Camouflage, UrbanCamouflage, Kickback, DeathFromAbove

**Cooking & Gathering**: Butchery, Harvestry, MealPreparation, Spicer, CarbideChef

**Tinkering**: Tinker1, Tinker2, Tinker3, Repair, Disassemble, ReverseEngineer, DeployTurret, Scavenger, GadgetInspector, LayMine

**Persuasion**: Intimidate, Berate, MenacingStare, InspiringPresence, Proselytize, RebukeRobot, SnakeOiler

**Physic**: StaunchWounds, Nostrums, Apothecary, AmputateLimb

**Survival**: Camp, Trailblazer, + terrain specializations (Desert, Jungle, Mountains, Plains, Rivers, Ruins, SaltDesert, Saltmarsh)

**Customs**: Tactful, Sharer, TrashDivining

**Discipline**: IronMind, Lionheart, Conatus, FastingWay, MindOverBody, Meditate

**Endurance**: Swimming, Calloused, Weathered, PoisonTolerance, Juicer, ShakeItOff, Longstrider

### Special / Initiatory

**Tenfold Path**: Bin, Hod, Hok, Ket, Khu, Ret, Sed, Tza, Vur, Yis (learned via Water Ritual)

**Nonlinearity**: Tomorrowful

### Creature-Specific
Snapjaw_Howl, Smash_Floor, Submersion

---

## 20. Mutation Points (MP) System

MP is stored as a `Statistic` keyed `"MP"`:

```csharp
// GameObject.cs
public bool GainMP(int amount)
{
    Statistics["MP"].BaseValue += amount;
    FireEvent(Event.New("GainedMP", "Amount", amount));
}

public bool UseMP(int amount, string context = "default")
{
    Statistics["MP"].Penalty += amount;  // Spent via Penalty, not BaseValue reduction
    FireEvent(Event.New("UsedMP", "Amount", amount, "Context", context));
}
```

**MP per level**: Default 1 (from `GenotypeEntry.BaseMPGain`). Only Mutants get MP; True Kin get 0.

---

## 21. Mutation Advancement

### Cost: 1 MP per rank (hardcoded)

From `StatusScreen.ShowMutationPopup()`:
```csharp
if (GO.Stat("MP") >= 1)
{
    GO.GetPart<Mutations>().LevelMutation(Mutation, Mutation.BaseLevel + 1);
    GO.UseMP(1);
}
```

### Level Calculation (`BaseMutation.CalcLevel()`)

The effective level stacks multiple bonuses on top of `BaseLevel`:

```
1. BaseLevel (raw invested rank)
2. + Stat modifier (from mutation's associated attribute, e.g., Ego for mental)
3. + AllMutationLevelModifier (global modifier on entity)
4. + Category modifier (e.g., PhysicalMutationLevelModifier)
5. + AdrenalLevelModifier (from Adrenal Control, physical only)
6. + RapidLevel bonus (from rapid advancement)
7. + MutationMods (from cooking, tonics, equipment)
8. Floor of 1 (minimum effective level)
9. Cap enforcement (see section 22)
```

### The ChangeLevel Hook

Each specific mutation overrides `ChangeLevel(int NewLevel)` to react to level changes. At level 15, an achievement unlocks.

### Mutation Level Scaling Examples

| Mutation | Scaling with Level |
|----------|-------------------|
| **Pyrokinesis** | Damage: Level×d3, Level×d4, Level×d6 across 3 rounds |
| **Heightened Strength** | +Str: 2 + (Level-1)/2. Daze: 13 + 2×Level % |
| **Regeneration** | Healing: 10% + 10%×Level. Limb regrowth: Level×10% |
| **Adrenal Control** | Quickness: 9 + Level. All physical mutations: +Level/3+1 |

---

## 22. Mutation Cap and Level Scaling

Mutations have a **level-based ceiling** preventing over-investment at low character levels:

```csharp
public static int GetMutationCapForLevel(int level)
{
    return level / 2 + 1;
}
```

**Formula: MutationCap = CharacterLevel / 2 + 1**

| Character Level | Mutation Cap |
|----------------|-------------|
| 1 | 1 |
| 2-3 | 2 |
| 4-5 | 3 |
| 6-7 | 4 |
| 8-9 | 5 |
| 10-11 | 6 |
| 12-13 | 7 |
| 14-15 | 8 |
| 16-17 | 9 |
| 18+ | 10 (default max) |

### Maximum Mutation Level

Default max level is **10**, configurable per mutation in Mutations.xml via `MaxLevel` attribute.

### Can Advance Check

```csharp
public bool CanIncreaseLevel()
{
    return CanLevel() && BaseLevel < GetMaxLevel() && Level < GetMutationCap();
}
```

---

## 23. Buying New Mutations

### Cost: 4 MP

```csharp
// MutationsAPI.BuyRandomMutation()
public static bool BuyRandomMutation(GameObject Object, int Cost = 4, ...)
{
    if (Object.Stat("MP") < Cost) return false;
    // ... selection UI ...
    Object.UseMP(Cost, "BuyNew");
}
```

### Selection Process

1. Get mutation pool via `Mutations.GetMutatePool()` (filtered: no defects, no already-owned, no excluded)
2. Shuffle with seeded random (deterministic per game state)
3. Select **3 mutations** (configurable via `GlobalConfig "RandomBuyMutationCount"`)
4. Prefers mutations with Cost >= 2 (favors higher-cost mutations)
5. For **Chimeras**: one choice includes "grow a new body part"
6. Player picks one, added via `Mutations.AddMutation()`

### IrritableGenome (defect)

Forces random spending: when you spend MP (except for buying new mutations via "BuyNew" context), it tracks the amount. When you next gain MP, it randomly spends that many MP. When buying new mutations, you get a random one instead of a choice of 3.

---

## 24. Rapid Advancement

### When It Triggers

```csharp
int RapidAdvancement = (isMutant && (level + 5) % 10 == 0 && !isEsper) ? 3 : 0;
```

- Must be a **Mutant** (not True Kin)
- Must **NOT** be an Esper
- Triggers at levels **5, 15, 25, 35, 45...**
- Grants **3 rapid levels** to a chosen physical mutation

### How It Works

```csharp
// Leveler.RapidAdvancement()
1. Sync mutation levels
2. If player has >= 4 MP, offer to buy a new mutation first
3. List all physical mutations that CanLevel()
4. Player chooses which physical mutation to advance
5. That mutation gains 3 rapid levels via BaseMutation.RapidLevel()
```

Rapid levels are stored as IntProperties (`"RapidLevel_{MutationName}"`) and added permanently to the effective level calculation. They are **physical-only and non-Esper only**.

---

## 25. Chimera vs Esper Paths

Both are `BaseMutation` parts (morphotypes) with `CanLevel() = false`.

### Esper
- Can only manifest **mental** mutations
- Does **NOT** get Rapid Advancement (explicitly excluded)
- Gets bonus in psionic Sifrah games
- All mental mutations contribute to **Psychic Glimmer** (attracts extradimensional attention)

### Chimera
- Can only manifest **physical** mutations
- **DOES** get Rapid Advancement (physical mutations + non-Esper = qualifies)
- When buying a new mutation, one choice includes growing a new body part
- Favors physical mutation build with rapid advancement synergy

### Filtering

```csharp
// MutationsAPI.IsNewMutationValidFor()
if (go.HasTagOrProperty("Esper") && !entry.Mutation.GetMutationType().Contains("Mental"))
    return false;
if (go.HasTagOrProperty("Chimera") && !entry.Mutation.GetMutationType().Contains("Physical"))
    return false;
```

---

## 26. Mutation Defects

Defects are mutations where `MutationEntry.Defect = true`:

- Most defects return `CanLevel() => false`
- Display with `(D)` annotation in red
- **Only one defect normally allowed** (unless `allowMultipleDefects` option)
- Give **negative point costs** at character creation (taking a defect gives extra MP to spend)
- Excluded from `GetMutatePool()` by default

### Notable Defects

| Defect | Effect | Can Level? |
|--------|--------|-----------|
| **Albino** | 1/5 healing rate in daylight | No |
| **Hemophilia** | Increased bleeding susceptibility | No |
| **IrritableGenome** | Forces random MP spending | No |
| **UnstableGenome** | 33% chance per level-up to manifest a latent mutation; loses one charge each trigger | Shows level but no |

---

## 27. Attribute Points

### Every 3rd Level (not divisible by 6): 1 Attribute Point

```csharp
int AttributePoints = (level % 3 == 0 && level % 6 != 0) ? 1 : 0;
// Levels 3, 9, 15, 21, 27, 33...
```

AP is stored in the `"AP"` stat. Players spend it in the character sheet to raise any one attribute by 1.

### Every 6th Level: +1 to ALL Attributes

```csharp
int AttributeBonus = (level % 3 == 0 && level % 6 == 0) ? 1 : 0;
// Levels 6, 12, 18, 24, 30, 36...
```

This automatically adds +1 to Strength, Intelligence, Willpower, Agility, Toughness, and Ego.

### Combined Schedule

| Level | Attribute Reward |
|-------|-----------------|
| 3 | 1 AP (choose one stat) |
| 6 | +1 to ALL six stats |
| 9 | 1 AP |
| 12 | +1 ALL |
| 15 | 1 AP |
| 18 | +1 ALL |
| 21 | 1 AP |
| 24 | +1 ALL |
| 27 | 1 AP |
| 30 | +1 ALL |

---

## 28. Creature Level and Tier

### Tier = Level / 5

Integer division. Used for tier scaling of XP and effect initialization (`ITierInitialized`).

| Level Range | Tier |
|-------------|------|
| 1-4 | 0 |
| 5-9 | 1 |
| 10-14 | 2 |
| 15-19 | 3 |
| 20-24 | 4 |
| 25-29 | 5 |
| 30-34 | 6 |
| 35-39 | 7 |
| 40+ | 8+ |

### HeroMaker XP Values

`HeroMaker` sets XPValue on dynamically generated creatures:
- **Heroes**: `Level/2 × 200` XP
- **Minions**: `Level/2 × 100` XP

### NPC Auto-Skill Spending

NPCs automatically spend SP on available skills when leveled up, picking randomly from affordable, requirement-meeting powers.

---

## 29. Genotype Differences

### True Kin vs Mutant

| Aspect | True Kin | Mutant |
|--------|----------|--------|
| **MP per level** | 0 | 1 |
| **Rapid Advancement** | No | Yes (non-Espers) |
| **Mutations** | None (can't gain) | Start with chosen mutations |
| **Cybernetics** | Yes (implant slots) | No |
| **Tonic reaction** | Normal | May trigger allergy |
| **Base HP dice** | Genotype-specific | Genotype-specific |
| **Base SP dice** | Genotype-specific | Genotype-specific |

### Level-Up Dice Sources

The dice strings for HP, SP, and MP are sourced with priority:
1. **SubtypeEntry** (Caste/Calling) — highest priority
2. **GenotypeEntry** (True Kin/Mutant) — fallback
3. **Defaults** — `HP: "1-4"`, `SP: "50"`, `MP: "1"`

```csharp
// Leveler.GetEntryDice()
GenotypeEntry geno = GenotypeFactory.RequireGenotypeEntry(ParentObject.GetGenotype());
SubtypeEntry sub = SubtypeFactory.GetSubtypeEntry(ParentObject.GetSubtype());

BaseHPGain = sub?.BaseHPGain ?? geno.BaseHPGain ?? "1-4";
BaseSPGain = sub?.BaseSPGain ?? geno.BaseSPGain ?? "50";
BaseMPGain = sub?.BaseMPGain ?? geno.BaseMPGain ?? "1";
```

---

## 30. Complete XP Source Reference

| Source | Amount/Formula | Tier Scaled? | Notes |
|--------|---------------|-------------|-------|
| **Kill** | Victim's `XPValue` stat | Yes (Level/5) | Consumed on death |
| **Hero Kill** | Level/2 × 200 | Yes | HeroMaker-generated |
| **Minion Kill** | Level/2 × 100 | Yes | HeroMaker-generated |
| **Quest Step** | Per-step from Quests.xml | No | — |
| **Location Discovery** | Per-location Value | No | — |
| **Golem Quest** | 40,000 | No | Flat |
| **Reclamation** | XP to level 37 | No | Gap fill |
| **Waking Dream** | 15,000 | No | Pleasant wake only |
| **Cooking Effect** | 1,000 | No | ProceduralCookingEffectUnit_XP |
| **Book Donation** | bookValue² / 25 | No | Per book to Librarian |
| **Resheph Secrets** | 250 → 40,000 escalating | No | Per secret shared |
| **Baetyl Offering** | RewardAmt × (50+Level) | No | Altar offering |
| **Conversation** | XML-defined | No | Arbitrary |
| **Reverse Engineering** | (T²-S) × (C+D) × [1 or 1.5 or 5] | No | Sifrah |
| **Examine Artifact** | (T²-S) × (C+D) × [1 or 3] | No | Sifrah |
| **Hacking** | (T²-S) × (C+D) × [1 or 2] | No | Sifrah |
| **Item Modding** | (T²-S) × (C+D) × [2 or 10] | No | Sifrah |
| **Repairing** | (T²-S) × (C+D) × [1 or 3] | No | Sifrah |
| **Disarming** | (T²-S) × D × [1 or 3] | No | Sifrah |
| **Social Sifrahs** | (T²-S) × D × [1/3 or 1] | No | Water Ritual, Beguile, etc. |
| **Wander Mode WXU** | Level-scaled fraction | No | Replaces kill XP |

### Key Formulas Summary

```
XP for Level L:        floor(L³ × 15) + 100
Tier:                  Level / 5 (integer division)
HP per level:          Roll("1-4") + ToughnessMod, min 1
SP per level:          50 + (Intelligence - 10) × 4
MP per level:          1 (Mutants only)
AP schedule:           1 AP at levels 3, 9, 15, 21, 27...
All Stats +1:          At levels 6, 12, 18, 24, 30...
Rapid Advancement:     3 ranks at levels 5, 15, 25, 35... (Mutant non-Esper)
Mutation Cap:          CharacterLevel / 2 + 1
Cost to advance 1 rank: 1 MP
Cost for new mutation:  4 MP
Stat Modifier:         floor((Score - 16) / 2)
Retroactive Toughness: (newMod - oldMod) × (Level - 1) HP
Retroactive Intel:     (newInt - peakInt) × 4 × (Level - 1) SP
```
