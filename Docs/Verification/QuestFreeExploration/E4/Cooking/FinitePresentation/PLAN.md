# Finite cooking rest/readout sub-slice — before production

Private only. Root approved this slice after core39 + neighbors97 native GREEN. No new station/data/family/version/placement, no Unity/Git/shared writes. Existing core39 and CookingService remain frozen.

## Sweep corrections

- CampfirePart currently offers both rest commands, direct execution, red/yellow Render override and warm proximity line independently of heat. Preserve this legacy behavior. Add explicit saved AllowRest=true; false refuses both gathered and direct rest before HP/clock changes, without changing RestSystem.
- LoadPart uses Activator.CreateInstance (SaveSystem.cs1495); missing fields receive constructor initializers. Execute actual zero-field and FiniteCooking-only wire, not assumed defaults or historical-byte claims. New false field must survive full graph save.
- FiniteCooking is already a saved default-false opt-in. Only it suppresses glyph flicker/crackle; AllowRest is independent. Do not pretend the separate ZoneRenderer ember registration is suppressed by this slice.
- ExaminablePart has no generic part description event. A narrow focused world-reader append can call CampfirePart's pure current source readout. No event polling, source writes, hidden/foreign-cell state, world Cook action, or per-frame inventory scan.
- Use CookingService.MinimumFiniteCookingTemperature. Enough heat + positive finite fuel means cooking-ready, not necessarily burning; hot+fuel0 remains hot but unusable; below threshold is no longer hot enough, not absolute cold.

## Contract and tests before implementation

New separate fixture FiniteCookingPresentationTests uses actual current Campfire content, rest actions and full SaveGraphSerializer paths. Paired old/new rest, direct/mapped commands, glyph/proximity, heat/fuel conditions and current-visible ownership; reflection keeps missing AllowRest tests compile-compatible. Description tests use actual BuildWorldExamineLine rather than a disconnected string helper. Exact time/HP/energy/source and no new action assertions bound read-only behavior. Snapshot borrowed globals through existing Hotbar fixture plus explicit direct state used by isolated runner.

Implementation after executed RED only: CampfirePart public AllowRest=true; guard rest contribution/execution and finite glyph/crackle; internal current readout; minimal ExaminablePart world-reader append. This is not a general readiness framework. No changes to cooking transactions, threshold, heat cadence, authored recipes or legacy station policy.

Q1 rest gather/direct share opt-out; Q2 readiness/thermal/rest independent; Q3 hidden/stale/old-wire/current paired controls; Q4 quietness claim covers glyph/crackle only, not native ember/light/model activation.

Executed baseline:35 cases,10 legacy controls PASS,25 expected missing/new behavior RED,0 errors (initial-red.xml). Production started only after this receipt. The private runner uses existing Unity-free fixture shims/stable hash, so native remains unrun.
