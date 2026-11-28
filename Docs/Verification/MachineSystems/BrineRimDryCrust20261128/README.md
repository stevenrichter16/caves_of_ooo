# Dry brine crust on the Beating rim

November 28, 2026, America/Chicago. Unity 6000.3.4f1 on Apple silicon.

At one newly generated natural Brine Lens (17,13), the salt rim gives
the scraper a dry-weather use. The wet streak, white flake and spent
scrape mark are drawn over the existing terrain. Examine describes the
current state. The reserve holds one canonical brine crust and does not
feed or draw from the neighboring brine pool. Existing surface caches
carry forward their original layout.

## Ordinary play

The private branch used the earned Ready2220 save from Sill. It kept
the traveler's original buckler, empty flask and full brine flask.
One canonical scraper was supplied at the start. Tool purchase and
discovery were outside this trial. All following movement, waiting,
inspection, work and travel used the normal commands.

- At tick 2390 the rain hid the flake. Examine showed the dark streak
  and the rim's interaction menu offered no scrape.
- The traveler made two ordinary steps around the sky-sari shadow.
  The dive missed. In clear weather at tick 2410, the same rim offered
  one scrape from the dry eastern approach at (41,14).
- One paid action brought one crust into the pack at tick 2420. The
  same pool still held 90 measures. The traveler saved and took the
  ordinary atlas route back to Sill at full health.
- In Sill, the Recipes tab showed shock-proofing and its resistance
  bonus. Pressing used the crust and gave the earned flask one coat.
  Applying it to the same worn buckler gave the traveler +35 shock
  resistance. A fresh Continue retained the spent rim, full pool,
  empty flask, exact buckler and the remaining coat duration.

The wet and dry open-world images come from a matched visual replay
without the supplied scraper. The Examine and action text are recorded
in the exact observations.

One attempt ignored the telegraphed sky-sari strike and died immediately
after the scrape committed. A second took the dry approach and pressed
while still in the bird's reach. Its subsequent attacks interrupted the
replay. The completed branch left the active threat before processing.

## Verification

Three control cases passed and eight installed-behavior cases failed
in the native run with the installation hook omitted. The final
focused Edit Mode run passed 438 cases. One explicit save-contract
regeneration helper was skipped. All 11 focused rim cases also passed
after a test-only naming cleanup. The suite includes wet footing,
stale rows, late invalidation, rollback into an existing stack,
carry capacity, native terrain cue, fog memory, finite stock and
serialized custody. Independent design, source and gameplay reviews
checked the exact site, the reserve and the displayed effects.

The 880 ordinary Quick files and 87 unrelated tracked paths matched
their pre-play hashes. Retained Mac builds stay available. This slice
was played in the Editor. It is newer than the last packaged Mac
milestone. Wider Brine Lens coverage, tool acquisition in this route,
the Deck and performance comparisons remain unverified.

- [Wet marker on the rim](01-wet-map.png)
- [Dry flake and missed dive](02-dry-flake.png)
- [Spent rim and pickup](03-spent-rim.png)
- [Shock-proofing preview in Recipes](04-shock-proofing-recipe.png)
- [Coating the worn buckler](05-coat-buckler.png)
- [Exact observations and save hashes](observations.json)
