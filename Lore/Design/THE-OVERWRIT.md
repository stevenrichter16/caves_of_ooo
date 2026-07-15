# The Overwrit

> **Authority:** Canonical design document (Phase 12, M12.4 — spine
> item C1). The scraped region: the game's under-text showcase and
> the sanctioned home of strata-bleed content. Extends the Phase-2
> place-network; Strangeness Tier 4.

---

## I. What it is

Canon holds that the Great Manifestation (Gen 25, ~year 750) — the
worst near-incarnation in recorded history — "unmade a region."
Phase 12 commits the sharper truth: **the region was scraped.** A
season-long failed birth of the seventh god pressed the knife across
a whole landscape, and what is left is what a scraped page is: not
empty. Faint.

**The Overwrit** (the deep villages' word; the Recension files it as
*the Blank Quarter*, which every field scribe knows is wrong twice)
is that region, three generations on. Nobody resettled it. The roads
around it are good roads — traffic bends away from the place the way
handwriting bends around a hole in the vellum.

## II. What it is like

**Normally:** near-blank terrain with wrongness-of-absence — ground
too smooth, growth too sparse and too new, no ruins where ruins
should be (that is the horror: a region this old should be *full*;
it has been *prepared*). The *sari... sari...* is louder here than
anywhere but the Felling-Site. Travelers describe the same sensation
in the same words without coordinating: *the feeling of being
somewhere a sentence used to be.*

**At thresholds** — certain hours, certain weathers, Strangeness
spikes, and (rarely, world-state-driven) Thinning surges — **the old
letters bleed through.** A road resolves underfoot. A well. A town
square with its name almost readable on the trough. The shallow
bleeds are the pre-Manifestation towns — the scraped layer's most
recent writing, and the most nearly legible; survivors' descendants
sometimes make the trip on the right evening to stand where a
grandmother's door faintly is. (The Catchers' founding fields are
here. This is the place that taught them to throw the salt early,
and Catcher cells still walk it on the anniversary, and on that
night they catch nothing and speak to no one.)

**The deep bleeds** are older layers, and they fit no record at all:

- a marshy **bookbinder-village** where no marsh is, its drying-lines
  hung with pages nobody can approach before the bleed closes;
- a **shore** — wet stones, weed-smell, the sound of water on a
  beach — where no sea is or has ever been;
- a **kiln-yard of unfired vessels**, rows and rows, none ever
  fired, arranged as if their makers meant to come back.

*(Authoring policy, per the amended under-text rules: these are
fragments, hand-placed, never labeled, never mapped, never explained
— no NPC exists who can identify them, including the gods; Maeleth's
entry on the deep bleeds reads, in full, "prior." Designers: these
draw from the repo's superseded strata as texture, and the game must
never know that.)*

## III. Underreading here

The Overwrit is the practice's proving ground (`UNDERREADING.md`
§III): unaided travelers get flickers; an underreader can **hold** a
bleed — steady it long enough to see it whole, and at mastery to
walk in it briefly. Holding is effortful, costed, and ends — always
— with the bleed closing from the edges inward, which is exactly as
scraping proceeds, and the first time a player notices that, the
place has done its job.

What can be brought out of a held bleed: what you read, what you
heard, and small things that were *written* rather than made — a
page from a drying-line, once; never twice from the same line. (Loot
discipline: bleed-yields are knowledge and singular curios, never
farmable resources.)

## IV. Mechanic sketch (engineering handoff)

- **Layered zone-states:** the region's zones carry 2+ authored
  variants (surface / bleed layers). A **bleed mask** (per-cell
  bitfield) controls which layer renders per cell; reveals are
  event-driven (threshold triggers, hold-actions), applied via
  `ZoneRenderHooks.MarkCellDirty` per changed cell — no per-frame
  work, per PERF-FOUNDATION.
- **Thresholds:** data-driven trigger table per zone (hour band,
  weather tag, Strangeness events, world-state flags). Holding =
  a channel-style action extending/stabilizing the mask locally.
- **Diag:** `BleedRevealed` / `BleedHeld` per `UNDERREADING.md` §V,
  plus `kind=BleedClosed` with a `cause` field.
- **Content budget (v1):** one hub-adjacent entry zone, two interior
  zones, one shallow-bleed town (authored), the three deep-bleed
  fragments (§II) as single-cell-to-few-cell set pieces. The region
  reads as *large* by rumor and map-gap, not by zone count.

## V. Do-nots

- No map of the under-town(s); no quest ever requires a specific
  deep bleed (shallow bleeds may carry quest content — the Catcher
  anniversary, the survivor-descendant pilgrimages).
- No procedural bleeds (amended policy rule 4).
- The deep-bleed fragments never speak, never contain NPCs, never
  persist after closing. If a future design wants an inhabitant of
  the under, that is a Mystery Ledger amendment discussion, not a
  content decision.
- The region never grows back. No ending re-writes the Overwrit —
  not even the practice-path (renewal writes *new* pages; it does
  not restore scraped ones; grief that is finished is still gone).
  The epilogue difference is small and telling: under Renewal,
  people finally build at its *edge*, facing it.
