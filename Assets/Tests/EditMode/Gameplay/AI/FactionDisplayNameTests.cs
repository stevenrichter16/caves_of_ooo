using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W0.3 of the Felling world overhaul — the display-name contract.
    ///
    /// <para>Canon renamed the archivist faction: the internal ID stays
    /// <c>Palimpsest</c> forever (it is baked into saves, blueprint
    /// tags, POI records and reputation keys), while the player is only
    /// ever shown <b>the Recension</b> — because "the Palimpsest" now
    /// means something else entirely, the cosmic under-text
    /// (<c>Lore/TERMS.md</c>).</para>
    ///
    /// <para>The verification sweep found that split shipped in data
    /// and prose with <b>zero test coverage</b> — nothing anywhere
    /// asserted any faction's display name, so the ID could have leaked
    /// back into the UI without a single red test. It had, in fact,
    /// already leaked: the dialogue portrait panel printed the raw ID.
    /// These are the pins.</para>
    /// </summary>
    public class FactionDisplayNameTests
    {
        [SetUp]
        public void SetUp()
        {
            string json = File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Factions.json"));
            FactionManager.Initialize(json);
        }

        [Test]
        public void Palimpsest_DisplaysAsTheRecension()
        {
            Assert.AreEqual("the Recension",
                FactionManager.GetDisplayName("Palimpsest"),
                "the ID is frozen for save-compat; the player sees the "
                + "canon name (Lore/TERMS.md)");
        }

        [Test]
        public void EveryShippedFaction_HasANonIdDisplayName()
        {
            // A DisplayName that merely echoes the ID is the failure
            // this whole split exists to prevent — it means the entry
            // was added without a player-facing name and GetDisplayName
            // silently fell back.
            foreach (string id in new[]
            {
                "RotChoir", "Palimpsest", "SaccharineConcord",
                "PaleCuration", "Villagers",
            })
            {
                string shown = FactionManager.GetDisplayName(id);
                Assert.IsNotEmpty(shown, id);
                Assert.AreNotEqual(id, shown,
                    $"{id} has no authored DisplayName — the player "
                    + "would be shown the raw internal ID");
            }
        }

        [Test]
        public void UnknownFaction_FallsBackToTheRawName()
        {
            // Counter-check: the fallback must be the input, not null
            // and not an exception. A Feelings-block typo should show
            // up as an odd row in the standings UI, not crash it.
            Assert.AreEqual("NotAFaction",
                FactionManager.GetDisplayName("NotAFaction"));
        }

        // ════════════════════════════════════════════════════════
        // The portrait panel's eight columns
        // ════════════════════════════════════════════════════════

        [Test]
        public void PortraitName_DropsTheArticleSoTheNameSurvives()
        {
            // The panel truncates at 8. "the Recension" would render as
            // "the Rece"; dropping the article renders "Recensio" —
            // still clipped, but recognisably the faction.
            Assert.AreEqual("Recension",
                DialogueUI.StripLeadingArticle("the Recension"));
            Assert.AreEqual("Saccharine Concord",
                DialogueUI.StripLeadingArticle("the Saccharine Concord"));
        }

        [Test]
        public void PortraitName_LeavesNamesWithoutAnArticleAlone()
        {
            // Counter-check: no over-eager prefix stripping.
            Assert.AreEqual("Theodore",
                DialogueUI.StripLeadingArticle("Theodore"));
            Assert.AreEqual("Villagers",
                DialogueUI.StripLeadingArticle("Villagers"));
            Assert.IsNull(DialogueUI.StripLeadingArticle(null));
        }
    }
}
