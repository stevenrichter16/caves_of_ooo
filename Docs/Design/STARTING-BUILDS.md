# Starting builds — four ways to begin

> **Status: DESIGN PROPOSAL, nothing implemented.** Verified against `main`
> at `158f2229` (2026-09-30). Numbers come from running the real combat,
> skill, AI and gas code outside Unity with scripted player behaviour
> (§2, §9). The Unity editor has not run any of this.
>
> **User request:** "explore a handful of starting builds a player can pick
> on a new game … attributes, weapons, skills, spells, abilities, and craft;
> craft 4 starting builds that vary how people can start playing."
>
> **Why it matters:** today every character starts identically (same stats,
> same six spells, fists). Build identity is something a new player feels in
> the first ten minutes; "everyone starts the same" was one of the gaps in
> the direction note that preceded this request.

---

## 1. The four, in one table

| | **A. Duelist** | **B. Breaker** | **C. Stormcaller** | **D. Bombardier** |
|---|---|---|---|---|
| One-line fantasy | Let them swing, punish the miss | Hit once, stun, shove | Read the target's state, pick the element | Win before the fight starts |
| Str / Agi / Tou / Ego | 16 / **22** / 14 / 18 | **20** / 16 / 16 / 18 | 16 / 20 / 16 / 18 | 16 / 18 / 14 / **22** |
| Starting DV | 11 | 7 | 9 | 8 |
| Weapon | Dagger | Cudgel | Dagger | Dagger |
| Defence kit | Buckler, Cloak | Buckler | Cloak | Leather Cap, Cloak |
| Skills (rows) | Short Blades root, Puncture, Bloodletter, Rejoinder, Shank, Flurry | Cudgel root, Expertise, Conk, Slam, Ground Pound, Shattering Blows | Kindle, Arc Bolt, Jet Blast, Ground Surge, Calm, Spellcraft root | Drench Lob, Cold Snap, Calm |
| Pack | 2 Healing, 2 Dried Meat | 2 Healing, 2 Dried Meat | 2 Healing, 2 Dried Meat | **Poison Gas Grenade, Lightning Tonic, Frost Tonic**, 2 Healing, 2 Dried Meat |
| Wins at | Defence, attrition, counters | Opening burst, crowd control | Free damage while they approach | One-shot consumable power |
| Loses at | Ambush by a pack | Choosing who gets stunned (see bug F8) | Being caught at melee range | Running out; ambush |
| First thing to buy | Spear (41 drams) | Mace (46 drams) | A grimoire | Poison grenades (31 drams at Ego 22) |

Everyone also keeps today's 50 drams, 50 Ink and farming starter kit. The
"Classic" start stays available and unchanged for tests and DevMode (§8).

---

## 2. Verification sweep — what the code actually does

This section is the reason the builds look the way they do. Each row is
something I assumed or the earlier docs implied, and what I found.

