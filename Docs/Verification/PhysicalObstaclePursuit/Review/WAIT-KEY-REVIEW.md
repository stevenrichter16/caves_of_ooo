# Controlled pursuit wait-key correction

Source review confirms the exact observer correction `Tap(Key.Space)` → `Tap(Key.Period)` for the existing paid-wait call. No gameplay or adapter change is needed.

`InputHandler.cs:497–516` handles unshifted Period or Keypad5 while Normal by calling EndTurnAndProcess. There is no corresponding Space wait branch. `InputHelper.cs:61,68` maps both keys accurately; the observer selected the wrong mapped key. Existing successful Hunting.cs:121 uses ExchangePaid(Tap(Key.Period), "local", ...), matching the proposed correction.

Keep the same action-count cap, native input Tap, ordinary scheduler and ExchangePaid evidence. `Exchange.cs:47–56` increments attempted paidInputs before requiring the real current-player diagnostic/tick/energy receipt. Thus retained attempt3's paidInputs=2 means two attempted inputs; only its east pull has a validated paid receipt. The Space attempt failed that receipt and does not establish an NPC wait or a pursuit baseline.

Root reports attempt3's new scalar render observations prove the prior timing premise: before the render boundary cellVisible=false with all other observed current/render flags true; after EndOfFrame all visual preconditions, cue, grab, pull and release passed. That closes the first-frame observer timing issue but not the pursuit behavior gate.

The key change is one observer argument only. No source, positions, actor target, goal, time/energy, path or rendering should be forced. Root retains every prior failure and owns native execution. This bounded read made no shared edits, Unity calls or Git changes.
