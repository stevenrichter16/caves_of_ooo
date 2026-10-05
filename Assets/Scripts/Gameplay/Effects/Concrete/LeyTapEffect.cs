namespace CavesOfOoo.Core
{
    /// <summary>A saved positive charge, aged only by its owner's EndTurn.</summary>
    public sealed class LeyTapEffect : Effect
    {
        public override string DisplayName => "ley tap";
        public int BonusDamage;

        public LeyTapEffect(int bonusDamage, int duration = 3)
        { BonusDamage = System.Math.Max(0, bonusDamage); Duration = duration; }

        public override bool OnStack(Effect incoming)
        {
            if (!(incoming is LeyTapEffect ley)) return false;
            BonusDamage = ley.BonusDamage; Duration = ley.Duration;
            JustApplied = TurnManager.Active?.CurrentActor == Owner;
            return true;
        }
    }
}
