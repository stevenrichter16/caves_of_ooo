# Lampwell hearth backing source review

Independent read-only reviewer: `/root/build_comparison_plan`, 2026-11-15.
Verdict: **0 runtime P1/P2**. Actual visual acceptance is separate and pending.

Reviewed catalog SHA256 `6cd6c778ba33b5673f4dd7803d97e7556178f253bfb506ea4cfd7e9e0b5f05d7`;
initial fixture `b62ddd0e768b587b10944b301d916a7f8ef204fc8a0e3cd4b8416e8d5f25fd58`.

The flag belongs only to the Lampwell companion and additionally requires exact
`Overworld.12.3.2`. Current graph, disclosure, resource and TileState checks remain.
The local finite owned-part validator admits the canonical live HearthPatch without
events or changes to light, collision, politics, readers or shared ending vetoes.
Material ground still requires an actual floor. Hearth-only and Rubble paths use
AmbientBackdrop; Stalagmite, duplicates and rejected real floors refuse.

Bounded test improvement requested: invalid-owner cases over real floors do not
independently exercise ambient refusal. Add Hearth-only positive-to-invalid
duplicate/foreign-role cases. Elsewhere tests through the Lampwell adapter prove
admission scope; separate existing caller suites retain their own public-path checks.

Root added the two Hearth-only ambiguous-owner controls; native 2/2 passed at 19:24:03Z. Final fixture SHA256 `0f9cf066006a6a3c2fc025529ad349240072ae5065d0c3872972515af046a906`. Runtime is unchanged from reviewed hash.
