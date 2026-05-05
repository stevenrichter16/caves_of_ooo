# Simplifying the Entrance — Plan

> **Status:** Plan-to-disk. No code changes yet. Sub-milestones are sized
> for one-commit-each delivery.
>
> **Origin:** Realization that the Palimpsest corpus is dense at every
> layer, with no surface a casual reader can land on. Qud is dense at the
> bottom but legible at the top; the audit-driven Palimpsest corpus
> currently has no comparable top.

---

## Goal

Add a **legible surface** to the Palimpsest lore corpus — an entrance any
new reader can walk through in five minutes — without modifying or
deleting any existing material.

The surface must:

1. Communicate the world's shape (cataclysm, two cosmic forces, five
   factions, central choice) using image-words and concrete examples.
2. Use only language a reader-with-no-genre-context can parse.
3. Invite — not require — descent into the deeper layers (audits,
   compendium, graph, bible).
4. Define every deep-corpus term it references on first use, with a
   casual label as the default and the deep term as a parenthetical.
5. Pass the **adversarial reader test** below.

---

## Non-goals

The following are explicitly out of scope and remain untouched:

- `Docs/Lore/Palimpsest_Lore_Bible.md` — the canonical source of truth.
- `Docs/Lore/audits/*-lore-audit.md` — the 22 timestamped audits are
  immutable per the audit cadence rules (`audits/README.md`: *audits are
  not deleted or rewritten retroactively*).
- `Docs/Lore/audits/ROUTINE_PROMPT.md` — the routine prompt continues
  producing Tier-3 audits, unchanged. (One small exception in S6 below.)
- `Docs/Lore/Palimpsest_Lore_Compendium.md` — the editorial monolith for
  the obsessive reader. Not edited.
- `Docs/Lore/Palimpsest_Lore_Graph.html` and `Qud_Lore_Graph.html` — the
  visualisations. Not edited.
- `ROTCHOIR_VOICES.md` and `Assets/Resources/Content/Conversations/RotChoir.json` —
  shipped game content. Not edited.

This plan **adds files only.** Quality is preserved by leaving the deep
work where it is and constructing a separate accessible entrance.

---

## Verification sweep (per CLAUDE.md §1.2)

Before writing surface prose, confirm these references — quickly, against
this conversation's reading of the corpus, but flag any drift:

| Claim | Source | Status |
|---|---|---|
| "The cataclysm has six in-fiction accounts and the duration has six readings." | Audits 1813, 1901, 2127, 2225, 2306, 2310 | ✓ confirmed (six factions × six durations table in compendium Part I) |
| "There are five factions plus two cosmic entities (seven total in the bible's expanded roster)." | Audit 2225 §4(b) committed Glassblown as the fifth bible-canonical faction | ✓ confirmed |
| "The doll-in-the-wall artifact (11-K-3318) is the corpus's clearest concrete object-choice." | Audit 1813 §3, Letter to Halen (1901 §5) | ✓ confirmed |
| "Mira is named through Palimpsest time-release annotation in her own cookbook." | Audit 2220 §5 | ✓ confirmed |
| "The junior cataloguer A. wrote the original doll-margin-note." | Audit 1813 §3 | ✓ confirmed |
| "A bowl-maker is the most ground-level Brine character and walks roads." | Audit 1301 §3 | ✓ confirmed |
| "Mira (smoke-born apprentice) is the most accessible Thermoclave voice." | Audit 2326 §5 | ⚠ name collision: this Mira is a different person from Curation Halen's mother Mira. Use a different name in the surface — e.g., "the apprentice in Tossin's hearth-log" or invent a fresh name. |

The Mira-collision flag is the only correction. The simplification can
proceed with a renamed Thermoclave apprentice — call her **Renne** (already
referenced as a hearth-log writer in the corpus) or **a smoke-born
apprentice**, plain.

---

## Constraints (immovable)

1. **Image-first, jargon-second.** Every deep term gets a casual label
   on first use. Casual label is the default in narrative prose; deep
   term is a parenthetical or a footnote.
2. **No new canon.** Surface prose is a *retelling* of existing material,
   not new lore. Every claim in surface prose must trace to a bible line,
   audit, or compendium passage.
3. **Three tiers must be visibly named.** Surface (Tier 1), Player (Tier
   2), Obsessive (Tier 3). The reader should be able to tell which tier
   they're reading.
4. **The five faction taglines** drafted in the brainstorm are
   provisional; refine them in S2 against the bible/compendium directly.
