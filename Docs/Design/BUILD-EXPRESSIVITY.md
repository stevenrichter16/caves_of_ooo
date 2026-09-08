# Build Expressivity — skills, practices, objects, and concepts for "every build playable"

> **Status: BRAINSTORM / DESIGN PROPOSAL.** Nothing here is implemented.
> Goal set by the user (2026-09): make every build playable, exploitable
> (in the emergent-synergy sense), and fun — and let players roleplay
> characters from other media *realistically*, meaning the systems must be
> expressive enough that a "Geralt," a "Walter White," a "Samwise," or a
> "Dark Souls hollow" feels like that character **through mechanics**,
> not through a name field.
>
> Grounding: every proposal is derived from committed canon
> (`Lore/10_Bible.md`, `Lore/11_SecondSpine.md`, `Lore/TERMS.md`,
> `Lore/MYSTERY-LEDGER.md`) and from systems that exist in the engine
> today. Where a proposal needs new engineering, it says so.

---

## 0. What exists today (so the design builds on it, not beside it)

| Surface | State | Notes |
|---|---|---|
| Skills (`SkillRegistry`, `SkillsPart`, JSON in `Content/Data/Skills/`) | 11 families, 75 files | Acrobatics, Axe, Cudgel, LongBlades, ShortBlades, Pyromancy, Cryomancy, Corrosion, Galvanism, Spellcraft, Persuasion. Command-routed, cooldowns, diag-instrumented. **All combat/elemental except Persuasion + Spellcraft.** |
| Mutations (`MutationRegistry`, ~30) | shipped | Elemental bolts/novas, Regeneration, Telepathy, Esper, Chimera, ExtraArm, Hearthwarm, WardGleam… Grimoire-driven acquisition. |
| Status effects (~30) | shipped | Bleeding, Burning, Frozen, Poisoned, Rooted, Stoneskin, FungalInfection, PaperSkin, Witnessed, Recruited, Hearth aura… |
| Alchemy (`ReagentPart` atoms, `BrewRules.json`) | merged | Property atoms: heat cold combustible corrosive conductive toxic binding vital sweet viscous volatile. MAX-potency combining. |
| Weaponcraft (`WeaponComponentPart`: Blade/Haft/Binding) | merged | Name-fragment composition; tempering. |
| Tinkering (bits, schematics, mod/disassemble) | shipped | `SchematicPart` learn flow. |
| Item enhancements (Lacquered etc.) | shipped | |
| Loot / gather nodes | shipped (untested in Unity) | `GatherNodePart`, `LootTableRegistry`. |
| Reputation (`FactionManager`, `PlayerReputation`, `GivesRepPart`) | shipped | Faction standing. |
| Followers (F.3), rentals, trade, player shop (design) | shipped / designed | |
| Storylets, House dramas | shipped | Note: several storylets are AT-era legacy (`Docs/LEGACY-CONTENT.md`). |
| Conversations | shipped | Recension, Choir, Concord, villagers, Wardens, House Thresker, Quartermaster. |
| Closure-ledger | **canon-designed, not implemented** | `11_SecondSpine.md` C4. |
| Names / Marks / Brands / Pacts | **identity-canon, not implemented** | `Docs/PROJECT-IDENTITY.md`. |

### 0.1 The unmerged FUN-P0 spine (`origin/feat/fun-p0-spine`) — read before designing

Surveyed 2026-09-08: `main` is *behind* this branch (0 unseen commits);
the only remote branch carrying content this branch lacks is
`feat/fun-p0-spine` (8 commits, last 2026-07-18, same merge-base
`c7fdd5a7`, **dry-run merge onto this branch: 0 conflicts**). It is the
playability spine this document must sit on top of:

| P0 move | State on the branch | What it gives this design |
|---|---|---|
| M1.a mortal start (`StartingLoadout`, 40 HP, dagger + 2 tonics, `DebugGrantsEnabled=false`) | shipped | there is finally a Level-1 to build *up from* |
| M1.b natural weapons materialized at spawn; 20 hostiles armed | shipped | threat exists; "solo survival tool" in §12 is now a real test |
| M1.c/d XP economy + MP stat | shipped | leveling and mutation-rank spending are live currencies |
| M1.e skill re-costing (root 1 / mid 2 / capstone 3-4) + `Requires` chains | shipped | **the tree grammar exists**: `"Requires": "Pyromancy_Cinder"` on Powers (class names, comma-AND). Root-level gating still needs code. |
| M2.a `GrimoireDistribution` (starter chest teaches 3; scribes sell by faction; lair chests; ambassador gifts) with a **totality test** (every grimoire reachable from exactly one source) | shipped | the reachability-pin pattern — every skill root in §3 should get the same "reachable from ≥1 world source" test |
| M2.b/c `LairTreasure`, LockPart→Container sync fix, TradeStockBuilder faction-gate fix, per-village ambassadors | **done but committed as `092d6d58 "asdfsadf"`** together with Roslyn DLLs, `Assets/UnityMCP/Log/*` (10k lines), and 16×24 sprite PNGs | needs a cleanup re-commit before merge |
| M3 quest announcer, Vex fix, cross-zone quests, chaining | planned, not started | the "game speaks" layer that Names (§10) will plug into |

