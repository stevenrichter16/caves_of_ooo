# Examine ledger independent review notes

Scope: authored descriptions, inventory caller publication, and reader typography.
The coordinating agent owns native Unity execution and final visual verification.
These notes distinguish source review, offline checks, and observed native results.

## Authored descriptions: 124 additions

The audit resolves blueprint inheritance and the ordered `Examinable.Text` /
`Description` aliases before identifying missing prose. It selects leaf blueprints
with concrete loot, stock, terrain or authored-placement references. The selected
cohort comprises 25 terrain entries, 16 workstations/containers, 27 crafting and
gathering materials, 10 plants/food, 19 weapons, 14 tonics and 13 hazards/lights.

Named authored coverage increased from **445/704 to 569/704**; missing named prose
fell from **259 to 135**. Among exact-token references found in runtime World C#
and content Data JSON, missing named prose fell from **198 to 74**. These are
static coverage counts, not measured spawn frequency or an acquisition guarantee.

- Before data edits, the strict authored-prose gate failed for exactly the 124
  intended missing texts. All targets existed and satisfied the leaf/source checks.
- JSON was edited surgically. Parsed comparison proves **124 changed blueprints,
  588 identical blueprints**, with only the allowed examine-prose fields changed.
  Stats, actions, composition, physical material, drops and other parts are unchanged.
- Final exact-copy/ASCII/length/source checks pass for all 124 entries. Seven
  offline counter-check methods pass, including inheritance, alias clearing, cycle
  rejection, forbidden material mutation and unplanned-object mutation.
- Cold prose review narrowed steel-blade and leather-binding repair wording to
  their actual supported gear families, removed an implied manufactured origin
  from ChoirSpine, and kept PalimpsestBlade's worn decoration ordinary and ambiguous.
  Sporeblade prose promises no spore emission. Fixed prose does not claim live
  fuel, lock or durability state, nor infer mechanical composition from appearance.
- The current `Lore` canon and mystery restrictions were used; `Docs/Lore/v2`
  explicitly describes a parallel redesign and was not treated as current canon.

Detailed cohort, exact texts, source references, correction table, reproduction
commands and before/after receipts:
[`../ExamineDescriptions/description-audit-notes.md`](../ExamineDescriptions/description-audit-notes.md).

## Inventory publication: nine confirmed failures repaired

Finding: inventory examination passed a nullable structured snapshot directly to
`AddPresentedAnnouncement`, then recorded success and closed the action popup.
If a description callback removed the item, or Render/Physics ownership was stale,
`BuildInventory` correctly rejected the snapshot but its caller still published
legacy prose. World examination already rejected a null snapshot.

`ExamineLedgerInventoryTests` exercises the actual `InventoryUI` action popup and
dispatcher, not just the builder. Twelve cases cover Dagger, HealingTonic and
FireClay:

- Three unchanged-owner controls require exactly one structured announcement,
  the correct subject title, one success diagnostic, unchanged ownership/quantity,
  no turn, and the expected popup closure.
- Three callbacks remove the selected item during description evaluation.
- Six cases corrupt Render or Physics ownership after opening the menu.
- All rejection cases require no announcement, no success diagnostic, exactly one
  appropriate rejection diagnostic, retained menu, visible failure text and no
  consumption or turn.

Native evidence in `second-pass.xml`: **nine expected failures and three passing
controls** for this fixture. The caller now captures the snapshot and rejects null
before publication, success diagnostics or popup closure. Both material and
ordinary/tonic routes use reason `invalid-examine-snapshot`; existing valid prose
and document metadata remain intact.

Native evidence in `third-pass.xml`: **12/12 inventory cases passed**.

## Typography: actual painted bounds, not occupied cells

Finding: title scale 2.1 produced 0.875 world units of painted width for a full
five-pixel glyph. At 1280x800 with the actual default sidebar/hotbar layout, the
popup cell advance is 0.777574 world units. Adjacent full-width title letters
therefore overlap by about 0.097426 world units, approximately 1.5 display pixels.
At 1920x1080 the same cell advance is 0.895221, so that collision does not occur.

`ExamineLedgerTypographyTests` contains six geometry cases. It reads each glyph's
nontransparent texture pixels, applies the actual per-tile matrix and grid-to-world
position, and compares the resulting painted rectangles. This also accommodates
fractional glyph translation without prescribing the implementation.

- Title separation at 1280x800 and 1920x1080, requiring all eight test letters to
  remain present.
- Body-letter separation, vertical row height and panel bounds at both sizes.
- Long-title clearance from the portrait rectangle and second title row at both
  sizes.

