# The Player Shop — an exploration

> **Authority:** Design study (exploration; not yet committed to a
> build plan). Explores models, stock, systems, and narrative hooks
> for letting the player open a shop, per user direction (2026-07-15).
> Grounded in shipped systems (`Docs/SHOPPING-PARITY.md` — the trade
> stack is Qud-exact and green) and current canon (Phases 1–12).
> §VIII proposes the v1 slice; the item catalogue in §IV is written
> to be turned directly into blueprints.

---

## I. Why a shop is uniquely a Caves of Ooo idea

Most games bolt shops onto their economy. CoO's canon makes a shop
load-bearing in four ways no other setting gets for free:

1. **A shop is a closure-engine.** The closure-ledger (Bible §IV, C4)
   makes every completed sale a *closed act* and every broken promise
   a dangling one. Commerce is not a money loop here — it is
   literally the physics of world-repair. A shopkeeper who fills
   orders, settles debts, and *refuses aloud* is doing cosmological
   work at retail scale. (And a shopkeeper who takes deposits and
   ghosts is, measurably, feeding Urqu. The game should never say
   this. The diag stream should know it.)
2. **A shop is a named place.** The world is an RPG with persistent
   zones; a shop is *territory the player keeps* — and naming it is
   a small act of binding (Phase 9: a name held is a knot tied). The
   sign over the door is an everyday charm in the most literal canon
   sense.
3. **A shop is a hospitality surface.** Tent-Right's oath-grammar
   gives "welcome before business" real mechanics to borrow; the
   practice-path makes a well-kept counter a rehearsal of the
   world's best ending.
4. **A shop inverts the quest graph.** Instead of the player walking
   to stories, **stories walk in.** Every customer is a delivery
   vector for faction texture, house-drama beats, rumors, and
   special orders. The shop is a narrative aggregator with a door.

And one economic fact makes it dramatic rather than cozy: **the
world already has a monopoly.** The Saccharine Concord trades with
everyone, everywhere, and canon gives it a god at the counter. A
player shop exists *in the Concord's shadow* — licensed, tolerated,
or defiant — and that tension is the spine of the whole feature
(§VI).

## II. Shop models (the avenues)

Ordered by engineering weight; they compose (later models grow from
earlier ones rather than replacing them).

