# Worksite connections — November 24, 2026

Three new bounded capabilities connect spare equipment, camp storage and an
existing assembled weapon. Implementation and native/Editor verification are
complete. Independent source and recorded-play review found no remaining P1/P2;
the three capabilities are accepted within this bounded scope. Runtime9a is
committed, pushed and remotely verified. The [current bounded Mac delivery](../MacCandidate9A0D55F93/README.md)
includes this batch. All twelve complete-machine gates
and the inherited failed full regression remain open.

| Capability | Cost and result | Useful alternative |
| --- | --- | --- |
| Spare leather armor recovery | One plain, sound, unworn LeatherArmor + usable cutter -> one CuredLeatherOffcut, one action. Armor and stitching are lost; cutter is kept. | Wear, repair or sell armor; buy prepared leather or a finished binding. |
| Work from an own camp box | Select an adjacent installed player-held CampBox. Primary and extra inputs come only from that box's direct contents; tools stay carried/equipped, output goes to the ordinary pack, one action. | Take materials out and use ordinary carried preparation or an appropriate workshop. |
| Field rebinding | Carry one untempered assembled weapon, one replacement binding and a separate usable cutter. One action; old binding destroyed, head/haft/weapon identity kept. | A normal forge refit returns the displaced component and can replace other slots. |

Box work supports the explicit MaterialProcess catalogue. It does not search
nested containers, borrow pack ingredients, grant access to town chests, or
make every legacy preparation recipe a storage action. Work requiring a forge
still requires normal workshop access. No tool wear, routine upkeep, weather
penalty, production clock, material-tag yield inference or new canon was added.
The leather manifest is an explicit gameplay abstraction, not rawhide tanning.

## Source corrections and verification

The engine is reused: Armor supplies a manifest-only permission adapter on its
existing saved Part; an explicit input Part, even a denying one, takes priority.
Input-only scalar policies do not authorize armor/repair output creation. Armor
material commands own their transaction receipt so a failed recovery cannot
rewind independent actor injury; a separate real1-case RED reproduced and fixed
that generic fallback-snapshot problem. Empty
equipment contribution receipts compare consistently, while nonempty receipts,
non-null repair references and unknown payloads cannot be laundered by splitting.
LeatherArmor's real Hitpoints statistic is preserved by splitting and fully
compared on merging. Ordinary equip-one-from-a-stack remains supported.

Box work adds an exact custody/access receipt to the existing material planner
and service. Claims precede callbacks; ordinary output capacity and merge checks
remain. A shared reserved-unit payment inverse preserves independent remainder
transfers and injury. Foreign Parts, unsupported changed payload and invalid
count bounds are preserved and diagnosed rather than receiving a fabricated
refund. Arbitrary forced raw-state edits are outside the representable rollback
contract; normal refusals and supported callback failures restore payment.

Field binding reads component definitions without creating transient entities
or a returned component. It uses the existing component-stat math/permanent
modification restoration, without clearing temper or writing HP. Tempered,
equipped or stacked source weapons refuse; only a binding can be changed.

Valid compiling native checkpoints are under Native/. Initial armor15 had
7 passes/8 failures; its incorrectly invoked equip action was a fixture defect
and is excluded as production RED evidence. The real equipment command was
used thereafter. Box RED16 had8 failures/8 controls, field RED12 had6/6. Separate
real REDs cover altered-payload refund1, guide/menu invalidation2, world box
dispatch1, field action time1, and F1 box quote identity1. No-clone/busy-clone
preflight refusals ran no tests and are not RED or acceptance evidence.

Final run worksite-three-regression-20261124g: **287/287, zero compiler errors**;
72 new cases and215 retained cases, including processing, multi-stack claims,
Spurgrass, tool guides, refit modifications and stack identity. Earlier overlapping
greens are retained, not added to this count. This does not clear the inherited
full suite:40,813 total/40,692 pass/104 fail/9 inconclusive/8 skip.

Actual play found two issues that command-only tests missed: per-redraw use
guides replaced executable quotes, and world F1 could not match a regenerated
random box token. Pure IDescribeUseRows adapters fix the former. The box cache
now retains the exact unchanged quote/token across reading, invalidates changed
promises, prunes unavailable rows and removes a token on use. New box and field
rows declare one-action dispatch explicitly.

## Actual Editor play

All actions used GameBootstrap and the native keyboard/UI. No runtime supplies,
stats, positions or ingredients were injected. The supplied camp fixture and the
earned M3c weapon branch stayed separate; their inventories were never merged.

- Supplied camp1840: armor2333 x3, box2150 empty/14HP, equipped cutter2127,
  player358HP. Native armor menu spent one armor -> offcut2728 at1850. Native
  put-in-box was free. Actual C-menu work spent that stored offcut -> binding2729
  at1860; box14HP, armor x2, health358 and purse50 remained.
- Fresh1860 preserved the selected identities/counts. All four armor-reader pages
  were free. Normal left/right movement ended1880. The existing cold coat and
  SteepedAcid expired normally; hearth fuel/temperature advanced. No wholeworld,
  save-byte or full-temperature equality is claimed.
- Stored armor2333 x2 at1880 then used the same recovery contract directly from
  the box. Actual F1 displayed the entire cost/loss and the box instructions in
  two free pages. Closing restored the exact selected command; native execution
  cost one action to1890, leaving armor2333 x1 stored, binding2729 x1 and new
  offcut2730 x1 carried, box14HP/player358. Fresh1890 preserved all selected state
  and the remaining work row; cancellation spent nothing.
- Earned M3c source4420, Cinderhold6.6 at57,6,40HP/4drams: axe13682 was equipped,
  so field work was absent. Free unequip exposed the row. Native field refit
  at4430 spent PlainCordBinding13695, destroyed the old GuardLashing, kept the
  axe/head/haft/cutter2679, and changed hit0->-2, penetration-1->0. Damage1d8,
  strength cap1 and the head's bleeding effect remained. The forge was out of
  reach throughout. All six weapon-reader pages were free. Fresh4430 retained
  the result/no spare binding; free re-equip and two normal moves ended4450.

Every native F5 checkpoint's four files was archived before Finish. Selected
observations and SHA receipts accompany each branch. ArmorMenuFailure1840 and
BoxReaderFailure1880 are preserved free failures, not successful feature evidence.
StartupExcluded retains the copied files from an interrupted hot-reload startup,
without a gameplay checkpoint claim. The owned Editor was restarted after compile
readiness stalled; its scene-recovery backup was preserved byte-exact outside
Assets, not imported over the scene. An ambiguous OpenScene response was checked
before continuing. See editor-restart.json.

Five unedited gameplay screenshots:01 is the excluded initial armor-menu attempt;
02 recovered material/use/storage,03 actual box work menu,04 field loss preview,
05 changed weapon stats. No additional gameplay screenshot sequence was made.
Source/native/Editor evidence does not prove packaged actions, combat balance,
ordinary opportunity density, Intel/controller behavior or FPS. No new hardware
or population-survival qualification is claimed.
