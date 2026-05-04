using System;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Static state machine for the Scene View mode — full-screen ASCII
    /// scenes that temporarily replace the tilemap during atmospheric
    /// moments (e.g. "Look at campfire").
    ///
    /// Mirrors the <c>ConversationManager.IsActive</c> pattern: pure data,
    /// no rendering. Subscribers (SceneViewUI in M2, InputHandler) check
    /// <see cref="IsActive"/> and react accordingly.
    ///
    /// Plan: Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md M1.
    /// Visual spec: Docs/Mockups/scene-views/campfire.html.
    /// </summary>
    public static class SceneViewManager
    {
        /// <summary>
        /// Identifier of the active scene (e.g. "Campfire"), or null if no
        /// scene is open. The <see cref="SceneViewUI"/> (M2) resolves this
        /// to a <c>SceneViewData</c> asset and renders accordingly.
        /// </summary>
        public static string ActiveSceneID { get; private set; }

        public static bool IsActive => !string.IsNullOrEmpty(ActiveSceneID);

        /// <summary>
        /// Fired when a scene becomes active. Argument is the scene ID.
        /// Subscribers: SceneViewUI (toggles ZoneRenderer.Paused, starts
        /// the renderer's frame loop), InputHandler (transitions to
        /// SceneOpen input state).
        /// </summary>
        public static event Action<string> OnActivated;

        /// <summary>
        /// Fired when the active scene is deactivated. Subscribers reverse
        /// whatever they did on activation.
        /// </summary>
        public static event Action OnDeactivated;

        /// <summary>
        /// Open a scene by ID. Replaces any currently-active scene.
        /// Null or empty IDs are silently ignored — no event fired,
        /// no state change.
        /// </summary>
        public static void Activate(string sceneID)
        {
            if (string.IsNullOrEmpty(sceneID)) return;
            ActiveSceneID = sceneID;
            OnActivated?.Invoke(sceneID);
        }

        /// <summary>
        /// Close the active scene. Safe to call when no scene is active
        /// (no-op, no event).
        /// </summary>
        public static void Deactivate()
        {
            if (!IsActive) return;
            ActiveSceneID = null;
            OnDeactivated?.Invoke();
        }

        /// <summary>
        /// Test/load-game hook: clear all state and event subscribers.
        /// Call from test SetUp/TearDown and from save-load to prevent
        /// stale subscriptions across game sessions.
        /// </summary>
        public static void Reset()
        {
            ActiveSceneID = null;
            OnActivated = null;
            OnDeactivated = null;
        }
    }
}
