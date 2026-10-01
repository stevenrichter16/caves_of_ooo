using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;
using UnityEngine;
using Random=System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>Each shipped species is harvested and picked up before its actual
    /// item command is exercised. Core assertions do not claim native pixels.</summary>
    public sealed class BiomeCropUtilityTests : CultivatedCropTestBase
    {
        readonly List<Action> restore=new List<Action>();
        EntityFactory oldMaterialFactory;
        const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
        [SetUp] public void SetupUtilities()
        {
            oldMaterialFactory=MaterialReactionResolver.Factory;MaterialReactionResolver.Factory=Factory;
            Capture(typeof(BrewRuleRegistry));Capture(typeof(GasRegistry));Capture(typeof(LiquidRegistry));
            BrewRuleRegistry.InitializeFromJson(Read("Alchemy/BrewRules.json"));
            GasRegistry.InitializeFromJsonSources(ReadDirectory("GasDefinitions"));
            LiquidRegistry.InitializeFromJsonSources(ReadDirectory("LiquidDefinitions"));
            Assert.True(Zone.RemoveEntity(Actor));Actor=Factory.CreateEntity("Player");Actor.GetPart<InventoryPart>().MaxWeight=200;
            Assert.True(Zone.AddEntity(Actor,5,5));Zone.AmbientLevel=0.1f;
        }
        [TearDown] public void RestoreUtilities(){MaterialReactionResolver.Factory=oldMaterialFactory;foreach(var action in restore.AsEnumerable().Reverse())action();restore.Clear();}
        void Capture(Type type)
        {
            foreach(var field in type.GetFields(Static))
            {
                if(field.IsLiteral)continue;var original=field.GetValue(null);
                if(original is IDictionary dictionary){var entries=dictionary.Cast<DictionaryEntry>().ToArray();restore.Add(()=>{dictionary.Clear();foreach(var entry in entries)dictionary.Add(entry.Key,entry.Value);});}
                else if(original is IList list){var entries=list.Cast<object>().ToArray();restore.Add(()=>{list.Clear();foreach(var entry in entries)list.Add(entry);});}
                else if(!field.IsInitOnly)restore.Add(()=>field.SetValue(null,original));
            }
        }
        static string Read(string path)=>File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data",path));
        static IEnumerable<string> ReadDirectory(string dir)=>Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data",dir),"*.json").OrderBy(p=>p).Select(File.ReadAllText);
        bool Use(Entity item,string command)=>InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(item,command),Actor,Zone).Success;
        Entity Harvested(BiomeCropDefinition species)
        {
            Cultivate();var crop=Factory.CreateEntity(species.CropBlueprint);crop.GetPart<CropPart>().GrowthStage=2;
            Assert.True(Zone.AddEntity(crop,5,5));Assert.True(Harvest(crop).Success);
            var item=Zone.GetReadOnlyEntities().First(e=>e.BlueprintName==species.YieldBlueprint);
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(item),Actor,Zone).Success);
            if(InventorySystem.IsEquipped(Actor,item)) Assert.True(InventorySystem.UnequipItem(Actor,item));
            return Actor.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName==species.YieldBlueprint);
        }
        static Effect Ailment(string name)=>name switch {"BleedingEffect"=>new BleedingEffect(),"PoisonedEffect"=>new PoisonedEffect(),
            "ConfusedEffect"=>new ConfusedEffect(),"BurningEffect"=>new BurningEffect(),"FungalInfectionEffect"=>new FungalInfectionEffect(),_=>throw new ArgumentException(name)};
        [TestCaseSource(typeof(BiomeCropContentTests),nameof(BiomeCropContentTests.Species))]
        public void ActualHarvestCanPerformItsAdvertisedNonCookingUse(string stem)
        {
            var species=BiomeCropCatalog.Find(stem);Assert.NotNull(species);var item=Harvested(species);var inventory=Actor.GetPart<InventoryPart>();
            switch(species.UtilityKind)
            {
                case "Cure":
                    string cure=item.GetPart<CureTonicPart>().CureEffect;var ailment=Ailment(cure);Assert.True(Actor.ApplyEffect(ailment,null,Zone));
                    var foreign=Factory.CreateEntity(species.YieldBlueprint);Assert.False(Use(foreign,"ApplyTonic"));Assert.True(Actor.GetPart<StatusEffectsPart>().GetAllEffects().Contains(ailment));
                    Assert.True(Use(item,"ApplyTonic"));Assert.False(Actor.GetPart<StatusEffectsPart>().GetAllEffects().Any(e=>e.ClassName==cure));break;
                case "Healing":
                    Actor.GetStat("Hitpoints").BaseValue=10;Assert.True(Use(item,"ApplyTonic"));Assert.That(Actor.GetStatValue("Hitpoints"),Is.InRange(12,18));break;
                case "Reagent":
                    var expected=BrewResolver.Resolve(item.GetPart<ReagentPart>().GetProperties());Assert.AreEqual(BrewOutcomeKind.Brew,expected.Kind);Assert.IsNotEmpty(expected.Effects);
                    Assert.True(InventorySystem.ExecuteCommand(new BrewReagentsCommand(new[]{item},Factory),Actor,Zone).Success);
                    var brew=inventory.Objects.Single(e=>e.BlueprintName=="BrewedTonic");Assert.True(Use(brew,"ApplyTonic"));
                    foreach(var effect in expected.Effects){var actual=TonicEffectFactory.Create(effect.Effect,0,"",effect.Potency,Actor);Assert.NotNull(actual);Assert.True(Actor.GetPart<StatusEffectsPart>().GetAllEffects().Any(e=>e.GetType()==actual.GetType()),stem+": "+effect.Effect);}break;
                case "Status":
                    var status=item.GetPart<StatusTonicPart>();Assert.True(Use(item,"ApplyTonic"));var stone=Actor.GetEffect<StoneskinEffect>();Assert.NotNull(stone);Assert.AreEqual(status.EffectDuration,stone.Duration);
                    var hit=GameEvent.New("BeforeTakeDamage");var damage=new Damage(10);hit.SetParameter("Damage",damage);Actor.FireEventAndRelease(hit);Assert.AreEqual(10-(int)status.EffectMagnitude,damage.Amount);break;
                case "ThrownStatus":
                    var name=item.GetPart<StatusTonicPart>().EffectName;
                    Assert.True(InventorySystem.ExecuteCommand(new ThrowItemCommand(item,5,5,new Random(64)),Actor,Zone).Success);
                    var effectType=TonicEffectFactory.Create(name,0,"",0.35f,Actor).GetType();Assert.True(Actor.GetPart<StatusEffectsPart>().GetAllEffects().Any(e=>e.GetType()==effectType));break;
                case "Gas":
                    string gasId=item.GetPart<GasGrenadePart>().GasId;
                    Assert.True(InventorySystem.ExecuteCommand(new ThrowItemCommand(item,7,5,new Random(64)),Actor,Zone).Success);
                    var cloud=Zone.GetReadOnlyEntities().FirstOrDefault(e=>e.GetPart<GasPoolPart>()?.GasId==gasId);Assert.NotNull(cloud);
                    var victim=Factory.CreateEntity("Player");var at=Zone.GetEntityPosition(cloud);Assert.True(Zone.AddEntity(victim,at.x,at.y));
                    Assert.True(cloud.GetPart<IObjectGasBehaviorPart>().ApplyGas(victim,Zone));
                    var kind=gasId=="sleep-vapor"?typeof(AsleepByGasEffect):gasId=="confusion-vapor"?typeof(ConfusedEffect):gasId=="stun-vapor"?typeof(StunnedEffect):typeof(FrozenEffect);
                    Assert.True(victim.GetPart<StatusEffectsPart>().GetAllEffects().Any(e=>e.GetType()==kind));break;
                case "WaterVessel":
                    var skin=item.GetPart<WaterskinPart>();int before=skin.Charges;Assert.False(Use(item,"FillWaterskin"));Assert.AreEqual(before,skin.Charges);
                    Assert.True(Zone.AddEntity(Factory.CreateEntity("Well"),6,5));Assert.True(Use(item,"FillWaterskin"));Assert.AreEqual(skin.Capacity,skin.Charges);
                    Assert.True(Actor.ApplyEffect(new ParchedEffect(),null,Zone));Assert.True(Use(item,"DrinkWaterskin"));Assert.AreEqual(skin.Capacity-1,skin.Charges);break;
                case "LiquidVessel":
                    var vessel=item.GetPart<LiquidVesselPart>();Assert.False(InventorySystem.GetActions(Actor,item).Any(a=>a.Command.StartsWith("FillLiquidVessel|")));
                    var pool=new Entity{ID="crop-use-water",BlueprintName="test-water"};pool.AddPart(new PhysicsPart{Takeable=false});pool.AddPart(new LiquidPoolPart{LiquidId="water",Volume=6});Assert.True(Zone.AddEntity(pool,6,5));
                    var fill=InventorySystem.GetActions(Actor,item).Single(a=>a.Command.StartsWith("FillLiquidVessel|"));Assert.True(Use(item,fill.Command));Assert.AreEqual(6,vessel.Volume);Assert.Zero(pool.GetPart<LiquidPoolPart>().Volume);
                    var pour=InventorySystem.GetActions(Actor,item).Single(a=>a.Command.StartsWith("PourLiquidVessel|")&&a.Command.EndsWith("|5|6"));Assert.True(Use(item,pour.Command),"pour action: "+MessageLog.GetLast());Assert.Zero(vessel.Volume);Assert.True(Zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="PouredLiquidPool"&&e.GetPart<LiquidPoolPart>().Volume==6),"Poured pool must hold six: "+string.Join(",",Zone.GetReadOnlyEntities().Where(e=>e.HasPart<LiquidPoolPart>()).Select(e=>e.BlueprintName+":"+e.GetPart<LiquidPoolPart>().Volume)));break;
                case "Light":
                    var map=new LightMap();map.Compute(Zone);float unheld=map.GetBrightness(5,5);Assert.True(InventorySystem.Equip(Actor,item));map.Compute(Zone);Assert.Greater(map.GetBrightness(5,5),unheld);
                    Assert.True(InventorySystem.UnequipItem(Actor,item));map.Compute(Zone);Assert.AreEqual(unheld,map.GetBrightness(5,5),0.0001f);break;
                case "Torch":
                    Assert.True(InventorySystem.Equip(Actor,item));Assert.False(Use(item,"LightTorch"));Assert.True(Zone.AddEntity(Factory.CreateEntity("Campfire"),6,5));
                    Assert.True(Use(item,"LightTorch"));Assert.True(item.GetPart<LightSourcePart>().Enabled);float fuel=item.GetPart<FuelPart>().FuelMass;
                    var turn=GameEvent.New("EndTurn");turn.SetParameter("Zone",Zone);Actor.FireEventAndRelease(turn);Assert.Less(item.GetPart<FuelPart>().FuelMass,fuel);
                    Assert.True(Use(item,"ExtinguishTorch"));Assert.False(item.GetPart<LightSourcePart>().Enabled);break;
                case "HeadCover":
                    Assert.False(BeatingGlareSystem.HasHeadCover(Actor));Assert.True(InventorySystem.Equip(Actor,item));Assert.True(BeatingGlareSystem.HasHeadCover(Actor));
                    Assert.True(InventorySystem.UnequipItem(Actor,item));Assert.False(BeatingGlareSystem.HasHeadCover(Actor));break;
                case "Handwear":
                    Assert.True(InventorySystem.Equip(Actor,item));Assert.True(InventorySystem.IsEquipped(Actor,item));var covered=Actor.GetPart<Body>().GetPartByType("Handwear");Assert.AreSame(item,covered.Equipped);Assert.AreEqual(1,CombatSystem.GetPartAV(Actor,covered));
                    Assert.True(InventorySystem.UnequipItem(Actor,item));Assert.False(InventorySystem.IsEquipped(Actor,item));Assert.Zero(CombatSystem.GetPartAV(Actor,covered));break;
                case "ThrownWeapon":
                    Assert.True(InventorySystem.ExecuteCommand(new ThrowItemCommand(item,7,5,new Random(64)),Actor,Zone).Success);
                    Assert.False(inventory.Objects.Contains(item));Assert.NotNull(Zone.GetEntityCell(item));break;
                case "Process":
                    string output=stem=="Marlroot"?"FireClay":"SalvagedTimber";Assert.True(Use(item,"ProcessBotanical"));
                    Assert.True(inventory.Objects.Any(e=>e.BlueprintName==output));Assert.False(inventory.Objects.Contains(item));break;
                default:Assert.Fail("Utility has no command witness: "+species.UtilityKind);break;
            }
        }
    }
}
