using CavesOfOoo.Rendering;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Part that contributes a "Look" action to the inventory action list
    /// (key 'l'). When the action is selected, calls
    /// <see cref="SceneViewManager.Activate(string)"/> with the configured
    /// <see cref="SceneID"/>, opening the corresponding full-screen Scene
    /// View overlay (e.g. the campfire scene).
    ///
    /// Mirrors <c>ConversationPart</c>'s two-stage HandleEvent dispatch:
    /// declare on <c>GetInventoryActions</c>, execute on <c>InventoryAction</c>
    /// with matching command. UI activation (the actual dissolve-in render
    /// of the scene) is the SceneViewUI's job, which subscribes to
    /// <c>SceneViewManager.OnActivated</c>.
    ///
    /// Plan: Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md M6.
    /// </summary>
    public class LookAtScenePart : Part
    {
        public override string Name => "LookAtScene";

        /// <summary>
        /// The Scene View ID to open when the action is selected. Must match
        /// a scene the SceneViewUI knows how to render (currently only
        /// "Campfire"; M5's SceneViewData asset format will register more).
        /// Set from blueprint params.
        /// </summary>
        public string SceneID;

        // Action wiring constants — kept stable so the InputHandler's
        // command-dispatch stays in sync and blueprint authors don't have
        // to chase magic strings. Note: unlike ConversationPart (where
        // Name=="Chat" and Command=="Chat" coincide), here Name=="Look"
        // (the user-facing label) is distinct from Command=="LookAtScene"
        // (the internal dispatch id) — "Look" is too generic a verb to
        // reserve globally; "LookAtScene" disambiguates from any future
        // examine/look actions on items, terrain, etc.
        private const string ACTION_COMMAND = "LookAtScene";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                // Hotkey 'l' = Look; priority 10 matches Chat (the other
                // primary "interact and pause" verb). Display "look at fire"
                // is wired here for the Campfire case; M5 will source the
                // display label from SceneViewData so other scenes can read
                // "look at vista", etc.
                actions?.AddAction("Look", "look at fire", ACTION_COMMAND, 'l', 10);
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command == ACTION_COMMAND)
                {
                    if (!string.IsNullOrEmpty(SceneID))
                    {
                        SceneViewManager.Activate(SceneID);
                    }
                    e.Handled = true;
                    return false;
                }
            }

            return true;
        }
    }
}
