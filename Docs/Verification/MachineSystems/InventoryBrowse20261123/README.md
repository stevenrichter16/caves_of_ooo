# Inventory categories, sorting and readability — November 23, 2026

Final native checks and actual Editor play are accepted after independent
read-only source/gameplay-evidence review with0remainingP1/P2. The reviewer
verified the checkpoint maps and screenshots; this is not a second native
operator. This is an inventory usability
increment, not a completed machine, route or economy system. Weapon-merchant
expansion is the next separate user request and is not accepted by this receipt.

## Player-facing behavior

F3 opens the category chooser, F4 the sorting chooser. The same choices are
available through painted controls and controller Commands. Search intersects
the chosen category. Category preserves the original category/ID ordering;
Name A–Z and Heaviest first sort globally using retained public names and group
total weights. Stable member-ID anchors break ties independently of the selected
representative. Exact selected owners survive sorting/regrouping; excluded
owners remain separate return memories. Explicit selection or opening actions
replaces that memory. Escape clears search, then category, then closes.

The user requested additional categories. Empty/Miscellaneous fallback now
recognizes Containers, Seeds, Reagents, Materials, Components, Camp Equipment
and Documents, plus existing Tools, Grenades, Light Sources, Books and Tonics.
Primary use wins over incidental glow. Explicit protected/custom categories
remain authoritative. Presence checks and short named-supply lists call no
state/action/description providers and do not rewrite Physics.Category or save
fields. Display category participates in UI grouping keys, so key bytes can
change; audited grouping permissions and physical state are unchanged. Merchant
fingerprints explicitly supply an empty category and retain their policy.

The user also requested removal of the armor paragraph explaining AV/DV and
item contributions. That paragraph is removed; armor value and dodge value
remain separate lines. Hit bonus and penetration remain separate weapon lines.

## Native verification

- Initial Browse RED19/19failed/0compilererrors; first GREEN168/168.
- Review RED2:6cases/4failed reproducing narrow Grouping/F1 collision and both
  pointer/keyboard fallback-action return errors. Corrections retain separate
  controls and commit explicit visible selection intent.
- Compact RED2/2failed/0CS reproduced an actual existing Fit argument exception
  when the new query region became one glyph. Compact80px controls now leave
  eleven query glyphs at480 logical width; insertion end remains readable.
- Browse final177/177,28new executions. Additional provider test initially
  expected zero initial selected-description reads; corrected to no additional
  reprojection reads, with the failed result retained.
- Category corrected RED54/9passed/45failed/0CS. Initial44-case run included
  invalid fixture assumptions that every flask/camp item has Stacker; those
  cases were corrected using the existing optional-Stacker helper.
- Category first GREEN263/260passed/3failed/0CS. Two missing liquid-registry
  scopes caused safe contents-unavailable/ungrouped behavior in fixtures;
  corrected with the existing isolated registry, without runtime workaround.
- Final categories/UI/equipment regression263/263/0CS,54new category executions
  plus28Browse executions. Includes existing category and shield-rendering
  controls. The later weapon_merchant_red3 run separately passed all84existing
  item-description tests; its49new merchant tests remain separate RED evidence
  (8controls passed,41feature failures). Earlier merchant test compilation
  attempts are excluded and do not establish RED.

The original full a9 regression remains failed. These targeted runs do not
supply a corrected full-suite total or close any of the twelve machine gates.

## Actual qualified play and fresh load

The retained supplied Cinderhold10660 character is a UI fixture, not ordinary
acquisition, affordability or difficulty evidence. Initial native pack had62
rows/68physical stacks/211units,7categories including34Miscellaneous rows.
Literal /tools matched nothing because search is public name/state matching,
not a semantic alias. The new same pack has13categories with no Miscellaneous
rows; all62groups/68owners/211units remain. Components is also implemented and
native-tested; the spent repair components are absent on this particular save.

The final qualified run uses ordinary Dynamic InputState.Change keys and actual
end-of-frame captures in continuously updating GameBootstrap. A temporary owned
keyboard/current-device boundary, legacy-axis reset and disabled Unity mouse
prevent unrelated physical input reaching the private play surface; the enabled
mouse and original input settings are restored before Stop. No runtime input
code, gameplay action API, HP/position/time/item injection or normal save edits.
Earlier stepped-input helper experiments are excluded, not production fixes.

Actual1280x800pack retains44-row capacity and480x360retains its existing compact
layout. F3 reached all13categories, including the late Documents row. Native
Container filtering retained six distinct state groups, including three empty
owners, two2/12water owners and two4/12mire owners. Singles selection2242,
regrouping and Name/Weight sorting preserved that exact empty-flask owner;
water search intersected Containers and clearing restored it. Group-weight
sorting used all26timber units, not the selected16-unit physical stack.

Actual F1 opened the guide17744, armor2333 and dagger2127. Final armor capture35
omits the requested paragraph, keeps AV/DV separate and retains its location
rule. Dagger37keeps Hit/Pen separate; its inherited on-hit modification is a
fixture, not a new merchant weapon. Final480water typing and accepted query are
readable with separate Browse/Grouped/F1controls. All final browsing/readers
remain turn10660/HP412/drams67.

Final native I/Tab opened the normal Pause menu; F5saved through the ordinary
save controller while protected by that menu. Four-file checkpoint verified
before cleanup. A full stopped-session Continue retained13categories,62groups,
68owners,211units, exact liquid distinctions, default Category/All view, pump
StableRepair, questdone, purse67, HP412,26timber and exact original Cinderhold
lot g0/install9490/LongSword|Battleaxe/28104+28105. Fresh F5again verified10660.
Before/final/fresh Quick.sav.gz hashes all equal
6208285513095e74915177753e67b310fc791add3d8fb8c78dac9c656075779a;
metadata timestamps differ as expected. This is a fresh check of these payloads,
not a claim about byte-identical global preferences.

## Preserved exclusions and limits

Earlier free10660browsing and exact2242drop left two empty owners, but later
world-close/G input was followed by uncontrolled movement/time/combat. Cause
unproven. Saved12710/HP283 in6.13is preserved; no pickup/recovery, safe-world-input
or inventory-caused-damage claim. Later stepped-helper attempts included an
unreachable unpause statement (no action frames), resize selection mismatch and
an unintended Torch reader/ordinary pickup/equip; partial10671/HP412 is preserved.
Those are not accepted clean gameplay or weapon-reader evidence. See the raw
exclusion JSON. Final accepted branch restarts a separate owned before10660copy,
uses the original stable frame helper, and has the exact payload checks above.

Private four-file archives: coo-inventory-browse-before-earned-20261123,
coo-inventory-browse-final-earned-20261123 and
coo-inventory-browse-fresh-earned-20261123 under /private/tmp. Also preserve
coo-inventory-browse-interrupted-saved12710-20261123,
coo-inventory-browse-partial10671-20261123 and
coo-inventory-browse-input-retry-before10660-20261123. No normal saves replaced.

Final main Editor stopped/rootnull/clean/profileroff, original seven view presets
and selected index0restored. Fresh preservation audit and independent final
review are recorded separately. No packaged UI play, controller hardware, FPS,
Intel or Deck claim. Current Mac7c predates this UI; batch a coherent new package
with the forthcoming weapon-stock milestone, preserving all prior apps.
