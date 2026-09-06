# GA02d — acquisition integrity and committed currency

Status: COMPLETE. Full8040/8040 GREEN, zero C# errors. Baseline e70b56b5,7939 GREEN.

## Outcome and boundaries

A43: ground pickup now requires an actual unowned source in the supplied zone
and a positive quantity, then revalidates after actor/item veto hooks. Adjacent
pickup stays supported. Gold source units are spent once and removed before
acquisition hooks; their five-drams-per-coin credit becomes spendable only when
the enclosing inventory transaction commits. Stale, foreign, already-owned and
empty sources cannot pay. Refusal/exception restores the exact source and count.

Container take and buying use GA02c's exact source/destination receipts. Complete
and partial merges unwind without changing unrelated work. Failed capacity no
longer re-merges source siblings or changes which physical stack remains.
Acquisition commands claim participating items and affected destination stacks
before exposing callbacks. Buying checks stock, pricing and funds again after
BeforeTrade, propagates the real refusal to its screen, and preserves successful
same-screen retry when capacity is cleared by the test.

Wallet changes now queue as transaction deltas. Commit stages and validates all
resulting purses before any payment, writes the complete batch, commits items
and releases claims, then publishes the usual IntPropertyChanged Name/OldValue/
NewValue notifications. A notification exception emits CurrencyObserverFailed;
it cannot restore already-paid goods or prevent another recipient being paid.
Buy and sale share this boundary. Failed precommit overflow restores sources
without rewinding independently committed payments or purchases. No public Part
field, currency API representation or save-version change was introduced.

Ground-only gold conversion is retained: coins taken from containers remain
carried trade goods. Pickup/container Taken events describe immediate acquisition;
arbitrary quest/effect callbacks still are not generally rolled back (A41).
Immediate pickup messages/ItemAcquisitionApplied can precede an enclosing refusal;
CurrencyCreditApplied/DebitApplied describe committed wallet deltas. Claims do
not lock raw dictionaries, every item action, or arbitrary future code.

## Verification evidence

Compiler logs are checked before trusting fresh XML.

| Gate | Raw archive | Result |
|---|---|---|
| Initial RED | GA02d-red.xml.gz |34:21 fail,13 controls;23:24:57UTC |
| Minimum + neighbors | GA02d-green.xml.gz |429/429;23:28:41–42UTC |
| Gold-hook RED | GA02d-adversarial-red.xml.gz |71:11 fail,60 controls;23:32:10UTC |
| Hooks expose wallet/veto gaps | GA02d-hooks-wallet-red.xml.gz |71:3 fail,68 controls;23:33:42UTC |
| Currency dependency RED | GA02d-currency-red.xml.gz |77:8 fail,69 controls;23:37:11UTC |
| Deferred credit + neighbors | GA02d-adversarial-green.xml.gz |472/472;23:39:33–34UTC |
| Observer probe first attempt | GA02d-wallet-observer-red.xml.gz |94:6 fixture failures,88 pass;23:45:02UTC |
| Native staging RED | GA02d-bench-red.xml.gz |101:7 missing-type failures plus6 unrepaired probes;23:47:28–29UTC |
| Corrected observer RED | GA02d-wallet-observer-corrected-red.xml.gz |60:5 true failures,55 controls;23:50:25–26UTC |
| Native harness compile check | GA02d-native-compile-red.log.gz |one invalid API,three repeated error-CS lines; no XML trusted |
| Final focused | GA02d-bench-green.xml.gz |496/496;23:56:52–54UTC;zero C# errors |

New cases:34 regression +60 dedicated adversarial +7 scenario/diagnostic =101.
They include source membership, exact item identity/order/count, real capacity
boundaries, same-screen retry, hooks/vetoes, exception retry, nested independent
and same-item operations, source/destination merge claims, multiple recipients,
combined credit overflow, provisional spending, committed notifications and
idempotent Commit/Rollback. Many passed on first run; they are regression pins,
not101 separate bugs. Ordinary gameplay uses actual factory-created fixtures;
malformed states and custom callbacks are explicitly extension/API controls.

## Methodology corrections and review

