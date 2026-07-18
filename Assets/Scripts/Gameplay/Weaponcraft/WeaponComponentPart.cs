namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marks an item as a weapon-forging component and carries its
    /// contribution to the assembled weapon (Docs/CRAFTING-ALCHEMY-SYSTEM.md
    /// §7.1 Layer 1). Blueprint-authorable like ReagentPart: part name
    /// "WeaponComponent" resolves via EntityFactory's name+"Part" convention.
    ///
    /// Slot vocabulary: "Blade" (drives damage dice + damage-class
    /// attribute), "Haft" (drives strength cap + handling), "Binding"
    /// (drives pen/hit bonuses + quirk). One component of each slot forges
    /// one weapon; contributions are combined by
    /// WeaponForgingService.ComputeStats.
    /// </summary>
    public class WeaponComponentPart : Part
    {
        public override string Name => "WeaponComponent";

        /// <summary>"Blade" | "Haft" | "Binding".</summary>
        public string Slot = "";

        /// <summary>Dice contributed to MeleeWeaponPart.BaseDamage. Only the Blade's is used.</summary>
        public string BaseDamage = "";

        /// <summary>Added to MeleeWeaponPart.PenBonus (summed across components).</summary>
        public int PenBonus = 0;

        /// <summary>Added to MeleeWeaponPart.HitBonus (summed across components).</summary>
        public int HitBonus = 0;

        /// <summary>
        /// Contribution to MeleeWeaponPart.MaxStrengthBonus — the MAX across
        /// components wins (a better haft raises the cap; a worse blade
        /// doesn't drag it down). -1 = contributes nothing.
        /// </summary>
        public int MaxStrengthBonus = -1;

        /// <summary>Space-delimited weapon attributes to union in (e.g. "Cutting LongBlades").</summary>
        public string Attributes = "";

        /// <summary>
        /// Optional on-hit quirk in MeleeWeaponPart.OnHitEffectsRaw grammar
        /// ("EffectName,ChancePercent,DamageDice,DurationTurns,Magnitude").
        /// Quirks from all components are joined with ';'.
        /// </summary>
        public string OnHitEffectSpec = "";

        /// <summary>
        /// Fragment used to build the forged weapon's display name.
        /// Assembly order: haft fragment, binding fragment, blade fragment
        /// (e.g. "long-hafted" + "serrated" + "steel blade").
        /// </summary>
        public string NameFragment = "";
    }
}
