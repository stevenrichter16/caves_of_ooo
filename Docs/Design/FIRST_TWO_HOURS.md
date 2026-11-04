# First Two Hours - Sill Opening Slice

> **Purpose:** Gameplay plan for the first two hours of *Caves of Ooo* using the
> new lore canon. This is a first-session design spec: pacing, quests, NPCs,
> state flags, fail states, and reveal discipline.
>
> **Canon inputs:** `Lore/10_Bible.md`, `Lore/03_History.md`, `Lore/06_Plot.md`,
> `Lore/09_Magic.md`, `Docs/Design/EVERYDAY_CHARMS.md`, and the faction docs.
>
> **Core rule:** The player should experience a local village problem first. The
> cosmology should be visible in objects, customs, failed charms, faction behavior,
> and consequences, not explained as doctrine.

---

## 1. Design Thesis

The first two hours are a village mystery in **Sill**, a recovered-world river
settlement preparing for a seasonal flower festival.

The player begins as an ordinary person who half-believes the old stories. By
the end of the slice, they should not understand the God-tree, Naro, Urqu, or
the three endings. They should understand five smaller things:

1. Everyday magic is real and charming.
2. Everyday magic is beginning to fail.
3. Different factions interpret the same strange event differently.
4. Promises and unfinished obligations have unusual weight.
5. The world underneath Sill is stranger than Sill admits.

The slice should end with the player feeling that the local problem has been
contained, but not solved.

---

## 2. Spoiler Guard

Do **not** reveal in the first two hours:

- Naro.
- Urqu as the unborn seventh god.
- The Six gods' mortal names.
- The full Felling truth.
- The three endings.
- Tent-Right as the true Renewal key.
- The completion-ratio as a visible global meter.

Allowed early language:

- One faint `sari...` sound, used sparingly.
- "the old Tree" as children's-story vocabulary.
- "the Root" as folk religion.
- "the Six" as oath-language and festival idiom.
- "the Choir" as a real faction people fear or tolerate.
- "Palimpsest" as scribes who write witness accounts.
- "Pale Curation" as preservers of the dead.
- "Catchers" as a whispered danger, not yet fully explained.
- "Concord" as traders of useful half-working things.

The player can hear contradictory explanations. None should be confirmed yet.

---

## 3. Starting State

### Location

**Sill**: Tier-1 river village, recovered-world, practical, lightly magical.
It should feel ordinary by local standards.

Required spaces:

- **Festival Meadow** - open field where the flower charm will be cast.
- **River Steps** - water, washing, gossip, small commerce.
- **Old Well / Root-Cellar** - the local threshold into the under-place.
- **Scribe Porch** - temporary Palimpsest table under an awning.
- **Concord Cart** - trader with charms, repair parts, Hush trinkets.
- **Sickroom / Neighbor House** - domestic errand target.
- **Irrigation Cave Edge** - first small dungeon, reachable from the well.

### Player

The player is local or local-adjacent. They know Sill's customs, but not the
hidden truth behind them. They can be skeptical without being ignorant.

Opening inventory:

- Plain tool or walking stick.
- One weak everyday charm known by default, recommended: `Petalfall` or `Warm
  Hearth`.
- One personal token from home, used later as a promise / memory anchor.
- No faction allegiance.

### Initial State To Track Quietly

Keep first-slice tracking small. These are not presented as big systems yet.

Slice-local variables:

- `AcceptedObligationCount`.
- `CompletedObligationCount`.
- `AbandonedObligationCount`.
- `SillTrust`.
- `ThinningLocalSill`.
- `TavinTriageState`.
- `ThreadClueRecovered`.

Durable hand-off flags:

- `PalimpsestLeadOpened`.
- `ConcordDebtOrInterest`.
- `CatcherEncounterSeen`.
- `ChoirThreadDisposition`.

The first two hours should make completion feel like care, not bookkeeping.

---

