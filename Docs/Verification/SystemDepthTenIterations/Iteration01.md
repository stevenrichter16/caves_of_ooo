# Iteration 1: useful shop keys

Status: implemented; standalone and native EditMode verification complete; live review pending. CoO content extension, no Qud parity claim.

The TinkerStock and MerchantStock entries rolled IronKey, but IronKey's NoTrade
tag caused the canonical trade transaction to reject it. They now roll a
SpareIronKey with the same ordinary iron lock identifier, weight and value,
reusing the existing iron key visual.
Bound and quest keys retain their existing definitions and protections.

## Verification sweep and corrections

| Premise | Verified code | Correction |
|---|---|---|
| Derive a tradable item from IronKey | BlueprintLoader merges inherited tags without a removal operation | Derive from PhysicalObject, explicitly provide the small key definition. |
| Every iron key is ordinary stock | SpreadWayhouse binds protected IronKey instances to site-specific IDs | Preserve IronKey and its descendants, replace only two stock entries. |
| A new key automatically has existing art | Portable and sprite recipes use explicit blueprint names | Add narrow aliases to the existing key art; retain normal visual guards. |

## Acceptance and counterchecks

Real authored trader creation must roll a purchasable spare; actual buy/sell
must transfer exactly the quoted drams and same object once. It must open an
ordinary iron lock while failing Stillleaf and site-bound locks. Insufficient
funds, full inventory, stale ownership, cancellation and protected key variants
must preserve the relevant balances and ownership. Save roundtrip is covered by
the binary graph test in both standalone and native runs. Dedicated adversarial
cases exercise the existing transaction boundary without changing its implementation.

## Evidence and review

`iteration01-red.xml`: 32 cases, 28 failed, 4 protected-key cases passed.
Six cases were inherited duplicates; the shared fixture was separated afterward.
`iteration01-green.xml`: 28 unique cases, 27 passed, 0 failed, 1 explicitly skipped
native visual test. The new save graph test proves spare/protected identities,
NoTrade flags and inventory backreferences survive the binary serializer.
The 20 dedicated adversarial cases cover protection, refused trades and exact
lock identity. This is content availability work: most initial failures were
missing-content preconditions, not 28 distinct gameplay bugs.

`iteration01-content-diff.json` records parsed before/after proof: only
SpareIronKey added; no existing blueprint changed; only MerchantStock and
TinkerStock modified. Direct diff confirms each table changes one entry name.
The main behavior tests were written and executed RED before production. The
visual alias and serialization regression checks were added after implementation;
no initial visual RED is claimed; the later Stump regression has its own native
RED below. The isolated runner is `/tmp/coo-system-depth-runner`,
using the tracked runner sources with COO_REPO set to this checkout.

Final native job `941ad3a3628542c19f5fda2b0a9e49b4`, recorded in
`native-final-integration.xml`, passed 810/810 with no skips. The raw XML includes
all 29 spare-key cases: 9 behavior/visual cases and 20 adversarial cases, including
both native visual resolvers and the binary save graph roundtrip.

🔵 Self-review: no change to trade transaction or lock authority; only explicit
stock and visual identities change. Buy success checks exact source ownership,
wallet delta and one diagnostic; identical stale retry is refused without charge.
⚪ Old saved shop inventories keep their protected old keys; later normal stock
refill can provide spares. No destructive save migration is introduced.
🧪 Native visual resolver checks establish selected art identities, not rendered
appearance. Controller purchase flow and live feel remain parent-owned follow-up
gates. No physical Deck check is claimed.

## Files

Changed: Objects.json, LootTables.json; SpreadPortableRecipes.cs and
EnvironmentSpriteRenderer.cs; MerchantSpareKeyTests.cs and
MerchantSpareKeyAdversarialTests.cs plus fresh metadata; this log and receipts.

## Cold-eye follow-up: carried key in Stump

The initial aliases covered Spread portable models and the 2D key body. Native
Stump uses its own exact family mapping, so a purchased spare dropped there
could fall back instead of reusing the existing Stump key. Added
`NativeStumpVisualUsesTheExistingKeyFamily` before the production alias: old-key
precondition, new spare, unrelated-name refusal and all four existing variants.
Parent-owned native RED job `2228a9af8fad4b228f1ebdba79828640` executed 1 case:
1 intended failure, expected `key`, actual null. The compact receipt
`iteration01-stump-native-red.json` transcribes the root's observed native result;
it is not presented as the full raw MCP response. Implemented one exact
`SpareIronKey` case sharing `IronKey`'s existing family. No model, material,
variant or visual guard changed. This native regression passed in both the first
integration run and the final 810-case run cited above.

Independent HUD-agent review of the spare-key, restock and compost changes found
no actionable regressions: protected original keys and exact stock identities
remain intact; nested-table membership matches the roller; existing authored
refill quantities and matching compost fallback are preserved. This Stump
coverage gap was found by the implementation agent's cross-region review.
