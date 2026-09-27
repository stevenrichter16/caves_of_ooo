# M4 sweep and readiness

Before production edits, 27 September 2026, baseline 0417e145.

- M0 sees existing Sill stock and nearby gear: no forced near-start pair or extra
  reward lead. E1 will be a rare regional alternative to an ordinary group.
- PopulationTable.Roll returns blueprint names, and current SpreadTier1Encounter
  contains only Viper and MarlbackScrabbler; mixed spatial placement needs a narrow
  pre-population transaction. Unselected sources must keep their existing RNG.
- Lunge attacks without moving. Use real ShortSword, Cudgel and one LeatherCap;
  clear inherited Pick and optional armor, keep actual Scrabbler stats.
- World metadata already has ordinary serialized Entity.Properties and save/load
  binding points alongside LairStacks. A bounded versioned address list fits
  those primitives; no format bump or global ledger is required. Old missing
  metadata must disable insertion, not initialize it.
- Generation pipeline retries reuse builders. Any committed/failed marker must
  be tied to the current graph/actual entity ownership, never a stale builder bool.
- Current map authority, reserved cells, body footprints and callback validation
  gate commit. Habitat refusal retains the complete ordinary encounter group.
- Exact model and corpse mappings are required; gameplay activation cannot ship
  while a new blueprint falls back to a glyph or unapproved placeholder.

Initial readiness was yellow pending RED and art. Current E1 source, art and staged
native acceptance are complete; final integration/publication remains pending.

## Implementation and verified bounds

- `SpreadRareEncounterPlan` freezes one original pair address with a stable rank,
  using current Spread/tier/formation/POI authority. Metadata uses ordinary World
  properties; an old missing plan or a saved empty plan never silently rerolls.
- `SpreadRareEncounterBuilder` requires an unreserved reachable dry hedge pocket,
  its actual manager factory and exact authored equipment. Callback, blocked-path,
  malformed-body and commit refusal retain the ordinary group. Only successful
  placement replaces the Spread hostile group; unrelated population stays intact.
- The notched ditch-cutter owns a short sword and leather cap; its slate-backed
  mate owns a cudgel. Both retain ordinary marlback stats. Core lifecycle tests
  exercise real Lunge/cooldown, assistance LOS, gear spill/pickup and changed-state
  save restoration with exact identities. A miss remains a valid attack.
- Pair48 and warned-source40 pass together in actual Unity (88/88), archived in
  `../M5a/source-native-green.json`. Three ordinary generated pair zones each
  admit the pair; this is source evidence, not walking discovery.
- Final imported art29/29 and 18 reviewed gallery frames are recorded in
  `../E1Art/`; only the exact optional pack is new.
- Native run `8771d89108f84346be0fa9d76c55bcdd` passes all13 named checks with
  zero unexpected errors: disclosed staged arena, real keyboard bypass, original
  dagger/control spells, player kill, same cap/sword acquired, free real Compare,
  F5, real item/position mutation and F6 restoring the exact graph. It takes20
  local inputs, four dagger attacks and zero tonics; final HP32/40. All five
  frames are visually reviewed. Native tactic use was not observed and is not
  claimed. The remaining mate, currency, effects/abilities, ground, turn/energy
  and original corpse/item identities survive exact restoration.

## Q1–Q4 self-review

1. **Symmetry:** selected/unselected/refused sites, moved factory authority,
   ordinary group retention, fresh/old/empty metadata, equipped/disarmed/dead
   ownership and mutated/reloaded graphs have paired controls.
2. **Consistency:** ordinary equipment, combat, body, save and approved rendering
   owners remain authoritative. No global rarity, acid, damage or save-format
   tuning was introduced for the encounters.
3. **Counterchecks:** sealed pockets, creation callbacks, mismatched factories,
   missing content, footprints and exact original item graphs are covered.
4. **Documentation:** generated-source, staged combat and gallery claims are
   separated. This does not prove ordinary discovery, all-seed safety, general
   balance or the separate M1 quest/shop journey. Full integration is pending.
