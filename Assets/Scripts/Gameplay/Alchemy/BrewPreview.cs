using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// What a mix of reagents would brew into, computed without consuming
    /// them.
    ///
    /// <para>This is the one that changes how alchemy plays. Today the
    /// player picks reagents blind and discovers the result after the
    /// ingredients are gone; the Crafting panel resolves the mix live, so
    /// adding a fourth reagent visibly changes the prediction before
    /// anything is spent (Docs/CRAFTING-FROM-THE-PACK.md §C1).</para>
    ///
    /// <para>Cheap to build on: <see cref="BrewResolver.Resolve"/> was
    /// already pure — it takes property lists and returns a
    /// <see cref="BrewResult"/>, touching no entity and no inventory. All
    /// this adds is the reagent-to-properties gather and a display name
    /// matching what <c>BrewingService</c> would stamp on the item.</para>
    /// </summary>
    public struct BrewPreview
    {
        /// <summary>True when the mix resolves to an actual brew. False
        /// for an empty mix, non-reagents, or a rule-rejected
        /// combination.</summary>
        public bool IsValid;

        /// <summary>Why not, when <see cref="IsValid"/> is false. Shown
        /// verbatim in the RESULT box.</summary>
        public string Reason;

        /// <summary>The name the produced item would carry.</summary>
        public string DisplayName;

        /// <summary>Tonic / Coating / Flask — drives what the brew can be
        /// used for, and worth showing since it is not obvious from the
        /// reagents.</summary>
        public string Form;

        /// <summary>Resolved effects with potencies, in rule order.</summary>
        public IReadOnlyList<BrewPropertyAmount> Effects;

        /// <summary>Flat "Healing:6 Poison:2" rendering, used for cheap
        /// equality checks and for the panel's property rows.</summary>
        public string Properties;
    }
}
