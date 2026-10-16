# A3-D12 selected precision check

**Evidence versions:** W1 remains the historical +2 Measured baseline; the current +4 decision and intervals are separate in `measured-revision-1.md` / `measured-revision-summary.md`. W4/W5 mechanics are unchanged. W6 initial columns preserve the original gate-selection history, while its final 100-seed columns use current +4 gameplay and corrected fallback-stun seeding. They must not be read as a causal before/after tuning comparison.

Audit 3 declared a single 20→100 seed expansion when a primary success-difference interval is wider than 20 percentage points. The initial matrix used 20 seeds and did not calculate this gate; it was neither satisfied nor superseded. This supplemental selection and interval recipe were recorded before running the extension, after observing the initial matrix. They are an explicit bounded refinement, not an originally preregistered statistical analysis.

We use 95% Wilson score intervals for each binary arm and the Newcombe square-root combination for their risk difference. The formula treats arms independently: matching a world seed does not guarantee identical subsequent random draws after different actions. These fixed seed ensembles are exploratory game tests, not random samples from a defined population; the interval is a conservative precision heuristic, not a formal confidence claim about all gameplay. There is no multiplicity correction or significance threshold. A favorable/unfavorable interval does not itself authorize further seed expansion or balance tuning; any separately approved bounded balance revision must preserve and distinguish these baseline receipts.

Only the exact pairs below receive this inference/precision check. All other initial matrix groups remain descriptive 20-seed observations. The selected extension reuses the existing real command policies, packages, geometry, levels and finite supplies with seeds 101–200. It includes the original 20 seeds, so analysis replaces those selected groups and never pools them as 120 observations. Production RNG is not modified by this analysis. The W6 final receipt version includes the separately approved +4 balance revision; its original gate-selection counts remain historical. A width above 20 points at 100 remains explicitly unresolved; the predeclared one expansion does not promise that every interval will become narrow.

W6 initial selection counts remain frozen in the pre-stun-correction archive. The corrected canonical first 20 records now match the current 100-seed receipt exactly; the original gate decision is not silently recalculated after the harness correction and +4 revision. `explicit-stream-comparisons-green.xml` records the corrected 28-case execution.

The physical extension completed in `precision100-physical-green.xml` (5/5 cases; 4,100 actual trials). Across the displayed baseline/current evidence versions, only 8 of the 25 selected intervals reach width at most 20 points; the other 17 remain explicitly imprecise. No extra seeds are added to force narrower results. `precision-overlap.md` verifies the repeated original seed outcomes before tuning.

W1 primary success means objective plus the weapon retained; survival and actions are secondary, and a faster death is not a benefit. W4 primary success means the enemy was defeated before withdrawal; these fixtures do not show a player-survival benefit. W5 means portable custody plus extraction; its one-action saving and dog displacement are separate observed mechanics. W6 means the finite expedition objective plus the weapon; inactive purchased buttons receive no causal credit. Focused/W7 outcome selection is recorded separately by the payload reviewer before its extension.

