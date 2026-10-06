using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Controlled detached command/scheduler/save witness. Authored items,
    /// granted skills/conditions and fixed geometry do not prove normal discovery,
    /// keyboard combat, campaign balance or presentation quality.</summary>
    [Scenario(name: "Fifty Combat Audit", category: "Combat", description: "Movement costs, finite repairs, temporary medicine and delayed kill credit.")]
    public sealed class FiftyCombatRuntimeBench : IScenario
    {
        public const int ExpectedCases = 12;
        public string RunId { get; private set; }
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public readonly List<object> Observations = new List<object>();
        public void Apply(ScenarioContext ctx)
        {
            RunId=Guid.NewGuid().ToString("N"); Cases=Failures=0; Audit.Clear(); Observations.Clear();
            var oldFactory=MaterialReactionResolver.Factory;
            try
            {
                using(var scope=new DetachedScope())
                {
                    MaterialReactionResolver.Factory=ctx.Factory;
                    var zone=new Zone("Overworld.0.0.0"); var actor=Creature("combat witness",100); actor.SetTag("Player");
                    actor.AddPart(new InventoryPart { MaxWeight=1000 }); var body=new Body(); actor.AddPart(body); body.SetBody(AnatomyFactory.CreateHumanoid());
                    actor.AddPart(new SkillsPart()); actor.AddPart(new ActivatedAbilitiesPart());
                    var disengage=new ShortBlades_Disengage(); var vault=new Acrobatics_Vault();
                    Require(actor.GetPart<SkillsPart>().AddSkill(disengage) && actor.GetPart<SkillsPart>().AddSkill(vault),"granted fixture abilities");
                    Require(zone.AddEntity(actor,10,10),"actor placement");
                    var dagger=ctx.Factory.CreateEntity("Dagger"); Carry(actor,dagger); Require(InventorySystem.Equip(actor,dagger),"ordinary dagger equip");
                    var strength=ctx.Factory.CreateEntity("StrengthTonic"); strength.GetPart<StackerPart>().StackCount=2; Carry(actor,strength);
                    Check("authored_items_and_fixed_owner",dagger.GetPart<RepairablePart>()?.PortableEquipment==true
                        && strength.GetPart<TonicPart>().Duration==20 && actor.GetStatValue("Speed")==100,
                        new { dagger=dagger.ID,tonic=strength.ID,tonicTurns=strength.GetPart<TonicPart>().Duration,controlled=true });
                    var turns=new TurnManager(); turns.AddEntity(actor); Require(turns.ProcessUntilPlayerTurn()==actor,"owner scheduling");
                    var wall=new Entity(); wall.SetTag("Solid"); Require(zone.AddEntity(wall,11,10),"blocking fixture");
                    int tick=turns.TickCount; var ability=actor.GetPart<ActivatedAbilitiesPart>().GetAbility(disengage.ActivatedAbilityID);
                    bool escaped=Cast(actor,zone,"CommandDisengage"); if(escaped)Advance(turns,actor,zone);
                    Check("blocked_disengage_is_free",!escaped && turns.TickCount==tick && ability.CooldownRemaining==0 && zone.GetEntityPosition(actor)==(10,10),
                        new { handled=escaped,beforeTick=tick,afterTick=turns.TickCount,cooldown=ability.CooldownRemaining });
                    Require(zone.RemoveEntity(wall),"open controlled lane");
                    escaped=Cast(actor,zone,"CommandDisengage"); int cooldownOnCast=ability.CooldownRemaining;
                    if(escaped)Advance(turns,actor,zone);
                    Check("clear_disengage_moves_and_pays",escaped && zone.GetEntityPosition(actor)==(13,10) && turns.TickCount==tick+10
                        && cooldownOnCast==ShortBlades_Disengage.COOLDOWN && ability.CooldownRemaining==ShortBlades_Disengage.COOLDOWN-1,
                        new { handled=escaped,x=zone.GetEntityPosition(actor).x,y=zone.GetEntityPosition(actor).y,beforeTick=tick,afterTick=turns.TickCount,cooldownOnCast,cooldown=ability.CooldownRemaining });
                    Require(actor.ApplyEffect(new RootedEffect(),actor,zone),"controlled root condition"); tick=turns.TickCount;
                    bool leaped=Cast(actor,zone,"CommandVault"); if(leaped)Advance(turns,actor,zone);
                    Check("rooted_vault_is_free",!leaped && turns.TickCount==tick && zone.GetEntityPosition(actor)==(13,10)
                        && actor.GetPart<ActivatedAbilitiesPart>().GetAbility(vault.ActivatedAbilityID).CooldownRemaining==0,new { handled=leaped,tick=turns.TickCount });
                    actor.RemoveEffect<RootedEffect>();
                    int intact=dagger.GetPart<MeleeWeaponPart>().HitBonus;
                    Require(dagger.ApplyEffect(new BrokenEffect(),actor,zone),"controlled gear damage");
                    Check("broken_authored_gear_has_penalty",dagger.GetPart<MeleeWeaponPart>().HitBonus==intact-2 && InventorySystem.IsEquipped(actor,dagger),
                        new { intact,broken=dagger.GetPart<MeleeWeaponPart>().HitBonus,stillEquipped=InventorySystem.IsEquipped(actor,dagger) });
                    Require(InventorySystem.UnequipItem(actor,dagger),"ordinary unequip before repair"); tick=turns.TickCount;
                    bool repaired=InventorySystem.PerformAction(actor,dagger,RepairablePart.RepairCommand,zone); if(repaired)Advance(turns,actor,zone);
                    Check("repair_without_material_is_free",!repaired && turns.TickCount==tick && dagger.HasEffect<BrokenEffect>(),new { repaired,tick=turns.TickCount });
                    var material=ctx.Factory.CreateEntity("SteelBladeComponent"); material.GetPart<StackerPart>().StackCount=2; Carry(actor,material);
                    repaired=InventorySystem.PerformAction(actor,dagger,RepairablePart.RepairCommand,zone); if(repaired)Advance(turns,actor,zone);
                    Check("repair_pays_one_finite_component",repaired && !dagger.HasEffect<BrokenEffect>() && dagger.GetPart<MeleeWeaponPart>().HitBonus==intact
                        && material.GetPart<StackerPart>().StackCount==1 && turns.TickCount==tick+10,
                        new { repaired,stock=material.GetPart<StackerPart>().StackCount,hit=dagger.GetPart<MeleeWeaponPart>().HitBonus,beforeTick=tick,afterTick=turns.TickCount });
                    int before=actor.GetStatValue("Strength"); tick=turns.TickCount;
                    bool drank=InventorySystem.PerformAction(actor,strength,"ApplyTonic",zone); if(drank)Advance(turns,actor,zone);
                    Check("finite_tonic_paid_owner_turn",drank && actor.GetStatValue("Strength")==before+4 && actor.GetEffect<TonicStatSurgeEffect>()?.Duration==20
                        && strength.GetPart<StackerPart>().StackCount==1 && turns.TickCount==tick+10,
                        new { drank,strength=actor.GetStatValue("Strength"),turns=actor.GetEffect<TonicStatSurgeEffect>()?.Duration,beforeTick=tick,afterTick=turns.TickCount });
                    for(int i=0;i<5;i++)Advance(turns,actor,zone);
                    drank=InventorySystem.PerformAction(actor,strength,"ApplyTonic",zone); if(drank)Advance(turns,actor,zone);
                    Check("second_dose_refreshes_without_stacking",drank && actor.GetStatValue("Strength")==before+4
                        && actor.GetEffect<TonicStatSurgeEffect>()?.Duration==20 && !actor.GetPart<InventoryPart>().Objects.Contains(strength),
                        new { drank,strength=actor.GetStatValue("Strength"),turns=actor.GetEffect<TonicStatSurgeEffect>()?.Duration });
                    tick=turns.TickCount; for(int i=0;i<20;i++)Advance(turns,actor,zone);
                    Check("surge_expires_on_twenty_owner_turns",actor.GetEffect<TonicStatSurgeEffect>()==null && actor.GetStatValue("Strength")==before
                        && turns.TickCount==tick+200,new { strength=actor.GetStatValue("Strength"),beforeTick=tick,afterTick=turns.TickCount });
                    var victim=Creature("poison witness",1); victim.Statistics["XPValue"]=new Stat { Owner=victim,Name="XPValue",BaseValue=7,Max=100 };
                    Require(zone.AddEntity(victim,16,10),"poison victim fixture"); turns.AddEntity(victim);
                    var poison=ctx.Factory.CreateEntity("PoisonTonic"); poison.GetPart<StackerPart>().StackCount=2; Carry(actor,poison);
                    bool thrown=InventorySystem.ExecuteCommand(new ThrowItemCommand(poison,16,10,new Random(11)),actor,zone).Success;
                    Check("thrown_poison_has_exact_source_and_cost",thrown && victim.GetEffect<PoisonedEffect>()?.DamageSource==actor
                        && victim.GetStatValue("Hitpoints")==1 && poison.GetPart<StackerPart>().StackCount==1,
                        new { thrown,source=victim.GetEffect<PoisonedEffect>()?.DamageSource?.ID,victim=victim.ID,hp=victim.GetStatValue("Hitpoints"),stock=poison.GetPart<StackerPart>().StackCount });
                    var manager=new OverworldZoneManager(null,64); manager.ReplaceLoadedState(new Dictionary<string,Zone>{{zone.ZoneID,zone}},zone.ZoneID,new Dictionary<string,List<ZoneConnection>>());
                    GameSessionState loaded;
                    using(var bytes=new MemoryStream())
                    { GameSessionState.Capture(RunId,"controlled combat witness",manager,turns,actor).Save(new SaveWriter(bytes)); bytes.Position=0; loaded=GameSessionState.Load(new SaveReader(bytes,null)); }
                    var restored=loaded.Player; var restoredZone=loaded.ZoneManager.ActiveZone; var restoredVictim=restoredZone.GetReadOnlyEntities().FirstOrDefault(e=>e.ID==victim.ID);
                    bool sourceRestored=restored!=actor && restoredVictim!=victim && restoredVictim?.GetEffect<PoisonedEffect>()?.DamageSource==restored;
                    if(thrown)
                    {
                        Advance(loaded.TurnManager,restored,restoredZone); // pay the thrown action
                        Advance(loaded.TurnManager,restored,restoredZone); // one ordinary wait lets the victim take its pending turn
                    }
                    Check("saved_poison_finisher_retains_kill_credit",sourceRestored && restored.GetStatValue("Experience")==7
                        && restoredVictim.GetStatValue("Hitpoints")<=0,
                        new { sourceRestored,xp=restored.GetStatValue("Experience"),victimHP=restoredVictim?.GetStatValue("Hitpoints") });
                }
            }
            finally { MaterialReactionResolver.Factory=oldFactory; }
        }
        static Entity Creature(string name,int hp)
        {
            var e=new Entity { ID=Guid.NewGuid().ToString("N"),BlueprintName=name }; e.SetTag("Creature");
            e.AddPart(new RenderPart { DisplayName=name }); e.AddPart(new PhysicsPart()); e.AddPart(new StatusEffectsPart()); e.AddPart(new ArmorPart());
            foreach(var value in new [] { ("Hitpoints",hp),("Strength",16),("Agility",16),("Toughness",16),("Speed",100),("Experience",0) })
                e.Statistics[value.Item1]=new Stat { Owner=e,Name=value.Item1,BaseValue=value.Item2,Min=0,Max=100000 };
            return e;
        }
        static void Carry(Entity actor,Entity item) { Require(item!=null && actor.GetPart<InventoryPart>().AddObject(item),"finite carried item"); }
        static bool Cast(Entity actor,Zone zone,string name)
        {
            var e=GameEvent.New(name); e.SetParameter("Zone",(object)zone); e.SetParameter("RNG",(object)new Random(11)); e.SetParameter("SourceCell",(object)zone.GetEntityCell(actor)); e.SetParameter("DirectionX",1);
            // Handled means success, so this caller must spend the action. BlocksTurnAdvance only
            // defers scheduler advancement while a visual effect is in flight;
            // neither of these movement commands defers its turn that way.
            try { actor.FireEvent(e); return e.Handled; } finally { e.Release(); }
        }
        static void Advance(TurnManager turns,Entity actor,Zone zone)
        { turns.EndTurn(actor,zone); Require(turns.ProcessUntilPlayerTurn()==actor,"owner turn retained"); }
        static void Require(bool value,string name) { if(!value)throw new InvalidOperationException("Combat audit fixture: "+name); }
        void Check(string name,bool passed,object observed)
        { Cases++; if(!passed)Failures++; Audit.Add((passed?"PASS ":"FAIL ")+name); Observations.Add(new { name,passed,state=observed }); }
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
            readonly Queue<AsciiFxRequest> queue;
            readonly FieldInfo cosmeticField;
            readonly int cosmeticSerial;
            public DetachedScope()
            {
                queue = (Queue<AsciiFxRequest>)typeof(AsciiFxBus).GetField("PendingRequests", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                cosmeticField = typeof(SpellFxCapture).GetField("_cosmeticSerial", BindingFlags.NonPublic | BindingFlags.Static);
                cosmeticSerial = (int)cosmeticField.GetValue(null);
                TurnManager.World = null; MessageLog.OnMessage = null;
            }
            public void Dispose()
            {
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