| # | Premise | Actual | Consequence for the design |
|---|---|---|---|
| F1 | The starting dagger is the player's weapon | It is **added to the pack and never equipped** (`NewGameLoadout.cs` uses `AddObject`; `InventorySystem.AutoEquip` has no caller on this path). Fists do 2.1 per swing vs a Scrabbler, a dagger 3.9. In the glade fight the dagger lifts the win rate from 68% to 80% (§4) | Every build equips its weapon at start. The Classic start should do the same (separate bug) |
| F2 | Equipment order is cosmetic | **The primary hand swings; a buckler or torch in it makes your fist the primary attack and the weapon a 15% off-hand** (`CombatSystem.GatherMeleeWeapons` falls through to the natural weapon for a non-weapon item). Measured: Cudgel at Str 22 does 13.4/swing, but 4.8 if the buckler was equipped first. Dagger at Str 16: 3.2 vs 2.0 | The start code must equip the weapon first. Players hit this silently today |
| F3 | Attributes decide a build | Weapon decides far more. Damage per swing vs a Scrabbler: fist 2.1, dagger 3.9, hatchet (Str 20) 8.2, cudgel (Str 18) 8.6, cudgel (Str 22) 13.4, mace (Str 20) 19.9. Strength adds penetrations on top of weapon penetration, so heavy weapons scale steeply | Weapon choice carries the main attack for B; A deliberately keeps the Str-capped dagger |
| F4 | Armor protects you | Body armor barely helps at the start: Leather Armor cuts an enemy's damage per swing by ~8%, Chain Mail ~9% (AV applies only to the body part hit, enemy penetration is high). **DV does the work**: Cloak+Buckler (DV +2) cuts enemy hit chance from 66% to 55% and damage per swing by 16% | A and C spend on DV, not armor. No build starts in body armor |
| F5 | Four attributes are all meaningful | **Intelligence and Willpower have no mechanical effect.** Toughness only affects bleed/stun recovery (±5% per step) and spore infection — **it gives no HP**. Ego affects trade price and Recruit. Strength: penetration, throw range, pickup. Agility: to-hit, DV, thrown accuracy. Modifier = `floor((score−16)/2)`, so only even scores change anything | Builds move points between Str, Agi, Tou and Ego (sum kept at 70) and leave Int/Will alone. Toughness is a "free" stat to lower, which is a design smell (§10) |
| F6 | Skills cost SP, so a start must spend SP | Every power is 1 SP but you start with 0 and gain 1 per level, so a start build must **grant** skills (`SkillsPart.AddSkill` bypasses SP, as `StartingSpellKit` does). Acrobatics (50–100 SP) and Persuasion (100 SP) are unbuyable at any level | Builds avoid those two trees. Budget rule: 6 skill rows, the same as today's 6 spells |
| F7 | Spell-power skills boost spells | Spellcraft, Empower and the element roots modify damage only through `SpellDamageHelpers.ApplySpellDamage`, which **only 5 projectile spells use** (Kindle, Quench, Ice Lance, Arc Bolt, Acid Spray). Ember Spit, Flaming Hands, Jet Blast, Ground Surge, Rime Grip and all rites skip it | C takes Spellcraft only because Kindle and Arc Bolt are on the list |
| F8 | Adjacent skills hit the creature you aim at | `Conk`, `Slam`, `Shank`, `Flurry`, `Backstab`, `Disarm`, `Hook and Drag`, `Rend Armor`, `Frostbind`, `Recruit`, `Tumble` pick **the first adjacent creature in scan order**, ignoring the chosen direction | In a 3-enemy fight A and B cannot choose their target. Fix before shipping A or B (§7) |
| **F9** | Freeze is one control among many | **Frozen creatures cannot act, and damage does not break it.** `Quench` (cooldown 6, range 5) on any target gives it Wet plus −150 J, and the target ends up Frozen on the same cast. Traced turn by turn: a JungleApe is Wet+Frozen after one Quench and never acts again. (Likely cause: the −150 J chill plus Wet's ×1.5 cold multiplier; observed, not traced to the line.) A Quench build wins 100% of all four scenarios with ≤1.4 HP lost | **C must not take Quench.** Separately, this is a balance bug (§7) |
| F10 | The starting kit has no cheap answer to a hard hitter | Rime Grip (starting spell, cooldown 35) freezes too. Today's start beats a lone JungleApe 82% of the time purely on that | Builds must each have their own answer to a hard hitter, or they are worse than today in that fight |
| F11 | Gas grenades are a minor item | I made them throwable in `Docs/DENSITY-PHASE-1.md` tranche 1. **Poison gas drops a 30-HP JungleApe to 13 in two turns** (sim, one gas tick per player turn). `ProvisionerStock` sells it at 45% for 49 drams at Ego 16. A start with one gas grenade plus two tonics wins 80–100% of every non-ambush fight | D is built on this. It is strong while stock lasts and needs a supply limit (§6, D) |
| F12 | Enemies are a speed bump | JungleApe averaged 12–13 per swing at ~84% vs a 40-HP, AV 0 player; one round did 29. Two Spread-lair guards plus an ape kill every melee start more than half the time | The lair is a "come back later" fight for any build. No start should try to pass it |
| F13 | Throwing is a verb | Throwing from inventory takes 4+n+d key presses (open pack, scroll, select, choose cell). No skill improves throwing. Throw accuracy is `1d(Agility) ≥ 3` | D works mechanically but has a UX tax (§7) |
| F14 | Followers are a build | `Recruit` costs 100 SP, needs a grant, hits followers with area spells, and follower assist is probably dead for a player leader (`Docs/FOLLOWERS.md` F.4 not started) | **Left out on purpose** (§5) |

---

## 3. Design rules

1. **Power parity, spent differently.** Today's start gives 6 skill rows
   (the six spells). Each build gets 6 skill rows. D takes 3 rows and a
   pack worth ~62 base value, because its power lives in items.
2. **Attributes sum to 70** across Str+Agi+Tou+Ego (today 18+18+18+16),
   in even steps, Int and Will untouched at 10.
3. **A kit is one loop.** Each build has one thing it does every fight and
   one thing it cannot do. No build is a stat stick.
4. **Every build has an answer to a hard hitter** (F10, F12) and a named
   weakness.
5. **Nothing from F6, F7, F9, F14.** No Acrobatics, no Persuasion, no
   Quench, no spell-power passives on spells that ignore them.
6. **One cheap first purchase** that visibly changes the build, so level 2
   has a goal.
7. **HP and the resource model are untouched.** 40 HP, cooldowns only, no
   mana. Anything else is a separate decision.

---

## 4. Evidence: per-swing and per-fight numbers

### Per swing (real `CombatSystem`, 1,000 swings per cell, enemy = Marlback Scrabbler)

| Setup | Hit % | Damage/swing |
|---|---:|---:|
| Fists (today) | 91 | 2.1 |
| Dagger, Str 18 (today, if equipped) | 90 | 3.9 |
| Dagger, Str 16 | 89 | 3.2 |
| Dagger, Str 16, **+Puncture** | 90 | 4.6 |
| Dagger, Str 16, Agi 22, +Puncture (A) | 98 | 5.0 |
| Spear, Str 16, Agi 22, +Puncture | 97 | **11.4** |
| Hatchet, Str 20 (with Axe skills) | 89 | 8.2 |
| Cudgel, Str 18 / Str 22 (no skills) | 94 / 95 | 8.6 / 13.4 |
| **Cudgel, Str 20, B's skills (B)** | 97 | **14.9** |
| Mace, Str 20 (with Cudgel skills) | 98 | 19.9 |
| Dagger, Str 16, **buckler equipped first** | 88 | 2.0 |
| Cudgel, Str 22, **buckler equipped first** | — | 4.8 |

A and C's dagger is deliberately weak. The **Spear is a 2.3× jump** over A's
dagger for 41 drams, which is the right first purchase and also a sign that
the shop's weapon ladder is steep (see §10).

### Whole fights (real turn loop and enemy AI, scripted player, 60 runs per cell, ±6 points)

Win % / death % / HP lost when won. No healing used. S1–S4: enemies start 6
cells away. S5–S6: enemies start 2 cells away (an ambush).

| Build | S1 glade: 2 Scrabbler + Gleaner | S2 lair: 2 Spider + Ape | S3 one Ape | S4 Sodden: Toad + 2 Frog | S5 glade, ambush | S6 lair, ambush |
|---|---|---|---|---|---|---|
| Today, dagger equipped | 80/20/25 | 3/97/32 | 82/18/15 | 100/0/7 | 67/33/26 | 0/100/— |
| Today, fists (as shipped) | 68/32/28 | 2/98/— | 75/25/14 | 100/0/8 | — | — |
| **A Duelist** | 73/27/21 | 22/78/30 | 80/20/16 | 100/0/6 | 73/27/21 | 20/80/29 |
| **B Breaker** (Str 20) | 90/10/18 | 37/63/22 | 100/0/0 | 100/0/8 | 90/10/18 | 58/42/23 |
| **C Stormcaller** | 75/25/26 | 48/52/23 | 100/0/0 | 100/0/5 | 83/17/20 | 5/95/29 |
| **D Bombardier** (3 throwables) | 92/5/9 | 85/15/8 | 93/7/1 | 100/0/4 | 75/25/18 | 0/100/— |
| D with 3 tonics, no gas grenade | 48/52/23 | 0/100/— | 5/95/24 | 100/0/9 | 2/98/32 | 0/100/— |
| *Exhibit: Quench build* | 100/0/1 | 100/0/0 | 100/0/0 | 100/0/1 | — | — |

Raw rows: `Docs/Design/StartingBuildsProbe/`.

**How to read it.** The table is a proxy, not a verdict. The player script
is simple (stand and cast, wait for the enemy to step adjacent, no
kiting, no retreat), so absolute numbers are meaningful mainly in
comparison. The point is the *shape*: all four sit near today's start in the
glade (S1) but each is strong where the others are weak. The ambush column
(S5/S6) is the honest one for C and D.

---

## 5. What I deliberately left out

| Option | Why not |
|---|---|
| Companion / Recruit build | F14. Recruit is 100 SP, area spells hurt followers, assist is probably dead for a player leader. Revisit after `FOLLOWERS.md` F.4 |
| Rites (grimoire-fed) | Each grimoire has 10 charges and nothing refills them (no production caller of `GrimoireChargePart.Refill`). A starting build would be a countdown |
| Acrobatics (Evasive Roll, Tumble, Vault) | 50–100 SP each by design. Granting it for free devalues the tree |
| Tinkering / schematics | Dev-only (`BitLocker` is granted only under `DevMode`). A product decision, not a build |
| Axe build for B | The Hatchet is a 37% fight against the ape (S3). The Cudgel is the right Strength weapon today; Axe is a good level-up branch (Whirlwind, Rend Armor) |
| Mace for B | 19.9 per swing at Str 20 is too strong for a free item. It is the 46-dram first purchase |
| Mutations | The system was deleted; nothing to grant |

---

## 6. The four builds

### A. The Duelist — "let them swing, punish the miss"

**Fantasy.** Quick, evasive, a knife held low. You do not outhit anyone; you
make their hits miss and make your hits count.

| Field | Value |
|---|---|
| Attributes | Str 16, **Agi 22**, Tou 14, Ego 18 (HP 40, Int/Will 10) |
| Derived | DV 11 (6 + Agi +3 + Cloak 1 + Buckler 1), hit chance vs Scrabbler 98% |
| Equip order | **Dagger first**, then Buckler, Cloak |
| Pack | 2 Healing Tonic, 2 Dried Meat |
| Skills | Short Blades root, **Puncture**, **Bloodletter**, **Rejoinder**, **Shank**, **Flurry** |
| Hotbar | Shank, Flurry (the other four are passive) |

**The loop.** High DV makes enemies miss (enemy hit chance 49% vs 66% today,
and their damage per swing drops 25%). Rejoinder turns a miss into a 60% free
counter. Bloodletter bleeds on half your hits. Shank gains +2 penetration
per negative status on the target, so it pays off against the bleeding
enemy it is already fighting. Flurry (cooldown 35) is three swings in one
action. Puncture lifts the dagger from 3.2 to 4.6 per swing.

**Numbers.** 5.0 damage/swing at 98%; 73% / 80% in the glade and vs a lone
ape; 22% in the lair, versus today's 3%.

**Weakness.** Ambush by a pack (S6: 20%). Low raw damage. Str 16 means no
heavy weapons.

**First purchase.** The **Spear** (Piercing, so every Short Blades skill
works): 11.4 per swing with Puncture, 41 drams at Ego 18. Skill picks from
level 2: Backstab, Disengage, Hobble.

**Depends on.** F8 (Shank and Flurry choose the first adjacent creature).

### B. The Breaker — "hit once, stun, shove"

**Fantasy.** A cudgel and the arm to swing it. You open a fight by ending
one enemy's turn and throwing another into a wall.

| Field | Value |
|---|---|
| Attributes | **Str 20**, Agi 16, Tou 16, Ego 18 |
| Derived | DV 7, hit chance ~95% (Cudgel penetration scales with Str, uncapped) |
| Equip order | **Cudgel first**, then Buckler |
| Pack | 2 Healing Tonic, 2 Dried Meat |
| Skills | Cudgel root (crit stuns), **Expertise** (+2 to-hit), **Conk**, **Slam**, **Ground Pound**, **Shattering Blows** |
| Hotbar | Conk, Slam, Ground Pound |

**The loop.** Conk (cooldown 10) is a guaranteed 4-turn stun, Slam (50)
pushes up to 3 cells and adds a damage roll per wall hit, Ground Pound (40)
stuns everything adjacent for a turn. A stunned enemy cannot act and has
DV −4. The cudgel does ~15 per swing at Str 20 with these skills (measured
14.9 vs a Scrabbler), which is about one swing per 15-HP raider.

**Numbers.** 90% in the glade, 100% vs a lone ape, 37% in the lair, 58% in
the lair ambush: the best melee start by a margin.

**Weakness.** No ranged answer. Buckler-only defence (DV 7). Cannot choose
who gets stunned (F8).

**Tuning lever.** This is the strongest opener (≈+10 over today). If it needs
trimming, drop Expertise or start at Str 18 (87/38/100/100/87/53, almost the
same). The **left-out skill is Bludgeon** (50% stun on every hit): with Conk it
looks like a stun-lock, so it is the level-2 purchase *after* the stun duration
is playtested.

**First purchase.** **Mace** (46 drams at Ego 18): 19.9 per swing at Str 20.
The Cudgel itself is not sold anywhere, which is why B is granted one.

### C. The Stormcaller — "read the target, pick the element"

**Fantasy.** A caster who treats each enemy as a puzzle: wet means shock,
dry means burn, and a long run-up means free damage.

| Field | Value |
|---|---|
| Attributes | Str 16, Agi 20, Tou 16, Ego 18 |
| Derived | DV 9, melee 3.3 per swing (weak on purpose) |
| Equip order | Dagger, Cloak |
| Pack | 2 Healing Tonic, 2 Dried Meat |
| Spells | **Kindle** (1d4 fire + ignition, cd 6, range 5), **Arc Bolt** (1d8 electric, Electrified, cd 7, range 5), **Jet Blast** (2 water, Wet, push 1, cd 20), **Ground Surge** (6 electric along a line, cd 30, range 4) |
| Other | **Calm** (pacify one target 50 turns, cd 20, range 6), **Spellcraft** root (+1 on the five projectile spells, so on Kindle and Arc Bolt) |

**The loop.** Open at range. A **Wet** target takes Arc Bolt (Wet doubles the
charge and adds a turn; Electrified also stuns for 2 turns). A **dry**
target takes Kindle (Wet blocks ignition for Ember Spit, Flame Jet and
Backdraft; I have not verified Kindle on a Wet target). Jet Blast makes a
dry enemy Wet and shoves it away; Ground Surge charges the wet line cells,
which shocks everyone standing in them. Calm is the escape: pacify the one
you cannot handle.

**Numbers.** 75% in the glade, 100% vs a lone ape (it walks five cells into
fire), 48% in the lair.

**Weakness.** Ambush. When the enemies start adjacent the caster does 3.3
per swing and takes the hits: lair ambush 5%, glade ambush 83% only because
the glade enemies are weak.

**Deliberately not Quench** (F9), not Spellcraft's Empower (the C1 variant
with Empower was 95/58/100/100/88/15, too strong), and not Rime Grip (the
other freeze).

