# Economy as a first-class system — findings (research in progress)

**Status:** research notes, 2026-11-01. No design is accepted yet. The survey of the
implemented economy code and the design come after the chunk-density milestone
(`Docs/CHUNK-ACTIVITY.md` §9).

**User direction (2026-11-01):** fill out the economy, formalize it, and make it a first-class
system. Start from existing design documents if there are any; otherwise brainstorm a system
that fits the game.

## 1. Existing documents

| Document | What it is | Authority |
|---|---|---|
| `Lore/History/08_MaterialCulture.md` §currency | The currency-trust map: which faction trusts which currency, and the stated texture ("money change meaning as they cross the world") | **Canon** (`Lore/` via `10_Bible.md`) |
| `Lore/Factions/04_SaccharineConcord.md` | The trading power: the honored debt, trade-posts, caravans, underwriting drams | **Canon** |
| `Docs/Design/Mechanics/TRADE_AND_ECONOMY.md` | A study of how Caves of Qud implements trade from the decompiled source: water drams, the Commerce value pipeline, Ego-based performance, buy/sell asymmetry, restocking, haggling and debt | Reference only, not a design for this game |
| `Docs/SHOPPING-PARITY.md` | The Qud-parity sweep of the shipped shopping stack, which it calls about 95% complete | Implemented-system record |
| `Docs/Design/PLAYER-SHOP.md` | A design study of letting the player open a shop; its own header says it is not committed to a build plan | Exploration |
| `Docs/WEAPON-RENTAL-SYSTEM.md` | Weapon rental priced in Ink, a separate wallet from drams | Implemented-system record |
| `Docs/Lore/Palimpsest_Lore_Compendium.md` | The superseded Palimpsest world | **Not** a design source (superseded) |

No existing document designs the economy as one system. The pieces are separate: shopping,
rental, inns, shrines, loot and the player shop.

## 2. What canon fixes

From `08_MaterialCulture.md`, the currency set of `00_Canon.md` Layer 6:

| Currency | Trusted by | Not trusted by | Character |
|---|---|---|---|
| Drams | the Concord, surface villages, most of the world | catacomb villagers (barter first), the Choir (no economy), Tent-Right (oath first) | the closest thing to universal; the Concord underwrites it |
| Ink | the Quartermaster, scribes, preservers | — | destroyed on use; the rental currency; the same substance as embalming ink |
| Lumen-hours | catacomb economies; the Concord as broker | surface dwellers | hyper-local; the Concord runs the futures market |
| Salt | Tent-Right, Pale Curation, preservers, the wasteland | — | the wasteland's reliable trade good; cheap near the flats, precious far from them; preservative and seasoning |
| Honored debt | the Concord internally | — | the Concord's true currency of trust; a Counter-signed contract outweighs any sum |

Two rules follow:
- **Cross-faction exchange is friction, and friction is content.** Drams are dead weight in
  the deep wasteland; the Choir sells nothing; Tent-Right gives water to a guest and never sells
  it.
- **The intended texture is money changing meaning as the player crosses the world.**

## 3. Tension to resolve in the design

The implemented trade uses water drams as coin, inherited from the Qud port. Canon says water
is the truest wealth in the Beating, and among Tent-Right it is given, never sold. A
first-class economy has to decide what a dram physically is in this world, and how a
dram-priced shop behaves in places that do not trust drams.

## 4. Next steps

1. Survey the implemented economy end to end:
   - the currency stores and wallets;
   - `TraderPart`, pricing and restocking;
   - weapon rental and Ink;
   - inn and rest costs, shrine donations and loot value;
   - the player shop, and any salt or barter paths.
2. Map each against the canon table above: matched, missing or contradicting.
3. Design: plan to disk, verification sweep, sub-milestones, then implementation under the
   major-feature workflow.
