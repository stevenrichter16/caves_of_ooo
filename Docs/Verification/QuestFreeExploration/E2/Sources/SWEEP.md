# E2 source/composer verification sweep (before production)

- SpreadTier1 is code in Data/Tables/PopulationTable.cs, not JSON. Its hostile group contains 1–2 Viper OR MarlbackScrabbler; independent Magpie, PetDog, harvest/scenery and dropped ordinary items are separate rolls.
- PopulationBuilder.SourceReceipt currently owns only the hostile group. It cannot authorize a loose Hatchet, Cudgel or LeatherBoots, or an ambient animal. Add separate exact, revision-bound receipts; do not infer source ownership from blueprint presence in a zone.
- F2 source decision: when the opted-in current manifest assigns OccupiedBank, replace the ordinary hostile list with one routine MarlbackScrabbler at generation, preserving the original table roll. This deliberately reduces the group/gear budget; no actor is deleted later to manufacture ownership.
- F3 source decision: replace at most the first actually rolled Magpie ambient slot with ReedbackGrazer. Zero ambient roll remains zero; no bonus spawn. Preserve other fauna and original group. Absence of two valid reachable ripe rows refuses feeding; no replenishment.
- New source policy executes only under the captured generation entry and exact current manager/factory; legacy tables and protected graphs keep their source behavior.
- A permanent river coating is not an enumerated fill source. F4 adds one finite LiquidPoolPart owner with ordinary Waterskin capacity; the separate dry draw-point base remains visible when empty. LiquidVesselService removes only PouredLiquidPool at zero, so a named source can retain its base.
- Existing late v1 composer cannot run in the same opted-in graph as v2: assignment/rewards/receipt consumption must not double-compose.
- Source census/automation are separate from human decisions. These source rewrites deliberately change individual generation RNG consumption after the preserved roll; only legacy output is pinned unchanged.
