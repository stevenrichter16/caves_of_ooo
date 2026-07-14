# Lore Revision Plan — fixing the literary-technical flaws

> **Status:** planned (M0–M8 not yet started).
> **Authority:** This plan governs the revision. Where it amends canon,
> the amendments land in `Lore/10_Bible.md` (which remains the canon
> authority) in the same milestone that commits them.
> **Mandate:** User approved 2026-07-14: permission to change lore,
> narratives, NPC dialogue, conversations, and artistic concepts for
> quality. User read and agreed with the full flaw analysis.
>
> The flaw analysis this plan answers was delivered in-session
> (2026-07-14) and is summarized in §I so this document stands alone.

---

## I. The flaws being fixed (traceability index)

| # | Flaw (one line) | Severity | Fixed by |
|---|---|---|---|
| F1 | Canon over-explains; single-truth flattening kills mystery (regression from the Palimpsest corpus's "the disagreement is load-bearing") | structural | M2 |
| F2 | "Canonical best ending" contradicts the all-endings-are-costly thesis; true Renewal is sentimental total resolution gated on 100% completion | structural | M1 |
| F3 | Completion-ratio moralizes tidiness and collides with the valorized-refusal (Naro) theme; Apatheia trap acknowledged but unresolved | structural | M1 |
| F4 | Over-symmetry (mirror formula, sect-triad template, preservation-horror over-allocation, grief-monotone pantheon) | structural | M3 |
| F5 | Urqu ontology wobbles: agency waffle; vessel-paths work despite installation being "incoherent"; practice-path mechanism is a metaphor | conceptual | M1 |
| F6 | Corpus is architecture, not literature: no in-world prose; one authorial voice for ten cultures; self-certifying tonal checks; register contamination | structural | M5, M6 |
| F7 | Naming hazards: confusable names (Maeleth/Telleth, Othren/Oreth/Oel, Vesh/Vael, Selen/Saren/Saedis); "Palimpsest" triple-duty; ROTCHOIR_VOICES violates the canon's own no-real-culture-names rule | mechanical | M4 |
| F8 | "0 outstanding findings" overclaim (Bible: Reader bodiless ~700 yrs vs Spine ~800); majority-deprecated text reads as canonical; cross-strata vocabulary collisions | mechanical | M0, M8 |
| F9 | Unacknowledged literary ancestry (Earthsea naming-magic, Instrumentality-shaped Consume, etc.) | cheap | M0 |
| F10 | Dramaturgical exposure: both climaxes are exposition-retrieval; cast scale exceeds realizable scope | design | M7 |

Every milestone lists which flaws it closes. M8 verifies the matrix is
fully covered before the revision is declared done.

---

## II. Revision principles (the rules this work runs under)

1. **Two-tier truth.** Split canon into **design-canon** (facts the
   systems need; lives in design docs; may be settled) and
   **witnessable canon** (what any in-world text, NPC, or epilogue may
   assert; deliberately incomplete). The player must never receive
   design-canon that the Mystery Ledger (M2) protects. This is the
   single most important rule in the plan.
2. **Prose before praise.** No document may claim a beat is affecting
   or a register is funny. Emotional and comic claims are made only by
   written samples, judged by the external gate (§M5). The
   self-certifying "tonal balance check" sections are retired.
3. **Voice is per-culture, not per-author.** The design-doc register
   (aphorism, em-dash cascade, italicized thesis) is banned from all
   in-world text. Each faction writes from its voice card (M5).
4. **Mirrors need permits.** New lore may not define an element as the
   inversion/reflection of another unless it also adds one element that
   reflects nothing. Vestiges are a feature (M3).
5. **No intent verbs for Urqu.** Canon text may not attribute wanting,
   seeking, trying, or offering to Urqu. (Mouthpieces may want things —
   in their own voice, for their own reasons.) Lintable: a grep gate in
   M8.
6. **Ledger discipline extends to shipped content.** The
   cultural-sources rule ("never lift real-culture names or sacred
   vocabulary into in-game text") applies to JSON content, not just
   Lore docs. ROTCHOIR_VOICES is brought into compliance, not
   grandfathered.
7. **Additive where possible, surgical where not.** Phase-history docs
   (00–09) are development record; they get corrections only where they
   contradict amended canon on bare fact. The Bible takes the
   amendments. Superseded material moves to an archive directory rather
   than being rewritten (M8).

---

## III. The creative commits (what actually changes in the world)

These are the load-bearing decisions. Everything in §IV is execution.

### C1 — Urqu is pressure, never agent (fixes F5-agency)

Committed model: **Urqu is weather, not will.** The empty seventh
position exerts incarnation-pressure along a gradient (toward whatever
un-names); it has no continuous self, no desire, no plan. All prior
"Urqu wants / Urqu uses / Urqu offers" language is rewritten as
gradient-language. **Mouthpieces carry the intent:** a mouthpiece is a
person (or numen) deformed by a near-incarnation, bargaining for their
own reasons with leaked proto-divine power. The bargain system
survives intact; the contradiction dies. Bargains become *more*
unsettling: there is no devil to blame — only people, leaking.

### C2 — Installation works only as long as the vessel keeps choosing
(fixes F5-vessel-coherence, and grounds the practice-path)

Why do vessel-path Renewals function at all if Naming can't be
installed? Committed mechanism: **the Strike cannot install Naming; it
can only install a *chooser*.** A struck vessel holds Naming exactly
as long as it keeps choosing to enact it — enactment smuggled inside
installation. The crack is that *choice cannot be inherited*: the
vessel ages, tires, or dies into its function (as the Six did), and no
successor can re-choose for it. Hence "works, then fails in a far
generation" stops being a patch and becomes the theme stated
mechanically.

The practice-path is then the same fact at population scale: **a world
of mortals never stops choosing, because it never stops being
replaced.** Mortality is the load-bearing feature; gods were the
mistake. "Urqu answered" is de-mystified: universal naming-practice
continuously drains the incarnation-pressure at its source — the
pressure that produced Urqu-phenomena finally has a channel. The
*sari... sari...* does **not** stop (see C3).

### C3 — The true Renewal gets permanent costs (fixes F2)

The practice-path keeps its hope and loses its sentimentality. Three
costs, committed:

1. **The gods end.** Universalizing Naming redistributes the six
   inherited functions back into the world. The Six begin — for the
   first time in ~1,080 years — to age. Maeleth and Othren's
   reconciliation is also a farewell; Tovreth's "allowed to be
   finished" (already canon) becomes the pantheon's fate, not his
   exception. The renewed world loses its gods *and the last living
   witnesses of the Tree.* To heal the world you must let the people
   who remember it end.
2. **The work never finishes.** The *sari... sari...* changes pitch
   instead of stopping — from the sound of naming failing to the sound
   of naming being done, continuously, forever. If any generation
   stops, the Thinning returns. The best ending is not a fix; it is a
   chore bequeathed to everyone, always. Hope as burden.
3. **The Root ends as itself.** Waking without fear means dissolving
   into the new binding: the Tree's last fragment stops being a self.
   Dohren's thousand-year reach ends with nothing left behind the wall
   to embrace. (His arc gains its real question: whether he can want
   this for the Root anyway.)

Consume and Preserve regain genuine appeal against these costs:
Preserve is now *the only ending in which the gods survive*; Consume is
*the only ending in which nothing is ever lost.* The phrase "canonical
'best' ending" is struck from all documents. The three endings are
re-stated as three verdicts on grief — keep it (Preserve), dissolve it
(Consume), or finish it (Renewal) — none endorsed.

### C4 — The closure-ledger replaces the completion-ratio (fixes F3)

The world-physics variable is renamed and redefined. What feeds Urqu
is not *incompletion* but **abandonment: acts left dangling without a
spoken end.** A deliberate, enacted refusal — a "spoken no" — is a
closure. Declining a quest to its giver's face, formally defaulting a
debt, renouncing a pact by ritual: all close the loop. Silently
walking away, ghosting a promise, dropping a rental in a ditch: these
dangle, and dangling feeds the pressure.

This dramatizes the Naro distinction the theme needed: Naro *refused* —
he did not merely fail to appear. **Small retcon (Bible §IV + Spine):**
Naro spoke his refusal aloud in the circle before the first Strike;
the Tree, senile and committed, had no category for a "no" and did not
register it. The six heard it. Their six accounts of what Naro said
disagree (see C5 — this feeds the epistemic re-opening; the god of
Memory's account is the *least* reliable, because she has re-copied the
memory ten thousand times, and every copy is an edit).

The Apatheia trap resolves into a real choice instead of a hidden
penalty: refusal *enacted* is stillness (closure); refusal *by
avoidance* is entropy (abandonment). An Apatheia purist can now play
serenely and cosmically-constructively — by saying every no out loud.
The true Renewal's gate changes from "high completion-ratio" to **a
clean closure-ledger** — which a completionist *or* a principled
refuser can earn. Moral depth decoupled from the content checklist.

### C5 — The Naro question is re-opened at the witnessable tier (fixes F1)

Design-canon keeps: vessel-paths crack (the systems need it).
Witnessable canon **never confirms why Naro refused, or whether he was
right.** Changes:

- The bolded "**was right**" assertions become design-canon only. No
  in-game text, god, or epilogue states it as fact. The crack in a
  vessel-path Renewal manifests generations beyond the game's horizon —
  the player never receives confirmation either way.
- **Three living readings** with authored evidence each, none
  adjudicated: (a) *Naro understood Naming and refused to spare the
  world* (the Declined's tradition; Saedis's family memory — which is
  itself an inheritance, from people who refuse inheritances, and the
  text lets the player notice that); (b) *Naro's refusal was the first
  Unsaying* — the moment a chosen mortal said no to the world's plan is
  precisely when the slippage began; Urqu is Naro's echo, not his
  vindication (a Palimpsest-faction counter-history, held sincerely,
  supported by real archival evidence); (c) *there was never a seventh
  summons at all* — the Tree, senile, dreamed six and the "empty place"
  is a story the guilty Six told (a Concord-actuarial heresy; Tovreth
  neither confirms nor denies, which is itself evidence read both
  ways).
- The Reader's sealed file supports (a) *and* (b) depending on how it
  is read; the finale asks the player to **commit to a reading and act
  on it**, not to retrieve an answer (see M7).

### C6 — The Mystery Ledger (fixes F1, and converts F8's strata-bleed into signature)

A new canonical document, `Lore/MYSTERY-LEDGER.md`: questions the
canon **promises never to answer**, protected by rule — no future doc,
quest, or NPC may close them. Initial entries:

1. Why Naro refused / whether he was right (C5).
2. What the Root dreams when it does not dream the Tree.
3. Whether the Tree was Inquiry's masterpiece (already soft-open; now
   protected).
4. **The tenth fire** — imported from the superseded Palimpsest corpus
   as an in-world relic: somewhere in the deep wasteland a fire burns
   that no current cosmology accounts for; it predates every theory
   held about it. Never explained.
5. **The doll in the wall** — imported likewise: a doll woven into a
   Choir grove wall, kept uneaten. The Choir will not say which things
   it keeps, or why. Never explained.
6. **The Glassblown Remnant** — the shipped faction from the
   superseded cosmology is canonized as exactly that: a remnant. A
   tiny, silent community whose practices answer to no god, no Strike,
   no substrate anyone can identify. They never explain themselves;
   nothing in the world can explain them. (This also reconciles
   `Factions.json` with canon at zero mechanical cost.)
7. What the Bower sees when she watches.
8. The Gin Frogs.

Entries 4–6 execute the **under-text policy**: the older lore strata
are not fought but canonized *as the world's under-writing* — the
palimpsest metaphor applied to the corpus itself. The word
**"Palimpsest" is returned to this cosmic meaning** (the old world
bleeding through) and the archivist faction is renamed (M4). Rule of
restraint: the under-text policy gets these three relics and **no
more**; it is a seasoning, not a license to re-import the old corpus.

### C7 — De-symmetrization commits (fixes F4)

1. **Emotional re-keying of the Six.** Grief stays load-bearing for
   Maeleth and Othren (the estrangement is the corpus's best arc) and
   is *removed as the dominant key* elsewhere: the Wedded's key becomes
   **joy** (the horror of genuine, warm, all-including delight — scarier
   than hunger); the Bower's becomes **appetite-of-the-eye** (curiosity
   shading into cruelty-of-delight; she is thrilled, not serene);
   Tovreth's becomes **wit** (he is the funny one — gallows humor from
   the only god who still stands in a market; his exhaustion lands
   harder as the thing under the jokes); Dohren's becomes
   **contentment** (a love with no lack in it — until C3 gives it one).
