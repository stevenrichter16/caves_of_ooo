# Approved starter audio runtime export

Status: export complete and independently verified; 25 unit cases GREEN after the original missing-module RED and a four-case all-offset-bound RED. All85 runtime WAVs and the catalog are copied into Assets after parent baseline approval. Parent task owns Unity playback, importer tests and live integration. No source recordings, approved revision2 audio or manifest are edited.

## Source pin and split contract

Approved revision2 manifest SHA256: `0f32e12303f04e5691b3f801b57760064d68d987d7f6cc51fa5ea6fea7289ab7`.

At reference contact C, neutral layers with actual precontact content are divided by a 20ms raised-cosine complementary crossover beginning at C. Prefix remains cast-relative and fades out through C+20ms. Suffix is contact-relative, starts at zero and fades in through its first20ms. Prefix is rounded once to PCM24 integer samples; suffix is the exact original integer minus prefix. Adding them back at C reproduces each approved layer bit-for-bit.

Any layer already silent through C is exported suffix-only, preserving all its approved samples and native fade. All outcome-gated layers have been checked to meet this condition. A future gated layer with nonzero precontact content or a nonzero initial contact sample is refused, because splitting it would leak a success cue before actual contact or add a discontinuity. Entirely silent phases are omitted and represented by null metadata.

Gate names agreed with runtime owner: always, target (Jet wet_slap), moved (Ground Surge grit_displacement), frozen (Rime brittle_lock and settling_fragments), pacified (Calm resolved_overtone), watered (all Conjure Rain layers). Prefix and suffix belong to the same material layer; optional null phases do not consume sources. Up to4 layers ×2 sources per voice remains sufficient.

## Verification

- Tests first: integer reconstruction, source preservation, crossover endpoints, silent phase omission, gated precontact refusal, invalid format/ranges and exhaustive optional-subset peak math.
- Verify all182 approved audio hashes before any staging mutation; require pinned manifest.
- Encode mono48kHz PCM24; independently reload every staged file and reconstruct the original layer and approved full-success mix (the mix permits only pre-existing per-layer quantization error).
- Measure four-times oversampled peaks for contact shifts0..1s inclusive in10ms steps, considering any combination of gated layers. Optional subset extrema are evaluated analytically per sample; this equals exhaustive subset maximization and is separately counterchecked by enumeration.
- Also bound arbitrary relative offsets, including contacts beyond that grid: for each variant and optional suffix subset, `maxabs(sum prefixes) + maxabs(sum selected suffixes)`. The triangle inequality covers any relative offset of the two synchronized phase families. Four-times oversampling is an intersample-peak estimate, not a proof over every possible analog reconstruction filter.
- Compute one fixed common runtime gain across all spells, variants and layers if shifted overlap needs it. Staged clip samples remain unchanged except complementary partition; catalog documents this gain. No per-layer renormalization.
- Shifted contacts retain smooth clip boundaries but cannot reproduce the original full reference waveform; the timing difference is the intended runtime adaptation. This check does not replace live DSP/listening verification.

## Results and runtime contract

The final catalog is `runtime/catalog.json`, SHA256 `4512b7cb8440ab25b59e8cb69b2cfa60ffd93935708e7debaaa1f12d07588d5e`. It contains six spells,18 variants,60 material layers and85 non-silent phase clips. `runtime-verification/03-independent.json` independently decodes every output without importing the exporter, validates all182 approved input hashes, restores every layer exactly, and explicitly checks5,757 mask/contact combinations. The full-success reference mix differs by at most2 PCM24 least-significant units from original independent-layer rounding.

All six new spells play uniformly **2.19 dB quieter** than the approved full mixes: common runtime gain0.7768969. This leaves room for changing the contact delay while preserving relative material levels. No clip is independently normalized. Exact sample reconstruction applies before that common runtime gain; ordinary heard playback is deliberately attenuated. The maximum four-times oversampled grid peak is0.890505485 before gain and0.691830950 after gain (−3.20000026 dBFS). The conservative arbitrary-offset bound is1.107535527 before gain and0.860440918 after gain, below the0.95 safety target even after Unity float32 conversion.

Resources are flattened into `Assets/Resources/Audio/StarterSpells/`; load each phase using `Audio/StarterSpells/` plus its `resourceBase`. Nullable prefix/suffix means no source is needed. Catalog `file` and `audioHashes` keys retain their offline staging-relative `wav/` paths for traceability; the runtime loads by `resourceBase`. `runtime-verification/05-assets-copy.json` checks all85 copied WAV hashes against their already independently verified staging bytes. Catalog metadata was extended with the arbitrary-offset bound without changing those WAVs or common gain. The prior grid-only export is retained outside Assets for review.

Credits are shipped outside Resources in `Assets/Audio/StarterSpells-SOURCES.md`; only the21 actually used recordings appear. This preserves creator/publication links, CC0 license URLs, file hashes, material assignments and explicit proxy/download limitations.

## Performance and implementation boundaries

All source I/O, partitioning, checksums and oversampled peak calculations happen offline. Runtime receives cached clips and catalog constants; no per-cast waveform construction is proposed. The export tool itself refuses Assets output paths; verified copies are an explicit integration step. No Unity controls, camera or gameplay mechanics are touched by this subtask. The tool fails before writing if approval hashes or input format differ. It refuses an already-existing output directory so previous exports remain reviewable.

## Review log

- Initial generic Python in the nested folder resolved to3.14 without numpy. Corrected invocation to the installed Python3.12.12 with numpy/scipy; only the subsequent missing-runtime_export result counts as RED.
- Outcome clips were inspected rather than assumed: all24 gated layer variants are zero through C, so suffix-only exports can preserve their exact approved attack.
- 🟡 Fixed before handoff: a finite contact grid alone did not cover unusual frame hitches or contacts between measured delays. Added the independent-offset bound and a gain policy that must satisfy both grid headroom and the arbitrary-offset limit. Four new tests reproduced RED before implementation; all25 pass afterward.
- 🔵 Cold-eye: source maxima and suffix subset maxima must be evaluated separately. A full-success sum can hide peaks cancelled by a layer that later gets gated out. Explicit subset enumeration in the independent checker confirms the analytical bound.
- ⚪ Deliberate divergence: runtime timing follows renderer contact and actual outcomes, so shifted or unsuccessful casts cannot sound sample-identical to the approved full-success reference. The uniform2.19 dB attenuation is also intentional and visible in the catalog.
- 🧪 Honesty bounds: these receipts verify hashes, decoded integer samples, numerical peaks, zero clip boundaries and phase contracts. They do not establish actual DSP scheduling, imported Unity sample fidelity, human listening quality or gameplay outcome wiring; parent-owned Unity checks cover those separate responsibilities. This is CoO-original audio integration, with no Qud parity claim.
