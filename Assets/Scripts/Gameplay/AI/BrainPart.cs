using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// AI state — informational, set by goals for backward compatibility.
    /// </summary>
    public enum AIState
    {
        Idle,
        Wander,
        Chase
    }

    /// <summary>
    /// AI Part that handles the TakeTurn event for NPC creatures.
    /// Mirrors Qud's Brain: owns a goal stack that drives behavior each tick.
    ///
    /// The goal stack is a LIFO list of GoalHandler objects. Each tick:
    /// 1. Finished goals are popped from the top
    /// 2. If the stack is empty, a BoredGoal is pushed as the default
    /// 3. The top goal's TakeAction() is called
    /// 4. If TakeAction pushed a child, the child executes immediately too
    /// </summary>
    public class BrainPart : Part
    {
        public override string Name => "Brain";

        // Configuration (settable from blueprint params)
        public int SightRadius = 10;
        public bool Wanders = true;
        public bool WandersRandomly = true;
        public float FleeThreshold = 0.25f;

        /// <summary>
        /// Passive creatures do not proactively initiate combat. They'll still defend
        /// themselves against entities in <see cref="PersonalEnemies"/> (populated when
        /// they're directly attacked), and they'll still flee when HP drops below
        /// <see cref="FleeThreshold"/> — but a Passive scholar won't chase a snapjaw
        /// across the zone just because it walked into sight.
        /// Mirrors Qud's Brain.Passive flag. Used by non-combat NPCs (scholars, clerics,
        /// civilians, wildlife that doesn't hunt).
        ///
        /// Semantics in <c>BoredGoal.TakeAction</c>:
        ///   <c>canInitiate = !Passive || IsPersonallyHostileTo(hostile)</c>
        /// Engagement happens when <c>canInitiate || ShouldFlee()</c>.
        /// </summary>
        public bool Passive = false;

        // Runtime state
        public AIState CurrentState = AIState.Idle;
        public Entity Target;
        public bool InConversation;

        /// <summary>
        /// Entities this NPC is personally hostile toward, independent of faction.
        /// Mirrors Qud's per-NPC opinion system (simplified: permanent hostility).
        /// </summary>
        public HashSet<Entity> PersonalEnemies = new HashSet<Entity>();

        public void SetPersonallyHostile(Entity target)
        {
            if (target == null) return;
            bool wasNew = PersonalEnemies.Add(target);
            Target = target;
            InConversation = false;

            if (wasNew && CurrentZone != null)
            {
                var myPos = CurrentZone.GetEntityPosition(ParentEntity);
                if (myPos.x >= 0)
                    AsciiFxBus.EmitParticle(CurrentZone, myPos.x, myPos.y - 1, '!', "&R", 0.25f);
            }
        }

        public bool IsPersonallyHostileTo(Entity target)
        {
            return target != null && PersonalEnemies.Contains(target);
        }

        // --- Followers (Phase F.1.2) ---

        /// <summary>
        /// The entity this creature is following, if any. Set via
        /// <see cref="SetPartyLeader"/> to preserve bidirectional integrity
        /// with the leader's <see cref="PartyMembers"/>.
        ///
        /// <para>Mirrors Qud's <c>Brain.LeaderReference.Object</c> (see
        /// <c>/Users/steven/qud-decompiled-project/XRL.World.Parts/Brain.cs:217</c>).</para>
        /// </summary>
        public Entity PartyLeader;

        /// <summary>
        /// Entities that are following this creature. Mirror of
        /// <see cref="PartyLeader"/> on each follower's brain; maintained
        /// automatically by <see cref="SetPartyLeader"/>.
        ///
        /// <para>Mirrors Qud's <c>Brain.PartyMembers</c> collection
        /// (Brain.cs:229). CoO simplifies the per-member flags map to a
        /// HashSet — flags can be added later via a parallel
        /// <c>Dictionary&lt;Entity,int&gt;</c> when F.2's typed-allegiance
        /// reasons require them.</para>
        /// </summary>
        public HashSet<Entity> PartyMembers = new HashSet<Entity>();

        /// <summary>
        /// Set this creature's leader, maintaining bidirectional state.
        /// Mirrors Qud's <c>Brain.SetPartyLeader</c> (Brain.cs:895-948).
        ///
        /// <para><b>Rejected cases:</b>
        /// <list type="bullet">
        ///   <item>Self-reference (<c>newLeader == ParentEntity</c>) →
        ///         returns false, no state change.</item>
        ///   <item>Cycle creation (<c>newLeader.IsLedBy(ParentEntity)</c>
        ///         transitively) → returns false, no state change.</item>
        /// </list></para>
        ///
        /// <para><b>Side effects on accept:</b>
        /// <list type="bullet">
        ///   <item>Removes self from old leader's <see cref="PartyMembers"/>
        ///         (if old leader exists and has a BrainPart).</item>
        ///   <item>Assigns <see cref="PartyLeader"/> = newLeader.</item>
        ///   <item>Adds self to new leader's <see cref="PartyMembers"/>
        ///         (if newLeader has a BrainPart — leader-without-brain is
        ///         tolerated for Qud parity).</item>
        ///   <item><b>Forgive:</b> removes newLeader from
        ///         <see cref="PersonalEnemies"/>, so a freshly-recruited
        ///         creature doesn't re-aggro its new leader on the next
        ///         tick. Mirrors Qud's <c>Forgive</c> step at Brain.cs:924.</item>
        /// </list></para>
        ///
        /// <para>Setting the same leader twice is idempotent — no duplicate
        /// roster entry, no spurious Forgive replay.</para>
        ///
        /// <returns>True if the leader was set (or no-op already-equal),
        /// false if rejected.</returns>
        /// </summary>
        public bool SetPartyLeader(Entity newLeader)
        {
            // Self-reference: rejected.
            if (newLeader != null && newLeader == ParentEntity)
            {
                Think("can't follow self");
                return false;
            }

            // Idempotent: same leader → no-op success.
            if (newLeader == PartyLeader)
            {
                return true;
            }

            // Cycle detection: if newLeader (or anyone in its chain) is led
            // by this entity, accepting would create a loop.
            // Walks newLeader's leader chain looking for ParentEntity.
            if (newLeader != null && WouldCreateCycle(newLeader))
            {
                Think("leader cycle blocked");
                return false;
            }

            // Remove self from old leader's roster (bidirectional mirror).
            if (PartyLeader != null)
            {
                var oldLeaderBrain = PartyLeader.GetPart<BrainPart>();
                if (oldLeaderBrain != null)
                {
                    oldLeaderBrain.PartyMembers.Remove(ParentEntity);
                }
            }

            // Assign new leader.
            PartyLeader = newLeader;

            // Add self to new leader's roster.
            if (newLeader != null)
            {
                var newLeaderBrain = newLeader.GetPart<BrainPart>();
                if (newLeaderBrain != null)
                {
                    newLeaderBrain.PartyMembers.Add(ParentEntity);
                }
                // Forgive: clear the new leader from PersonalEnemies so the
                // recruit doesn't immediately re-aggro.
                PersonalEnemies.Remove(newLeader);
            }

            return true;
        }

        /// <summary>
        /// Walks <paramref name="candidate"/>'s leader chain. Returns true
        /// if accepting candidate as our leader would create a cycle
        /// (candidate is led by us, directly or transitively).
        /// </summary>
        private bool WouldCreateCycle(Entity candidate)
        {
            // candidate's leader chain: does it contain ParentEntity?
            var node = candidate.GetPart<BrainPart>()?.PartyLeader;
            int safety = 64; // depth cap — prevents pathological infinite walks
            while (node != null && safety-- > 0)
            {
                if (node == ParentEntity)
                    return true;
                node = node.GetPart<BrainPart>()?.PartyLeader;
            }
            return false;
        }

        /// <summary>
        /// Walks the leader chain looking for <paramref name="candidate"/>.
        /// Returns true if this creature is led by candidate (directly
        /// or transitively). Mirrors Qud's <c>Brain.IsLedBy</c>
        /// (Brain.cs:1438).
        /// </summary>
        public bool IsLedBy(Entity candidate)
        {
            if (candidate == null) return false;
            var node = PartyLeader;
            int safety = 64;
            while (node != null && safety-- > 0)
            {
                if (node == candidate) return true;
                node = node.GetPart<BrainPart>()?.PartyLeader;
            }
            return false;
        }

        /// <summary>
        /// Walks the leader chain to the top. Returns the topmost entity
        /// in the chain (the entity with no leader of its own). Returns
        /// null if this creature has no leader. Mirrors Qud's
        /// <c>Brain.GetFinalLeader</c> (Brain.cs:1623).
        /// </summary>
        public Entity GetFinalLeader()
        {
            var node = PartyLeader;
            int safety = 64;
            while (node != null && safety-- > 0)
            {
                var next = node.GetPart<BrainPart>()?.PartyLeader;
                if (next == null) return node;
                node = next;
            }
            return node;
        }

        /// <summary>
        /// F.1.4 — returns true if <paramref name="a"/> and
        /// <paramref name="b"/> are in the same party (one leads the other,
        /// directly or transitively, OR they share an ancestor in the
        /// leader chain — sibling followers under a common leader).
        ///
        /// <para><b>Cases:</b></para>
        /// <list type="bullet">
        ///   <item>a == b → true (trivial self-alignment).</item>
        ///   <item>a.IsLedBy(b) → true (b is a's leader chain).</item>
        ///   <item>b.IsLedBy(a) → true (a is b's leader chain).</item>
        ///   <item>Both share a non-null final leader → true (siblings
        ///         under a common root). Without the non-null check, two
        ///         leader-less entities would both have <c>null</c> final
        ///         leaders and falsely register as aligned.</item>
        /// </list>
        ///
        /// <para>Used by <see cref="FactionManager.GetFeeling"/> to suppress
        /// hostility between party members. Composable with
        /// <see cref="IsPersonallyHostileTo"/> — personal hostility takes
        /// precedence over party alignment (a vendetta against your leader
        /// survives the leadership tie).</para>
        ///
        /// <para>Null-safe: returns false if either entity is null or
        /// neither side has a BrainPart from which to walk the chain.</para>
        /// </summary>
        public static bool ArePartyAligned(Entity a, Entity b)
        {
            if (a == null || b == null) return false;
            if (a == b) return true;

            // Quick direct-link checks (cheap; common case).
            var aBrain = a.GetPart<BrainPart>();
            var bBrain = b.GetPart<BrainPart>();
            if (aBrain != null && aBrain.IsLedBy(b)) return true;
            if (bBrain != null && bBrain.IsLedBy(a)) return true;

            // Sibling check: both walk up to the same non-null final leader.
            // The non-null guard is critical — two unrelated leader-less
            // entities both have null final leaders and would falsely
            // register as aligned without it.
            if (aBrain == null || bBrain == null) return false;
            var aFinal = aBrain.GetFinalLeader();
            var bFinal = bBrain.GetFinalLeader();
            return aFinal != null && aFinal == bFinal;
        }

        // Zone reference (set externally by GameBootstrap)
        public Zone CurrentZone;

        // RNG for AI decisions (injectable for deterministic testing)
        public Random Rng;

        // --- Starting Cell / Home ---

        /// <summary>Cell where this NPC was first placed. Used by BoredGoal to return home.</summary>
        public int StartingCellX = -1;
        public int StartingCellY = -1;
        public bool HasStartingCell => StartingCellX >= 0 && StartingCellY >= 0;

        /// <summary>When true, NPC returns to StartingCell when idle instead of wandering.</summary>
        public bool Staying = false;

        /// <summary>Set the NPC's home cell and enable Staying behavior.</summary>
        public void Stay(int x, int y)
        {
            StartingCellX = x;
            StartingCellY = y;
            Staying = true;
        }

        // --- Hostile target cache (Tier-A Fix #3) ---

        /// <summary>
        /// Cached result of the last <c>FindNearestHostile</c> scan. Populated
        /// by <see cref="AIHelpers.FindNearestHostileCached"/> and validated
        /// (cheap LOS + distance + faction check) on subsequent calls before
        /// being trusted, so a target that died, walked out of sight, or
        /// flipped factions is detected and re-scanned.
        ///
        /// <para>The cache turns a per-NPC O(N · LOS) zone scan into an
        /// O(LOS) validation when targets stay stable, which they do for
        /// most of combat. Re-validated every call; <see cref="_cachedHostileTtlTurnsLeft"/>
        /// forces a periodic full re-scan so the AI doesn't permanently
        /// stick to a non-optimal target if a closer hostile appears.</para>
        /// </summary>
        private Entity _cachedHostile;

        /// <summary>
        /// Turns remaining before the cached hostile is treated as stale and
        /// the next call falls back to a full zone scan. Decremented on every
        /// cache hit; reset to <see cref="HostileCacheTtlMax"/> on every full
        /// scan (whether or not a target was found, so a "no hostile in
        /// sight" answer is also cached for the same TTL).
        /// </summary>
        private int _cachedHostileTtlTurnsLeft;

        /// <summary>
        /// Max TTL — small enough that AI re-evaluates targets reasonably
        /// often (catches "closer hostile just walked into sight"), large
        /// enough that the per-turn AI cost is dominated by validation
        /// rather than full scans. 4 turns is a starting point — playtest
        /// and tune.
        /// </summary>
        public const int HostileCacheTtlMax = 4;

        /// <summary>
        /// True if there's a cached hostile within its TTL. Callers must
        /// still validate it (alive, in zone, in LOS, hostile) before use —
        /// see <see cref="AIHelpers.FindNearestHostileCached"/>.
        /// </summary>
        public bool HasFreshHostileCache =>
            _cachedHostile != null && _cachedHostileTtlTurnsLeft > 0;

        /// <summary>The cached hostile, or null. No validation — caller must verify.</summary>
        public Entity GetCachedHostile() => _cachedHostile;

        /// <summary>
        /// Tick the TTL down by one. Call once per cache hit so the cache
        /// expires after <see cref="HostileCacheTtlMax"/> consecutive hits.
        /// </summary>
        public void TickHostileCacheTtl()
        {
            if (_cachedHostileTtlTurnsLeft > 0)
                _cachedHostileTtlTurnsLeft--;
        }

        /// <summary>
        /// Replace the cached hostile and reset the TTL. Pass null to cache
        /// "no hostile in sight" — the null answer is also TTL'd so we don't
        /// re-scan every tick when the zone is empty.
        /// </summary>
        public void RefreshHostileCache(Entity hostile)
        {
            _cachedHostile = hostile;
            _cachedHostileTtlTurnsLeft = HostileCacheTtlMax;
        }

        /// <summary>
        /// Drop the cache. Call when the cached target failed validation
        /// (died, left zone, lost LOS, faction shift) — the next call will
        /// fall back to a fresh scan.
        /// </summary>
        public void InvalidateHostileCache()
        {
            _cachedHostile = null;
            _cachedHostileTtlTurnsLeft = 0;
        }

        // --- Debug Introspection (Phase 10) ---

        /// <summary>
        /// Most recent thought string set by a goal handler via <see cref="Think"/>.
        /// Null until first call. Surfaces in the AI goal-stack inspector UI when
        /// <see cref="CavesOfOoo.Diagnostics.AIDebug.AIInspectorEnabled"/> is true.
        /// Mirrors Qud's <c>Brain.LastThought</c> — a single slot, not a history buffer.
        /// </summary>
        public string LastThought;

        /// <summary>
        /// When true, every <see cref="Think"/> call also emits a
        /// <c>[Think:{entityName}] {thought}</c> line to
        /// <see cref="UnityEngine.Debug.Log"/>. Default false so production builds
        /// are silent. Set per-entity (e.g. from a scenario) to stream a specific
        /// NPC's reasoning without spamming every creature's thoughts.
        /// </summary>
        public bool ThinkOutLoud;

        /// <summary>
        /// Record a debug thought for this NPC's current tick. Mirrors Qud's
        /// <c>Brain.Think(string)</c>: single-slot <see cref="LastThought"/>
        /// assignment, plus optional <see cref="UnityEngine.Debug.Log"/> echo
        /// when <see cref="ThinkOutLoud"/> is on.
        ///
        /// Safe to call on every goal tick — the no-echo path is a single
        /// field write with no allocation. The expensive interpolation for
        /// the Debug.Log format sits behind the <see cref="ThinkOutLoud"/>
        /// gate so it costs nothing in the common case.
        ///
        /// Goals should call this at BRANCH POINTS (phase changes, gate passes,
        /// bailouts), not inside tight per-frame loops — that would allocate
        /// every tick if the caller interpolated <c>$"hp is {hp}"</c>.
        /// </summary>
        public void Think(string thought)
        {
            LastThought = thought;
            if (ThinkOutLoud && thought != null)
            {
                string name = ParentEntity?.GetDisplayName() ?? "?";
                UnityEngine.Debug.Log($"[Think:{name}] {thought}");
            }
        }

        // --- Goal Stack ---

        private List<GoalHandler> _goals = new List<GoalHandler>();

        private const int MaxChildChainDepth = 10;

        /// <summary>Number of goals on the stack.</summary>
        public int GoalCount => _goals.Count;

        /// <summary>Peek at the top goal without removing it. Returns null if empty.</summary>
        public GoalHandler PeekGoal()
        {
            return _goals.Count > 0 ? _goals[_goals.Count - 1] : null;
        }

        /// <summary>
        /// Peek at a specific stack index without removing. Index 0 is the
        /// BOTTOM (oldest / root — typically BoredGoal); <c>GoalCount - 1</c>
        /// is the TOP (innermost / currently executing). Returns null if
        /// index is out of range. Used by the Phase 10 goal-stack inspector
        /// UI to render the whole chain without exposing the backing list.
        /// </summary>
        public GoalHandler PeekGoalAt(int index)
        {
            return (index >= 0 && index < _goals.Count) ? _goals[index] : null;
        }

        public List<GoalHandler> GetGoalsSnapshot()
        {
            return new List<GoalHandler>(_goals);
        }

        public void RestoreGoalsForLoad(List<GoalHandler> goals)
        {
            _goals.Clear();
            if (goals == null)
                return;

            for (int i = 0; i < goals.Count; i++)
            {
                GoalHandler goal = goals[i];
                if (goal == null)
                    continue;

                goal.ParentBrain = this;
                _goals.Add(goal);
            }
        }

        /// <summary>Push a goal onto the top of the stack.</summary>
        public void PushGoal(GoalHandler goal)
        {
            goal.ParentBrain = this;
            _goals.Add(goal);
            goal.OnPush();
            if (Diag.IsChannelEnabled("ai"))
            {
                Diag.Record(
                    category: "ai", kind: "GoalPushed",
                    actor: ParentEntity,
                    payload: new
                    {
                        goal = goal.GetType().Name,
                        details = goal.GetDetails(),
                        stackDepth = _goals.Count,
                    });
            }
        }

        /// <summary>Remove a specific goal from the stack.</summary>
        public void RemoveGoal(GoalHandler goal)
        {
            if (_goals.Remove(goal))
            {
                goal.OnPop();
                if (Diag.IsChannelEnabled("ai"))
                {
                    Diag.Record(
                        category: "ai", kind: "GoalPopped",
                        actor: ParentEntity,
                        payload: new
                        {
                            goal = goal.GetType().Name,
                            details = goal.GetDetails(),
                            stackDepthAfter = _goals.Count,
                        });
                }
            }
        }

        /// <summary>Clear all goals from the stack.</summary>
        public void ClearGoals()
        {
            for (int i = _goals.Count - 1; i >= 0; i--)
                _goals[i].OnPop();
            _goals.Clear();
        }

        /// <summary>
        /// Emit an <c>ai/TurnSkipped</c> diag record. Used by every
        /// HandleTakeTurn early-return path that's NOT the player frame
        /// (which would flood the buffer). Lets a debug query answer
        /// "why didn't this NPC act last turn?" without log-grep.
        /// </summary>
        private void EmitTurnSkipped(string reason)
        {
            if (!Diag.IsChannelEnabled("ai")) return;
            Diag.Record(
                category: "ai", kind: "TurnSkipped",
                actor: ParentEntity,
                payload: new
                {
                    reason,
                    goalStackDepth = _goals.Count,
                    topGoal = _goals.Count > 0 ? _goals[_goals.Count - 1].GetType().Name : null,
                });
        }

        /// <summary>Check if any goal of type T is on the stack.</summary>
        public bool HasGoal<T>() where T : GoalHandler
        {
            for (int i = 0; i < _goals.Count; i++)
            {
                if (_goals[i] is T) return true;
            }
            return false;
        }

        /// <summary>
        /// Check if any goal on the stack has a type whose class name equals typeName.
        /// Mirrors Qud's Brain.HasGoal(string) — used by behavior parts to gate
        /// "am I already doing X?" (e.g. "TurretTinker only places a turret if !HasGoal('PlaceTurretGoal')").
        /// </summary>
        public bool HasGoal(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;
            for (int i = 0; i < _goals.Count; i++)
            {
                if (_goals[i].GetType().Name == typeName) return true;
            }
            return false;
        }

        /// <summary>
        /// Retrieve the first (topmost) goal of type T on the stack, or null.
        /// Mirrors Qud's Brain.FindGoal pattern. Returns null if no matching goal exists.
        /// Scans top-down so the most recent goal of that type wins.
        /// </summary>
        public T FindGoal<T>() where T : GoalHandler
        {
            for (int i = _goals.Count - 1; i >= 0; i--)
            {
                if (_goals[i] is T typed) return typed;
            }
            return null;
        }

        /// <summary>
        /// Retrieve the first (topmost) goal whose class name equals typeName, or null.
        /// String variant — mirrors Qud's Brain.FindGoal(string) used by ModPsionic
        /// to find the Kill goal and insert Reequip above it.
        /// </summary>
        public GoalHandler FindGoal(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            for (int i = _goals.Count - 1; i >= 0; i--)
            {
                if (_goals[i].GetType().Name == typeName) return _goals[i];
            }
            return null;
        }

        /// <summary>
        /// True if the stack has any goal whose class name is NOT the given typeName.
        /// Useful for "act only if idle" checks that want to exclude a specific
        /// background goal (typically BoredGoal). Mirrors Qud's HasGoalOtherThan(name).
        /// </summary>
        public bool HasGoalOtherThan(string typeName)
        {
            for (int i = 0; i < _goals.Count; i++)
            {
                if (_goals[i].GetType().Name != typeName) return true;
            }
            return false;
        }

        // --- Event Handling ---

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "TakeTurn")
                return HandleTakeTurn();
            return true;
        }

        private bool HandleTakeTurn()
        {
            using (PerformanceMarkers.Turns.AiTakeTurn.Auto())
            {
                // Guard: no zone or not in zone (dead/removed)
                if (CurrentZone == null)
                {
                    EmitTurnSkipped("NoZone");
                    return true;
                }
                if (CurrentZone.GetEntityCell(ParentEntity) == null)
                {
                    EmitTurnSkipped("NotInZone");
                    return true;
                }

                // Skip turn when in conversation
                if (InConversation)
                {
                    EmitTurnSkipped("InConversation");
                    return true;
                }

                // Safety: skip player entities
                if (ParentEntity.HasTag("Player"))
                {
                    // No emission — player frame is not an "AI skip"; this
                    // path runs every frame and would flood the diag stream.
                    return true;
                }

                // Ensure RNG exists
                if (Rng == null) Rng = new Random();

                // Clear dead/removed target
                if (Target != null)
                {
                    if (CurrentZone.GetEntityCell(Target) == null)
                        Target = null;
                }

                // Set starting cell on first turn if not already set
                if (!HasStartingCell)
                {
                    var pos = CurrentZone.GetEntityPosition(ParentEntity);
                    if (pos.x >= 0)
                    {
                        StartingCellX = pos.x;
                        StartingCellY = pos.y;
                    }
                }

                // Clean finished goals from top of stack
                while (_goals.Count > 0 && _goals[_goals.Count - 1].Finished())
                {
                    var done = _goals[_goals.Count - 1];
                    _goals.RemoveAt(_goals.Count - 1);
                    done.OnPop();
                }

                // Ensure default goal exists
                if (_goals.Count == 0)
                    PushGoal(new BoredGoal());

                // Increment age on all goals
                for (int i = 0; i < _goals.Count; i++)
                    _goals[i].Age++;

                // Execute top goal — emit GoalSelected first so a debug
                // session asking "what goal did this NPC pick this turn?"
                // can resolve it without inspecting the stack.
                int stackSize = _goals.Count;
                var topGoal = _goals[stackSize - 1];
                if (Diag.IsChannelEnabled("ai"))
                {
                    Diag.Record(
                        category: "ai", kind: "GoalSelected",
                        actor: ParentEntity,
                        target: Target,
                        payload: new
                        {
                            goal = topGoal.GetType().Name,
                            details = topGoal.GetDetails(),
                            age = topGoal.Age,
                            stackDepth = stackSize,
                            hasTarget = Target != null,
                        });
                }
                topGoal.TakeAction();

                // Child-chain execution: if TakeAction pushed a child, execute it immediately.
                // This ensures BoredGoal -> KillGoal -> attack all happen in one tick.
                int depth = 0;
                while (_goals.Count > stackSize && depth < MaxChildChainDepth)
                {
                    depth++;
                    // Clean any immediately-finished goals
                    while (_goals.Count > 0 && _goals[_goals.Count - 1].Finished())
                    {
                        var done = _goals[_goals.Count - 1];
                        _goals.RemoveAt(_goals.Count - 1);
                        done.OnPop();
                    }

                    if (_goals.Count <= stackSize) break;

                    stackSize = _goals.Count;
                    _goals[stackSize - 1].TakeAction();
                }

                return true;
            }
        }
    }
}
