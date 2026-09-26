# C8 independent navigation review

Status: read-only source/test review complete; no blocking defect found. Reviewer
read TerrainNavigationWeight, FindPath, both 56-case navigation fixtures and the
actual LiquidPoolPart, LiquidCoveredEffect, LiquidSlipSystem and Damage semantics.

Q1: real pools and slippery tile state are separate exposure paths; max aggregation
avoids double-charging projected pool coatings. Prospective full-body cells use
the existing allocation-free occupied-cell view. Q2: actor absence leaves legacy
routing unchanged; finite cap90 preserves a forced hazardous route. Gas combines
by maximum rather than sum. Q3: root's three earlier adversarial corrections cover
case-insensitive incoming immunity and absent-stat modifiers; the neighboring
controls cover harmless water, empty pools, partial/full resistance and body
boundaries. Q4: this is a conservative route preference, not an exact forecast
of damage or arbitrary callback immunity.

The only source-documentation drift found was FindPath's gas-only wording; it is
now corrected to terrain/gas. No formula or new gameplay test was added by this
reviewer. Existing root receipts prove 56 focused +52 nearby checks; this review
does not add a native movement, visual or balance claim.
