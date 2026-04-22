using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Abstract base for all AI goal handlers. Goals live on BrainPart's goal stack.
    /// Mirrors Qud's GoalHandler: a LIFO stack of composable behaviors.
    ///
    /// Each tick, the brain pops finished goals, then calls TakeAction() on the top goal.
    /// Goals can push child goals (sub-tasks) and fail back to their parent.
    /// </summary>
    public abstract class GoalHandler
    {
        /// <summary>Ticks since this goal was pushed.</summary>
        public int Age;

        /// <summary>The brain that owns this goal's stack.</summary>
        public BrainPart ParentBrain;

        /// <summary>The goal that pushed this as a child (null for root goals).</summary>
        public GoalHandler ParentHandler;

        // --- Core virtuals ---

        /// <summary>Execute this goal's behavior for one tick.</summary>
        public abstract void TakeAction();

        /// <summary>Return true when this goal is complete and should be popped.</summary>
        public virtual bool Finished() => false;

        /// <summary>Can combat interrupt this goal? Default: true.</summary>
        public virtual bool CanFight() => true;

        /// <summary>Is the NPC busy with this goal? Default: true. Bored/Wander return false.</summary>
        public virtual bool IsBusy() => true;

        /// <summary>Called when this goal is pushed onto the stack.</summary>
        public virtual void OnPush() { }

        /// <summary>Called when this goal is popped off the stack.</summary>
        public virtual void OnPop() { }

        /// <summary>Called when a child goal fails back to this goal.</summary>
        public virtual void Failed(GoalHandler child) { }

        // --- Composition ---

        /// <summary>Push a child goal on the brain's stack, tracking this as its parent.</summary>
        public void PushChildGoal(GoalHandler child)
        {
            child.ParentHandler = this;
            ParentBrain.PushGoal(child);
        }

        /// <summary>Remove this goal from the stack.</summary>
        public void Pop()
        {
            ParentBrain?.RemoveGoal(this);
        }

        /// <summary>Pop this goal and notify the parent that we failed.</summary>
        public void FailToParent()
        {
            var parent = ParentHandler;
            Pop();
            parent?.Failed(this);
        }

        // --- Convenience accessors ---

        protected Entity ParentEntity => ParentBrain?.ParentEntity;

        protected Zone CurrentZone => ParentBrain?.CurrentZone;

        protected Random Rng => ParentBrain?.Rng ?? _fallbackRng ?? (_fallbackRng = new Random());
        private static Random _fallbackRng;

        // --- Debug ---

        /// <summary>
        /// Record a debug thought on the parent brain. Shim for
        /// <see cref="BrainPart.Think"/> so goals can write
        /// <c>Think("walking to bone")</c> without a ParentBrain.null-check at
        /// every call site. Mirrors Qud's <c>GoalHandler.Think(string)</c>.
        /// </summary>
        protected void Think(string thought) => ParentBrain?.Think(thought);

        /// <summary>
        /// One-liner shown in the AI goal-stack inspector UI. Default shape:
        ///   <c>"TypeName"</c>            — when <see cref="GetDetails"/> is null
        ///   <c>"TypeName: details"</c>   — when <see cref="GetDetails"/> returns a string
        ///
        /// Override <see cref="GetDetails"/> for state-specific strings; only
        /// override this method directly if the <c>"Type: Details"</c> shape
        /// is wrong for your goal (e.g. you want the type name suppressed).
        /// Non-abstract so unimplemented subclasses still produce readable
        /// output — mirrors Qud's <c>GoalHandler.GetDescription</c>.
        /// </summary>
        public virtual string GetDescription()
        {
            string details = GetDetails();
            string typeName = GetType().Name;
            return string.IsNullOrEmpty(details) ? typeName : $"{typeName}: {details}";
        }

        /// <summary>
        /// State summary for this goal (target name, phase, counter values).
        /// Default: null → inspector shows just the type name. Override to
        /// surface interesting runtime state. Format convention: single line,
        /// fields joined with <c>" | "</c>, no trailing punctuation.
        ///
        /// Examples of good overrides:
        ///   "target=Snapjaw"
        ///   "phase=Pickup | attempts=1/2 | item=Bone"
        ///   "to=(44,11) age=3/100"
        ///
        /// Examples to AVOID (too verbose / multi-line / unstable reprs):
        ///   "{Target=Snapjaw, Pos=[44,11]}"      // object-dump style
        ///   "target: Snapjaw\nphase: Pickup"     // multi-line
        ///
        /// Mirrors Qud's <c>GoalHandler.GetDetails</c>.
        /// </summary>
        public virtual string GetDetails() => null;

        // --- Shared helpers ---

        /// <summary>Check if the entity should flee based on HP and FleeThreshold.</summary>
        protected bool ShouldFlee()
        {
            if (ParentBrain == null || ParentEntity == null) return false;
            int hp = ParentEntity.GetStatValue("Hitpoints", 1);
            int maxHp = ParentEntity.GetStat("Hitpoints")?.Max ?? 1;
            return hp > 0 && maxHp > 0 && (float)hp / maxHp < ParentBrain.FleeThreshold;
        }
    }
}
