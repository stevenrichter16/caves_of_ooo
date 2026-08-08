using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ALPHA-READINESS item 4 — strip-debug (P0, verified). Normal play
    /// shipped as a dev sandbox: 8 free attack mutations, free
    /// weapons/NPC at spawn, ALL tinkering recipes + bits, a 19-stack
    /// crafting kit + 8 tonics, live F7/F8/F9/P debug keys (F8
    /// permanently dismembers the player), and barrel demos in every
    /// village. Everything now sits behind DevMode.Enabled
    /// (default OFF), and a small designed loadout takes its place.
    /// </summary>
    [TestFixture]
    public class AlphaStripDebugTests
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
        public void Setup() => MessageLog.Clear();

        [Test]
        public void DevMode_ShipsDisabled()
        {
            Assert.IsFalse(DevMode.Enabled,
                "the shipping default is a designed game, not a dev sandbox");
        }

        // ── SM3: the designed starter loadout ────────────────────

        [Test]
        public void NewGameLoadout_IsTheDesignedKit_Exactly()
        {
            // The kit is deliberately small: a sidearm, heals, food.
            // Spells come from grimoires, seeds from the farming kit,
            // everything else from play.
            var expected = new (string, int)[] {
                ("Dagger", 1), ("HealingTonic", 2), ("DriedMeat", 2),
            };
            Assert.AreEqual(expected.Length, NewGameLoadout.Items.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i].Item1, NewGameLoadout.Items[i].Blueprint);
                Assert.AreEqual(expected[i].Item2, NewGameLoadout.Items[i].Count);
            }
        }

        [Test]
        public void NewGameLoadout_Grant_FillsInventoryStackAware()
        {
            var player = new Entity { ID = "p", BlueprintName = "Player" };
            player.AddPart(new InventoryPart { MaxWeight = 500 });

            int granted = NewGameLoadout.Grant(player, _factory);

            Assert.AreEqual(NewGameLoadout.Items.Length, granted);
            var inv = player.GetPart<InventoryPart>();
            bool hasDagger = false; int tonics = 0; int meat = 0;
            foreach (var item in inv.Objects)
            {
                var stack = item.GetPart<StackerPart>();
                int n = stack != null ? stack.StackCount : 1;
                if (item.BlueprintName == "Dagger") hasDagger = true;
                if (item.BlueprintName == "HealingTonic") tonics += n;
                if (item.BlueprintName == "DriedMeat") meat += n;
            }
            Assert.IsTrue(hasDagger, "sidearm");
            Assert.AreEqual(2, tonics, "two heals");
            Assert.AreEqual(2, meat, "two food");
        }

        [Test]
        public void NewGameLoadout_Grant_NullSafe()
        {
            Assert.AreEqual(0, NewGameLoadout.Grant(null, _factory));
            var bare = new Entity { ID = "b" }; // no inventory part
            Assert.AreEqual(0, NewGameLoadout.Grant(bare, _factory));
        }

        // ── SM1: debug keys dead when DevMode off ────────────────

        [Test]
        public void DebugDismember_IsNoOp_WhenDevModeOff()
        {
            bool saved = DevMode.Enabled;
            var go = new GameObject("ih-test");
            try
            {
                DevMode.Enabled = false;
                var ih = go.AddComponent<InputHandler>();
                var player = _factory.CreateEntity("Player");
                ih.PlayerEntity = player;
                var body = player.GetPart<Body>();
                int partsBefore = body.GetBody().GetParts().Count;

                typeof(InputHandler).GetMethod("TryDebugDismember",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ih, null);

                Assert.AreEqual(partsBefore, body.GetBody().GetParts().Count,
                    "F8 must never sever the player's limbs in normal play");
            }
            finally
            {
                DevMode.Enabled = saved;
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DebugDismember_StillWorks_WhenDevModeOn()
        {
            // Counter-check: dev scenarios keep the tool.
            bool saved = DevMode.Enabled;
            var go = new GameObject("ih-test2");
            try
            {
                DevMode.Enabled = true;
                var ih = go.AddComponent<InputHandler>();
                var player = _factory.CreateEntity("Player");
                ih.PlayerEntity = player;
                var body = player.GetPart<Body>();
                int partsBefore = body.GetBody().GetParts().Count;

                typeof(InputHandler).GetMethod("TryDebugDismember",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ih, null);

                Assert.Less(body.GetBody().GetParts().Count, partsBefore,
                    "with DevMode on, the debug dismember tool still functions");
            }
            finally
            {
                DevMode.Enabled = saved;
                Object.DestroyImmediate(go);
            }
        }
    }
}