## 4. Pacing Overview

| Time | Beat | Player Experience | Primary System |
|---|---|---|---|
| 0-10 min | Morning in Sill | ordinary life, festival prep | movement, talk, inspect |
| 10-25 min | One setup task + optional errands | promises matter, charms are domestic | quest acceptance, inventory |
| 25-40 min | Failed flower charm | whimsy breaks | everyday magic, inspect |
| 40-60 min | Witness gathering | same event, conflicting meanings | dialogue, testimony |
| 60-80 min | First under-place trip | ecology, danger, material recovery | light, combat/avoidance |
| 80-100 min | Wounded NPC / Catcher scare | kindness as horror | triage choice, faction tension |
| 100-115 min | Choir thread | biological mystery, first faction fork | thread clue handling |
| 115-120 min | Festival ending | beauty with one wrong note | consequence summary |

The time ranges should survive player speed variation. The MVP golden path is
about 90-100 minutes; optional dialogue, errands, and cave exploration fill the
rest.

### MVP Golden Path

This is the buildable first-session cut. Everything outside this path is
optional, reactive, or later content:

1. Perform one pleasant flower-patch charm in the meadow.
2. Complete one non-counted festival setup step that grants the needed cave tool
   or light source.
3. See the crescent-shaped flower failure and inspect the wrong patch.
4. Collect or resolve enough witness information to point toward the well.
5. Enter the irrigation cave, bypass or fight the Gin Frog, and recover Tavin's
   broken reed-hook plus the pale thread clue.
6. Resolve the well-mouth pressure scene, choose what happens to the thread
   clue, and see the festival end with one wrong note.

The optional errands should modify warmth, trust, and visual omissions. They
should not block the player from reaching the cave or ending the slice.

---

## 5. Cast

### Elder Mara or equivalent

Festival organizer. Teaches the player the flower charm setup. Practical,
affectionate, uninterested in panic.

Function:

- Introduces domestic charm-work.
- Gives one non-counted setup step, plus optional obligations around town.
- Measures Sill's ordinary values: finish what you said you would do.

Sample line:

> "The charm does not need belief. It needs hands, water, and someone who will
> come back when they say they will."

### The Sick Child / Old Neighbor

Domestic stakes. Needs glow-porridge, warmed cloth, or lantern-beetle light.

Function:

- Shows magic as care.
- Gives a low-pressure obligation that can be completed or neglected.
- Later reacts to the festival charm's failure.

### Palimpsest Seasonal Scribe

Temporary scribe collecting festival records and oral accounts. Not ominous.
Kind, tired, ink-stained.

Function:

- Introduces witness accounts.
- Names the "sari" sound only as something worth recording.
- Offers the first choice between living memory and written memory.

### Concord Trader

Cheerful, useful, faintly predatory. Sells half-working charms and practical
goods.

Function:

- Introduces drams, trade, disclaimers, and "Hush charm" nonsense.
- Offers a shortcut with a cost.
- Models the Concord's comedy without explaining its god.

### Sister Wenil

First Catcher encounter. Gentle, apologetic, terrifying.

Function:

- Introduces preservation horror through action.
- Should not be a boss.
- Her kindness must be sincere.

### Injured Villager

Name them. Do not make them generic. Recommended: **Tavin Reedhand**, a young
irrigation worker who went below the well to check a clogged channel.

Function:

- Forces the player to act under pressure.
- Can be healed, preserved, or become a later rescue lead.

### Choir Tendril

Not an NPC conversation yet. A biological presence.

Function:

- Shows that "the Choir" is not just rumor.
- Creates a thread-clue handling choice.
- Sets up later Choir contact.

---

## 6. Detailed Beat Plan

### Beat 1: Morning in Sill (0-10 min)

Start with the player already in motion. No lore crawl.

Opening task:

- Walk from the player's dwelling / river steps to the Festival Meadow.
- Elder Mara asks for help setting the festival charm.
- The meadow has empty soil patches arranged in a spiral, with small stones,
  bowls of river-water, and tied scraps of colored cloth.

Interaction:

- The player inspects a charm marker.
- The UI explains a simple action: **Bind / tend / arrange**.
- The player performs a tiny charm action. One patch blooms correctly.

Design requirement:

- This must be pleasant. The first magic beat should be beautiful and useful,
  not ominous.

Lore seed:

- Someone jokes, "The Six can argue later. Today they can let us have flowers."
- No explanation follows.

### Beat 2: Setup Step And Optional Errands (10-25 min)

Give the player one required, non-counted setup step and three truly optional
errands. Each optional errand is an accepted obligation only if the player
explicitly says yes.

Required setup step - **Ready the Festival Light**

- Acquire or arrange a light source needed for both the meadow and the cave
  approach: lantern-beetle jar, borrowed lamp, or equivalent.
- This teaches item handling without becoming a promise-tracking test.
- The player can get the light from Elder Mara, the river storehouse, or the
  Concord cart. The Concord version can create debt only if the player chooses
  credit.

System:

- Basic item pickup / placement.
- Light-bearing object.
- No obligation count.

Errand A - **Return the Borrowed Shears**

- Take shears from the river steps to a gardener.
- The gardener will not start trimming festival reeds until they are returned.
- Completion visibly improves the meadow.

System:

- Promise accepted.
- Object delivery.
- Small `SillTrust +1`.

Errand B - **Glow-Porridge**

- Bring warmed glow-porridge to the sick child / old neighbor.
- If delayed, no death. The porridge dims and the recipient remarks on it.

System:

- Perishable item.
- Warmth/light property.
- Domestic consequence.

Errand C - **Lantern-Beetle Jar**

- Fetch an extra beetle jar from the Concord cart or river storehouse.
- This improves the festival lighting but is not required if the setup step
  already supplied a light source.
- The player can pay, borrow, barter, or promise later payment.

System:

- First transaction.
- Introduces Concord trader.
- Creates optional debt if the player takes credit.

Completion-ratio rule:

- Only accepted errands count.
- Declining an errand is not incompletion.
- The required setup step never increments accepted, completed, or abandoned
  obligation counts.
- Forgetting or abandoning an accepted errand increments only a local hidden
  flag, not a punitive global meter.

### Beat 3: The Failed Flower Charm (25-40 min)

The festival setup begins. If the player completed more errands, the ceremony is
warmer and more populated. If they completed fewer, it is thinner but still
proceeds.

Failure image:

- Most of the meadow blooms.
- One crescent-shaped patch blooms wrong.
- The flowers are pale, stemless, and scentless.
- Inspecting them shows a blank or unstable name field in the usual UI place.
- Nearby sound briefly dampens, as if the meadow forgot how loud it should be.

Player options:

- Touch the wrong flowers.
- Ask Elder Mara.
- Ask the Palimpsest scribe.
- Ask the Concord trader.
- Use `Clean Surface`, `Petalfall`, `Warm Hearth`, or equivalent if known.

NPC readings:

- Elder: "Bad soil. Bad timing. Do not frighten the children."
- Scribe: "Tell me what went quiet, exactly as you noticed it."
- Trader: "I have a charm for this. Mostly for this."
- Child: "It forgot what it was."

System:

- Inspect mode shows property oddity: low scent, low name-stability, abnormal
  local chill or sound dampening.
- No enemy yet.

### Beat 4: Witness Gathering (40-60 min)

The scribe asks the player to collect three witness accounts. This is the first
explicit Palimpsest-shaped quest, but it is framed as practical help.

Witnesses:

1. **Child witness** - heard a word disappear.
2. **Gardener** - saw a thread of white fungus near the well.
3. **Concord trader** - saw nothing, but offers to sell a story that will calm
   people.

Player can:

