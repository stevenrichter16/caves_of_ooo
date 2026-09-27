# M0 execution evidence

Current baseline `0417e145`, Unity 6000.3.4f1, 27 September 2026.

## Source census

`source-census.json` observes detached actual Unity generation for seeds 1, 64,
1729: glade 11.10, Sill 10.10, and adjacent 11.9, 12.10, 11.11. Factory,
loadout/container/trader wiring uses the existing DensityLootTestScope; its
explicit per-seed RNG is not a production-global-RNG replay. Static scope was
disposed. No user save was used.

All three Sill samples contain actual stocked Weaponsmith, Armorer and Provisioner
owners, canonical Ellun/Hallun conversation owners and the OldStump. Ellun/stump
coordinates respectively: seed 1 (56,14)/(66,3), seed 64 (49,9)/(2,5), seed 1729
(29,7)/(29,8). A constant eastward instruction is false for the latter two.
Nearby containers can already contain ordinary weapons/armor (e.g. seed 1729
11.11 contains Dagger, Hatchet and LeatherBoots). No guaranteed extra early gear
source or general loot increase is justified by this census. Shop gear is not
automatically offered, affordable, legally reachable or acquired; friendly gear
is not a reward. Native discovery remains a separate gate.

## Sidebar observation

The actual isolated Play frame is retained in `isolated-gameplay-sidebar-before.png`
and `sidebar-before.json`. It is a 1920x1080 full-frame capture with a 459x1080
sidebar camera. 710 sprite bounds produced zero outside-camera glyphs, but a
minimum inset of zero pixels. Vitals touches the top boundary. The probe's Screen
dimensions differed from the fixed GameView camera dimensions; use projected
camera bounds, not the Screen report alone, for layout assertions.

The ad-hoc N press remained held between tool calls and autorepeated normal
movement after the boot menu closed. The captured actor reached 12.12 at tick850
without injury. This is **not** an ordinary discovery, pacing or quest-route
witness. Release was queued, the temporary view setting was restored, and native
save isolation requested cleanup. Subsequent input checks must release on the
next rendered frame, using a finite pulse driver rather than separate tool calls.
This harness mistake does not establish a gameplay defect.

## Decisions before implementation

- Keep existing start/Sill rewards and repair guidance; do not force an extra
  near-start encounter or reward on the basis of scarcity.
- Rare content is a regional variety addition, with E2/E3 admission gates intact.
- Layout is a measured zero-margin issue; outside-camera clipping was not shown.
- No new global encounter ledger, retention rule or save-format bump is justified.
  Use bounded world/entity metadata for fresh-world selection and no old-save
  backfill. A versioned empty plan must remain distinguishable from no plan.