**First purchase.** A grimoire (Arc Bolt or Kindle upgrades are in vault
loot; Ice Lance and Acid Spray are the same projectile family, so Spellcraft
helps them).

**Depends on.** F9 (fix Quench before shipping any caster build).

### D. The Bombardier — "win before the fight starts"

**Fantasy.** An apothecary who fights with what is in the pack. The skills are
set-up and escape; the damage is a grenade, a bolt of lightning in a bottle,
and a flask of frost.

| Field | Value |
|---|---|
| Attributes | Str 16, Agi 18, Tou 14, **Ego 22** |
| Derived | DV 8; trade performance 0.56 (vs 0.35 today): Poison grenade 31 drams, Healing Tonic 26, instead of 49 and 41 |
| Equip order | Dagger, Leather Cap, Cloak |
| Pack | **Poison Gas Grenade ×1, Lightning Tonic ×1, Frost Tonic ×1**, 2 Healing, 2 Dried Meat |
| Skills (3 rows) | **Drench Lob** (Wet burst, radius 2, range 6, cd 30), **Cold Snap** (Hobble everyone within 2, cd 30), **Calm** |
| Hotbar | Drench Lob, Cold Snap, Calm |

**The loop.** Open at range 3+: grenade into the group, then the Lightning
Tonic (Wet doubles its charge; Electrified stuns 2 turns) and the Frost Tonic
(Frozen enemies cannot act). The AI appears to have no hazard avoidance (a grep of
`Gameplay/AI` found none), so enemies stand in the cloud. Cold
Snap and Calm are what you press when the plan fails.

