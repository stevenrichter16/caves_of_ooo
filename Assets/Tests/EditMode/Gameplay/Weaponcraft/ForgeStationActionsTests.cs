using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Gameplay.Weaponcraft
{
    /// <summary>
    /// M3-L3 SM4 — the forge's world-action surface: the look-mode menu rows
    /// ForgePart declares and the InventoryAction handlers that resolve the
    /// player's set-aside items into Forge/Reforge/Temper command executions.
    /// Selection-count rules live here and must reject LEGIBLY with nothing
    /// consumed (counter-checked per §3.4). Statics discipline mirrors
    /// AlchemyStillActionsTests: ForgePart.Factory wired in Setup, cleared
    /// in TearDown.
    /// </summary>
    public class ForgeStationActionsTests
    {
        private const string TestBlueprintsJson = @"{
  ""Objects"": [
    {
      ""Name"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" } ] },
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""item"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": []
    },
    {
      ""Name"": ""ForgedWeapon"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""forged weapon"" } ] },
        { ""Name"": ""MeleeWeapon"", ""Params"": [ { ""Key"": ""BaseDamage"", ""Value"": ""1d2"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": []
    },
    {
      ""Name"": ""SteelBlade"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""steel blade"" } ] },
        { ""Name"": ""WeaponComponent"", ""Params"": [
          { ""Key"": ""Slot"", ""Value"": ""Blade"" },
          { ""Key"": ""BaseDamage"", ""Value"": ""1d6"" },
          { ""Key"": ""NameFragment"", ""Value"": ""steel blade"" }
        ]}
      ],
      ""Stats"": [],
      ""Tags"": []
    },
    {
      ""Name"": ""IronSpike"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""iron spike"" } ] },
        { ""Name"": ""WeaponComponent"", ""Params"": [
          { ""Key"": ""Slot"", ""Value"": ""Blade"" },
          { ""Key"": ""BaseDamage"", ""Value"": ""1d4"" },
          { ""Key"": ""NameFragment"", ""Value"": ""iron spike"" }
        ]}
      ],
      ""Stats"": [],
      ""Tags"": []
    },
    {
      ""Name"": ""OakHaft"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""oak haft"" } ] },
        { ""Name"": ""WeaponComponent"", ""Params"": [
          { ""Key"": ""Slot"", ""Value"": ""Haft"" },
          { ""Key"": ""NameFragment"", ""Value"": ""oak-hafted"" }
        ]}
      ],
      ""Stats"": [],
      ""Tags"": []
    },
    {
      ""Name"": ""LeatherBinding"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""leather binding"" } ] },
        { ""Name"": ""WeaponComponent"", ""Params"": [
          { ""Key"": ""Slot"", ""Value"": ""Binding"" },
          { ""Key"": ""NameFragment"", ""Value"": ""leather-bound"" }
        ]}
      ],
      ""Stats"": [],
      ""Tags"": []
    },
    {
      ""Name"": ""TinkersForge"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""tinker's forge"" } ] },
        { ""Name"": ""Forge"", ""Params"": [] }
      ],
      ""Stats"": [],
      ""Tags"": []
    }
  ]
}";

        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            _factory = new EntityFactory();
            _factory.LoadBlueprints(TestBlueprintsJson);
            ForgePart.Factory = _factory;
        }

        [TearDown]
        public void TearDown()
        {
            ForgePart.Factory = null;
        }

        private static Entity CreateSmith()
        {
            var smith = new Entity { ID = "smith", BlueprintName = "Player" };
            smith.AddPart(new RenderPart { DisplayName = "smith" });
            smith.AddPart(new InventoryPart());
            return smith;
        }

        private Entity GiveMarked(Entity smith, string blueprint)
        {
            Entity item = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(item, $"fixture blueprint '{blueprint}' must exist");
            Assert.IsTrue(smith.GetPart<InventoryPart>().AddObject(item));
            Assert.IsTrue(CraftingMarkPart.Toggle(item), "fixture marks the item");
            return item;
        }

        private Entity GiveMarkedCoating(Entity smith, string effects = "Burning:2")
        {
            var coating = new Entity { ID = "coating", BlueprintName = "coating" };
            coating.AddPart(new RenderPart { DisplayName = "flame coating" });
            coating.AddPart(new BrewItemPart { Form = "Coating", EffectsRaw = effects });
            Assert.IsTrue(smith.GetPart<InventoryPart>().AddObject(coating));
            Assert.IsTrue(CraftingMarkPart.Toggle(coating));
            return coating;
        }

        /// <summary>Smith at (5,5), forge adjacent at (6,5).</summary>
        private Zone MakeZoneWithForge(Entity smith, out Entity forge)
        {
            var zone = new Zone("ForgeActionZone");
            Assert.IsTrue(zone.AddEntity(smith, 5, 5));
            forge = _factory.CreateEntity("TinkersForge");
            Assert.IsNotNull(forge);
            Assert.IsTrue(zone.AddEntity(forge, 6, 5));
            return zone;
        }

        private static void FireWorldAction(Entity target, Entity actor, Zone zone, string command)
        {
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", command);
            e.SetParameter("Actor", (object)actor);
            e.SetParameter("Zone", (object)zone);
            target.FireEventAndRelease(e);
        }

        private static List<Entity> Carried(Entity smith)
        {
            return smith.GetPart<InventoryPart>().Objects;
        }

        // ════════════════ Menu rows ════════════════

        [Test]
        public void GatherActions_ForgeDeclaresAllFourRows()
        {
            var smith = CreateSmith();
            MakeZoneWithForge(smith, out Entity forge);

            var commands = WorldInteractionSystem.GatherActions(forge).ConvertAll(a => a.Command);

            CollectionAssert.Contains(commands, "ForgeWeapon");
            CollectionAssert.Contains(commands, "ForgeWeaponBatch");
            CollectionAssert.Contains(commands, "ReforgeWeapon");
            CollectionAssert.Contains(commands, "QuenchWeapon");
        }

        [Test]
        public void GatherActions_WithActor_ListsComponentsWeaponsAndCoatings()
        {
            // Live-playtest finding (2026-07-18): the forge menu must offer
            // its kit-building toggles in place, like the still's mix rows —
            // components, quenchable weapons, and coatings; reagents belong
            // to the still and get no row here.
            var smith = CreateSmith();
            Entity blade = _factory.CreateEntity("SteelBlade");
            Assert.IsTrue(smith.GetPart<InventoryPart>().AddObject(blade));
            var weapon = GiveMarked(smith, "ForgedWeapon");
            var coating = GiveMarkedCoating(smith);
            var reagent = new Entity { ID = "moss-x", BlueprintName = "moss" };
            reagent.AddPart(new RenderPart { DisplayName = "fire moss" });
            reagent.AddPart(new ReagentPart { PropertiesRaw = "heat:1" });
            Assert.IsTrue(smith.GetPart<InventoryPart>().AddObject(reagent));
            MakeZoneWithForge(smith, out Entity forge);

            var actions = WorldInteractionSystem.GatherActions(forge, smith);

            Assert.IsNotNull(actions.Find(a => a.Command == "CraftToggle:" + blade.ID),
                "carried component gets a toggle row");
            Assert.IsNotNull(actions.Find(a => a.Command == "CraftToggle:" + weapon.ID),
                "carried melee weapon gets a toggle row (quench/re-forge target)");
            Assert.IsNotNull(actions.Find(a => a.Command == "CraftToggle:" + coating.ID),
                "carried coating gets a toggle row (quench medium)");
            Assert.IsNull(actions.Find(a => a.Command == "CraftToggle:" + reagent.ID),
                "a reagent gets NO row on the FORGE — wrong station");
        }

        // ════════════════ Sectioned forge menu + Craft button (redesign) ════════════════

        [Test]
        public void GatherActions_WithActor_ShowsSectionsInOrderWithCraftAtBottom()
        {
            // Forge redesign (user spec): one section per part kind — Blades,
            // Hafts, Bindings, Quenches — each listing the carried items,
            // with ONE Craft button at the very bottom. Headers are inert
            // rows; the old flat verb list is gone from the actor menu.
            var smith = CreateSmith();
            Entity blade = _factory.CreateEntity("SteelBlade");
            Assert.IsTrue(smith.GetPart<InventoryPart>().AddObject(blade));
            Entity haft = _factory.CreateEntity("OakHaft");
            Assert.IsTrue(smith.GetPart<InventoryPart>().AddObject(haft));
            var coating = GiveMarkedCoating(smith);
            MakeZoneWithForge(smith, out Entity forge);

            var actions = WorldInteractionSystem.GatherActions(forge, smith);

            int iBlades = actions.FindIndex(a => a.Display.Contains("Blades"));
            int iHafts = actions.FindIndex(a => a.Display.Contains("Hafts"));
            int iBindings = actions.FindIndex(a => a.Display.Contains("Bindings"));
            int iQuench = actions.FindIndex(a => a.Display.Contains("Quenches"));
            int iCraft = actions.FindIndex(a => a.Command == "CraftKit");

            Assert.GreaterOrEqual(iBlades, 0, "Blades header present");
            Assert.GreaterOrEqual(iHafts, 0, "Hafts header present");
            Assert.GreaterOrEqual(iBindings, 0, "Bindings header present");
            Assert.GreaterOrEqual(iQuench, 0, "Quenches header present");
            Assert.GreaterOrEqual(iCraft, 0, "Craft button present");

            Assert.Less(iBlades, iHafts, "section order: Blades before Hafts");
            Assert.Less(iHafts, iBindings, "Hafts before Bindings");
            Assert.Less(iBindings, iQuench, "Bindings before Quenches");
            Assert.AreEqual(actions.Count - 1, iCraft, "Craft is the LAST row");

            int iBladeItem = actions.FindIndex(a => a.Command == "CraftToggle:" + blade.ID);
            Assert.IsTrue(iBladeItem > iBlades && iBladeItem < iHafts,
                "the carried blade lists inside the Blades section");
            int iCoatItem = actions.FindIndex(a => a.Command == "CraftToggle:" + coating.ID);
            Assert.IsTrue(iCoatItem > iQuench, "the coating lists inside the Quenches section");

            Assert.IsNull(actions.Find(a => a.Command == "ForgeWeaponBatch"),
                "the old flat verb rows are gone from the sectioned menu");
        }

        [Test]
        public void GatherActions_QuenchesSection_ListsEveryEffectCarryingBrew()
        {
            // User-directed contract (2026-07-19): ALL effect-carrying brews
            // quench — coatings, tonics, and throwable flasks list under
            // Quenches. Only an effect-less brew stays hidden (TryTemper
            // would reject it as carrying nothing).
            var smith = CreateSmith();
            var coating = GiveMarkedCoating(smith);
            var molotov = new Entity { ID = "molotov", BlueprintName = "molotov" };
            molotov.AddPart(new RenderPart { DisplayName = "burning & wet flask" });
            molotov.AddPart(new BrewItemPart { Form = "Throwable", EffectsRaw = "Burning:2;Wet:1" });
            Assert.IsTrue(smith.GetPart<InventoryPart>().AddObject(molotov));
            var tonic = new Entity { ID = "tonic", BlueprintName = "tonic" };
            tonic.AddPart(new RenderPart { DisplayName = "galvanic draught" });
            tonic.AddPart(new BrewItemPart { Form = "Tonic", EffectsRaw = "Electrified:1" });
            Assert.IsTrue(smith.GetPart<InventoryPart>().AddObject(tonic));
            var dud = new Entity { ID = "dud", BlueprintName = "dud" };
            dud.AddPart(new RenderPart { DisplayName = "empty vial" });
            dud.AddPart(new BrewItemPart { Form = "Coating", EffectsRaw = "" });
            Assert.IsTrue(smith.GetPart<InventoryPart>().AddObject(dud));
            MakeZoneWithForge(smith, out Entity forge);

            var actions = WorldInteractionSystem.GatherActions(forge, smith);

            Assert.IsNotNull(actions.Find(a => a.Command == "CraftToggle:" + coating.ID),
                "coating lists under Quenches");
            Assert.IsNotNull(actions.Find(a => a.Command == "CraftToggle:" + molotov.ID),
                "throwable flask lists under Quenches now");
            Assert.IsNotNull(actions.Find(a => a.Command == "CraftToggle:" + tonic.ID),
                "tonic lists under Quenches now");
            Assert.IsNull(actions.Find(a => a.Command == "CraftToggle:" + dud.ID),
                "an effect-less brew is not offered");
        }

        [Test]
        public void CraftKit_FullKit_ForgesTheWeapon()
        {
            var smith = CreateSmith();
            GiveMarked(smith, "SteelBlade");
            GiveMarked(smith, "OakHaft");
            GiveMarked(smith, "LeatherBinding");
            var zone = MakeZoneWithForge(smith, out Entity forge);

            FireWorldAction(forge, smith, zone, "CraftKit");

            Entity weapon = Carried(smith).Find(e => e.HasPart<WeaponAssemblyPart>());
            Assert.IsNotNull(weapon, "Craft with a full kit forges");
            Assert.AreEqual("1d6", weapon.GetPart<MeleeWeaponPart>().BaseDamage);
        }

        [Test]
        public void CraftKit_FullKitPlusQuench_ForgesAndTempersInOneGo()
        {
            // The composite the redesign exists for: pick the parts, pick a
            // quench, press Craft — the fresh blade comes out tempered.
            var smith = CreateSmith();
            GiveMarked(smith, "SteelBlade");
            GiveMarked(smith, "OakHaft");
            GiveMarked(smith, "LeatherBinding");
            var coating = GiveMarkedCoating(smith);
            var zone = MakeZoneWithForge(smith, out Entity forge);

            FireWorldAction(forge, smith, zone, "CraftKit");

            Entity weapon = Carried(smith).Find(e => e.HasPart<WeaponAssemblyPart>());
            Assert.IsNotNull(weapon);
            Assert.IsFalse(Carried(smith).Contains(coating), "quench consumed");
            var temper = weapon.GetPart<WeaponTemperPart>();
            Assert.IsNotNull(temper, "the NEW weapon is tempered");
            Assert.AreEqual(1, temper.TemperCount);
            StringAssert.Contains("Burning", weapon.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
        }

        [Test]
        public void CraftKit_WeaponPlusOneComponent_Reforges()
        {
            var smith = CreateSmith();
            GiveMarked(smith, "SteelBlade");
            GiveMarked(smith, "OakHaft");
            GiveMarked(smith, "LeatherBinding");
            var zone = MakeZoneWithForge(smith, out Entity forge);
            FireWorldAction(forge, smith, zone, "CraftKit");
            Entity weapon = Carried(smith).Find(e => e.HasPart<WeaponAssemblyPart>());
            Assert.IsNotNull(weapon, "fixture: forge first");

            Assert.IsTrue(CraftingMarkPart.Toggle(weapon));
            GiveMarked(smith, "IronSpike");

            FireWorldAction(forge, smith, zone, "CraftKit");

            Assert.AreEqual("1d4", weapon.GetPart<MeleeWeaponPart>().BaseDamage,
                "Craft with weapon + one part re-forges");
        }

        [Test]
        public void CraftKit_WeaponPlusQuench_TempersTheExistingWeapon()
        {
            var smith = CreateSmith();
            var weapon = GiveMarked(smith, "ForgedWeapon");
            GiveMarkedCoating(smith);
            var zone = MakeZoneWithForge(smith, out Entity forge);

            FireWorldAction(forge, smith, zone, "CraftKit");

            var temper = weapon.GetPart<WeaponTemperPart>();
            Assert.IsNotNull(temper, "Craft with weapon + quench tempers it");
            Assert.AreEqual(1, temper.TemperCount);
        }

        [Test]
        public void CraftKit_NothingSelected_LegibleAndNothingConsumed()
        {
            var smith = CreateSmith();
            Entity blade = _factory.CreateEntity("SteelBlade");
            Assert.IsTrue(smith.GetPart<InventoryPart>().AddObject(blade));
            var zone = MakeZoneWithForge(smith, out Entity forge);

            FireWorldAction(forge, smith, zone, "CraftKit");

            Assert.IsTrue(Carried(smith).Contains(blade), "unpicked blade untouched");
            Assert.IsNull(Carried(smith).Find(e => e.HasPart<WeaponAssemblyPart>()));
            StringAssert.Contains("blade", (MessageLog.GetLast() ?? string.Empty).ToLowerInvariant(),
                "the guidance names what Craft needs");
        }

        // ════════════════ ForgeWeapon handler ════════════════

        [Test]
        public void ForgeWeapon_MarkedTriple_ForgesAndConsumes()
        {
            var smith = CreateSmith();
            var blade = GiveMarked(smith, "SteelBlade");
            var haft = GiveMarked(smith, "OakHaft");
            var binding = GiveMarked(smith, "LeatherBinding");
            var zone = MakeZoneWithForge(smith, out Entity forge);

            FireWorldAction(forge, smith, zone, "ForgeWeapon");

            Assert.IsFalse(Carried(smith).Contains(blade), "blade consumed");
            Assert.IsFalse(Carried(smith).Contains(haft), "haft consumed");
            Assert.IsFalse(Carried(smith).Contains(binding), "binding consumed");
            Entity weapon = Carried(smith).Find(e => e.HasPart<WeaponAssemblyPart>());
            Assert.IsNotNull(weapon, "forged weapon lands in inventory");
            Assert.AreEqual("1d6", weapon.GetPart<MeleeWeaponPart>().BaseDamage);
        }

        [Test]
        public void ForgeWeapon_TwoBladesMarked_LegibleAndNothingConsumed()
        {
            // Counter-check: ambiguous selection must not guess — reject with
            // a message and leave every component in place.
            var smith = CreateSmith();
            var blade1 = GiveMarked(smith, "SteelBlade");
            var blade2 = GiveMarked(smith, "IronSpike");
            var haft = GiveMarked(smith, "OakHaft");
            var binding = GiveMarked(smith, "LeatherBinding");
            var zone = MakeZoneWithForge(smith, out Entity forge);

            FireWorldAction(forge, smith, zone, "ForgeWeapon");

            Assert.IsTrue(Carried(smith).Contains(blade1), "first blade untouched");
            Assert.IsTrue(Carried(smith).Contains(blade2), "second blade untouched");
            Assert.IsTrue(Carried(smith).Contains(haft));
            Assert.IsTrue(Carried(smith).Contains(binding));
            Assert.IsNull(Carried(smith).Find(e => e.HasPart<WeaponAssemblyPart>()),
                "no weapon minted from an ambiguous selection");
        }

        [Test]
        public void ForgeWeapon_MissingSlot_LegibleAndNothingConsumed()
        {
            var smith = CreateSmith();
            var blade = GiveMarked(smith, "SteelBlade");
            var haft = GiveMarked(smith, "OakHaft");
            var zone = MakeZoneWithForge(smith, out Entity forge);

            FireWorldAction(forge, smith, zone, "ForgeWeapon");

            Assert.IsTrue(Carried(smith).Contains(blade));
            Assert.IsTrue(Carried(smith).Contains(haft));
            Assert.IsNull(Carried(smith).Find(e => e.HasPart<WeaponAssemblyPart>()));
            StringAssert.Contains("binding", (MessageLog.GetLast() ?? string.Empty).ToLowerInvariant(),
                "the message must name the missing slot");
        }

        [Test]
        public void ForgeWeapon_FactoryUnwired_LegibleNotCrash()
        {
            var smith = CreateSmith();
            GiveMarked(smith, "SteelBlade");
            GiveMarked(smith, "OakHaft");
            GiveMarked(smith, "LeatherBinding");
            var zone = MakeZoneWithForge(smith, out Entity forge);

            ForgePart.Factory = null;
            Assert.DoesNotThrow(() => FireWorldAction(forge, smith, zone, "ForgeWeapon"));
            Assert.AreEqual(3, Carried(smith).Count, "components untouched");
        }

        [Test]
        public void ForgeWeaponBatch_StackedComponents_ForgesFullBatch()
        {
            // Q3 cold-eye pin: the batch ROUTING (command string → batch=true)
            // — a typo'd case label would silently forge one instead of the
            // stack, and no other test would notice.
            var smith = CreateSmith();
            foreach (var bp in new[] { "SteelBlade", "OakHaft", "LeatherBinding" })
            {
                var item = GiveMarked(smith, bp);
                item.AddPart(new StackerPart { StackCount = 2 });
            }
            var zone = MakeZoneWithForge(smith, out Entity forge);

            FireWorldAction(forge, smith, zone, "ForgeWeaponBatch");

            int weapons = Carried(smith).FindAll(e => e.HasPart<WeaponAssemblyPart>()).Count;
            Assert.AreEqual(2, weapons, "batch forges down to the smallest stack");
        }

        // ════════════════ QuenchWeapon handler ════════════════

        [Test]
        public void QuenchWeapon_MarkedWeaponAndCoating_Tempers()
        {
            var smith = CreateSmith();
            var weapon = GiveMarked(smith, "ForgedWeapon");
            var coating = GiveMarkedCoating(smith);
            var zone = MakeZoneWithForge(smith, out Entity forge);

            FireWorldAction(forge, smith, zone, "QuenchWeapon");

            Assert.IsFalse(Carried(smith).Contains(coating), "coating consumed");
            var temper = weapon.GetPart<WeaponTemperPart>();
            Assert.IsNotNull(temper);
            Assert.AreEqual(1, temper.TemperCount);
        }

        [Test]
        public void QuenchWeapon_NoCoatingMarked_LegibleAndNoTemper()
        {
            // Counter-check: a marked weapon alone must not quench.
            var smith = CreateSmith();
            var weapon = GiveMarked(smith, "ForgedWeapon");
            var zone = MakeZoneWithForge(smith, out Entity forge);

            FireWorldAction(forge, smith, zone, "QuenchWeapon");

            Assert.IsNull(weapon.GetPart<WeaponTemperPart>(), "no temper without a coating");
            StringAssert.Contains("coating", (MessageLog.GetLast() ?? string.Empty).ToLowerInvariant());
        }

        // ════════════════ ReforgeWeapon handler ════════════════

        [Test]
        public void ReforgeWeapon_MarkedWeaponPlusOneComponent_Swaps()
        {
            var smith = CreateSmith();
            var blade = GiveMarked(smith, "SteelBlade");
            var haft = GiveMarked(smith, "OakHaft");
            var binding = GiveMarked(smith, "LeatherBinding");
            var zone = MakeZoneWithForge(smith, out Entity forge);

            FireWorldAction(forge, smith, zone, "ForgeWeapon");
            Entity weapon = Carried(smith).Find(e => e.HasPart<WeaponAssemblyPart>());
            Assert.IsNotNull(weapon, "fixture: forge first");

            // Mark the weapon (forged fresh = unmarked) and one replacement.
            Assert.IsTrue(CraftingMarkPart.Toggle(weapon));
            var spike = GiveMarked(smith, "IronSpike");

            FireWorldAction(forge, smith, zone, "ReforgeWeapon");

            Assert.IsFalse(Carried(smith).Contains(spike), "replacement consumed");
            Assert.AreEqual("1d4", weapon.GetPart<MeleeWeaponPart>().BaseDamage,
                "stats recomputed from the swapped-in blade");
        }

        [Test]
        public void ReforgeWeapon_NoWeaponMarked_LegibleAndNothingConsumed()
        {
            var smith = CreateSmith();
            var spike = GiveMarked(smith, "IronSpike");
            var zone = MakeZoneWithForge(smith, out Entity forge);

            FireWorldAction(forge, smith, zone, "ReforgeWeapon");

            Assert.IsTrue(Carried(smith).Contains(spike), "component untouched");
            StringAssert.Contains("weapon", (MessageLog.GetLast() ?? string.Empty).ToLowerInvariant());
        }
    }
}
