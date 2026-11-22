# Wells and ordinary liquid flasks — next bounded increment

Proposed after RegionalClay20261122; not implemented or accepted. Restored
Gleaners well9998 works for direct drinking and source-supported Waterskins,
but generic LiquidFlasks need a pool. The reader broadly promises vessel filling.
Use the existing water rules wherever practical vessels make the same request.

Independent source critique also found a related admission gap: generic flask
ValidSource currently accepts a positive pool-backed Well without checking its
structural repair or TentRightOath refusal. A correction must cover every Well-
backed flask source, not just pool-less wells. This is source review, not yet a
native RED reproduction. Keep unrelated non-Well pool/tray callbacks separate.

Bounded contract:

- Reuse WellDrinkingService.SourceAvailable and actor oath rules for every Well.
  Preserve literal no-Physics wells; reject ambiguous contained-source/Well owners.
- An attached water pool supplies finite quantity first. Empty pool never falls
  back to Stock. With no pool, positive Stock is finite,0dry,-1legacy endless;
  values below-1invalid. Do not invent a pool or change the source's existing tariff.
- One source unit becomes one flask-volume unit, matching waterskin charge units.
  Fill min(free capacity, available quantity); endless-1unchanged. Drinking relief
  remains separate. Empty or same-water flask only; never purify mixed water.
- Bind source/recipient identities, current Parts, custody, anchors, exact quoted
  capacity/amounts and repair/access/phase/safety. Revalidate after callbacks and
  before commit, including intentional exhaustion. No grants/static cached yield.
- Bounded debit/credit undo preserves independent injury, stock additions, drops
  and replacement Parts; diagnose unrepresentable raw mutations. Narrow native
  receipt bypasses blanket actor snapshot. Completion/audio/visuals follow commit.
- Preserve exhausted Well owners. No poured-pool retirement, new saved field,
  per-town dispatcher, forced upkeep, water scarcity retcon or hydration benefit.
- Existing WorldInteractionSystem.CollectToolRows projects AtEntity/InGroup Fill
  rows with exact Via owner into C menus. Declare OneAction explicitly for both
  pack and world menus. Identical counted flasks choose the intended member.

RED/countercases: finite3->flask3, endless-1->capacity, finite pool precedence,
dry pool/stock, repaired/broken well, oath refusal, frozen/ice/mixed water, literal
no-Physics source, same-water top-up, unlike/full/stacked recipients, ambiguous
owners, stale quoted amounts/Parts/anchors, outer veto, re-entry, independent
additions/injury/drop and source/output Part replacement. Existing ordinary
pool/tray transfer, Waterskin and direct-drink regression must remain covered.

Actual gate: reuse final earned3640 restored well but first capture the missing
generic-flask menu. After correction exercise real Inventory and C-menu member
selection/free cancellation; carry water to a useful existing cooling/crop use.
Finite stock requires a real finite source and normal F5/fresh Continue accounting;
the legacy endless Gleaners well cannot establish that gate. No fabricated thirst
or renewed repair bill. Independent source/gameplay review and current Mac follow
accepted implementation. This plan does not promote any full-machine gate.
