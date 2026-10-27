# Shared Bed/Chair independent review

Status: bounded source and native review complete:395 distinct Passed results
plus3 existing allocation-probe Inconclusive results. Bounded actual body/reader and seated-restart review is recorded below;
whole-area polish and packages remain unaccepted. Review is independent of the runtime and test authors. No Unity,
Git, gameplay, build, source-art mutation or native execution was performed by
this reviewer.

## Source findings

No significant defect was found in the reviewed three-file implementation.

- The companion resolves only exact Bed and Chair owners with the matching
  unique owned part, visible owned Render/Physics/Examinable, Furniture identity,
  expected glyph/layer, nonsolid/nonportable single-cell body and no conflicting
  actor, terrain, container or service meaning. Duplicate/foreign essential
  parts, custody, malformed explicit footprint and reskin variants refuse.
  Optional status/stack parts remain optional; duplicate/foreign instances and
  nonpositive stacks refuse. It does not invent Wood, structural HP or thermal
  components for these factory bodies.
- Owner, Occupied and Occupant are never queried for art selection. There is no
  IdleQuery, service/action/event dispatch, effect application, clock advance,
  RNG or world scan. Queries cannot reserve furniture or decide availability.
- All four Greybox entry points make known Bed/Chair refusal authoritative.
  The modern path uses a separate output and the furnitureKnown conditional,
  so the earlier weaker East result cannot rescue a refused body. Both subject
  helpers return the actual fallback directly after refusal. Existing current
  graph/placement/disclosure and selected-owner authority remain in the world
  selector; the semantic reader helper still does not authorize documents.
- Explicit injected East art is borrowed through the new disposed-aware atlas
  accessor, then evaluated by the stronger shared guard. It never calls the
  old East semantic resolver to grant acceptance. Explicit new null/malformed
  shared companions retain refusal; no hidden Resources load repairs them.
  Resources-disabled Greybox retains the existing East-only injection route.
- Successful native furniture opts into the existing single Render-event color
  receipt. The companion and reader do not consume effects by firing another
  event. Owner, classification, collision, ground and independent hazards are
  unchanged. Full/dirty painter behavior still requires native/actual evidence.
- Exactly two FullRect16×24 centered16PPU views are cached from the288×24 atlas:
  Bed rect32,0,16,24 and Chair rect48,0,16,24. Disposal is idempotent and destroys
  only the companion's Sprite/Tile objects, not the borrowed texture. The East
  accessor introduces no ownership transfer. No per-query sprite/resource
  construction or collections were introduced; no measured allocation or
  performance claim follows from this code inspection.

## Reviewed bytes and immutable art

| File | SHA256 |
| --- | --- |
| FlatPixelSharedFurnitureCatalog.cs | `5ebb088d74b5a0b31635694b72ae2e825bcb4494c6634dc85f49474968cf54b8` |
| FlatPixelGreyboxCatalog.cs | `95a8d370879626fb133b72b042c99421fe0f7f7b5faebe4dc5b062d5a4bb207e` |
| FlatPixelEastKitchenCatalog.cs | `823c1957396a2be45300e09ad3dbe5f115fcb8f5e255e986338d02a694bea099` |

The source and runtime East PNGs both match the preflight pin
`594079f4dbfe9d17f0b58f8fcb6adc647d63dae9cfd385ce32fd8b6e31413c56`.
Editable blend, source-cells.json and original builder also match their saved
preflight hashes. This is unchanged-body reuse, not a regenerated art packet.

## Native evidence boundary

Independently parsed `shared-furniture-red.json`: two distinct public
world/authorized-reader cases, Bed and Chair, both failed before production at
the required native-world assertion. The raw result is2total/0passed/2failed/
0skipped, SHA256`647c11adb244bc3cec750e1ba4b07734d49ee866533123e2f375a1f4218e534d`.
Root owns that execution. Expanded countertests and related native GREEN are
still pending at this review checkpoint. No actual after, package, whole-room
fullness, settlement or biome acceptance is asserted here.


## Expanded native and fixture review

