using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.1.3 — registry for <see cref="IItemEnhancement"/>
    /// subclasses. Mirrors <c>SkillRegistry</c>'s shape: lazy-init,
    /// dict-based, case-insensitive lookup by class-name and
    /// display-name. Code-side registration only for v1; E.5+ may add
    /// JSON content-loading.
    ///
    /// <para><b>Auto-discovery (E.3.4):</b> <see cref="EnsureInitialized"/>
    /// scans the loaded assemblies once and registers every concrete
    /// <see cref="IItemEnhancement"/> subclass found. Production-side
    /// callers (e.g. <see cref="ItemEnhancing.Apply"/>, Tinker mod
    /// shims) call <see cref="EnsureInitialized"/> first; in tests,
    /// <see cref="ResetForTests"/> sets the initialized flag to true so
    /// auto-load is suppressed and the test opts into specific types.
    /// Pattern mirrors <c>TinkerRecipeRegistry.EnsureInitialized</c>
    /// (TinkerRecipeRegistry.cs:33-49).</para>
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

        /// <summary>Auto-discovery guard. Once set, <see cref="EnsureInitialized"/>
        /// is a no-op. Set to true on:
        /// <list type="bullet">
        ///   <item>First production-side <see cref="EnsureInitialized"/> call (auto-load runs)</item>
        ///   <item>Any <see cref="ResetForTests"/> call (auto-load suppressed for test isolation)</item>
        /// </list></summary>
        private static bool _initialized;

        /// <summary>If not yet initialized, scan loaded assemblies once
        /// and register every concrete <see cref="IItemEnhancement"/>
        /// subclass. Idempotent. Tests that need a blank registry call
        /// <see cref="ResetForTests"/> which sets the flag to true so
        /// this call is a no-op until the next <see cref="ResetForTests"/>.</summary>
        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            // Walk every loaded assembly and find concrete IItemEnhancement
            // subclasses. Defensive: skip assemblies that throw on type
            // enumeration (some Unity assemblies do).
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (var t in types)
                {
                    if (t == null) continue;
                    if (t.IsAbstract) continue;
                    if (!typeof(IItemEnhancement).IsAssignableFrom(t)) continue;
                    Register(t);
                }
            }
        }

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

        /// <summary>Test isolation — clears all registrations AND sets
        /// the <c>_initialized</c> flag to true so subsequent
        /// <see cref="EnsureInitialized"/> calls don't auto-load.
        /// Tests that want auto-discovery should call
        /// <see cref="ForceReinitialize"/> instead.</summary>
        public static void ResetForTests()
        {
            _byClassName.Clear();
            _byDisplayName.Clear();
            _initialized = true; // suppress auto-load — tests opt in explicitly
        }

        /// <summary>Test helper: clear registrations AND re-enable
        /// auto-discovery on next <see cref="EnsureInitialized"/> call.
        /// Used by tests that exercise the production auto-load path.</summary>
        public static void ForceReinitialize()
        {
            _byClassName.Clear();
            _byDisplayName.Clear();
            _initialized = false;
        }
    }
}
