using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.1.3 — registry for <see cref="IItemEnhancement"/>
    /// subclasses. Mirrors <c>SkillRegistry</c>'s shape: lazy-init,
    /// dict-based, case-insensitive lookup by class-name and
    /// display-name. Code-side registration only for v1; E.5+ may add
    /// JSON content-loading.
    ///
    /// <para><b>Qud parity:</b> mirrors
    /// <c>/Users/steven/qud-decompiled-project/XRL.World/ModificationFactory.cs</c>.
    /// CoO simplifies away the Qud Tinker-bits cost field on ModEntry
    /// (deferred to E.5+).</para>
    /// </summary>
    public static class EnhancementFactory
    {
        // Two backing dicts — same shape as SkillRegistry's
        // _skillsByName / _skillsByClass split. Case-insensitive.
        private static readonly Dictionary<string, Type> _byClassName =
            new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Type> _byDisplayName =
            new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Register an enhancement type. Idempotent — re-registering
        /// the same type is a no-op. Null types and non-enhancement
        /// types are silently skipped (defense-in-depth; matches
        /// SkillRegistry's defensive filtering).</summary>
        public static void Register(Type enhancementType)
        {
            if (enhancementType == null) return;
            if (!typeof(IItemEnhancement).IsAssignableFrom(enhancementType)) return;
            if (enhancementType.IsAbstract) return;

            string className = enhancementType.Name;
            if (_byClassName.ContainsKey(className)) return; // idempotent

            _byClassName[className] = enhancementType;

            // Capture display name via a throwaway instance. Display name
            // can vary by tier, but at Tier=1 (the default ctor) it's
            // typically the canonical content-name. Failed instantiation
            // means no display-name entry; the by-class lookup still works.
            try
            {
                var instance = (IItemEnhancement)Activator.CreateInstance(enhancementType);
                string displayName = instance.GetDisplayName();
                if (!string.IsNullOrEmpty(displayName) && !_byDisplayName.ContainsKey(displayName))
                    _byDisplayName[displayName] = enhancementType;
            }
            catch { /* swallow — display-name registration is best-effort */ }
        }

        /// <summary>Look up an enhancement type by its class name (case-
        /// insensitive). Returns false if not registered.</summary>
        public static bool TryGet(string className, out Type enhancementType)
        {
            if (string.IsNullOrEmpty(className))
            {
                enhancementType = null;
                return false;
            }
            return _byClassName.TryGetValue(className, out enhancementType);
        }

        /// <summary>Look up an enhancement type by its display name (case-
        /// insensitive). Returns false if not registered.</summary>
        public static bool TryGetByDisplayName(string displayName, out Type enhancementType)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                enhancementType = null;
                return false;
            }
            return _byDisplayName.TryGetValue(displayName, out enhancementType);
        }

        /// <summary>Instantiate a registered enhancement by class name.
        /// Returns null if not registered or if instantiation fails.
        /// Tier defaults to 1. To customize, use the
        /// <see cref="Create(string, int)"/> overload.</summary>
        public static IItemEnhancement Create(string className)
        {
            return Create(className, tier: 1);
        }

        /// <summary>Instantiate a registered enhancement by class name
        /// AND call <see cref="IItemEnhancement.ApplyTier"/> with the
        /// supplied tier (per E.1 Lockdown #2 — tier scaling).</summary>
        public static IItemEnhancement Create(string className, int tier)
        {
            if (!TryGet(className, out Type type)) return null;
            try
            {
                var inst = (IItemEnhancement)Activator.CreateInstance(type);
                if (inst != null) inst.ApplyTier(tier);
                return inst;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Test isolation — clears all registrations. Mirrors
        /// <c>SkillRegistry.ResetForTests</c>.</summary>
        public static void ResetForTests()
        {
            _byClassName.Clear();
            _byDisplayName.Clear();
        }
    }
}