- Record honestly.
- Smooth contradictions.
- Sell the trader's false account to the scribe.
- Refuse to participate.

Consequences:

- Honest accounts: `PalimpsestLeadOpened`, later better explanation.
- Smoothed accounts: `SillTrust +1`, weaker Palimpsest lead.
- False account: `ConcordDebtOrInterest`, Palimpsest friction if discovered.

Important:

- The scribe should not say "Urqu."
- The scribe may say: "There are old records of this kind of quiet. Not here.
  Not in living memory."

### Beat 5: First Under-Place Trip (60-80 min)

The wrong flowers trace back to the Old Well / Root-Cellar. Tavin Reedhand went
below earlier to clear an irrigation clog and has not returned.

Objective:

- Enter the shallow irrigation cave.
- Find a stabilizing material or clue.
- Find Tavin's dropped tool or blood trail.

Dungeon size:

- 6-10 rooms.
- One loop.
- One optional side chamber.
- One water crossing.
- One low-light section.

Environmental teaching:

- Glow-fungus gives light.
- Moisture conducts small electric hazards if present.
- Petalfall / Breeze / Warm Hearth can create small emergent advantages.
- The under-place is not a dungeon theme park. It is Sill's infrastructure,
  grown strange at the edge.

Creature encounter:

- **Gin Frog** recommended.
- It blocks a narrow wet passage or guards eggs near glow-fungus.
- Baby-frog armor makes the fight memorable.

Noncombat options:

- Distract with scent / food.
- Use light or warmth to lure it away.
- Take the longer wet route.
- Wait for it to move.

Combat lesson:

- First fight can be won by direct attack, but the player sees that ecology
  matters.
- Fire/electric/acid stripping babies faster can be hinted but not required.

Loot / clue:

- `Pale record-stone chip`, later identifiable as low-grade Memory-Marble
  residue.
- `Tavin's broken reed-hook`.
- `Pale thread clue` from fungus near the wall.

### Beat 6: Return With Pressure (80-100 min)

On the way back or at the well-mouth, the player finds Tavin injured.

State:

- Tavin is alive, low HP, frightened, partially unable to say what happened.
- He repeats a broken phrase: "I heard someone trying to say it."

Sister Wenil arrives.

Presentation:

- She is not hostile-red by default.
- She knows Tavin's name.
- She speaks gently.
- She throws or prepares preservative only if Tavin remains critically injured
  or the player delays.

Player choices:

1. **Heal Tavin above threshold**
   - Use tonic, warm cloth, glow-porridge if saved, or fetch help.
   - Wenil backs off, apologetic.
   - `TavinTriageState = Healed`.
   - Durable flag: `CatcherEncounterSeen`.
   - `SillTrust +1`.

2. **Interrupt Wenil**
   - Shove, disarm, threaten, or fight.
   - She retreats wounded or shaken, but this first encounter should not include
     a killable Wenil branch.
   - `TavinTriageState = Interrupted`.
   - Sill may be shaken, and Wenil remembers the player.

3. **Let Wenil preserve Tavin**
   - Tavin becomes a later rescue / recovery lead in Marrowstye.
   - This is not a game over or hard fail.
   - The player learns that "saved" can mean something horrifying.
   - `TavinTriageState = Preserved`.

Support options:

- **Call the scribe** before choosing.
  - The scribe objects: Tavin's last memory is not final.
  - Wenil replies: "That is why I am early."
  - Faction conflict without exposition.
- **Delay too long**.
  - Wenil preserves Tavin.
  - The player is not punished mechanically, but Sill reacts.

Sample Wenil lines:

> "Tavin Reedhand. Stay with me. You are too much to lose."

> "I know you are afraid. I am afraid of what happens if I wait."

> "Please do not make me throw from farther away. It sets worse."

Design note:

- This scene is the first proof that horror in this setting often comes wearing
  the shape of care.