### A. The Stall (v1 candidate ★)
A market pitch at Sill, Gantry, or Tally: a container the player
stocks + a price-set per item + **sales resolved by a daily tick**
against a demand model (§V) — no live NPC browsing AI needed. The
player returns to a *sales ledger*: what sold, to whom, what was
asked for and not found. Cheap, testable, diag-friendly, and the
ledger report is itself a storytelling surface ("a Curation officer
bought all your salt and left a intake form for the rest").

### B. The Named Shop (the full fantasy)
A building — claimed, rented (the rental system exists), or earned —
with a **name the player gives it** (a naming beat; the sign is a
charm), shelf slots, a **display window** (see the Bower hook, §VI),
stock storage, and eventually a hired keeper (the followers system
exists) so it sells while the player adventures. Persistent, savable,
upgradable. This is the v2 shape the Stall grows into.

### C. The Pack-Trade (itinerant)
No premises: a route. Buy where it's cheap, walk the travel graph,
sell where it's dear — surface bread doubling in the deep villages is
*already canon* (`08_MaterialCulture`: "wonders in the deep").
Location-sensitive pricing is a one-multiplier feature with huge
flavor. Composes with A/B as the *sourcing* half of shopkeeping.

### D. The Hearth-House (the tavern variant)
Sell comfort, not goods: tea, food, light, a warm room, charm-
services. Revenue is modest; the payoff is narrative — a hearth-house
is where rumors, house-drama witnesses, and travelers *come to you*,
and where guest-right mechanics turn the business into a sanctuary
(Catchers cannot operate inside; that is canon and it is marketing).

### E. The Curiosity Counter (the provenance shop)
The late-game specialist: sell *stories attached to things* —
underread documents, bleed-curios, lineage-tagged heirlooms. Value =
provenance depth; the underreading practice becomes a margin
("bought as a scraped quire, sold as a love letter with the
under restored"). Small stock, huge markups, discerning buyers.

### F. The Franchise / the Free Counter (faction-political)
Run a Concord trade-post as a licensed Factor (join-the-guild path,
steady and constrained) — or run **unlicensed** and live the
consequences (§VI). Endgame: a charter signed by Tovreth personally,
which canon says every faction honors with unusual seriousness.

## III. The stock problem — the honest inventory

What exists today that a shop could sell: 15 tonics, ~20 grimoires,
6 foods, torches/lantern oil, `PaleSalt`, `GlowQuartz`, `ChoirIron`,
`WardOil`, weapons. That is an *adventurer's* supply list — nothing a
villager, a scribe, a mourner, or a grandmother would cross the
street for. The gap is exactly the game's strength: **the canon is
full of cultural goods nobody has itemized yet.** Six families follow
— ~40 concrete items, each grounded in an existing canon line, named
in the workaday register (voice cards: no "palimpsest points").

## IV. The catalogue (ready to become blueprints)

*Price bands: `c` cheap / `m` mid / `d` dear / `L` luxury. "Buys
high": faction premium via the existing faction-modifier hook.*

### Family 1 — Charms & small bindings (`Docs/Design/EVERYDAY_CHARMS.md` tie-in)
Consumables with small real effects; **quality degrades as the
Thinning advances** — the stock itself tells the main plot.

| Item | What it does / flavor | Band | Buys high |
|---|---|---|---|
| Meadow-knot | conjures a festival flower-patch; wilts by morning | c | villagers, Bower-Folk |
| Hearth-loop | warms a camp/room one night (rest bonus) | c | travelers |
| Glow-porridge jar | a child's night-light you can eat; tiny regen | c | villagers |
| Petal-step ribbon | petals in your footsteps for a day; pure vanity | m | Bower-Folk (L) |
| Dry-thread | boots stay dry one storm | c | caravaners |
| Sweetwater pebble | freshens a waterskin | c | everyone |
| Name-tag charm | holds your name on you near the Hush (minor Slip ward) | m | Hush-edge travelers (d) |
| Door-knot | household ward; the folk version of the Sealed Dim | m | villagers |
| Quiet-bell | rings only when something unnamed passes; sleeps easier | d | catacomb villages |

### Family 2 — Light (the lumen trade; catacomb canon)
| Item | Flavor | Band | Buys high |
|---|---|---|---|
| Lumen-jar | stored bio-glow, sold by the hour — the canonical currency, itemized | c–m | everyone underground |
| Beetle-lantern | alive; needs feeding; never "runs out," sometimes wanders | m | miners |
| Glow-strain cutting | rare-spectrum patch stock | d | Bower-Folk, villages |
| Dark-wick candle | burns *dim* on purpose, for the Sealed Dim | c | villages (bulk) |
| Miner's dawn | one brilliant flare; ruins your dark-sight; saves your life | m | delvers |

### Family 3 — Paper, ink & provenance (Phase 12 tie-in ★)
| Item | Flavor | Band | Buys high |
|---|---|---|---|
| Dew-ink vial | Drosera ink; glows faintly fresh | m | Recension, Inkbound |
| **Scraped quire** | second-hand vellum — **every sheet has under-text**; buying is a lottery, underreading before resale is a margin | c buy / ? sell | Recension, collectors |
| Grimoire copy | the existing `GrimoireCopy` economy, retail | m–d | casters |
| Route-paper | a map fragment; Branchwork-derived routes are the premium tier | m–d | caravaners |
| Letter of carriage | courier board: carry-and-deliver contracts (closure-ledger native) | fee | — |
| Testimony slip | the Recension *pays* for witnessed events, verbatim | they pay | Recension |
| Memory-marble chip | the stone that holds names hardest, cut small | d | Curation, charm-wrights |

### Family 4 — Salt, seals & the funerary trade
| Item | Flavor | Band | Buys high |
|---|---|---|---|
| Pale-Salt measure | exists (`PaleSalt`); the money you can eat or embalm with | m | Curation (always) |
| Seal-kit, Curation-standard | body-transport certification; grimly practical | d | families, Catch-wary travelers |
| Honey-seal jar | preservation-grade honey; ask no questions about the larder canon | m | Curation, Bower |
| Plaque blank | a First-Plaque stone, uncut | m | villagers (rite good) |
| Resin thread | Bower composition supply | m | Bower-Folk |
| Wake-bread | funeral loaf; stales in exactly three days, by design | c | villagers |

### Family 5 — Food & hearth (location-priced ★)
| Item | Flavor | Band | Buys high |
|---|---|---|---|
| Marsh-rye loaf | surface staple; **price doubles underground** (canon) | c/m | deep villages |
| Larva paste pot | deep staple; surface folk gag; nutritious | c | villagers; comedy elsewhere |
| Starapple preserves | shelf-stable orchard sweetness | m | everyone |
| Black tea brick | *the Counter drinks this.* The label says so. It's even true | m | scribes, clerks — and one god |
| Grove honey | Choir-adjacent apiary; delicious; provenance conversations | m | the untroubled |
| Festival cake | a charm you can eat (tiny morale buff) | c | holidays |

### Family 6 — Curios & strange goods (singulars; the E-model stock)
| Item | Flavor | Band | Buys high |
|---|---|---|---|
| Spore pouch | the Choir will ask you to carry these. Selling them is a *statement* | m | Choir-aligned; horror elsewhere |
| Unfired vessel | Remnant paste-glass — reversible, unfinished on principle; owners report unease | d | collectors |
| Bower-Starstone trinket | set stones; Curation refuses to file them ("too aesthetic," canon) | L | Bower-Folk |
| Bleed-page | a page from a drying-line that isn't there anymore; **never twice from the same line** | L | Recension (quietly) |
| Bog-amber | preserved pre-Felling insects; the cheap end of deep time | m | everyone |
| Choir spine | exists (`ChoirSpine`); retail provenance matters | m | — |

### Services (sell verbs, not nouns)
- **Underreading** ("read the under of this for me") — fee scales
  with the owner-question (§UNDERREADING moral edge applies in full).
- **Charm-weaving to order** — commissions; closure-ledger promises.
- **Witnessing** — oaths, contracts, guest-bonds need a third party;
  the player's signature accrues weight with reputation.
- **Rental counter** — sub-broker loaner weapons using the *existing*
  `RentalPart`/Ink system with the roles flipped: the player as
  lessor. (The Ink system's adversarial test suite already pins the
  contract semantics; this is the cheapest "new" feature in the doc.)
- **Tea** — costs almost nothing; sets the guest flag; opens the
  rumor/drama table (§VI). The most profitable thing on the menu is
  not priced.

## V. Systems fit (what exists / what's new)

**Exists and carries the load:** `CommercePart` values;
`TradeSystem` price math + faction modifiers (demand tiers are
literally already implemented as price multipliers); `TradeUI`
(reusable in "keeper mode"); `BeforeTrade` veto (= stocking-policy
reactions: sell spore pouches and watch Curation standing move);
`TradeStockBuilder` (invert it: it becomes the *customer generator*);
rental parts; followers; SaveSystem persistence; the closure-ledger
design; diag substrate.

**New (v1, small):**
1. **StallPart** — a container entity flagged as player stock, with
   per-item asking prices.
2. **The daily market tick** — resolves sales offscreen from: zone
   traffic × faction demand table (§IV columns) × price-vs-value ×
   player reputation. Emits the **sales ledger** (a generated codex-
   style report — write it in the Concord clerk register for free
   comedy).
3. **Special orders** — the tick also generates *requests* (an NPC
   wants X by day Y). Accepting = a promise on the closure-ledger:
   fulfill (closure), **refuse aloud** (closure — the spoken no gets
   a UI verb here, which the whole C4 design has been waiting for),
   or let it lapse (abandonment; the ledger notices).
4. **Diag** (decided now, per the observability rule):
   `category=trade`, kinds `ShopSale`, `ShopOrderTaken`,
   `ShopOrderClosed`, `ShopOrderRefused`, `ShopOrderLapsed`,
   `ShopInspected`, payloads carrying item, buyer faction, price
   delta, ledger state.

**New (v2):** premises + naming + display window + hired keeper +
live browsing NPCs (the only genuinely expensive piece; defer it —
the daily tick fakes a living shop convincingly at 1% of the cost).

## VI. Narrative hooks (the reason to build it)

- **The Concord question (the spine).** An unlicensed stall draws
  Factor Brenn: first a visit, then a fee schedule, then
  "inspections" (half-working charm of a visit, premium-priced). The
  player's options map the factions: *pay the license* (Concord
  track, ends at the Factor path); *pitch under guest-right* in
  Tent-Right territory (the Concord honors the oath there — canon —
  and hates every minute of it); *charter with a catacomb village*
  (their light-economy would love a surface-goods counter); or *stay
  free* and play tariff-dodging cat-and-mouse. Endgame: **the
  Counter-signed charter** — bought not with drams but by settling
  one perfect debt's worth of paperwork in front of Tovreth, who
  will find the request "irregular," meaning *interesting*, meaning
  he has not been asked something new in two hundred years.
- **The Bower window.** If the shop has a display slot, the
  Bower-Folk *judge it* — aesthetic-value reputation applied to shelf
  arrangement (their canon economy, pointed at the player's
  storefront). A well-composed window draws Bower customers and one
  day a Seer, who relays that *She* has noticed your window, which is
  wonderful for business and bad for sleeping.
- **Choir stocking requests.** The `mycelium.txt` escalation design
  slots straight in: carrying spore pouches → stocking them → the
  Choir asking you to *carry the shop's unsold dead stock somewhere*
  → the pronoun shift. The shop gives that arc a counter to stand at.
- **The Thinning on the shelves.** Charm quality is a world-state
  read: the day the meadow-knots start wilting *before* morning, the
  player's own stock is the canary. The apocalypse arrives as a
  customer-complaints arc — which is the project's tonal register in
  one design.
- **Dramas walk in.** House-drama witnesses, Catcher pamphlets left
  on the counter, a Vess-Mara caravan wanting wake-bread for a
  funeral the player then hears about — the daily tick doubles as a
  storylet delivery channel.

## VII. Risks / do-nots

- **No spreadsheet creep.** One stall, one ledger, a dozen prices —
  never inventory-management-as-punishment. If a screen needs
  scrolling, cut stock families, not corners.
- **No infinite-money loops.** The demand table saturates (villagers
  need only so many door-knots); location-arbitrage margins decay on
  repetition (the Concord notices and undercuts — diegetic economy
  balancing).
- **Don't gate main-quest content behind shop wealth.** The shop is
  a *place to be from*, not a required income.
- **The comedy stays deadpan.** The sales ledger is funny the way
  the carriage contract is funny — by procedure, never by winking.

## VIII. Recommended v1 slice

**The Stall + three families + orders.** Ship: StallPart, the daily
tick + sales ledger, special orders with the refuse-aloud verb,
Families 1/2/5 (charms, light, food — ~20 items; they're also the
best world-texture per item), the rental sub-counter (cheapest
feature), and the Brenn license beat (one visit, one choice).
Defer: premises/naming/window (v2), Families 3/6 as loot-side items
that trickle in (they need Phase-12 systems anyway), the hearth-house
and franchise models (v3 shapes).

Estimated: item content ≈ 2h (blueprints + CommercePart values +
demand table); StallPart + tick + ledger ≈ engineering, Unity-side;
orders/closure integration rides the ledger work already handed off.
One user checkpoint: this document.

---
---

# PART II — The Strange Shop (the get-more-creative pass)

> Part I is the floor: goods, prices, a ledger. Part II is what only
> this game can build on that floor. Everything here obeys the canon
> locks (pressure-model Urqu, the Mystery Ledger, the voice cards);
> in-world text samples below are **drafts, ungated** — they run the
> blind review before shipping, like everything else.

## IX. The immaterial counter — selling things that aren't things

The deepest shelf in the shop sells no objects at all. In a world
whose physics is Naming and whose sickness is abandonment, the
premium goods are *speech acts*:

### The no-smith ★

Canon: a swallowed no is a seed of the Hush; refusal must be spoken
to close (`04_TentRightOath`: "go *spoken*, guest"). But most people
**cannot say their no's** — to the Choir's standing offer, to a
Catcher's gentle insistence, to a debt-holder, to a dead parent's
unreasonable last wish. So they hire the player to say it for them.

The no-smith service: the client tells you the refusal they cannot
utter; you deliver it — to the tendril, the creditor, the shrine —
formally, aloud, in their name. Design questions with teeth: *does a
proxy no close the client's ledger, or the smith's?* The Namers
split on it (live doctrinal content); the closure-ledger's answer is
committed here: **it closes if and only if the client stands within
hearing.** So the service is really escort-plus-courage: you bring
them to the brink and say the hard sentence, and they must witness
their own refusal. Every job is a micro-drama with a walk, and the
client roster writes itself: the widow refusing the Choir her
husband's body; the apprentice refusing a Curation intake officer;
the village refusing, at last, a god.

*(Cheap to build: dialogue-driven, no new systems — a storylet
family hung on existing faction content. The single best
creativity-per-engineering-hour ratio in this document.)*

### The name-counter

Naming is the world's binding-function; a naming service is
retail cosmology. Tiers: **naming boats, tools, and babies**
(villagers pay; each naming is a small real binding — a named tool
gets the tiniest durability edge, which blacksmiths deny and
quietly rely on); **naming the nameless** (a foundling, a new
sinkhole, a dish you invented); and the gray market — **selling
names**: a dead debtor's name to their creditor, a disgraced name
laundered into a new one, *your own* name (once; never buy it back
at the price you got). Tent-Right watches this counter very
closely: to the Namers, a name *enacted* at a counter is practice —
a name *priced* is the Concord's oldest error wearing new clothes.
Whether the player's name-counter is holy or corrosive is decided
by conduct, not category, and both factions will tell them so, at
length, while buying.

### The unfinishable shelf (grief-storage)

A pawnshop for things people can neither keep nor discard: the dead
husband's boots, the letter never sent, the child-sized First
Plaque that was never cut. The service is not storage — it is
**display**. The item sits on a public shelf, visible, tended,
*still in the world*, while its owner learns to walk past the
window. Fees are nominal; some owners pay for decades; some, one
day, come in and take the thing home, and that transaction (drams:
zero) is logged by the closure-ledger as one of the largest closure
events retail can produce. The shelf also slowly turns the shop
into something the Curation cannot classify — an archive where
everything is still owned by the living — and their attempts to
file it are a running comedy with a knife inside.

## X. The Under-Shop ★ — the shop's own palimpsest

Premises come with under-text. The moment the player takes a
building (model B), Phase 12 applies to the *floor*:

- Customers occasionally reach for shelves that aren't yours. Ask
  for goods you never stocked — always the same few items. Leave,
  once, a coin whose face is worn to nothing.
- Underread the floorboards and the counter, and the previous shop
  bleeds up: shelf-shadows, a price-board in an older hand, and a
  **scraped sign** — the last keeper erased their own shop's name
  before leaving. (First-position knife-work. They scraped only
  what was theirs.)
- The mystery-line: who scrapes their own sign? The answer is never
  fully given (ledger discipline), but the *orders* are: the old
  shop's order-book bleeds through page by page, and it ends
  mid-line — a special order taken and never filled, seventy years
  dangling. **The player can fill it.** Track down the customer's
  descendants (lineage system), deliver the impossible late order,
  and close *someone else's abandonment* — the rarest closure type
  in the game, and the moment the shop stops being haunted and
  starts being *yours*. The doorbell sounds different afterward.
  Nobody comments.

## XI. Customers only this game could seat

### The Six, shopping

Each god visits at most once per game, unannounced, in the grammar
of their function — six scripted vignettes, each a reward for
world-state the player built:

- **Tovreth** comes for the tea brick. Stands in line. Pays exact
  change. The comedy runs for months — the god of Exchange as your
  most punctual regular — until the day he pays with *three minutes
  of undivided attention* instead of coin, which the canon prices
  as his entire annual allowance of rest, and asks you one question
  about your ledger, and leaves before you understand what you
  answered. *(His question seeds the charter arc, §VI.)*
- **Ylaes** never enters. She rearranges your window — one item per
  night, for a week, via a Seer who apologizes at dawn. Sales
  double. Sleep suffers. On the seventh morning the window is
  *finished*, and moving any item feels like vandalism, and the
  Seer's last relay is: *"She says: now leave it until it isn't."*
- **Selen** sends customers. Tendrils, buying gifts *for the people
  inside her* — "the one who sang by the ford would like the blue
  ribbon." Payment is always exact, always slightly damp, and
  once, terribly, includes the coin the Under-Shop customer left
  (§X), which means she has been shopping here longer than you
  have.
- **Maeleth** buys nothing; requests receipts. For everything —
  purchases she didn't make, conversation, the weather. A scribe
  follows her writing it all down, until you realize (voice-shift)
  that the scribe stopped writing minutes ago and the pen is still
  moving.
- **Othren's** officers file purchase-requisitions in triplicate
  for one measure of salt, annually. Year three, the requisition
  carries a second signature line, unexplained: "[reviewed]." Your
  shop has a file now. The file says: *continuing.*
- **Dohren** cannot come. The deep villages buy on his dream's
  instruction — "the light wants dark-wicks this month" — and once
  a decade, every catacomb order on one night is the same single
  item, and no Tender will say what the patch is dreaming, and the
  item is never anything alarming, and that is somehow worse.

### The standing invitations

- **A mouthpiece** becomes a regular: pays triple, in scraped coins
  (underread them at your peril), always buying one category —
  *things with names on them*. Serving it is legal, profitable, and
  feeds the gradient; the arc ends when the player either bars it
  (a spoken refusal to something that cannot want) or follows the
  coins. Pressure-model discipline throughout: the hunger has no
  face; the *courtesy* is all the mouthpiece's own, which is the
  unsettling part.
- **The Branchwork rents a shelf.** Payment: routes. Give the
  world's one disinterested mind a nutrient tray in the corner and
  it becomes your supply-chain consultant — restock paths, caravan
  timing, once a warning ("do not order honey this month") that it
  declines to explain and that proves correct. The slime mold is
  the best employee the player will ever have, and it asks only
  lunch.
- **The Spirits browse.** Inquiry asks about every item and buys
  nothing (answering patiently raises affinity — the shop as a
  Spirit-shrine nobody built on purpose). Apatheia stands in the
  corner for an afternoon, purchasing nothing, and the whole day's
  customers linger longer and haggle less. Bloodlust never comes
  inside. It waits, politely, by the weapons rack you keep meaning
  to move indoors.
- **The dead shop by proxy.** Plaque-Tenders relay orders from the
  niches: "Grandmother, third row, wants the river told toward her
  wall — and she left credit." Memorial goods, delivered down, on
  standing accounts opened by people who died before your
  grandmother was born and *kept in good standing since*. The
  catacomb ledger pages are the oldest accounts receivable in the
  world, and the villages pay them without irony, and so will you.

## XII. Strange money

- **Memory-payment** (Choir-brokered): a customer pays with the
  memory of the item's origin — provenance transfers to the player
  as *knowledge* (KnowledgePart), and knowledge is margin at the
  curiosity counter (§II.E). The Choir takes its brokerage in the
  usual coin: a little more of the customer.
- **Word-barter:** a customer who cannot pay teaches you a **true
  word** — an old-world name for a thing you stock. Items sold
  under their true names bind a little better (the charm family
  gets a quality tier that literally cannot be bought, only
  bartered). The Recension will pay handsomely for the words; the
  Namers will pay more attention.
- **The tab is a binding.** Extending credit is a promise with
  physics: a defaulted tab manifests as a hairline Slip at the
  defaulter's threshold — nothing dangerous, just *noticeable*,
  and known, which is why village tabs are paid. The player's own
  supplier tabs obey the same law. (Design note: this is the
  closure-ledger's teeth, made municipal.)
- **Warm coins.** Some coins arrive warm. The Shopkeeper's Rules
  (§XIII) address this. The Rules do not explain it. Neither do we.

## XIII. The Shopkeeper's Rules — folk-horror retail

Every trade-culture in the world runs on procedure; the player's
shop inherits a hand-me-down rule-sheet from the previous keeper
(via the Under-Shop bleed, or a Concord starter-pamphlet, or a
villager aunt — origin varies by shop model). The rules are
half-superstition and half load-bearing, and the game *never marks
which half is which* — some rules, tested, are just lint; one or
two are the Sealed Dim in retail clothing. Draft artifact
*(ungated)*:

> **THE KEEPER'S RULES** *(as passed down; annotations in three
> different hands)*
> 1. Open with the sun or a lamp, never with neither.
> 2. First customer of the day buys cheap. Argue with the custom,
>    not the customer.
> 3. If the coins are warm, count them twice. If they are warm the
>    second time, the price was wrong. Not the coins. The price.
> 4. The till answers to its name. Never teach it another.
> 5. Sell nothing after the third dim. *(second hand: "nothing" is
>    strong — bread is fine)* *(third hand: bread is not fine. Ask
>    Wett's boy. You can't. That's the point.)*
> 6. A customer who asks for an item twice in the same words is not
>    asking twice. Wrap it quickly and do not touch their hand.
> 7. Sweep toward the door. Everything you sweep toward the counter
>    stays in the shop. Everything.
> 8. *(scraped — first-position knife-work)*
> 9. Greet the shop when you open it and thank it when you close
>    it. This one is not superstition. This one is the whole trick.

Rule 9 is §XIV.

## XIV. The genius loci — the shop grows a soul

Canon: numina pool where the old writing pools; meaning accretes.
A **named, tended, greeted place** where promises are kept daily is
exactly the kind of vessel small presence collects in. Over a long
game, the shop develops — not a ghost, not an NPC — *disposition*:

- Stage 1 (quiet): the door sticks for people the shop has reason
  to dislike. The till drawer opens a half-second before you reach
  it, on good days.
- Stage 2 (habits): items the shop "approves of" don't gather dust;
  the display window resists arrangements Ylaes would score poorly
  (the shop has *taste* now, and it is derivative, and the Seer
  finds this hilarious).
- Stage 3 (one opinion): the shop becomes capable of exactly one
  small opinion, expressed once — the canonical numen scale, "too
  incoherent to be a god, too place-bound to be Urqu." What the
  opinion is depends on the whole game's conduct at that counter
  (the closure record, the no-smith jobs, the shelf of the
  unfinishable). It might be about a customer. It might be about
  you. The shop says it the only way a building can — a door held
  open, or held shut, at one load-bearing moment — and then it is
  just a shop again, forever, and you will keep greeting it anyway.
  *(Rule 9.)*

## XV. The shop and the endings

- **Consume:** the Choir offers a consignment corner — their goods
  never run out and are never restocked by anyone you see. Late
  Thinning, the corner's offer sharpens: the whole shop, absorbed
  as a *remembered place* — "we will keep it exactly. Every shelf.
  Every kept promise. There is room." The kindest buyout offer in
  history.
- **Preserve:** the Curation offers to file the shop *as is* — a
  perpetual heritage designation: "Status: continuing." Nothing may
  ever change, be sold, or be moved again. It is meant with total
  sincerity as the highest honor they can pay a place, and it is a
  death sentence with a plaque, and refusing it aloud is one of the
  game's great small scenes.
- **The practice-path:** Saedis, visiting, points at the ledger and
  declines to say anything grand — but at the Staking, if the
  player's shop has a deep closure record, **the ledger is
  admissible**: the world's fate argued, in part, from a grocery
  book in which every no was spoken and every order was filled or
  refused and nothing — check the margins — was ever left to
  dangle. The humblest exhibit at the end of the world. This is
  the whole project's thesis (the flowers, the small acts) with a
  till.
- **The free counter** (any path): a player may, at any point, stop
  charging. The gift-economy shop is economically ruinous,
  cosmologically pristine (every gift closes), doctrinally
  catastrophic for the Concord — and Tovreth *must* come see it,
  because "nothing is free" is his function's floor, and your
  counter is standing on it. What he does there depends on his arc.
  The strongest version: he buys one item, insists on paying,
  and the player's refusal — *aloud, formal, complete* — is the
  only spoken no in the game addressed to the god of Exchange. The
  ledger records it as a closure. His face records it as something
  the design does not caption.

## XVI. Grafting order (what strangeness costs)

| Idea | Rides on | Cost |
|---|---|---|
| The no-smith | dialogue + storylets | **cheap ★** — build first |
| Shopkeeper's Rules | flavor + a few event triggers | cheap ★ |
| God-customer vignettes | scripted events, arc-gated | mid, one at a time |
| Strange money (word-barter, memory-pay, tab-binding) | KnowledgePart + closure-ledger | mid |
| The Under-Shop | premises (v2) + Phase-12 underread hooks | mid-heavy, flagship of v2 |
| Dead-customer standing accounts | catacomb content | mid |
| Branchwork shelf / Spirit browsing | existing actor systems | mid |
| The unfinishable shelf | display slots + storylets | mid |
| Genius loci | long-arc world-state | v3; write nothing until stages 1–2 can be felt |
| Ending integrations / the ledger at the Staking | ending scenes | rides V1-DramaticCore work |

The v1 slice (§VIII) stands — but adopt the **no-smith** and the
**Keeper's Rules** into it immediately: they are nearly free, and
they are the difference between "a shop opened in the world" and
"a shop that could only have opened in this one."
