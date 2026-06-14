using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// RED scaffold for Challenge 1 — "Forge an Elemental Weapon".
    /// See Docs/PROGRAMMING-CHALLENGES.md §Challenge 1.
    ///
    /// Goes GREEN once you add a "VenomDagger" blueprint to
    /// Assets/Resources/Content/Blueprints/Objects.json that inherits
    /// "MeleeWeapon" and sets OnHitEffectsRaw = "Poisoned,50,1d4,6,0".
    /// No C# is needed for this challenge — it's pure content.
    /// </summary>
    public class VenomDaggerChallengeTests
    {
        private const string ExpectedSpec = "Poisoned,50,1d4,6,0";

        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            string path = Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(path));
        }

        [SetUp]
        public void Setup()
        {
            // Until the VenomDagger blueprint exists, CreateEntity logs an
            // "unknown blueprint" error. Ignore log-driven failures so the RED
            // is the clean assertion below — not Unity's unhandled-log failure.
            // (Harmless once the blueprint exists: no error is logged then.)
            LogAssert.ignoreFailingMessages = true;
        }

        [Test]
        public void VenomDagger_Exists_AndProcsPoisonOnHit()
        {
            Entity dagger = _factory.CreateEntity("VenomDagger");
            Assert.IsNotNull(dagger,
                "No 'VenomDagger' blueprint yet — add one to Objects.json (Challenge 1).");

            var weapon = dagger.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon,
                "VenomDagger should inherit \"MeleeWeapon\", giving it a MeleeWeaponPart.");
            Assert.AreEqual(ExpectedSpec, weapon.OnHitEffectsRaw,
                "VenomDagger's MeleeWeapon part needs OnHitEffectsRaw = \"" + ExpectedSpec + "\".");
        }

        // Counter-check (expected GREEN now and after): the stock Dagger must
        // NOT carry an on-hit proc — proves the spec is something you added,
        // not an accident inherited from the MeleeWeapon base.
        [Test]
        public void PlainDagger_HasNoOnHitProc()
        {
            Entity dagger = _factory.CreateEntity("Dagger");
            Assert.IsNotNull(dagger, "The stock 'Dagger' blueprint should already exist.");

            var weapon = dagger.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon);
            Assert.IsTrue(string.IsNullOrEmpty(weapon.OnHitEffectsRaw),
                "Plain Dagger should have an empty OnHitEffectsRaw.");
        }
    }
}