2. **Two mirrors broken.** (a) The Concord sect-triad loses its
   "conscience": the Closed Accounts are re-cast as moralists who are
   *also wrong* (their purity would starve the Beating's salt-villages
   inside a season; the mainline is cynical *and* correct about that).
   The player's lever against the Concord stops being a sect and
   becomes a person (Tovreth's one perfect debt). (b) The Catchers stop
   being "the Salted's reflection": their founding trauma predates
   their theology — the Great Manifestation cells existed as ad-hoc
   rescue crews *before* anyone reached for the Salted as
   justification. Doctrine as retrofit, not mirror.
3. **Horror-mode reallocation** for the four preservation factions:
   Pale Curation keeps **body-preservation** (the file, the salt);
   Bower-Folk shift to **staging** — they do not preserve you, they
   *compose* you, preferring living tableaux held by will and resin
   thread (the Resin-Cast is their extreme, not their default); the
   Catchers keep **fear-driven preservation**; the Inkbound shift to
   **annotation** — the horror of being *edited*: a person-as-manuscript
   can receive marginalia, and senior Inkbound bear corrections in
   their skin *in other people's handwriting.* Four modes, four
   textures.
4. **Vestige quota.** M3 adds at least three elements that mirror
   nothing and mean nothing beyond themselves (the Mystery Ledger's
   relics count toward this).

### C8 — Naming system repairs (fixes F7)

1. **Diachronic naming.** The gods' phonaesthetic (-eth/-en/-aes) is
   re-scoped as *archaic*: it marks the pre-Felling generation only.
   Thirty-six generations of drift give modern mortals a shifted
   phonology (harder onsets; -is/-un/-ai/-ok endings; clipped
   two-syllable given names). The gods' names now *sound a thousand
   years old*, which is characterization for free, and the mortal cast
   stops colliding with the divine cast.
2. **Collision renames** (modern-phonology proposals; final picks at
   the M4 checkpoint): Telleth → **Dassun**; Oreth → **Korrai**; Vael →
   **Ilsun**; Saren → **Vess-Mara** (Tent-Mothers take hyphenated
   office-names — a small custom, free flavor); Oel and Vesh survive
   (each unique once the others move). Rule adopted: no two named
   characters may share initial letter + ending; the -eth ending is
   reserved for the Six and Naro-era figures.
3. **The archivist faction is renamed** — "Palimpsest" returns to the
   cosmic under-text (C6). Candidates, in preference order: **the
   Recension** (a recension is a critically-revised text — scholarly,
   evocative, unclaimed), **the Quillbound**, **the Vellum House**.
   User-taste checkpoint before the mechanical rename.
4. **Title-diction rule.** First mention in any document or scene:
   name + title ("Maeleth the Reader"); bare titles only where
   unambiguous; in-world speakers prefer whatever their culture would
   actually say (Curators say "the Salted"; Concord clerks say "Tov" and
   are corrected by their seniors, who are ignored).
5. **ROTCHOIR_VOICES compliance.** The five absorbed-individual voices
   are kept (the conceit is good); the real-language names (Mogu 慕菇,
   etc.) and explicit tradition labels ("Daoist / Confucian") are
   replaced: constructed names, and each voice re-specified
   *structurally* (a maxim-voice, a ledger-voice, a lullaby-voice…)
   with the real-tradition inspiration retired to a design-note credit
   in the ledger, per the standing rule. Blueprint/conversation IDs in
   `Objects.json` / `RotChoir.json` migrate with them. **Save-compat
   note:** blueprint renames break existing saves; M4 ships a
   blueprint-alias shim or lands before any save-compat promise —
   engineering decides at the M4 checkpoint.

### C9 — The climaxes become enactment, not retrieval (fixes F10)

1. **Selen's name.** Learning it still runs through Tovreth (settling
   his one perfect debt — earned through action, already canon). The
   delivery becomes the beat: the player must *speak the name aloud in
   the Deepest Cathedral* — and since a recovered true name spoken with
   intent is practice-path magic (Phase 9), the utterance is a spell
   the player casts, with a choice in the casting (speak it to turn
   her; withhold it as leverage; or give it to her and ask for nothing
   — three different scenes).