| World / variant | A − B | Initial successes A/B | Initial Δ | Initial 95% interval | Width | Expand? | 100-seed successes A/B | 100-seed interval / width |
|---|---|---:|---:|---|---:|---|---:|---|
| W1 Viper/AV0/daggers2 | root − bank | 18/20 vs 18/20 | +0.0 pp | [-21.4, 21.4] | 42.7 pp | Yes | 85/100 vs 84/100 | [-9.2, 11.2] / 20.3 pp |
| W1 Viper/AV0/daggers2 | measured − root | 19/20 vs 18/20 | +5.0 pp | [-15.0, 25.5] | 40.5 pp | Yes | 94/100 vs 85/100 | [0.4, 17.9] / 17.5 pp |
| W1 Viper/AV0/daggers2 | measured − puncture | 19/20 vs 17/20 | +10.0 pp | [-11.0, 31.4] | 42.5 pp | Yes | 94/100 vs 90/100 | [-3.9, 12.1] / 16.0 pp |
| W1 MarlbackScrabbler/AV0/daggers2 | root − bank | 10/20 vs 11/20 | -5.0 pp | [-32.8, 23.9] | 56.7 pp | Yes | 55/100 vs 58/100 | [-16.4, 10.6] / 27.0 pp |
| W1 MarlbackScrabbler/AV0/daggers2 | measured − root | 9/20 vs 10/20 | -5.0 pp | [-32.8, 23.9] | 56.7 pp | Yes | 61/100 vs 55/100 | [-7.6, 19.3] / 26.8 pp |
| W1 MarlbackScrabbler/AV0/daggers2 | measured − puncture | 9/20 vs 15/20 | -30.0 pp | [-53.6, 0.2] | 53.8 pp | Yes | 61/100 vs 92/100 | [-41.5, -19.6] / 21.9 pp |
| W1 Viper/AV4/daggers1 | root − bank | 12/20 vs 11/20 | +5.0 pp | [-23.7, 32.6] | 56.3 pp | Yes | 44/100 vs 41/100 | [-10.5, 16.4] / 26.9 pp |
| W1 Viper/AV4/daggers1 | measured − root | 10/20 vs 12/20 | -10.0 pp | [-37.0, 19.3] | 56.3 pp | Yes | 40/100 vs 44/100 | [-17.3, 9.5] / 26.9 pp |
| W1 Viper/AV4/daggers1 | measured − puncture | 10/20 vs 16/20 | -30.0 pp | [-53.4, -0.5] | 52.8 pp | Yes | 40/100 vs 74/100 | [-45.8, -20.4] / 25.4 pp |
| W1 MarlbackScrabbler/AV4/daggers1 | root − bank | 6/20 vs 7/20 | -5.0 pp | [-31.7, 22.6] | 54.3 pp | Yes | 39/100 vs 39/100 | [-13.3, 13.3] / 26.6 pp |
| W1 MarlbackScrabbler/AV4/daggers1 | measured − root | 6/20 vs 6/20 | +0.0 pp | [-26.8, 26.8] | 53.6 pp | Yes | 37/100 vs 39/100 | [-15.2, 11.3] / 26.5 pp |
| W1 MarlbackScrabbler/AV4/daggers1 | measured − puncture | 6/20 vs 14/20 | -40.0 pp | [-61.9, -9.0] | 52.8 pp | Yes | 37/100 vs 79/100 | [-53.2, -28.7] / 24.4 pp |
| W4 gas | driving − root | 20/20 vs 4/20 | +80.0 pp | [53.1, 91.9] | 38.9 pp | Yes | 99/100 vs 23/100 | [65.8, 83.2] / 17.4 pp |
| W4 dry | driving − root | 4/20 vs 4/20 | +0.0 pp | [-24.7, 24.7] | 49.4 pp | Yes | 23/100 vs 23/100 | [-11.6, 11.6] / 23.2 pp |
| W4 wall | driving − root | 4/20 vs 4/20 | +0.0 pp | [-24.7, 24.7] | 49.4 pp | Yes | 23/100 vs 23/100 | [-11.6, 11.6] / 23.2 pp |
| W4 brace | driving − root | 4/20 vs 4/20 | +0.0 pp | [-24.7, 24.7] | 49.4 pp | Yes | 19/100 vs 19/100 | [-10.9, 10.9] / 21.8 pp |
| W4-reversal gas-at-current-position | driving − root | 4/20 vs 15/20 | -55.0 pp | [-73.3, -24.3] | 49.0 pp | Yes | 23/100 vs 75/100 | [-62.3, -38.9] / 23.4 pp |
| W4-reversal dry-control | driving − root | 4/20 vs 4/20 | +0.0 pp | [-24.7, 24.7] | 49.4 pp | Yes | 23/100 vs 23/100 | [-11.6, 11.6] / 23.2 pp |
| W5 quiet | recovery − bank | 20/20 vs 20/20 | +0.0 pp | [-16.1, 16.1] | 32.2 pp | Yes | 100/100 vs 100/100 | [-3.7, 3.7] / 7.4 pp |
| W5 pursuit | recovery − bank | 20/20 vs 20/20 | +0.0 pp | [-16.1, 16.1] | 32.2 pp | Yes | 100/100 vs 100/100 | [-3.7, 3.7] / 7.4 pp |
| W5 acid | recovery − bank | 20/20 vs 20/20 | +0.0 pp | [-16.1, 16.1] | 32.2 pp | Yes | 100/100 vs 100/100 | [-3.7, 3.7] / 7.4 pp |
| W5 free-dog | recovery − bank | 20/20 vs 20/20 | +0.0 pp | [-16.1, 16.1] | 32.2 pp | Yes | 100/100 vs 100/100 | [-3.7, 3.7] / 7.4 pp |
| W5 busy-dog | recovery − bank | 20/20 vs 20/20 | +0.0 pp | [-16.1, 16.1] | 32.2 pp | Yes | 100/100 vs 100/100 | [-3.7, 3.7] / 7.4 pp |
| W6 outbound-return | all5-water − bank | 13/20 vs 15/20 | -10.0 pp | [-35.7, 17.6] | 53.4 pp | Yes | 65/100 vs 69/100 | [-16.7, 8.9] / 25.7 pp |
| W6 outbound-return | all5-water − water-acrobatics | 13/20 vs 18/20 | -25.0 pp | [-47.9, 1.3] | 49.1 pp | Yes | 65/100 vs 74/100 | [-21.4, 3.7] / 25.1 pp |

## Retention and reversal limits

- **Sure Grip:** real eligible retry and pre-second-point natural use are observed; W1 compares it against ordinary throws at matched cost, including armor/scarcity conditions. This does not establish universal superiority to keeping the weapon or saving the point. A separate high-Agility cheap-dip matrix was not run.
- **Measured Throw:** the +2 baseline here failed to establish its armored/scarce objective role. Revision 1 is retained at +4 for the separately recorded AV4 scarce-projectile endpoint survival/remaining-health benefit, with a low-armor Puncture reversal. Its objective differences against root-only remain wide and imprecise; partial living endpoints are not successful extraction. See `measured-revision-1.md` for the explicit benefit/reversal/no-dominance decision.
- **Driving Throw:** destination gas provides an actual benefit; pushing an enemy out of existing gas is an actual harmful reversal. Dry/wall/brace preserve no-benefit paid cooldown controls. Player survival was identical in these fixtures.
- **Recovery:** actual carried/equipped origins and exact-unit custody have correctness controls. Ordinary pickup can finish the same objective; Recovery saves a paid action in quiet/pursuit/acid/busy-dog cases, but cannot retrieve a free dog's current item and does not remove entry damage. Binary success alone understates this tactical timing comparison.
- **Focused Delivery:** payload reviewer separately reconciles adjacent-ally sparing against loss of useful broad exposure, and W7 ground-control outcomes. Natural use of this earned child remains unverified.
- **Whole package:** W6 intentionally compares shared finite policies, not optimized use of every active skill. Its result cannot establish the value of inactive Driving/Focused purchases. Naturally earned 4/6 SP and renewable supply pacing remain separate unverified gates.
