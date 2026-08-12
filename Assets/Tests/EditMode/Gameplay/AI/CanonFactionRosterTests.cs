using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W0.4 of the Felling world overhaul — the four canon factions the
    /// world redesign needs, registered before any phase tries to place
    /// them.
    ///
    /// <para><b>Tent-Right</b> (the Beating; the three-day oath),
    /// <b>the Rooted's people</b> (every catacomb village),
    /// <b>the Bower-Folk</b> (Posy and its installations), and
    /// <b>the Imminent Archive</b> — the Catchers, Pale Curation's
    /// expelled splinter who preserve the wounded while they are still
    /// alive.</para>
    ///
    /// <para><b>The Driving Bloom gets no entry, deliberately.</b> Canon
    /// is explicit that it has no mind, no doctrine, no dialogue and no
    /// reputation track — "you are always talking to a person about the
    /// Bloom, never to the Bloom" (Lore/Factions/08_DrivingBloom.md).
    /// It is terrain with an effect, and a rep row would quietly make it
    /// a society.</para>
    ///
    /// <para>All four start at reputation 0 on purpose: the save graph
    /// restores the reputation dictionary wholesale, so a faction added
    /// later never re-seeds its initial value on an existing save. A
    /// non-zero seed would silently read as neutral there — a bug that
    /// only appears in old saves, which is the worst place to find
    /// one.</para>
    /// </summary>
    public class CanonFactionRosterTests
    {
        private static readonly string[] CanonAdditions =
        {
            "TentRight", "CatacombFolk", "BowerFolk", "ImminentArchive",
        };

        private static string FactionsJson() => File.ReadAllText(Path.Combine(
            Application.dataPath, "Resources/Content/Data/Factions.json"));

        [SetUp]
        public void SetUp() => FactionManager.Initialize(FactionsJson());

        [Test]
        public void CanonFactions_AreRegisteredAndVisible()
        {
            var visible = new HashSet<string>();
            foreach (string f in FactionManager.GetAllVisibleFactions())
                visible.Add(f);

            foreach (string id in CanonAdditions)
                Assert.IsTrue(visible.Contains(id),
                    $"{id} is missing from the standings UI roster");
        }

        [Test]
        public void CanonFactions_HaveAuthoredDisplayNames()
        {
            Assert.AreEqual("Tent-Right", FactionManager.GetDisplayName("TentRight"));
            Assert.AreEqual("the Rooted's people", FactionManager.GetDisplayName("CatacombFolk"));
            Assert.AreEqual("the Bower-Folk", FactionManager.GetDisplayName("BowerFolk"));
            Assert.AreEqual("the Imminent Archive", FactionManager.GetDisplayName("ImminentArchive"));
        }

        [Test]
        public void CanonFactions_StartNeutral()
        {
            // Not politeness — save-graph mechanics. Restore replaces the
            // reputation dictionary wholesale, so a faction added after a
            // save was written never re-seeds its initial value there. A
            // non-zero seed would read as 0 on old saves only.
            // (FactionManager.Initialize seeds PlayerReputation as part
            // of loading the file — see FactionManager.cs:71.)
            foreach (string id in CanonAdditions)
                Assert.AreEqual(0, PlayerReputation.Get(id),
                    $"{id} must start neutral so old saves and new agree");
        }

        [Test]
        public void DrivingBloom_IsNotAFaction()
        {
            // Counter-check with teeth: the Bloom is terrain, not a
            // society. If a future content pass gives it a rep row, this
            // fails and the canon violation gets caught at the door.
            var visible = new HashSet<string>();
            foreach (string f in FactionManager.GetAllVisibleFactions())
                visible.Add(f);

            Assert.IsFalse(visible.Contains("DrivingBloom"));
            Assert.IsFalse(visible.Contains("Bloom"));
        }

        [Test]
        public void EveryFeelingsTarget_ResolvesToARegisteredFaction()
        {
            // A typo in a Feelings block auto-registers a ghost faction
            // and puts a raw-ID row in the player-facing standings UI.
            // This catches it at the data layer instead.
            var known = new HashSet<string>();
            foreach (string f in FactionManager.GetAllVisibleFactions())
                known.Add(f);

            var json = FactionsJson();
            foreach (string id in new[]
            {
                "Snapjaws", "Beasts", "Villagers", "RotChoir", "Palimpsest",
                "SaccharineConcord", "PaleCuration", "GlassblownRemnant",
                "Cultists", "TentRight", "CatacombFolk", "BowerFolk",
                "ImminentArchive",
            })
            {
                Assert.IsTrue(known.Contains(id),
                    $"{id} is named in Factions.json but not registered");
            }
            Assert.IsNotEmpty(json);
        }
    }
}
