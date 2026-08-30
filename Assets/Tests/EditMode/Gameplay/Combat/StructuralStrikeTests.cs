using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The melee-bypass fix (deferred 🔴 from the W5.7 close-out).
    ///
    /// <para><b>The bug:</b> DestructionSystem.Damage never fires
    /// TakeDamage — only RouteDamage does — and BOTH player melee paths
    /// (bump-to-break and the Break world-action) called Damage
    /// directly. Every Part listening on the prop-damage seam was blind
    /// to swords: a player could chop down all ~51 hearth-patch tiles,
    /// the village's holiest object, for ZERO reputation, while a fire
    /// spell billed the war. The most obvious way to destroy the patch
    /// was the one path that was free.</para>
    ///
    /// <para><b>The fix:</b> a single seam — StrikeStructure — wraps
    /// the structural blow in a Bludgeoning-attributed Damage and sends
    /// it through RouteDamage. Both input-layer call sites route here.
    /// The widening is deliberate and these tests bound it: fire-gated
    /// listeners (BurnOffGasPart) must NOT react to a bludgeon, and the
    /// break/destroy semantics must not change.</para>
    /// </summary>
    public class StructuralStrikeTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            PlayerReputation.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            SettlementRuntime.Reset();
            PlayerReputation.Reset();
        }

        private static Entity MakeStriker(Zone zone, bool player = true)
        {
            var s = new Entity { ID = "striker", BlueprintName = "Striker" };
            s.Tags["Creature"] = "";
            if (player) s.Tags["Player"] = "";
            s.AddPart(new RenderPart { DisplayName = player ? "you" : "beast" });
            s.Statistics["Hitpoints"] = new Stat
            { Owner = s, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            zone.AddEntity(s, 11, 10);
            return s;
        }

        [Test]
        public void MeleeOnTheHearth_DeclaresTheWar()
        {
            // THE bug: swords were the one free path. A bare-handed
            // strike is a threat like any other.
            var zone = new Zone("StrikeWar");
            SettlementRuntime.ActiveZone = zone;
            FactionManager.Initialize();
            var patch = _factory.CreateEntity("HearthPatch");
            zone.AddEntity(patch, 10, 10);
            var you = MakeStriker(zone);

            DestructionSystem.StrikeStructure(patch, you, zone, new Random(7));

            Assert.AreEqual(PlayerReputation.Attitude.Hated,
                PlayerReputation.GetAttitude("CatacombFolk"),
                "threatening the patch is war, with a sword as with a torch");
        }

        [Test]
        public void ABeastsBlow_IsStillNotACrime()
        {
            // Counter-check: the Player gate lives in HearthPatchPart,
            // and the routed path must not erase it.
            var zone = new Zone("StrikeBeast");
            SettlementRuntime.ActiveZone = zone;
            FactionManager.Initialize();
            var patch = _factory.CreateEntity("HearthPatch");
            zone.AddEntity(patch, 10, 10);
            var beast = MakeStriker(zone, player: false);

            DestructionSystem.StrikeStructure(patch, beast, zone, new Random(7));

            Assert.AreEqual(0, PlayerReputation.Get("CatacombFolk"),
                "the village does not blame the walker for a beast");
        }

        [Test]
        public void AStrikeStillBreaksThings()
        {
            // Pin: destroy-at-zero survives the rerouting. A crate has
            // no TakeDamage listeners; hitting it must behave exactly
            // as the old direct path did — damage, then gone.
            var zone = new Zone("StrikeBreak");
            SettlementRuntime.ActiveZone = zone;
            var crate = _factory.CreateEntity("Crate");
            zone.AddEntity(crate, 10, 10);
            var you = MakeStriker(zone);

            for (int swing = 0; swing < 200; swing++)
            {
                if (zone.GetEntityPosition(crate).x < 0) break;
                DestructionSystem.StrikeStructure(crate, you, zone, new Random(swing));
            }
            Assert.Less(zone.GetEntityPosition(crate).x, 0,
                "two hundred swings; the crate is kindling");
        }

        [Test]
        public void AStrikeStillLandsAtLeastOne()
        {
            // Pin: the hardness floor ("a hit that connects always does
            // something") survives the rerouting.
            var zone = new Zone("StrikeHard");
            SettlementRuntime.ActiveZone = zone;
            var crate = _factory.CreateEntity("Crate");
            zone.AddEntity(crate, 10, 10);
            var you = MakeStriker(zone);
            var hp = crate.GetPart<DestructiblePart>();
            int before = hp.HP;

            DestructionSystem.StrikeStructure(crate, you, zone, new Random(7));

            Assert.Less(hp.HP, before, "the blow landed");
        }

        [Test]
        public void ABludgeon_DoesNotOutgasThePeat()
        {
            // The widening's boundary: BurnOffGasPart accumulates only
            // Heat/Fire-attributed damage. The strike's Bludgeoning
            // attribute must sail past the accumulator — a player
            // punching a peat bank must not vent methane.
            var zone = new Zone("StrikePeat");
            SettlementRuntime.ActiveZone = zone;
            var peat = new Entity { ID = "peat", BlueprintName = "PeatBank" };
            peat.AddPart(new RenderPart { DisplayName = "peat bank" });
            peat.AddPart(new DestructiblePart { HP = 40, MaxHP = 40 });
            var gas = new BurnOffGasPart { GasId = "MethaneGas", DamagePer = 5 };
            peat.AddPart(gas);
            zone.AddEntity(peat, 10, 10);
            var you = MakeStriker(zone);

            DestructionSystem.StrikeStructure(peat, you, zone, new Random(7));

            Assert.AreEqual(0, gas.DamageTaken,
                "a fist is not a fire; the accumulator never moves");
        }

        [Test]
        public void TheBlowIsStillNarrated()
        {
            // Pin: Damage()'s player-blow narration survives the route.
            var zone = new Zone("StrikeSay");
            SettlementRuntime.ActiveZone = zone;
            var crate = _factory.CreateEntity("Crate");
            zone.AddEntity(crate, 10, 10);
            var you = MakeStriker(zone);

            MessageLog.Clear();
            DestructionSystem.StrikeStructure(crate, you, zone, new Random(7));

            bool narrated = false;
            foreach (var line in MessageLog.GetMessages())
                if (line.Contains("strike") || line.Contains("break")) narrated = true;
            Assert.IsTrue(narrated, "one swing, one line");
        }
    }
}
