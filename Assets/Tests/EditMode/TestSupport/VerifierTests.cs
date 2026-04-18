using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios;
using NUnit.Framework;

namespace CavesOfOoo.Tests.TestSupport
{
    /// <summary>
    /// Phase 3c tests — exercise every <see cref="ScenarioVerifier"/> chain
    /// method and sub-verifier. Verifies both the happy path (assertion passes
    /// silently) and the failure path (assertion throws <see cref="AssertionException"/>
    /// with a message referencing the assertion name).
    /// </summary>
    [TestFixture]
    public class VerifierTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        // =========================================================
        // Root / entry point
        // =========================================================

        [Test]
        public void Verify_ReturnsNonNullRoot()
        {
            var ctx = _harness.CreateContext();
            Assert.IsNotNull(ctx.Verify());
        }

        [Test]
        public void Verify_NullContext_Throws()
        {
            ScenarioContext nullCtx = null;
            Assert.Throws<System.ArgumentNullException>(() => nullCtx.Verify());
        }

        // =========================================================
        // ScenarioVerifier (root)
        // =========================================================

        [Test]
        public void EntityCount_CorrectCount_Passes()
        {
            var ctx = _harness.CreateContext();
            ctx.Spawn("Snapjaw").At(20, 10);
            ctx.Spawn("Snapjaw").At(25, 10);
            // Plus the player = 3 total with Creature tag.
            ctx.Verify().EntityCount(withTag: "Creature", expected: 3);
        }

        [Test]
        public void EntityCount_WrongCount_Fails()
        {
            var ctx = _harness.CreateContext();
            var ex = Assert.Throws<AssertionException>(
                () => ctx.Verify().EntityCount(withTag: "Creature", expected: 5));
            StringAssert.Contains("EntityCount", ex.Message);
            StringAssert.Contains("expected 5", ex.Message);
        }

        [Test]
        public void PlayerIsAlive_HealthyPlayer_Passes()
        {
            var ctx = _harness.CreateContext();
            ctx.Verify().PlayerIsAlive();
        }

        [Test]
        public void PlayerIsAlive_ZeroHp_Fails()
        {
            var ctx = _harness.CreateContext();
            ctx.PlayerEntity.GetStat("Hitpoints").BaseValue = 0;
            var ex = Assert.Throws<AssertionException>(() => ctx.Verify().PlayerIsAlive());
            StringAssert.Contains("PlayerIsAlive", ex.Message);
        }

        [Test]
        public void PlayerIsAlive_RemovedFromZone_Fails()
        {
            var ctx = _harness.CreateContext();
            ctx.Zone.RemoveEntity(ctx.PlayerEntity);
            var ex = Assert.Throws<AssertionException>(() => ctx.Verify().PlayerIsAlive());
            StringAssert.Contains("not in the zone", ex.Message);
        }

        [Test]
        public void TurnCount_MatchesTickCount()
        {
            var ctx = _harness.CreateContext();
            // Fresh TurnManager — TickCount = 0.
            ctx.Verify().TurnCount(0);
        }

        [Test]
        public void Cell_OutOfBounds_Fails()
        {
            var ctx = _harness.CreateContext();
            Assert.Throws<AssertionException>(() => ctx.Verify().Cell(-1, -1));
        }

        // =========================================================
        // EntityVerifier
        // =========================================================

        [Test]
        public void Entity_NullEntity_Fails()
        {
            var ctx = _harness.CreateContext();
            Assert.Throws<AssertionException>(() => ctx.Verify().Entity(null));
        }

