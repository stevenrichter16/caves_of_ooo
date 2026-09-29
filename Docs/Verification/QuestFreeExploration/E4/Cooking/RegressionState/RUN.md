# Private matched diagnosis (not native Unity)

The isolated runner copies `/tmp/coo-questfree-implementation/cooking-cadence/runner`; all commands use `COO_REPO=/Users/steven/caves-of-ooo`. No repository sources or shared runner files were changed.

1. Copy `current.props` to private `runner/tests.props`, then run `runner/run.sh 'class == CavesOfOoo.Tests.TorchReactionStateProbe' <private>/current.xml`.
2. Copy `before.props` to `runner/tests.props`, then the same filter with output `<private>/prior-thermal.xml`.
3. Restore current props and add `MerchantLoadoutStateProbe.cs`; run `runner/run.sh 'class == CavesOfOoo.Tests.MerchantLoadoutStateProbe' <private>/merchant-state.xml`.

Each run built successfully. The last complete build log is retained; earlier build logs were overwritten by the same private runner. Both torch XMLs intentionally retain the two original assertion failures: NUnit records those failures even though the probe catches the assertion to inspect fuel/intensity/HP. No claim of a green original torch assertion under enabled reactions is made. The two merchant counterchecks pass.

This .NET runner uses the existing repository gameplay sources, Unity API stubs and its documented deterministic hash substitutions. The torch/merchant checks do not generate maps. They establish matched arithmetic/static-state dependence, not native rendering or native full-suite success. Root owns native receipts and the clean-domain rerun.