The frozen fixture SHA256 is
`1b0e291b60c799762503563201835cd579563919fd8c2bc198d994315b9be700`.
Its33 cases all Passed in root's retained native run. A bounded cold read found
no concrete API/setup error: real factory bodies and current attached managers,
actual submitted Tile/UV checks, authorized examine documents, real NPC
IdleQuery offer/reservation/cleanup, local semantic refusal, explicit missing
shared resources with valid old East art, existing injected-atlas continuity,
real footprint mutation, disclosure/cover/removal and stale-graph refusal, and
cached-view disposal all exercise the intended paths. The paint fixture
detaches its borrowed world catalog before teardown and restores hooks, bound
tile-state callbacks and performance counters.

The color test uses a deliberate actual Render-event probe to change its
ColorString once; it verifies one dispatch, retained neighbor/reader receipt
and neutral recovery. It is not evidence of a particular applied gameplay
status effect. The new fixture proves real NPC reservation semantics; existing
bed/chair action suites separately cover service use. Its name-only standalone
zone is still attached to the test manager, so it does not independently test
a manager-free zone. No material issue follows: the unchanged fallback's
manager-free authority was inspected in source. These are coverage bounds,
not reasons to invent broader mechanics or another test framework.

Independently parsed and deduplicated raw results:

| Receipt | Actual named cases | Result | SHA256 |
| --- | --- | --- | --- |
| shared-furniture-expanded-green.json |259 |256 Passed,3 Inconclusive | `02e9145c5368291640f1d7316d236033670a04e286bd24f42afacfad1338c0b6` |
| shared-furniture-related-green.json |139 |139 Passed | `1da24af254a5c19412d58a49bca17ac86037b25a7e97898e52a791b9e02cd538` |
| shared-furniture-initial-green.json |2 |2 Passed, already included above | `6db8f0eeaab29e7c9795a9f5db70eead72e6855c0165f27dadf6bbe28c457bcb` |

The expanded JSON's summary counts256 while its results array contains259;
the extra three are explicitly Inconclusive. They are the existing Sodden
WarmRegionalAuthorityLookupDoesNotAllocateAndStillReadsCurrentMap probes for
Overworld.15.7.0,16.7.0 and17.7.0. Each reports that the current-thread
allocation counter failed to detect its positive control. They are not runtime
failures or evidence of zero allocations. The expanded and related identity
sets do not overlap. Total398 distinct outcomes therefore means395 Passed
and3 Inconclusive, zero Failed; initial2 must not be added again.

Rehashed all three reviewed production files after the run: they still match
the source pins above. This clears the bounded source/native review with no
significant finding. Ordinary after screenshots, real shared-body gameplay,
whole-view composition, profile, restart and packaged acceptance remain
separate gates owned by root. No performance or whole-area claim is made.


## Actual ordinary Editor views: initial13 captures

Directly viewed before02 and every after01–13 at the saved1280×800 resolution.
The two small approved bodies read distinctly at normal zoom: the narrow
straw cot has a contained light mattress/slat pattern, while the chair has
separated back/seat/legs. Their warm wood/straw pixels remain readable against
the dark green room floor without a new opaque black backing or exaggerated
scale. Reader03 and06 show crisp larger portraits consistent with the actual
world identities and readable descriptions. East control13 shows those same
compact identities in the already-authored kitchen cluster; it is a second
earned control, not proof of one continuous journey from Sodden.

World selection remains truthful in the observed action sequence. The player
covers the chair while on it; no invented seated-player sprite is claimed.
After08 displays Sitting and the ordinary successful Sit log. After09 removes
the status and records Stand. After11 retains Sitting at55,19,HP40 after
root's fresh Continue; after12 moves to54,19, removes Sitting and reveals the
chair body. Bed rest04 reports2HP restored; Vitals shows40/40. Its existing
Focus snapshot still says38/40 at that capture, so the review uses Vitals and
the actual diagnostic receipt rather than treating every displayed panel as
fresh state. This does not establish a new service or sprite regression.

Read `shared-furniture-after-diag.json`: actual Rested record reports2healed
and60clockAdvanced atturn2370; adjacent Sit refusal isnot_on_seat at2460;
PlayerSat2470, PlayerStood2480 and PlayerSat2490 follow for Chair12666.
These diagnostic records support the visible logs; this reviewer did not
issue gameplay inputs. After02 is the rejected Skills panel and after05 is a
stone-floor reader control. Neither counts as furniture identity/use proof.