**Numbers (3 throwables).** 92% glade, 85% lair, 93% vs the ape, losing 1–9
HP. **Without the gas grenade** the same pack wins 48% / 0% / 5%, which is
why the grenade is the build. It is a **one-fight kit**: three items.

**Weakness.** Supply, ambush (S6: 0%), and the UX: a throw is 4+ key
presses (F13). Gas also reaches you.

**The economy is the build.** With Ego 22 a Poison grenade costs 31 drams,
the Alembic in Sill brews acid, shock and poison tonics from reagents that
cost 11 drams (17 at Ego 16), and sales fetch ~59% of value (35% today). The loop is "spend on
a fight, restock from the fight's loot".

**Open risk.** F11. A strong consumable at that price is easy to turn into
the default answer for everyone. Cap how much a Bombardier can carry
(weight 2 each, 150 lb cap is not a limit), or reprice the grenade, or both.

**First purchase.** More grenades.

---

## 7. Prerequisites and bugs found

Ordered by what blocks which build.

| # | Item | Blocks | Size |
|---|---|---|---|
| P1 | **Equip the starting weapon.** Classic start never equips the dagger (F1). Also the build applier must equip the weapon before any shield (F2) | all | S |
| P2 | **Quench / Frozen.** Quench gives Wet −150 J and (likely) the Wet ×1.5 multiplier freezes on the same cast; Frozen creatures never act and damage does not break it. Either raise the freeze threshold, make damage thaw, or cap freezes per target. Rime Grip has the same property at cooldown 35 | C, and any future caster | S–M, design call |
| P3 | **Adjacent skills pick the wrong target** (F8). Use the aimed cell like Flaming Hands does | A, B | S |
| P4 | **Grenade value** (F11): poison gas deals ~10 per turn to a 30-HP enemy. Decide price, stock and carry cap | D | S, design call |
| P5 | **Throw UX** (F13): a quick-throw key (last-used throwable, nearest-target cell) | D | M |
| P6 | **Equip-order trap for players.** Warn when a shield would displace the weapon, or auto-seat the weapon in the primary hand | all players | S |
| P7 | **Toughness gives nothing and Int/Will are dead** (F5). If attributes are to be a build choice, wire Toughness to HP and give Int/Will a consumer; otherwise the sheet reads like a dump-stat menu | all | M, design call |

