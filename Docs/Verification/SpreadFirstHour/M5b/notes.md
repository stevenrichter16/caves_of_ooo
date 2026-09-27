# Drain-slime admission: deferred

Executed in Unity 6000.3.4f1 Play mode under NativeSaveIsolation, seed 1729,
with detached factory CaveSlime and Player graphs. The 40 HP Player had zero
AcidResistance; no HP, resistance or healing was added. This is an isolated
mechanic probe, not ordinary discovery, AI frequency or a survival playtest.
The actual CommandAcidSpray and status lifecycle events were used. One cast
was admitted directly, without changing the production AI chance or dice.

One 4-damage initial hit plus 16 corrosion ticks left the Player at **1/40 HP**.
Actual movement put the target outside cast range on the third turn; the
remaining coating continued dealing damage. No damage occurred during seven
additional turns after real effect expiry. A hedge blocked the cast preview.
A separate 40 HP target receiving a second cast after its actual cooldown
expired died on the thirteenth subsequent turn. Full per-turn receipts and
the exact probe are beside this note.

Decision: do not add SpreadDrainSlime, its source, or assets in this release.
A first-hour ranged threat that can remove 39/40 HP from one exposure is not
admitted merely because it is optional. This result does not prove that every
ordinary player would die or that healing/control cannot help. A later design
needs a demonstrated readable bypass and viable ordinary-kit aftermath; no
global acid nerf or fabricated resistance was introduced to pass the gate.

The native audit used a disposable save root and did not load or mutate the
user's save. Finish requested normal Play shutdown and restoration of the
previous bootstrap seed/root/preferences. No scene or GameView settings were
changed by this probe.