Native evidence in `third-pass.xml`: **one expected Deck title failure, five
passing controls**. The failed separation was 11.5273895 against a required
minimum of 11.6258163 (including the test's 0.001 epsilon), matching the predicted
collision. The title now uses a 1.25-cell horizontal advance, 32-character wrapping
and a uniform scale limited by the actual cell width. Native evidence in
`render-state-red.xml`: **all six typography cases passed**. That file also contains
an independent portrait-render-state RED case; it is not an all-green suite receipt.
Visual approval remains separate from geometry checks.

## Additional source-review controls

- Distinct footer regions match their labels: Left/LB previous, Right/RB next,
  Enter/A advance or close, Escape/B back. Menu-controller bindings match these
  keys. Outside-panel clicks are rejected.
- Title rows 3 and 5, divider 7, body rows 8 through 32, divider 33 and footer rows
  34 through 35 remain vertically separated in the maximum-height 37-row panel.
- The portrait uses header columns 43 through 54 and rows 0 through 6. Long-title
  clearance is explicitly checked by the geometry fixture.
- Shared queue metadata retains exact authored strings and the existing document
  audio ordering. Clear/restore discard transient subject metadata. World surface
  section reconstruction forwards the original subject, cell and viewer context.
- This review did not observe additional confirmed queue, footer or vertical
  layout defects. That bounded conclusion does not substitute for live screenshots,
  controller testing, Linux GPU behavior or physical Steam Deck ergonomics.
- A final cold source review after the title fix rechecked fractional placement,
  fact widths, page/footer bounds, modal queue chaining, inventory restoration,
  surface-section subject context and reading-audio transport. No further
  actionable integration finding was identified in that bounded pass.

New test metadata GUIDs were generated with `uuid.uuid4().hex` and checked to have
32 hexadecimal characters; no hand-composed GUIDs were used. `git diff --check`
passed after these changes. No native Unity calls or commits were made by this
review subtask.

## Independent native-image review

Reviewed the coordinating agent's actual 1280x800 captures `first-live.png`
(clay-lined well), `live-FireClay.png` and `live-HearthbulbCrop.png` against the
selected C mockup. All three have readable native-size text, separated title and
portrait, contained footers and no observed clipping/ghost tiles. The palette and
title/portrait hierarchy follow C; the implemented reader is denser than the
concept image.

The well's 14-character `Repair supplies` label wraps onto a separate row and
disconnects the value from otherwise aligned facts. Recommended shortening to
`Repair` or `Supplies`. A blank row after visible section headings would give the
green heading more separation from the closely spaced white body ink. Further
body enlargement is not recommended without revisiting line advance and capacity.
Repeated repair/composition information in the current-state provider is minor
noise; no brittle string-stripping change is recommended. These are image-review
observations, not proof of physical handheld readability or Linux rendering.

## Bounded portrait coverage and carried-item review

See [portrait-coverage.md](portrait-coverage.md) for exact source boundaries,
counts, current carried support and remaining limitations. This static audit
does not replace the coordinating agent's native portrait test receipt.

## Final source and evidence cross-check

The later polish resolves the first-capture suggestions above: the fact label is
now `Repair`, and visible section headings receive a blank row. These comments
refer to the first screenshots and should not be presented as final acceptance.

Cold review of the level-up facts found no additional actionable defect. Grant
order and values remain unchanged; facts preserve the original announcement,
use computed totals, omit absent/replaced stat instances after reward callbacks,
and show current health without promising a dead actor was healed. Tests cover
missing stats, callback removal, caps/penalties, death and sequential levels.

The completed `final-sweep.xml` records 1,072 cases, 1,071 passed and one failed.
The failure is the pre-existing repair-kit size pin (37 expected, 38 actual after
the one new FireClay model); it is not recorded as all GREEN. The coordinating
agent owns the pin correction, follow-up run and final live acceptance. The final
art import hashes match `world-art-diff.json`: eight objects, 15 refined existing
variants, one added FireClay model, 42 unchanged models. This is distinct from
the eight target images, 124 added descriptions, 135 remaining missing named
descriptions, and 288 declared authored carried-model mappings.
# Final cold review after native acceptance

An independent final pass rechecked queue sidecars and legacy consume/clear/restore, world metadata cloning, inventory null rejection, portrait ownership/cache and current level-up reward facts. It also inspected the final well, inventory sword and level-up 1280×800 captures. No remaining high- or medium-priority issue was identified. The reviewer confirmed that the no-MP actor's level-up correctly omits MP and that the bounded art/fallback claims match the source. Physical Deck/Linux validation remains open; see `final-live-review.md`.