2. **The Naro commitment.** The finale never hands the player the
   truth (C5). The player assembles contradictory evidence across the
   three readings and, at the Renewal ritual, must **stake the ending
   on a reading** — the ritual's Naming-resolution requires the player
   to declare what they believe Naro's refusal *was*, and the world
   answers the declaration without ever grading it. Different
   declarations color the ritual differently; none is labeled correct.
3. **The v1 dramatic core** (scope triage, proposed): deep arcs for
   five factions (Choir, the renamed archivists, Pale Curation,
   Tent-Right, Catacomb-villagers) and **three full god-arcs**
   (Tovreth; Dohren; Maeleth+Othren as one braided arc). Concord and
   Bower-Folk ship mid-depth; Bloom, Catchers, Inkbound ship as
   encounter-content, not arc-content. Renewal still requires six
   agreements, but three are short scenes gated on the world-state the
   deep arcs produce, not six parallel epics. User-taste checkpoint.

---

## IV. Milestones (smallest blast radius first)

Each milestone = one reviewable commit-set on this branch, with its
acceptance gate defined **before** authoring begins (the prose analog
of RED-first). Estimates are agent-work-hours; user checkpoints
flagged per PROJECT-IDENTITY convention.

### M0 — Canon hygiene quick wins *(≈1h; no checkpoint)* — F8, F9
- Fix the 700/800 drift (Bible value wins: ~700; correct `01_Spine.md` §VI).
- Strike "0 outstanding findings" from `10_Bible.md` §VI; replace with
  a dated findings log (first entry: the 700/800 drift, found
  2026-07-14).
