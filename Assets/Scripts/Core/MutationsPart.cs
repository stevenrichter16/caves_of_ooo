using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Container Part that manages an entity's mutations.
    /// Mirrors Qud's Mutations part: maintains a list of BaseMutation instances,
    /// handles adding/removing mutations with lifecycle callbacks,
    /// and can parse a StartingMutations string to auto-grant mutations on Initialize.
    /// </summary>
    public class MutationsPart : Part
    {
        public override string Name => "Mutations";

        /// <summary>
        /// All active mutations on this entity.
        /// </summary>
        public List<BaseMutation> MutationList = new List<BaseMutation>();

        /// <summary>
        /// Comma-separated string of starting mutations in format "ClassName:Level,ClassName:Level".
        /// Parsed during Initialize() to auto-grant mutations.
        /// Set via blueprint params.
        /// </summary>
        public string StartingMutations = "";

        /// <summary>
        /// Parse StartingMutations and grant them.
        /// Called automatically when the part is added to an entity.
        /// </summary>
        public override void Initialize()
        {
            if (string.IsNullOrEmpty(StartingMutations))
                return;

            string[] entries = StartingMutations.Split(',');
            foreach (string entry in entries)
            {
                string trimmed = entry.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                string className = trimmed;
                int level = 1;

                int colon = trimmed.IndexOf(':');
                if (colon >= 0)
                {
                    className = trimmed.Substring(0, colon);
                    int.TryParse(trimmed.Substring(colon + 1), out level);
                    if (level < 1) level = 1;
                }

                // Create mutation instance by class name via reflection
                BaseMutation mutation = CreateMutationByName(className);
                if (mutation != null)
                {
                    AddMutation(mutation, level);
                }
            }
        }

        /// <summary>
        /// Add a mutation to this entity at the given level.
        /// Attaches the mutation as a Part, calls Mutate lifecycle.
        /// </summary>
        public bool AddMutation(BaseMutation mutation, int level = 1)
        {
            if (mutation == null) return false;

            // Don't add duplicates
            for (int i = 0; i < MutationList.Count; i++)
            {
                if (MutationList[i].GetType() == mutation.GetType())
                    return false;
            }

            MutationList.Add(mutation);

            // Add as a Part on the entity (gives it ParentEntity, Initialize, HandleEvent)
            ParentEntity.AddPart(mutation);

            // Call mutation lifecycle
            mutation.Mutate(ParentEntity, level);

            return true;
        }

        /// <summary>
        /// Remove a mutation from this entity.
        /// Calls Unmutate lifecycle and removes the Part.
        /// </summary>
        public bool RemoveMutation(BaseMutation mutation)
        {
            if (mutation == null) return false;

            if (!MutationList.Remove(mutation))
                return false;

            // Call mutation lifecycle
            mutation.Unmutate(ParentEntity);

            // Remove as a Part
            ParentEntity.RemovePart(mutation);

            return true;
        }

        /// <summary>
        /// Get a mutation by type.
        /// </summary>
        public T GetMutation<T>() where T : BaseMutation
        {
            for (int i = 0; i < MutationList.Count; i++)
            {
                if (MutationList[i] is T typed)
                    return typed;
            }
            return null;
        }

        /// <summary>
        /// Check if the entity has a mutation of the given type.
        /// </summary>
        public bool HasMutation<T>() where T : BaseMutation
        {
            return GetMutation<T>() != null;
        }

        /// <summary>
        /// Create a mutation instance by class name using reflection.
        /// Searches the assembly for a matching BaseMutation subclass.
        /// </summary>
        private static BaseMutation CreateMutationByName(string className)
        {
            // Search all types in the assembly that contains BaseMutation
            var assembly = typeof(BaseMutation).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAbstract && typeof(BaseMutation).IsAssignableFrom(type) && type.Name == className)
                {
                    return (BaseMutation)Activator.CreateInstance(type);
                }
            }
            return null;
        }
    }
}
