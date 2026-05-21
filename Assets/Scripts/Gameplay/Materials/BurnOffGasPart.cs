using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.9 — outgassing-on-fire. CoO port of Qud's
    /// <c>XRL.World.Parts.BurnOffGas</c> (qud BurnOffGas.cs:1-118).
    /// The INVERSE of the IObjectGasBehaviorPart family: where those
    /// make a gas cloud cause effects, this makes a flammable/volatile
    /// ENTITY emit gas when it takes fire damage. A peat bog venting
    /// methane when torched; a fungal pod puffing spores when burned.
    ///
    /// <para><b>Mechanic (Qud parity):</b> accumulate the Amount of any
    /// damage whose attributes match <see cref="DamageTriggerTypes"/>;
    /// each time the running total crosses <see cref="DamagePer"/>, roll
    /// <see cref="Chance"/>-in-100 and (on success) spawn
    /// <see cref="Number"/> copies of <see cref="GasId"/> at the entity's
    /// cell. A single large hit can cross the threshold multiple times
    /// (the while-loop drains it).</para>
    ///
    /// <para><b>Qud-divergence:</b> Qud hooks <c>BeforeTookDamage</c>;
    /// CoO hooks <c>TakeDamage</c> (POST-resistance, fires on the target
    /// only when damage actually lands). So outgassing is proportional
    /// to fire damage ACTUALLY taken — a fire-immune entity
    /// (HeatResistance 100) takes 0 and never outgasses. The Qud
    /// doc-comment's own wording ("takes a total of DamagePer ... damage")
    /// favors this reading.</para>
    ///
    /// <para><b>Zone resolution:</b> the TakeDamage event carries no
    /// Zone, so the spawn cell is resolved via
    /// <c>SettlementRuntime.ActiveZone</c> (the same fallback the G.8d.3
    /// contagion uses). Correct for CoO's single-active-zone combat.</para>
    /// </summary>
    public class BurnOffGasPart : Part
    {
        public override string Name => "BurnOffGas";

        /// <summary>Running total of trigger-type damage taken. Wraps via
        /// the threshold while-loop (never exceeds <see cref="DamagePer"/>
        /// after a cycle). Public for save round-trip.</summary>
        public int DamageTaken;

        /// <summary>Damage needed to trigger one spawn cycle.</summary>
        public int DamagePer = 10;

        /// <summary>Percent-in-100 chance to spawn per threshold crossing.</summary>
        public int Chance = 100;

        /// <summary>How many to spawn per successful cycle. Plain int OR a
        /// dice formula ("1d3-1"). Empty/garbage ⇒ 0 (graceful).</summary>
        public string Number = "1";

        /// <summary>Semicolon-delimited damage attribute tags that count
        /// toward the accumulator. Qud default "Heat;Fire".</summary>
        public string DamageTriggerTypes = "Heat;Fire";

        /// <summary>Gas to spawn. Empty ⇒ no-op (Qud parity:
        /// Blueprint.IsNullOrEmpty()).</summary>
        public string GasId = "";

        /// <summary>Density override; -1 ⇒ gas default.</summary>
        public int GasDensity = -1;

        /// <summary>Level override; -1 ⇒ gas default.</summary>
        public int GasLevel = -1;

        // Test-injected RNG for deterministic Chance + Number rolls.
        public static System.Random TestRng;
        private static readonly System.Random _defaultRng = new System.Random();

        public override bool HandleEvent(GameEvent e)
        {
            // CoO hooks TakeDamage (post-resistance) not Qud's
            // BeforeTookDamage — outgas ∝ fire damage ACTUALLY taken.
            if (e.ID != "TakeDamage") return true;

            var damage = e.GetParameter<Damage>("Damage");
            if (damage == null || damage.Amount <= 0) return true;

            // Only trigger-type damage counts. Every non-fire hit fires
            // TakeDamage, so a silent early-out here (no diag) is correct
            // — this isn't a rejected outgas, just an irrelevant event.
            string trigger = FirstMatchingTrigger(damage);
            if (trigger == null) return true;

            DamageTaken += damage.Amount;

            var rng = TestRng ?? _defaultRng;
            while (DamageTaken >= DamagePer)
            {
                // Drain BEFORE the chance/spawn gates (Qud parity): a
                // failed roll still consumes the threshold — the part
                // doesn't "save up" missed rolls.
                DamageTaken -= DamagePer;

                if (rng.Next(100) >= Chance)
                {
                    Diag.Record("gas", "BurnOffChanceFailed", ParentEntity, null,
                        new { gasId = GasId, chance = Chance, damagePer = DamagePer });
                    continue;
                }
                if (string.IsNullOrEmpty(GasId)) continue;

                // TakeDamage carries no Zone → resolve via ActiveZone
                // (same fallback as the G.8d.3 contagion path).
                var zone = SettlementRuntime.ActiveZone;
                if (zone == null) continue;
                var pos = zone.GetEntityPosition(ParentEntity);
                if (pos.x < 0) continue; // ParentEntity not in the active zone

                int count = RollNumber(Number, rng);
                if (count <= 0) continue;

                for (int i = 0; i < count; i++)
                    GasFactory.SpawnGas(zone, pos.x, pos.y, GasId,
                        density: GasDensity, level: GasLevel, creator: ParentEntity);

                if (ParentEntity != null)
                    MessageLog.Add(ParentEntity.GetDisplayName() + " burns off a cloud of gas.");

                Diag.Record("gas", "BurnOff", ParentEntity, null,
                    new { gasId = GasId, count, x = pos.x, y = pos.y,
                          damagePer = DamagePer, triggerAttribute = trigger });
            }
            return true;
        }

        /// <summary>First <see cref="DamageTriggerTypes"/> tag the damage
        /// carries, or null. Drives both the gate and the diag payload.</summary>
        private string FirstMatchingTrigger(Damage damage)
        {
            if (string.IsNullOrEmpty(DamageTriggerTypes)) return null;
            foreach (var raw in DamageTriggerTypes.Split(';'))
            {
                var t = raw.Trim();
                if (t.Length > 0 && damage.HasAttribute(t)) return t;
            }
            return null;
        }

        /// <summary>Roll the spawn count. CRITICAL: handles a plain int
        /// FIRST — <see cref="DiceRoller"/> requires the <c>NdM</c> form
        /// (regex <c>^(\d+)d(\d+)...</c>), so <c>Roll("1")</c> returns 0.
        /// Qud's <c>Number</c> default is the plain string "1"; without
        /// this int-first path a Number="1" part would spawn NOTHING.
        /// (Verification-sweep false premise — see GAS-SYSTEM-PLAN.md G.9.)</summary>
        private static int RollNumber(string spec, System.Random rng)
        {
            if (string.IsNullOrEmpty(spec)) return 0;
            spec = spec.Trim();
            if (int.TryParse(spec, out int flat)) return flat;
            return DiceRoller.Roll(spec, rng); // 0 on unparseable (graceful)
        }
    }
}
