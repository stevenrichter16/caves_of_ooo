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
        private bool HandleTryChainElectricity(GameEvent e)
        {
            if (ParentEntity == null)
                return true;

            bool isConductor = Conductivity > 0.5f || HasMaterialTag("Metal") || HasMaterialTag("Conductor");
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

            // Scale propagated charge by our own conductivity — better conductors
            // pass the charge on more effectively.
            float passCharge = charge * (Conductivity > 0f ? Conductivity : 0.5f);
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

                    bool targetConducts = mat.Conductivity > 0.5f
                        || mat.HasMaterialTag("Metal")
                        || mat.HasMaterialTag("Conductor");
                    if (!targetConducts)
                        continue;

                    if (!target.HasEffect<ElectrifiedEffect>())
                        target.ApplyEffect(new ElectrifiedEffect(charge: passCharge), source ?? ParentEntity, zone);
                }
            }
            return true;
        }
    }
}