P1 and P2 must land before this ships. P3 before A or B. P4 before D.

---

## 8. Implementation sketch (smallest blast radius first)

No title screen or character-creation UI exists. The only start prompt is
`BootMenuController`, which appears only if a save exists. The world is
generated before the player does.

1. **Data.** `Assets/Resources/Content/Data/Builds/*.json`, one file per
   build: `id`, `name`, `tagline`, `attributes {Strength, Agility, Toughness,
   Ego}`, `items [{blueprint, count, equip}]` (listed in equip order),
   `skills [className]` (listed in hotbar order). Classic stays code-defined.
2. **Applier.** `StartingBuildService.Apply(Entity player, StartingBuild b)`:
   set the four stat `BaseValue`s, then items (create, `AddObject`, equip in
   order), then `SkillsPart.AddSkill(class, "start-build:" + id)` in order
   so the hotbar binds 1…n. Grants never bypass the validation that matters:
   an unknown skill class or blueprint, or a failed equip, logs and aborts
   with a message instead of a half-built player.
3. **Seam.** In the player-setup lambda of `GameBootstrap` (`:300-344`),
   between `CreateEntity("Player")` (`:303`) and the `NewGameLoadout.Grant`
   branch (`:318`). DevMode and Classic keep their existing path.
4. **Selection UI.** A popup in the style of `PauseMenuUI` /
   `SkillsScreenUI`, shown before step 6 for a fresh game only. Four
   cards: name, tagline, attributes, kit, one weakness line.
