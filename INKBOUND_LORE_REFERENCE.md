# Inkbound Lore Reference

Purpose: consolidate the lore that is explicitly documented in `/Users/steven/Setup Guide In-Editor Tutorial` into a single reference inside this repository.

This is a documentation import, not a gameplay integration. It captures what the Inkbound project says about its world, factions, tone, and implied setting structure.

## Source Material Reviewed

Primary sources:
- `/Users/steven/Setup Guide In-Editor Tutorial/FACTIONS.md`
- `/Users/steven/Setup Guide In-Editor Tutorial/HOW_FACTIONS_WORK.md`
- `/Users/steven/Setup Guide In-Editor Tutorial/NPC-FACTION-REFACTOR.md`
- `/Users/steven/Setup Guide In-Editor Tutorial/Assets/Ink/Resources/Factions/Inkbound.asset`
- `/Users/steven/Setup Guide In-Editor Tutorial/Assets/Ink/Resources/Factions/InkboundScribes.asset`
- `/Users/steven/Setup Guide In-Editor Tutorial/Assets/Ink/Resources/Factions/Inkguard.asset`
- `/Users/steven/Setup Guide In-Editor Tutorial/Assets/Ink/Resources/Factions/Goblin.asset`
- `/Users/steven/Setup Guide In-Editor Tutorial/Assets/Ink/Resources/Factions/Skeleton.asset`
- `/Users/steven/Setup Guide In-Editor Tutorial/Assets/Ink/Resources/Factions/Ghost.asset`
- `/Users/steven/Setup Guide In-Editor Tutorial/Assets/Ink/Resources/Factions/Demon.asset`
- `/Users/steven/Setup Guide In-Editor Tutorial/Assets/Ink/Resources/Factions/Slime.asset`
- `/Users/steven/Setup Guide In-Editor Tutorial/Assets/Ink/Resources/Factions/Snake.asset`
- `/Users/steven/Setup Guide In-Editor Tutorial/Assets/Ink/Resources/Dialogues/Dialogue_Inkbound_*.asset`
- `/Users/steven/Setup Guide In-Editor Tutorial/Assets/Ink/Resources/Dialogues/Dialogue_Inkguard_*.asset`
- `/Users/steven/Setup Guide In-Editor Tutorial/Assets/Ink/Resources/Dialogues/Dialogue_Merchant_Offer.asset`
- `/Users/steven/Setup Guide In-Editor Tutorial/Assets/Ink/Resources/Dialogues/Dialogue_Merchant_TurnIn.asset`

Interpretation rule used in this document:
- `Explicit`: directly stated in dialogue/assets/docs.
- `Inferred`: implied by rank names, economic policy, or system data rather than direct narrative text.

## High-Level Setting Pillars

These are the clearest recurring ideas in the Inkbound material.

### 1. Memory versus erasure
Explicit:
- The Inkbound define themselves as preservers of truth and memory.
- Their voice frames history as fragile, layered, and worth copying before time destroys it.

Implication:
- Writing, archives, and record-keeping are not bookkeeping details. They are civilizational survival tools.

### 2. Order versus breach
Explicit:
- The Inkguard talk about gates, rules, duty, survival, and “the Siege.”
- Their tone is military and defensive rather than expansionist.

Implication:
- The world has already endured at least one serious existential crisis, and the Inkguard are shaped by it.

### 3. Faction identity is stronger than species identity
Explicit from design docs:
- The project treats faction as a compositional identity that can apply to any entity.
- “Enemy” is a behavior/state, not a species or class.

Implication:
- The world is socially organized. Allegiance, oath, rank, and reputation matter more than simple monster taxonomy.

### 4. Economy reflects worldview
Explicit in faction assets:
- Every faction has a distinct economic policy plus produced, desired, and banned goods.

Implication:
- Trade is not generic. It expresses culture, taboos, and material priorities.

## Major Factions

## Inkbound

### Core identity
Explicit:
- A scholarly, archival faction centered on memory, truth, copying, and preservation.
- Friendly to the player by default (`defaultReputation: 30`).
- Calm disposition rather than aggressive.

