# Counted inventory groups — November22,2026

User priority: five empty flasks share one counted entry; three flasks holding
the same liquid at2/12 share another. Extend the rule to other suitable items.

The first increment is counted Inventory presentation with retained individual
owners. Existing physical Stacker quantities and command/save behavior stay
authoritative. Separate grouping metadata contains member displays, selected
member, total units and total weight; never overwrite ItemDisplay.StackCount.

Compare complete audited known-type scalar payloads, exact blueprint/name,
condition/stats, tags/properties, liquid ID/volume/capacity, water/tool charges
and treatments/lifetimes. Unknown Parts, malformed state, active effects/claims,
stale custody, equipped items and stateful containers remain separate. No blanket
name/clipped-badge or incomplete StackPayloadIdentity comparison. No extra
processing/physical merge permission. Keys are internal ephemeral data, never UI
text. Cache identity at model rebuild; row paint/search/resize must not rescan
Parts or invoke providers. Filter retained snapshots before grouping.

Grouped is default. G and a painted control toggle individual entries for exact
member selection. Preserve the selected owner as its group's active member
through grouping/search/readers/model changes. Commands affect that owner only;
scope must say one item or the selected physical stack's actual size. Aggregate
quantity/weight belongs in the row/pane, owner facts in the existing reader.
No implicit group-wide drop, transfer, sale or use. A used flask whose contents
change automatically leaves its prior group on the existing rebuild.

Bounded independent design/source critique accepted this direction with these
constraints; existing reviewer identified physical StackCount/action scope and
CanMergeProcessingInputs' nonpayload true fallback as specific hazards.
Meaningful RED/countercases: requested5/3 examples, all content/charge/capacity
and condition differences, unknown/malformed/hidden/custody/equipped exclusions,
existing physical-stack totals, exact selected member, no providers on query/
resize, use/drop only one owner, cancellation, regrouping and actual fresh save.
Actual gameplay, targeted native checks and independent final review must follow.
This is the implementation contract, not a completion claim. Water-cooling P2
remains separate; this explicit inventory priority is now first.

## Bounded qualification

[InventoryCounted20261122](Verification/MachineSystems/InventoryCounted20261122/README.md)
records the implementation,30new/329final native passes, isolated mutants and
actual five-empty/three2-of12 examples. Selected-member fill/pour/drop, two-page
reader and fresh2700 preserve quantities and exact owners. Unknown stateful graphs
remain conservative exclusions. The original contract above is historical;
this receipt supplies the bounded completion and its explicit limitations.