5. **Persistence.** Store `StartingBuildId` as an entity property so
   dialogue, achievements or a later respec can read it.
6. **Diag (CLAUDE.md rule).** `build/Applied {id, attrs, skills, items}`,
   `build/Rejected {id, reason, field}` for each failed grant.

### Tests (RED first)

- Per build: attributes sum to 70; weapon equipped *and* primary; skill
  count matches; each skill registers an ability on the hotbar (≤ 10); every
  blueprint exists.
- **Counter-check for F2:** a build with the buckler listed first must be
  rejected or reordered, and a test must show the weapon, not the fist, is
  the primary attacker.
- Classic start unchanged (the existing `AlphaStripDebugTests` pin).
- Save/load keeps attributes, skills, hotbar order and the build id.
- **A self-auditing bench** (promote `StartingBuildsProbe/BuildFightSim.cs`
  into the repo): a table of builds × scenarios with a **band assertion**
  (for example, no build above 95% or below 40% in the glade, none at 100%
  with ≤1 HP lost in two scenarios, which is what the Quench exhibit shows).
  A run stamps its id and writes one diag record per cell, per the
  self-auditing-scenario rules.

---

## 9. Honesty bounds

**Can verify (script-observable, and done):** per-swing damage and hit
rates through the real `CombatSystem`; full-fight outcomes through the
real `TurnManager`, enemy AI, skills, effects and gas system; blueprint
and skill names; the equip-order and Frozen behaviours (traced turn by
turn); counts of what each attribute and skill actually does.

