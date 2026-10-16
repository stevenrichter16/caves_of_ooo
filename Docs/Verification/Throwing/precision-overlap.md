# Initial/precision seed overlap verification

Before the Measured balance revision, the first 20 seeds (101–120) of every selected physical 100-seed arm were compared to the retained original 20-seed archive. All existing outcome, HP, action, enemy, supply, custody, purchase, command and position data match. No observed outcome mismatch is attributed to a skill.

One receipt schema addition predates the precision run: W1/W5 afterActions now includes player HP after the player coordinate. Only that additive `/HP` token is removed when comparing those intermediate action strings against the old format. The older receipt cannot independently prove intermediate HP that it never recorded; final HP and all originally recorded fields match exactly. W4/W4-reversal/W6 match byte-equivalent parsed trial fields without that normalization.

| World | Repeated trials | Outcome/old-field mismatches | Rows with additive snapshot format only |
|---|---:|---:|---:|
| W1 | 320 | 0 | 320 |
| W4 | 160 | 0 | 0 |
| W4-reversal | 80 | 0 | 0 |
| W5 | 200 | 0 | 200 |
| W6 | 60 | 0 | 0 |

This records one successful replay of those originally recorded physical trial fields; it does not prove every random path is fully controlled. It does not erase the separate uncontrolled StunnedEffect stream discovered in a W7 alternative policy, nor promise that unexamined arms are reproducible. The payload reviewer owns that correction and any required rerun.

A later W7 review found fallback StunnedEffect randomness. Its first test-only fix also overwrote legitimate supplied combat RNG and therefore invalidated the first +4 revision before/after comparison. The preserving-stream correction and corrected full rerun are tracked separately; the table above remains the original pre-tuning overlap observation, not a blanket certification of the first correction.
