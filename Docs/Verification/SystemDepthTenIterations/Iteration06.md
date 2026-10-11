# Iteration 06 — companion combat participation

Status: standalone RED→GREEN, independent review and final native Unity EditMode verification complete. Iteration09 integrated its saved Stay order into the responder gate.

The player attacks through `CombatSystem.PerformMeleeAttack`; `FollowLeaderGoal` only watches AI leaders with a `KillGoal`. Connect committed player melee attempts to current recruited allies. Also connect actual hostile HP harm to bounded local self/party defense. This is CoO design, not a new Qud-parity claim.

## Verified premises and corrections

- `CombatSystem.cs:108–128` has the canonical melee veto. Signal after that gate, so a miss is a committed swing while a veto does not rally anyone.
- `BrainPart.HandleTakeTurn` skips Player-tagged owners. Installing an AI combat goal on the player would not solve the integration and risks stale state. Use a stateless helper that assigns the existing follower goal instead.
- `RecruitedEffect` owns the leader link and persistent `FollowLeaderGoal`; inspect both plus the exact roster before dispatch.
- Existing `KillGoal` provides bounded six-action last-seen pursuit, target death/departure cleanup and save persistence. Preserve it; do not invent a second target timer.
- Positive damage already has a canonical post-HP boundary. Restrict reactive defense to existing hostile, nonparty sources and direct player party members. Every responder must independently see the attacker and harmed member. No faction-wide relay, hidden tracking, player FOV dependency or free attack.
- The navigation/order agent owns `FollowLeaderGoal`, `BrainPart`, `RecruitedEffect` and zone transit. This slice owns a new helper and narrow `CombatSystem` hooks, with the `CompanionOrders.IsStaying` responder gate added by iteration09 to keep each scoped commit independently compilable.

## Acceptance and tests

Actual Player-tagged canonical attacks, misses/veto, exact target, duplicate notification, active-goal priority, perception and same-zone ownership, party/allegiance changes, death/departure, bounded search, save graph reconstruction; positive/zero/vetoed hostile damage to leader/self/other member, no neutral or party aggression. Counterchecks and a dedicated adversarial fixture are required.

## Scope and honesty

No general threat scoring, faction alert bus, follower equipment interface, arbitrary neutral-on-neutral retaliation, or spell-assist expansion. Staying companions do not abandon their orders to pursue. Only ordinary scheduled AI actions execute the selected combat goal. Standalone .NET checks can establish gameplay and production save graph behavior; native Unity EditMode is now verified; actual input, Play rendering and feel remain separate evidence.

## Verification / review / files

- Observed standalone RED: `iteration06-red.xml`, 15 cases: 8 failed for the missing combat goal and 7 counterchecks passed. No production was changed before that run.
- Initial GREEN: `iteration06-initial-green.xml`, 15/15.
- Adversarial/companion regression: `iteration06-adversarial-green.xml`, 69/69: 15 new core + 32 dedicated adversarial + 22 existing follow cases. .NET SDK 10.0.105, isolated copy of the tracked .NET 8 runner under `/tmp/system-depth-combat-runner`, `COO_REPO` pointing at this checkout; single worker.
- Independent cold-eye review by the navigation/order agent found no blocking issue in current ownership/witness flow. Its concrete integration note is accepted: Stay gates responders, not membership, so following allies can still defend a staying victim.
- Self-review: 🟡 closed player assist disconnect and bounded positive-damage response; 🔵 intentionally exclude current busy/peaceful/conversing followers, hidden witnesses, neutral damage, and lethal victims; 🧪 actual Play/input/render/feel evidence remains separate from the passed native EditMode suite.
- Changed files: new `CompanionCombat.cs` + meta, two narrow hooks in `CombatSystem.cs`, new core/adversarial tests + metas, this log and receipts. No shared follower/order file edited.
- No new resource, timer, static cache or save format was introduced. Existing KillGoal and recruitment references are tested through the production token-graph save pipeline.

- Final native Unity EditMode: [native-final-integration.xml](native-final-integration.xml), job `941ad3a3628542c19f5fda2b0a9e49b4`, **810/810 passed, 0 failed, 0 skipped**. All 47 CompanionCombat core/adversarial cases passed. This is native gameplay/save-graph evidence; it does not claim an organic recruitment/combat playthrough or controller feel.
