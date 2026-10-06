using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
using NUnit.Framework;
using F = CavesOfOoo.Tests.MultiCellAbilityConsumerTests;

namespace CavesOfOoo.Tests
{
    internal sealed class FiftySecondCombatFixture : IDisposable
    {
        readonly FieldMedicineFixture environment = new FieldMedicineFixture();
        readonly EntityFactory oldWall = Cryomancy_GlacialWall.Factory;
        readonly EntityFactory oldReaction = MaterialReactionResolver.Factory;
        readonly EntityFactory oldLoadout = LoadoutPart.Factory;
        readonly Random oldLoadoutRng = LoadoutPart.Rng;
        public EntityFactory Factory => environment.Factory;
        public Zone Zone => environment.Zone;
        public readonly Random Rng = new FiftyCombatStatusTests.FixedRandom();
        public FiftySecondCombatFixture()
        {
            Cryomancy_GlacialWall.Factory = Factory;
            MaterialReactionResolver.Factory = Factory;
            LoadoutPart.Factory = Factory; LoadoutPart.Rng = Rng;
            FactionManager.Initialize();
        }
        public Entity Actor(BaseSkillPart skill = null, string weapon = "Cutting LongBlades", int x = 10, int y = 10)
        {
            var actor = F.ArmedActor(weapon);
            // The minimum-roll RNG's penetration die is -1. A +1 fixture sword
            // cannot penetrate AV0; Follow Through needs a real lethal-strike control.
            if (skill is LongBlades_FollowThrough)
                SkillCombatHelpers.FindEquippedWeaponOfClass(actor,"LongBlades").PenBonus=3;
            actor.AddPart(new SkillsPart()); actor.AddPart(new ActivatedAbilitiesPart());
            F.Place(Zone, actor, x, y);
            if(skill != null) Learn(actor, skill);
            return actor;
        }
        public Entity Target(int x = 11, int y = 10, int hp = 1000)
        { var e=F.Owner(creature:true); e.GetStat("Hitpoints").BaseValue=hp; F.Place(Zone,e,x,y); return e; }
        public Entity Wall(int x, int y, int hp = 0)
        {
            var e = F.Owner(); e.SetTag("Solid"); e.GetPart<PhysicsPart>().Solid=true;
            if(hp>0) e.AddPart(new DestructiblePart { HP=hp, MaxHP=hp });
            F.Place(Zone,e,x,y); return e;
        }
        public Entity Npc(int x=10,int y=10,string classes="Cryomancy_IceLance")
        {
            var actor = Factory.CreateEntity("Creature"); Assert.NotNull(actor);
            actor.ID=Guid.NewGuid().ToString("N"); actor.SetTag("Faction","OutlandRaiders");
            actor.GetStat("Hitpoints").BaseValue=500; actor.GetStat("Hitpoints").Max=500;
            actor.AddPart(new CombatTacticsPart { SkillClasses=classes,AbilityChance=100 });
            actor.FireEventAndRelease(GameEvent.New("ObjectCreated"));
            F.Place(Zone,actor,x,y);
            actor.GetPart<BrainPart>().CurrentZone=Zone; actor.GetPart<BrainPart>().Rng=Rng;
            actor.GetPart<BrainPart>().FleeThreshold=0; return actor;
        }
        public Entity Hostile(Entity npc,int x=13,int y=10)
        { var target=Target(x,y); target.SetTag("Player"); npc.GetPart<BrainPart>().SetPersonallyHostile(target); return target; }
        public void Act(Entity actor, Entity target)
        {
            var brain=actor.GetPart<BrainPart>(); brain.CurrentZone=Zone; brain.Target=target;
            if(!(brain.PeekGoal() is KillGoal)) brain.PushGoal(new KillGoal(target));
            actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
        }
        public static BaseSkillPart Skill(string name)
        {
            var type=typeof(BaseSkillPart).Assembly.GetType("CavesOfOoo.Skills."+name);
            Assert.NotNull(type,"Missing ordinary purchasable skill: "+name);
            return (BaseSkillPart)Activator.CreateInstance(type);
        }
        public static Part Part(string name)
        {
            var type=typeof(Entity).Assembly.GetType("CavesOfOoo.Core."+name);
            Assert.NotNull(type,"Missing gameplay part: "+name); return (Part)Activator.CreateInstance(type);
        }
        public static void Set(object target,string name,object value)
        {
            var field=target.GetType().GetField(name,BindingFlags.Public|BindingFlags.Instance);
            Assert.NotNull(field,"Missing authored option "+target.GetType().Name+"."+name); field.SetValue(target,value);
        }
        public static void Learn(Entity actor, BaseSkillPart skill)
            => Assert.True(actor.GetPart<SkillsPart>().AddSkill(skill,"fifty-ii-test"));
        public bool Cast(Entity actor,BaseSkillPart skill,Cell target=null,int dx=1,int dy=0,Random rng=null)
        {
            var e=GameEvent.New(skill.DeclareActivatedAbility(actor).Command);
            e.SetParameter("Zone",(object)Zone); e.SetParameter("RNG",(object)(rng??Rng));
            e.SetParameter("SourceCell",(object)Zone.GetEntityCell(actor)); e.SetParameter("DirectionX",dx); e.SetParameter("DirectionY",dy);
            e.SetParameter("Range",skill.DeclareActivatedAbility(actor).Range);
            if(target!=null)e.SetParameter("TargetCell",(object)target);
            try { actor.FireEvent(e); return e.Handled; } finally { e.Release(); }
        }
        public static int Cooldown(Entity actor,BaseSkillPart skill)
            => actor.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID).CooldownRemaining;
        public static void Ready(Entity actor)
        { foreach(var a in actor.GetPart<ActivatedAbilitiesPart>().AbilityList)a.CooldownRemaining=0; }
        public void Tick(Entity actor,int count=1)
        { for(int i=0;i<count;i++) FiftyCombatStatusTests.Tick(actor,Zone); }
        public static Effect Effect(Entity actor,string className)
            => actor.GetPart<StatusEffectsPart>()?.GetAllEffects().FirstOrDefault(e=>e.ClassName==className);
        public void Dispose()
        {
            Cryomancy_GlacialWall.Factory=oldWall; MaterialReactionResolver.Factory=oldReaction;
            LoadoutPart.Factory=oldLoadout; LoadoutPart.Rng=oldLoadoutRng;
            FactionManager.Reset(); environment.Dispose();
        }
    }
}
