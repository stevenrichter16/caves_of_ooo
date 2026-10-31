using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// P2 (Docs/PLACE-WORK-AND-PROGRESSION.md §3 rule 1): every creature's place on
    /// the curve. A creature's Level is authored inside its Tier's band, and its
    /// XPValue follows from that Level, so the two can never drift (before P2,
    /// XPValue was a hand-set number on 123 of 124 creatures and Level existed on one).
    /// </summary>
    public sealed class CreatureLevelTests
    {
        static EntityFactory factory;
        [OneTimeSetUp] public void Load()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        static IEnumerable<string> CreatureBlueprints()
        {
            foreach (var name in factory.Blueprints.Keys.OrderBy(n => n))
            {
                if (name == "Creature" || name == "Player") continue;
                Entity e; try { e = factory.CreateEntity(name); } catch { continue; }
                if (e != null && e.HasTag("Creature") && !e.HasTag("Player")) yield return name;
            }
        }

        [Test] public void EveryCreatureHasALevelInsideItsTierBand()
        {
            var missing = new List<string>();
            foreach (var name in CreatureBlueprints())
            {
                var e = factory.CreateEntity(name);
                int tier = ProgressionCurve.CreatureTier(e);
                var level = e.GetStat("Level");
                var band = ProgressionCurve.CreatureLevelBand(tier);
                if (level == null || level.BaseValue < band.Min || level.BaseValue > band.Max)
                    missing.Add(name + " (tier " + tier + ", level " + (level == null ? "none" : level.BaseValue.ToString()) + ")");
            }
            Assert.IsEmpty(missing, "creatures off the curve: " + string.Join(", ", missing));
        }

        [Test] public void XPValueFollowsLevelUnlessTheCreatureGivesNone()
        {
            var drift = new List<string>();
            foreach (var name in CreatureBlueprints())
            {
                var e = factory.CreateEntity(name);
                int level = e.GetStatValue("Level", 0), xp = e.GetStatValue("XPValue", 0);
                if (e.HasTag("NoXP")) { if (xp != 0) drift.Add(name + " is NoXP but pays " + xp); continue; }
                if (xp != ProgressionCurve.XPValueForLevel(level)) drift.Add(name + " L" + level + " pays " + xp + ", curve " + ProgressionCurve.XPValueForLevel(level));
            }
            Assert.IsEmpty(drift, string.Join("; ", drift));
        }

    }
}
