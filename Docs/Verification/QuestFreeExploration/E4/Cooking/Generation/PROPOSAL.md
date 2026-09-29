# F10 exact row source and cooling work patch — private proposed contract

Status: source/contract review only. No production, blueprint, manifest or shared file changes. Root controls adoption and Unity; AllowRest/current readout belongs to standalone_verify and original hot/cooled art to render_completion. Current local cooking observer is separate: it uses an existing Sill campfire and does not prove this finite site.

## Source corrections before code

| Source finding | Required implementation decision |
|---|---|
| `SpreadCompositionBuilder` produces RipeCropRow at priority2000 from exact successful BuilderSpawn output, lines37–57; its Plan alone cannot prove a later same-blueprint owner came from that build. | Add `CaptureCookingSources` opt-in and one receipt per exact successful ripe row, same builder/factory/Plan/revision/zone; invalidate on rebuild/disabled capture. Reuse SpreadGenerationReceipt. |
| Current terrain receipt admission only names `OwnsPassageReceipt` (`SpreadWildernessSituationPlan.cs:57–59`). | Add narrow `OwnsCookingReceipt` alternative; never authorize an arbitrary caller-created row. Passage receipt behavior unchanged. |
| Current manager sets opt-ins before terrain build (`OverworldZoneManager.cs:67–82`). | Set cooking capture only for current selected v6 CoolingWorkPatch. No capture for F3/quiet/old v2–v5 or generic terrain previews. |
| Current FieldStrips always selects LastGleanings (`SpreadExplorationPlan.cs:103`). | Propose new fresh-v6 split below; exactly one family, with no grazer rewrite at cooking sites. A failed cooking site stays refused, never falls back to another principal. |
| Restore currently names2/3/4/current5 and caps family at9 (`SpreadExplorationPlan.cs:283–295`). | Preserve explicit version5 when current becomes6; old2/3/4/5 family/habitat literals retain their original paths. New future-header test stays999. |
| The planning census constructs an unplaced factory Player to query faction hostility. That is a labelled measurement and must not become an extra production factory call. | Production cold screening should read native `PlayerReputation.GetFeeling(FactionManager.GetFaction(actor)) <= HOSTILE_THRESHOLD` on current living Brain actors; it matches ordinary unaligned/player defaults without creating an actor. Pin matched real-Player fixture controls. This is a cold ordinary-player reputation screen, not a guarantee against future vendetta, changed reputation, AI movement or ranged attacks. Root review requested. |
| `Geometry.Place` rejects every non-bare occupant; the new coals themselves therefore must be ignored when recomputing their original bare anchor. | Final proof ignores only the exact owned new source, separately validates that source, and allows no second non-bare owner on its anchor. Critical routes may additionally avoid the coals cell, even though actual Physics is non-solid. |
| Native scenery material passes occur once per input completion, not once per clock tick. | Preserve actual .02 cooling and four-port modeled entry+walk+Harvest+walk+Cook budget<=60. Do not translate global ticks into cooling or claim player safety. |
| PhysicalObject inherits only Render/Physics/Examinable, whereas Campfire inherits light/flicker and wood traits. | Author directly from PhysicalObject; exact six parts, no legacy flame/light/loot/destruction/rest inheritance. Root thermal/Fuel fields below are explicit. |

## Blueprint and art contract

One new `SpreadCookingCoals : PhysicalObject` with no stats, loot, effects or inventory. Root-approved presentation `DisplayName="cooking coals"`, `RenderString="*"`, neutral `ColorString="&K"`, RenderLayer4, empty VisualID/VisualVariant/GlyphVariants. Renderer recommends neutral ASCII because fixed red would falsely imply heat after cooling. Root confirmed these neutral presentation literals before data tests.