Two TDD errors are retained honestly. The minimum implementation initially
unified gold hooks before its dedicated assertions existed; that extension was
reverted to prior skip-hooks behavior, its11 failing cases observed, then it was
re-enabled. The first wallet-observer tests read typed event parameters through
generic object getters and asserted goods ownership on the money receiver.
Those six failures were fixtures, not gameplay evidence. Corrected getters and
payer ownership were rerun against our earlier setter-based payment iteration:
five real callback ordering/exception cases failed, with nonthrowing sale as a
valid control. Only then was the staged implementation reapplied and verified.

Other fixture corrections: TradeUI must be created with AddComponent and safely
destroyed; inner+outer successful gold commits emit two credit records. Native
compile caught an invented GetVisibleChoices API; actual VisibleChoices/Actions
keys and dialogue cursor were verified against the shipped GA02c driver. An
overescaped diagnostic substring was corrected before native execution.

Root and independent taxonomy/Qud-contract reviewers traced exact receipts,
claims, both trade directions and Entity.SetIntProperty. Its synchronous event
was a false premise in early reasoning, caught by the independent reviewer.
The final staged implementation passed the corrected five REDs. Property
notifications release pooled events in finally; the preexisting general
Entity.FireEventAndRelease exception-pooling issue is recorded under A41.

Qud references: Inventory.CommandTakeObject2185–2295, GameObject.ReceiveObject5381,
TradeUI1495. Qud includes a broader containment/acceptance/theft/receive protocol;
CoO's ground-only source restriction, hard150 capacity, gold conversion and
transaction receipts are deliberate local guarantees. CoO buying retains its
preexisting lack of pickup/Taken hooks; no complete Qud receive parity is claimed.

- 🟡 Fixed: stale/foreign/owned gold payouts and missing source/payment rollback.
- 🟡 Fixed: complete/partial merge undo and refused source sibling re-merging.
- 🟡 Fixed: misleading buy UI, callback fund reuse and provisional spending.
- 🟡 Fixed: synchronous property notification could interrupt a wallet batch.
- 🟡 Fixed verification: typed-event and goods-owner probes, native API/payload.
- ⚪ Kept: container coins as goods, legacy bool trade API and local capacity.
- 🧪 Bounds: synthetic callback/malformed tests do not prove ordinary UI exploits;
  A41 arbitrary callback effects and A03 functional stack identity remain queued.

No ordinary per-frame/per-turn production path was added. Transaction wallet
work happens only during inventory operations. There is no performance speedup
or 75-second capture claim, and no new content or art. GUID audit:2333 unique,
zero collisions. Protected ownership audit and final evidence follow below.


## Final native, full-suite and ownership gates

GA02d-native.json records runa7e5751c50684a09befc4eba9c14355b,
57/57 PASS,0 failures,12.251163709seconds, batch exit0. The matching full Unity
log is GA02d-native-unity.log.gz; zero compiler errors. Ground GoldCoin3 paid15
once and could not pay again. Real Sack and Merchant stock each retained their
exact99+1 source stacks when the player's52 apples made acquiring99 exceed150.
Native inventory Eat spent one apple; repeat looting/buying succeeded at51.
Intermediate native Drop and small-stack looting checked exact merged quantities,
source identities, carried/ground ownership and wallet arithmetic. Real Chat,
world interactions, loot/inventory and trade controls routed every action.

**Can verify:** scripted keyboard routing, actual content and menu selections,
source order/counts/backreferences, payment, capacity refusal and successful retry,
fresh diagnostic records and isolated audit completion. **Cannot verify:** pixels
or feel, ordinary UI exploits from synthetic callbacks/malformed states, or a
native same-screen retry (native closes/eats/reopens; EditMode covers that screen).
Manual cleanup is source-reviewed. The previously recorded A31 destroyed-camera
error appears twice after capture during shutdown; logs retain it. Licensing
also reports an unavailable access token; neither prevented the run.

Final full suite GA02d-full-green.xml.gz:8040/8040 GREEN, zero C# errors,
2026-09-05 23:59:53UTC through2026-09-06 00:01:21UTC (still September5 locally).
Independent final production and native reviews found no remaining must-fix
findings. The native failure message's stale “Transfer” label was corrected to
“Acquisition” after verification; no behavioral code changed after these runs.

Ownership audit: 36 task-owned paths; zero overlap with the1027-path
preexisting/concurrent-work manifest. Stage only this explicit list.
