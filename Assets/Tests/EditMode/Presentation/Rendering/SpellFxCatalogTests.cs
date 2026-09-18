using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class SpellFxCatalogTests
    {
        [Test] public void RegisteredMagic_EveryCastAndRetortHasValidArt()
        {
            var castable = typeof(BaseSkillPart).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(BaseSkillPart).IsAssignableFrom(t))
                .Where(t => new[] {"Pyromancy_","Cryomancy_","Galvanism_","Hydromancy_","Corrosion_","Spellcraft_","Rites_"}.Any(p => t.Name.StartsWith(p)))
                .Where(t => ((BaseSkillPart)Activator.CreateInstance(t)).DeclareActivatedAbility(null) != null)
                .Select(t => t.Name).ToArray();
            Assert.AreEqual(49, castable.Length);
            CollectionAssert.IsEmpty(SpellFxCatalog.ValidateCoverage(castable));
            foreach (string id in castable)
            {
                Assert.IsTrue(SpellFxCatalog.TryGetDefinition(id, out var definition),id);
                Assert.IsTrue(SpellFxCatalog.TryGetAsset(definition,out var asset),id);
                Sprite frame = asset.Detail(SpellFxPrimitive.Head,0);
                Assert.AreEqual(16,frame.rect.width);
                Assert.AreEqual(16,frame.pixelsPerUnit);
                Assert.AreEqual(FilterMode.Point,frame.texture.filterMode);
                Assert.AreEqual(16,asset.Impact(0,4,false).rect.width);
                Assert.AreEqual(8,asset.Impact(0,0,false).rect.width);
            }
            Assert.AreEqual(1,SpellFxCatalog.ValidateCoverage(new[]{"Not_A_Registered_Spell"}).Count);
        }
        [Test] public void InvalidCatalog_RejectsDuplicatesFamiliesAndUnexplainedMissingArt()
        {
            var issues = SpellFxCatalog.ValidateJson("{\"Definitions\":[{\"ID\":\"test\",\"Family\":\"Unknown\"},{\"ID\":\"test\"}]}");
            Assert.IsTrue(issues.Any(s=>s.Contains("Duplicate")));
            Assert.IsTrue(issues.Any(s=>s.Contains("unknown Family")));
            Assert.IsTrue(issues.Any(s=>s.Contains("no explicit fallback")));
            CollectionAssert.IsEmpty(SpellFxCatalog.ValidateJson("{\"Definitions\":[{\"ID\":\"test\",\"Family\":\"Ward\",\"FallbackReason\":\"ASCII until art is authored\"}]}"));
            Assert.IsTrue(SpellFxCatalog.GetOrFallback("unknown").IsExplicitFallback);
        }
    }
}
