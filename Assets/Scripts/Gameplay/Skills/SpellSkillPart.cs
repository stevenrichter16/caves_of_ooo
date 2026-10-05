using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Every active magic execution path, including direct scenario calls, captures one resolved sequence.</summary>
    public abstract class SpellSkillPart : BaseSkillPart
    {
        public sealed override bool OnCommand(SkillEventContext ctx)
        {
            using (var damageCast = SpellDamageHelpers.BeginCast(ctx?.Attacker))
            using (var capture = new SpellFxCapture(GetType().Name, ctx?.Zone, ctx?.Attacker))
            {
                bool cast = ResolveSpell(ctx);
                if (cast)
                {
                    damageCast.Commit();
                    capture.Commit();
                    if (ctx != null) ctx.BlocksTurnAdvance = true;
                }
                else if (ctx != null) ctx.BlocksTurnAdvance = false;
                return cast;
            }
        }
        protected abstract bool ResolveSpell(SkillEventContext ctx);
    }
}
