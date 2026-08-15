using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Declares an entity's material identity and properties.
    /// Used by the material interaction system to determine how entities
    /// respond to fire, acid, frost, electricity, etc.
    /// </summary>
    public class MaterialPart : Part
    {
        public override string Name => "Material";

        // Blueprint-configurable fields
        public string MaterialID = "Generic";

        /// <summary>
        /// ALL of these are on a <b>0-100</b> scale, matching how they are
        /// authored in Objects.json — oil is Combustibility 90, copper is
        /// Conductivity 100.
        ///
        /// <para>This was undocumented, and the codebase drifted into two
        /// conventions as a result. <c>TilePropagationSystem</c> and
        /// <c>LiquidCoveredEffect</c> read 0-100; the electricity chain
        /// here read 0-1 and multiplied charge by the raw value, turning
        /// every hop along a copper pipe into a hundredfold amplifier
        /// (a 2.0 source reached "Infinity" in play). A material-reaction
        /// blueprint had also been authored with MinConductivity 0.5,
        /// which on the real scale is a gate nothing could ever fail.
        ///
        /// If you add a consumer, treat these as STRENGTH inputs on the
        /// 0-100 scale (divide by 100) — never as a capability gate.
        /// Capability is answered by material TAGS via
        /// <see cref="ObjectStatusMatrix.IsConductiveMaterial"/>.</para>
        /// </summary>
        public float Combustibility = 0f;
        public float Conductivity = 0f;
        public float Porosity = 0f;
        public float Volatility = 0f;
        public float Brittleness = 0f;
        public string MaterialTagsRaw = "";

        // Parsed at runtime
        public HashSet<string> MaterialTags = new HashSet<string>();

        public override void Initialize()
        {
            ParseTags();
            ApplyTagsToEntity();
        }

        private void ParseTags()
        {
            MaterialTags.Clear();
            if (string.IsNullOrEmpty(MaterialTagsRaw))
                return;

            string[] parts = MaterialTagsRaw.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string tag = parts[i].Trim();
                if (tag.Length > 0)
                    MaterialTags.Add(tag);
            }
        }

        private void ApplyTagsToEntity()
        {
            if (ParentEntity == null)
                return;

            foreach (string tag in MaterialTags)
                ParentEntity.SetTag(tag);
        }

        public bool HasMaterialTag(string tag)
        {
            return MaterialTags.Contains(tag);
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "TryIgnite")
                return HandleTryIgnite(e);

            if (e.ID == "QueryMaterial")
                return HandleQueryMaterial(e);

            if (e.ID == "TryShatter")
                return HandleTryShatter(e);

            if (e.ID == "TryChainElectricity")
                return HandleTryChainElectricity(e);

            return true;
        }

        private bool HandleTryIgnite(GameEvent e)
        {
            if (Combustibility <= 0f)
            {
                e.SetParameter("Cancelled", true);
                return false;
            }
            return true;
        }

        private bool HandleQueryMaterial(GameEvent e)
        {
            e.SetParameter("MaterialID", MaterialID);
            e.SetParameter("MaterialTags", (object)MaterialTags);
            e.SetParameter("Combustibility", (object)Combustibility);
            e.SetParameter("Conductivity", (object)Conductivity);
            e.SetParameter("Porosity", (object)Porosity);
            e.SetParameter("Volatility", (object)Volatility);
            e.SetParameter("Brittleness", (object)Brittleness);
            return true;
        }

        /// <summary>
        /// A brittle material cracks under thermal or freeze shock. Low-brittleness
        /// materials ignore the shatter; high-brittleness materials lose HP or are
        /// destroyed outright.
        /// </summary>
        private bool HandleTryShatter(GameEvent e)
        {
            if (Brittleness <= 0.5f)
            {
                e.SetParameter("Cancelled", true);
                return true;
            }

            if (ParentEntity == null)
                return true;

            int maxHp = ParentEntity.GetStat("Hitpoints")?.Max ?? 0;
            int currentHp = ParentEntity.GetStatValue("Hitpoints", 0);

            // Catastrophic failure at very high brittleness: drop HP to zero.
            if (Brittleness >= 0.9f && currentHp > 0)
            {
                CombatSystem.ApplyDamage(ParentEntity, currentHp, null, null);
                MessageLog.Add(ParentEntity.GetDisplayName() + " shatters!");
                return true;
            }

            // Partial failure: take a percentage of max HP in damage.
            if (maxHp > 0 && currentHp > 0)
            {
                int damage = System.Math.Max(1, (int)(maxHp * (Brittleness - 0.5f)));
                CombatSystem.ApplyDamage(ParentEntity, damage, null, null);
                MessageLog.Add(ParentEntity.GetDisplayName() + " cracks under stress!");
            }
            return true;
        }

        /// <summary>
        /// ElectrifiedEffect fires this event each turn. Conductive materials
        /// (Conductivity > 0.5 or Metal tag) propagate the charge to adjacent
        /// conductive entities.
        /// </summary>
        /// <summary>
        /// Does this material carry a charge?
        ///
        /// <para>Threshold 50 on the authored 0-100 scale, matching
        /// <c>TilePropagationSystem.ConductiveThreshold</c>. The old test
        /// here was <c>Conductivity &gt; 0.5f</c>, which on a 0-100 scale
        /// means "anything above half a percent" — a rubber mat at 5
        /// counted as a conductor. Two systems disagreeing about what
        /// conducts is worse than either answer.</para>
        /// </summary>
        internal static bool IsConductiveMaterial(MaterialPart mat)
        {
            // Delegates to the matrix so the chain and the Electrified
            // row answer conduction from ONE vocabulary (Conductor /
            // Metal / Water tags). The numeric Conductivity field stays
            // a STRENGTH input (pass-through efficiency below), never a
            // capability gate — it is authored on two scales and already
            // bit twice (study §4 violator #4: Thunderclap could
            // electrify a WaterPuddle the chain then refused to pass
            // charge into; MetalGrate conducted at 100 with no tag).
            // Tile-side conduction keeps its own numeric threshold —
            // tile liquids are strings and have no tags to ask.
            return ObjectStatusMatrix.IsConductiveMaterial(mat);
        }

        private bool HandleTryChainElectricity(GameEvent e)
        {
            if (ParentEntity == null)
                return true;

            bool isConductor = IsConductiveMaterial(this);
            if (!isConductor)
                return true;

            var zone = e.GetParameter<Zone>("Zone");
            var source = e.GetParameter<Entity>("Source");
            float charge = e.GetParameter<float>("Charge");
            if (zone == null || charge <= 0f)
                return true;

            var sourceCell = zone.GetEntityCell(ParentEntity);
            if (sourceCell == null)
                return true;

            // Scale propagated charge by our own conductivity — better
            // conductors pass the charge on more effectively.
            //
            // CONDUCTIVITY IS AUTHORED 0-100, NOT 0-1. CopperGrate and
            // CopperPipe are both 100. This line used to multiply the
            // charge by that raw value, so every hop AMPLIFIED it a
            // hundredfold: a 2.0 source read 2E+16 after a few hops and
            // "Infinity" shortly after — reported from play once pipe
            // runs (lines of adjacent conductors) started generating.
            //
            // The other two readers of this field already use the 0-100
            // scale (TilePropagationSystem compares against 50,
            // LiquidCoveredEffect divides by 100); this was the odd one
            // out. Efficiency is capped at 1.0 because a transmission
            // line must never output more than it took in.
            float efficiency = Conductivity > 0f
                ? UnityEngine.Mathf.Clamp01(Conductivity / 100f)
                : 0.5f;
            float passCharge = charge * efficiency;
            if (passCharge < 0.05f)
                return true;

            for (int dir = 0; dir < 8; dir++)
            {
                var cell = zone.GetCellInDirection(sourceCell.X, sourceCell.Y, dir);
                if (cell == null)
                    continue;

                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var target = cell.Objects[i];
                    if (target == ParentEntity || target == source)
                        continue;

                    var mat = target.GetPart<MaterialPart>();
                    if (mat == null)
                        continue;

                    if (!IsConductiveMaterial(mat))
                        continue;

                    // Through the matrix door: with the vocabularies
                    // unified the verdicts agree, and refusals become
                    // diag-visible instead of silent.
                    if (!target.HasEffect<ElectrifiedEffect>())
                        ObjectStatusMatrix.TryApply(new ElectrifiedEffect(charge: passCharge),
                            target, source ?? ParentEntity, zone);
                }
            }
            return true;
        }
    }
}