- Save killable Catcher branches and Pale Curation approval/disapproval for a
  second Catcher encounter after the player understands preservation better.

### Beat 7: The Choir Thread (100-115 min)

After Tavin is handled, the recovered cave material stabilizes most of the
festival charm. But the wrong flower patch exposes the same thin, pale mycelial
thread the player noticed in the cave, now running toward the well.

Player choice: what happens to the thread clue?

Options:

1. **Give it to the Palimpsest scribe**
   - Scribe preserves it in a small Memory-Marble dish.
   - Durable flag: `PalimpsestLeadOpened`.
   - Later Quillhold lead.

2. **Burn it**
   - Immediate local safety.
   - `ChoirThreadDisposition = Burned`.
   - Future Choir distrust seed.
   - Elder approves if frightened.

3. **Bury it near the meadow**
   - Village folk-practice choice.
   - `ChoirThreadDisposition = Buried`.
   - Subtle Choir route seed.
   - The wrong flowers may bloom again at night.

4. **Sell it to the Concord trader**
   - Durable flag: `ConcordDebtOrInterest`.
   - Later the trader resells the information.
   - Sill may lose control of the story.

5. **Keep it**
   - Unlocks later dream / inventory event.
   - `ChoirThreadDisposition = Kept`.
   - Small risk of Choir-Touched trace.

The Choir still does not speak directly. The player should feel watched by
biology, not contacted by a faction leader.

### Beat 8: Festival End Beat (115-120 min)

The festival happens.

The result depends on local choices, but the main image always lands:

- Completed errands produce more light, more villagers, stronger bloom.
- Abandoned accepted errands produce small visible omissions: an untrimmed reed
  row, a dim bowl, a missing lantern, a worried neighbor.
- The wrong patch is improved but not cured.

Final image:

- Flowers bloom across Sill.
- People relax.
- For one minute, the world is exactly as charming as it should be.
- At the edge of the meadow, one flower's name field empties.
- The player hears the slice's single clear `sari...`

End-of-slice quest leads:

- **Palimpsest:** travel with or write to the seasonal scribe.
- **Marrowstye / Catchers:** follow Tavin if preserved, or investigate Wenil.
- **Choir:** trace the tendril below Sill.
- **Concord:** ask who buys Hush charms and pale thread clues.
- **Local:** help Sill prepare a stronger charm before the next festival.

---

## 7. Player Choice Matrix

| Choice | Immediate Read | Later Use |
|---|---|---|
| Complete errands | care makes the festival stronger | teaches completion without meter |
| Ignore errands after accepting | small visible omissions | first local incompletion trace |
| Tell witness truth | scribe trusts player | Palimpsest route seed |
| Smooth witness truth | village stays calmer | local trust route seed |
| Buy Hush charm | trader is useful but suspect | Concord route seed |
| Interrupt Wenil | protects Tavin from preservation this time | Catcher memory hook |
| Let Wenil preserve Tavin | horror consequence, not fail | Marrowstye rescue hook |
| Burn thread clue | decisive local safety | future Choir distrust |
| Preserve thread clue | knowledge route | Palimpsest analysis |
| Sell thread clue | profit and leakage | Concord complication |
| Keep thread clue | mystery route | future dream / Choir-Touched trace |

No choice should be labeled good or evil.

---

## 8. Systems Introduced In Order

Required MVP systems:

1. Movement and inspection.
2. Dialogue with local tone.
3. Everyday charm as property modifier.
4. Basic item pickup / placement.
5. Light-bearing object.
6. Optional accepted obligation tracking.
7. Failed charm investigation.
8. Witness lead collection.
9. Low-light exploration.
10. First ecological creature encounter.
11. Injured NPC triage.
12. Consequence state in a village event.

Optional first-slice systems:

- Perishable item handling.
- Simple trade and debt.
- Harvest / clue handling.
- Faction intervention under pressure.

Avoid introducing:

- Full faction reputation UI.
- Global completion-ratio UI.
- Skill trees.
- Multiple gods.
- World map.
- Formal ending language.
- Killable faction representatives.

---

## 9. Required Assets / Content

### Maps

- `Sill_FestivalMeadow`
- `Sill_RiverSteps`
- `Sill_OldWell`
- `Sill_ScribePorch`
- `Sill_ConcordCart`
- `Sill_IrrigationCave_01`

### NPCs

- Elder Mara or equivalent festival elder.
- Sick child / old neighbor.
- Palimpsest seasonal scribe.
- Concord trader.
- Tavin Reedhand or equivalent injured worker.
- Sister Wenil.
- 3-5 ambient villagers.

### Items

- Borrowed shears.
- Glow-porridge.
- Lantern-beetle jar.
- Hush charm, flawed.
- Pale record-stone chip.
- Pale thread clue.
- Reed-hook.
- Basic tonic.

### Creatures

- Gin Frog adult.
- Baby Gin Frog shedlings.
- Optional harmless cave fauna for ecology.

### Charms

- Petalfall or Field of Flowers.
- Warm Hearth.
- Clean Surface.
- Optional Gentle Breeze.

---

## 10. Fail States And Recovery

The first two hours should be hard to brick.

If the player skips errands:

- Festival is thinner.
- No hard failure.
- Incompletion is visible through omissions, not punishment.

If the player loses the Gin Frog fight:

- Wake near the well, dragged out by villagers, with the frog still blocking the
  optional passage.
- Tavin scene still fires, but with less time to intervene.

If Tavin is preserved:

- He is shipped to Marrowstye.
- The player gets a later rescue / un-preservation objective.
- Sill does not treat this as simple death.

If Wenil is interrupted:

- Sill is frightened.
- Palimpsest wants an account.
- Wenil retreats and remembers.
- Later Catcher branches can decide whether this becomes approval, anger, or
  recruitment pressure.

If the player destroys all evidence:

- The festival still ends with one wrong flower.
- The lack of evidence becomes its own Palimpsest friction later.

---

## 11. Tone Targets

### Whimsy

- Festival prep.
- Flower charm.
- Glow-porridge.
- Villagers arguing over reed placement.
- Concord trader's disclaimers.

### Biological Horror

- Gin Frog brood armor.
- Pale fungal thread.
- Wenil's preservative pot.
- Tavin's partial inability to hold what he heard.

### Cosmic Horror

- Name missing from flower.
- One faint `sari...`
- The sense that a local failure has too much weight.
- The wrongness persists after the practical fix.

### Comedy

- Trader selling a Hush charm that "works on most sounds except the expensive
  ones."
- Elder refusing to let theology ruin a festival schedule.
- Scribe's bureaucratic anxiety about exact wording.

Comedy should not deflate the horror. It should make Sill feel alive.

---

## 12. Acceptance Criteria

The slice is working if a first-time player can say:

- "I know small charms can change local conditions."
- "I care whether the festival succeeds."
- "The scribe, trader, Catcher, and Choir thread all felt different."
- "I made at least one choice I expect to matter later."
- "I heard one faint `sari...`, but I do not fully know what it means."
- "The world feels charming enough that the threat to it matters."

The slice is failing if:

- The player receives a lore lecture about the Tree.
- The Catcher reads as a random combat enemy.
- The Choir reads as generic evil fungus.
- Completion tracking feels punitive.
- The festival feels like tutorial dressing instead of a real village event.
- The final wrong flower feels like a cliffhanger only, not the first visible
  crack in a larger cosmology.

---

## 13. Hand-Off Notes

This slice should be implemented before any god-facing content. It is the test
of whether the setting can carry its core trick: the same binding-work that
makes a field of flowers for a festival is the force that holds the world
together.

Build the first two hours around care, not destiny. The player should save,
fail, trade, record, and tidy small things before learning that small things are
the cosmology.
