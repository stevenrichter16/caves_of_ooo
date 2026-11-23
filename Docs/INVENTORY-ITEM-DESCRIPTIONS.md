# Readable item descriptions — November22 user priority

Implemented and bounded Editor-qualified in this increment; source and27 actual
screenshots are in [the verification receipt](Verification/MachineSystems/InventorySections20261122/README.md).
Final focused native471/471/zeroCS includes20new UI cases. Native GameBootstrap
selection/F1/pages/return/resize/F5/fresh Continue preserve measured gameplay;
independent source/gameplay-artifact review found0P1/P2. This is not whole-suite,
packaged-game, controller or hardware acceptance. All12 machine gates stay open.

The user's instruction to proceed before the full regression finished supersedes
older sequencing. The full a9 snapshot is now assessed separately; no broad
all-green claim. The design/source findings below record the original cause and
implementation contract, rather than current unfixed behavior.

## Player-facing result

The selected item remains at the top. Practical facts precede longer prose.
Show short labeled rows, with gold section headings, neutral body text, spacing
and thin dividers. Use size/spacing as well as color so the structure remains
clear without relying on color perception. Preserve the dense inventory's item
capacity and text-size controls. The full details reader uses the same logical
sections and retains every supported fact; the sidebar is a concise overview
with a clear Full details continuation when content does not fit.

- Combat: separate rows for damage per penetration, hit bonus and penetration
  bonus, followed by the relevant
  attribute and strength cap. A bonus is never labeled as a final chance, final
  penetration or guaranteed damage. Explain existing abbreviations in plain
  labels; do not invent DPS or dice ranges for unsupported expressions.
- Protection: separate rows for Armor value (AV), Dodge value (DV) and speed effect.
  Worn armor names its actual protected location; carried armor names eligible
  or planned locations for each equip choice. Armor on one body part does not
  protect the whole body. Hybrid weapon/armor items show both applicable sections.
- Handling and equip slots: weight per item, grip, eligible slots and current
  restrictions. Present concise facts first, with detailed carrying/throwing
  and planner explanations available in the full reader.
- Effects and condition: current state and attached enhancements. Separate
  conditional bonuses and on-damaging-hit attempts; retain resistance/saving
  throw qualifications. Do not present an attempted effect as guaranteed.
- Equipment comparison: current equipment and exact per-slot changes, separate
  from the candidate's own stats. Preserve displaced multi-slot gear and natural
  attacks; no single misleading better/worse total for heterogeneous outcomes.
- Description: canonical flavor text, visually secondary to practical facts.
  Optional repair/material information follows only when supported by live Parts.

Example structure, deliberately without invented item values:

    ITEM NAME
    COMBAT                       PROTECTION (when applicable)
    Damage: existing dice         Armor (AV): signed item contribution
    Accuracy bonus: signed        Dodge (DV): signed item contribution
    Penetration bonus: signed     Protects: actual location

    HANDLING                     EFFECTS AND CONDITION
    Grip / weight / equip slots   Existing conditional effects and state

    EQUIPMENT COMPARISON          DESCRIPTION
    Actual affected slots        Existing flavor prose

## Verified source causes and implementation seams

InventoryUI.Clarity.SelectedDecisionPresentation currently replaces the
already-structured examine document with a single AnnouncementSection named
Decision details. InventoryUI.DensePack.RenderDenseSelection draws the entire
cached decision string through one LLabel/LInk call. The equipment view similarly
flattens its preview into five uniform lines in InventoryUI.LedgerView.

AnnouncementDocument already carries immutable typed sections and facts.
AnnouncementUI already renders section headings separately; avoid rich-text
markup or guessing headers from arbitrary prose. SkillScreenText is the native
glyph renderer, so distinct labels/colors/heights/spacing should use that system.
AnnouncementUI deliberately suppresses the heading exactly named Description;
use an equipment-specific heading such as About this item, or a narrowly scoped
presentation rule, rather than changing every existing world reader incidentally.

EquipmentComparisonService and ItemExamineService are the authoritative live
formatters for weapon/armor facts and actual equip-planner outcomes. Extend their
presentation output with typed sections in the same validated capture, retaining
the plain-text fallback for existing consumers. Do not parse damage/armor numbers
back out of display text or maintain a second copy of combat/equip formulas.
Emit sections alongside the existing validated comparison capture, including
the actual current/displaced items. A new universal inspection framework is not
needed. Empty sections are omitted; unknown facts remain honestly unavailable.

Inventory's retained selection snapshot must remain authoritative: layout,
hover/modal repaint and pagination must not rerun mutable enhancement providers.
Preserve exact subject/viewer/ownership and revalidation guards. Action-specific
container, disassembly, modification, recipe and ability previews must retain
their distinct scope; they must not inherit irrelevant generic equipment sections.

## Review and acceptance

The patch was prepared privately, then applied after explicit user instruction
to proceed while the pinned full run finished independently. Its source snapshot
was preserved. Complete receipts retain the compiling RED/targeted corrections,
final GREEN and actual native screenshot/save evidence before publication.

Bounded checks should prove useful behavior rather than fixed string copies:
weapons, armor and hybrid gear have correct independent sections and live values;
conditional effects retain their qualifiers; per-location armor and natural
weapon comparisons remain truthful; headers are separate styled labels in both
inventory layouts; long text and larger text sizes wrap or paginate without
covering facts or reducing the established item capacity. Missing/unknown fields
stay explicit instead of inventing values.

Preserve existing selected-owner, stale callback, read-only inspection, group
scope and free F1 reader tests. Native play should inspect an ordinary weapon,
armor, a modified item and equipped-versus-carried selections, use both the sidebar
and full reader, then return to the same inventory selection without time/resource
changes. Actual screenshots are required before claiming improved legibility.
No balance, equipment, item stacking, save fields, art or lore changes are implied.

## Independent bounded source/design critique

Existing shared_source_review agent reviewed this plan and the named source
seams read-only, with no edits, tests, processes or native operations. No blocker
to the bounded presentation design. This is not implementation acceptance.

Required refinements:

- Emit typed sections and their complete plain-text fallback once within the
  validated comparison capture. The current SelectedDecisionPresentation calls
  BuildSelectedDecisionDetails and then BuildInventory, reacquiring enhancement
  and mechanics providers. Capture subject metadata without that second provider
  pass, while retaining exact subject/ownership/slot revalidation.
- Keep carried eligible/planned armor coverage distinct from worn protection.
  Preserve current/displaced/natural equipment by actual body slot. Conditional
  enhancement prose must not be added again as a second numeric bonus.
- F1 currently calls SelectedDecisionPresentation directly; returning from the
  reader calls RestoreAfterDecisionReader/Rebuild. Define explicit-read refresh
  separately from cached repaint. Resize, hover, search, modal backdrops and
  pagination must not reacquire mutable providers; do not claim the existing F1
  route already reuses the retained sidebar snapshot.
- Keep each heading with its first body line across page boundaries. Reserve
  real height for headers and spacing; increasing a header's size inside the
  old fixed-line budget can overlap or orphan text.
- Preserve specialized action-preview scope and group selected-member versus
  aggregate quantities. Unknown or unsupported facts remain unavailable.

Use the shared section renderer for item families already offering structured
descriptions, with weapons/armor the first fully verified formatting increment.
Do not claim every item family has been reformatted if only equipment is done.
Unstructured custom-provider text must retain a complete fallback instead of
guessing headings from its content. Later tool/consumable passes use the same
section contract without changing their underlying mechanics.
