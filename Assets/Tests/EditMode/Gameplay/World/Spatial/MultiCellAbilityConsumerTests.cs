using System;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Measure dispatched pulses independently of thermal reaction recipes.</summary>
    public sealed class MultiCellAbilityProbePart : Part
    {
        public int DirectHeatCalls, DamageCalls, ChargeAttempts, EntryCalls, AfterMoveCalls;
        public bool VetoBeforeMove;
        public float DirectJoules;
        public float CountedJoules = float.NaN;
        public Action OnHeat, OnDamage, OnEntry;
        public override bool WantEvent(int id) => true;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeMove" && VetoBeforeMove) return false;
            if (e.ID == "EntityEnteredCell") { EntryCalls++; OnEntry?.Invoke(); }
            if (e.ID == "AfterMove") AfterMoveCalls++;
            if (e.ID == "ApplyHeat")
            {
                if (!e.GetParameter<bool>("Radiant")
                    && (float.IsNaN(CountedJoules) || e.GetParameter<float>("Joules") == CountedJoules))
                {
                    DirectHeatCalls++;
                    DirectJoules += e.GetParameter<float>("Joules");
                    OnHeat?.Invoke();
                }
                return false;
            }
            if (e.ID == "BeforeTakeDamage") { DamageCalls++; OnDamage?.Invoke(); }
            if (e.ID == "BeforeApplyEffect" && e.GetParameter<Effect>("Effect") is ElectrifiedEffect)
                ChargeAttempts++;
            return true;
        }
    }

    /// <summary>Cross-system ability gates: physical contact, one owner per pulse,
    /// independent casts, holes, and mutation-safe target snapshots.</summary>
    public sealed class MultiCellAbilityConsumerTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); Diag.ResetAll(); }
        internal static Entity Owner(string footprint = null, bool creature = false)
        {
            var e = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "spatial-probe" };
            e.AddPart(new RenderPart { DisplayName = "probe" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.AddPart(new MultiCellAbilityProbePart());
            e.AddPart(new StatusEffectsPart());
            if (creature)
            {
                e.Tags["Creature"] = "";
                Stat(e, "Hitpoints", 1000); Stat(e, "Strength", 16); Stat(e, "Agility", 16);
                Stat(e, "Toughness", 16); Stat(e, "Speed", 100); Stat(e, "DV", 0);
                e.AddPart(new ArmorPart());
            }
            if (footprint != null) e.AddPart(new SpatialFootprintPart { CellsRaw = footprint });
            return e;
        }
        private static void Stat(Entity e, string name, int value)
            => e.Statistics[name] = new Stat { Owner = e, Name = name, BaseValue = value, Min = 0, Max = 10000 };
        internal static void Place(Zone z, Entity e, int x, int y) => Assert.IsTrue(z.AddEntity(e, x, y));
        internal static SkillEventContext Context(Zone z, Entity actor) => new SkillEventContext
        { Attacker = actor, Defender = actor, Zone = z, Rng = new Random(17),
          SourceCell = z.GetEntityCell(actor), DirectionX = 1, DirectionY = 0 };
        private static BaseSkillPart HeatSpell(string name)
        {
            if (name == "Conflagration") return new Pyromancy_Conflagration();
            if (name == "RimeNova") return new Cryomancy_RimeNova();
            return new Cryomancy_ChillDraft();
        }
        internal static Entity ArmedActor(string weaponClass, string footprint = null)
        {
            var actor = Owner(footprint, true);
            actor.AddPart(new InventoryPart { MaxWeight = 150 });
            var body = new Body(); actor.AddPart(body); body.SetBody(AnatomyFactory.CreateHumanoid());
            var weapon = Owner(); weapon.Tags["Item"] = "";
            weapon.GetPart<PhysicsPart>().Takeable = true;
            weapon.AddPart(new MeleeWeaponPart { BaseDamage = "1d1", HitBonus = 100,
                PenBonus = 1, Attributes = weaponClass });
            weapon.AddPart(new EquippablePart { Slot = "Hand" });
            actor.GetPart<InventoryPart>().EquipToBodyPart(weapon,
                body.GetParts().Find(p => p.Type == "Hand"));
            return actor;
        }
        private static BaseSkillPart Sweep(string name)
            => name == "Axe" ? (BaseSkillPart)new Axe_Whirlwind() : new Cudgel_GroundPound();

        [TestCase("Conflagration", true, 250f)] [TestCase("Conflagration", false, 250f)]
        [TestCase("RimeNova", true, -200f)] [TestCase("RimeNova", false, -200f)]
        [TestCase("ChillDraft", true, -100f)] [TestCase("ChillDraft", false, -100f)]
        public void RadiusHeat_ReachesRemoteBodyExactlyOnceEachCast(string spell, bool body, float joules)
        {
            var z = new Zone(); var actor = Owner(null, true); Place(z, actor, 10, 10);
            int anchor = spell == "ChillDraft" ? 8 : 7;
            var prop = Owner(body ? "0,0;1,0;2,0" : null); prop.AddPart(new ThermalPart());
            Place(z, prop, anchor, 10); var probe = prop.GetPart<MultiCellAbilityProbePart>();
            probe.CountedJoules = joules;
            Assert.IsTrue(HeatSpell(spell).OnCommand(Context(z, actor)));
            Assert.AreEqual(body ? 1 : 0, probe.DirectHeatCalls);
            Assert.AreEqual(body ? joules : 0f, probe.DirectJoules);
            Assert.IsTrue(HeatSpell(spell).OnCommand(Context(z, actor)));
            Assert.AreEqual(body ? 2 : 0, probe.DirectHeatCalls, "Separate casts retain separate pulses.");
        }

        [TestCase("Conflagration")] [TestCase("RimeNova")] [TestCase("ChillDraft")]
        public void RadiusHeat_SnapshotsOwnersBeforeCallbacksMoveAndAddTargets(string spell)
        {
            var z = new Zone(); var actor = Owner(null, true); Place(z, actor, 10, 10);
            var first = Owner(); first.AddPart(new ThermalPart()); Place(z, first, 9, 9);
            var later = Owner(); later.AddPart(new ThermalPart()); Place(z, later, 11, 11);
            var newcomer = Owner(); newcomer.AddPart(new ThermalPart());
            // Propagation is a separate legitimate pass, including its direct-heat flag.
            float primaryDose = spell == "Conflagration" ? 250f : spell == "RimeNova" ? -200f : -100f;
            foreach (var target in new[] { first, later, newcomer })
                target.GetPart<MultiCellAbilityProbePart>().CountedJoules = primaryDose;
            first.GetPart<MultiCellAbilityProbePart>().OnHeat = () =>
            { z.MoveEntity(later, 20, 20); if (z.GetEntityCell(newcomer) == null) Place(z, newcomer, 10, 11); };
            Assert.IsTrue(HeatSpell(spell).OnCommand(Context(z, actor)));
            Assert.AreEqual(1, later.GetPart<MultiCellAbilityProbePart>().DirectHeatCalls,
                "A target present at pulse capture is not skipped because an earlier callback moved it.");
            Assert.AreEqual(0, newcomer.GetPart<MultiCellAbilityProbePart>().DirectHeatCalls,
                "A callback cannot inject a new target into the in-flight pulse.");
        }

        [TestCase("Conflagration")] [TestCase("RimeNova")] [TestCase("Thunderclap")]
        public void RadiusCreatureDamage_SkipsVictimRemovedByEarlierHit(string spell)
        {
            var z = new Zone(); var actor = Owner(null, true); Place(z, actor, 10, 10);
            var first = Owner(null, true); Place(z, first, 8, 8);
            var later = Owner(null, true); Place(z, later, 12, 12);
            first.GetPart<MultiCellAbilityProbePart>().OnDamage = () => z.RemoveEntity(later);
            var skill = spell == "Thunderclap" ? (BaseSkillPart)new Galvanism_Thunderclap() : HeatSpell(spell);
            Assert.IsTrue(skill.OnCommand(Context(z, actor)));
            Assert.AreEqual(1, first.GetPart<MultiCellAbilityProbePart>().DamageCalls);
            Assert.AreEqual(0, later.GetPart<MultiCellAbilityProbePart>().DamageCalls);
            Assert.IsFalse(later.HasEffect<BurningEffect>() || later.HasEffect<FrozenEffect>() || later.HasEffect<ElectrifiedEffect>());
        }

        [TestCase(true)] [TestCase(false)]
        public void DryingBreeze_UsesRemotePhysicalContactAndStillDriesCaster(bool body)
        {
            var z = new Zone(); var actor = Owner(null, true); Place(z, actor, 10, 10);
            var target = Owner(body ? "0,0;1,0;2,0" : null, true); Place(z, target, 8, 10);
            actor.ApplyEffect(new WetEffect(), actor, z); target.ApplyEffect(new WetEffect(), actor, z);
            Assert.IsTrue(new Hydromancy_DryingBreeze().OnCommand(Context(z, actor)));
            Assert.IsFalse(actor.HasEffect<WetEffect>());
            Assert.AreEqual(!body, target.HasEffect<WetEffect>());
        }

        [TestCase(true)] [TestCase(false)]
        public void Thunderclap_ChargesRemoteSceneryOncePerCast(bool body)
        {
            var z = new Zone(); var actor = Owner(null, true); Place(z, actor, 10, 10);
            var target = Owner(body ? "0,0;1,0;2,0" : null); target.Tags["Metal"] = "";
            Place(z, target, 7, 10); var probe = target.GetPart<MultiCellAbilityProbePart>();
            Assert.IsTrue(new Galvanism_Thunderclap().OnCommand(Context(z, actor)));
            Assert.AreEqual(body ? 1 : 0, probe.ChargeAttempts);
            Assert.AreEqual(body, target.HasEffect<ElectrifiedEffect>());
            Assert.IsTrue(new Galvanism_Thunderclap().OnCommand(Context(z, actor)));
            Assert.AreEqual(body ? 2 : 0, probe.ChargeAttempts);
        }

        [TestCase(true)] [TestCase(false)]
        public void Overload_RemoteConductorSpanningRayTakesOneHitPerCast(bool body)
        {
            var z = new Zone(); var actor = Owner(null, true); Place(z, actor, 5, 10);
            var target = Owner(body ? "0,0;0,1;1,1;2,1" : null, true); Place(z, target, 8, 9);
            target.ApplyEffect(new WetEffect(), actor, z);
            Assert.AreEqual(body, new Galvanism_Overload().OnCommand(Context(z, actor)));
            Assert.AreEqual(body ? 992 : 1000, target.GetStatValue("Hitpoints"));
            Assert.AreEqual(body, new Galvanism_Overload().OnCommand(Context(z, actor)));
            Assert.AreEqual(body ? 984 : 1000, target.GetStatValue("Hitpoints"));
        }

        [Test] public void Overload_SnapshotPreventsMovingConductorBeingStruckAgainDownRay()
        {
            var z = new Zone(); var actor = Owner(null, true); Place(z, actor, 5, 10);
            var target = Owner(null, true); Place(z, target, 7, 10); target.ApplyEffect(new WetEffect(), actor, z);
            target.GetPart<MultiCellAbilityProbePart>().OnDamage = () => z.MoveEntity(target, 10, 10);
            Assert.IsTrue(new Galvanism_Overload().OnCommand(Context(z, actor)));
            Assert.AreEqual(992, target.GetStatValue("Hitpoints"));
        }

        [Test] public void Overload_DryRemoteBodyStopsChainBeforeLaterConductor()
        {
            var z = new Zone(); var actor = Owner(null, true); Place(z, actor, 5, 10);
            var blocker = Owner("0,0;0,1", true); Place(z, blocker, 7, 9);
            var target = Owner(null, true); Place(z, target, 10, 10); target.ApplyEffect(new WetEffect(), actor, z);
            Assert.IsFalse(new Galvanism_Overload().OnCommand(Context(z, actor)));
            Assert.AreEqual(1000, target.GetStatValue("Hitpoints"));
            z.RemoveEntity(blocker);
            Assert.IsTrue(new Galvanism_Overload().OnCommand(Context(z, actor)));
            Assert.AreEqual(992, target.GetStatValue("Hitpoints"));
        }

        [TestCase(true)] [TestCase(false)]
        public void Pyroclasm_DetonatesVisibleContactCellRatherThanRemoteAnchor(bool body)
        {
            var z = new Zone(); var actor = Owner(null, true); Place(z, actor, 10, 10);
            var target = Owner(body ? "0,0;1,0;2,0" : null, true); Place(z, target, 7, 10);
            target.ApplyEffect(new BurningEffect(rng: new Random(17)) { Duration = 3 }, actor, z);
            var bystander = Owner(null, true); Place(z, bystander, 10, 11);
            Assert.AreEqual(body, new Pyromancy_Pyroclasm().OnCommand(Context(z, actor)));
            Assert.AreEqual(body ? 991 : 1000, target.GetStatValue("Hitpoints"));
            Assert.AreEqual(body ? 991 : 1000, bystander.GetStatValue("Hitpoints"));
            Assert.AreEqual(!body, target.HasEffect<BurningEffect>());
        }

        [Test] public void Pyroclasm_BodyIntersectingSeveralBlastCellsTakesOneHit()
        {
            var z = new Zone(); var actor = Owner(null, true); Place(z, actor, 10, 10);
            var target = Owner("0,0;1,0;2,0", true); Place(z, target, 9, 9);
            target.ApplyEffect(new BurningEffect(rng: new Random(17)) { Duration = 4 }, actor, z);
            target.GetEffect<BurningEffect>().Duration = 4;
            Assert.IsTrue(new Pyromancy_Pyroclasm().OnCommand(Context(z, actor)));
            Assert.AreEqual(988, target.GetStatValue("Hitpoints"));
        }

        [TestCase("Axe", true)] [TestCase("Axe", false)]
        [TestCase("Cudgel", true)] [TestCase("Cudgel", false)]
        public void WeaponSweep_ReachesRemoteBodyOnce(string weapon, bool body)
        {
            var z = new Zone(); var actor = ArmedActor(weapon); Place(z, actor, 10, 10);
            var target = Owner(body ? "0,0;1,0;2,0;2,1" : null, true); Place(z, target, 7, 10);
            Assert.AreEqual(body, Sweep(weapon).OnCommand(Context(z, actor)));
            Assert.AreEqual(body ? 1 : 0, target.GetPart<MultiCellAbilityProbePart>().DamageCalls);
        }

        [TestCase("Axe")] [TestCase("Cudgel")]
        public void WeaponSweep_SkipsTargetRemovedByEarlierStrike(string weapon)
        {
            var z = new Zone(); var actor = ArmedActor(weapon); Place(z, actor, 10, 10);
            var first = Owner(null, true); Place(z, first, 10, 9);
            var later = Owner(null, true); Place(z, later, 11, 11);
            first.GetPart<MultiCellAbilityProbePart>().OnDamage = () => z.RemoveEntity(later);
            Assert.IsTrue(Sweep(weapon).OnCommand(Context(z, actor)));
            Assert.AreEqual(1, first.GetPart<MultiCellAbilityProbePart>().DamageCalls);
            Assert.AreEqual(0, later.GetPart<MultiCellAbilityProbePart>().DamageCalls);
        }

        [TestCase("Axe")] [TestCase("Cudgel")]
        public void WeaponSweep_LargeAttackerReachesFromItsBodyPerimeter(string weapon)
        {
            var z = new Zone(); var actor = ArmedActor(weapon, "0,0;1,0;2,0"); Place(z, actor, 7, 10);
            var target = Owner(null, true); Place(z, target, 10, 10);
            Assert.IsTrue(Sweep(weapon).OnCommand(Context(z, actor)));
            Assert.AreEqual(1, target.GetPart<MultiCellAbilityProbePart>().DamageCalls);
        }

        [TestCase("0,0;1,0", 11, "Poison", false)]
        [TestCase(null, 11, "Poison", true)]
        [TestCase("1,0", 10, "Poison", true)]
        [TestCase("0,0;1,0", 11, "Cryo", true)]
        public void LingeringPoison_SuppressedOnlyByMatchingGasOnPhysicalBody(string shape, int gasX, string gasType, bool tick)
        {
            var z = new Zone(); var target = Owner(shape, true); Place(z, target, 10, 10);
            var gas = Owner(); gas.AddPart(new GasPoolPart { GasType = gasType }); Place(z, gas, gasX, 10);
            var context = GameEvent.New("BeginTakeAction"); context.SetParameter("Zone", (object)z);
            try { new PoisonedByGasEffect { DamagePerTurn = 2, GasTypeKey = "Poison" }.OnTurnStart(target, context); }
            finally { context.Release(); }
            Assert.AreEqual(tick ? 998 : 1000, target.GetStatValue("Hitpoints"));
        }

        [TestCase(true)] [TestCase(false)]
        public void Vault_WholeIntermediateBodyRespectsArchiveButStillJumpsOrdinaryWalls(bool archive)
        {
            var z = new Zone(); var actor = Owner("0,0;0,1", true); Place(z, actor, 10, 10);
            var barrier = Owner(); barrier.GetPart<PhysicsPart>().Solid = true; barrier.Tags["Solid"] = "";
            if (archive) barrier.AddPart(new SealedLibraryBarrierPart());
            Place(z, barrier, 11, 11);
            Assert.AreEqual(!archive, new Acrobatics_Vault().OnCommand(Context(z, actor)));
            Assert.AreEqual((archive ? 10 : 12, 10), z.GetEntityPosition(actor));
            Assert.IsTrue(z.GetOccupants(archive ? 10 : 12, 11).Contains(actor));
        }

        [TestCase("Vault", true)] [TestCase("Vault", false)]
        [TestCase("Disengage", true)] [TestCase("Disengage", false)]
        [TestCase("Charge", true)] [TestCase("Charge", false)]
        public void MovementSkill_DispatchesEntryAtBodyEdgeAndPreservesMoveVetoBypass(string name, bool body)
        {
            var z = new Zone();
            var actor = ArmedActor(name == "Disengage" ? "Piercing" : "Cudgel", body ? "0,0;0,1" : null);
            Place(z, actor, 10, 10);
            var actorProbe = actor.GetPart<MultiCellAbilityProbePart>();
            actorProbe.VetoBeforeMove = true;
            var entry = Owner(); Place(z, entry, name == "Vault" ? 12 : 11, 11);
            if (name == "Charge") Place(z, Owner(null, true), 13, 10);
            BaseSkillPart skill = name == "Vault" ? (BaseSkillPart)new Acrobatics_Vault()
                : name == "Disengage" ? new ShortBlades_Disengage() : new Cudgel_ChargingStrike();

            Assert.IsTrue(skill.OnCommand(Context(z, actor)));
            Assert.AreEqual((name == "Disengage" ? 13 : 12, 10), z.GetEntityPosition(actor),
                "These existing movement skills intentionally bypass the ordinary BeforeMove veto.");
            Assert.AreEqual(body ? 1 : 0, entry.GetPart<MultiCellAbilityProbePart>().EntryCalls,
                "Only physical newly entered body cells activate a field beside the anchor lane.");
            Assert.AreEqual(name == "Vault" ? 1 : name == "Disengage" ? 3 : 2, actorProbe.AfterMoveCalls,
                "Every actual landing or walked step uses the shared movement event path.");
        }

        [TestCase("Disengage", "relocate", true)] [TestCase("Disengage", "relocate", false)]
        [TestCase("Charge", "relocate", true)] [TestCase("Charge", "relocate", false)]
        [TestCase("Disengage", "remove", true)] [TestCase("Disengage", "remove", false)]
        [TestCase("Charge", "remove", true)] [TestCase("Charge", "remove", false)]
        public void WalkingSkill_StopsWhenEntryCallbackChangesMover(string name, string change, bool reacts)
        {
            var z = new Zone();
            var actor = ArmedActor(name == "Disengage" ? "Piercing" : "Cudgel", "0,0;0,1");
            Place(z, actor, 10, 10);
            var entry = Owner(); Place(z, entry, 11, 11);
            var victim = Owner(null, true); if (name == "Charge") Place(z, victim, 12, 10);
            entry.GetPart<MultiCellAbilityProbePart>().OnEntry = () =>
            {
                if (!reacts) return;
                if (change == "relocate") Assert.IsTrue(z.MoveEntity(actor, 20, 20));
                else z.RemoveEntity(actor);
            };
            BaseSkillPart skill = name == "Disengage" ? (BaseSkillPart)new ShortBlades_Disengage()
                : new Cudgel_ChargingStrike();
            skill.OnCommand(Context(z, actor));
            Assert.AreEqual(1, entry.GetPart<MultiCellAbilityProbePart>().EntryCalls);
            Assert.AreEqual(reacts ? change == "relocate" ? (20, 20) : (-1, -1)
                : (name == "Disengage" ? 13 : 11, 10), z.GetEntityPosition(actor));
            Assert.AreEqual(!reacts && name == "Charge",
                victim.GetPart<MultiCellAbilityProbePart>().DamageCalls > 0,
                "Entry relocation/removal stops the original charge before its next attack.");
        }

        [TestCase(true)] [TestCase(false)]
        public void Slam_DoesNotStunTargetRemovedByEntryReaction(bool removes)
        {
            var z = new Zone(); var actor = ArmedActor("Cudgel"); Place(z, actor, 10, 10);
            var target = Owner("0,0;1,0", true); Place(z, target, 10, 9);
            var entry = Owner(); Place(z, entry, 11, 8);
            entry.GetPart<MultiCellAbilityProbePart>().OnEntry = () => { if (removes) z.RemoveEntity(target); };
            Assert.IsTrue(new Cudgel_Slam().OnCommand(Context(z, actor)));
            Assert.AreEqual(1, entry.GetPart<MultiCellAbilityProbePart>().EntryCalls);
            Assert.AreEqual(!removes, target.HasEffect<StunnedEffect>());
            Assert.AreEqual(removes ? (-1, -1) : (10, 6), z.GetEntityPosition(target));
        }

        [TestCase(true, true)] [TestCase(false, true)] [TestCase(true, false)]
        public void TilePropagation_MaterialExistsAtBodyCellsOnly(bool body, bool receptive)
        {
            var z = new Zone(); var target = Owner(body ? "1,0;2,0" : null);
            target.AddPart(new MaterialPart { Conductivity = receptive ? 100 : 0, Combustibility = receptive ? 100 : 0 });
            Place(z, target, 10, 10);
            Assert.AreEqual(body && receptive, TilePropagationSystem.IsConductive(z, 11, 10));
            Assert.AreEqual(body && receptive, TilePropagationSystem.IsFlammable(z, 11, 10));
            Assert.AreEqual(!body && receptive, TilePropagationSystem.IsConductive(z, 10, 10), "Anchor hole is not material contact.");
        }
    }
}
