# Canonical village quests

Status: implemented and verified. Canonical quests and model counterchecks pass
in CG17 and CG18; no new full-suite failures remain.

The six generic village quests already share global quest IDs, objective facts and completion state. They are six individual stories, not reusable local quest templates. Fresh generation now gives each story exactly one canonical surface host; other villages retain normal services and bespoke local work. Sill's starter quests and Morrowfast are unchanged.

| Host | Address | Existing story |
|---|---|---|
| Tine | 13.7.0 | CrunchyLocket — lost personal token |
| Quillhold | 14.9.0 | HiddenShrine — remembered shrine |
| Wellmeet | 8.16.0 | ClearTheWarren — a farmer's trouble |
| Gantry | 7.8.0 | TheCandyTax — exchange clerk's collection |
| Posy | 5.9.0 | MessageForHermit — baker and withdrawn neighbor |
| Sumphold | 15.6.0 | StrongestInOoo — physical labor feat |

These placements do not create new faction claims or expand the stories. `VillagePopulationBuilder.PickVillageQuest` uses exact addresses and returns null elsewhere. The placement switch has no fallback: an unassigned village cannot silently create a shrine quest. Existing conversation and objective IDs remain unchanged, preserving completion/reward guards.

A scoped-instance design was rejected for this bounded fix: scoping quest IDs alone would leave shared facts, marker effects, item identity and dialogue arguments able to affect another town. Unique placement removes that duplication for new generation without a registry refactor.

## Verification and limits

CG02-discovery-red captured all nine new canonical assignment/native generation tests failing with zero C# errors before implementation. Existing distribution tests now require each story once across the 400 surface addresses, rather than a story at every address. Dedicated adversarial tests cover malformed/depth addresses, the old default-shrine trap, retained services, generation order, revisits and active/completed progress through serialization. Existing native objective/action fixtures remain necessary regression coverage for quest completion and rewards.

Old cached graphs are not rewritten. Saves with already generated duplicate givers may retain them; this is not a migration or a retroactive removal pass. These changes do not restore destroyed givers or guarantee successful placement in arbitrary malformed test terrain. The real manager census tests cover the six authored host pipelines at two seeds.

Qud reference: not applicable; this is Caves of Ooo content identity. No shared quest lifecycle, reward or dialogue implementation changed. Production changes are in VillagePopulationBuilder and the exact quest-actor mappings in SpawnRing3DRecipes; tests and this living document accompany them. Independent review is complete; final full regression verification remains pending.

### Full-suite rendering integration correction

CG13 exposed five existing Wellmeet model-coverage failures after moving the Warren there. Its authored farmer and gnomes had explicit conversation/quest or kill-fact guarded models in other working towns, but Wellmeet was excluded from those exceptions. The recipe now applies those same narrow contracts at Wellmeet, reusing its existing adult and small humanoid models. Arbitrary glyph reskins remain rejected. Two additional native-owner counterchecks mutate the farmer conversation or gnome kill fact and require fallback, then restore the exact model. CG14 passed the new counterchecks and all five affected model-coverage cases.

### Final full-suite comparison

CG14 passed all 354 targeted cases. CG15 completed with 14,726 passed and
32 failed, zero C# errors. Its exact 32 failure names match the pre-change
CG01 baseline; no new failures remain. See the central implementation report
for rendered acceptance and its limits.

Final acceptance: CG17 356/356 focused, CG18 14,728 passed with the same 32
pre-existing failures, zero C# errors. CGN06 passed all 28 live checks and
60-second aggregate editor profiling; see the central implementation/native
reports for exact scope, visual evidence and performance bounds.
