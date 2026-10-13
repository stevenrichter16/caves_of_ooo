namespace CavesOfOoo.Core
{
    /// <summary>Two-owner-turn guard. CombatSystem spends it on one eligible would-hit strike;
    /// direct damage, spells and natural misses do not consume it.</summary>
    public sealed class EnGardeEffect : Effect
    {
        public override string DisplayName => "en garde";
        public bool Spent;
        public EnGardeEffect(int duration=2) { Duration=duration; }
        public override bool OnStack(Effect incoming) => incoming is EnGardeEffect;
    }
}
