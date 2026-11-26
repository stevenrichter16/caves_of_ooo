# The Peat Works rig explains when it can recover a haft

The ordinary Peat Works seed 64 save starts at tick 620. The player has
their leather-bound mallet 9354, dagger 2698 and two bought measures of
prepared lining clay 6854. The visible adjacent rig 7420 is still broken.

- In the native pack, forge recovery is unavailable and says the
  nearby forge or rig needs repair. Selecting the row preserved the
  entire pack, the rig, position and tick. The field cutter alternative
  stayed available. [Nearby fault](needs-repair.png)
- The ordinary world interaction quoted the two clay measures. The
  player paid those exact measures in one action. At tick 630 their
  same mallet offered both forge and field recovery. Their dagger,
  40 HP and 39 drams remained. [Both recovery choices](ready-for-recovery.png)
- F5 and a fresh Editor Continue kept the repair and both choices at
  tick 630. The retained save still holds the same mallet. Earlier
  [physical recovery evidence](../FieldRigCompatibleClay20261125/README.md)
  verifies what the functioning rig returns if that option is taken.

The old runtime failed all four new disabled-row checks and passed the
ready-forge control. The first corrected Unity run passed 120 cases,
including missing, obscured, adjacent broken and working stations, a
move out of reach, one physical repair and recovery, and existing
inventory timing and receipt tests. Independent source and play review
also found an invisible owner on an otherwise visible cell could reveal
the specific fault. A separate native check reproduced that issue.
The final guard requires a visible owned render. The combined run
passed 398 cases. The current Mac predates this change.
[Native observations and test hashes](observations.json)
