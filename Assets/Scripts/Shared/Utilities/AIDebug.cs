namespace CavesOfOoo.Diagnostics
{
    /// <summary>
    /// Runtime toggles for AI debug / introspection tooling. All default off
    /// so production builds render unchanged — the inspector contributes
    /// exactly zero work when <see cref="AIInspectorEnabled"/> is false.
    ///
    /// Toggle sources (all write the same static field):
    /// - Scenarios (e.g. <c>InspectAIGoals</c>) that set it in their Apply
    /// - Editor menu <c>Tools/Caves of Ooo/AI/Toggle Goal Inspector</c>
    ///   (editor-only, added in Commit 5 of Phase 10)
    /// - Tests toggling the flag per fixture (MUST reset in TearDown)
    ///
    /// Sibling of <see cref="PerformanceDiagnostics"/> — same namespace, same
    /// "static knob with no backing ScriptableObject" shape. We specifically
    /// do NOT introduce a settings asset: the toggle is runtime-only and
    /// transient, and a ScriptableObject would make test isolation harder.
    /// </summary>
    public static class AIDebug
    {
        /// <summary>
        /// When true, <c>LookQueryService</c> populates the goal-stack and
        /// last-thought fields on <c>LookSnapshot</c> for Creature-tagged
        /// entities with a <c>BrainPart</c>, and the sidebar renderer shows
        /// them. When false, both paths short-circuit and the inspector
        /// contributes zero work.
        ///
        /// Default: false. Production builds see no difference; dev/test
        /// sessions flip this on via the editor menu, a scenario, or a
        /// per-test setup.
        /// </summary>
        public static bool AIInspectorEnabled;
    }
}
