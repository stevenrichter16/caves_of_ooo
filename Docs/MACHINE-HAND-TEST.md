# Hand-testing the camp and machine work

Nothing in this list has been played in the Editor yet: every machine below was verified by EditMode tests, mutation checks and independent read-only reviews, not by a person at the keyboard. This is the order to try them in, what each step should show, and what to tell me if it does not. Evidence and design notes for each live in [MACHINE-INVENTED-USES.md](MACHINE-INVENTED-USES.md) (sections 3a-3q) and [HOME-BASE-IMPLEMENTATION.md](HOME-BASE-IMPLEMENTATION.md).

Scenarios are under **Caves Of Ooo > Scenarios > Home** in the Editor menu bar (start from the menu, not by pressing Play first). Press **C** toward a thing to open its menu; highlight a row to read the small text under the list.

## 1. The camp kits: "Camp Kits - wild ground" (Home menu, 2nd entry)

You start on ordinary wild ground with the box, hearth, bench and still, timber, a torch, a flint striker, craft parts, reagents and a flask of mire.

1. Open the pack (**i**), pick a camp kit, choose **make camp here (4 kits)**: the hearth goes down first, the still beside it, the bench and box clear of the flames.
2. **C** toward the hearth, highlight **stoke**: the small text says what it does. Choose it: it feeds timber and lights the fire from the striker in one action. The hearth glows.
3. **C** toward the hearth again: **rest until <time of day>** shows what the night heals and how much fuel it burns. (Use the "night, hurt" variant to see it matter.)
4. **C** toward the still: the menu shows your mix and what your mire would settle to; settle it (with the hearth lit within two cells).
5. **C** on any kit: **move** opens a picker (arrow keys or cursor; green border = can go there, red = cannot), **pack up camp**, **name this camp**. Open the world map (**<**): a line names the camp and what it holds.

## 2. The newer machines: "Camp Machines - wild ground"

You start on wild ground with the trough kit, hearth, still, five flasks (two of bog mire), garments and a Coldward cloak, a reagent for each element, three fire clay, a sounding rod and the parts Cinderhold's pump asks for.

- **Steam vent heat** (3p). A steam vent steams six cells east of you. Walk three cells east, set the still down east: it lands two cells from the vent (the cell right beside it is refused). **C** toward the still with a mire flask: the settle rows are enabled with no fire anywhere, and the small text says "The vent's steady heat pays; no fuel." Settle one: the message says "on the vent's heat" and no fuel moves. The bake row says the vent is not hot enough. Examine the vent while carrying mire or the still kit: it tells you what it is good for.
- **What a flask is good for.** Examine a flask of oil, water or mire: one extra sentence says what it does (oil "burns readily and makes ground slippery"; water "carries a charge and damps fire"; mire "damps fire"). It is the same data the game's reactions already use.
- **Proofing** (3q, [MACHINE-PROOFING.md](MACHINE-PROOFING.md)). Pack, a reagent (frost lichen, fire moss, spark root, sourmantle fold): **press into a flask** (an empty one); the flask then holds that element's proofing, one unit a coat. On the flask: **coat <garment>** (+35 of that resistance for 100 worn turns; works at once on what you wear; another coat of the same adds turns). Examine the flask and the garment: they say what it does and what status it also blunts. Set the trough down and **proof <garment> against <element> for good** (4 units of that proofing and 2 fire clay: +25, never wears out). Try the cold demo the scenario's text walks through: wear the leather armor (proofed) and the Coldward cloak, coat the cloak, and a freeze cannot take hold at 110.
- **Status dampening** (3q). Any resistance now shortens or weakens the status: a freeze, a burn, a shock stun, corrosion. The scenario carries two frost tonics and two lightning tonics: drink one bare (a full-strength freeze, six turns), then again after coating a garment you wear (four), with the lasting proofing and the cloak (one), and at 100 or more ("You shrug off the freezing."). The steam vent six cells east scalds for heat.
- **Baking clay** (3j). **C** toward the still beside a lit hearth: **bake 8 of the 12 mire to 1 fire clay** (spends 10 fuel). That is where more clay comes from.
- **Douse** (3m). With the hearth lit, open a flask of mire (pack, the flask): **pour ... east** over the hearth. The row hint says what it would do ("Puts out your hearth. It keeps its fuel."). A full flask puts it out; **pour one** only cools it. The hearth keeps its timber; stoke and light it again.
- **Sounding rod** (3g, with the survey log). Home menu, **Sounding Rod - an ordinary cave**: the fresh game starts one floor down in an ordinary cave with the rod. Elsewhere, in any ordinary deep cave (take stairs down and step in): pack, the rod, **sound the ground**. It names open ground, a way up or down, and dense stone you have not seen, as a bearing and a distance. The strike is loud. **read the survey log** reprints your last eight soundings with where you stood.
- **Fish trap** (3n). The scenario digs a small pond four cells west of you: walk two cells west and set the trap down west (it lands on the shore). In the real world, set it down on open ground beside a cascade pool in the Stump (the north-west of the world map; look for zones with a wide pool of falling water). **C** toward it: a reader says what it holds and how the water looks; the reader says when the next fish is due ("due around Dusk"); wait half a day (rest) and **take the fish**. A second trap on the same pool shares one supply, so it will not double the catch. Anywhere else the set-down verb offers no cell: that is the rule, not a bug.
- **Shade awning** (3o). Home menu, **Camp Awning - the Beating at noon**: the fresh game starts on wild ground in the Beating with the clock at the start of the Height band (take off any hat). Elsewhere, in the Beating (south of the world map) at the Height hours of the day, wait on open ground with a bare head: after ten turns the sidebar parches you. Set the awning down and stand under it or in the ring of cells around it: no stacks come, and stepping out starts the count again. Taking it up ends the shade. A hat or a roof still works too.
- **Cinderhold's mine pump** (3f). Travel to Cinderhold (map 6,6, north-west), find the pump beside the well, **C**: the menu shows what each repair needs against what you carry; **mend for good** uses the two spikes, the haft and the binding. It then reads "running". The same menu is on Tine's fishing weir, the Drowned Ledger's survey line and Wellmeet's pan scales (beside the well; the scales read "are", not "is").