### Voice
Explicit from dialogue:
- They speak in archival metaphors: page, margin notes, stacks, crossed-out names, first drafts, copying truth.
- Their tone is formal, literate, and protective of knowledge.

### Ranks
Explicit:
- Inkbound Novice
- Inkbound Adept
- Inkbound Magus

### Gameplay-facing cultural traits
Explicit:
- Default spells include fireball.
- Produced goods include ink, potions, ink quills, ink vials, and scribe robes.
- Desired goods include gems, rings, focus crystals, and soul gems.

### Lore interpretation
Inferred:
- This is not just a wizard faction. It is a record-keeping magical order that sees knowledge preservation as sacred work.
- Their hostility language suggests they see destruction of records as a moral violation, not just theft or vandalism.

## Inkbound Scribes

### Core identity
Explicit:
- A related but distinct faction from the main Inkbound.
- Uses rank names centered on scribal status rather than broad magical status.

### Ranks
Explicit:
- Scribe Acolyte
- Scribe Adept
- Scribe Magus

### Material identity
Explicit:
- Lower ranks carry dagger, leather armor, and ring.
- Higher ranks move toward sword and heavier armor.

### Lore interpretation
Inferred:
- This looks like a more field-ready or institutionally formal branch of the wider Inkbound tradition.
- They appear less purely scholarly and closer to a disciplined order of archivist-agents.

## Inkguard

### Core identity
Explicit:
- A military or watch-order centered on gates, rules, oaths, holding the line, and survival through discipline.
- Neutral by default, aggressive disposition.

### Voice
Explicit from dialogue:
- Their tone is terse, duty-bound, and institutional.
- “We keep the gates and the rules that keep us alive.”
- “We do not love war. We love what survives it.”
- Hostile lines frame enemies as breaches to be closed.

### Historical anchor
Explicit:
- They refer to “the Siege.”

### Ranks
Explicit:
- Inkguard Recruit
- Inkguard Soldier
- Inkguard Captain

### Material identity
Explicit:
- Default gear is sword + iron armor.
- Produced goods include weapons, shields, armor, halberds, and ration packs.
- Desired goods include potions, ink, captain’s signet, and brimstone.

### Lore interpretation
Inferred:
- The Inkguard are a post-catastrophe defense institution. Their worldview is shaped by walls, attrition, and civic survival.
- Their relationship to Inkbound is likely complementary: one preserves memory, the other preserves continuity.

## Goblins

### Core identity
Explicit:
- Hierarchical martial scavenger culture with escalating leadership ranks.
- Aggressive disposition.

### Ranks
Explicit:
- Goblin Runt
- Goblin Warrior
- Goblin Captain
- Goblin Chieftain

### Economic signature
Explicit:
- Produced goods: dagger, leather armor, shiv, scrap armor, lucky tooth.
- Desired goods: gems, large potions, spectral blade, antivenom.
- Banned goods: steel armor.

### Lore interpretation
Inferred:
- Goblins are improvised, opportunistic, and materially scrappy rather than refined.
- The steel armor ban suggests either cultural rejection, impracticality, or inability to support heavy elite equipment.

## Skeletons

### Core identity
Explicit:
- Militarized undead hierarchy with knightly escalation.

### Ranks
Explicit:
- Skeleton Minion
- Skeleton Warrior
- Skeleton Knight
- Skeleton Lord

### Economic signature
Explicit:
- Produced goods: sword, shield, bone blade, bone plate, skull talisman.
- Desired goods: iron armor, steel armor, halberd, demon plate.
- Banned goods: potions.

### Lore interpretation
Inferred:
- Skeleton society reads as martial, feudal, and materially obsessed with arms and armor.
- The potion ban suggests an anti-living-material bias or simple uselessness for undead bodies.

## Ghosts

### Core identity
Explicit:
- Fast, incorporeal spectral hierarchy.

### Ranks
Explicit:
- Specter
- Phantom
- Wraith
- Revenant

### Economic signature
Explicit:
- Produced goods: spectral blade, wraith cloak, soul gem.
- Desired goods: gem, ring, focus crystal, infernal eye.
- Banned goods: potion.

