# Hand-testing the camp and machine work

Nothing in this list has been played in the Editor yet: every machine below was verified by EditMode tests, mutation checks and independent read-only reviews, not by a person at the keyboard. This is the order to try them in, what each step should show, and what to tell me if it does not. Evidence and design notes for each live in [MACHINE-INVENTED-USES.md](MACHINE-INVENTED-USES.md) (sections 3a-3p) and [HOME-BASE-IMPLEMENTATION.md](HOME-BASE-IMPLEMENTATION.md).

Scenarios are under **Caves Of Ooo > Scenarios > Home** in the Editor menu bar (start from the menu, not by pressing Play first). Press **C** toward a thing to open its menu; highlight a row to read the small text under the list.

## 1. The camp kits: "Camp Kits - wild ground" (Home menu, 2nd entry)

You start on ordinary wild ground with the box, hearth, bench and still, timber, a torch, a flint striker, craft parts, reagents and a flask of mire.

1. Open the pack (**i**), pick a camp kit, choose **make camp here (4 kits)**: the hearth goes down first, the still beside it, the bench and box clear of the flames.
2. **C** toward the hearth, highlight **stoke**: the small text says what it does. Choose it: it feeds timber and lights the fire from the striker in one action. The hearth glows.
3. **C** toward the hearth again: **rest until <time of day>** shows what the night heals and how much fuel it burns. (Use the "night, hurt" variant to see it matter.)
4. **C** toward the still: the menu shows your mix and what your mire would settle to; settle it (with the hearth lit within two cells).
5. **C** on any kit: **move** opens a picker (arrow keys or cursor; green border = can go there, red = cannot), **pack up camp**, **name this camp**. Open the world map (**<**): a line names the camp and what it holds.

## 2. The newer machines: "Camp Machines - wild ground"

You start on wild ground with the trough kit, hearth, still, two flasks of bog mire, garments, resin, three fire clay, a sounding rod and the parts Cinderhold's pump asks for.

- **Steam vent heat** (3p). A steam vent steams six cells east of you. Walk three cells east, set the still down east: it lands two cells from the vent (the cell right beside it is refused). **C** toward the still with a mire flask: the settle rows are enabled with no fire anywhere, and the small text says "The vent's steady heat pays; no fuel." Settle one: the message says "on the vent's heat" and no fuel moves. The bake row says the vent is not hot enough. Examine the vent while carrying mire or the still kit: it tells you what it is good for.
- **Steeping trough** (3h). Set it down, **C** toward it: **steep leather armor** (1 resin + 1 clay). Examine the armor: it reads the treatment. Wear it: acid resistance +35 for 100 turns of wear; take it off and the turns that remain are kept.
- **Clay-wash** (3l). Same trough: **clay-wash kilnfelt apron** (2 clay). Wear it: heat resistance +35. A garment holds one treatment or the other; a new one replaces it and the row says which.
- **Baking clay** (3j). **C** toward the still beside a lit hearth: **bake 8 of the 12 mire to 1 fire clay** (spends 10 fuel). That is where more clay comes from.
- **Douse** (3m). With the hearth lit, open a flask of mire (pack, the flask): **pour ... east** over the hearth. The row hint says what it would do ("Puts out your hearth. It keeps its fuel."). A full flask puts it out; **pour one** only cools it. The hearth keeps its timber; stoke and light it again.
- **Sounding rod** (3g, with the survey log). In any ordinary deep cave (take stairs down and step in): pack, the rod, **sound the ground**. It names open ground, a way up or down, and dense stone you have not seen, as a bearing and a distance. The strike is loud. **read the survey log** reprints your last eight soundings with where you stood.
- **Fish trap** (3n). The scenario digs a small pond four cells west of you: walk two cells west and set the trap down west (it lands on the shore). In the real world, set it down on open ground beside a cascade pool in the Stump (the north-west of the world map; look for zones with a wide pool of falling water). **C** toward it: a reader says what it holds and how the water looks; the reader says when the next fish is due ("due around Dusk"); wait half a day (rest) and **take the fish**. A second trap on the same pool shares one supply, so it will not double the catch. Anywhere else the set-down verb offers no cell: that is the rule, not a bug.
- **Shade awning** (3o). In the Beating (south of the world map) at the Height hours of the day, wait on open ground with a bare head: after ten turns the sidebar parches you. Set the awning down and stand under it or in the ring of cells around it: no stacks come, and stepping out starts the count again. Taking it up ends the shade. A hat or a roof still works too.
- **Cinderhold's mine pump** (3f). Travel to Cinderhold (map 6,6, north-west), find the pump beside the well, **C**: the menu shows what each repair needs against what you carry; **mend for good** uses the two spikes, the haft and the binding. It then reads "running". The same menu is on Tine's fishing weir, the Drowned Ledger's survey line and Wellmeet's pan scales (beside the well; the scales read "are", not "is").

## 3. The Concord's carriage: "Concord Carriage"

You start with 60 drams, timber, a dagger, fire clay, a flask, a spore, and **one parcel already waiting at Cinderhold** (two iron spikes, "sent" from the Last Counter).

1. Travel to Cinderhold (map 6,6) and talk to the Concord Factor in the pruning post. The greeting lists the parcel ("ready to collect") and offers **[Carriage] Collect what has arrived (1)**: take it.
2. Choose **[Carriage] Send a parcel to the Last Counter (about 3 days)**. The list shows each stack with its fee (the spore is not offered: clause five). Pick the timber: the bargain shows **all**, **half** and **one**, each with its fee, only those you can pay.
3. Send some. The greeting now says "due in 3 days". Open the world map: one line says how many parcels ride.
4. Travel to the Last Counter (map 18,18) and wait the days out (rest at a fire). Talk to the Envoy there: **Collect what has arrived**. Your timber comes back, the same stack.
5. Things that should refuse: a short purse (no send row), a worn item, a rented item, six parcels at once, a name in default on a Concord debt.

## What to tell me

For anything that looks wrong, the most useful report is the exact row text you clicked, what the small text said it would do, and what happened (the message log lines). The diagnostic stream has records for every machine (`furniture/HearthDoused`, `trade/CarriageSent`, `skill/Sounded`, and the refusals), so "it did nothing" is usually answerable from those.

Known limits: wording is placeholder pending your voice review; numbers (fees, turns, degrees per unit, road speed) are proposals; Tally keeps no ledger so the cart has two posts; cached towns generated before a change keep their old content.