- Physics: Takeable=false, Solid=false; no Solid tag, no footprint.
- Campfire: FiniteCooking=true, AllowRest=false (peer-owned new saved field, defaulttrue for legacy).
- Thermal: Temperature500, AmbientTemperature25, FlameTemperature300, HeatCapacity0.8, AmbientDecayRate0.02; other Thermal defaults untouched.
- Fuel: FuelMass25, MaxFuel25, BurnRate1, HeatOutput1, ExhaustProduct empty.
- Examinable: factual fixed base text describing leftover cooking coals; current finite readout is peer-owned. No Cooking direct world action claim.
- No LightSource, Flicker, Material combustion extras, Burning, Container, Inventory, Harvestable, Destructible, LootDrop or Rest grant. Existing native torch-lighting from true heat remains possible. Cold/spent owner remains; no replacement/removal/rubble.

Exact source creation validation must pin owned part references, unique nonempty ID, exact blueprint, initial state above, no foreign spatial/held/equipped owner and no unintended loot/light/action-bearing extra part. Source factories run only after a real row/layout passes. No created Player, ingredient or scenery marker.

## New-world family/version proposal

Append `CoolingWorkPatch=10` after FieldPassage9, CurrentVersion6. Proposed FieldStrips selection for version>=6 uses existing `Rank(seed,id,"family-variant") % 2`:0 LastGleanings,1 CoolingWorkPatch. All current quiet/exclusion/adjacency rules remain. v2–v5 literal FieldStrips remains LastGleanings, with unchanged rows/dispositions and zero retrofit. Root must approve this split; no new F9/F11 placeholder or family registration is included.

The new utility is an explicit allowance of one owner at most, with zero added food/animal/stock. Ordinary rows stay exactly where produced. v6 cooking selection removes the v5 F3 Magpie→grazer rewrite, so it is deliberately a new-world population/stock distribution change; no v5 economic or RNG equivalence claim. Paired composer-disabled validation uses the same v6 source policy, then allows exactly one new utility and unchanged caller RNG, row yields and all original stock/gear/owners.

## API and finite placement

Proposed independent helper:

`SpreadExplorationCooking.TryPlace(Zone, EntityFactory, SpreadCompositionBuilder, Entity row, Func<bool> authority, out Entity source, out Func<bool> finalState) -> bool`.

Production composer obtains only current CookingSources from its captured terrain producer. Exact row is current, unspent RipeCropRow, positive owned Physics/Render/FieldHarvest, YieldBlueprint Emberwheat and YieldCount1, no carried/equipped/quest/owned/protected/creature/foreign part. Its receipt must still be original and unconsumed. One successful claim authorizes a utility beside this row, never harvesting/moving/replacing it. Other original rows remain unchanged. Public helper has no generic live placement authority: caller must prove exact cold manager/manifest attempt and captured factory.

Geometry begins with at most2000 actual cells; build one dry physical walk mask, one bare nonreserved source mask and exact living hostile set. Station2–5 Chebyshev cells from row, more than6 from current hostile anchors, two dry cardinal row approaches and two station approaches. Remove walk cells within3 of those hostiles. Require one usable row stand and station stand with cardinal distance<=8. All four native composition ports must reach that same row stand; `distance(port,rowStand)+1(entry)+1(Harvest)+distance(rowStand,stationStand)+1(Cook)<=60` for each. Cardinal paths are valid native moves and a conservative authoring screen; actual eight-direction movement can be shorter.

Bound work using cached distance maps from four ports and at mostfour stands per actual row (normally1–3 rows), deterministic candidate ordering, at most32 eligible source positions and256 source/approach trials. Apply cheap dry/bare/range/threat filters before caps so edge/occupied clutter cannot starve valid sites. No exhaustive geometry rebuild inside every tuple. Keep the original critical component proof, required lanes/ports and stairs, and require a bypass avoiding the exact coals cell. Refuse long/blocked/unsafe layouts without modifying terrain or inventing a source. No new terrain shaping is necessary for this first variant.

Transaction: capture all original non-bare owners and all original row owners before factory. After factory and before AddEntity recheck exact cold attempt/producer/receipt, unchanged synchronous originals, row state, unique unheld output and current layout. Consume only selected receipt, place the one source; rollback only that same owned source if still the admitted instance, never undo foreign replacements or arbitrary callback mutations. A failed callback makes the generation fail when originals changed; ordinary no-layout refusal preserves everything.

