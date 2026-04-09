namespace CavesOfOoo.Core
{
    /// <summary>
    /// Interface for effects that provide a visual aura.
    /// Replaces hardcoded BurningEffect/PoisonedEffect checks in StatusEffectsPart.
    /// Any effect implementing this will automatically get aura start/stop calls.
    /// </summary>
    public interface IAuraProvider
    {
        AsciiFxTheme GetAuraTheme();
    }
}