### Lore interpretation
Inferred:
- Ghosts are associated with refined supernatural goods and soul-adjacent commerce.
- Their desired items suggest a culture interested in condensed spiritual or magical value rather than raw supplies.

## Demons

### Core identity
Explicit:
- Hierarchical infernal faction with negative default reputation (`-25`) and aggressive disposition.

### Ranks
Explicit:
- Lesser Demon
- Demon
- Greater Demon
- Demon Lord

### Economic signature
Explicit:
- Produced goods: ring, hellfire brand, demon plate, brimstone.
- Desired goods: steel armor, ink, soul gem, focus crystal.

### Lore interpretation
Inferred:
- Demons are not portrayed as mindless monsters. They have structured rank, trade preferences, and industrial/infernal outputs.
- Their interest in ink is notable and suggests potential overlap or rivalry with knowledge-bearing factions.

## Slimes

### Core identity
Explicit:
- Biological hierarchy from spawn to elder, but with very narrow material identity.

### Ranks
Explicit:
- Slime Spawn
- Slime
- Slime Brute
- Slime Elder

### Economic signature
Explicit:
- Produced goods: potion.
- Desired goods: gem.
- Banned goods: sword, iron armor.

### Lore interpretation
Inferred:
- Slimes are positioned less as political actors and more as an alchemical ecosystem that still fits into trade logic.
- Their economy is chemically productive, not martial.

## Snakes

### Core identity
Explicit:
- Fast reptilian hierarchy with venom- and hide-oriented trade profile.

### Ranks
Explicit:
- Snake Baby
- Snake
- Snake Adult
- Snake Elder

### Economic signature
Explicit:
- Produced goods: dagger, venom fang, snakeskin vest, antivenom.
- Desired goods: potion, large potion, scribe robes, lucky tooth.

### Lore interpretation
Inferred:
- Snake society reads as agile, poisonous, and materially specialized rather than expansionist.
- Interest in robes and potions suggests selective interaction with more civilized factions rather than total isolation.

## Merchant / Quest Tone

The merchant dialogue assets are simple, but they show the non-faction civic layer of the setting.

Explicit:
- Merchants can offer practical local work: clear out nearby slimes, make the road safer, get paid.
- The tone is grounded and transactional.

Implication:
- Even with strong faction theming, the setting still supports ordinary town-level life and small contracts.

## Reputation and Social Order

From `HOW_FACTIONS_WORK.md`, the setting assumes:
- factions track standing numerically
- friendliness and hostility are threshold-driven, not purely scripted
- allies can rally around attacks or kills
- some groups are forgiving, others retaliate immediately

Lore implication:
- Reputation is not a hidden combat stat. It is part of how the world socially coheres.
- Violence against faction members has political meaning.

## Distinctive Setting Vocabulary

Terms with strong identity value:
- Inkbound
- Inkguard
- archive
- stacks
- page
- margin notes
- palimpsest
- breach
- gates
- oath
- Siege

These words matter because they make the setting feel authored instead of generic fantasy.

## What Is Actually Documented Versus Missing

Clearly documented:
- faction names
- ranks
- default reputational posture
- some faction voice
- economic preferences
- aggression/discipline model

Not clearly documented in the source material reviewed:
- a full chronology of world events
- the exact nature of the Siege
- geography
- named cities or regions beyond system examples
- a central plot bible
- a formal cosmology

That means the Inkbound project currently has a strong faction-and-tone bible, but not a fully written narrative bible.

## Practical Import Notes for `caves-of-ooo`

If this material is used as reference later, the highest-value pieces are:
1. faction voice anchored in a few repeated metaphors
2. rank ladders that imply social structure
3. economic signatures that express worldview
4. hostility/reputation as social lore, not only AI logic
5. a small number of historical proper nouns like `the Siege`

The least portable pieces are:
- exact Unity faction-system mechanics
- asset-level GUID references
- literal fireball loadouts and sprite assignments

## Condensed World Read

Inkbound presents a world where civilization survives by preserving memory, enforcing boundaries, and attaching moral meaning to allegiance. Knowledge is treated as infrastructure. Order is treated as trauma-informed necessity. Even nonhuman factions are framed as cultures with rank and trade preferences rather than as anonymous monster types.