        [Test]
        public void Entity_IsAt_Passes()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").At(20, 10);
            ctx.Verify().Entity(snapjaw).IsAt(20, 10);
        }

        [Test]
        public void Entity_IsAt_Fails_WithPositionInMessage()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").At(20, 10);
            var ex = Assert.Throws<AssertionException>(
                () => ctx.Verify().Entity(snapjaw).IsAt(99, 99));
            StringAssert.Contains("(20,10)", ex.Message);
            StringAssert.Contains("(99,99)", ex.Message);
        }

        [Test]
        public void Entity_HasHpFraction_WithinTolerance_Passes()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").WithHp(0.5f).At(20, 10);
            ctx.Verify().Entity(snapjaw).HasHpFraction(0.5f);
        }

        [Test]
        public void Entity_HasHpFraction_OutsideTolerance_Fails()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").WithHp(0.2f).At(20, 10);
            Assert.Throws<AssertionException>(
                () => ctx.Verify().Entity(snapjaw).HasHpFraction(0.9f));
        }

        [Test]
        public void Entity_IsAlive_HealthyEntity_Passes()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").At(20, 10);
            ctx.Verify().Entity(snapjaw).IsAlive();
        }

        [Test]
        public void Entity_IsAlive_ZeroHp_Fails()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").At(20, 10);
            snapjaw.GetStat("Hitpoints").BaseValue = 0;
            Assert.Throws<AssertionException>(() => ctx.Verify().Entity(snapjaw).IsAlive());
        }

        [Test]
        public void Entity_HasStat_Passes()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").WithStat("Strength", 20).At(20, 10);
            ctx.Verify().Entity(snapjaw).HasStat("Strength", 20);
        }

        [Test]
        public void Entity_HasStatAtLeast_Passes()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").WithStat("Strength", 20).At(20, 10);
            ctx.Verify().Entity(snapjaw).HasStatAtLeast("Strength", 15);
        }

        [Test]
        public void Entity_HasStatAtLeast_BelowMin_Fails()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").WithStat("Strength", 10).At(20, 10);
            Assert.Throws<AssertionException>(
                () => ctx.Verify().Entity(snapjaw).HasStatAtLeast("Strength", 30));
        }

        [Test]
        public void Entity_HasPartOfType_Passes()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").At(20, 10);
            ctx.Verify().Entity(snapjaw).HasPartOfType<BrainPart>();
        }

        [Test]
        public void Entity_HasPartOfType_Missing_Fails()
        {
            var ctx = _harness.CreateContext();
            // A plain chest has no BrainPart.
            var chest = ctx.World.PlaceObject("Chest").At(20, 10);
            Assert.Throws<AssertionException>(
                () => ctx.Verify().Entity(chest).HasPartOfType<BrainPart>());
        }

        [Test]
        public void Entity_HasTag_Passes()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").At(20, 10);
            ctx.Verify().Entity(snapjaw).HasTag("Creature");
        }

        [Test]
        public void Entity_HasTag_Missing_Fails()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").At(20, 10);
            var ex = Assert.Throws<AssertionException>(
                () => ctx.Verify().Entity(snapjaw).HasTag("NoSuchTag"));
            StringAssert.Contains("HasTag", ex.Message);
            StringAssert.Contains("NoSuchTag", ex.Message);
        }

        [Test]
        public void Entity_HasStat_WrongValue_Fails()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").WithStat("Strength", 10).At(20, 10);
            var ex = Assert.Throws<AssertionException>(
                () => ctx.Verify().Entity(snapjaw).HasStat("Strength", 99));
            StringAssert.Contains("Strength", ex.Message);
            StringAssert.Contains("expected 99", ex.Message);
        }

        [Test]
        public void Entity_HasStat_MissingStat_Fails()
        {
            var ctx = _harness.CreateContext();
            // Chests don't have a Strength stat.
            var chest = ctx.World.PlaceObject("Chest").At(20, 10);
            var ex = Assert.Throws<AssertionException>(
                () => ctx.Verify().Entity(chest).HasStat("Strength", 5));
            StringAssert.Contains("not present", ex.Message);
        }

        // =========================================================
        // HasGoalOnStack — previously uncovered (filled in review)
        // =========================================================

        [Test]
        public void Entity_HasGoalOnStack_WithMatchingGoal_Passes()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").At(20, 10);
            var brain = snapjaw.GetPart<BrainPart>();
            brain.PushGoal(new WaitGoal(3));

            ctx.Verify().Entity(snapjaw).HasGoalOnStack<WaitGoal>();
        }

        [Test]
        public void Entity_HasGoalOnStack_GoalNotPresent_Fails()
        {
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").At(20, 10);
            // No goals pushed — stack is empty.
            var ex = Assert.Throws<AssertionException>(
                () => ctx.Verify().Entity(snapjaw).HasGoalOnStack<WaitGoal>());
            StringAssert.Contains("HasGoalOnStack", ex.Message);
            StringAssert.Contains("WaitGoal", ex.Message);
        }

        [Test]
        public void Entity_HasGoalOnStack_NoBrainPart_Fails()
        {
            var ctx = _harness.CreateContext();
            // Chests don't have a BrainPart.
            var chest = ctx.World.PlaceObject("Chest").At(20, 10);
            var ex = Assert.Throws<AssertionException>(
                () => ctx.Verify().Entity(chest).HasGoalOnStack<WaitGoal>());
            StringAssert.Contains("no BrainPart", ex.Message);
        }

        // =========================================================
        // PlayerVerifier
        // =========================================================

        [Test]
        public void Player_IsAt_Passes()
        {
            var ctx = _harness.CreateContext(playerX: 5, playerY: 5);
            ctx.Verify().Player().IsAt(5, 5);
        }

        [Test]
        public void Player_HasHpFraction_Passes()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            ctx.Player.SetHpFraction(0.3f);
            ctx.Verify().Player().HasHpFraction(0.3f);
        }

        [Test]
        public void Player_HasMutation_Passes()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            ctx.Player.AddMutation("FireBoltMutation");
            ctx.Verify().Player().HasMutation("FireBoltMutation");
        }

        [Test]
        public void Player_HasMutation_Missing_Fails()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            Assert.Throws<AssertionException>(
                () => ctx.Verify().Player().HasMutation("FireBoltMutation"));
        }

        [Test]
        public void Player_HasItemInInventory_Passes()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            ctx.Player.GiveItem("HealingTonic");
            ctx.Verify().Player().HasItemInInventory("HealingTonic");
        }

        [Test]
        public void Player_HasEquipped_Passes()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            ctx.Player.Equip("ShortSword");
            ctx.Verify().Player().HasEquipped("ShortSword");
        }

        [Test]
        public void Player_HasFactionRep_Passes()
        {
            var ctx = _harness.CreateContext();
            PlayerReputation.Set("Villagers", 50);
            ctx.Verify().Player().HasFactionRep("Villagers", 50);
        }

        [Test]
        public void Player_HasFactionRepAtLeast_Passes()
        {
            var ctx = _harness.CreateContext();
            PlayerReputation.Set("Villagers", 100);
            ctx.Verify().Player().HasFactionRepAtLeast("Villagers", 50);
        }

        // =========================================================
        // CellVerifier
        // =========================================================

        [Test]
        public void Cell_ContainsBlueprint_Passes()
        {
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("Chest").At(25, 15);
            ctx.Verify().Cell(25, 15).ContainsBlueprint("Chest");
        }

        [Test]
        public void Cell_ContainsBlueprint_Missing_Fails()
        {
            var ctx = _harness.CreateContext();
            Assert.Throws<AssertionException>(
                () => ctx.Verify().Cell(25, 15).ContainsBlueprint("Chest"));
        }

        [Test]
        public void Cell_IsEmpty_Passes()
        {
            var ctx = _harness.CreateContext();
            ctx.Verify().Cell(5, 5).IsEmpty();
        }

        [Test]
        public void Cell_IsEmpty_WithEntity_Fails()
        {
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("Chest").At(5, 5);
            Assert.Throws<AssertionException>(() => ctx.Verify().Cell(5, 5).IsEmpty());
        }

        [Test]
        public void Cell_HasNoEntityWithTag_CreatureNotPresent_Passes()
        {
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("Chest").At(5, 5); // Chest has no Creature tag
            ctx.Verify().Cell(5, 5).HasNoEntityWithTag("Creature");
        }

        [Test]
        public void Cell_HasNoEntityWithTag_CreaturePresent_Fails()
        {
            var ctx = _harness.CreateContext();
            ctx.Spawn("Snapjaw").At(5, 5);
            Assert.Throws<AssertionException>(
                () => ctx.Verify().Cell(5, 5).HasNoEntityWithTag("Creature"));
        }

        [Test]
        public void Cell_IsPassable_EmptyCell_Passes()
        {
            var ctx = _harness.CreateContext();
            ctx.Verify().Cell(5, 5).IsPassable();
        }

        [Test]
        public void Cell_IsPassable_SolidCell_Fails()
        {
            // Cell.IsSolid checks the "Solid" TAG KEY — walls carry it explicitly,
            // but creatures only have PhysicsPart.Solid=true (a field, not a tag).
            // So a cell with a creature is still Passable by Cell's definition;
            // we need an actual Wall blueprint for this test.
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("StoneWall").At(5, 5);
            var ex = Assert.Throws<AssertionException>(
                () => ctx.Verify().Cell(5, 5).IsPassable());
            StringAssert.Contains("IsPassable", ex.Message);
        }

        [Test]
        public void Cell_IsSolid_WithSolidEntity_Passes()
        {
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("StoneWall").At(5, 5);
            ctx.Verify().Cell(5, 5).IsSolid();
        }

        [Test]
        public void Cell_IsSolid_EmptyCell_Fails()
        {
            var ctx = _harness.CreateContext();
            var ex = Assert.Throws<AssertionException>(
                () => ctx.Verify().Cell(5, 5).IsSolid());
            StringAssert.Contains("IsSolid", ex.Message);
        }

        // =========================================================
        // Fluent chaining — Back() + multi-step flow
        // =========================================================

        [Test]
        public void FluentChain_EntityThenPlayerThenCell_RunsAllAssertions()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player", playerX: 5, playerY: 5);
            var snapjaw = ctx.Spawn("Snapjaw").At(20, 10);
            ctx.World.PlaceObject("Chest").At(25, 15);
            ctx.Player.GiveItem("HealingTonic");

            // Single fluent chain verifying entity, player, and cell state.
            ctx.Verify()
                .Entity(snapjaw)
                    .IsAt(20, 10)
                    .HasTag("Creature")
                .Back()
                .Player()
                    .IsAt(5, 5)
                    .HasItemInInventory("HealingTonic")
                .Back()
                .Cell(25, 15)
                    .ContainsBlueprint("Chest")
                .Back()
                .EntityCount(withTag: "Creature", expected: 2); // player + snapjaw
        }

        [Test]
        public void FluentChain_BackReturnsRoot_AllowsRepeatedEntity()
        {
            var ctx = _harness.CreateContext();
            var a = ctx.Spawn("Snapjaw").At(10, 10);
            var b = ctx.Spawn("Snapjaw").At(20, 20);

            ctx.Verify()
                .Entity(a).IsAt(10, 10).Back()
                .Entity(b).IsAt(20, 20).Back()
                .EntityCount(withTag: "Creature", expected: 3);
        }
    }
}
