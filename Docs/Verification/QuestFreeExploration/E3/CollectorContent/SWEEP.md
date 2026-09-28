# Collector blueprint sweep and TDD

Private candidate only. Content tests first ran4:3 missing-blueprint failures/1 original Magpie+Dog control; added actual TradeStockBuilder counter ran5:4RED/1control. Only then appended Tatterjay and TatterjayCorpse to a private copy, preserving every preexisting parsed blueprint. Role native implementation/integration remains pending.

Tatterjay inherits Creature, not Magpie; explicit Avian body has two Wings and no humanoid Hand. No NaturalWeapon or BodyNaturalAttack tag is added. It is a passive Villagers-faction carrier with20 hard weight capacity and XP0. NoRandomStock uses the existing TradeStockBuilder exclusion to prevent arbitrary trade goods being minted into the bird; it deliberately subtracts the replaced Magpie's later2–5 stock roll, not a neutrality claim. Other actual Magpie stocking/RNG is paired by a real builder test. Corpse mapping/chance70 is explicit; remains have no added Harvestable/LootDrop. Ordinary global death loot is unchanged, so this does not claim no death loot. No unimplemented flight is implied by Avian anatomy.

Actual scoped collector code is required for the empty-role/CurrentCarriedItem test before this candidate can goGREEN. Source placement, trade-stock effects, art/carry and save/action acceptance belong to the full family integration; these definitions alone do not finishF6.
