# Earned resin, still and temper journey

2026-10-26. Actual-play plan saved before continuation. Use the new
3aca7c18e Mac package after its ordinary hotbar/restart check. The unchanged
earned Nella fixture began at tick9470; one real En Garde activation advances
it to9480. HP42/42,40drams and two earned Pitchpod resin remain. Preserve this
checkpoint and all failures. Root owns the player; no runtime injection.

The established playable greybox/concepts/native kits remain valid. This is a
real use/state acceptance pass with existing sprites, not a new art packet.

## Intended ordinary route and contracts

- Walk Nella's southern exit into North; approach the real still67,5. The
  retained snapshot places its armed Patchbearer at64,3, not the stale62,6
  coordinate in older records. Positions may change as actual turns advance.
  Observe in short segments; geometry is not a combat safety guarantee.
- Select only one Pitchpod resin at the still; ordinary single brew should
  consume one of two and yield one Burning coating at potency1. Leave BogSap
  unselected. Record actual selection/output, costs and reader art.
- Reach the real East forge45,5 via the glade. Unequip the existing sword so
  the ordinary carried-item picker can select it. Select that exact sword and
  the brew, then Craft through the existing quench path; no component kit is
  required. Expected existing rules: same singleton sword, flame-quenched,
  one temper, Burning30% potency1, maximum durability10→8, damage1d8/Pen2.
- Re-equip, save, full quit/Continue and inspect the actual retained item.
  One resin should remain and the coating should be consumed. A natural
  Burning hit is a separate optional gate, not guaranteed by30% chance.

## Bounds and next decision

No crop/alchemy/forge/combat mechanics are being added. These rules predate this
graphics run. Native source coverage does not prove this ordinary route. Do not
claim a triggered Riposte or Burning application without observing it. Inspect
actual world/menu/reader consistency; if a named defect appears, save its
evidence and use RED/GREEN plus real replay before changing behavior/art.

Package smoke and this route share one owned standalone session after its real
restart; final preservation/restoration happens after the route. Current and
previous packages, normal saves/preferences, approved art and foreign edits
remain protected by the new hotbar-delivery baseline. Storage is inspection-only.

Current status: bounded earned route and restart accepted, 2026-10-26.
Full opening/regions, Linux runtime, physical Steam Deck and sustained
performance remain open. The suspected coating portrait gap was resolved by
source/visual audit; next is fresh whole-scene acceptance.

## Actual route and saved-state evidence

Root used only the ordinary packaged keyboard/mouse flow. Nella wildlife
blocked the initial path; a short detour reached43,24 and the North border.
The armed Patchbearer approached at North56,5. En Garde, real sword exchanges
and Lunge led to its death at58,5; its healing tonic, damage, bleeding and
corpse/drop feedback were observed. Player HP42→25, XP2→77. No actual parry or
Riposte was observed, so those gates remain open.

At North68,5, C/Left opened the still. Select only Pitchpod resin, then b for
one brew: the log announced the heat/fuel discovery and Burning coating.
The original inventory/action/reader captures show its correct potency1 and
forge-use description. The player walked North→glade→East, reached forge45,5
from45,6, unequipped the same long sword, selected coating and sword and chose
Craft. The coating was consumed; the sword was re-equipped using the normal
left-hand picker. F5, full process quit/relaunch, Continue, inventory/F1 and F5
preserved the result without another game turn.

The independent saved-state audit passed26/26 assertions:
- sword2710 is the same singleton, still equipped by player2706;
- one temper, Burning30% potency1, HP/max10/10→8/8, damage1d8/Pen2/Hit0 unchanged;
- resin13757 count2→1, still carried, no BrewItem remains in either final graph;
- guard6930 is gone; corpse13765 records its identity and player killer;
- restart retains all scoped fields at East45,6, tick11960, HP25/42,40drams.

Evidence: [32 original Mac captures](Verification/Live16x24/earned-resin-captures.json),
[actual-play summary](Verification/Live16x24/earned-resin-play-summary.json),
[26-check audit](Verification/Live16x24/earned-resin-state-audit.json),
[reproducible read-only script](Verification/Live16x24/earned-resin-state-audit.py),
[independent visual review](Verification/Live16x24/earned-resin-visual-review.json).
Unmodified before/after restart checkpoints and raw compressed player logs are
preserved beside these records. The starting hotbar-parity checkpoint is also
retained. No gameplay rule, source asset or production code was changed here.

## Visual review and next pass

The full flame-quenched name fits the ledger, the far-right [E] and1d8 are clear,
and the native sword matches the before/after-restart reader. The reader shows
separate25% Bleeding and30% Burning attempts. Existing neutral sword art remains
appropriate: tempering changes mechanics without inventing permanent fire FX.

The initial suspicion about the muted bottle was resolved by independent audit:
it is the approved native `north-works-products` frame5, not a glyph fallback
or routing failure. `BrewingService` creates `BrewedTonic` with an owned
`BrewItemPart(Form=Coating, EffectsRaw=Burning:1)`;
`FlatPixelOpeningArtCatalog.cs:307–314` intentionally shares this flask across
canonical brewed forms. The source manifest and earlier ordinary Burning
reader acceptance (`LIVE-16X24-WORKS-PRODUCTS.md:261–265`) match capture17.
Preserve the accepted art and ownership guards. A future effect-specific bottle
would be a separate design choice requiring native art/counter-checks, not a
bug fix justified by this observation. Continue the fresh whole-scene release
review and highest-impact remaining composition/motion gaps.
Can verify: actual UI actions/captures and independently decoded scoped state.
Cannot verify: every potion/temper permutation, natural Burning application,
Riposte proc, continuous motion quality, sustained performance or physical
Deck behavior. Screenshot08/09 remain attempted AIM input evidence;10 shows the
resolved encounter. Capture27 is reader page2; durability/temper-count claims
come from the saved-state audit, not that image.

Self-review: this closes a previously open ordinary route; it does not add
new mechanics or certify complete art. No new RED/GREEN run is claimed.
The suspected generated-coating mapping defect was disproven. Same-session
preservation is recorded in the [package delivery](GRAPHICS-G3-HOTBAR-DELIVERY.md).
No source/cache/save deletion occurred.


Audit reproduction: run the saved script with `--baseline`, `--before-restart`
and `--after-restart` pointing to the three preserved fixture directories and
`--output` pointing to a new JSON path. It reproduces26/26 and confirms all six
input files unchanged; output-inside-input and overwrite attempts are refused.
It is a fixture-specific format8 reader, not a universal save parser or a
replacement for native tests. The [final coating identity audit](Verification/Live16x24/earned-resin-coating-identity-review.json)
supersedes the provisional generic-bottle observation.