Final validator through existing strict4arg TryMarkPlacementCommitted repeats current attempt, producer reference, exact selected row/source/all original ripe rows, anchor/parts/heat/fuel, current hostile clearance and full four-port/critical route proof. Allow normal unrelated LocalPeople naming after synchronous helper return. No stale receipt refresh, no callbacks saved, no source recreation on load/return. After acceptance, actual harvest/cooling are saved ordinary consequences and do not invalidate or reinitialize the committed owner.

## Test-first packages before production

1. **Version/source/blueprint reflection RED**: missing exact child; fresh6/new family; literal2–5 retain headers/rows and cannot admit family10; future999/wrong seed/duplicate/corrupt habitat still refuse. Selected capture enabled before terrain versus disabled/quiet/F3 counters; original producer revision/zone/factory/same-ID decoy/rebuild/opt-out/moved/harvested/changed-yield rows cannot authorize utility.
2. **Helper actual-API RED**: useful canonical four-port route produces exactly one source while row unchanged; long61-pass versus exact60; any one port disconnected; required-lane/occupied/water/hazard/hostile-near source and route masks refuse. Harmless off-route control. Bounds and zero source factory calls on preflight refusal. No source manufacture if one allowed row absent or no layout.
3. **Factory/late callback adversarial**: exact source removed/moved/replaced/malformed/duplicate ID/part transferred; row mutation/harvest/source receipt or manager attempt swap; second anchor occupant/late route blocker fail, unchanged/off-route controls pass. Preserve foreign replacement and original stock. One-consume and no second invocation source.
4. **Full fixed1/64/1729 pipeline**: report all eligible FieldStrips, selected families, original one-unit rows, source-compatible layouts, committed/refused and generation milliseconds; actual paired same-v6 no-composer yields exact original source/stock/RNG plus0or1 coals. No denominator or seed-shopping shortcut. Include explicit v5 source fixture where a historical source assertion depends on old distribution.
5. **Saved aftermath**: actual harvest and native cooling passes, full inactive cached graph save/load retain exact source/row IDs, spent row, heat/fuel/rest flags and disposition without new factory placement, no old-save backfill; actual recipe/core tests remain separate. Native passage of time/player route/reader/pixels/torch interaction stay root gates.

Root reviews this contract before production. First executable tests may remain compile-compatible through reflection; executed private RED precedes private implementation; root-native RED precedes shared production adoption. Scope excludes peer Campfire/readout/presentation, generic material changes, source-framework rewrites, new animals and any F11 implementation.

## Executed initial boundary and fixture correction

Root approved this contract before production. Initial37 actual current-source results:29 expected featureRED/8 controlsPASS. The first candidate exposed14 positive-fixture premise failures: the initially chosen10.15/BrokenEnclosures row is65,16; independent route oracle proves zero useful<=60 candidates. This is a correct optional refusal. Controlled helper fixture now uses pre-existing ActivationPlan census10.5/BrokenEnclosures with a documented59-pass useful row at32,7; no production seed/address selection changed. Full fixed1/64/1729 pipeline denominator remains owned by the independent fixture. Original failed run and independent diagnostic retained.

The first private implementation also wrongly spent capped layout trials on approaches already impossible under cached60-pass distances. Retained second37 run has14 failed positive/callback premise checks; independent exact32,7 oracle found18 usable layouts while candidate Find refused. Move the cached finite-budget admission before the32-position/256-full-geometry-trial caps. Cheap scan remains bounded to the11×11 local square and<=16 cached row/approach lookups per position; no route/safety/budget condition is relaxed.

Root independent output-contract review prompted three real factory probes before repair: unchanged controlPASS; canonical attached BurningEffect controlPASS (the new StatusEffectsPart already violated exact six-part authoring); hidden Render1RED. Valid now explicitly requires Visible and no StatusEffectsPart. The malformed hidden/burning owner is not repaired or inserted. Exact raw output-contract-red.xml is retained.
