# Sumphold oil extends the Sill watch lantern

Seed 2 generation 31 began at the retained ordinary Sumphold oil
delivery save, tick 2160, with 43 drams. The player bought one actual
portion from Vellin's three-unit consignment for 14 drams and took it
to Sill. The elder gave the existing oil recipe. The warden accepted
the one purchased portion and the original lantern became stably
repaired. The recipe and 29 drams stayed with the player.

| Native observation | Result |
| --- | --- |
| Sumphold tick 2160 | Selected one unit from stock 12629 in the ordinary trade interface. |
| Sill tick 2650 | At `15,15`, the original lantern 18296 still used radius 10. The renderer measured 0.4000 at the already visible path cells `13,8` and `14,10`. |
| Sill tick 3640 | The warden's existing paid repair consumed the purchased oil and set the exact lantern to StableRepair, radius 12. |
| Return at tick 4120 | The player stood at the same `15,15` approach. With the same 0.4 ambient level, the two visible path cells read 0.5228 and 0.5163. The approach cell itself remained 0.8. |
| Fresh Continue at tick 4120 | Exact lantern, measured light, recipe, purse and position matched. |

The guided route met a natural soot gremlin. The player equipped their
existing short sword, a nearby innkeeper struck the gremlin, and the
player finished the fight. Final health was 26/40. The screenshots span
ordinary movement and combat between the two views, with both views at
the same approach position. Existing light flicker still runs.

This change extends actual renderer illumination on a paid stable
repair. The original light, occlusion, fog and existing creature rules
provide the baseline. The playtest did not establish any creature
deterrence. The existing Mac package predates this change.

- [Original approach](before.png)
- [Same approach after repair](restored.png)

The original native run failed the radius assertion with the three
detached-owner controls passing. The connected corrected run passed
151 of 151 checks. The renderer unit test uses a managerless zone.
The actual Sill play used the normal world and settlement manager.
Independent review checked the source, both captures, the saved files
and the recorded light measurements.
[Exact play and test receipts](observations.json)
