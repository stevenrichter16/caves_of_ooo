# Scoped flight scratch peer review

Read-only delta from frozen stage3 to stage4; no tests/Unity/source changes by this reviewer. No concrete blocker found.

The retained per-grazer queue/set contain coordinate/depth/direction values only, are nonserialized and initialize lazily after replacement load. Both are cleared before search and again before MovementSystem.TryMove can publish callbacks. The synchronous search reads movement blockers/LOS without dispatching simulation actions, preserving the original neighbor order, score/tie order and finite depth. Reentrant movement cannot inherit an outer unfinished search; no post-movement scratch reads exist. Saved flight/hunter/phase fields and role admission are unchanged.

Whole-stack checks now use BrainPart.GoalCount/PeekGoalAt with the same order and exact goal types/parent-membership test as the previous copied list. The read-only loop contains no callbacks or goal mutations. Eligible replaces only the Parts.Any predicate with an equivalent indexed type test; authority and current-owner gates remain. Existing animation/interaction/readout paths are untouched.

Reported private allocation RED/GREEN and reference results belong to the mechanics agent; this note does not claim additional execution or native performance. Root owns native allocation and behavior verification.