5. **The starter NPCs are inventions** *only* in name and ground-level
   role; they must not contradict any existing audit or bible line.

---

## Deliverable layering

```
Tier 1  (5 min read)   ← S1 PRIMER, S5 README points here first
Tier 2  (30 min read)  ← S3 FACTION ONE-PAGERS, S4 STARTER NPCs
Tier 3  (hours)        ← existing audits, compendium, graphs, bible
```

S1 lands the surface. S2 builds the glossary that lets every other piece
use plain language consistently. S3 unfolds each faction. S4 gives every
faction a face. S5 ties the index together. S6 protects the surface from
audit-cycle drift.

---

## Sub-milestones

Six commits, each independently shippable. Smallest blast radius first.

### S1 — The Glossary

**Goal:** Reference table mapping every deep-corpus term to a casual
label, with a short explanation and one example sentence per pair.

**Output:** `Docs/Lore/Palimpsest_Glossary.md` (new file, ~1500 words).

**Content sketch:**

```
# Palimpsest Glossary

> Two columns. Left: the deep term as it appears in the bible / audits /
> compendium. Right: the casual label used in surface prose. Both are
> "correct"; the deep term is technical, the casual label is everyday.
> Pick whichever the situation needs.

## Cosmic-scale
| Deep term         | Casual label              | Notes |
|-------------------|---------------------------|-------|
| The Choir         | (kept)                    | "Choir" is already image-led. |
| The Palimpsest    | the layered world         | "Palimpsest" needs a one-line gloss on first use. |
| The substrate     | the world's foundation    |       |
...

## Apotheoses (end-states)
| Communion         | becoming-the-Choir        | |
| Transcription     | becoming-a-memory         | |
| Closure           | becoming-unledgered       | |
...

## Faction grammars
| The catalogued mood     | the on-record voice (Curation) | |
| The unsaid mood         | the silence-voice (Glassblown) | |
| The trade tense (-up-)  | closed/open voice (Concord)    | |
...

## Apotheosis-shaped middles
| The Long Throat   | the slow-voice (Choir middle)        | |
| The Re-pressed    | the layered reader (Palimpsest mid)  | |
| The Doubled Mouth | the two-voice (both at once)          | |

## Substrate strata
| Material stratum   | (kept)                       | |
| Temporal stratum   | the layered tense's ground   | |
| Held depth         | the still place (Brine)      | |
| Ember-floor        | the warm rock (Thermoclave)  | |
...

## Artifacts
| Sleeve R-7         | the founders' folder         | |
| The Six-Almost     | the Six Refusals             | |
| Cabinet 7's first-shapes | the four warm vessels  | |
...
```

**Acceptance criteria:**
- Every deep term that appears more than three times in the audits is
  in the glossary.
- Every casual label is verifiably parseable by a reader who has not
  read any audit.
- No casual label contradicts the deep term's meaning.

**Counter-check:** for each entry, write a one-sentence example *using
the casual label* and check that a reader of just that sentence can
predict the *shape* of the deep concept. If they can't, the casual label
is too lossy.

**Adversarial test:** show the glossary to someone who has not read the
audits. Ask them to define five random deep terms using only the casual
label. They should produce something *consistent* with the deep meaning
even if not complete.

**Risk:** scope creep — every deep term tempts a long explanation. Cap
each row at one sentence of notes.

---

### S2 — The Primer

**Goal:** A single 250–500 word document a new reader can finish in five
minutes and walk away with the world's shape.

**Output:** `Docs/Lore/Palimpsest_Primer.md` (new file).

**Content sketch:**

```
# A Primer to the Palimpsest

> The five-minute introduction. Read this first. Everything else is
> deeper.

## What happened
[3 paragraphs: cataclysm as overwriting, four hundred years ago,
six-faction disagreement as invitation]

## Two cosmic forces
[2 paragraphs Choir + 2 paragraphs Palimpsest, image-led, no jargon]

## Five cultures
[Five faction taglines + one image-paragraph each. Use casual labels
from S1.]

## The choice
[1 paragraph: the doll-in-the-wall as concrete object-choice, with
the philosophical question in the closing line.]

## Where to go from here
[A pointing-list to Tier 2 (faction one-pagers, S3) and Tier 3 (audits,
compendium, graph). Frame as invitations.]
```

**Acceptance criteria:**
- 250–500 words total.
- Zero deep terms appear without a casual gloss.
- Reader-without-genre-context can summarise the corpus in two sentences
  after reading.
- Every claim traces to bible / audit / compendium.

