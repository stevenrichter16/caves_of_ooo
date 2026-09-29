# F9 observer correction: movement facing

The actual first hauling Play run69943f81cda740bca84e1617239611f1 failed its immutable-facts observer after one successful paid pull. It did not demonstrate a hauling gameplay defect. The load moved to the vacated player cell and the native energy/grip checks passed.

Six paired core cases fail the old signature (beam/barrel west/east/north), ten controls pass. The field-level JSON difference is exactly RenderPart.VisualFacing, South to the real movement direction. South controls do not change facing. ID, Physics.Weight and RenderString mutations remain detectable. Both real post-pull facing/save reconstruction cases pass.

The correction excludes only RenderPart.VisualFacing from the authored primitive signature. Initial and untouched hedge facing remain pinned; every pull must face the actual direction, free menus/captures must preserve facing, and inactive save/load/return must retain the actual parked facing. Existing owner/parts/grip/position/shape/material checks remain. No gameplay, RNG, input cost, source or rendering change.

Private core after the change:16 passed, zero failed/skipped. This invokes the shared HaulFacts body extracted verbatim and its candidate replacement by reflection in a private core shell; it is not Unity execution of the scenario MonoBehaviour. The permanent16-case fixture instead reflects the actual compiled runtime scenario method, with the existing HaulingContentScope isolation. Actual Unity bundled Roslyn/reference compilation passes for runtime and EditModeTests; native execution is root-owned and still required.
