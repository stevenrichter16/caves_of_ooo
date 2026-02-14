namespace CavesOfOoo.Core
{
    /// <summary>
    /// Abstract base for all entity components.
    /// Mirrors Qud's IPart: parts are the building blocks of entity behavior.
    /// Each Part can register interest in events via WantEvent() and respond via HandleEvent().
    /// </summary>
    public abstract class Part
    {
        /// <summary>
        /// The entity this part is attached to.
        /// </summary>
        public Entity ParentEntity;

        /// <summary>
        /// The blueprint name of this part type (e.g. "Render", "Physics").
        /// Defaults to the class name.
        /// </summary>
        public virtual string Name => GetType().Name;

        /// <summary>
        /// Called when this part is first added to an entity.
        /// Override to perform initialization.
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Called when this part is removed from its entity.
        /// Override to perform cleanup.
        /// </summary>
        public virtual void Remove() { }

        /// <summary>
        /// Called during event registration phase. Return true if this part
        /// wants to receive the given event ID.
        /// This is an optimization: parts that don't register for an event won't have
        /// HandleEvent called for it.
        /// </summary>
        public virtual bool WantEvent(int eventID)
        {
            return false;
        }

        /// <summary>
        /// Handle an event fired on the parent entity.
        /// Return true to allow the event to continue propagating.
        /// Return false to stop propagation (event is "handled").
        /// </summary>
        public virtual bool HandleEvent(GameEvent e)
        {
            return true;
        }

        /// <summary>
        /// Fire an event on the parent entity. Convenience method.
        /// </summary>
        protected bool FireEvent(GameEvent e)
        {
            return ParentEntity?.FireEvent(e) ?? true;
        }

        /// <summary>
        /// Get another part on the same entity.
        /// </summary>
        protected T GetPart<T>() where T : Part
        {
            return ParentEntity?.GetPart<T>();
        }
    }
}
