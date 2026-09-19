namespace CavesOfOoo.Core
{
    /// <summary>ER.1 (Docs/ENDING-ROUTES.md): the taproot's exposed face in the Root
    /// chamber. ER.2: it offers the routes' enactments (<see cref="EndingRoutes"/>)
    /// to the player beside it while no ending has been enacted.</summary>
    public sealed class RootFacePart : Part
    {
        public override string Name => "RootFace";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
                EndingRoutes.AddActions(e.GetParameter<InventoryActionList>("Actions"), CavesOfOoo.Storylets.StoryletPart.LocalPlayer);
            return true;
        }
    }
}
