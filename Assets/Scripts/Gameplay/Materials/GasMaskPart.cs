using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.6 — gas-mask defense Part. Direct port of Qud's
    /// <c>XRL.World.Parts.GasMask</c> (GasMask.cs:1-56) reduced to
    /// CoO's event vocabulary.
    ///
    /// <para><b>Two gates in one Part</b> (Qud parity):
    /// <list type="number">
    ///   <item><b>Intake reduction</b> — listens to
    ///         <c>GetRespiratoryPerformance</c> (the event G.5
    ///         introduced) and subtracts <see cref="Power"/> × 5 from
    ///         the "Intake" param. <c>Power=10</c> → intake -50;
    ///         <c>Power=20</c> → intake -100 → fully zeroed, which trips
    ///         the <c>ZeroIntake</c> veto in <see cref="GasPoisonPart"/>.</item>
    ///   <item><b>Damage scaling</b> — listens to <c>BeforeTakeDamage</c>;
    ///         if the incoming damage carries the "Gas" attribute,
    ///         multiplies the amount by <c>(100 - Power) / 100</c>. So a
    ///         mask whose intake-filter wasn't enough to fully veto still
    ///         reduces the damage that lands.</item>
    /// </list></para>
    ///
    /// <para><b>Where this Part lives.</b> For G.6 the Part attaches
    /// directly to a creature entity (the wearer). The equipment-time
    /// "equip a mask item → grant the wearer this Part" routing is the
    /// next layer (mirrors how <see cref="LightSourcePart"/> ships in
    /// LB.4 — the Part lives on the wearer, item equip-routing is the
    /// equipment system's concern).</para>
    /// </summary>
    public class GasMaskPart : Part
    {
        public override string Name => "GasMask";

        /// <summary>Mask quality. 10 = standard Qud mask; reduces intake
        /// by 50 and incoming gas damage by 10%. 20 = sealed hazmat:
        /// intake -100 (effectively immune via the ZeroIntake gate) and
        /// damage -20%.</summary>
        public int Power = 10;

        /// <summary>Per-event intake reduction = <see cref="Power"/> × 5.
        /// Qud parity (GasMask.cs:33).</summary>
        public const int INTAKE_PER_POWER = 5;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetRespiratoryPerformance")
            {
                int intake = e.GetParameter<int>("Intake");
                int reduction = Power * INTAKE_PER_POWER;
                int adjusted = intake - reduction;
                if (adjusted < 0) adjusted = 0;
                e.SetParameter("Intake", (object)adjusted);
                Diag.Record("gas", "MaskIntakeReduced", ParentEntity, null,
                    new { power = Power, intakeBefore = intake,
                          intakeAfter = adjusted, reduction });
                return true;
            }

            if (e.ID == "BeforeTakeDamage")
            {
                var damage = e.GetParameter<Damage>("Damage");
                if (damage == null || damage.Amount <= 0) return true;
                if (!damage.HasAttribute("Gas")) return true; // only gas damage
                int before = damage.Amount;
                int after = (before * (100 - Power)) / 100;
                if (after < 0) after = 0;
                damage.Amount = after;
                Diag.Record("gas", "MaskDamageReduced", ParentEntity, null,
                    new { power = Power, before, after, reduction = before - after });
                return true;
            }

            return true;
        }
    }
}
