using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public class EntitySystemTests
    {
        // ========================
        // Entity + Part basics
        // ========================

        [Test]
        public void Entity_AddPart_SetParent()
        {
            var entity = new Entity();
            var render = new RenderPart { DisplayName = "test" };
            entity.AddPart(render);

            Assert.AreEqual(entity, render.ParentEntity);
            Assert.AreEqual(1, entity.Parts.Count);
        }

        [Test]
        public void Entity_GetPart_ByType()
        {
            var entity = new Entity();
            entity.AddPart(new RenderPart { DisplayName = "sword" });

            var found = entity.GetPart<RenderPart>();
            Assert.IsNotNull(found);
            Assert.AreEqual("sword", found.DisplayName);
        }

        [Test]
        public void Entity_GetPart_ByName()
        {
            var entity = new Entity();
            entity.AddPart(new RenderPart { DisplayName = "axe" });

            var found = entity.GetPart("Render");
            Assert.IsNotNull(found);
            Assert.IsInstanceOf<RenderPart>(found);
        }

        [Test]
        public void Entity_RemovePart_ClearsParent()
        {
            var entity = new Entity();
            var render = new RenderPart();
            entity.AddPart(render);
            entity.RemovePart(render);

            Assert.IsNull(render.ParentEntity);
            Assert.AreEqual(0, entity.Parts.Count);
        }

        [Test]
        public void Entity_HasPart_ReturnsTrueWhenPresent()
        {
            var entity = new Entity();
            entity.AddPart(new RenderPart());

            Assert.IsTrue(entity.HasPart<RenderPart>());
            Assert.IsTrue(entity.HasPart("Render"));
        }

        // ========================
        // Stats
        // ========================

        [Test]
        public void Stat_Value_ComputedCorrectly()
        {
            var stat = new Stat
            {
                Name = "Hitpoints",
                BaseValue = 20,
                Bonus = 5,
                Penalty = 3,
                Boost = 2,
                Min = 0,
                Max = 100
            };

            // Value = 20 + 5 - 3 + 2 = 24
            Assert.AreEqual(24, stat.Value);
        }

        [Test]
        public void Stat_Value_ClampedToMinMax()
        {
            var stat = new Stat
            {
                Name = "HP",
                BaseValue = -10,
                Min = 0,
                Max = 50
            };
            Assert.AreEqual(0, stat.Value);

            stat.BaseValue = 100;
            Assert.AreEqual(50, stat.Value);
        }

        [Test]
        public void Entity_Stats_GetAndSet()
        {
            var entity = new Entity();
            entity.Statistics["Strength"] = new Stat
            {
                Name = "Strength",
                BaseValue = 16,
                Min = 1,
                Max = 50,
                Owner = entity
            };

            Assert.AreEqual(16, entity.GetStatValue("Strength"));

            entity.SetStatValue("Strength", 20);
            Assert.AreEqual(20, entity.GetStatValue("Strength"));
        }

        // ========================
        // Tags
        // ========================

        [Test]
        public void Entity_Tags_SetAndGet()
        {
            var entity = new Entity();
            entity.SetTag("Creature");
            entity.SetTag("Faction", "Snapjaws");

            Assert.IsTrue(entity.HasTag("Creature"));
            Assert.AreEqual("Snapjaws", entity.GetTag("Faction"));
            Assert.IsFalse(entity.HasTag("Item"));
        }

        // ========================
        // Events
        // ========================

        [Test]
        public void GameEvent_Parameters_TypedCorrectly()
        {
            var e = GameEvent.New("TestEvent");
            e.SetParameter("Actor", "player");
            e.SetParameter("Damage", 5);
            e.SetParameter("Weapon", new Entity());

            Assert.AreEqual("player", e.GetStringParameter("Actor"));
            Assert.AreEqual(5, e.GetIntParameter("Damage"));
            Assert.IsNotNull(e.GetParameter<Entity>("Weapon"));
        }

        [Test]
        public void GameEvent_ID_Registration()
        {
            int id1 = GameEvent.GetID("BeforeMove");
            int id2 = GameEvent.GetID("BeforeMove");
            int id3 = GameEvent.GetID("AfterMove");

            Assert.AreEqual(id1, id2);
            Assert.AreNotEqual(id1, id3);
        }

        private class CounterPart : Part
        {
            public int EventCount;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "TestEvent")
                    EventCount++;
                return true;
            }
        }

        [Test]
        public void Entity_FireEvent_ReachesParts()
        {
            var entity = new Entity();
            var counter = new CounterPart();
            entity.AddPart(counter);

            entity.FireEvent("TestEvent");
            entity.FireEvent("TestEvent");
            entity.FireEvent("OtherEvent");

            Assert.AreEqual(2, counter.EventCount);
        }

        private class BlockingPart : Part
        {
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "Blocked")
                    return false;
                return true;
            }
        }

        [Test]
        public void Entity_FireEvent_PropagationStops()
        {
            var entity = new Entity();
            var blocker = new BlockingPart();
            var counter = new CounterPart();
            entity.AddPart(blocker);
            entity.AddPart(counter);

            bool result = entity.FireEvent("Blocked");

            Assert.IsFalse(result);
            Assert.AreEqual(0, counter.EventCount);
        }

        // ========================
        // Blueprint + Factory
        // ========================

        private const string TestBlueprints = @"{
          ""Objects"": [
            {
              ""Name"": ""Creature"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""RenderString"", ""Value"": ""?"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&y"" },
                  { ""Key"": ""RenderLayer"", ""Value"": ""10"" }
                ]}
              ],
              ""Stats"": [
                { ""Name"": ""Hitpoints"", ""Value"": 1, ""Min"": 0, ""Max"": 999 },
                { ""Name"": ""Strength"", ""Value"": 10, ""Min"": 1, ""Max"": 50 }
              ],
              ""Tags"": [
                { ""Key"": ""Creature"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""Snapjaw"",
              ""Inherits"": ""Creature"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""snapjaw"" },
                  { ""Key"": ""RenderString"", ""Value"": ""s"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&w"" }
                ]}
              ],
              ""Stats"": [
                { ""Name"": ""Hitpoints"", ""Value"": 15, ""Min"": 0, ""Max"": 15 },
                { ""Name"": ""Strength"", ""Value"": 16 }
              ],
              ""Tags"": [
                { ""Key"": ""Faction"", ""Value"": ""Snapjaws"" }
              ]
            },
            {
              ""Name"": ""Dagger"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""dagger"" },
                  { ""Key"": ""RenderString"", ""Value"": ""/"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&c"" }
                ]}
              ],
              ""Stats"": [
                { ""Name"": ""Hitpoints"", ""Value"": 5, ""Min"": 0, ""Max"": 5 }
              ],
              ""Tags"": [
                { ""Key"": ""Item"", ""Value"": """" },
                { ""Key"": ""Tier"", ""Value"": ""1"" }
              ],
              ""Props"": [
                { ""Key"": ""DamageDice"", ""Value"": ""1d4"" }
              ],
              ""IntProps"": [
                { ""Key"": ""PenetrationBonus"", ""Value"": 1 }
              ]
            }
          ]
        }";

        [Test]
        public void BlueprintLoader_LoadsFromJson()
        {
            var blueprints = BlueprintLoader.LoadFromJson(TestBlueprints);

            Assert.IsTrue(blueprints.ContainsKey("Creature"));
            Assert.IsTrue(blueprints.ContainsKey("Snapjaw"));
            Assert.IsTrue(blueprints.ContainsKey("Dagger"));
        }

        [Test]
        public void BlueprintLoader_InheritanceResolved()
        {
            var blueprints = BlueprintLoader.LoadFromJson(TestBlueprints);
            var snapjaw = blueprints["Snapjaw"];

            // Snapjaw should inherit Creature tag
            Assert.IsTrue(snapjaw.Tags.ContainsKey("Creature"));
            // And have its own
            Assert.AreEqual("Snapjaws", snapjaw.Tags["Faction"]);

            // Should inherit Render part with merged params
            Assert.IsTrue(snapjaw.Parts.ContainsKey("Render"));
            Assert.AreEqual("s", snapjaw.Parts["Render"]["RenderString"]);
            // Inherited RenderLayer from Creature
            Assert.AreEqual("10", snapjaw.Parts["Render"]["RenderLayer"]);
        }

        [Test]
        public void EntityFactory_CreatesDagger()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(TestBlueprints);

            var dagger = factory.CreateEntity("Dagger");

            Assert.IsNotNull(dagger);
            Assert.AreEqual("Dagger", dagger.BlueprintName);

            // Check render part
            var render = dagger.GetPart<RenderPart>();
            Assert.IsNotNull(render);
            Assert.AreEqual("dagger", render.DisplayName);
            Assert.AreEqual("/", render.RenderString);
            Assert.AreEqual("&c", render.ColorString);

            // Check stats
            Assert.AreEqual(5, dagger.GetStatValue("Hitpoints"));

            // Check tags
            Assert.IsTrue(dagger.HasTag("Item"));
            Assert.AreEqual("1", dagger.GetTag("Tier"));

            // Check properties
            Assert.AreEqual("1d4", dagger.GetProperty("DamageDice"));
            Assert.AreEqual(1, dagger.GetIntProperty("PenetrationBonus"));
        }

        [Test]
        public void EntityFactory_CreatesSnapjaw_WithInheritedStats()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(TestBlueprints);

            var snapjaw = factory.CreateEntity("Snapjaw");

            Assert.IsNotNull(snapjaw);

            // Overridden stats
            Assert.AreEqual(15, snapjaw.GetStatValue("Hitpoints"));
            Assert.AreEqual(16, snapjaw.GetStatValue("Strength"));

            // Render
            var render = snapjaw.GetPart<RenderPart>();
            Assert.AreEqual("snapjaw", render.DisplayName);
            Assert.AreEqual("s", render.RenderString);
            Assert.AreEqual(10, render.RenderLayer); // Inherited from Creature

            // Tags
            Assert.IsTrue(snapjaw.HasTag("Creature"));
            Assert.AreEqual("Snapjaws", snapjaw.GetTag("Faction"));
        }

        [Test]
        public void EntityFactory_MultipleInstances_IndependentState()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(TestBlueprints);

            var s1 = factory.CreateEntity("Snapjaw");
            var s2 = factory.CreateEntity("Snapjaw");

            // Different IDs
            Assert.AreNotEqual(s1.ID, s2.ID);

            // Mutating one doesn't affect the other
            s1.SetStatValue("Hitpoints", 0);
            Assert.AreEqual(15, s2.GetStatValue("Hitpoints"));
        }
    }
}
