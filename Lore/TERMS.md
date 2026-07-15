# TERMS — the cross-strata vocabulary map

> **Authority:** Canonical reference (Phase 11 revision, M0). This table
> disambiguates every term that means different things across the
> repo's three lore strata: **S1** the Adventure-Time-flavored
> prototype content, **S2** the Palimpsest corpus (`Docs/Lore/`,
> immutable), **S3** the current god-tree canon (`Lore/`).
>
> Rule: new writing uses only the **canonical meaning** column. The
> older meanings are listed so a reader of old documents knows what
> they are looking at — and because three of them have been
> deliberately canonized as under-text relics (see
> `Lore/MYSTERY-LEDGER.md`).

| Term | S2 meaning (Palimpsest corpus) | S3 meaning (god-tree canon) | Canonical meaning going forward |
|---|---|---|---|
| **the Palimpsest** | A cosmic force: the old world's memory pressing up through the new ("the writing under the writing") | An archivist faction founded by the Reader | **The cosmic under-text again** — the residue of older worlds (including, at the corpus level, the older lore strata themselves) bleeding through. The archivist faction is renamed **the Recension** (revision M4). **Structural stratum as of Phase 12** (Bible §IV; Mystery Ledger amended policy): the Felling was an overwriting, and the under-text is everywhere the binding thins. |
| **the under / the bleed / scraped / underreading** | *(absent)* | *(absent)* | The **in-world workaday vocabulary** for palimpsest phenomena (Phase 12; per the voice cards, the world never says "palimpsest points"): *the under* (what lies beneath any writing or place), *a bleed* (the under showing through), *scraped* (deliberately erased — always leaves a ghost), *underreading* (the practice of recovering it — `Lore/Design/UNDERREADING.md`). |
| **First Root** | The Rot Choir's monumental deep architecture (its "throne-chamber") | The Tree's surviving taproot, sleeping beneath the stump | The Tree's taproot (S3). The Choir structure is un-named; its deepest chamber is simply **the Deepest Cathedral**. |
| **Ink** | (a) A rental currency (`WEAPON_RENTAL_AND_INK_SYSTEM.md`); (b) trade good of the imported Inkbound reference | Dew-ink: glowing Drosera-derived manuscript ink; the Inkbound sub-sect's medium | Both S3 senses stand. The rental currency keeps the name in mechanics but is understood in-world as **scrip backed in ink** — Quartermasters literally keep their ledgers in it. The imported Inkbound reference (`INKBOUND_LORE_REFERENCE.md`) remains non-canonical. |
| **Rot Choir** | A faction: sentient mycelial network among seven cultures | The faction founded by the Wedded (Selen); the Consume ending's engine | S3 meaning. The S2 Choir's escalation designs (`Assets/Content/Narrative/mycelium.txt`) remain good design reference but their faction ecosystem (Thermoclaves, Ferment, Brine) is superseded. |
| **Glassblown Remnant** | One of the seven S2 cultures (reversible glass; the unsaid mood) | *(absent)* | **Canonized as an under-text relic** (MYSTERY-LEDGER §6): a tiny silent community answering to no god, no Strike, no known substrate. Never explained. The shipped `Factions.json` entry is therefore canon. |
| **the tenth fire** | Thermoclave doctrine: the uncounted fire that tends itself | *(absent)* | **Canonized as an under-text relic** (MYSTERY-LEDGER §4): a fire in the deep wasteland that predates every theory held about it. |
| **the doll in the wall** | The Palimpsest-corpus emblem of the central choice | *(absent)* | **Canonized as an under-text relic** (MYSTERY-LEDGER §5): a doll the Choir keeps uneaten, reason never given. |
| **completion-ratio** | *(absent)* | The world-physics lever: incompletion feeds Urqu | **Replaced by the closure-ledger** (revision M1 / `11_SecondSpine.md` C4): what feeds Urqu is *abandonment* — acts left dangling without a spoken end. A deliberate, enacted refusal is a closure. |
| **Ooo** | — | — | Legacy title vocabulary (S1). No in-world referent in the canon; the shipped Adventure-Time-flavored quests are quarantined as legacy content (`Docs/LEGACY-CONTENT.md`). |
| **Urqu** | *(absent)* | The unborn seventh god; formerly written with intent-verbs ("Urqu wants…") | The unborn seventh **as pressure, never agent** (revision M1 / C1). Canon text may not attribute wanting, seeking, trying, or offering to Urqu; mouthpieces carry their own intent. |

## Faction ID vs. display name (engineering note)

Shipped code and saves reference faction **internal IDs** (`Palimpsest`,
`RotChoir`, …). The revision changes **display names and dialogue
text only**; internal IDs are frozen for save- and code-compat. So
`"Name": "Palimpsest"` in `Assets/Resources/Content/Data/Factions.json`
is an ID; its `DisplayName` is **"the Recension"** from revision M6
onward. The same rule covers NPC blueprint IDs (e.g. blueprint `Mogu`
displays a compliant constructed name; see `ROTCHOIR_VOICES.md`).
