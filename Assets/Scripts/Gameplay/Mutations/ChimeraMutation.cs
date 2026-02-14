using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Morphotype mutation: restricts future mutation manifestations to physical mutations.
    /// </summary>
    public class ChimeraMutation : BaseMutation
    {
        private bool _applied;
        private bool _hadTag;
        private string _priorTagValue;
        private bool _hadMarkerProperty;
        private string _priorMarkerProperty;
        private bool _hadMutationLevelProperty;
        private string _priorMutationLevel;

        public override string Name => "Chimera";
        public override string MutationType => "Physical";
        public override string DisplayName => "Chimera";

        public override bool CanLevel()
        {
            return false;
        }

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ApplyMorphotypeMarkers(entity);
        }

        public override void Unmutate(Entity entity)
        {
            RemoveMorphotypeMarkers(entity);
            base.Unmutate(entity);
        }

        private void ApplyMorphotypeMarkers(Entity entity)
        {
            if (entity == null)
                return;

            if (!_applied)
            {
                _hadTag = entity.Tags.TryGetValue("Chimera", out _priorTagValue);
                _hadMarkerProperty = entity.Properties.TryGetValue("Chimera", out _priorMarkerProperty);
                _hadMutationLevelProperty = entity.Properties.TryGetValue("MutationLevel", out _priorMutationLevel);
                _applied = true;
            }

            entity.SetTag("Chimera");
            entity.Properties["Chimera"] = "";
            entity.Properties["MutationLevel"] = "Chimera";
        }

        private void RemoveMorphotypeMarkers(Entity entity)
        {
            if (!_applied || entity == null)
                return;

            if (_hadTag)
                entity.Tags["Chimera"] = _priorTagValue;
            else
                entity.Tags.Remove("Chimera");

            if (_hadMarkerProperty)
                entity.Properties["Chimera"] = _priorMarkerProperty;
            else
                entity.Properties.Remove("Chimera");

            if (_hadMutationLevelProperty)
                entity.Properties["MutationLevel"] = _priorMutationLevel;
            else if (string.Equals(entity.GetProperty("MutationLevel"), "Chimera", StringComparison.OrdinalIgnoreCase))
                entity.Properties.Remove("MutationLevel");

            _applied = false;
        }
    }
}
