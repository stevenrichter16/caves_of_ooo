# W6.3b verification — summit and sima fauna

Status: implementation, review, native capture and final single-editor suite complete.
This report covers W6.3b only. W6.4–W6.7 remain next in the phase plan.

## Evidence

| Gate | Result |
| --- | --- |
| Untouched worktree baseline | 7302/7302, zero C# errors (`W63b-baseline-7302.xml.gz`) |
| Final single-editor full suite | 7394/7394, zero C# errors, 17:38:40–17:39:40 UTC (`W63b-final-full-green-7394.xml.gz`) |
| Dedicated adversarial fixture | 43 cases: partial activation, occupancy, real movement vetoes, distribution, null context, input, unload, whole-save, diagnostics and action scheduling |
| Sprite audit | 12 distinct 16×16 RGBA sprites; binary alpha; shared outline; copied metadata differs only by unique GUID (`W63b-sprite-audit.json`) |
| Native bootstrap audit | Six of six PASS, run `868449a126e4415fb6b5d2cef38adc63` |
| Native workload | 75.0006 seconds, 70,659 captured frames, 4,980 clock ticks / 498 player actions, zero capture failures, launcher exit 0 |

The untouched workspace includes 90 tests from protected, previously authored
spell work. This phase adds **92 tests**; those earlier changes are not staged.
Raw XML/CSV artifacts are gzip-compressed without changing their bytes. Individual RED/GREEN XML files preserve the failures and their controls. The
scenario's initial missing-class compile RED is archived separately. One later
population splice syntax error and a duplicate-reference error in a temporary
PlayMode assembly were caught before trusting XML; neither was a test result.

Native raw data: [profile](W63b-live-profile.json), [all frames](W63b-live-frames.csv.gz),
[active-frame summary](W63b-live-summary.json). The first idle capture has its own
`W63b-live-idle-rejected-*` artifacts: all six audit rows passed, but zero clock
ticks and zero AI samples caused four explicit capture failures. It is not a
successful performance run. Batch input now uses a disposable settings clone
and keyboard; cleanup restores the original settings and removes the device.

## Implemented behavior

- Five authored animals with imported sprites and real anatomy/attack contracts.
  Sky-Sari rolls require UrquActive alongside the Sari-Snake; summit endemics use
  actual tree/bromeliad habitat. Ordinary Helmwood frogs inhabit real Grovelands
  forest, and rare sima indicators accompany paired water passages.
- Healthy Sentinels retreat by ordinary movement into visible bromeliad cover.
  Quiet/covered animals remain cryptic. Rooting, occupied cells, occlusion and
  retaliation retain their existing gameplay rules.
- Communal pricklebrow nests preflight all sixteen defenders before activation,
  register each with the energy scheduler, preserve hostility/latches in saves,
  and do not respawn a second swarm. Generation uses the same capacity rule.
- Hidden water passages connect Drowned-Sima floors to their own surface stream,
  bypassing the descent. Exact-cell up/down input handles the transition and
  followers; blocked/orphan routes refuse without moving the actor. Surviving
  endpoints repair unloaded peers lazily. Ordinary stair searches do not reveal
  them. Pairing occurs after both chunks have generated: initial discovery is
  floor-first, and subsequent surface return exposes its existing peer.
- Three short tepuibone seams on surface slopes use four visual variants and
  the existing connectivity repair. Harvesting produces heavy, tradeable stone;
  a full pack leaves overflow on the ground rather than deleting it.
- Loaded zones rebuild their tag index as well as positions, so saved creatures
  remain discoverable by AI/ecology queries. Population placement respects
  physics-only occupants. Handless authored fauna can opt into their own damage
  dice without overriding equipped weapons or untagged fallback attacks.

## Cold-eye review

| Severity | Finding | Disposition |
| --- | --- | --- |
| 🟡 | Quadruped anatomy silently reduced authored natural damage to 1d2 | Opt-in body attack fallback, actual combat diagnostics and equipped/untagged controls |
| 🟡 | Population overlap and cyclic habitat-selection bias | BlocksMovement plus uniform reservoir selection; occupied cell and distribution tests |
| 🟡 | Nest stamps could fit fewer defenders than activation required | One shared capacity predicate; no partial spawn or consumed failed activation |
| 🟡 | Save-loaded creatures vanished from tag queries | Rebuild tag index; missing/stale entries and whole-save activation tested |
| 🟡 | Indicator frog blocked its own passage; unloaded surface lost the route | Exact rings plus lazy peer generation; real input, save and blocked-peer controls |
| 🟡 | Heavy harvest lost chips at inventory capacity | Overflow ground placement, unit-count assertions accounting for stack merging |
| 🟡 | Helmwood forest rows lived only on treeless bands | Actual Grovelands routing and nonzero generated-world population sweep |
| 🔵 | Fail-soft habitat and AI decisions lacked diagnosis | Guarded worldgen rejection and opt-in AI decision records |
| 🧪 | Live look/feel, Singer audio and flying behavior | Explicit boundaries below; no headless visual claims |

Independent read-only reviews found no remaining material defect in the
passage, overflow, habitat or occupancy changes. Additional recommended coverage
(carried-source harvest fallback and walking from a floor stair to the hidden
endpoint) is recorded as coverage debt, not an observed failure.

## Native measurements and honesty bounds

| CPU marker | Mean when active | p99 when active | Whole-run maximum |
| --- | ---: | ---: | ---: |
| All scheduled AI in the scenario | 1.476 ms | 2.520 ms | 27.777 ms |
| Sentinel retreat (three subjects) | 0.0502 ms | 0.0866 ms | 0.1038 ms |
| Zone renderer | 0.746 ms | 1.112 ms | 9.110 ms |

The full scheduler's largest spike was on the first workload action (frame 2).
Raw maximums are retained. Global p99 for a turn-only marker can be zero because
less than 1% of captured render frames contain an action; the table therefore
uses active samples. GC includes the editor, renderer, diagnostic logging and
capture setup: roughly 28.5 KB/frame mean with a 12.8 MB maximum. This is not a
claim of zero-allocation gameplay or a before/after regression comparison.

**Can verify (script-observable):** six live treatment/control outcomes, actual
input-driven turns after bootstrap, scheduled combat activity, CPU counters,
scene renderer execution, imported assets and persistent graph behavior.

**Cannot verify (visual / feel):** whether the sprites read well in motion,
cloud composition, audio atmosphere, perceived combat fairness, aerial movement
or a flight/stoop animation. The contact sheet was inspected at nearest-neighbor
scale; that is art review, not a live play-look judgment.

**Scope boundaries:** Singer silence/alarm belongs to W8. The Sky-Sari ships its
Urqu-gated apex combat role, Avian anatomy and winged sprite; advanced aerial
locomotion/stooping is not implemented. Tepuibone ships harvest, weight and trade
value; anti-Urqu infusion/slurry/specimen consumers remain outside this wave.
The Helmwood route is a minimal Door within its own sima stack, not a generalized
wormhole generation system. New Part fields use the existing object graph; no change to the positional save format or version.

A pre-existing camera-lifetime bug in the protected spell-renderer diff emits a
MissingReferenceException during editor teardown (`ZoneRenderer` camera-accent
callback uses `?.` on a destroyed Unity Camera). It occurred after the measured
capture and is recorded for the authorized whole-game audit; this phase does
not rewrite that protected work or count teardown as a visual pass.

Metadata retains the template's whitespace exactly, per the sprite-copy rule; source-code whitespace checks pass.
