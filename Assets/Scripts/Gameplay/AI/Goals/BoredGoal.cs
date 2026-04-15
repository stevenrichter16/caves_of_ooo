namespace CavesOfOoo.Core
{
    /// <summary>
    /// Default bottom-of-stack goal. Scans for hostiles, wanders, or idles.
    /// Mirrors Qud's Bored goal handler.
    ///
    /// This is the decision-maker: it identifies what to do and pushes
    /// the appropriate child goal. The child-chain loop in BrainPart.HandleTakeTurn
    /// ensures the child executes in the same tick.
    /// </summary>
    public class BoredGoal : GoalHandler
    {
        public override bool IsBusy() => false;

        public override void TakeAction()
        {
            // Scan for hostiles
            Entity hostile = AIHelpers.FindNearestHostile(ParentEntity, CurrentZone, ParentBrain.SightRadius);
            if (hostile != null)
            {
                bool firstAggro = ParentBrain.Target == null;
                ParentBrain.Target = hostile;

                // Aggro indicator on first detection
                if (firstAggro)
                {
                    var myPos = CurrentZone.GetEntityPosition(ParentEntity);
                    if (myPos.x >= 0)
                        AsciiFxBus.EmitParticle(CurrentZone, myPos.x, myPos.y - 1, '!', "&R", 0.25f);
                }

                if (ShouldFlee())
                {
                    PushChildGoal(new FleeGoal(hostile));
                }
                else
                {
                    PushChildGoal(new KillGoal(hostile));
                }
                return;
            }

            // No hostile — wander or idle
            if (ParentBrain.Wanders && ParentBrain.WandersRandomly)
            {
                PushChildGoal(new WanderRandomlyGoal());
            }
            else
            {
                PushChildGoal(new WaitGoal(1));
            }
        }
    }
}