- Add `Lore/TERMS.md`: the cross-strata vocabulary table (First Root /
  Ink / Palimpsest / Root of the World…), each term mapped to its
  per-stratum meaning and its post-revision canonical meaning.
- Add the **literary-ancestry section** to the cultural-sources ledger
  (Bible §IX): Le Guin's Earthsea (naming-as-binding; the Unsaying vs.
  *The Farthest Shore*), *Neon Genesis Evangelion* / Cordwainer Smith
  Instrumentality (Consume's shape), *Outer Wilds* / *Undertale*
  (answered-not-defeated finales), Caves of Qud (structure throughout)
  — each with one line on divergence.
- Gate: TERMS table covers every collision found in the flaw analysis;
  grep for "0 outstanding findings" returns nothing.

### M1 — The Second Spine (ontology + endings + closure-ledger)
*(≈4h; **user checkpoint** — these are the biggest creative commits)* — F2, F3, F5
- New doc `Lore/11_SecondSpine.md` committing C1–C4 in full, written
  as a decision record (bets surfaced, alternatives noted).
- Amend `Lore/10_Bible.md` §IV (canon lock) and §V (epilogues):
  pressure-model Urqu; vessel-as-chooser; the three Renewal costs; the
  closure-ledger; Naro's spoken refusal; endings re-stated as three
  verdicts on grief, hierarchy language struck.
- Surgical corrections to `06_Plot.md` §I/§V/§X and `05_Spirits.md`
  §IV where they contradict the amendments on bare fact (marked
  `[REVISED — see 11_SecondSpine]`, preserving the development trail).
- Gate (counter-check style): grep `Urqu (wants|seeks|tries|offers|uses)`
  → 0 unattributed hits corpus-wide; grep `canonical.{0,10}best` → 0;
  a fresh-context agent given only the amended Bible must correctly
  answer "does any ending dominate?" (expected: no) and "who wants the
  Root unmade?" (expected: no one — wanting is the wrong category).

### M2 — Epistemic re-opening *(≈3h; user checkpoint on the three Naro readings)* — F1
- New doc `Lore/MYSTERY-LEDGER.md` (C6) with the protection rule.
- New doc `Lore/Design/NaroReadings.md`: the three readings, their
  evidence sets, which sources support which, and the two-tier truth
  policy statement (C5).
- Demotion pass: rewrite every player-reachable "Naro was right"
  assertion across `06_Plot.md`, `07_Characters.md`, `10_Bible.md`
  into design-canon framing.
- Gate: fresh-context agent reads only witnessable-tier text and is
  asked "was Naro right?" — acceptable answer must be "the corpus
  doesn't say"; the ledger's 8 entries each name the doc that is
  *forbidden* from answering them.

### M3 — De-symmetrization *(≈3h; user checkpoint on the Six's new keys)* — F4
- Emotional re-keying (C7.1) landed in `07_Characters.md` and the
  relevant faction docs; each re-keyed god gets 3–5 *written sample
  lines* in the new key (the first real test of the keys).
- Break the two mirrors (C7.2): rewrite Concord sects in `04`; rewrite
  the Catcher founding in `09`.
- Horror-mode reallocation (C7.3): amend `05_BowerFolk.md` (staging)
  and `10_Inkbound.md` (annotation).
- Vestige additions (C7.4) cross-referenced to the Mystery Ledger.
- Gate: the "swap test" — for each remaining mirror, ask whether the
  doc *needs* it; a table lists every X-mirrors-Y pair kept, with one
  line of justification each; count must be lower than pre-revision.

### M4 — The naming pass *(≈3h; user checkpoint on rename picks + faction name)* — F7
- Land C8: rename table applied across `Lore/` and `Docs/`;
  diachronic-naming rule documented in `07_Characters.md` §VI.
- Faction rename (user picks from candidates) applied to docs **and**
  shipped content: `Factions.json`, `Palimpsest.json` conversation IDs
  and display strings, `Objects.json` blueprints as needed.
- ROTCHOIR_VOICES rewrite (C8.5): five constructed names, structural
  voice specs, ledger credit note; migrate `RotChoir.json` +
  `Objects.json`; decide save-compat handling (alias shim vs. clean
  break) with a note in the commit body.
- Gate: collision rule holds corpus-wide (scripted check: no two named
  characters share initial + ending); EditMode content tests pass on
  the user's machine (JSON schema/reference integrity — flagged for
  the next Unity session, since this environment has no editor).