**Counter-check:** read the primer aloud. If a sentence requires a
glossary lookup to follow, rewrite.

**Adversarial test:** give the primer to a reader. Ask them, without
referring back, *what each of the five factions cares about* and *what
the central choice is*. They should get all five within ~80% accuracy
and the choice exactly.

**Risk:** the primer becomes a bullet-list of compressed facts rather
than a *short essay*. Resist. Prose, not bullets.

---

### S3 — Faction One-Pagers (×5)

**Goal:** One ~500-word document per faction, accessible but with some
texture. Tier-1.5.

**Output:**
- `Docs/Lore/Factions/PaleCuration.md`
- `Docs/Lore/Factions/SaccharineConcord.md`
- `Docs/Lore/Factions/BrineCommunion.md`
- `Docs/Lore/Factions/Thermoclaves.md`
- `Docs/Lore/Factions/GlassblownRemnant.md`

(Optionally `Docs/Lore/Factions/RotChoir.md` and `ThePalimpsest.md` —
these are cosmic, not factional, but new readers will look for them.
Include for completeness.)

**Content template (~500 words each):**

```
# [Faction]

> [Tagline — one image-led sentence. From S1's glossary.]

## What they are
[1 paragraph. Plain English. Image-first.]

## What they believe
[1 paragraph. Their substrate-claim or state-claim, in everyday terms.
Faction grammar mentioned as "they have a way of speaking" not as
linguistics.]

## A starter NPC
[1 paragraph. Names a young, ground-level person of the faction —
introduced in S4 — that a player would meet first.]

## Where they live
[1 paragraph. Their place(s).]

## Going deeper
[A pointing-list: the senior figures, the artifacts, the apotheosis,
linked into the compendium and audits. "If you want to know more about
the founder's contract, see…"]
```

**Acceptance criteria per faction:**
- 400–600 words.
- One starter NPC named (cross-link to S4).
- Tagline at top.
- "Going deeper" section lists 3–5 entry points into Tier 3.
- Reader can summarise the faction in two sentences after reading.

**Counter-check:** for each one-pager, ask: *if the player only ever
reads this page about this faction, do they know enough to interact
with NPCs of that faction without confusion?* If no, the page is too
shallow. If the page assumes terms from the glossary that aren't yet
casual-labelled, it's too deep.

**Adversarial test:** give all five one-pagers to a reader. Ask them
to assign each to a real-world cultural archetype. They should produce
distinct archetypes (e.g., archivists / merchants / sea-people /
fire-tenders / silent-craftspeople). If two pagers produce the same
archetype, one is too shallow.

**Risk:** the seven faction-pages diverge in tone and structure across
the writes. Mitigate by writing one full template-pass first
(Glassblown is a good start — most concrete), reviewing it, then
applying the same template to the other four.

---

### S4 — Starter NPCs (the seven faces)

**Goal:** One young, ground-level, *accessible* person per faction the
player can meet and learn the world through. Each NPC ~150 words.

**Output:** `Docs/Lore/StarterNPCs.md` (single file, all seven).

**Per-NPC template:**

```
## [Name], [role/title]

> Faction: [faction]
> First met: [where the player would encounter them]

[~100 words: physical description, voice, what they do day-to-day,
why they matter as an entry point. End with one line they typically
say to a stranger.]

[~50 words: what they offer the player — a small quest hook, a
trade, a story-piece. Optional.]
```

**The seven (proposed):**

1. **A.**, junior Curation cataloguer. Already exists in the audits
   (the original doll-marginalia author). Promote him to faction-face.
2. **(Concord) Brem the apprentice**, learning the Reckoner's chair.
   Already cameo'd in audit 2216 §5 as a working trader's name; can be
   reused or invented fresh.
3. **(Brine) A bowl-maker** — the inland sub-population that walks
   roads gathering vessels. Already in audit 1301 §3.
