namespace CavesOfOoo.Core
{
    /// <summary>
    /// Followers F.3.2 — companion-limit query. Static helper that asks
    /// "how many followers of <paramref name="Means"/> can this actor have?"
    /// by firing a <c>"GetCompanionLimit"</c> <see cref="GameEvent"/> on
    /// the actor and reading back the <c>Limit</c> param after all
    /// listeners have had a chance to bump it.
    ///
    /// <para><b>Qud parity:</b> mirrors
    /// <c>XRL.World/GetCompanionLimitEvent.cs</c>'s
    /// <c>GetFor(GameObject, string, int)</c>. CoO replaces Qud's
    /// per-event-type <c>PooledEvent&lt;T&gt;</c> with the existing
    /// <see cref="GameEvent"/> pool — same intent (reduce GC), idiomatic
    /// CoO shape (single class + string ID + dynamic param dicts). See
    /// <c>Docs/FOLLOWERS-F3.md §F.3.1 sweep</c> for the design choice.</para>
    ///
    /// <para><b>Listener convention:</b> any <see cref="Part"/> can
    /// contribute to the limit by overriding
    /// <see cref="Part.HandleEvent"/> and checking
    /// <list type="number">
    ///   <item><c>e.ID == "GetCompanionLimit"</c></item>
    ///   <item><c>e.GetStringParameter("Means") == </c> the means the
    ///         listener cares about (e.g. <c>"Recruit"</c>)</item>
    /// </list>
    /// If both match, the listener reads the current <c>Limit</c> via
    /// <c>e.GetIntParameter("Limit")</c> and writes back the bumped
    /// value via <c>e.SetParameter("Limit", current + N)</c>. Listeners
    /// MUST return <c>true</c> from HandleEvent (don't consume the
    /// event) so subsequent listeners can also contribute.</para>
    ///
    /// <para><b>Used by:</b> <see cref="CavesOfOoo.Skills.Persuasion_Recruit"/>
    /// adds <c>+1</c> for <c>Means == "Recruit"</c>. Future
    /// CompanionCapacity-equivalent items (F.5+ content) would
    /// contribute additional slots.</para>
    /// </summary>
    public static class GetCompanionLimitEvent
    {
        /// <summary>The <c>GameEvent.ID</c> the query fires.</summary>
        public const string EVENT_ID = "GetCompanionLimit";

        /// <summary>Param key carrying the means string
        /// (e.g. <c>"Recruit"</c>).</summary>
        public const string PARAM_MEANS = "Means";

        /// <summary>Param key carrying the running limit total.
        /// Listeners read it, add their contribution, and write it back.</summary>
        public const string PARAM_LIMIT = "Limit";

        /// <summary>Conventional means tag for the
        /// <see cref="CavesOfOoo.Skills.Persuasion_Recruit"/> path.</summary>
        public const string MEANS_RECRUIT = "Recruit";

        /// <summary>
        /// Query the actor's companion limit for the given means. Returns
        /// <paramref name="baseLimit"/> if the actor is null or has no
        /// listeners that bump for this means.
        /// </summary>
        public static int GetFor(Entity actor, string means, int baseLimit = 0)
        {
            if (actor == null) return baseLimit;

            var e = GameEvent.New(EVENT_ID);
            e.SetParameter(PARAM_MEANS, means ?? "");
            e.SetParameter(PARAM_LIMIT, baseLimit);

            actor.FireEvent(e);

            int result = e.GetIntParameter(PARAM_LIMIT, baseLimit);
            e.Release();
            return result;
        }
    }
}
