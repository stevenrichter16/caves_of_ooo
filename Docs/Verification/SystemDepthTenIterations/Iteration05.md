# Iteration 05 — the wellkeeper learns from the donated book

Implemented: the WellKeeper's existing purification-copy choice now offers only
when the player carries a usable copy that actually teaches KnowsPurifyWater and
the well needs purification. One real copy establishes saved local knowledge,
purifies the water, and allows the caretaker to renew temporary purification on
later visits. The filtration ring remains unrepaired; the manual/silver-sand
route and its improved-care milestone remain distinct. No additional copy is
taken once the keeper knows the rite or the well is already repaired.

## Sweep and design

The old choice consumed any GrimoireCopy and only displayed a promise. The
existing TeachCaretaker method applies to structurally repaired wells, so simply
calling it would reject the broken well this dialogue describes. The new service
uses the existing settlement condition save stream (no binary format change) and
existing temporary-purification stage. Long absences reconcile one renewal rather
than looping through every missed period. The conversation reports this exact
result and uses a required action, so refusal cannot advance to success text.

## Evidence and review

- Executed RED: 13 cases, 5 missing-behavior failures, 8 passing counters.
- First GREEN attempt: 12/13 exposed a second bug: equal-name grimoire copies
  with different knowledge merged, destroying one payload. StackPayloadIdentity
  now compares the four GrimoirePart fields; identical copies still stack/pay one.
- Subsequent review added save round-trip, repeat payment, wrong/original book,
  other settlements, manual-repair continuation, four payload differences and
  unavailable-ownership probes. Two real ownership failures were observed and
  fixed by validating actual carried/equipped/world ownership before donation.
- Final combined root regression: 100/100 (25 caretaker cases, 24 natural-liquid
  cases and 51 existing regression cases). Six core RED and five keeper RED
  failures are recorded separately, not counted as tests newly written twice.
- 🟡 Corrected payload loss and ownership admission before commit.
- 🔵 Conditions survive the actual settlement serializer and maintain separate
  sites; the unrepaired well still has a temporary outcome, not a repair reward.
- 🧪 Native Unity and Play sanity remain pending final synchronized verification.
- ⚪ No promise of an NPC performing an animated rite while absent; renewal uses
  the existing entry-time settlement reconciliation. No new Qud parity claim.