4. **(Thermoclaves) The smoke-born apprentice in Tossin's hearth-log**
   — recast under a non-conflicting name (NOT Mira; she is Curation
   Halen's mother in the cookbook). Use **the apprentice** generically
   or pick a fresh name.
5. **(Glassblown) An *unfired* child** — Glassblown name for an
   un-shape-named child. The pre-name. Audit 2150 §7 names the
   convention.
6. **(Choir) A new tendril** — a Choir surface presence not yet a Long
   Throat. Audit 2326 §6 implies these exist.
7. **(Palimpsest) A first-time Echo-Reader** — surprised by the
   second-shadow at the edge of vision. Audit 1101 §7 describes the
   Re-pressed morning; the first-time reader is a younger version.

**Acceptance criteria:**
- Each NPC has a name (or a clearly intentional non-name like *the
  bowl-maker*).
- Each NPC has a one-line catchphrase the reader can quote.
- Each NPC's voice fits their faction's grammar (mention only, not
  full grammar).
- No NPC contradicts an existing audit or bible line.

**Counter-check:** for each NPC, identify which audit / bible passage
they are extrapolated from. If the NPC has no source, demote to "named
in this surface only" and flag.

**Adversarial test:** show the seven NPCs to a reader cold. Ask them
to assign each to a faction. They should get 6/7 right based on the
NPC's voice and concerns alone.

**Risk:** the NPCs become canon-by-stealth. Mitigate by clearly marking
them as **surface-introduced** (a "Tier 1 inventions" footer) so a
future audit knows they are accessible-faces, not bible-canon.

---

### S5 — The Index / README

**Goal:** A single entry-point document that points new readers at the
primer first, interested players at the one-pagers, and the obsessive
at the audits/compendium/graph.

**Output:** `Docs/Lore/README.md` (new file, ~300 words).

**Content sketch:**

```
# Lore

The world's lore lives in three layers. Pick where you want to land.

## Tier 1 — Five minutes
- Palimpsest_Primer.md  → what the world is, in 250 words.
- Palimpsest_Glossary.md → casual labels for the deep terms.
- StarterNPCs.md → the seven people you'd meet first.

## Tier 2 — Thirty minutes
- Factions/*.md → one page per faction, with image and texture.

## Tier 3 — Hours
- Palimpsest_Lore_Bible.md → the canonical source.
- Palimpsest_Lore_Compendium.md → the editorial monolith (40k words).
- Palimpsest_Lore_Graph.html → the interactive force-directed atlas.
- audits/*.md → the 22 audit reports that built the corpus.
- Qud_Lore_Graph.html → comparison-graph for Caves of Qud lore.

## How the lore was made
[One paragraph: the audit cadence, the bible-as-source-of-truth,
audits as suggestion-documents, the layered structure.]
```

**Acceptance criteria:**
- Every existing file in `Docs/Lore/` is reachable from this README.
- Reader who lands on the README can navigate to whichever tier they
  want without confusion.

**Counter-check:** check the file system. Every `.md` and `.html` in
`Docs/Lore/` is linked.

**Risk:** the README accidentally becomes its own primer. Stop it from
ballooning past ~300 words.

---

### S6 — Audit-prompt amendment

**Goal:** Tell the audit routine the surface exists, so future audits
don't accidentally rewrite the surface or conflict with it.

**Output:** Edit `Docs/Lore/audits/ROUTINE_PROMPT.md` — add ONE
paragraph to INPUTS section.

**Proposed text:**

> The corpus now has a Tier-1 surface at `Docs/Lore/Palimpsest_Primer.md`,
> a glossary at `Docs/Lore/Palimpsest_Glossary.md`, faction one-pagers
> in `Docs/Lore/Factions/`, and a starter-NPC list at
> `Docs/Lore/StarterNPCs.md`. Read these on each run for orientation.
> The audit may *propose* refinements to surface text but should never
> directly rewrite it; surface text is maintained by hand to preserve
> accessibility.

**Acceptance criteria:**
- The audit-prompt change is one paragraph in the INPUTS section.
- No other audit-cycle rules change.
- Future audits can reference the surface without confusion.

**Counter-check:** run the existing audit prompt mentally with the new
paragraph included. The audit should still produce Tier-3 deep work
without trying to overwrite the surface.

**Risk:** the audit prompt becomes the wrong place to define
surface-stewardship rules. Mitigate by keeping the addition to one
paragraph and pointing at the README for governance.

---

## Order of operations & dependencies

```
S1 GLOSSARY  ───────┐
                    │
                    ├──> S2 PRIMER ────┐
                    │                  │
                    │                  ├──> S3 FACTION PAGES ────┐
                    │                  │                          │
                    │                  │                          ├──> S5 README
                    │                  │                          │
                    └──────────────────┴──> S4 STARTER NPCs ──────┘
                                                                  │
                                                                  └──> S6 AUDIT PROMPT
```

S1 unblocks S2 (the primer needs the glossary's casual labels). S2 and
S3 can be done in parallel after S1; S3 produces 5 (or 7) one-pagers,
each independently committable. S4 depends on S3 (one-pagers reference
the NPCs). S5 depends on S1–S4. S6 is last — it acknowledges the
surface only after the surface exists.

Each sub-milestone is one commit. Total: ~6 commits (or more, if S3 is
split per-faction).

---

## Per-milestone self-review (RED → GREEN → adversarial)

Per CLAUDE.md §1.4, each sub-milestone should:

1. Phrase the invariant in user-visible terms.
2. Write the surface text against that invariant.
3. Run the counter-check (above).
4. Run the adversarial test (above).
5. Self-review with severity markers (🔴/🟡/🔵/🧪/⚪).
6. Fix 🟡+ findings before commit.
7. Commit using the §2.3 template.

The "RED" state for surface lore is: *can a fresh reader walk through
this without bouncing off?* If they bounce, the surface is RED.

---

## What to bury vs. what to surface

(Restating from the brainstorm for clarity.)

**Surface (S1–S5):**
- Cataclysm as three paragraphs.
- Choir + Palimpsest as two paragraphs each.
- Five faction taglines + one image-paragraph each.
- One concrete object-choice (the doll-in-the-wall).
- Seven starter NPCs.
- A glossary that translates jargon to image.

**Mid-depth (already exists in compendium / audits):**
- Substrate-claims in image-form.
- Each grammar with one example.
- Three apotheoses out of nine, mentioned by name.
- The Long Throat / Re-pressed / Doubled Mouth seam.
- The Veshen ↔ Suul ↔ Nesh triangle.

**Buried (Tier 3, in audits / compendium / graph):**
- Full grammar specifications.
- All seven outsider-name tables.
- All nine apotheoses with mechanics.
- All artifact catalog numbers.
- The full senior-figure roster.
- The methodology itself.

---

## Risks

| Risk | Mitigation |
|---|---|
| Surface text drifts from canon over time. | S6's audit-prompt amendment; periodic re-read by a human steward. |
| The simplified prose accidentally introduces new lore. | S4's "Tier 1 inventions" footer. Verification-sweep section in this plan flags any drift before each commit. |
| Tone diverges between surface authors. | One template per artifact-type (S3 faction one-pager template, S4 NPC template). Write one example pass first; refine; then apply. |
| The deep-term advocates object to casual labels. | The deep terms are not deleted. Both forms appear; the casual is default in narrative, the deep is default in linguistics-deep sections. |
| Six commits is too many before any value lands. | S1 (the glossary) is independently useful even before S2 ships. Each subsequent commit also lands incremental value. |
| The Mira-name collision (Curation Halen's mother vs. the smoke-born apprentice) creates confusion. | Verification-sweep catches this. S4 uses a different name for the apprentice. |
| New readers still bounce because the world is genuinely strange. | Acceptable — strangeness is the point. The fix is *legibility of the surface*, not removing the strangeness. The Glassblown will still be silent. The Choir will still be slow. |

---

## What this plan does *not* solve

- The Compendium is still 40k words. Readers who land there are reading
  Tier 3. That's intentional.
- The audit corpus is still in audit-prose. Future audits will continue
  in that register. That's intentional too — audits are for the
  obsessive.
- The bible itself is not amended by this plan. (Audits 0845, 1001,
  1101, and 1201 propose four bible amendments; that's a separate piece
  of work, tracked elsewhere.)

This plan adds a surface; it does not refactor the depth.

---

## Acceptance criteria for the whole effort

After all six sub-milestones:

1. A reader who has never seen the corpus can read the primer and write
   two sentences about each faction with ~80% accuracy.
2. Every term in the audits' first 50 lines has a casual label in the
   glossary.
3. The seven starter NPCs are clearly *introduced by* the surface and
   *not* canonised in the bible.
4. A reader can follow the Tier 1 → Tier 2 → Tier 3 path without
   confusion.
5. Future audits can run without breaking the surface.

---

## Open questions for the steward

- Should the seven starter NPCs become actual conversation files in
  `Assets/Resources/Content/Conversations/` over time? (Not in this
  plan's scope; flag for future.)
- Should the surface text be translated into in-fiction artifacts
  later — e.g., a "Walker's Almanac" written by a junior cataloguer?
  (Tempting; out of scope here.)
- Should the surface include a small map? (Not currently; the corpus
  doesn't have a canonical map, and inventing one is a separate piece
  of work.)

---

*Plan filed 2026-05-04. Six sub-milestones; ~10 new files; zero edits
to existing canon. Smallest blast radius first; one commit per
milestone; deep work preserved untouched.*