### M5 — Voice bibles + Codex wave 1 *(≈6h; the external tone gate starts here)* — F6
- `Lore/Voices/` — ten **voice cards** (one per faction + one for the
  Six's shared archaic register): register, syntax habits, lexicon,
  taboo words, a forbidden-styles line ("never the design-doc
  aphorism"), and 5 sample lines each. The good Palimpsest-corpus
  grammar ideas return here as *voice*, not grammar-specs (Curation:
  catalogued passive; Choir: first-person-plural present; Tent-Right:
  second-person imperative welcome; Concord: priced clauses).
- `Lore/Codex/` — **twelve in-world artifacts, zero design voice**:
  three contradictory Felling myth-tellings (one per Naro reading);
  the Tent-Right oath text; a plaque-wall lineage inscription; a
  Concord contract (Tovreth's signature line present and unread); the
  Salted's file — six annual entries sampled across 600 years, idiom
  drifting, status line never changing; a Catcher pamphlet in Kavin's
  voice (the comedy, written); a catacomb ward-song; an Inkbound
  marginalia page (someone else's handwriting in the corrections); a
  Choir grove-sign; a child's version of the sari story as told in
  Sill.
- **The external tone gate** (replaces self-certification): each
  artifact gets an intent card written *before* drafting (voice,
  register, what it must do, what it must never sound like). A
  fresh-context agent then grades the finished piece against the card
  *without seeing the intent* (it must infer register and land within
  the card's bounds); comedy pieces must be identified as funny by a
  reviewer who wasn't told to find them funny. Failures are rewritten,
  not re-graded.
- Gate: 12/12 artifacts pass the blind review; a shuffled-lineup test
  (10 unlabeled samples, one per faction) is correctly attributed ≥8/10
  by a fresh-context agent — the one-voice problem, measured.

### M6 — Shipped-dialogue upgrade *(≈4h; no checkpoint, mechanical + creative)* — F6
- Rewrite existing conversation JSON against the voice cards:
  `RotChoir.json` (five voices + tendril), `Palimpsest.json` (new
  faction voice), `Factions.json` conversations, `Villagers.json`,
  `FriendlyNPCs.json` where faction-voiced.
- The AT-era quest content (BMO, Marceline, etc.) is **quarantined,
  not rewritten**: moved under a `Legacy/` content flag with a
  README, out of this plan's scope (re-skinning it is content work for
  a later plan; deleting it is a user decision).
- Gate: every rewritten line passes its faction's voice card (blind
  attribution as in M5); EditMode content tests green next Unity
  session.

### M7 — Dramaturgy commits *(≈2h; user checkpoint on the v1 core cut)* — F10
- Land C9 in `06_Plot.md` (marked revisions) + a new
  `Lore/Design/V1-DramaticCore.md` with the scope triage table.
- The two climax redesigns get full scene treatments (beats, choices,
  failure states) — written as design, pointing at Codex artifacts for
  their texts.
- Gate: neither climax's critical path contains a "read document →
  learn truth" step; each contains a player-enacted commitment.

### M8 — Restructure + honest final pass *(≈2h; no checkpoint)* — F8
- Move phase-history docs (`00`–`09`, old faction docs superseded by
  M1–M4 amendments) under `Lore/History/` with a one-page README;
  `Lore/` root retains: `10_Bible.md` (amended), `11_SecondSpine.md`,
  `MYSTERY-LEDGER.md`, `TERMS.md`, `Voices/`, `Codex/`, `Factions/`
  (current), `Design/`.
- Re-run the full consistency pass **with the findings log kept** —
  the deliverable is the log, not the claim; any new drift found is
  fixed or logged, never silently absorbed.
- Verify the §I traceability matrix: every flaw row cites the commit
  that closed it.
- Gate: the M1 grep-lints re-run clean corpus-wide; README reading
  order updated; the findings log has ≥1 entry (a log with zero
  entries after a revision this size would itself be a finding).

---

## V. Sequencing, estimates, and checkpoints

Order is M0 → M1 → M2 → M3 → M4 → M5 → M6 → M7 → M8. M1 is the
keystone — M2/M3/M7 depend on its commits; M0 is independent and lands
first as warm-up. M5 must precede M6 (voice cards before dialogue).

**Totals:** ≈28 agent-work-hours across 9 milestones. **Five
user-taste checkpoints:** M1 (ending costs + closure-ledger), M2 (the
three Naro readings), M3 (the Six's new emotional keys), M4 (rename
picks + faction name), M7 (v1 scope cut). Calendar time is gated on
those checkpoints, not on authoring.

**Out of scope, explicitly:** engine work (closure-ledger
implementation, blueprint aliasing), quest scripting, the AT-era
content's final disposition, and any Palimpsest-corpus (`Docs/Lore/`)
edits — that corpus stays immutable per its own rules; this revision
only *borrows from* it (C6) and never modifies it.

---

## VI. What this plan protects (so quality-raising doesn't flatten)

The revision must not sand off what already works: the Rooted's pose;
the Counter keeping Selen's name ("Memory forgot; the ledger
remembered"); "Status: continuing"; the Root as a hand holding a door
shut; the Branchwork's clean wonder; "the flowers matter because the
Thinning is coming for them." These survive every milestone unchanged
in substance. Where a milestone touches their documents, the gate
includes a regression check: the protected beats still read the same
or better.
