# Iteration 04 — collect the actual natural solution

Implemented: finite BrinePool and MirePool can fill the ordinary liquid flask,
then pour brine or bog mire using their existing effects. Their legacy water
terrain source describes a reaction family, not a second pure-water ingredient.
`EffectiveCoating` projects the literal pool identity; `GroundReactionFamily`
lets the existing heat/cold/electric rules consume that literal coating.
Independent water, acid, oil or another unlike pool still blocks collection.
Frozen solutions cannot be sampled. Drinking-water eligibility remains unchanged.

## Sweep and design correction

Changing the purity check to ignore water would allow a mixed cell to produce
pure ingredients. Changing the blueprint coating alone would disable its water
reactions and leave existing saved Parts unchanged. The shipped bounded bridge
therefore resolves the legacy source against the live liquid definition and
matches reaction families only inside reaction resolution. Purity remains exact.
Two liquid definitions changed; Objects.json was not edited for this iteration.
Existing saved short water leases may need to expire before sampling; they are
not silently erased, because an independent spilled-water lease has no separate
provenance. Generic fluid mixtures and distillation remain unimplemented.

## Evidence and review

- Executed standalone RED: 18 cases, 6 intended failures, 12 passing counters.
- Initial GREEN: 18/18. Added six independent-pool, temperature and one-shock
  counterchecks; no extra RED claim for those already-passing counters.
- Combined root regression: 100/100, including 24 natural-liquid cases,
  25 caretaker cases, existing flask, terrain reaction, conductor and settlement
  tests. Two old terrain assertions now pin literal brine instead of phantom
  pure water; their live electrical reaction test remains unchanged.
- Heat/cold consequences, exact volume, outer rollback, unlike liquids, drinking
  refusal, freeze phase and suppression of duplicate dry-conductor shock checked.
- 🧪 Native Unity and Play sanity are pending the final synchronized run.
- ⚪ Original CoO extension; no new Qud parity claim. No graphics change.

### Propagation counter-review

A further whole-sheet test exposed one real regression: literal bog mire has body
conductivity 0 and its pool material is below the propagation threshold, so
removing fictitious water also removed ground charge travel. Observed RED26:
25 pass, MirePool propagation fails. Ground propagation now consults the same
explicit reaction family; body coating resistance/conductivity remains unchanged.
Dry adjacent cells remain nonconductive. Final native sweep will include this fix.

Standalone confirmation: 102/102 combined cases pass after the propagation fix;
26 are natural-liquid cases. Cryogel ground-action review is recorded separately
in ReviewLiquidFreeze.md. Native verification remains the final gate.
