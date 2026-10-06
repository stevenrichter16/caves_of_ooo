using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Controlled, detached command/owner-turn/replacement-save witness.
    /// This does not claim an ordinary journey or unattended NPC simulation.</summary>
    [Scenario(name:"Second Exploration Audit",category:"World",description:"Exact finite services, physical routes and saved exploration consequences.")]
    public sealed class FiftySecondExplorationBench:IScenario
    {
        public const int ExpectedCases=21;
        public string RunId{get;private set;} public int Cases{get;private set;} public int Failures{get;private set;}
        public readonly List<string> Audit=new List<string>(); public readonly List<object> Observations=new List<object>();
        public void Apply(ScenarioContext ctx)
        {
            RunId=Guid.NewGuid().ToString("N");Cases=Failures=0;Audit.Clear();Observations.Clear();
            using(var scope=new DetachedScope())
            {
                SecondExplorationActions.Factory=ctx.Factory;
                Run(ctx,"selected_copy",f=>{var s=f.Npc<ScribeCopyServicePart>("Scribe");var a=f.Carry("WardGleamGrimoire");var b=f.Carry("DryingBreezeGrimoire");f.Carry("InkVial");return f.Act(s,"CopyVolume|"+b.ID)&&f.Count("InkVial")==0&&f.Count("GrimoireCopy")==1&&f.Pack.Objects.Single(e=>e.BlueprintName=="GrimoireCopy").GetPart<GrimoirePart>().SkillClassName==b.GetPart<GrimoirePart>().SkillClassName&&f.Pack.Objects.Contains(a)&&TradeSystem.GetDrams(f.Actor)==95;});
                Run(ctx,"smith_supplies_finite_repair",f=>{var s=f.Npc<ArtisanRepairServicePart>("Weaponsmith");var gear=f.Carry("Dagger");gear.ApplyEffect(new BrokenEffect());var r=RepairRecipeRegistry.Get(gear.GetPart<RepairablePart>().RecipeId);s.GetPart<InventoryPart>().Objects.Clear();for(int i=0;i<r.Quantity;i++)Require(s.GetPart<InventoryPart>().AddObject(f.Factory.CreateEntity(r.MaterialBlueprint)),"repair stock");return f.Act(s,"ArtisanRepair|"+gear.ID)&&!gear.HasEffect<BrokenEffect>()&&!s.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName==r.MaterialBlueprint)&&TradeSystem.GetDrams(f.Actor)==92;});
                Run(ctx,"selected_rental_return",f=>{var s=f.Npc<RentalDeskPart>("Quartermaster");var a=f.Loan(s,"Dagger");var b=f.Loan(s,"LeatherArmor");return f.Act(s,"ReturnRental|"+a.ID)&&s.GetPart<InventoryPart>().Objects.Contains(a)&&!a.HasPart<RentalPart>()&&f.Pack.Objects.Contains(b)&&b.HasPart<RentalPart>()&&RentalSystem.GetInk(f.Actor)==106;});
                Run(ctx,"worn_rental_buyout",f=>{var s=f.Npc<RentalDeskPart>("Quartermaster");var a=f.Loan(s,"Dagger");Require(InventorySystem.Equip(f.Actor,a),"worn rental");var slot=f.Pack.FindEquippedBodyPart(a);int price=TradeSystem.GetBuyPrice(a,TradeSystem.GetTradePerformance(f.Actor),s);return f.Act(s,"BuyRental|"+a.ID)&&!a.HasPart<RentalPart>()&&f.Pack.FindEquippedBodyPart(a)==slot&&RentalSystem.GetInk(f.Actor)==100&&TradeSystem.GetDrams(f.Actor)==100-price;});
                Run(ctx,"guest_claim_outlasts_oath",f=>{var c=f.Place("WellmeetGuestLocker");f.Actor.ApplyEffect(new UnderTheClothEffect{ExpiryTick=100});bool done=f.Act(c,"ClaimGuestLocker");f.Actor.RemoveEffect(typeof(UnderTheClothEffect));return done&&!c.GetPart<ContainerPart>().IsLocked&&c.GetPart<GuestLockerPart>().ClaimedBy==f.Actor&&!f.Act(c,"ClaimGuestLocker");});
                Run(ctx,"two_cell_locksmith",f=>{var s=f.Npc<LocksmithServicePart>();var c=f.Place("CounterStoreChest",13,10);return f.Act(s,"LocksmithOpen|"+c.ID)&&!c.GetPart<ContainerPart>().IsLocked&&!c.GetPart<LockPart>().IsLocked&&TradeSystem.GetDrams(f.Actor)==94;});
                Run(ctx,"courtesy_is_real_movement",f=>{var s=f.Npc<CivilianCourtesyPart>();var old=f.Zone.GetEntityPosition(s);return f.Act(s,"StepAside")&&f.Zone.GetEntityPosition(s)!=old&&f.Zone.GetEntityPosition(f.Actor)==(10,10);});
                Run(ctx,"finite_medicine_benefits_patient",f=>{var s=f.Npc<CivilianAidPart>();s.GetStat("Hitpoints").BaseValue=1;var t=f.Carry("HealingTonic");int own=f.Actor.GetStatValue("Hitpoints");return f.Act(s,"TreatPatient|"+t.ID)&&s.GetStatValue("Hitpoints")>1&&!f.Pack.Objects.Contains(t)&&f.Actor.GetStatValue("Hitpoints")==own;});
                Run(ctx,"gift_occupies_actual_npc_hand",f=>{var s=f.Npc<CivilianEquipmentGiftPart>();var a=f.Carry("Dagger");return f.Act(s,"DonateEquipment|"+a.ID)&&!f.Pack.Objects.Contains(a)&&s.GetPart<InventoryPart>().FindEquippedBodyPart(a)!=null&&a.GetPart<PhysicsPart>().Equipped==s;});
                Run(ctx,"body_remains_in_graveyard",f=>{var g=f.Place("Graveyard");g.AddPart(new BurialPart());var body=f.Carry("ReedbackGrazerCorpse");return f.Act(g,"InterCorpse|"+body.ID)&&g.GetPart<ContainerPart>().Contents.Contains(body)&&!f.Pack.Objects.Contains(body)&&TradeSystem.GetDrams(f.Actor)==100;});
                Run(ctx,"same_descent_jar_becomes_hand_light",f=>{var jar=f.Place("BeetleJar");jar.AddPart(new RecoverableLampPart());var light=jar.GetPart<LightSourcePart>();return f.Act(jar,"RecoverLamp")&&f.Zone.GetEntityCell(jar)==null&&InventorySystem.Equip(f.Actor,jar)&&f.Pack.FindEquippedBodyPart(jar)!=null&&jar.GetPart<LightSourcePart>()==light;});
                Run(ctx,"reciprocal_rope_and_real_stair_roundtrip",f=>
                {
                    f.NewZone("Overworld.2.7.1");var lower=new Zone("Overworld.2.7.2");var a=f.Place("RopeAnchor");var b=f.Factory.CreateEntity("RopeAnchor");Require(lower.AddEntity(b,11,10),"lower anchor");
                    f.Place("StairsDown",40,10);Require(lower.AddEntity(f.Factory.CreateEntity("StairsUp"),40,10),"existing lower stairs");
                    a.AddPart(new RopeShortcutPart{ZoneID=f.Zone.ZoneID,OtherZoneID=lower.ZoneID,X=11,Y=10,OtherX=11,OtherY=10,Down=true});b.AddPart(new RopeShortcutPart{ZoneID=lower.ZoneID,OtherZoneID=f.Zone.ZoneID,X=11,Y=10,OtherX=11,OtherY=10,Down=false});var manager=f.Manager(lower);f.Carry("KnotflaxCord");f.Carry("KnotflaxCord");if(!f.Act(a,"RigRopeShortcut"))return false;
                    Require(f.Zone.MoveEntity(f.Actor,11,10),"stand on rope");var down=ZoneTransitionSystem.TransitionPlayerVertical(f.Actor,f.Zone,true,11,10,manager);if(!down.Success)return false;var up=ZoneTransitionSystem.TransitionPlayerVertical(f.Actor,lower,false,11,10,manager);
                    return up.Success&&up.NewZone==f.Zone&&up.NewPlayerX==11&&up.NewPlayerY==10&&manager.GetConnections(f.Zone.ZoneID).Count==2&&manager.GetConnections(lower.ZoneID).Count==2&&manager.GetConnections(f.Zone.ZoneID).Count(c=>c.SourceZoneID==f.Zone.ZoneID)==1&&manager.GetConnections(lower.ZoneID).Count(c=>c.SourceZoneID==lower.ZoneID)==1&&f.Count("KnotflaxCord")==0;
                });
                Run(ctx,"jammed_spike_salvage_once",f=>{var trap=f.Place("SpikeTrap");trap.AddPart(new TrapSalvagePart());trap.GetPart<TrapJammingPart>().Jammed=true;return f.Act(trap,"SalvageJammedTrap")&&!f.Act(trap,"SalvageJammedTrap")&&f.Zone.GetEntityCell(trap)==null&&f.Count("IronSpikeComponent")==1&&f.Count("SalvagedTimber")==0;});
                Run(ctx,"cloth_cover_exchanged_for_finite_cord",f=>{var screen=f.Place("FrontierClothScreen");bool solid=screen.GetPart<PhysicsPart>().Solid;return solid&&f.Act(screen,"StripClothScreen")&&f.Zone.GetEntityCell(screen)==null&&f.Count("KnotflaxCord")==2&&!f.Act(screen,"StripClothScreen");});
                Run(ctx,"loaded_locker_moves_with_exact_cargo",f=>
                {
                    var chest=f.Place("Chest");chest.AddPart(new HandlingPart{Weight=40,Carryable=false});chest.AddPart(new ContainerLoadPart());var goods=f.Factory.CreateEntity("HealingTonic");Require(chest.GetPart<ContainerPart>().AddItem(goods),"cargo");f.Actor.GetStat("Strength").BaseValue=30;int weight=DragRules.WeightOf(chest);
                    if(DragSystem.TryGrab(f.Actor,chest,f.Zone)!=DragVerdict.Ok)return false;bool moved=f.Move(-1,0)&&f.Move(0,-1);DragSystem.Release(f.Actor);return moved&&f.Zone.GetEntityPosition(chest)==(9,10)&&chest.GetPart<ContainerPart>().Contents.Contains(goods)&&goods.GetPart<PhysicsPart>().InInventory==chest&&weight>40&&DragRules.WeightOf(chest)==weight;
                });
                Run(ctx,"paid_pass_ends_after_actual_entry_exit",f=>{var guard=f.Npc<LocalPassagePermitPart>();var post=f.Place("SoddenPassagePost",12,9);var role=new SpreadTerritoryPart();guard.AddPart(role);Require(role.Configure(f.Zone,post,11,9,14,11,2),"guard duty");if(!f.Act(guard,"PermitPassage"))return false;var permit=guard.GetPart<LocalPassagePermitPart>();bool before=permit.Allows(f.Actor,f.Zone);return before&&f.Move(1,1)&&f.Actor.GetPart<LocalPassageTokenPart>().Entered&&f.Move(-1,0)&&!permit.Allows(f.Actor,f.Zone)&&f.Actor.GetPart<LocalPassageTokenPart>().Spent&&TradeSystem.GetDrams(f.Actor)==96;});
                Run(ctx,"gate_controls_real_finite_grazer",f=>
                {
                    // A controlled copy of the authored gate/ring; the source fixture separately verifies cold placement.
                    f.Zone.MoveEntity(f.Actor,35,10);Entity gate=null;
                    for(int y=-2;y<=2;y++)for(int x=-2;x<=2;x++)if(Math.Abs(x)==2||Math.Abs(y)==2){var e=f.Place(x==2&&y==0?"FrontierPenGate":"Hedge",20+x,10+y);if(x==2&&y==0)gate=e;}
                    var grazer=f.Place("ReedbackGrazer",20,10);var food=f.Place("RipeCropRow",24,10);var reserve=f.Place("RipeCropRow",24,12);var role=grazer.GetPart<SpreadGrazerPart>();Require(role.ConfigureForage(f.Zone,food,reserve),"pen forage");grazer.AddPart(new GrazerPenPart{Gate=gate});var brain=grazer.GetPart<BrainPart>();
                    for(int i=0;i<30;i++)role.TakeIdleAction(brain,f.Zone);bool closed=!role.Fed&&role.ApproachAttempts==0;f.Zone.MoveEntity(f.Actor,23,10);bool opened=f.Act(gate,DoorPart.OpenCommand);f.Zone.MoveEntity(f.Actor,35,10);for(int i=0;i<24&&!role.Fed;i++)role.TakeIdleAction(brain,f.Zone);
                    return closed&&opened&&role.Fed&&food.GetPart<FieldHarvestPart>().Harvested&&!reserve.GetPart<FieldHarvestPart>().Harvested;
                });
                Run(ctx,"collector_trades_exact_carried_find",f=>{var bird=f.Npc<CollectorBarterPart>("Magpie");var role=bird.GetPart<SpreadCollectorPart>();if(role==null){role=new SpreadCollectorPart();bird.AddPart(role);}var home=f.Place("Chest",14,10);var find=f.Place("Hatchet",12,10);Require(role.Configure(f.Zone,home,find),"collector role");Require(f.Zone.RemoveEntity(find)&&bird.GetPart<InventoryPart>().AddObject(find),"controlled already-collected find");role.Phase=SpreadCollectorPhase.Carrying;var food=f.Carry("CandyCarrot");return f.Act(bird,"BarterCollector|"+food.ID)&&f.Pack.Objects.Contains(find)&&role.Phase==SpreadCollectorPhase.Stopped&&!home.GetPart<ContainerPart>().Contents.Contains(find)&&f.Count("CandyCarrot")==0;});
                Run(ctx,"finite_library_study_and_exact_return",f=>{var shelf=f.Place("QuillholdLoanShelf");var book=f.Factory.CreateEntity("QuillholdLoanWardGleam");Require(shelf.GetPart<InventoryPart>().AddObject(book),"loan shelf stock");int cost=RentalSystem.GetRentalCost(book,f.Actor,shelf);string skill=book.GetPart<GrimoirePart>().SkillClassName;return f.Act(shelf,"BorrowVolume|"+book.ID)&&f.Act(book,"ReadGrimoire")&&f.Actor.GetPart<SkillsPart>().HasSkill(skill)&&f.Act(shelf,"ReturnRental|"+book.ID)&&shelf.GetPart<InventoryPart>().Objects.Contains(book)&&!book.HasPart<RentalPart>()&&RentalSystem.GetInk(f.Actor)==100-cost+(int)Math.Floor(cost*RentalSystem.REFUND_FRACTION);});
                Run(ctx,"counter_key_opens_same_finite_cache",f=>{var chest=f.Place("CounterStoreChest");var good=f.Factory.CreateEntity("HealingTonic");Require(chest.GetPart<ContainerPart>().AddItem(good),"finite keyed stock");var key=f.Carry("CounterStoreKey");return f.Act(chest,"Unlock")&&!chest.GetPart<ContainerPart>().IsLocked&&chest.GetPart<ContainerPart>().Contents.Single()==good&&f.Pack.Objects.Contains(key);});
                Run(ctx,"replacement_save_keeps_service_and_depletion_graph",f=>
                {
                    var chest=f.Place("WellmeetGuestLocker");f.Actor.ApplyEffect(new UnderTheClothEffect{ExpiryTick=100});Require(f.Act(chest,"ClaimGuestLocker"),"claimed before save");f.Actor.RemoveEffect(typeof(UnderTheClothEffect));var screen=f.Place("FrontierClothScreen",10,11);Require(f.Act(screen,"StripClothScreen"),"stripped before save");var jar=f.Place("BeetleJar",9,10);jar.AddPart(new RecoverableLampPart());Require(f.Act(jar,"RecoverLamp")&&InventorySystem.Equip(f.Actor,jar),"held before save");var loaded=f.Save(RunId);var z=loaded.ZoneManager.ActiveZone;var c=z.GetReadOnlyEntities().Single(e=>e.ID==chest.ID);var held=loaded.Player.GetPart<InventoryPart>().EquippedItems.Values.Single(e=>e.ID==jar.ID);
                    return loaded.Player!=f.Actor&&c!=chest&&!c.GetPart<ContainerPart>().IsLocked&&c.GetPart<GuestLockerPart>().ClaimedBy==loaded.Player&&!z.GetReadOnlyEntities().Any(e=>e.ID==screen.ID||e.ID==jar.ID)&&loaded.Player.GetPart<InventoryPart>().FindEquippedBodyPart(held)!=null&&loaded.Player.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="KnotflaxCord").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1)==2;
                });
            }
        }
        void Run(ScenarioContext ctx,string name,Func<Fixture,bool> act)
        {
            bool pass=false;string error=null;Fixture f=null;
            try{f=new Fixture(ctx.Factory);pass=act(f);}catch(Exception ex){error=ex.GetType().Name+": "+ex.Message;}
            Cases++;if(!pass)Failures++;Audit.Add((pass?"PASS ":"FAIL ")+name+(error==null?"":" — "+error));Observations.Add(new{name,passed=pass,ticks=f?.Turns.TickCount,paidCommands=f?.PaidCommands,error,scope="controlled detached fixture; direct command/owner turn, not ordinary discovery"});
        }
        static void Require(bool value,string what){if(!value)throw new InvalidOperationException(what);}
        sealed class Fixture
        {
            internal readonly EntityFactory Factory;internal Zone Zone;internal readonly Entity Actor;internal readonly TurnManager Turns;internal int PaidCommands;
            internal InventoryPart Pack=>Actor.GetPart<InventoryPart>();
            internal Fixture(EntityFactory factory)
            {Factory=factory;Zone=new Zone("Overworld.10.10.0");SettlementRuntime.ActiveZone=Zone;Actor=Factory.CreateEntity("Player");var brain=Actor.GetPart<BrainPart>();if(brain!=null)Actor.RemovePart(brain);Require(Zone.AddEntity(Actor,10,10),"actor");Pack.MaxWeight=500;TradeSystem.SetDrams(Actor,100);RentalSystem.SetInk(Actor,100);Turns=new TurnManager();Turns.AddEntity(Actor);Require(Turns.ProcessUntilPlayerTurn()==Actor,"owner turn");MessageLog.TickProvider=()=>Turns.TickCount;}
            internal Entity Place(string bp,int x=11,int y=10){var e=Factory.CreateEntity(bp);Require(e!=null&&Zone.AddEntity(e,x,y),bp);return e;}
            internal Entity Carry(string bp){var e=Factory.CreateEntity(bp);Require(e!=null&&Pack.AddObject(e),bp);return e;}
            internal Entity Npc<T>(string bp="PeatCutter")where T:Part,new(){var e=Place(bp);if(!e.HasPart<InventoryPart>())e.AddPart(new InventoryPart());e.GetPart<InventoryPart>().MaxWeight=500;var b=e.GetPart<BrainPart>();b.Target=null;b.Passive=true;b.Staying=true;e.AddPart(new T());Require(!FactionManager.IsHostile(e,Actor),"willing NPC");return e;}
            internal Entity Loan(Entity owner,string bp){var e=Carry(bp);e.GetPart<StackerPart>().MaxStack=1;e.AddPart(new RentalPart{InkPaid=12,LessorBlueprintName=owner.BlueprintName});return e;}
            internal int Count(string bp)=>Pack.Objects.Where(e=>e.BlueprintName==bp).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
            internal bool Act(Entity source,string command){if(!InventorySystem.PerformAction(Actor,source,command,Zone))return false;Pay();return true;}
            internal bool Move(int x,int y){if(!MovementSystem.TryMove(Actor,Zone,x,y))return false;Pay();return true;}
            void Pay(){int before=Turns.TickCount;Turns.EndTurn(Actor,Zone);Require(Turns.ProcessUntilPlayerTurn()==Actor&&Turns.TickCount>before,"paid owner action");PaidCommands++;}
            internal void NewZone(string id){Zone.RemoveEntity(Actor);Zone=new Zone(id);SettlementRuntime.ActiveZone=Zone;Require(Zone.AddEntity(Actor,10,10),"detached source zone");}
            internal OverworldZoneManager Manager(params Zone[] others){var m=OverworldZoneManager.CreateDetached(Factory,64);m.ReplaceLoadedState(others.Concat(new[]{Zone}).ToDictionary(z=>z.ZoneID),Zone.ZoneID,new Dictionary<string,List<ZoneConnection>>());return m;}
            internal GameSessionState Save(string id){var m=Manager();using(var bytes=new MemoryStream()){GameSessionState.Capture(id,"controlled exploration bench",m,Turns,Actor).Save(new SaveWriter(bytes));bytes.Position=0;return GameSessionState.Load(new SaveReader(bytes,null));}}
        }
        sealed class DetachedScope : IDisposable
        {
            readonly EntityFactory exploration = SecondExplorationActions.Factory;
            readonly TurnManager active = TurnManager.Active; readonly Entity world = TurnManager.World; readonly Zone zone = SettlementRuntime.ActiveZone;
            readonly SettlementManager settlement = SettlementManager.Current;
            readonly CavesOfOoo.Data.EntityFactory crop = CropSystem.Factory, seed = SeedPart.Factory, harvest = HarvestablePart.Factory, reaction = MaterialReactionResolver.Factory;
            readonly Action<string> onMessage = MessageLog.OnMessage; readonly Func<int> tickProvider = MessageLog.TickProvider;
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries(); readonly List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue;
            readonly List<SpellFxSequence> spells = SpellFxBus.Drain(); readonly List<AsciiFxRequest> ascii = AsciiFxBus.Drain();
            readonly Queue<AsciiFxRequest> queue; readonly FieldInfo cosmeticField; readonly int cosmeticSerial;
            public DetachedScope()
            {
                queue = (Queue<AsciiFxRequest>)typeof(AsciiFxBus).GetField("PendingRequests", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                cosmeticField = typeof(SpellFxCapture).GetField("_cosmeticSerial", BindingFlags.NonPublic | BindingFlags.Static); cosmeticSerial = (int)cosmeticField.GetValue(null);
                TurnManager.World = null; MessageLog.OnMessage = null; typeof(SettlementManager).GetProperty("Current").SetValue(null, null);
            }
            public void Dispose()
            {
                foreach (var request in AsciiFxBus.Drain()) AsciiFxBus.Release(request); foreach (var request in ascii) queue.Enqueue(request);
                SpellFxBus.Clear(); foreach (var spell in spells) SpellFxBus.Emit(spell); cosmeticField.SetValue(null, cosmeticSerial);
                MessageLog.Restore(messages, announcements, flash, serial); MessageLog.OnMessage = onMessage; MessageLog.TickProvider = tickProvider;
                TurnManager.World = world; SettlementRuntime.ActiveZone = zone; typeof(TurnManager).GetProperty("Active").SetValue(null, active); typeof(SettlementManager).GetProperty("Current").SetValue(null, settlement);
                SecondExplorationActions.Factory = exploration; CropSystem.Factory = crop; SeedPart.Factory = seed; HarvestablePart.Factory = harvest; MaterialReactionResolver.Factory = reaction;
            }
        }
    }
}
