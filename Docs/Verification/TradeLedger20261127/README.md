# The trading screen uses the inventory palette

Trade uses smooth type, the inventory palette and shared item icons.
The merchant and pack have separate purses and aligned quantity and
price columns. Special stock is labeled in amber. The screen fits 18
rows per side and supports the mouse wheel and Page Up and Page Down
for longer lists. The displayed item name leaves the quantity to its
own column.

Shop confirmations show the exact item, whole-stack quantity, price
and the resulting purse and carry weight. If the displayed quantity
or price changes, the screen asks the player to review the current
quote. Composed offers show marked and available quantities, a per-item
quote and the net balance. F1 describes the selected quantity and its
line value, then returns to the same offer. A barter-only merchant uses
goods and salt and displays values in place of currency.

The inventory coverage test found that Peat Works' fixed field case
lacked an icon. It now uses the existing iron-case icon.

## Native play with Yevven Reedfall

The seed-65 traveler opened Trade through Yevven's ordinary dialogue
at Sill. The 19-dram waterskin was refused at a purse of three drams.
Canceling the offer to sell three lamp oils kept all three and all the
money. Reopening and confirming sold those oils for seven drams. A mouse
click on the special-stock mushroom opened the matching purchase quote.
The purchase moved the exact mushroom to the pack for seven drams.
Fresh Continue kept the mushroom and three drams. The lamp oils stayed
out of the pack.

A separate composed offer marked two of Yevven's three dried meats,
at 16 drams each. F1 showed a 32-dram marked line. Closing the reader
kept those same two marked units and the same price. Closing Trade
cleared the unsubmitted offer. All these actions stayed at turn 1720
and 40 HP.

The native playtest took place on Apple M5 with a 2317 × 1267 Game View.
A true barter-only merchant and shelves beyond the visible page were
outside this live visit.

- [Shop and special stock](01-trade-shop.png)
- [Whole-stack sale quote](02-sell-stack.png)
- [Counted offer](03-counted-offer.png)
- [Native state and test receipts](observations.json)

## Native regression

The original screen passed the faction control and failed the three
new retained-view checks. All 508 final focused Unity checks passed
with zero compiler errors, skips or inconclusive cases. That run
included trade, consignment, barter, inventory presentation, F1,
capacity refusal and modal ownership. The accepted whole-suite
accounting remains on the separately pinned regression snapshot.
[Full regression accounting](../MachineSystems/CurrentNativeRegression69AE7EE85/README.md)

## Mac startup

The universal Mac package for `4822322fe` built and started on Apple M5.
Six bundled binaries contain arm64 and x86_64. The startup check
preserved the previous package and all 876 normal Quick files. The
gameplay checks above ran in the Editor.
[Mac startup receipt](mac-startup-verification.json)
