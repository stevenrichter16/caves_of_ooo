using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Tests
{
    public sealed class TerritorySelfDefenseInputTests : LookKeyFixture
    {
        Entity guard, post;
        SpreadTerritoryPart role;
        BrainPart brain;
        TerritoryStrikeProbe guardProbe, playerProbe;

        void SetupGuard(bool west = false)
        {
            guard = Item("SoddenPassageGuard"); post = Item("SoddenPassagePost");
            Assert.True(Zone.AddEntity(guard, west ? 9 : 11, 10)); Assert.True(Zone.AddEntity(post, 12, 9));
            role = guard.GetPart<SpreadTerritoryPart>(); brain = guard.GetPart<BrainPart>();
            brain.CurrentZone = Zone; brain.Rng = new Random(64);
            Assert.True(role.Configure(Zone, post, 8, 8, 14, 13, 2));
            guardProbe = new TerritoryStrikeProbe(); guard.AddPart(guardProbe);
            playerProbe = new TerritoryStrikeProbe(); Player.AddPart(playerProbe);
            Assert.False(FactionManager.IsHostile(guard, Player)); Assert.False(FactionManager.IsHostile(Player, guard));
        }
        void GuardTurn() => guard.FireEventAndRelease(GameEvent.New("TakeTurn"));
        void Enforce()
        {
            for (int i = 0; i < 4; i++) GuardTurn();
            Assert.AreEqual(1, guardProbe.Strikes, "Real neutral permit enforcer reached native melee after warning and two grace actions.");
            Assert.AreSame(Player, role.WarningTarget); Assert.AreEqual(0, role.GraceRemaining); Assert.AreSame(Player, brain.Target);
            Assert.False(brain.IsPersonallyHostileTo(Player)); Assert.False(FactionManager.IsHostile(Player, guard));
        }
        void RefusesBump(Key key)
        {
            var input = Input(); var at = Zone.GetEntityPosition(Player);
            int tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player);
            Press(key, () => Tick(input));
            Assert.AreEqual(0, playerProbe.Strikes); Assert.AreEqual(at, Zone.GetEntityPosition(Player));
            Assert.AreEqual(tick, input.TurnManager.TickCount); Assert.AreEqual(energy, input.TurnManager.GetEnergy(Player));
        }

        [TestCase(false)] [TestCase(true)]
        public void ActualBumpCanDefendAgainstCurrentNeutralEnforcementAndCostsExactlyOneAction(bool west)
        {
            SetupGuard(west); Enforce(); var input = Input();
            int tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player);
            Press(west ? Key.A : Key.D, () => Tick(input));
            Assert.AreEqual(1, playerProbe.Strikes, "The current bug silently refuses this native input because factions are neutral.");
            Assert.AreEqual(1, playerProbe.Ends); Assert.AreEqual((10, 10), Zone.GetEntityPosition(Player));
            Assert.Greater(input.TurnManager.TickCount, tick);
            Assert.AreEqual(energy - TurnManager.ActionThreshold + (input.TurnManager.TickCount - tick) * Player.GetStatValue("Speed", 100), input.TurnManager.GetEnergy(Player));
            Assert.False(brain.IsPersonallyHostileTo(Player), "Admission is temporary; the vetoed strike itself must not create permanent hostility.");
            Assert.False(FactionManager.IsHostile(Player, guard));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void AWarningAndEveryAdvertisedGraceOpportunityRemainNoncombatBumps(int turns)
        {
            SetupGuard(); for (int i = 0; i < turns; i++) GuardTurn();
            Assert.AreEqual(0, guardProbe.Strikes); Assert.IsNull(brain.Target); RefusesBump(Key.D);
        }

        [TestCase("withdrawn")] [TestCase("post-removed")] [TestCase("post-moved")]
        [TestCase("role-removed")] [TestCase("wrong-warning")]
        [TestCase("conversation")] [TestCase("calm")] [TestCase("party")]
        [TestCase("cloth-player")] [TestCase("cloth-guard")] [TestCase("dead")]
        public void AStaleBrainTargetCannotAuthorizeSelfDefenseAfterTheActualClaimChanges(string change)
        {
            SetupGuard(); Enforce(); Key key = Key.D;
            if (change == "withdrawn")
            { Assert.True(Zone.MoveEntity(Player, 15, 10)); Assert.True(Zone.MoveEntity(guard, 14, 10)); key = Key.A; }
            if (change == "post-removed") Assert.True(Zone.RemoveEntity(post));
            if (change == "post-moved") Assert.True(Zone.MoveEntity(post, 12, 8));
            if (change == "role-removed") Assert.True(guard.RemovePart(role));
            if (change == "wrong-warning") role.WarningTarget = post;
            if (change == "conversation") brain.InConversation = true;
            if (change == "calm") brain.PushGoal(new NoFightGoal(10, false));
            if (change == "party") { Assert.True(brain.SetPartyLeader(Player)); brain.Target = Player; }
            if (change == "cloth-player") Assert.True(Player.ApplyEffect(new UnderTheClothEffect()));
            if (change == "cloth-guard") Assert.True(guard.ApplyEffect(new UnderTheClothEffect()));
            if (change == "dead") guard.GetStat("Hitpoints").BaseValue = 0;
            Assert.AreSame(Player, brain.Target, "No further guard action has cleared this stale pointer.");
            RefusesBump(key); Assert.False(brain.IsPersonallyHostileTo(Player));
        }

        [Test] public void NativePermitPurchaseCancelsBumpAdmissionBeforeTheGuardGetsAnotherTurn()
        {
            SetupGuard(); Enforce();
            Assert.True(Zone.MoveEntity(Player, 7, 10)); Assert.True(Zone.MoveEntity(guard, 8, 10));
            TradeSystem.SetDrams(Player, 20); int before = TradeSystem.GetDrams(Player);
            Assert.True(InventorySystem.PerformAction(Player, guard, "PermitPassage", Zone));
            Assert.AreEqual(before - 4, TradeSystem.GetDrams(Player));
            var token = Player.GetPart<LocalPassageTokenPart>(); Assert.NotNull(token);
            Assert.True(Zone.MoveEntity(Player, 9, 10)); Assert.True(guard.GetPart<LocalPassagePermitPart>().Allows(Player, Zone));
            Assert.AreSame(Player, brain.Target, "The bought permit must win over an unscheduled stale pursuit.");
            RefusesBump(Key.A); Assert.False(token.Spent); Assert.AreSame(token, Player.GetPart<LocalPassageTokenPart>());
        }

        [Test] public void ClothProtectionAlsoStopsNativePermitEnforcementRatherThanOnlyHidingItsBumpResponse()
        {
            SetupGuard(); Assert.True(Player.ApplyEffect(new UnderTheClothEffect()));
            Assert.True(UnderTheClothEffect.Protects(guard, Player));
            for (int i = 0; i < 4; i++) GuardTurn();
            Assert.AreEqual(0, guardProbe.Strikes); Assert.IsNull(brain.Target); Assert.IsNull(role.WarningTarget);
        }

        [Test] public void CurrentEnforcementQueryIsReadOnlyAndDoesNotLaunderAStaleTargetIntoAggression()
        {
            SetupGuard(); Enforce();
            var method = typeof(SpreadTerritoryPart).GetMethod("IsEnforcingAgainst", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method, "Explicit current enforcement authority is absent.");
            for (int i = 0; i < 3; i++) Assert.True((bool)method.Invoke(role, new object[] { Player, Zone }));
            Assert.AreSame(Player, role.WarningTarget); Assert.AreSame(Player, brain.Target); Assert.AreEqual(0, role.GraceRemaining);
            Assert.AreEqual(1, guardProbe.Strikes); Assert.False(brain.IsPersonallyHostileTo(Player));
            Assert.True(Zone.MoveEntity(Player, 15, 10));
            Assert.False((bool)method.Invoke(role, new object[] { Player, Zone }));
            Assert.AreSame(Player, brain.Target, "A query must not mutate AI state to manufacture its answer.");
        }

        [Test] public void OrdinaryFactionHostilityRetainsItsExistingBumpAttack()
        {
            SetupGuard(); guard.RemovePart(role); brain.SetPersonallyHostile(Player, false);
            Assert.True(FactionManager.IsHostile(Player, guard)); var input = Input();
            Press(Key.D, () => Tick(input)); Assert.AreEqual(1, playerProbe.Strikes); Assert.AreEqual(1, playerProbe.Ends);
        }

        public sealed class TerritoryStrikeProbe : Part
        {
            public int Strikes, Ends;
            public override string Name => "TerritoryStrikeProbe";
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "BeforeMeleeAttack") { Strikes++; return false; }
                if (e.ID == "EndTurn") Ends++;
                return true;
            }
        }
    }
}