## 2b. The proofing workshop: "Proofing Workshop (everything, 10 armor sets, hazards, elemental foes)"

You start on wild ground as a sturdy test character with every machine, every reagent, flasks, fire clay, ten full armor sets, status tonics and the four elemental spell books, beside a **proving post** (press **C** by it). The scenario's log walks the loop; in short:

1. Press reagents into flasks, coat a garment from a flask, set down the trough and proof a garment for good (a stack of identical armor is treated one unit at a time).
2. Wear a set. Call enemies at the post (heat, cold, shock, acid, two of each), or walk the hazards: heat north-east, cold north-west, acid south-east, shock south-west.
3. **Mend yourself** at the post between trials; **send away** what it called.
4. Compare bare against coated against proofed: the status shortens, the damage falls, and at 100 the status is refused ("You shrug off ...").
5. **Catch reagent for nothing.** Carry a settling tray (you start with two) to a hazard: a steam vent or ash bed, a frost vent or ice sheet, or a pool of brine, acid or mire. Press **C** on the tray kit in your pack: each set-down row says what that cell would catch ("set down east (catches heat)", "(nothing to catch here)"); a cell beside a hazard is refused, and so is the cell in line beyond an ash bed or frost vent. Set it down, rest a few hours, press **C** beside it: the reader says what it is settling and when the next is due ("due around Dusk" or "due soon"); "gather the vent scale (2)" takes them. What it holds is capped at four, one comes every half day, and a source gives only 3 to 5 before it is spent for a while (it looks "thin" and then "spent for now"; a day brings one back). A second tray on the same pool shares that supply. Press a catch into a flask: one coat each. The catches and any tonic brewed from them cannot be sold.
6. **Undo a lasting proofing.** Set down the proofing trough beside a proofed garment: "unpick the cold proofing from <garment>" frees the slot (nothing comes back); a coat on the same garment stays.

## 2c. The Sorters' frame at Posy

Start a fresh game (or use a scenario that generates Posy, map 5,9) and walk to the well in Posy's square. The frame stands a few cells from it (a pale tall frame, glyph `]` in magenta). Press **C** beside it: every carried thing with a real colour is a row naming the place it would go in and whether it is striking or dim; greys, whites and blacks are not offered; a costly thing is a greyed row that says it is worth too much. Set a bright, cheap thing in: it is spent, the message says which place, and the Bower-Folk's regard rises a little (the sidebar or a faction screen). A dim piece can be bettered once by a bright one; after that the place refuses. Fill all six places with bright pieces: "The frame is whole" and a last small regard; it never pays again. Examine the frame: it says what is placed, what is wanted, and what a bright piece would better, and shows no number.

## 2d. Hush strips and the pale stalkers

Hush strips are in the Tinker's stock (a bundle of four to eight, a 25% roll) and in the workshop scenario's pack. Carry them below ground, where the pale stalkers are (tier 3 and below; the eyeless ones that hunt by sound). Press **C** on the strips in your pack: one row for each open neighbouring cell ("lay hush strip east"). Lay a lane toward a stair, standing still between; step along it with the stalker in earshot: it should not turn toward you while you are on the felt, and should the moment you step off the end. Strike something from a strip: that is heard. Press **C** beside a strip: "take up the hush strip" returns the same item.

## 2e. The scraper

You start the workshop scenario with one (a Tinker stocks it too). Stand within two cells of a hazard that gives something off (a steam vent or ash bed, a frost vent or ice sheet, a pool of brine, acid or mire) and press **C** on the scraper: "scrape rime flake from the frost vent" and so on. One grain comes at once; the message says it rang. A spent source shows a greyed row; a day later it gives one more. Set a settling tray at the same source and notice that the two share one supply. Below ground, scrape a frost vent for cold reagent and then press it into a flask. With a pale stalker nearby, the ring draws it.

## 2f. The spring trap

In the workshop scenario (or from a Tinker). Press **C** on the trap kit in your pack: "load the spring trap with fire moss" (or frost lichen, spark root). Pour a flask of oil (or water) on a cell beside you, then press **C** on the loaded kit and choose where to lay it. Step onto the cell: oil plus fire moss goes up (fire damage and a smoke cloud that blocks sight); water plus spark root shocks; water plus frost lichen freezes the water and you. A trap on bare ground only marks the cell. In a Grovelands zone a fire-loaded kit refuses to be laid.

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
