namespace CavesOfOoo.Core
{
    public class PaperSkinEffect: Effect
    {
        public override string DisplayName => "paperskin";

        public int Increase;
        
        public PaperSkinEffect(int increase = 5, int duration = 5)
        {
            Increase = increase;
            Duration = duration;
        }
        
        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + "'s skin thins to paper.");
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + "'s skin thickens.");
        }

        public override void OnBeforeTakeDamage(Entity target, GameEvent e)
        {
            if (e.GetParameter("Damage") is Damage damage)
            {
                damage.Amount += Increase;
                MessageLog.Add("Original damage: " + damage.Amount);
                MessageLog.Add($"Damage increased from {damage.Amount} to {damage.Amount + Increase}.");
            }
            
        }
    }
}