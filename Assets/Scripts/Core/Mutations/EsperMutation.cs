using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Morphotype mutation: restricts future mutation manifestations to mental mutations.
    /// </summary>
    public class EsperMutation : BaseMutation
    {
        private bool _applied;
        private bool _hadTag;
        private string _priorTagValue;
        private bool _hadMarkerProperty;
        private string _priorMarkerProperty;
        private bool _hadMutationLevelProperty;
        private string _priorMutationLevel;

        public override string Name => "Esper";
        public override string MutationType => "Mental";
        public override string DisplayName => "Esper";

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
                _hadTag = entity.Tags.TryGetValue("Esper", out _priorTagValue);
                _hadMarkerProperty = entity.Properties.TryGetValue("Esper", out _priorMarkerProperty);
                _hadMutationLevelProperty = entity.Properties.TryGetValue("MutationLevel", out _priorMutationLevel);
                _applied = true;
            }

            entity.SetTag("Esper");
            entity.Properties["Esper"] = "";
            entity.Properties["MutationLevel"] = "Esper";
        }

        private void RemoveMorphotypeMarkers(Entity entity)
        {
            if (!_applied || entity == null)
                return;

            if (_hadTag)
                entity.Tags["Esper"] = _priorTagValue;
            else
                entity.Tags.Remove("Esper");

            if (_hadMarkerProperty)
                entity.Properties["Esper"] = _priorMarkerProperty;
            else
                entity.Properties.Remove("Esper");

            if (_hadMutationLevelProperty)
                entity.Properties["MutationLevel"] = _priorMutationLevel;
            else if (string.Equals(entity.GetProperty("MutationLevel"), "Esper", StringComparison.OrdinalIgnoreCase))
                entity.Properties.Remove("MutationLevel");

            _applied = false;
        }
    }
}