**Cannot verify (and did not):**

- Anything in the Unity editor. These runs used the editor-less runner
  (`Tools/EditModeRunner`, its stub `UnityEngine`) on `main` at `158f2229`.
- **Feel.** Whether Conk, Arc Bolt or a thrown grenade is fun to press.
- **Player skill.** The scripted player never kites, retreats, uses
  terrain, drinks tonics or reads the sidebar. A real player is better in
  some builds (C, D) and worse in others.
- **Gas.** One gas tick per player turn is an assumption about how often
  the real game ticks it.
- **Real glade placement.** The scenarios place enemies by hand, not as
  `ReferenceGladeBuilder` does; the Spread territory warning is not modelled.
- Win rates are 60 runs per cell, so ±6 points. Differences smaller than that
  are noise.
- The Qud comparison is from memory (the decompile is not available here):
  that Qud's character is chosen at the start and that Toughness drives HP
  are `[KNOWN]`, not checked.

---

## 10. Open decisions for the user

1. **Does Classic stay as a fifth option?** It preserves today's start and every
   test that pins it. I would keep it, labelled for returning players, and fix
   P1 in it.
2. **How is the freeze exploit fixed (P2)?** Frozen is currently the strongest
   control by a wide margin. This changes combat feel everywhere, so it needs
   your call, not mine.
3. **Breaker at Str 20 or Str 18?** Str 20 is the clear "Strength build" but
   it is the strongest opener.
4. **Should Toughness mean HP (P7)?** If yes, every build's Toughness choice
   becomes real and the "dump Toughness" smell goes away.
5. **The steep weapon ladder.** Dagger 3.9, Spear ~6–11, Mace ~20 per swing
   for 18–46 drams. That makes "first purchase" the biggest power spike in
   the early game. I did not touch it; it belongs in a loot/economy pass.