**P0's binding design rule** (its design judge cut the Thinning scalar
and deferred character creation on this ground): *"the diagnosis is
systems starved of content — add wiring and content against tested
systems; new engine surface only where a gap has no existing
mechanism."* This document's proposals are re-sequenced in §13 to honor
that rule: express each root through **existing** surfaces first
(skill JSON + `Requires`, mutations/grimoires, Persuasion, reputation,
storylets, effects, followers), and add the two genuinely new substrates
(Names, closure-ledger) only once those are fed.

**Conclusion:** the *fighting* layer is deep. The *lore-native* layers —
memory, substrate, preservation, beauty, exchange, roots, naming; the
Spirits; the closure-ledger; names-as-progression — are where the
expressivity has to come from, and they are exactly the layers that make
non-combat and off-archetype builds viable.

---

## 1. Design principles (all derived from canon)

1. **A build is a Naming.** Canon: *a self is a Naming (a binding-knot)*.
   So progression is not "levels in a class" — it is **the Names the world
   has recorded for you**. Names are earned by witnessed acts, keystone
   whole branches, and can be *Unsaid* (lost). This makes "RP as X" a
   loop, not a label: you earn the Name that matches the fantasy by doing
   the things, and the Reading writes it down.
2. **Seven roots, not classes.** The Tree had seven functions; six were
   inherited; the seventh is empty. Every practice-tree hangs from one of
   the seven. Every faction is a *verdict on grief* built on one root, so
   faction alignment is emergent from what you practice, never a menu
   pick.
3. **One scale spectrum.** Canon: magic is binding-work on one seam, from
   *everyday charms* to *deep costly operations*. So every root has a cheap
   cantrip tier anyone can use, a middle tier of practice, and a deep tier
   that costs something permanent (a Mark, a Brand, a Pact). Permanent
   costs are the RPG's identity — they are never balance-nerfs, they are
   *who you became*.
4. **Everything speaks the same verbs.** Interoperability is what makes
   builds exploitable. Six shared "atoms" (see §6) are read and written by
   every system, so a cook's dish, a smith's quench, a scribe's entry, a
   Choir hymn, and a Concord contract all combine.
5. **The world answers; the designer doesn't nerf.** Every intended
   degenerate loop has an in-world response (§11). Exploits are content.
6. **No dead builds.** Every root must pass the four-hook checklist (§12):
   solo survival, economy hook, social hook, ending relevance.
