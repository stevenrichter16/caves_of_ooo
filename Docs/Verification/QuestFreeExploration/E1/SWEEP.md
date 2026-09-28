# E1 manifest sweep and API readiness

Private only; shared production held behind root baseline capture. Read the current quest-free plan, CLAUDE, generator/manager/save/receipt boundaries at 8b01bf02.

Corrections before implementation:
- Map/ZoneManager.cs is the actual lifecycle owner. Ordinary travel caches; explicit Unload removes graph+connections; settlement changes are the observed unload caller.
- Existing three-argument internal manager constructor uses activate:false for both load and fresh detached worlds. Add a distinct four-argument fresh-manifest flag; retain default-off activation until E2 is ready.
- SaveGraphSerializer builds and attaches cached zones before World entity bodies are hydrated. Attachment cannot infer fresh installation. Decode into an explicitly unbound exploration state; only validated post-body restore binds authority.
- Current v1 selector's gleanings label is not the row placement gate. Existing ripe rows stay unchanged in this phase.
- Full graph/stock RNG is not made visit-order deterministic by the manifest. Only finite assignment/variant/package seeds are guaranteed here.
- New v2 assignment catalog is closed to None plus RoadSpill, OccupiedBank, LastGleanings, WateringMargin. Public topology enum: Legacy, OffsetLanes, BrokenEnclosures, BankCrossing. Definitions remain metadata until root E2 activation.
- Separate frozen placement/persistence masks: actual authored-supported Spread no-POI ordinary graphs may retain; glade/rare/Wayhouse and regional exclusions cannot receive new placement. Settlements/specialized POIs and authored special pipelines keep their lifecycle.
- Runtime producer receipts remain ephemeral and are not serialized by this plan.

Proposed integration: public immutable entry collection, Find pure frozen lookup, TryGetPlacement current authority query; explicit fresh CreateDetached overload; manager-owned exact final-accepted graph references. A tiny first pipeline guard records exact generation attempt/plan using weak keys; final acceptance refuses callback plan swaps. No generation/RNG in queries. BindForSave writes bounded scalar metadata; absent means legacy; corrupt/future data or installed-without-graph throws before global activation. Old default manager constructors stay disabled.

Test-first fixtures use reflection for absent APIs so current native assembly compiles and produces meaningful missing-feature RED. Actual legacy/unload controls and full save graph checks are separate. Isolated runner uses COO_REPO and only copied runner configuration; no tracked runner changes.
