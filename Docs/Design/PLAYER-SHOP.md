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