### Baseline comparison limits

Independently decoded before02→after01 within `[0,40,940,640]`:3,274changed
RGBA pixels out of564,000, global change bounds`[394,388,556,538]`, matching
the saved comparison receipt. Changes are **not** restricted to the two
furniture cells. Of those changes,2,537are floor red13→14 and316red13→15
with green35/blue29/alpha255unchanged; another57are a one-unit green change
from1,37,23to1,38,23near the entrance. Larger color changes include the
new warm body pixels and retired glyph pixels.

The room boundaries, entrance, player50,17,HP38 and broad arrangement appear
stable, but this is not a pixel-isolated controlled artifact replacement.
The before capture may precede the exact saved107turn; logs also differ.
Low-level lighting/render variation is a plausible explanation for the
small-channel background differences, not a demonstrated diagnosis. Existing
LightSourceSpriteHook.Update uses time-dependent light flicker, and source
review found no ground/light/geometry mutation in the furniture adapter;
that alone does not prove which live light caused these pixels. No retouching
or image editing was performed.

Separately decoded actual seated-save10→seated-Continue11 in the same
564,000-pixel world rectangle: **zero changed RGB pixels**. Both show the
player at55,19,HP40 and Sitting. This is the valid bounded restart comparison,
not a substitute for the imperfect before/after pair.

### Composition and acceptance boundary

The body/reader integration is a useful bounded improvement over the prior
`=`/`h` glyphs, with no observed material blocker. The south room still has a
broad empty center and only two small props; its surrounding visible terrain
remains a regular dark repeated field with mixed glyph details. The East
control concentrates its readable objects in a small cluster amid a broad
plain grass field. Those are composition/coverage debts, not evidence that
the new furniture itself is unreadable or permission to fill undisclosed
fog with props. No entire chunk, interior, settlement or biome is accepted
as full or polished.

This review accepts only the shown shared bodies/portraits and ordinary
Editor use/restart evidence. Actual final East actions, wider travel/profile,
complete preservation receipts, packages and release commit verification
remain root-owned follow-up evidence; no new Linux/Deck or performance claim.


## Package-helper cold review before execution

Independently cold-reviewed `shared-furniture-package-audit.py` SHA256
`f70cf5426889f8a1acb58673e45e1438da7bb691ff97debedfbc32917edb15e9`
against the reviewed shared-storage helper. No concrete adaptation blocker
was found. AST comparison independently confirms nine generic functions are
unchanged, including canonical-path, exclusive-create, build-receipt, complete
tree-manifest and streamed archive verification guards. Main changes are
phase/output labels and truthful coverage metadata; the mutation boundary and
collision refusal remain intact.

The new native parser separately pins both complete result files, actual job
receipts, the source summary, three changed runtime files, focused fixture and
borrowed atlas. All current pinned bytes match. Actual job progress is259/259
and139/139, with ordered real timestamps; each must precede the caller's
freeze boundary, and the binding summary itself must precede that boundary.
Only the exact three known allocation-positive-control identities and their
exact Inconclusive reason are permitted. It checks fixture counts, disjoint
identities, all33focused passes and395Passed+3Inconclusive overall, with
`all_cases_passed=false`; the initial2cases are not counted again.

Fresh build receipts must match platform/output and follow the supplied
freeze; full source SHA and phase-specific fresh directory names are required.
The helper explicitly leaves final Git/source-to-binary provenance to root
and does not imply that byte-pinning these selected files proves every input
or reproducible binaries. Support/receipt/archive outputs refuse overwrite.
The existing complete file/hash/size/mode/path checks and separate gzip
CRC/ISIZE/footer drain to EOF are retained, as are original-build byte checks
and no-cleanup behavior on failure. No Linux execution, Deck play or actual
gameplay is inferred from packaging.

This reviewer parsed source/AST and current receipt bytes only: did not import
or execute the helper, run its main/dry-run, build, archive, mutate preferences
or operate Unity/Git. Actual forthcoming archive and Mac review remain
separate from this pre-execution approval.
