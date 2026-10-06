using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Detached controlled combat witness: authored items/enemies, granted skills,
    /// fixed positions and deterministic rolls. Does not prove ordinary discovery or keyboard combat.</summary>
    [Scenario(name:"Second Fifty Combat Audit",category:"Combat",description:"Elemental ground, positional sword choices and finite enemy tactics.")]
    public sealed class FiftySecondCombatBench : IScenario
    {
        public const int ExpectedCases=15;
        public string RunId { get; private set; }
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit=new List<string>();
        public readonly List<object> Observations=new List<object>();
        EntityFactory factory;
        readonly Random rng=new MinimumRandom();
        public void Apply(ScenarioContext ctx)
        {
            RunId=Guid.NewGuid().ToString("N");Cases=Failures=0;Audit.Clear();Observations.Clear();factory=ctx.Factory;
            var oldReaction=MaterialReactionResolver.Factory;var oldWall=Cryomancy_GlacialWall.Factory;
            var oldLoadout=LoadoutPart.Factory;var oldLoadoutRng=LoadoutPart.Rng;
            try
            {
                using(var scope=new DetachedScope())
                {
                    MaterialReactionResolver.Factory=factory;Cryomancy_GlacialWall.Factory=factory;LoadoutPart.Factory=factory;LoadoutPart.Rng=rng;
                    Run("01_cone_floor_fan",()=>
                    {
                        var z=NewZone();var a=Actor(z);var fire=new Pyromancy_FlameJet();var water=new Hydromancy_JetBlast();Learn(a,fire,water);
                        bool fired=Cast(a,z,fire),soaked=Cast(a,z,water);
                        Check(fired&&soaked&&z.TileState.Get(12,11)?.Heat>0&&z.TileState.HasCoating(12,11,"water")&&!z.TileState.Has(10,11),new{fired,soaked,sideHeat=z.TileState.Get(12,11)?.Heat,sideWater=z.TileState.HasCoating(12,11,"water")});
                    });
                    Run("02_drying_cuts_only_transient_water",()=>
                    {
                        var z=NewZone();var a=Actor(z);var s=new Hydromancy_DryingBreeze();Learn(a,s);
                        z.TileState.WriteCoating(11,10,"water",6);z.TileState.WriteCoating(11,10,"oil",6);z.TileState.WriteCoating(10,11,"water",ZoneTileState.Permanent);
                        bool cast=Cast(a,z,s);Check(cast&&!z.TileState.HasCoating(11,10,"water")&&z.TileState.HasCoating(11,10,"oil")&&z.TileState.HasCoating(10,11,"water"),new{cast,temporary=z.TileState.CoatingTurns(11,10,"water"),permanent=z.TileState.CoatingTurns(10,11,"water")});
                    });
                    Run("03_crosswise_wall_slots",()=>
                    {
                        var z=NewZone();var a=Actor(z);var s=new Cryomancy_GlacialWall();Learn(a,s);bool cast=Cast(a,z,s);
                        var ice=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="IceWall").ToArray();Check(cast&&ice.Length==3&&Enumerable.Range(-1,3).All(y=>z.GetCell(12,10+y).IsSolid())&&!z.GetCell(11,10).IsSolid(),new{cast,slots=ice.Select(e=>z.GetEntityPosition(e).ToString()).ToArray(),cooldown=Cooldown(a,s)});
                    });
                    Run("04_wall_expiry_finite_meltwater",()=>
                    {
                        var z=NewZone();var ice=factory.CreateEntity("IceWall");Require(z.AddEntity(ice,12,10),"ice placement");Tick(ice,z,8);int water=z.TileState.CoatingTurns(12,10,"water");for(int i=0;i<4;i++)z.TileState.Tick();
                        Check(ice.SpatialZone==null&&water==4&&!z.TileState.HasCoating(12,10,"water"),new{removed=ice.SpatialZone==null,waterAfterExpiry=water,waterAfterFour=z.TileState.CoatingTurns(12,10,"water")});
                    });
                    Run("05_trail_entry_and_finite_owners",()=>
                    {
                        var z=NewZone();var a=Actor(z);var target=Actor(z,12,11);var s=new Corrosion_CausticTrail();Learn(a,s);bool cast=Cast(a,z,s);var films=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="CausticFilm").ToArray();
                        bool entered=MovementSystem.TryMoveTo(target,z,12,10);float corrosion=target.GetEffect<AcidicEffect>()?.Corrosion??0;foreach(var film in films)Tick(film,z,4);
                        Check(cast&&entered&&films.Length==3&&Math.Abs(corrosion-.35f)<.001f&&films.All(e=>e.SpatialZone==null)&&target.GetStatValue("Hitpoints")==1000,new{cast,entered,filmCount=films.Length,corrosion,hp=target.GetStatValue("Hitpoints")});
                    });
                    Run("06_draw_spends_real_primer",()=>
                    {
                        var z=NewZone();var a=Actor(z);var target=Actor(z,13,10);var s=new Corrosion_CausticDraw();Learn(a,s);target.ApplyEffect(new AcidicEffect(.5f),a,z);bool cast=Cast(a,z,s);
                        Check(cast&&target.GetStatValue("Hitpoints")==992&&!target.HasEffect<AcidicEffect>()&&Cooldown(a,s)==20,new{cast,hp=target.GetStatValue("Hitpoints"),marked=target.HasEffect<AcidicEffect>(),cooldown=Cooldown(a,s)});
                    });
                    Run("07_en_garde_one_melee_parry",()=>
                    {
                        var z=NewZone();var a=Actor(z);Arm(a,"LongSword");var attacker=Actor(z,11,10);var s=new LongBlades_EnGarde();Learn(a,s);bool cast=Cast(a,z,s);
                        Hit(a,attacker,z,9,"Melee");int first=a.GetStatValue("Hitpoints");Hit(a,attacker,z,9,"Melee");Check(cast&&first==995&&a.GetStatValue("Hitpoints")==986&&!a.HasEffect<EnGardeEffect>(),new{cast,first,second=a.GetStatValue("Hitpoints"),guard=a.HasEffect<EnGardeEffect>()});
                    });
                    Run("08_follow_through_paid_trap_entry",()=>
                    {
                        var z=NewZone();var a=Actor(z);Arm(a,"LongSword");var target=Actor(z,11,10,1);var trap=new Entity();trap.AddPart(new SpikeTrapTriggerPart{Damage=12});Require(z.AddEntity(trap,11,10),"trap placement");
                        var s=new LongBlades_FollowThrough();Learn(a,s);a.SetTag("Player");var turns=new TurnManager();turns.AddEntity(a);Require(turns.ProcessUntilPlayerTurn()==a,"scheduled owner");int before=turns.TickCount;
                        bool cast=Cast(a,z,s,z.GetEntityCell(target));int paidCooldown=Cooldown(a,s);if(cast){turns.EndTurn(a,z);Require(turns.ProcessUntilPlayerTurn()==a,"next owner action");}
                        Check(cast&&CombatSystem.IsDeathHandled(target)&&z.GetEntityPosition(a)==(11,10)&&a.GetStatValue("Hitpoints")==988&&paidCooldown==12&&turns.TickCount==before+10,new{cast,x=z.GetEntityPosition(a).x,hp=a.GetStatValue("Hitpoints"),victimDead=CombatSystem.IsDeathHandled(target),paidCooldown,beforeTick=before,afterTick=turns.TickCount});
                    });
                    Run("09_slam_collision_partner_damage",()=>
                    {
                        var z=NewZone();var a=Actor(z);Arm(a,"Cudgel");var target=Actor(z,11,10);var obstacle=Actor(z,12,10);var s=new Cudgel_Slam();Learn(a,s);bool cast=Cast(a,z,s,z.GetEntityCell(target));Check(cast&&obstacle.GetStatValue("Hitpoints")<1000&&z.GetEntityPosition(target)==(11,10)&&z.GetEntityPosition(obstacle)==(12,10),new{cast,obstacleHP=obstacle.GetStatValue("Hitpoints"),targetHP=target.GetStatValue("Hitpoints"),cooldown=Cooldown(a,s)});
                    });
                    Run("10_vault_one_accuracy_opening",()=>
                    {
                        var z=NewZone();var a=Actor(z);Arm(a,"LongSword");var s=new Acrobatics_Vault();Learn(a,s);bool cast=Cast(a,z,s);var target=Actor(z,13,10);var weapon=SkillCombatHelpers.FindEquippedWeaponOfClass(a,"LongBlades");
                        CombatSystem.PerformSingleAttack(a,target,weapon,true,z,rng);var receipt=DiagQuery.Apply(new DiagQuery.Filter{Category="damage",Kind="HitRoll",Actor=a.ID,Target=target.ID,Limit=1}).Records.FirstOrDefault();
                        Check(cast&&z.GetEntityPosition(a)==(12,10)&&receipt.PayloadJson.Contains("\"skillHitBonus\":2")&&!a.HasEffect<VaultPoiseEffect>(),new{cast,roll=receipt.PayloadJson,poise=a.HasEffect<VaultPoiseEffect>()});
                    });
                    Run("11_authored_caster_retreats_for_one_action",()=>
                    {
                        var z=NewZone();var a=Npc(z,"MarlbackSoursprayer");var target=Actor(z,11,10);Hostile(a,target);foreach(var ability in a.GetPart<ActivatedAbilitiesPart>().AbilityList)ability.CooldownRemaining=5;Act(a,target,z);
                        Check(z.GetEntityPosition(a)==(9,10)&&target.GetStatValue("Hitpoints")==1000&&a.GetPart<ActivatedAbilitiesPart>().AbilityList.All(e=>e.CooldownRemaining==5),new{x=z.GetEntityPosition(a).x,targetHP=target.GetStatValue("Hitpoints"),range=a.GetPart<CombatTacticsPart>().PreferredRange});
                    });
                    Run("12_sidestep_then_separate_cast",()=>
                    {
                        var z=NewZone();var a=Actor(z);a.AddPart(new BrainPart{CurrentZone=z,Rng=rng,FleeThreshold=0});a.AddPart(new CombatTacticsPart{SkillClasses="Cryomancy_IceLance",AbilityChance=100,RepositionForShot=true});a.FireEventAndRelease(GameEvent.New("ObjectCreated"));
                        var target=Actor(z,12,10);var ally=Actor(z,11,10);Hostile(a,target);Act(a,target,z);bool moved=z.GetEntityPosition(a)!=(10,10)&&target.GetStatValue("Hitpoints")==1000;Act(a,target,z);
                        Check(moved&&target.GetStatValue("Hitpoints")<1000&&ally.GetStatValue("Hitpoints")==1000,new{moved,targetHP=target.GetStatValue("Hitpoints"),allyHP=ally.GetStatValue("Hitpoints")});
                    });
                    Run("13_exact_disarmed_weapon_recovered",()=>
                    {
                        var z=NewZone();var a=Actor(z);Arm(a,"Cudgel");var target=Actor(z,11,10);var weapon=Arm(target,"LongSword");target.AddPart(new BrainPart{CurrentZone=z,Rng=rng,FleeThreshold=0});target.AddPart(new WeaponRecoveryPart());Hostile(target,a);
                        var s=new Cudgel_Disarm();Learn(a,s);bool cast=Cast(a,z,s,z.GetEntityCell(target));bool dropped=z.GetEntityCell(weapon)==z.GetEntityCell(target);Act(target,a,z);bool picked=target.GetPart<InventoryPart>().Contains(weapon)&&!InventorySystem.IsEquipped(target,weapon);Act(target,a,z);
                        Check(cast&&dropped&&picked&&InventorySystem.IsEquipped(target,weapon)&&a.GetStatValue("Hitpoints")==1000,new{cast,dropped,picked,equipped=InventorySystem.IsEquipped(target,weapon),weapon=weapon.ID});
                    });
                    Run("14_medic_spends_actual_cure_keeps_healing",()=>
                    {
                        var z=NewZone();var a=Npc(z,"MarlbackPatchbearer");var target=Actor(z,11,10);Hostile(a,target);var inventory=a.GetPart<InventoryPart>();var cure=inventory.Objects.Single(e=>e.BlueprintName=="Antidote");var healing=inventory.Objects.Single(e=>e.BlueprintName=="HealingTonic");int hp=a.GetStatValue("Hitpoints");a.ApplyEffect(new PoisonedEffect(6,"1d1"),target,z);Act(a,target,z);
                        Check(!a.HasEffect<PoisonedEffect>()&&!inventory.Contains(cure)&&inventory.Contains(healing)&&a.GetStatValue("Hitpoints")==hp-1&&target.GetStatValue("Hitpoints")==1000,new{poison=a.HasEffect<PoisonedEffect>(),cureCarried=inventory.Contains(cure),healingCarried=inventory.Contains(healing),beforeHp=hp,hp=a.GetStatValue("Hitpoints"),preActionPoisonDamage=1});
                    });
                    Run("15_stormbinder_setup_save_and_depth_source",()=>
                    {
                        var z=new Zone("Overworld.0.0.0");var a=Npc(z,"MarlbackStormbinder");var target=Actor(z,13,10);target.SetTag("Player");Hostile(a,target);Act(a,target,z);bool soaked=target.HasEffect<WetEffect>()&&!target.HasEffect<ElectrifiedEffect>();Act(a,target,z);
                        var turns=new TurnManager();turns.AddEntity(target);turns.AddEntity(a);var manager=new OverworldZoneManager(null,64);manager.ReplaceLoadedState(new Dictionary<string,Zone>{{z.ZoneID,z}},z.ZoneID,new Dictionary<string,List<ZoneConnection>>());GameSessionState loaded;
                        using(var bytes=new MemoryStream()){GameSessionState.Capture(RunId,"controlled second combat witness",manager,turns,target).Save(new SaveWriter(bytes));bytes.Position=0;loaded=GameSessionState.Load(new SaveReader(bytes,null));}
                        var restored=loaded.ZoneManager.ActiveZone.GetReadOnlyEntities().Single(e=>e.ID==a.ID);var row=PopulationTable.UndergroundTier(3).Entries.Single(e=>e.BlueprintName=="MarlbackStormbinder");
                        Check(soaked&&target.GetStatValue("Hitpoints")<1000&&target.HasEffect<ElectrifiedEffect>()&&restored!=a&&restored.GetPart<ActivatedAbilitiesPart>().AbilityList.Single(e=>e.Command=="CommandArcBolt").CooldownRemaining==7&&restored.GetPart<InventoryPart>().Objects.Count(e=>e.BlueprintName=="SparkRoot")==1&&row.EncounterGroup=="DepthEncounter"&&row.Weight==1,new{soaked,hp=target.GetStatValue("Hitpoints"),electrified=target.HasEffect<ElectrifiedEffect>(),replaced=restored!=a,group=row.EncounterGroup,weight=row.Weight});
                    });
                }
            }
            finally{MaterialReactionResolver.Factory=oldReaction;Cryomancy_GlacialWall.Factory=oldWall;LoadoutPart.Factory=oldLoadout;LoadoutPart.Rng=oldLoadoutRng;}
        }
        string current;
        void Run(string name,Action action){current=name;int before=Cases;try{action();}catch(Exception error){if(Cases==before)Check(false,new{error=error.ToString()});else throw;}}
        void Check(bool passed,object state){Cases++;if(!passed)Failures++;Audit.Add((passed?"PASS ":"FAIL ")+current);Observations.Add(new{name=current,passed,state});}
        static void Require(bool condition,string reason){if(!condition)throw new InvalidOperationException("Second combat fixture: "+reason);}
        static Zone NewZone()=>new Zone("second-combat-"+Guid.NewGuid().ToString("N"));
        static Entity Actor(Zone z,int x=10,int y=10,int hp=1000)
        {
            var a=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="combat witness"};a.SetTag("Creature");a.AddPart(new PhysicsPart{Solid=true});a.AddPart(new RenderPart{DisplayName="combat witness"});a.AddPart(new StatusEffectsPart());a.AddPart(new ArmorPart());a.AddPart(new InventoryPart{MaxWeight=1000});a.AddPart(new SkillsPart());a.AddPart(new ActivatedAbilitiesPart());var body=new Body();a.AddPart(body);body.SetBody(AnatomyFactory.CreateHumanoid());
            foreach(var value in new[]{("Hitpoints",hp),("Strength",30),("Agility",16),("Toughness",16),("Speed",100),("DV",0),("Experience",0)})a.Statistics[value.Item1]=new Stat{Owner=a,Name=value.Item1,BaseValue=value.Item2,Min=0,Max=value.Item1=="Hitpoints"?hp:100000};Require(z.AddEntity(a,x,y),"actor placement");return a;
        }
        Entity Arm(Entity actor,string blueprint){actor.GetStat("Agility").BaseValue=100;var item=factory.CreateEntity(blueprint);Require(item!=null&&actor.GetPart<InventoryPart>().AddObject(item)&&InventorySystem.Equip(actor,item),"authored weapon equipped");return item;}
        Entity Npc(Zone z,string blueprint){var a=factory.CreateEntity(blueprint);Require(a!=null&&z.AddEntity(a,10,10),"authored NPC placement");var brain=a.GetPart<BrainPart>();brain.CurrentZone=z;brain.Rng=rng;brain.FleeThreshold=0;return a;}
        static void Hostile(Entity actor,Entity target)=>actor.GetPart<BrainPart>().SetPersonallyHostile(target);
        static void Learn(Entity actor,params BaseSkillPart[] skills){foreach(var skill in skills)Require(actor.GetPart<SkillsPart>().AddSkill(skill,"controlled-runtime-witness"),"granted fixture skill");}
        bool Cast(Entity a,Zone z,BaseSkillPart s,Cell target=null){var e=GameEvent.New(s.DeclareActivatedAbility(a).Command);e.SetParameter("Zone",(object)z);e.SetParameter("RNG",(object)rng);e.SetParameter("SourceCell",(object)z.GetEntityCell(a));e.SetParameter("DirectionX",1);e.SetParameter("Range",s.DeclareActivatedAbility(a).Range);if(target!=null)e.SetParameter("TargetCell",(object)target);try{a.FireEvent(e);return e.Handled;}finally{e.Release();}}
        static int Cooldown(Entity a,BaseSkillPart s)=>a.GetPart<ActivatedAbilitiesPart>().GetAbility(s.ActivatedAbilityID).CooldownRemaining;
        static void Act(Entity a,Entity target,Zone z){var b=a.GetPart<BrainPart>();b.CurrentZone=z;b.Target=target;if(!(b.PeekGoal() is KillGoal))b.PushGoal(new KillGoal(target));a.FireEventAndRelease(GameEvent.New("TakeTurn"));}
        static void Tick(Entity a,Zone z,int count){for(int i=0;i<count;i++){var e=GameEvent.New("EndTurn");e.SetParameter("Zone",(object)z);a.FireEventAndRelease(e);}}
        static void Hit(Entity target,Entity source,Zone z,int amount,string type){var d=new Damage(amount);d.AddAttribute(type);CombatSystem.ApplyDamage(target,d,source,z);}
        sealed class MinimumRandom:Random{public override int Next(int max)=>0;public override int Next(int min,int max)=>min;}
        private sealed class DetachedScope : IDisposable
        {
            readonly TurnManager active = TurnManager.Active;
            readonly Entity world = TurnManager.World;
            readonly SettlementManager settlement = SettlementManager.Current;
            readonly Action<string> onMessage = MessageLog.OnMessage;
            readonly Func<int> tickProvider = MessageLog.TickProvider;
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries();
            readonly List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue;
            readonly List<SpellFxSequence> spells = SpellFxBus.Drain();
            readonly List<AsciiFxRequest> ascii = AsciiFxBus.Drain();
            readonly Action<int,int,string> dirty = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> fullDirty = ZoneRenderHooks.FullDirtyCallback;
            readonly Dictionary<PropertyInfo,object> visualHooks = typeof(EntityVisualHooks).GetProperties(BindingFlags.Public|BindingFlags.Static).Where(p=>p.CanRead&&p.CanWrite).ToDictionary(p=>p,p=>p.GetValue(null));
            readonly Queue<AsciiFxRequest> queue;
            readonly FieldInfo cosmeticField;
            readonly int cosmeticSerial;
            public DetachedScope()
            {
                queue = (Queue<AsciiFxRequest>)typeof(AsciiFxBus).GetField("PendingRequests", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                cosmeticField = typeof(SpellFxCapture).GetField("_cosmeticSerial", BindingFlags.NonPublic | BindingFlags.Static);
                cosmeticSerial = (int)cosmeticField.GetValue(null);
                TurnManager.World = null; MessageLog.OnMessage = null;
                EntityVisualHooks.Reset(); ZoneRenderHooks.CellDirtyCallback=null; ZoneRenderHooks.FullDirtyCallback=null;
            }
            public void Dispose()
            {
                foreach(var pair in visualHooks)pair.Key.SetValue(null,pair.Value);
                ZoneRenderHooks.CellDirtyCallback=dirty;ZoneRenderHooks.FullDirtyCallback=fullDirty;
                foreach (var request in AsciiFxBus.Drain()) AsciiFxBus.Release(request);
                foreach (var request in ascii) queue.Enqueue(request);
                SpellFxBus.Clear(); foreach (var spell in spells) SpellFxBus.Emit(spell);
                cosmeticField.SetValue(null, cosmeticSerial);
                MessageLog.Restore(messages, announcements, flash, serial);
                MessageLog.OnMessage = onMessage; MessageLog.TickProvider = tickProvider; TurnManager.World = world;
                typeof(TurnManager).GetProperty("Active").SetValue(null, active);
                typeof(SettlementManager).GetProperty("Current").SetValue(null, settlement);
            }
        }
    }
}
