using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Actual item/anatomy fixture; combat RNG/stat/skill configuration is explicit.
    /// Full combat APIs are the attack stimuli. N/F5/F6/L use real native input.</summary>
    [Scenario(name: "Mortal Death Audit", category: "Combat",
        description: "Verify sourced Axe mortality, committed death, and native healthy-save recovery.")]
    public sealed class GameAuditMortalDeathBench : IScenario
    {
        public string RunId { get; private set; }
        public Entity Enemy, Victim, PlayerAxe, EnemyAxe, Boots;
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            if (Application.isPlaying && string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Launch this audit through its isolated batch.");
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            foreach (string bp in new[] { "Villager", "Battleaxe", "IronshodBoots", "StoneFloor", "CreatureCorpse" })
                Require(ctx.Factory.Blueprints.ContainsKey(bp), "Missing actual blueprint: " + bp);
            var player = ctx.PlayerEntity;
            ConversationManager.EndConversation(); DragSystem.Release(player);
            player.GetPart<Body>().DropAllEquipment(ctx.Zone);
            foreach (var item in player.GetPart<InventoryPart>().Objects.ToArray()) player.GetPart<InventoryPart>().RemoveObject(item);
            player.GetPart<StatusEffectsPart>()?.RemoveAllEffects();
            foreach (var entity in ctx.Zone.GetAllEntities().ToArray())
                if (entity != player) { ctx.Turns.RemoveEntity(entity); ctx.Zone.RemoveEntity(entity); }
            ctx.Zone.GenReservedCells.Clear();
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            {
                ctx.Zone.TileState.Clear(x, y);
                if (x > 0 && x < Zone.Width - 1 && y > 0 && y < Zone.Height - 1) Place(ctx.Factory.CreateEntity("StoneFloor"), x, y);
            }
            ctx.Zone.RemoveEntity(player); Place(player, 20, 12);
            Enemy = ctx.Factory.CreateEntity("Villager"); Victim = ctx.Factory.CreateEntity("Villager");
            Require(Enemy != null && Victim != null, "Actual Villager creation failed.");
            Enemy.GetPart<RenderPart>().DisplayName = "mortal audit axe source";
            Victim.GetPart<RenderPart>().DisplayName = "mortal audit victim";
            foreach (var actor in new[] { player, Enemy, Victim })
            {
                var brain = actor.GetPart<BrainPart>(); if (brain != null) actor.RemovePart(brain); // Explicit inert arena setup.
                var body = actor.GetPart<Body>(); var inventory = actor.GetPart<InventoryPart>();
                Require(body != null && inventory != null && body.DismemberedParts.Count == 0, "Fresh real Humanoid/storage required.");
                body.DropAllEquipment(ctx.Zone);
                foreach (var item in inventory.Objects.ToArray()) inventory.RemoveObject(item); // Remove fixture NPC trader stock before exact gear staging.
                Reset(actor, "Strength", 16, 40); Reset(actor, "Agility", 16, 40); Reset(actor, "Speed", 100, 200); Reset(actor, "Hitpoints", 100, 100);
                actor.GetPart<ArmorPart>().AV = 6; inventory.MaxWeight = 150; inventory.RefreshHandlingCarryPenalty();
                actor.GetPart<StatusEffectsPart>()?.RemoveAllEffects();
                if (actor.GetPart<StatusEffectsPart>() == null) actor.AddPart(new StatusEffectsPart());
                if (actor.GetPart<SkillsPart>() == null) actor.AddPart(new SkillsPart());
                foreach (var skill in actor.GetPart<SkillsPart>().SkillList.ToArray())
                    Require(actor.GetPart<SkillsPart>().RemoveSkill(skill, "GA03h-fixture"), "Remove unrelated fixture skill failed.");
            }
            foreach (var actor in new[] { player, Enemy })
            {
                Require(actor.GetPart<SkillsPart>().AddSkill(new Axe_Dismember(), "GA03h-fixture"), "Dismember skill grant failed.");
                Require(actor.GetPart<SkillsPart>().AddSkill(new Axe_Decapitate(), "GA03h-fixture"), "Decapitate marker grant failed.");
            }
            Reset(player, "Experience", 0, 999999); var level = player.GetStat("Level"); level.BaseValue = 1; level.Bonus = level.Penalty = level.Boost = 0;
            player.GetStat("Speed").Penalty = 7; // Unrelated penalty must survive death equipment cleanup.
            player.SetIntProperty("GA03hCheckpoint", 0); PlayerReputation.Set("Villagers", 0);
            PlayerAxe = ctx.Factory.CreateEntity("Battleaxe"); EnemyAxe = ctx.Factory.CreateEntity("Battleaxe"); Boots = ctx.Factory.CreateEntity("IronshodBoots");
            Require(PlayerAxe != null && EnemyAxe != null && Boots != null, "Actual audit gear creation failed.");
            Require(new GlowQuartzTinkerModification().Apply(PlayerAxe, out var reason), "Actual GlowQuartz fixture mod failed: " + reason);
            Equip(player, PlayerAxe, "Hand", Laterality.LEFT); Equip(Enemy, EnemyAxe, "Hand", Laterality.LEFT); Equip(player, Boots, "Feet", 0);
            Place(Enemy, 21, 12); Place(Victim, 20, 13);
            ctx.Turns.AddEntity(player); ctx.Turns.AddEntity(Enemy); ctx.Turns.AddEntity(Victim); ctx.Turns.ProcessUntilPlayerTurn();
            ZoneRenderHooks.MarkFullDirty("GameAuditMortalDeathBench");
            if (Application.isPlaying) new GameObject("Mortal Death Native Audit").AddComponent<GameAuditMortalDeathBenchPlayer>().Initialize(ctx, this);
            void Place(Entity entity, int x, int y) { Require(entity != null && ctx.Zone.AddEntity(entity, x, y), "Arena placement refused."); }
        }
        private static void Reset(Entity actor, string name, int value, int maximum)
        { var stat = actor.GetStat(name); Require(stat != null, "Missing actual stat: " + name); stat.BaseValue = value; stat.Bonus = stat.Penalty = stat.Boost = 0; stat.Min = 0; stat.Max = maximum; }
        private static void Equip(Entity actor, Entity item, string type, int laterality)
        {
            var part = actor.GetPart<Body>().GetParts().Single(p => p.Type == type && p.GetLaterality() == laterality);
            Require(actor.GetPart<InventoryPart>().AddObject(item) && InventorySystem.Equip(actor, item, part), "Normal fixture equip refused.");
        }
        public void Check(string label, bool passed)
        { Cases++; if (!passed) Failures++; Audit.Add((passed ? "PASS " : "FAIL ") + label); Require(passed, "Mortal native audit failed: " + label); }
        public static void Require(bool passed, string reason) { if (!passed) throw new InvalidOperationException(reason); }
    }

    /// <summary>Temporary observer, attached only AFTER a checkpoint. Never saved.</summary>
    public sealed class GameAuditMortalProbePart : Part
    {
        public override string Name => nameof(GameAuditMortalProbePart);
        public bool Veto, DiedMarked, ResidentAtDied;
        public int Before, After, Died, BeforeHp, DiedBase, DiedValue, DealtCount, DealtAmount, HpAfterDamage, Bleeds;
        public Entity Killer, Target, DamageTarget, BleedSource;
        public Zone ZoneAtDied;
        public BodyPart Selected;
        public int DeathX, DeathY;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeDismember") { Before++; Selected = e.GetParameter<BodyPart>("Part"); BeforeHp = ParentEntity.GetStatValue("Hitpoints"); return !Veto; }
            if (e.ID == "AfterDismember") After++;
            if (e.ID == "DamageDealt")
            { DealtCount++; DealtAmount += e.GetIntParameter("Amount"); DamageTarget = e.GetParameter<Entity>("Defender"); HpAfterDamage = DamageTarget.GetStatValue("Hitpoints"); }
            if (e.ID == "EffectApplied" && e.GetParameter<Effect>("Effect") is BleedingEffect)
            { Bleeds++; BleedSource = e.GetParameter<Entity>("Source"); }
            if (e.ID == "Died")
            {
                Died++; Killer = e.GetParameter<Entity>("Killer"); Target = e.GetParameter<Entity>("Target"); ZoneAtDied = e.GetParameter<Zone>("Zone");
                DiedBase = ParentEntity.GetStat("Hitpoints").BaseValue; DiedValue = ParentEntity.GetStatValue("Hitpoints"); DiedMarked = CombatSystem.IsDeathHandled(ParentEntity);
                var cell = ZoneAtDied?.GetEntityCell(ParentEntity); ResidentAtDied = cell != null; DeathX = cell?.X ?? -1; DeathY = cell?.Y ?? -1;
            }
            return true;
        }
    }
}
