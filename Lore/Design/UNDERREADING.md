# Underreading

> **Authority:** Canonical design document (Phase 12, M12.3 — spine
> item D1). The practice that unifies the palimpsest layer under one
> verb. In-world vocabulary per `TERMS.md`: *the under, a bleed,
> scraped, to underread* — workaday words; nobody in the world says
> "palimpsest magic."

---

## I. What it is

**To underread is to recover what was scraped.** Cosmologically
(Bible §IV, Phase 12): scraping never removes everything — the ghost
of the writing stays in the fibers of the page, the stone, the place.
Underreading is the trained attention that raises the ghost far
enough to read. It is not un-scraping: nothing is restored to the
surface; the reader carries away what they read, and the under stays
under.

Within Phase 9's magic frame it is a **binding-art of the everyday
end** shading deep with mastery: attention, patience, and method —
a hum, a raking light, a wetted thumb — not fireworks. The deepest
uses are costly the way deep binding is costly.

## II. What can be underread (v1 scope)

| Target | What recovery yields | Tier |
|---|---|---|
| **Documents** (the vellum economy — nearly everything is written over something) | the under-text: older letters, scraped clauses, smuggled messages | basic; the bread-and-butter use |
| **Surfaces** (plaque-blanks, way-markers, walls) | ghost-inscriptions; *always* with the moral edge (§IV) — many were scraped on purpose, by verdict or by grief | basic-mid |
| **Bleed-sites** (the Overwrit; hand-placed Tier 3+ sites) | holding a bleed stable long enough to see it whole — and, at mastery, to *walk in it* briefly | mid-mastery |
| **The First Account** | the capstone (`FIRST-ACCOUNT.md`); one recovery, once | mastery + arc |

**Deferred beyond v1** (explored, not canonized): underreading
*people* — the Unsaid recovered as conjectural emendations of
themselves (integration study §A4). The dread-preserving rules are
already drafted there; do not ship person-recovery without them.

## III. Who teaches it

- **The Recension** teaches document-underreading — *grudgingly.* It
  borders their taboo: the order that never corrects old hands is
  training people to disturb old scrapings, and every senior teacher
  opens the first lesson with the same sentence: "Before I show you
  how, you will learn to ask whether." Their curriculum is
  method-heavy, consent-heavy, and terrified of itself.
- **The Namers (Tent-Right)** teach the surface-and-place tier —
  naming what was un-named is their whole theology, and underreading
  a scraped way-marker aloud is, to them, a small act of the
  practice-path (a name returned is a name enacted).
- **The Overwrit teaches the rest itself** (`THE-OVERWRIT.md`).
  There is no instructor for holding a bleed; there is standing in
  one, failing, and standing in the next one longer. The deep
  villages call self-taught underreaders *thumb-wet* — it is not a
  compliment and they wear it as one.
- **Spirit affinity: Inquiry**, obviously — with the cruelty caveat
  fully live: underreading a person's scraped grief without consent
  is a canonical cruel-knowing storylet. Knowing is not kindness.

## IV. The moral edge (load-bearing; never soften)

Every underreading undoes a **deliberate forgetting**, and the world
is full of deliberate forgettings with owners:

- The chiseled plaque-blank was a **court verdict** (`Codex/05`) —
  underreading it in a catacomb village is a crime against the
  wall, and the Tender will say so with a lamp in her hand.
- The knife-tithe scrapings are **sacrifices** — a scribe's
  surrendered self; underreading a colleague's tithe is the
  Recension's ugliest breach of trust.
- Maeleth's two scrapings are **a god's mercy or a god's crime**,
  and recovering them is the endgame precisely because the question
  "should this be read?" has no floor under it.

Mechanically: underreading in witnessed contexts carries reputation
consequences keyed to *whose* scraping it was; the practice's
teachers refuse students who ask "how" before "whether" (a small
dialogue gate that states the theme once and never again).

## V. Engineering notes

- **Documents:** an `undertext` field on readable items
  (KnowledgePart pattern); two read-states; underreading reveals
  state two. Cheapest possible implementation of the whole theme —
  ship this first.
- **Surfaces/bleeds:** interactable + zone-variant hooks per
  `THE-OVERWRIT.md`.
- **Diag** (per the observability rule — decided before code):
  `category=undertext`, kinds `Underread` (target, targetKind,
  ownerFaction, witnessed), `UnderreadRefused` (gate + reason),
  `BleedRevealed` (site, trigger), `BleedHeld` (site, durationTurns).
  Success and failure paths both emit, as always.
- **No new architecture:** the practice is KnowledgePart + status
  effects + zone variants + reputation, all existing systems.

## VI. Do-nots

- No "underreading skill level" number surfaced to the player;
  progression is by taught tiers and arc gates (RPG framing).
- Recovery output is never system-voiced ("You learn the truth!");
  it is always *text*, in the voice of whatever hand wrote it.
- Never a mass-use tool: bleed-holding stays rare and effortful;
  if playtests show players underreading every wall by rote, add
  friction, not content.
