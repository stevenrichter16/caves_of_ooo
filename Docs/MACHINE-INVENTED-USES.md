# Invented uses for the machines that had none

Provenance: user direction, 2026-11-19: "if a use for a machine does not exist, analyze the game and the purpose of the machine and invent a use for that machine that flows naturally with the rest of the game mechanics and the lore", and "you have full free reign of the game".

This replaces the dossiers' standing advice ([MachineDesign/](MachineDesign/README.md): "no demonstrated purpose, do not build") for the machines the owner named. It does **not** relax the principles in [SYSTEMS-DESIGN-PRINCIPLES.md](SYSTEMS-DESIGN-PRINCIPLES.md): finite accounting, custody and save integrity, honest unsupported-state limits, protected lore, additive saves, and the ordinary path (repair, healing, carrying) staying free. Evidence tags: **[V]** read in code or data this session; **[V\*]** verified by a dossier at its time; **[I]** inference or design, to be checked by the slice's first RED test or a lore read.

## 1. What the game gives a machine to attach to

A machine has an honest purpose when it lets the player do something through a rule the game already runs. The research found five such rules.

1. **One rule engine for energy and coatings.** A cell can hold heat, cold and charge (0..2) plus coatings (oil, water, ice) and residues; `Reactions.json` turns them into outcomes (oil ignites on heat, water steams, ice melts, water conducts charge). Every fire and lightning ability enters through `ZoneTileStateSystem` (`ApplyFireToTile`, `AddCharge`) and `ResolveAfterAbility`. [V] Machines that put a bounded, previewed amount of energy into a cell inherit every reaction, including ones nobody authored for them.
2. **One ignition seam.** `IgnitionSources.Find` answers "is there a flame in reach" for lighting a torch and relighting a finite cooking fire, and `GroveLaw.OnIgnite` charges the grove law where every ignition passes. [V] Anything that makes fire should be one more answer to that lookup, not a new verb.
3. **Concerns.** Twenty settlement problems, each with two or three approaches (Supply, Confront, Read, Restore, Settle) that set place conditions. [V] Several ask for goods the player can only buy or loot today: lamp oil (Quillhold, Slip), ward oil (the Quiet's Door), fire clay (Sill's oven, Cinderhold's seal), iron spikes, an oak haft and a binding for Cinderhold's pump. A machine earns a place by being one more honest way to get such a good, with its own cost.
4. **No survival clock.** There is no thirst and no ambient cold; the only exposure is the Beating glare, and a hat answers it. [V\*] So a machine cannot be a relief from a need. It must be an opportunity (a new thing to reach or make), an alternative with different costs, or a preparation for a known hazard.
5. **Site-bound and carried fixtures.** The Wellmeet main well shows a site-owned part with readable states; the four camp kits show a carried tool that becomes a station when set down. [V] Both patterns are reusable.

Rules the inventions below follow: attach to an existing rule or loop; add a path, never take one away; spend something finite or place-bound and never mint money (the Beating's salt economy was already inflated once by a free salt vein; EC5); preview the consequence; keep canon nouns plain and gear-only where a body could be mistaken for a specimen.

## 2. One invented use per machine

| # | Machine | Gap it fills | Invented use | Why it flows | Status |
|---|---|---|---|---|---|
| 9 | Dynamo and charging bench | Charge exists as a tile energy with real reactions, but only skills write it; a non-caster cannot make fire where no flame burns. | **Spark and surge.** Portable, finite stored work that puts one bounded, previewed energy into one cell: heat first (a flint striker), charge later (a coiled-cord rod discharged into standing water or a conductor). Renewed by spending parts, not free. | Rule 1 and 2. The striker is one more answer to `IgnitionSources` and writes heat through the routine fire abilities use, so oil and occupants behave as for any fire ability. | **Built (this commit): flint striker.** Charge rod [I]: needs a lore read of the everyday scale of binding-work (`Lore/History/09_Magic.md`) and a Sumphold East Cut consumer. |
| 8 | Thermal exchanger | Heat is a tile energy with reactions, but no machine aims it; fire clay (a repair material for Sill and Cinderhold) has no maker. | **Firing.** The camp hearth, fuelled, fires raw clay into fire clay, spending fuel per batch; later a directed plume (choose the side, preview what melts, steams or ignites) and a vent tap on a wild steam vent. | A kiln is what fire clay implies. Clay becomes a third source for three existing repair approaches without minting drams, because the deposit and the fuel are finite. | Design. Needs a finite raw-clay find (new gatherable) [I]. |
| 7 | Sump pump | Cinderhold's "Drowned Level" has data (a Pump site, a PumpRunning condition) and no physical pump. | **The pump is the town's heart.** A site-owned pump object at Cinderhold whose states (sheared rod, clay-plugged, running) the concern drives and the reader names; mending it with the reclamation chain's spikes, haft and binding is the purpose of that chain. A second, portable sump comes after it. | The pattern exists (Wellmeet's main well). The approaches and conditions already exist, so the object is the only missing piece. | Design; cheapest next. |
| 2 | Fractionating still | Ward oil is bought upstairs and otherwise needs sari venom the catacomb folk cannot fetch; lamp oil is already sold at three stock tables and strained at one shed, so it is not the gap. | **Rendering ward.** A camp still that renders `VenomGland` into ward oil, fractions chosen by input, finite by the source. | The Quiet's Door text says ward is made from sari venom; `VenomGland` exists as an item. [V] The player's still becomes the catacomb folk's craft for the player's use. | Design. Needs a lore read of ward (Dim, Rooted) [I]. |
| 10 | Survey resonator | Hidden sinkhole mouths are deliberately indistinguishable on the map. | **Sounding.** A tool struck on the overworld that reports "hollow" or "solid" for neighbouring map cells, never which hollow, never classifying the sound. | The world is a tepui of hollows; it answers a real question (where to look) and leaves the discovery to exploration. Lore: the sound is recorded, never systematized. | Design. [I] |
| 11 | Signal relay | A Concord caravan carries goods only as far as the Last Counter by contract; scattered camps cannot move goods. | **Carriage.** Entrust a carried stack to a Concord keeper for a fee through the ledger; collect it at another post after days; beyond the Last Counter the receipt says only "departed". | `Lore/Codex/06_ConcordCarriageContract.md` already writes the clauses. Arrival is reconciled when the player reaches the post, never by a timer on an unloaded zone. | Design. |
| 4 | Assay cabinet | Items explain themselves freely; there is no unknown to read. | **Reading a find.** At a scribes' desk, spend a measure of Ink and a sample to learn what a component can be recovered into or what a fluid does. | The Recension reads; Ink is the scribes' finite resource. [I] | Design; weakest of the set. |
| 5 | Culture vat | No living stock exists that is not a seed. | **A tended colony jar** for the catacomb folk's living light and ward, started from a cutting the folk give a trusted guest. | Their culture is a living light kept in jars. Needs the Quiet's Door standing (`Liked`) and a lore read. | Design. [I] |
| 12 | Treatment bath | Acid spray from the Marlback Soursprayer is the one under-served hazard near the start. | A **steeping trough** (gear only, never a body; the word "bath" stays out of player text because Curation and the Bower use it for bodies) that gives a garment a finite acid-proofing. | See the bath dossier. | Design; narrow. |
| 3 | Restoration cradle | The field rig already refits with parts. | Merge into the rig: the rig **is** the cradle. | Avoids a second apparatus. | Design. |
| 6 | Anchored capstan | No heavy objective qualifies. | Park until a heavy reclamation find is worth more intact at a workshop. | Dossier gate G0. | Parked. |
| 1 | Forge and reclamation rig | Already purposeful. | Portable cold bench (built). | | Done. |

## 3. The first build: the flint striker (spark)

**The gap.** `TorchLightPart` lights "at nearby fire" and the camp hearth relights "from flame". A player with no flame, no mutation and a dead torch had no way to make fire. The hearth kit's "carry a flame from a village fire" was a deliberate consequence, kept: a striker does not erase it, it gives a second approach with its own cost (finite strikes, a spike to renew).

**What it is.** `FlintStrikerPart` and the `FlintStriker` blueprint (20 strikes, weight 1, sold by the Tinker). It is not a fire, a stack or a light source.

| Behaviour | Mechanism |
|---|---|
| Lights a torch or relights a finite fire with no flame in reach | `IgnitionSources.Find` returns a carried, dry striker with a strike left, only after real fires and equipped lit torches; `IgnitionSources.Consume` spends one strike inside the caller's transaction after the claim, undone with it. The torch and hearth verbs are unchanged. |
| A real fire in reach is always used first and costs nothing | Search order. |
| Strikes a spark into an adjacent cell | Rows "strike a spark <direction>" appear only where `TileReactionSystem.HeatWouldIgnite` says one point of heat would light a combustible coating; it writes heat through `ZoneTileStateSystem.ApplyFireToTile` and resolves, after the action commits, so an outer failure never leaves a fire with the strike returned. |
| Will not spark wet or frozen | Same thresholds as the torch. |
| Renewed by spending an iron spike | "knap a new edge": one spike, ten strikes, never above the maximum; the reclamation chain's spikes are the sink. Disabled row says why when there is no spike. |
| Reader, save, diagnostics | `IDescribeState` line ("7 strikes left"); two public fields round-trip; `StrikerLit`, `StrikerSpark`, `StrikerKnapped`, `StrikerRejected` in the `event` category. |

**Consequences.** A spark into a slick burns it through the shared reaction, whose row also damages occupants of that cell; the row warns when you are standing in oil yourself. Whatever the world does for any fire ability happens here too, because the heat is written by the same routine. No rule was written or bypassed in the striker, and the grove law is **not** claimed: it is charged where an entity ignites through the thermal seam, and a bare burning slick in a grove was not tested.

**Honest limits.** Voice and art: the description is a mechanical placeholder and the item is a glyph (`/`). No tinder or dry-fuel model exists, so the striker lights only things the game already treats as ignitable. Only heat is built; the charge rod is design.

## 3b. The second build: rest beside the hearth

**The gap.** A rest is an instant full heal plus a 60-tick clock jump, free at any waysite campfire. The camp hearth was kept from resting ("the shipped coals opt out of rest"; "rest at a lit hearth" was listed as an open balance decision) because nothing simulates a zone-bound fire through a clock jump: a finite fire would keep its fuel through a night.

**The invention.** `HearthRestPart` on the hearth: the fire burns, while you sleep, what it would have burned in those ticks: ticks times its own burn rate, rounded up (60 ticks at the hearth's 0.25 is 15 fuel, about a timber and a half). The row says so ("rest (the fire burns about 15 fuel)"). It is refused up front, with the numbers in the reason, when the fire is out or has less than that, and nothing changes on a refusal. "Rest until the next time of day" is offered only when the fire could last it: from tick 0 the next band is 300 ticks away, 75 fuel, more than the hearth's 30, so it is only offered in the last stretch before a band. Heal, bleeding cure, clock advance and the hostile-within-eight rule are the shared `RestSystem`; fuel is charged only after it has admitted the rest, and returns with the transaction.

**Why it flows.** It uses the hearth's own numbers and no new constant; supplies the cost the owner asked a rest-at-fire to have (finite timber you carry or stash in the camp box); keeps every other fire's rest exactly as it was (a separate Part with its own commands, so the free waysite rest and the coals' opt-out are untouched); and answers the documented limit by charging the fuel instead of pretending the fire was simulated. A camp becomes a place you can sleep, at the price of the timber you brought.

**Honest limits.** A night is a full heal for about 15 fuel: a real, bounded lever that the owner should be able to retune by changing the hearth's `BurnRate`, `MaxFuel` or the timber's fuel. Other fuel burners (a torch in the pack) still do not burn through a rest. No Editor play yet.

## 4. Order

1. Flint striker (built), rest beside the hearth (built).
2. The Cinderhold pump as a site-owned object (cheapest, gives the reclamation chain a visible destination).
3. Carriage (the Last Counter has no forge and the contract text is written).
4. Sounding (a real question the map cannot answer).
5. Firing (needs a finite clay deposit; gives three repair approaches a third source).
6. Charge rod, rendering, colony jar, steeping trough, reading a find, each after its lore read.

Each ships RED-first with counter-checks, an adversarial file, actual play and a fresh-eyed review, as the home kits did.
