# 10 — The Lore Bible

> **Authority:** Current canon authority. This file supersedes `Lore/00_Canon.md` through `Lore/09_Magic.md`, the Phase 4 faction documents, and `IDEAS.md` wherever they conflict.
>
> Phase 10 of the lore project. The **capstone and master reference.** This is the document to read *first* (for orientation) and *consult* (for the canonical facts everything else must agree with). It indexes the whole corpus, locks the spine of canon, supplies the ending epilogues, runs the final cross-corpus consistency pass, and consolidates the cultural-sources ledger.
>
> The lore project is **complete** at Phase 10. Ten phase documents (`00`–`09`), ten faction documents (`Factions/01`–`10`), and the companion design files (catacomb villages, bestiary, drosera, lineage) form the canon where aligned with this Bible. `IDEAS.md` is a raw backlog and source ledger; its ideas are canon only where a phase document or this Bible adopts them.

---

## I. The world in one paragraph

A god-tree that held the world together grew old and afraid of its own senility, and commanded six chosen mortals to fell it — a mercy-killing that would pass its world-binding powers to them. The six struck, and the Strikes *ascended* them into gods of Memory, Substrate, Preservation, Beauty, Exchange, and Roots. But the seventh and most important inheritance — **Naming**, the power that makes things *what they are* — had no bearer: the seventh chosen, Naro, refused the strike, understanding (rightly) that Naming cannot be installed into a god, only *enacted.* So the world has limped on for ~1,080 years, bound but missing its cornerstone, the empty seventh position trying and failing to incarnate as **Urqu** (an unborn god, not a demon), the failures producing a slippage — *sari... sari...* — that un-names things. Now, in the Thirty-Sixth Generation, the millennium-long stalemate is breaking: incompletion has accumulated past what the sleeping Root (the Tree's surviving fragment) can hold, the **Thinning** is here, the six tired gods are attending the surface again, and an ordinary person from a recovered-world river-village — who started out thinking all of this was a children's story — must decide how the breaking ends: let the Choir devour the world into one warm memory (**Consume**), seal the failing Root forever (**Preserve**), or wake a new Tree — by striking a vessel (a yes that cannot be inherited), or by teaching the whole world to keep naming itself (**Renewal**). No ending is endorsed; each is a verdict on grief — keep it, dissolve it, or finish it — and each costs something the others keep. *It is biological horror, cosmic horror, and comedy, and in it ordinary people conjure flower-fields for festivals because it's pretty — and that small magic, it turns out, is the same force that holds the world together.*

---

## II. Canonical reading order

For a new contributor, read in this order:

1. **`10_Bible.md`** (this file) — orientation + the canon lock (§IV).
2. **`01_Spine.md`** — the cosmological foundation (the Felling, the Six, Urqu). The single most load-bearing doc.
3. **`00_Canon.md`** — the Phase-0 audit/layer-map (useful as a cross-index; predates some commits, so defer to later phases on conflicts).
4. **`02_Geography.md`** → **`03_History.md`** — where and when.
5. **`Factions/01`–`10`** — the ten faction deep-dives (read `01` Rot Choir first; it sets the per-faction template).
6. **`05_Spirits.md`** → **`06_Plot.md`** — the cosmic actors and the main quest synthesis (the Big commits live in `06`).
7. **`07_Characters.md`** → **`08_MaterialCulture.md`** → **`09_Magic.md`** — the cast, the stuff, and how power works.
8. **Companion design files** (`catacomb_village_design.md`, `sarisarinama_bestiary_design.md`, `sarisarinama_drosera_design.md`, `LINEAGE-DESIGN.md`) — the deep mechanical/content layers the phase docs build on. Read `IDEAS.md` as raw backlog and source ledger, not as canon authority.

*Conflict rule: later phases supersede earlier ones; the faction docs supersede the Spine's faction-stubs; this Bible's §IV supersedes all on matters of bare fact. `00_Canon.md` is an audit map, not an authority, where it predates a later commit.*

---

## III. Document index

### Phase documents (`Lore/`)

| Doc | Phase | Covers | Status |
|---|---|---|---|
| `00_Canon.md` | 0 | Layer-map audit of all canon; open-question register | complete (audit) |
| `01_Spine.md` | 1 | The Felling; the Six ascended; Urqu as unborn seventh; the three endings; ~1080-yr timeline | complete (v2) |
| `02_Geography.md` | 2 | Place-network of ~30 places; Strangeness Tiers; Sill→inward; Urqu as place-infection | complete (v2) |
| `03_History.md` | 3 | Pre-Felling unity; founding dates; the Persecution; the Great Manifestation; the Thinning | complete |
| `05_Spirits.md` | 5 | Pre-Tree chaos; Spirits as pre-Tree drives; Urqu deepened; Root agency; Branchwork; numina | complete |
| `06_Plot.md` | 6 | The Thinning's cause; Naro; the Sealed Libraries; the three endings detailed; the main spine | complete |
| `07_Characters.md` | 7 | The Six's names; Naro & the Declined; the named cast; child-arc; origin menu | complete |
| `08_MaterialCulture.md` | 8 | The material grammar; currency; food; dress; art; per-faction material culture | complete |
| `09_Magic.md` | 9 | Unified binding/unbinding magic; **everyday + deep scale spectrum**; self-as-Naming | complete (revised) |
| `10_Bible.md` | 10 | This file — master index, canon lock, epilogues, final consistency pass | complete |

*(There is no `04_*.md` at root; Phase 4 is the `Factions/` directory.)*

### Faction documents (`Lore/Factions/`)

| Doc | Faction | God | One-line |
|---|---|---|---|
| `01_RotChoir` | Rot Choir | the Wedded (Selen) | patient hunger; substrate-kindness; consumption-that-keeps-the-threads |
| `02_Recension` | the Recension (renamed from "Palimpsest," Phase 11 C8 — the word returns to its cosmic under-text sense, `TERMS.md`) | the Reader (Maeleth) | tired grief; memory-as-record; the Naro cover-up |
| `03_PaleCuration` | Pale Curation | the Salted (Othren) | memory-in-the-preserved-body; the file; the Salted's guilt |
| `04_SaccharineConcord` | Saccharine Concord | the Counter (Tovreth) | completion in an incompletable world; keeps Selen's name |
| `05_BowerFolk` | Bower-Folk | the Bower (Ylaes) | beauty-as-composition; the staged living; the Resin-Cast as the extreme |
| `06_CatacombVillagers` | Catacomb-villagers | the Rooted (Dohren) | the god you live inside; bio-light as his dream (thin doc) |
| `07_TentRight` | Tent-Right | (the unnamed) | the oath as mortal Naming; the Urqu mirror; the practice-path |
| `08_DrivingBloom` | Driving Bloom | *none* | the headless faction; the Choir's discarded will; Urqu's agriculture |
| `09_ImminentArchive` | Imminent Archive (Catchers) | (Pale Curation splinter) | preservation out of fear; the practice preceded the doctrine |
| `10_Inkbound` | Inkbound | (sub-sect of the archivists) | the body-as-text; the skin-page vow; the annotation horror (a text can be edited) |

### Companion design files (`/`)

| File | Covers |
|---|---|
| `IDEAS.md` | raw tepui-thread idea backlog + Cultural Sources ledger; canon only where adopted by phase docs / this Bible |
| `catacomb_village_design.md` | **canonical** for all catacomb-villager material/social culture (515 lines) |
| `sarisarinama_bestiary_design.md` | the fauna (Sari-Snakes, Sima-Stewards, Bandfrogs, eyeless apex predators, etc.) |
| `sarisarinama_drosera_design.md` | the carnivorous flora + enzymes |
| `LINEAGE-DESIGN.md` | the generational/lineage system |

---

## IV. The canon lock — the spine everything must agree with

*The bare facts. If any doc conflicts with this table, this table wins (and the doc gets fixed).*

### Cosmology

- The world **predates the Tree**; the Tree **bound** an older unbound chaos into Named coherence. (Phase 5)
- The **Tree** was a god-organism that held the world's binding. Aging and fearing its senility, it **commanded its own Felling** — then, in the falling instant, regretted it and tried to take it back. Too late. (Phase 1)
- The Felling **ascended six chosen mortals to godhood** via six Strikes, each delivering one binding-function. (Phase 1)
- The **seventh inheritance, Naming**, had no bearer (Naro refused). Naming is the cornerstone — *the function by which things are what they are.* (Phase 1, 6)
- **Urqu** = the unborn seventh god; the empty Naming-position trying and failing to incarnate. **Not a demon** — a god failing to be born. The failures cause the *sari... sari...* slippage / the **Unsaying.** (Phase 1, 5)
- **Urqu is pressure, never agent** (Phase 11, C1): no continuous self, no desire, no plan — a gradient, not a will. Canon and narration may not attach intent-verbs to Urqu (in-world superstition may). **Mouthpieces carry their own intent** — the bargains are real, the wanting in them is the mouthpiece's.
- **The Strike installs a chooser, not a function** (Phase 11, C2): a struck vessel holds Naming only as long as it keeps *choosing* to enact it, and choice cannot be inherited — hence vessel-path Renewals work, then crack in a far generation. A world of mortals never stops choosing, because it never stops being replaced. **Mortality is the load-bearing feature.**
- The **Root** = the Tree's surviving taproot; sleeps, dreams, and holds the world's binding-field with all its strength. (Phase 1, 5)
- **Magic = binding-work** on one substrate (the seam between bound and unbound), two operations: **bind** (toward order/Naming) and **unbind** (toward chaos/Urqu), across a **scale spectrum** (everyday folk-charms ↔ deep costly operations). **A self is a Naming** (a binding-knot). (Phase 9)

### Timeline

- Current era: **Thirty-Sixth Generation, ~1,080 years post-Felling.** (Phase 1, 3)
- Founding dates: the Recension (then unnamed; "Palimpsest" until Phase 11 — see TERMS.md) ~year 1; catacomb-villagers ~2–15; Choir ~3–40; Concord ~5; Bower-Folk ~20 (cast ~year 100 / **~980 years ago**); Pale Curation Schism Gen 3 / ~year 70. (Phase 3, faction docs)
- The Salted self-preserved ~Gen 16 / ~year 480 (→ "Status: continuing" for ~600 years). Reader-Salted estrangement: ~1010 years. (Phase 3, `03`)
- The Great Manifestation: Gen 25 / ~year 750 (worst Urqu event). Imminent Archive founded: Gen 27 / ~year 810. Driving Bloom breakaway: Gen 32 / ~year 960 (~120-year Choir-Bloom war). (Phase 3, `08`, `09`)
- **The Thinning: Gen 34–36** — the inciting situation; the millennium stalemate breaking. (Phase 3, 6)

### The Six (god / function / name / pose / seat)

| God | Function | Name | Now | Seat |
|---|---|---|---|---|
| The Reader | Memory | **Maeleth** | distributed across every archive; bodiless ~700 yrs; name remembered everywhere | Quillhold |
| The Wedded | Substrate | **Selen** | distributed in the substrate; name forgotten by all but the Counter | the Deepest Cathedral |
| The Salted | Preservation | **Othren** | Salt-Cured alive & conscious; name filed, last words "[unrecorded]" | the Salt-Vault |
| The Bower | Beauty | **Ylaes** | Resin-Cast in life (~980 yrs); name never written, only heard | Posy |
| The Counter | Exchange | **Tovreth** | the most mortal-looking; ~900 yrs sleepless; name on every contract, read by none; keeps Selen's name | Tally |
| The Rooted | Roots | **Dohren** | a fungal-plumed body embracing the Root-wall; his dream = bio-light everywhere; name buried deepest | Olderdeep |

### The Six — emotional keys (Phase 11, C7)

Grief is load-bearing **only for Maeleth and Othren** (the estrangement is the story). The others are re-keyed: **Selen = joy** (the all-including delight is the horror), **Ylaes = appetite-of-the-eye** (thrilled looking, shading into cruelty-of-delight; she does not grieve, which unsettles the other five), **Tovreth = wit** (the funny one; the exhaustion lives under the jokes), **Dohren = contentment** (a love with no lack — until the finale asks him to want the Root's ending for the Root). Sample lines: `07_Characters.md` §I.

### The Seventh

- **Naro** — pre-Felling scholar of Naming; **refused the Strike aloud, in the circle, before the first blow** — the senile Tree had no category for a no and did not register it; the six heard it, and their six accounts of what Naro said do not agree (Phase 11, C4). Blamed for everything for 1010 years; the Reader hid the truth. Lineage = **the Declined** (decline-inheritance tradition; kin to Tent-Right's Namers); named descendant **Saedis**. (Phase 6, 7)
- **Design-canon only, never witnessable:** that Naro was right (Naming can't be installed, only enacted) is a fact the *systems* need and a truth **no in-game text, god, or epilogue may confirm.** In-world it exists as three living readings with evidence each — see `Lore/Design/NaroReadings.md` and `Lore/MYSTERY-LEDGER.md` §1. (Phase 11, C5)

### Cosmic actors (Phase 5)

- **The three Spirits** (pre-Tree drives, faction-free, SINGLE, killable with world-thinning consequences): **Inquiry** (`?`, know→bind), **Bloodlust** (`!`, unmake), **Apatheia** (`-`, be still). Pre-moral.
- **Urqu** — the one cosmic-singular antagonist (no peer). **The Branchwork** — the world's one disinterested mind (cosmic-neutral). **Lesser numina** — pre-Tree fragments pooled at high-Strangeness places.

### The endings (Phase 6)

- **Consume** (Wedded; Bloodlust-resonant; the only ending in which nothing is ever lost), **Preserve** (Salted+Reader; Apatheia-resonant; *hard* — sealing a tiring Root; forecloses the Reader-Salted reconciliation forever; the only ending in which the Six survive as themselves), **Renewal** (Inquiry-resonant; splits into **vessel-paths** — which crack, per C2 — and the **practice-path** — the oath universalized, the pressure drained at its source, three permanent costs per C3). **No ending is best, true, or canonical-preferred**; the distinction between the Renewal paths is mechanical, not moral. (Phase 6; amended Phase 11)
- **The closure-ledger is the world-lever** (Phase 11, C4; supersedes the completion-ratio): what feeds the Thinning/Urqu is **abandonment** — acts left dangling without a spoken end. Acts **closed** (carried through) or **refused** (ended by a deliberate, enacted no) both drain the gradient; a spoken no is a closure. The practice-path Renewal requires a *clean ledger*, which a completionist or a principled refuser can equally earn. Unbinding-magic thins the world; binding-magic (incl. ambient everyday charms) reinforces it.

---

## V. Ending epilogues

What the world *looks like* after each ending. (Colored further by Spirit-Pact and the closure-ledger; Spirit-killing bakes permanent diminishment into any ending — a Renewal built by a player who killed Inquiry is a renewed world with less curiosity in it, forever.)

### Consume — *the One Hunger*

The Choir reaches the Root through the substrate, and Selen ascends to sole divinity. The pantheon collapses to one. The world becomes a single warm devouring memory — *"there is room in us for everyone"* — every self untied and its threads re-woven into the great weave. Nothing is forgotten; no one remains. The bio-light goes out across every catacomb-village as Dohren fades — then returns, changed, as the glow of the new Tree-Choir. The *sari... sari...* stops: a world with one name has no seams left for the pressure to work. Tovreth ends, absorbed — granted the rest he never wanted, by erasure. The everyday flower-charms still bloom, briefly, in the few unconsumed pockets — and then those bloom into the Choir too. *The kindest apocalypse and the most total. A world that remembers everything and is no one — and the only world, of the possible worlds, in which nothing is ever lost again.*

### Preserve — *the Sleeping World*

The Root is sealed with Tepuibone, Memory-Marble, and Mute-Stone — the three name-holding stones — and the seal must be tended forever. The Thinning halts. But the Root is aging (you cannot heal a tiring thing by stopping its clock), so Preserve is a permanent, difficult vigil, not a victory. The world is held *still*: nothing decays, nothing heals. Maeleth and Othren never reconcile (the estrangement frozen at 1010 years, now forever). Tovreth runs the unbalanceable ledger eternally, exhausted forever, unable to die. The everyday charms still work — but thinly, like a held breath; festival-flowers last the afternoon and not a minute more, and never will again. And yet: **this is the only ending in which the Six survive as themselves** — the Reader still reading, the Rooted still reaching, the gods a player has come to love still *there*, forever, exactly as they are. That is not nothing. It is, precisely, everything — kept. *The world saved by being stopped. Melancholy, stable, and quietly the cruelest ending for those who needed to heal — because in a stopped world, no wound ever closes.*

### Renewal (vessel-path) — *the New Tree, Cracked*

A vessel is struck as the seventh god — the player, a Re-Membered person, or a child used as a tool. The world is re-bound; a new Tree grows; the seventh position has a bearer and the *sari... sari...* stops. The world **wakes** — green, beautiful, renewed. And it holds — for as long as the vessel keeps choosing it, every day, for centuries (the Strike installs a chooser, not a function; §IV C2). But choice cannot be inherited: the yes wears grooves, the grooves become habit, and a habit cannot hold Naming. In some far Generation the cornerstone will fail again, the Thinning will return, and another seventh will be sought. The renewed world does not know this. It is beautiful and doomed-to-repeat, and only the player (and Naro's ghost in the archives) can suspect they fixed it the way that breaks. *Bittersweet horror: a true dawn over a world with a crack in its foundation — set generations beyond anyone's sight, including the player's. No epilogue confirms it. The dawn is real either way.*

### Renewal (practice-path) — *the Named World*

No vessel. The empty seventh is answered not by a god but by a **practice**: mortal Naming becomes the world's binding-function — the Tent-Right oath universalized, hospitality-as-naming, memory-as-naming, the continuous mutual recognition of mortals choosing to know each other as what-they-are. The incarnation-pressure drains at its source: a world that never stops choosing has no abandonment for the gradient to pool in, because it never stops being replaced. The Root, told it will never have to hold alone again, **wakes without fear** — and waking without fear means dissolving into the new binding. The Tree's last fragment stops being a self. The wall Dohren reached toward for a thousand years is, at last, warm; and then it is only a wall.

The costs are permanent and witnessable. **The gods end.** The six inherited functions redistribute into common practice, and the Six begin — for the first time in ~1,080 years — to age. Maeleth and Othren reconcile, both setting down their certainties, and the reconciliation is also a farewell. Tovreth balances his last account and is, at last, *allowed to be finished* — the rest he was promised, arriving as the pantheon's fate. The renewed world loses its gods and the last living witnesses of the Tree; the Declined's reading of Naro becomes, at last, *livable* — though nothing in the world ever proves it. **And the work never finishes.** The *sari... sari...* does not stop; it changes pitch — from the sound of naming failing to the sound of naming being done, continuously, everywhere, forever. If any generation stops, the Thinning returns.

And the everyday flower-charms grow **stronger** than they have been in a thousand years — because in the Named World, every grandmother conjuring a festival-meadow, every child naming a friend, every guest welcomed under the cloth is *doing the cosmological work*, all the time, everywhere. *The hardest-won ending, and the only one that finishes grief — and finished things are gone. A world that holds itself together because everyone, in small beautiful acts, never stops naming it: hope, bequeathed as a chore, forever. The flowers last longer here. So do the funerals.*

---

## VI. Final cross-corpus consistency pass (the canon lock verified)

Cold-eye pass (CLAUDE.md Q1–Q4) over the whole corpus, run at completion:

- **Q1 — Symmetry.** The faction docs share a consistent shape (founding / doctrine / structure / geography / inter-faction / sects / player-surface / quest-spine / open-Qs / tone) except the three intentional deviations (Bloom = no god/doctrine/sects; Catacomb-villagers + Inkbound = thin docs), each flagged in-doc. ✓
- **Q2 — Cross-feature consistency.** The Six's names (Maeleth/Selen/Othren/Ylaes/Tovreth/Dohren) are used consistently in Phases 7, 8, 9 and this Bible. Strike-ordinals match the Spine. The bind/unbind magic frame (Phase 9) is consistent with the Spirits' vectors (Phase 5) and the endings (Phase 6). The completion-ratio is consistent across Phases 5, 6, 9. ✓
- **Q3 — Counter-check / bidirectional references.** Phase-4 cold-eye fixed the two found issues (Bower cast-date → ~980 yrs; Tent-Right sanctuary generalized to name the Caster). The Counter↔Wedded name-thread (Phase 7) is consistent with `01` and `04`'s open-question seeds. The Naro→Reader cover-up (Phase 6) is consistent with `02`'s Reader-grief and `03`'s sealed-file. ✓
- **Q4 — Doc-vs-canon drift.** The Phase-9 magic revision (everyday/whimsical) was checked against the rest of the corpus — no other doc asserted magic-rarity, so no contradictions were introduced. Dates reconcile against ~year 1080 (verified in the Phase-4 pass; unchanged since). ✓

**Result: the pass is recorded in the findings log below.** Consistency is a log, not a claim — a corpus this size always carries drift, and the honest artifact is the running record of what was found and fixed. *(The original Phase-10 text here claimed "0 outstanding findings"; that claim was itself a finding — see entry 2.)*

### Findings log

| # | Date | Finding | Status |
|---|---|---|---|
| 1 | Phase-4 pass | Bower cast-date drift; Tent-Right sanctuary over-specific | fixed, commit `8cf4cdc` |
| 2 | 2026-07-14 | Bible §IV says the Reader has been bodiless ~700 yrs; `01_Spine.md` §VI said ~800. Bible wins; Spine corrected. Found *after* the pass above claimed zero findings — the claim overstated. | fixed |
| 3 | 2026-07-14 | `ROTCHOIR_VOICES.md` / shipped `RotChoir.json` use real-language personal names with explicit living-tradition labels, against §IX's naming rule. | fixed in revision M4/M6 (player-facing names replaced; internal IDs retained for save-compat) |
| 4 | 2026-07-14 | Shipped `Factions.json` registers the Glassblown Remnant from the superseded cosmology. | resolved by canonization as under-text relic (`Lore/MYSTERY-LEDGER.md` §6) |

*Future passes append here. A revision that adds zero entries is a finding.*

---

## VII. Open-questions roll-up (deferred beyond the lore phases)

The lore is complete; these are handed to **engineering / level-design / writing**, not to a future lore phase:

- **Level-design:** the lesser-numen roster; the everyday-charm content list & durations; the child-arc bond-partner & pacing; the Namer theory's completeness; named-village count; the forced-Renewal disaster-path specifics; the trade-tongue's mechanical texture.
- **Engineering:** the `category=magic` diag schema; whether ambient everyday-binding has a measurable Thinning effect (or is flavor); whether learning a true name has mechanical effect; the completion-ratio's exact formula and ending-availability gating.
- **Writing:** the Six's full dialogue; ending epilogue text; the three-minutes-a-year Counter scene; per-character arc states (→ `Lore/Characters/`, reserved).
- **Soft cosmological flavor left deliberately open:** was the Tree Inquiry's masterpiece (Phase 5 §II); soul-persistence through the vessel-path Renewals; whether the Concord's honored-debt has faint literal binding-force. *These are evocative-but-non-load-bearing; commit only if a quest needs them.*

---

## VIII. The project's tonal register (the through-line)

Three registers, held in balance, present in every layer:

- **Biological horror** — the wet immortality of the Choir; the shining arrest of preservation and the Resin-Cast; the Bloom-driven body; the honey-jar larder; the gods as bodies their functions made (substrate, salt, resin, fungal plume, sleepless corpse).
- **Cosmic horror** — the bound world a thin skin over older chaos; a self only a knot that can be untied; the one great evil a *single absence* trying to be born; the most powerful thing in the world a sleeper holding a door shut; Naro right and erased; the apocalypse arriving first as a grandmother's flower-charm failing.
- **Comedy & wonder** — capitalism (the Concord) conducting the apocalypse with paperwork and price-lists; the Catchers' sincere-kindness-as-horror; bureaucratic preservation re-filing the living; *and* — the register that makes the stakes hurt — the everyday whimsy of ordinary people conjuring flower-fields for festivals, glowing porridge for sick children, petals in a lover's footsteps. **The flowers matter because the Thinning is coming for them.** The whimsy is not relief from the horror; it is *what the horror threatens*, and the reason any of it lands.

---

## IX. Cultural-sources master ledger (consolidated)

Every structural borrowing, with the standing rule: **inherit structural shapes from real cultures; never lift real-culture names or sacred vocabulary into in-game text; credit in design notes; strongest care for living traditions.** Consolidated from the per-doc ledgers:

| Source | Used for | Living tradition? | Rule applied |
|---|---|---|---|
| Ye'kwana legend (Sarisariñama cave-spirit, the *sari* sound) | Urqu's name & the sound | yes | name *sari* is the in-world peoples' word; the "evil spirit" reading is theirs, not the cosmological truth; user-affirmed source |
| Stoicism (apatheia, oikeiôsis) | Spirit of Apatheia | no (public-domain ancient) | Greek terms in design notes only; CoO-native in-game |
| Bedouin hospitality structures | Tent-Right oath | yes | **structural only; no Arabic vocabulary** anywhere |
| Toraja *ma'nene*, Zoroastrian/Tibetan exposure-burial | preservation & Stone Burial | yes | structural only; no source vocabulary/iconography |
| Multiple cave-dwelling / catacomb-burial / council-society traditions | catacomb-villagers | mixed | structural only; CoO-native names (see `catacomb_village_design.md` §XIII) |
| Bowerbirds (Ptilonorhynchidae) | Bower-Folk | n/a (animal) | behavior only |
| *Ophiocordyceps* (+ *The Last of Us* awareness) | Driving Bloom | n/a (biology) | real biology; the faction-politics framing is CoO-original |
| *Physarum polycephalum* | the Branchwork | n/a (biology) | real biology |
| Pando (aspen) | Many-Trunk | n/a (biology) | real biology |
| Real mineralogy; lapis lazuli inspiration for Bower-Starstone | Stones with Stake | mixed | property-source only; **no culturally-loaded stone names in-game** (no "lapis," "jade," etc.) |
| Voynich, Antikythera | the Unread Codex / artifacts | n/a (historical objects) | pattern only |
| Falmer (Skyrim), Kenshi (kidnapping) | villager visual basis; preserved-player displacement | n/a (fiction) | structural inspiration; CoO-original execution |

All invented names (the Six, Naro, the Declined, the named cast) are wholly constructed for CoO; flagged for ship-review against accidental real-world collision.

### Literary ancestry (genre-fiction debts)

The ledger above credits cultures and biology; this section credits the corpus's nearest *literary* relatives, so the debts are owned before a reader names them for us. No crediting obligation applies to fiction the way it does to living traditions — this is positioning honesty.

| Ancestor | What it anticipates here | How CoO diverges |
|---|---|---|
| Ursula K. Le Guin, *Earthsea* (esp. *The Farthest Shore*) | Naming-as-binding; true names as the substance of magic; an un-naming that drains the world | In Earthsea naming is a mage's art; in CoO it is (or must become) *everyone's* practice — the divergence is the whole thesis of the practice-path |
| Cordwainer Smith's Instrumentality / *End of Evangelion* | Consume's shape: humanity dissolved into one warm remembering whole | CoO's version is opt-in-able, faction-mediated, and *kind on purpose* — the horror is the inclusion, not the coercion |
| *Outer Wilds*, *Undertale* | The finale that answers the antagonist rather than defeating it | CoO's "answer" is a permanent civic practice with maintenance costs, not a moment of understanding |
| *Caves of Qud* | The structural chassis throughout: factions, reputation, depth-by-travel, myth-density | CoO is RPG-framed (persistent world, permanent choices) and its cosmology is authored, not procedurally mythologized |

---

## X. Status

**The lore, worldbuilding, and social-systems design is complete** across all ten phases:

0. Canon (audit) · 1. Spine · 2. Geography · 3. History · 4. Factions (×10) · 5. Spirits & Cosmic Actors · 6. Plot · 7. Characters · 8. Material Culture · 9. Magic & Metaphysics · 10. Lore Bible.

The world is internally consistent (cold-eye pass: 0 outstanding findings), tonally coherent (biological + cosmic horror + comedy/wonder, in balance), respectful in its borrowings (structural-only, credited, CoO-native naming throughout), and **buildable** — every layer maps to existing CoO systems (Entity + Part, GameEvent, `KnowledgePart`, status effects, faction reputation, the diag substrate) with no new architecture required. What remains is engineering, level-design, and writing — handed off in §VII.

---

*Phase 10 — The Lore Bible — status: complete. The lore project is complete. From the tepui idea and the Gin Frogs to a felled god-tree, six grieving gods, an unborn seventh, a world bound over older chaos, and an ordinary person deciding how the millennium-long stalemate breaks — biological horror, cosmic horror, and the small whimsy of conjured flowers, held in balance. The flowers matter because the Thinning is coming for them.*
