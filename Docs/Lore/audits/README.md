# Lore Audits

This directory holds periodic lore audits produced by an automated review
routine (Claude Desktop Routines, GitHub Actions, or any other scheduler).

The routine reads the canonical lore bible at
`Docs/Lore/Palimpsest_Lore_Bible.md`, plus the in-game lore artifacts
(`ROTCHOIR_VOICES.md`, `Assets/Resources/Content/Conversations/RotChoir.json`,
and anything new in `Docs/Lore/`), and writes its findings here as a
timestamped markdown file:

```
Docs/Lore/audits/YYYY-MM-DD-HHMM-lore-audit.md
```

`HHMM` is the 24-hour zero-padded local time the routine fired
(e.g. `2026-04-30-0930-lore-audit.md` for a 9:30 AM audit). Including
the minute timestamp means same-day reruns don't clobber each other —
useful when you trigger an ad-hoc audit alongside the scheduled one.

Each audit is a *suggestion document*, not an edit. The routine never writes
into the bible, the .json, the .md, or any code file directly. Acting on a
suggestion is a manual decision: copy the relevant section into a normal
Claude Code session and ask for it to be implemented.

---

## What the routine looks for

The routine is briefed to draw on the **depth and character** of Elder
Scrolls lore — its mythic-historical stratification, unreliable in-world
authorship, mutating motifs across cultures, internally consistent
cosmology, anthropological framing, marginalia — but to **never imitate
its writing style**. No "verily," no apostrophe-fantasy names, no
Aedra/Daedra/Tower/CHIM borrowings. The exact prompt is committed at
`Docs/Lore/audits/ROUTINE_PROMPT.md`.

Each audit report contains, in order:

1. **State snapshot** — file mtimes and a one-line summary of each source.
2. **Depth audit** — one section per depth dimension (8 of them), each
   citing a specific line, what's strong, what's thin, and one concrete
   suggested addition written *in the project's voice*.
3. **Faction asymmetry check** — the bible weights heavily on Choir +
   Palimpsest; one underdeveloped faction (Pale Curation, Saccharine
   Concord, Brine Communion, Thermoclaves) gets a small lore-artifact
   each run, rotating.
4. **Contradictions and bleeds** — places where the bible and the in-game
   files disagree or fail to embody each other.
5. **The "merchant's diary" suggestion** — one in-world document, under
   300 words, that lands a major piece of lore parenthetically.
6. **Anti-style self-check** — the routine quotes one of its own sentences
   and confirms it does not sound like a TES book.

---

## How to run the audit manually

If you want to fire an audit outside the routine schedule, paste the
contents of `ROUTINE_PROMPT.md` into a Claude session that has filesystem
access (Claude Code, or a Routines run), point it at this repo, and let
it write the dated audit file.

You can run audits as often as useful, but the suggested cadence is
**weekly** — frequent enough to catch drift between the bible and the
shipped content, infrequent enough that you have time to act on what
came back.

---

## Acting on an audit

Audits are advisory, not authoritative. A reasonable workflow:

1. Skim the latest dated audit.
2. Pick 1–3 suggestions that resonate.
3. Open a fresh Claude Code session, paste the relevant section, ask for
   implementation. The session will edit the actual files (bible, dialogue
   JSON, voice doc) — not the audit doc.
4. Commit the edits separately from the audit doc itself.

Audits accumulate as a record of how the lore has been examined over time.
They are *not* deleted or rewritten retroactively — even if a suggestion
turns out to be wrong-headed, leaving it in the trail is useful. Future
audits can reference and disagree with prior ones.

---

## File layout

```
Docs/Lore/
├── Palimpsest_Lore_Bible.md      # canonical lore source (markdown)
└── audits/
    ├── README.md                  # this file
    ├── ROUTINE_PROMPT.md          # the prompt the routine fires
    └── YYYY-MM-DD-HHMM-lore-audit.md   # one per audit run
```

The bible is the source of truth. Voice references (`ROTCHOIR_VOICES.md`)
and shipped dialogue (`RotChoir.json`) are *implementations* of the bible —
when they drift, the audit should flag the drift and suggest which side
to fix.