7. **Witnessable only.** Nothing in a skill description adjudicates a
   protected mystery (Naro's reason, the sound, the layers). Skills may
   *use* the under-text; they never explain it.
8. **Feed before you build** (the FUN-P0 rule, §0.1). A root ships first
   as content on an existing mechanism; it earns new engine surface only
   when a node has *no* existing mechanism to ride. Every new system in
   this doc is tagged in §13 as either **wiring/content** or **new
   surface**, and new surface goes last.

---

## 2. Architecture: three layers + two threads

```
   PACTS         (3)   the Spirits — Inquiry ? / Bloodlust ! / Apatheia -
   PRACTICES     (7)   the seven roots — Keeping, Wedding, Salting, Posy,
                       Counting, Deep, Naming — each a small tree (7–10
                       nodes) + CROSSINGS (nodes needing two roots)
   TRADES        (~12) mundane learn-by-doing — weapons, cooking, brewing,
                       smithing, tinkering, farming, hunting, stealth,
                       athletics, performance, medicine, letters

   thread A: EVERYDAY CHARMS — the cantrip layer, available to all,
             cosmologically load-bearing (ambient binding reinforces the
             world; the Thinning comes for them first)
   thread B: NAMES — earned titles that keystone branches; the
             progression currency; can be borrowed, stolen, Unsaid
```

Existing skill families map cleanly: Axe/Cudgel/Blades/Acrobatics →
**Trades: weapons/athletics**; Pyro/Cryo/Corrosion/Galvanism → **Trades:
brewing-adjacent atom-channeling** (see Avatar row in §8) or **Inquiry
pact** at deep tiers; Spellcraft → **Inquiry** root; Persuasion → **Posy /
Counting / Naming** (split by intent).

---

## 3. The seven Practice roots

Format per root: the fantasy → nodes (mechanic in one line) → signature
exploit → world objects it needs → media characters it enables.

### 3.1 KEEPING (Memory — the Reader; Recension, Curation)
*The fantasy:* the one who knows. Detective, archivist, chronicler,
body-reader, underreader, cartographer.
- **Entry** — write a testimony-scroll about any witnessed event/person; scrolls are items (evidence, blackmail, memorial, trade good).
- **Recall** — re-read any entry you hold to relive its exact details (reveals hidden properties/NPC secrets recorded at the time).
- **Body-Reading** — read a corpse's last hour (who killed it, what it carried, what it said). Requires Recension standing; the Choir hates you for it.
- **Underreading I: places** — perceive bleeds/scraped features in a zone (reveals under-text relics; never explains them).
- **Underreading II: people** — read what was *scraped* from a person (lost names, Unsaid kin). **Without consent this is the cruel-knowing act** (canon storylet) — Inquiry-aligned, reputation cost.
- **Seal / Unseal** (Curation branch) — file an entry so it cannot be read/altered; unseal others' at cost.
- **Cartography** — map the over-text (sells to the Concord; forbidden for under-text — the map refuses the pen there, a hard mystery rule made visible).
- **The Page Carried** (capstone) — carry one page of the Reading: read aloud to restore a scraped detail temporarily, or to make a lie impossible in earshot.
*Signature exploit:* evidence economy — testimony scrolls are admissible in House-drama disputes and Concord contract fights; a Keeper can win wars with paperwork.
*Objects:* scriptorium/press, testimony-scrolls, seals, Memory-marble, the Reading's pages, carrier-script letters.
*Media:* Sherlock Holmes, Phoenix Wright, Indiana Jones (over-text cartographer/relic-hunter), a Gene Wolfe narrator, a Fallout scribe.

### 3.2 WEDDING (Substrate — the Wedded; Rot Choir, Driving Bloom)
*The fantasy:* the body as garden. Necromancy-adjacent, rot/fungus, poison, communion, spore-bond beastmastery, healing-by-replacement.
- **Spore-Sense** — see through the network: any fungal patch in the zone is an eye.
- **Include** — "heal" a wound by letting substrate replace the flesh (fast, cheap, permanent `FungalInfection` stack; body horror as medicine).
- **The Warm Dark** — lay a corpse into substrate: it returns *included* (a follower that sings; not the person).
- **Bloom-Drive** (Bloom branch) — the body driven by the Bloom: berserk-shapeshift on demand; costs a Mark per use over threshold.
- **Toxin Craft** — poisons/spores as reagent atoms (`toxic`, `vital`, `viscous`).
- **Spore-Bond** — a beast tamed through shared substrate; loyalty absolute, the beast slowly *includes* you back.
- **Hymn** — a sung buff that stacks per Choir-aligned ally; heard by the Recension as a crime.
- **Lie Down** (capstone) — enter the substrate briefly: teleport between fungal patches; each use forgets one of your Names.
*Signature exploit:* the corpse economy — every kill is a resource three factions want differently; a Wedded build farms kills into an included army.
*Objects:* cathedral-patches, spore-lamps, Bloom-seeds, corpse-baskets, the honey-jar larder (Concord/Choir crossing).
*Media:* a necromancer whose dead sing; Deadpool (substrate regen); Pokémon trainer (spore-bond); a Last of Us cordyceps-adjacent host; Berserker/werewolf (Bloom-Drive).

### 3.3 SALTING (Preservation — the Salted; Pale Curation, the Catchers)
*The fantasy:* stillness, wards, arrest. Paladin-of-stasis, monk, warden, healer-by-arrest, anti-mage.
- **Ward** — a held-still zone tile: nothing decays or changes in it (extinguishes fire, halts bleeding, freezes gas).
- **Arrest** — stop a wound (no healing; it simply stops getting worse — the Catchers' kindness).
- **Cure** — preserve food/reagents indefinitely (atoms don't decay).
- **Salt-Quench** — a blade quenched in salt bites under-text things harder (the anti-bleed weapon).
- **Still Hands** — immunity to Posy glamour and Naming suggestion while unmoving.
- **Seal the Door** — bar a passage against manifestations for N turns.
- **Salt-Cure Self** (capstone) — a limb or organ preserved: it never fails, and never heals; each cured part is a permanent Mark and a step toward the Salted's state. The **hollowing build**: a fully cured body gradually loses Names.
*Signature exploit:* freeze-the-world tactics — Ward-tiles turn any fight into a positional puzzle; the "nothing decays" rule also starves Choir substrate in the tile.
*Objects:* salt-blocks, ward-stones (Mute-Stone), curing racks, the Salt-Vault's filing rooms.
*Media:* Dark Souls hollow/undead (Salt-Cure Self + Unsaying), Jedi deflection/monk, a Warhammer null, a Witcher's Quen, Bene Gesserit body control.

### 3.4 POSY (Beauty — the Bower; Bower-Folk, Finishers)
*The fantasy:* glamour, performance, adornment, the beautiful act. Bard, charmer, illusionist, artist, assassin-aesthete, seducer.
- **Glamour** — a worn appearance (item-level disguise; the Reading sees through it).
- **Performance** — a stage act that amplifies everyday charms for everyone watching (festival-scale binding — the meadow-town scene).
- **Adorn** — garments/dyes carrying atoms (a cloak dyed `volatile`; boots `viscous`).
- **Composition** — art objects that apply an aura effect while displayed (a painting that calms; a sculpture that unsettles).
- **The Beautiful Kill** — a kill witnessed as art grants Bower standing and a Name; a clumsy kill loses it.
- **Resin-Cast** — cast a target (or a limb of yours) in resin: perfect preservation, perfect immobility. On others without consent: the Finishers' crime.
- **Found-Beauty** (capstone) — become beautiful *as you are*: your current state is fixed as your Name; damage no longer changes your appearance, and neither does healing.
*Signature exploit:* the audience engine — Performance turns any crowd into a charm-battery; a bard build powers a village's binding single-handed.
*Objects:* looms, dye-vats, ateliers, resin-blocks, stages/commons, mirrors, instruments (hymn-flutes, counting-boards).
*Media:* any bard; Hannibal (Posy + cooking + Wedding: the beautiful meal); an Assassin's Creed hidden-blade aesthete; Lestat; a Sith seducer; a Kingdom Hearts kid's charm-sequences.

### 3.5 COUNTING (Exchange — the Counter; Saccharine Concord, Rising Market)
*The fantasy:* everything has a price. Merchant prince, contract-mage, gambler, bounty broker, logistician, con artist.
- **Appraise** — see any item's/act's dram value, including intangible ones (a name, a debt, a grudge).
- **Chit** — turn a debt into an item: transfer, sell, forgive, collect.
- **Contract** — a bound agreement object; breaking it is an *abandonment* (feeds the Thinning) — so contracts have metaphysical teeth.
- **Tariff** — tax passage through a controlled route (with the shop system).
- **Futures** (Rising Market branch) — bet on outcomes (manifestation sites, harvests, deaths); the Counter despises you; huge payouts.
- **Credit** — borrow against a clean closure-ledger; a dirty ledger cannot borrow.
- **The Perfect Debt** (capstone) — settle one impossible account for someone; grants a Name; the Counter's arc.
*Signature exploit:* debt-magic — chits + contracts let a Counting build bind NPCs without a single spell; buy a village's obligations and *own* its ledger.
*Objects:* counters, ledgers, chits, contract-paper, honey-jars, tariff-posts, the player's shop.
*Media:* Tywin/Littlefinger, a Ferengi, Saul Goodman (Counting + Posy + borrowed Names), Walter White (Counting + brewing: the Bloom-trade kingpin), a Death Note-style ledger-keeper.

### 3.6 DEEP (Roots — the Rooted; catacomb-villages, Olderdeep)
*The fantasy:* earth, patience, growth, light. Farmer, miner, druid, ranger, geomancer, light-tender.
- **Tend the Light** — feed/redirect bio-light sconces; carry light; darken a room (the light is somebody's dream — over-tending has consequences).
- **Root-Sense** — feel the zone's structure: ore, water, hollows, what's under a wall.
- **Grow** — accelerate a gather node / crop; regrow a depleted node.
- **Burrow** — dig; make/unmake passages.
- **Stone-Kin** — `Stoneskin` at will; slow, hard.
- **Beast-Bond (rooted)** — tame by patience, not spores (the *other* beastmaster; the beast stays itself).
- **Endure** — the long-rest build: immunities scale with turns stood still.
- **Embrace the Wall** (capstone) — a permanent bond to one place: you never tire there, and everything there grows; leaving it costs.
*Signature exploit:* the farm-that-saves-the-world — everyday binding from cultivation literally reinforces the region against the Thinning; a Stardew build has cosmological weight.
*Objects:* fields, sconces, seams, picks, seeds, hearths, granaries.
*Media:* Samwise, Radagast/Aragorn, a Dwarf Fortress dwarf, a Stardew farmer, an earthbender, Groot.

### 3.7 NAMING (the empty seventh — Namers of Tent-Right; the Declined)
*The fantasy:* identity as power. True names, oaths, hospitality, and its negative: **the No**.
- **Host** — the tent-oath: anyone under your cloth is named guest; violence under it is an abandonment for the attacker.
- **Know the Name** — learn a true name (a quest item, never a skill roll); speaking it compels one act or one truth.
- **Give a Name** — name a beast, a child, a place, a thing: it becomes *what you said* (a sword named "Unfailing" jams less; a village named holds its charms longer).
- **Borrowed Name** — wear a Name that isn't yours; works until the Reading notices; then the Inkbound come to annotate.
- **The No** (Declined branch) — a spoken refusal as a spell: closes any act aimed at you (cancels a contract, breaks a compulsion, ends a courtship, refuses an inheritance). Cost: it must be *meant* — using it to dodge consequences you later pursue marks you.
- **Decline the Inheritance** — refuse a Name/Mark/Pact being pressed on you; the Declined's whole theology as a button.
- **Naming the Wound** (capstone) — name a damage as finished: it closes. Once per scar; every scar named is a Name you carry.
*Signature exploit:* the refusal build — a character who *only says no* can clear the closure-ledger, void hostile contracts, and reach the practice-path ending without finishing a single quest. Canon (C4) explicitly allows this; the design makes it a playstyle.
*Objects:* tents/cloths, name-stones (Tepuibone), oath-tokens, the Reading's name-registry.
*Media:* the Bene Gesserit Voice, a Jedi mind-trick, Frodo (a carried Named burden), a Sorrel, an Earthsea wizard, Bartleby ("I would prefer not to").

---

## 4. The three Pacts (the Spirits — pre-Tree drives)

Rare, costly, world-altering. A Pact is a Brand. Killing a Spirit is
possible and *permanently diminishes every ending* (canon) — the
god-slayer build exists and the world will never forgive it.

- **Inquiry (`?`)** — know → bind. The wizard/scientist root: Spellcraft's deep tiers, mutation research, underreading without consent. Cruelty-of-knowing storylets fire more often. *Media:* Dr. Strange, Rick Sanchez, a Star Trek engineer, Walter White's other half.
- **Bloodlust (`!`)** — unmake. Berserk/unbinding: Axe_Berserk's deep tier, unbinding-magic (thins the world — the Thinning meter moves *because of you*), damage that ignores Wards. *Media:* Guts (Bloodlust + a Brand that draws manifestations at night), Kratos, a Doom Slayer.
- **Apatheia (`-`)** — be still. The void-monk: immune to compulsion, Names, glamour, fear; can't be pressured by the gradient; slowly stops *wanting*, which the game renders as dialogue options greying out. *Media:* a Souls hollow at peace, an Airbender monk, a Vulcan.

---

## 5. Trades (mundane, learn-by-doing, no lore gate)

Every Trade must be a complete build on its own for a "pure mortal"
fantasy (Conan, a Witcher without signs, Samwise). Depth comes from the
**verb economy**, not from magic.

| Trade | Exists? | Depth source |
|---|---|---|
| Weapons (Axe/Cudgel/Long/Short + Haft/Blade/Binding composition) | yes | component naming + atom-quenching + tempering |
| Athletics (Acrobatics) | yes | terrain + Salting/Deep crossings |
| Cooking | **new** | dishes carry atoms → buffs, bribes, poisons, oath-feasts |
| Brewing/Alchemy | yes | atom matrix |
| Smithing | partial | quench-troughs write atoms onto blades |
| Tinkering | yes | bits/schematics; under-text relic modding |
| Farming | new | gather-node growth, everyday binding |
| Hunting/Beast-lore | partial | bestiary drops; bonds |
| Stealth | new (skill) | glamour/Ward/quiet-Binding synergies |
| Performance | new | crowd charm-amplification |
| Medicine | new | three philosophies (arrest/grow/include) — a moral stance as a build |
| Letters (literacy) | new | reading/writing entries; forgery (Keeping+Posy) |

---

## 6. The verb economy (why builds are exploitable)

Six atoms every system reads and writes:

1. **Property atoms** (exist): heat cold combustible corrosive conductive toxic binding vital sweet viscous volatile. **Extend to everything:** dishes, garments, quenches, gases, bio-light (`vital`+`binding`), spores (`vital`+`viscous`), resin (`binding`+`viscous`), salt (`binding`+`cold`), honey (`sweet`+`binding`), ink (`binding`+`toxic`).
2. **Closure states** (canon): every act is open → closed / refused / abandoned. Readable by Concord credit, Namers' oaths, sanctuary, manifestation risk.
3. **Names**: items in all but form. Earned, given, borrowed, stolen, Unsaid.
4. **Entries** (testimony): items. Evidence, memorial, blackmail, trade good.
5. **Debts** (chits): items. Transferable obligations with metaphysical teeth.
6. **Bodies**: items three factions price differently (the corpse economy).

**The exploit matrix is the product of these.** A cook who can write
`sweet+vital` into a dish can bribe a Choir patch; a smith who quenches
in `corrosive+conductive` has an acid-lightning blade; a Keeper who holds
an entry of a Concord broker's abandoned contract holds his credit rating;
a Namer who *gives* a Name to a Bower-cast statue may wake it. None of
these need bespoke code once the atoms are shared.

---

## 7. Crossings (nodes that need two roots — the named combos)

| Crossing | Roots | What it is |
|---|---|---|
| Body-Reader | Keeping + Wedding | read the dead through substrate |
| Debt-Cutter | Counting + Naming | void any contract with a spoken No |
| Glamour-Smith | Posy + Trades:smithing | weapons that look like something else |
| Salt-Chef | Salting + Cooking | food that never spoils; a feast that arrests plague |
| Spore-Tamer | Wedding + Deep | beasts bonded both ways |
| Underreader | Keeping + Inquiry | the deepest reading tier |
| Sanctuary-Keeper | Naming + Salting | a tent that is also a Ward |
| Hymn-Broker | Wedding + Counting | sell Choir hymns as buffs |
| Light-Thief | Deep + Posy | carry stolen bio-light as a glamour |
| The Refusal | Naming + Apatheia | a No nothing can pressure |
| Beautiful Ledger | Posy + Counting | contracts as art; breaking one is ugly, and ugliness costs |
| Included Archive | Keeping + Wedding (Inkbound) | annotate living people |

---

## 8. Media-archetype coverage map

The test: can a player *feel* like the character through mechanics?

| Character | Build recipe | The lore-native twist |
|---|---|---|
| Geralt | Trades:weapons (two blades, one Salt-quenched) + brewing (`toxic`+`vital` potions) + everyday charms as signs (heat=Igni, glamour=Axii, Ward=Quen) + Concord bounties | monster contracts are Concord paper; the "silver sword" is a Salt-quench that bites under-text things |
| Sherlock Holmes | Keeping (Entry, Recall, Body-Reading, Underreading:people) + Persuasion | deduction is *literally reading* what was scraped; cases are House-drama storylets |
| Walter White | Brewing + Counting (Futures, Chit) + Wedding (Bloom-seeds) | the Bloom-trade is canon-tolerated-and-despised; the Counter personally audits you |
| Hannibal | Posy (Composition, Beautiful Kill) + Cooking + Wedding | the Bower approves the plating; the Choir approves the eating; the Recension enters both |
| Samwise | Deep (Grow, Tend the Light) + Cooking + Naming (Host) + follower loyalty | everyday binding from a garden is *cosmologically load-bearing* |
| Guts | Bloodlust pact + Axe (big Haft/Blade) + Salting (Arrest) + a Brand | the Brand draws manifestations at night — canon manifestation mechanics |
| Jedi / Sith | Naming (Know the Name, suggestion) + Salting (deflect) + Apatheia or Bloodlust | the "lightsaber" is a blade of tended bio-light (Deep + conductive quartz) |
| Avatar bender | Pyro/Cryo/Corrosion/Galvanism as atom-channeling (heat / cold / corrosive / conductive) | elemental magic reuses the alchemy atom system — no new elements needed |
| Naruto ninja | Stealth + everyday-charm *sequences* (hand-seals = chained cantrips) + Spore-Bond summons | charm-chaining as a combo system |
| Pokémon trainer | Wedding:Spore-Bond or Deep:Beast-Bond + bestiary followers | two bond philosophies with different endings for the beast |
| Dwarf Fortress dwarf | Deep (Burrow, Root-Sense) + smithing + brewing + tinkering | strike the earth; GlowQuartz seams exist |
| Dark Souls hollow | Salting:Salt-Cure Self, escalating | hollowing *is* Unsaying: each cured part loses a Name |
| Bene Gesserit | Naming (the Voice = Know the Name) + Salting (Still Hands) + Keeping | the breeding ledger is a Recension file |
| Fallout wastelander | Tinkering + scavenging + Overwrit exposure | "radiation" is under-text bleed sickness (`PaperSkin` effect exists!) |
| Star Trek engineer | Tinkering + Inquiry pact + schematics | |
| Ash (Evil Dead) | Tinkering (boomstick) + Bloom-Drive prosthetic + an Overwrit grimoire | the Necronomicon is an unexplained under-text relic |
| Frodo | Naming (a carried Named burden) + Host | the burden draws the gradient; abandonment of the carry is the catastrophe |
| JoJo stand user | bind a lesser numen (canon: pre-Tree fragments at high-Strangeness places) | Stands are numina; each is a Pact-lite |
| Kratos | Bloodlust + Spirit-killing | every ending is permanently diminished — the game remembers |
| Ezio | Stealth + Posy (Beautiful Kill) + Counting contracts + Salting (stillness) | |
| Paladin/cleric | pick a god: Salted (wards), Rooted (light-healing), Choir (rot-communion), Bower (devotion) | four priesthoods, four medicines |
| Lestat/Dracula | Posy (Found-Beauty) + Wedding (Include) + Inkbound (feed on entries) | a name-vampire who annotates the living |
| Phoenix Wright | Keeping (Entry, Seal/Unseal) + Counting (Contract) + Persuasion | courtroom = House-drama storylet with admissible scrolls |
| Tywin / Ferengi | Counting full tree + shop + Tariff | own a village's ledger |
| Sanji / Remy | Cooking + Posy + Naming:Host | the oath-feast: everyone who eats is guest |
| Aragorn / Radagast | Deep + Beast-Bond + herb-lore + weapons | |
| Necromancer | Wedding:The Warm Dark | the dead return included, and singing |
| Shaolin monk | Apatheia + Salting + unarmed (Haft-less) | |
| Gandalf / Strange | Inquiry + Spellcraft + deep binding ops | |
| Saul Goodman | Posy:Glamour + Counting:Contract + Naming:Borrowed Name | the Reading eventually notices |
| Werewolf | Wedding:Bloom-Drive | body-horror shapeshift with Mark costs |
| Stardew farmer | Deep + shop + everyday charms | |
| Bartleby / the refuser | Naming:The No + Decline + Apatheia | reach the rarest ending by refusing everything, aloud |
| Indiana Jones | Keeping:Cartography + Overwrit relic-hunting + Concord fencing | the map refuses the under-text |
| Vulcan | Apatheia + Keeping | |

Coverage check: fighter ✓ mage ✓ rogue ✓ diplomat ✓ scholar ✓ crafter ✓
trader ✓ healer ✓ necromancer ✓ beastmaster ✓ druid ✓ bard ✓ monk ✓
engineer ✓ chef ✓ farmer ✓ priest ✓ detective ✓ assassin ✓ con artist ✓
shapeshifter ✓ god-slayer ✓ pacifist/refuser ✓.

---

## 9. World objects (new) — the non-combat toolset

**Stations:** hearth/kitchen, still, forge + quench-trough, loom + dye-vat,
scriptorium/press, atelier (resin), stage/common, counter, filing room,
cathedral-patch, tent, sconce, field, granary, curing rack.

**Materials with Naming properties:** Tepuibone (holds a name), Memory-marble
(remembers the hand that cut it), Mute-Stone (refuses a name) — the three
name-holding stones are already canon (Preserve ending). Salt-blocks,
resin-blocks, honey-jars, Bloom-seeds, spore-lamps, ink (Inkbound),
carrier-script paper.

**Documents as items:** testimony-scrolls, entries, seals, chits, contracts,
oath-tokens, pages of the Reading, maps (over-text only), *borrowed names*
(a written name that can be worn).

**Instruments:** hymn-flutes, counting-boards, mirrors, masks.

**Unexplained things:** under-text relics from the Overwrit — chaos items
with unpredictable atom signatures; never described, only *used*. The
Glassblown Remnant's glass — does something; the item text says nothing.

---

## 10. World concepts

- **Intake as character creation.** No class screen. You are entered by a
  Recension scribe: name, origin (surface-born / deep-born / faction-raised),
  boots, business. Your answers seed starting Trades and one Name. A
  player who wants to be "a monster hunter from the surface" *says so*,
  and the entry writes it.
- **Names as progression.** Titles earned by witnessed acts ("who carried
  the letter," "who refused the Concord," "who read the dead at
  Quillhold"). Each keystones a branch. Displayed in every conversation;
  NPCs react. Can be Unsaid.
- **The ledger as reputation.** Clean ledger = credit, sanctuary, oaths
  accepted. Dirty (abandoned acts) = the gradient pools near you;
  manifestation risk rises where you sleep. A visible meter with a lore
  name: **your standing in the Reading**.
- **Devotion tracks with the Six.** Each god offers a capstone that
  partially makes you *them*: a cured limb, a resin-cast arm, a page of
  the Reading, a spore-bond, a perfect debt, an embraced wall. Multi-god
  builds exist and are watched by all six.
- **Marks and Brands** (identity canon): permanent. A Mark is a cost you
  paid; a Brand is a Pact's signature. Both are Names of a kind.
- **Ending-lean.** Every root leans: Wedding → Consume; Salting/Keeping →
  Preserve; Naming/Deep → Renewal (practice); Inquiry → Renewal (vessel);
  Bloodlust → whatever thins. Builds *tilt* the ending they'll be offered
  without locking it. No root is "the good one" (C3).

---

## 11. Exploits as content (the world answers)

| Intended loop | The world's answer (not a nerf) |
|---|---|
| Corpse-farming for Choir favor | Recension hostility; Curation *files* you; villages stop selling you food |
| Abandonment-baiting to farm manifestations | the Thinning localizes around you; villages evacuate your zones; charms fail near you; the Declined send someone to *refuse* you |
| Name-stealing / borrowed-name stacking | the Reading notices; the Inkbound come to annotate you (a chase storylet) |
| Infinite-debt / chit-laundering | the Counter spends his three minutes a year on you |
| Ward-tile stalling | Ward tiles starve everything in them, including your allies' healing |
| Performance charm-battery | crowds tire; a bard who over-draws a festival dims the village's charms for a season |
| God-slaying a Spirit | permanent diminishment baked into every ending; NPCs who knew the Spirit grieve at you |
| Pure-refusal speedrun | fully allowed; the rarest ending is *earned*; the Concord blacklists you; Tent-Right adores you |

---

## 12. No-dead-build checklist (gate for every root/trade before it ships)

- [ ] **Solo survival tool** — can this build get out of a fight it didn't pick?
- [ ] **Economy hook** — can it make drams, or something drams buy?
- [ ] **Social hook** — does at least one faction *want* it, and one *fear* it?
- [ ] **Ending relevance** — does it tilt or enable an ending, or change what an ending costs?
- [ ] **A crossing** — does it combine with at least two other roots in a named node?
- [ ] **A media anchor** — can we name three characters from other media a player would recognize it as?
- [ ] **Witnessable only** — does no node text adjudicate a protected mystery?

---

## 13. Sequencing recommendation (agent-pace) — reconciled with FUN-P0

**Step 0 — land the P0 spine.** Clean-recommit `092d6d58` (keep the
M2.b/c code + tests; drop the DLLs, MCP logs, and PNGs unless the user
wants them), finish M3, merge. Nothing below should start on a branch
that still ships the debug loadout.

**Step 1 — roots as wiring/content on existing mechanisms** (no new
engine surface; each is one skill-JSON family + blueprints + tests, and
each gets a `GrimoireDistribution`-style reachability pin):

| Root | First nodes | Rides on |
|---|---|---|
| Naming | Host, The No, Decline | **Persuasion** family (exists) + `Requires` chains; The No = a Power that cancels an active `Recruited`/contract/effect via existing effect-removal-with-cause |
| Salting | Ward, Arrest, Salt-Quench | `Frozen`/`Stoneskin`/`Hibernating` effects + `WardGleam` mutation + weapon attributes (exist) |
| Wedding | Include, Spore-Bond, Hymn | `FungalInfection`, `Regeneration`, `Recruited`, followers (F.3), `HearthAura`-style aura |
| Keeping | Entry (a testimony item), Recall | a `TestimonyPart` item blueprint + storylet facts (`SetFact`/`IfHaveItem` vocabulary exists) |
| Deep | Grow, Root-Sense, Tend the Light | `GatherNodePart` regrow, light sources, `Rooted` effect |
| Posy | Adorn, Performance | item enhancements (Lacquered pattern) + everyday-charm mutations (`Hearthwarm`, `Kindle`) |
| Counting | Appraise, Tariff, Chit | `TradeSystem`, player shop, rentals (43-test surface exists) |
| Trades | Cooking | alchemy atoms on a `DishPart` (BrewRules pattern) |

**Step 2 — the two new substrates**, once the roots above have players:
Names (`IdentityPart`: earned titles keystoning branches — this is also
the P0 "character creation / origins" P2 item, now with a mechanism) and
the closure-ledger (`LedgerPart`: open acts → closed/refused/abandoned;
canon-designed in `11_SecondSpine.md` C4). Diag categories `name` /
`ledger`. These are the only items in this document that are **new
surface**, plus root-level `Requires` gating (P0 noted it needs code).

**Step 3 — Pacts and Crossings** as content once ≥3 roots and Names exist.

Each step is one reviewable ship with tests, per CLAUDE.md.
