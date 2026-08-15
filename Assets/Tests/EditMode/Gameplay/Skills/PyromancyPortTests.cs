using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The Pyromancy batch of the mutations→skills migration — six
    /// ported powers (Kindle, Kindle Flame, Ember Vein, Conflagration,
    /// Flaming Hands, Hearthwarm) plus the grimoire-teaches-skill flow.
    ///
    /// <para>All casts dispatch through <c>FireEvent</c> with the
    /// InputHandler's exact parameter conventions — never by calling
    /// OnCommand directly (the blind spot that let six rites ship
    /// dead).</para>
    /// </summary>
    public class PyromancyPortTests
    {
        [SetUp]
        public void SetUp()
        {
            Diag.ResetAll();
            MessageLog.Clear();
        }

        // ── Fixtures ─────────────────────────────────────────────────

        private static Entity Caster(Zone zone, int x, int y)
        {
            var e = new Entity { ID = "caster", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        private static T Learn<T>(Entity caster) where T : BaseSkillPart, new()
        {
            var skill = new T();
            Assert.IsTrue(caster.GetPart<SkillsPart>().AddSkill(skill),
                $"fixture: {typeof(T).Name} must attach");
            return skill;
        }

        private static Entity Snapjaw(Zone zone, int x, int y, int hp = 20)
        {
            var e = new Entity { ID = "snapjaw", BlueprintName = "Snapjaw" };
            e.Tags["Creature"] = "";
            e.AddPart(new PhysicsPart { Solid = true });
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            zone.AddEntity(e, x, y);
            return e;
        }

        /// <summary>Dry tinder: flammable, breakable, thermally live.</summary>
        private static Entity Bush(Zone zone, int x, int y)
        {
            var e = new Entity { ID = "bush", BlueprintName = "Bush" };
            e.AddPart(new PhysicsPart { Solid = false });
            e.AddPart(new DestructiblePart { HP = 6, MaxHP = 6 });
            e.AddPart(new MaterialPart { MaterialTagsRaw = "Organic,Plant", Combustibility = 0.6f });
            e.AddPart(new ThermalPart { FlameTemperature = 320f, HeatCapacity = 1.0f });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static (bool handled, bool blocks) Cast(
            Entity caster, Zone zone, string command,
            int dx = 0, int dy = 0, Cell targetCell = null)
        {
            var src = zone.GetEntityCell(caster);
            var cmd = GameEvent.New(command);
            cmd.SetParameter("Zone", (object)zone);
            cmd.SetParameter("RNG", (object)new System.Random(11));
            cmd.SetParameter("SourceCell", (object)src);
            cmd.SetParameter("DirectionX", dx);
            cmd.SetParameter("DirectionY", dy);
            cmd.SetParameter("Range", 5);
            if (targetCell != null) cmd.SetParameter("TargetCell", (object)targetCell);
            caster.FireEvent(cmd);
            bool handled = cmd.Handled;
            bool blocks = cmd.GetParameter<bool>("BlocksTurnAdvance");
            cmd.Release();
            return (handled, blocks);
        }

        // ── Kindle (projectile base proof) ───────────────────────────

        [Test]
        public void Kindle_BoltHitsACreatureAndChargesTheCooldown()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            var kindle = Learn<Pyromancy_Kindle>(caster);
            var snapjaw = Snapjaw(zone, 7, 5);

            var (handled, blocks) = Cast(caster, zone, "CommandKindle", dx: 1, dy: 0);

            Assert.IsTrue(handled, "the bolt flew");
            Assert.IsTrue(blocks, "projectile FX hold the turn");
            Assert.Less(snapjaw.GetStatValue("Hitpoints", 0), 20, "damage landed");
            var ability = caster.GetPart<ActivatedAbilitiesPart>()
                .GetAbility(kindle.ActivatedAbilityID);
            Assert.AreEqual(Pyromancy_Kindle.COOLDOWN, ability.CooldownRemaining);
        }

        [Test]
        public void Kindle_BoltHitsAndIgnitesABush()
        {
            // The correctness dividend the port exists for: the mutation
            // walk skipped scenery; the skill walk collects it, and the
            // 600J on-hit crosses a bush's 320° flame point in one hit.
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Pyromancy_Kindle>(caster);
            var bush = Bush(zone, 7, 5);

            Cast(caster, zone, "CommandKindle", dx: 1, dy: 0);

            Assert.Less(bush.GetPart<DestructiblePart>().HP, 6, "structural damage landed");
            Assert.IsTrue(bush.HasEffect<BurningEffect>(), "and the bush caught fire");
        }

        [Test]
        public void Kindle_NoDirectionIsFreeAndUnhandled()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            var kindle = Learn<Pyromancy_Kindle>(caster);

            var (handled, _) = Cast(caster, zone, "CommandKindle", dx: 0, dy: 0);

            Assert.IsFalse(handled, "no direction → the InputHandler prints the failure line");
            Assert.AreEqual(0, caster.GetPart<ActivatedAbilitiesPart>()
                .GetAbility(kindle.ActivatedAbilityID).CooldownRemaining, "and it cost nothing");
        }

        // ── Kindle Flame ─────────────────────────────────────────────

        [Test]
        public void KindleFlame_WarmsLowFlashpointTinder()
        {
            // The gate is flashpoint < 250°: tar, oil, paper — NOT a
            // 320° bush. Preserved from the mutation verbatim.
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Pyromancy_KindleFlame>(caster);
            var tar = new Entity { ID = "tar", BlueprintName = "TarSeep" };
            tar.AddPart(new ThermalPart { FlameTemperature = 200f, HeatCapacity = 1.0f });
            tar.AddPart(new MaterialPart { MaterialTagsRaw = "Liquid,Tar", Combustibility = 0.95f });
            zone.AddEntity(tar, 6, 5);
            float before = tar.GetPart<ThermalPart>().Temperature;

            var (handled, _) = Cast(caster, zone, "CommandKindleFlame",
                targetCell: zone.GetCell(6, 5));

            Assert.IsTrue(handled);
            Assert.Greater(tar.GetPart<ThermalPart>().Temperature, before,
                "150J of warmth arrived");
        }

        [Test]
        public void KindleFlame_A320DegreeBushIsBeyondIt()
        {
            // Documents the niche: the weakest fire cannot coax ordinary
            // greenery — only sub-250° flashpoints. A bush refusal is
            // free, same as an empty cell.
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Pyromancy_KindleFlame>(caster);
            Bush(zone, 6, 5); // FlameTemperature 320

            var (handled, _) = Cast(caster, zone, "CommandKindleFlame",
                targetCell: zone.GetCell(6, 5));

            Assert.IsFalse(handled, "320° is past the 250° gate — refused, free");
        }

        [Test]
        public void KindleFlame_AnEmptyCellIsAFreeRefusal()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            var kf = Learn<Pyromancy_KindleFlame>(caster);

            var (handled, _) = Cast(caster, zone, "CommandKindleFlame",
                targetCell: zone.GetCell(6, 5));

            Assert.IsFalse(handled, "nothing kindleable → refusal");
            Assert.AreEqual(0, caster.GetPart<ActivatedAbilitiesPart>()
                .GetAbility(kf.ActivatedAbilityID).CooldownRemaining,
                "the mutation only charged its cooldown when something was affected — preserved");
        }

        // ── Flaming Hands (level-freeze decision) ────────────────────

        [Test]
        public void FlamingHands_HitsEverythingInTheCellIncludingScenery()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Pyromancy_FlamingHands>(caster);
            var snapjaw = Snapjaw(zone, 6, 5);
            var bush = Bush(zone, 6, 5);

            var (handled, blocks) = Cast(caster, zone, "CommandFlamingHands",
                targetCell: zone.GetCell(6, 5));

            Assert.IsTrue(handled);
            Assert.IsTrue(blocks);
            Assert.Less(snapjaw.GetStatValue("Hitpoints", 0), 20, "creature hit");
            Assert.Less(bush.GetPart<DestructiblePart>().HP, 6, "scenery hit too");
        }

        [Test]
        public void FlamingHands_DamageIsFlatOneD4_TheLevelFreezePin()
        {
            // User decision (plan §5): freeze at level-1 numbers. 1d4
            // caps at 4 — a level-scaled 5×1d4 would exceed it. Pin the
            // ceiling so scaling cannot silently return.
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Pyromancy_FlamingHands>(caster);
            var snapjaw = Snapjaw(zone, 6, 5, hp: 30);

            Cast(caster, zone, "CommandFlamingHands", targetCell: zone.GetCell(6, 5));

            int dealt = 30 - snapjaw.GetStatValue("Hitpoints", 0);
            Assert.GreaterOrEqual(dealt, 1);
            Assert.LessOrEqual(dealt, 4, "flat 1d4 — the frozen level-1 figure");
        }

        [Test]
        public void FlamingHands_BlastingEmptySpaceStillConsumesTheCast()
        {
            // Faithful to the mutation: the flames flew, the cooldown is
            // spent, the message says so.
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            var fh = Learn<Pyromancy_FlamingHands>(caster);

            var (handled, _) = Cast(caster, zone, "CommandFlamingHands",
                targetCell: zone.GetCell(6, 5));

            Assert.IsTrue(handled);
            Assert.AreEqual(Pyromancy_FlamingHands.COOLDOWN,
                caster.GetPart<ActivatedAbilitiesPart>()
                    .GetAbility(fh.ActivatedAbilityID).CooldownRemaining);
        }

        // ── Hearthwarm ───────────────────────────────────────────────

        [Test]
        public void Hearthwarm_AnchorsTheAuraWhenTheCellHasAThermalTarget()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Pyromancy_Hearthwarm>(caster);
            Bush(zone, 6, 5);

            var (handled, _) = Cast(caster, zone, "CommandHearthwarm",
                targetCell: zone.GetCell(6, 5));

            Assert.IsTrue(handled);
            Assert.IsTrue(caster.HasEffect<HearthAuraEffect>(), "the aura rides the caster");
        }

        [Test]
        public void Hearthwarm_RefusesFreelyWithNothingToWarm()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            var hw = Learn<Pyromancy_Hearthwarm>(caster);

            var (handled, _) = Cast(caster, zone, "CommandHearthwarm",
                targetCell: zone.GetCell(6, 5));

            Assert.IsFalse(handled);
            Assert.IsFalse(caster.HasEffect<HearthAuraEffect>());
            Assert.AreEqual(0, caster.GetPart<ActivatedAbilitiesPart>()
                .GetAbility(hw.ActivatedAbilityID).CooldownRemaining);
        }

        // ── The grimoire flow ────────────────────────────────────────

        [Test]
        public void AGrimoireTeachesASkillWithFullHotbarIdentity()
        {
            var zone = new Zone("Z");
            var reader = Caster(zone, 5, 5);

            var book = new Entity { ID = "book", BlueprintName = "KindleGrimoire" };
            book.AddPart(new PhysicsPart { Takeable = true });
            book.AddPart(new GrimoirePart { SkillClassName = "Pyromancy_Kindle" });

            var read = GameEvent.New("InventoryAction");
            read.SetParameter("Command", "ReadGrimoire");
            read.SetParameter("Actor", (object)reader);
            book.FireEvent(read);
            read.Release();

            var skills = reader.GetPart<SkillsPart>();
            Assert.IsTrue(skills.HasSkill("Pyromancy_Kindle"), "the book taught the skill");

            // Identity: the ability's power-class key must resolve a
            // tooltip row, or the power is invisible in the grimoire
            // picker (the S4 blocker).
            var abilities = reader.GetPart<ActivatedAbilitiesPart>();
            ActivatedAbility found = null;
            for (int i = 0; i < abilities.AbilityList.Count; i++)
                if (abilities.AbilityList[i].Command == "CommandKindle")
                    found = abilities.AbilityList[i];
            Assert.IsNotNull(found, "the ability registered");
            Assert.AreEqual("Pyromancy_Kindle", found.SourcePowerClass);
            Assert.IsTrue(GrimoireTooltipData.IsGrimoirePower(found.SourcePowerClass),
                "and the picker filter recognizes it");
        }

        [Test]
        public void ReadingABookYouAlreadyKnowSaysSo()
        {
            var zone = new Zone("Z");
            var reader = Caster(zone, 5, 5);
            Learn<Pyromancy_Kindle>(reader);

            var book = new Entity { ID = "book", BlueprintName = "KindleGrimoire" };
            book.AddPart(new PhysicsPart { Takeable = true });
            book.AddPart(new GrimoirePart
            { SkillClassName = "Pyromancy_Kindle", AlreadyKnownMessage = "You already know the kindle rite." });

            var read = GameEvent.New("InventoryAction");
            read.SetParameter("Command", "ReadGrimoire");
            read.SetParameter("Actor", (object)reader);
            book.FireEvent(read);
            read.Release();

            Assert.IsTrue(MessageLog.GetRecent(3).Exists(
                m => m.Contains("already know")), "buy-then-find double route resolves politely");
        }

        // ── The starting kit ─────────────────────────────────────────

        [Test]
        public void TheStartingKitNowGrantsFlamingHandsAsASkill()
        {
            var zone = new Zone("Z");
            var player = Caster(zone, 5, 5);
            player.Statistics["SP"] = new Stat
            { Owner = player, Name = "SP", BaseValue = 0, Min = 0, Max = 999 };

            int granted = StartingSpellKit.GrantAll(player);

            Assert.IsTrue(player.GetPart<SkillsPart>().HasSkill("Pyromancy_FlamingHands"),
                "the former StartingMutation now arrives through the kit");
            Assert.GreaterOrEqual(granted, 5, "the four elemental spells + FlamingHands");
        }
    }
}
