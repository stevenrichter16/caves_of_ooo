# Finite hearth cooling — proposed correction, November 22

Status: bounded correction implemented; native175/175, three killed mutants,
actual cooling/recovery/readers/movement/F5/fresh2760. Final independent source/artifact/gameplay-evidence review0P1/P2.
Publication follows with this contract and dependency/continuation updates. See Verification/MachineSystems/FiniteCooling20261122.
The ordinary regional mire receipt reproduces a distinct water defect: pouring,
recovering and repouring one unit repeatedly cools an owned hearth without
consuming that water. The closed poured-oil fuel defect stays closed.

A water-like dose N on one installed, player-held, finite lit hearth spends
U=min(N,ceil((temperature−ambient)/40)) newly poured units. Positive finite heat
only; bounded double arithmetic precedes integer conversion. The last partial
unit is spent. U evaporates into an abstract nonrecoverable sink; no steam
entity, condensation loop or physical simulation is introduced. N−U remains
recoverable ground liquid. Older pooled stock and natural sources are untouched.
For 450/25 degrees and 12 units: 11 evaporate, 1 remains. One unit banks the fire
by 40 degrees. Fuel remains. Existing shared ApplyHeat, cooking and light rules
supply consequences; water never intentionally cools below ambient.

Menu choices bind the shown hearth identity and thermal/fuel quote; a changed
quote, newly appeared fire, missing fire or ambiguous owned target refuses.
Nonwater/fuel liquids and cells with no eligible fire keep ordinary pouring.
Invalid thermal numbers refuse cooling. Preview states evaporated and recoverable
units and banking/extinguishing outcome; existing ground stock is additional.

Stage the flask debit and residual ground credit in the existing transaction.
Reserve before callbacks; claim the hearth and revalidate exact captured Parts,
amounts, ownership and anchor before commit. No residual means no new pool,
coating, contact or irrigation from the consumed dose. Existing older pool is
preserved. Only surplus can irrigate or participate in ground contact.

Full ApplyHeat runs after commit, before this pour's contact/audio, exactly once.
It can remove effects, emit Extinguished, shatter, or invoke arbitrary callbacks:
those are authoritative committed consequences, never a reversible scalar edit.
Outer veto/exception before commit refunds staged quantities and produces no
thermal event. Callback failure after commit cannot refund spent water. A
cooling-specific inventory receipt must preserve independent injury or a forced
flask drop when a changed hearth causes precommit rejection; generic blanket
item/stat snapshot restoration cannot be used for this path. Bounded quantity inverses preserve representable independent additions. Raw
callbacks that consume provisional credit, overfill, detach or replace a liquid
Part cannot receive an invented rollback guarantee: quantities are bounded and
identity changes diagnosed rather than restored into another liquid. Cooling owns
the net pool credit after any crop absorption, so separate bounded inverses cannot
erase an independent addition. Postcommit contact and success reporting recheck
the exact current pool/hearth Parts and anchors after thermal callbacks.
This does not claim
to repair every older liquid transaction's callback policy.

Native cases must cover tariffs/surplus/old stock/no ghosts, freshwater/mire/brine,
ordinary town/cold/fuel counters, stale menu and preparation changes, rollback,
reentry, callback failure, independent heat/injury/drop and persistence. Actual
GameBootstrap input must prove a paid cooling dose cannot be recovered and reused,
and show residual recovery plus save/fresh continuity. Tests are not play.

Lore: this is finite material handling and workmanship, with no new supernatural
energy source or explanation of protected mysteries. Shared future cooling of
other materials remains a separate capability increment.
