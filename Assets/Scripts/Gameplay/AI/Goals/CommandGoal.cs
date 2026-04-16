namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that fires a named GameEvent on the NPC, then pops immediately.
    /// Mirrors Qud's Command goal handler — used by behavior parts to trigger
    /// a single command-style action (e.g., "CommandSubmerge" for aquatic
    /// creatures returning to water).
    ///
    /// CanFight() returns false — combat won't interrupt mid-command. Since
    /// the goal pops immediately after firing the event, this only matters
    /// if the event handler itself pushes additional goals.
    /// </summary>
    public class CommandGoal : GoalHandler
    {
        public string Command;

        public CommandGoal(string command)
        {
            Command = command;
        }

        public override bool CanFight() => false;

        public override void TakeAction()
        {
            if (!string.IsNullOrEmpty(Command) && ParentEntity != null)
            {
                var e = GameEvent.New(Command);
                ParentEntity.FireEvent(e);
                e.Release();
            }
            Pop();
        }
    }
}
