# Repair and cultivation verification

Authoritative results are native Unity6000.3.4f1, not a headless approximation.

- `Tests/final-green-result.json`:915/915 passing,48 affected fixtures (188new feature cases).
- `Tests/final-regression-result.json`: preceding912/915 with the legacy maturity render-hook regression (2cases) and changed seed stock count (1pin), both corrected.
- `Tests/initial-native-result.json`: complete content RED before the17blueprints/site were added; repair/crop runtime already green.
- `Tests/ui-village-red-result.json`: actual-menu8RED; its4village setup errors are not feature RED.
- `Tests/art-ui-first-result.json`: menu8GREEN, art25/26,4meaningful village RED; fixed native terrain-variant and missing village adapter.
- `Tests/art-site-use-result.json`:62/62 across final30art,8site/save,6use,18stock.
- `Tests/*-review.md`: bounded subsystem handoffs; their “root pending” wording is historical. The final integration results are above and in the living doc.
- `Tests/repair-*.xml`, `Tests/crops-*.xml`: reference RED/GREEN/counter-check receipts. Scratch runner shims cannot establish real input, native editor logs or pixels.
- `Tests/content-preservation.json`: all old parsed blueprints unchanged;17added. `script-meta-check.json`:21new C#metadata identities validated.

Native route receipts are kept without deleting failed attempts:

| Run | Result | Interpretation |
|---|---|---|
| `8dbf58bdfa5e4cbda018a6f97c825cd0` |21/21;14screenshots|First actual gather/repair/grow/harvest/save/load success.|
| `0f51c948340e4fccbf379d5617fd69fd` |Stopped after watering|A presentation-only step aside exposed the waiting player to a roaming enemy. The safety precondition logged one editor error. No successful completion claimed.|
| `aca44f9a21ec45acac45e40c358e2a51` |21/21;14screenshots;0errors|Final ordinary route after fixes; strict no-healing condition; original route restored.|

## Can verify

The real generated field and exact current owners, keyboard material collection, repairs, restored well/gate operation, standing harvest, physical pickup, returned-seed planting, real rain, paid player-turn growth and native save graph replacement. Native screenshots show the models and UI; the root inspected representative final frames. Tests additionally cover all three crop utilities, malformed ownership, transaction rollback, metadata, old crop behavior and village presentation.

## Cannot verify

The route uses one explicitly recorded initial player travel shortcut from an isolated glade launch into the genuine west-of-Morrowfast field. It proves no unaided discovery, full-world/all-seed placement frequency, long-term encounter/economy balance, offscreen growth or subjective art quality. Some crop frames are partly occluded by the player. No inventory, HP, material, moisture, clock or stage grants, enemy deletion, suppressed scheduling or forced harvest outcomes are used. User saves, scene setup and preferences are isolated/restored. Existing cached areas are not migrated.
