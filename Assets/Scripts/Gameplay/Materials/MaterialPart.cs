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
            return true;
        }
    }
}
