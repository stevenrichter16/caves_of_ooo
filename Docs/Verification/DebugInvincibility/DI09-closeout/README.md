# Invincibility verification

F12 toggles damage/death/dismemberment immunity for the living player, without spending a turn or changing HP. OFF by default, resets on load/recreated player. Intentional nonlethal spell costs remain.

DI07:230/230 targeted checks. DI08:14516 passed,32 unchanged baseline failures,zero C# errors. All40 new cases pass, including22 dedicated adversarial cases. Existing failure names/messages are identical to CQ18. DI06 was intentionally interrupted for test cleanup; its receipt is not a completed regression result.

Native keyboard injection verified input wiring and ON/OFF text; no physical keyboard or running-session reload claim.2,107 frozen source/content/assembly/meta inputs are identical between the working project and isolated test project. GUID audit has no task collisions. Raw XML/logs remain local; compact receipts are committed.

Mixed CombatSystem/InputHandler changes are installed in the working tree, with exact phase-only delta preserved in implementation.patch and hashes in integration.json. Clean shared/new files are committed directly. Unrelated work remains untouched.
