using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class EntityVisualStateTests
    {
        [SetUp]
        public void SetUp()
        {
            EntityVisualHooks.Reset();
            MessageLog.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EntityVisualHooks.Reset();
        }

        private static Entity Actor(string id, string blueprint, string glyph = "@", int hp = 20)
        {
            var entity = new Entity { ID = id, BlueprintName = blueprint };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat
            {
                Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp,
            };
            entity.Statistics["Strength"] = new Stat
            {
                Name = "Strength", BaseValue = 18, Min = 1, Max = 50,
            };
            entity.Statistics["Agility"] = new Stat
            {
                Name = "Agility", BaseValue = 18, Min = 1, Max = 50,
            };
            entity.AddPart(new RenderPart
            {
                DisplayName = blueprint,
                RenderString = glyph,
                RenderLayer = 20,
            });
            return entity;
        }

        [Test]
        public void SuccessfulMovement_SavesFacingAndEmitsResolvedCoordinates()
        {
            var zone = new Zone("movement-visual");
            Entity actor = Actor("p", "Player");
            actor.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(actor, 5, 5);

            Entity movedEntity = null;
            int oldX = -1, oldY = -1, newX = -1, newY = -1;
            bool forced = true;
            EntityVisualHooks.MovedCallback = (entity, eventZone, ox, oy, nx, ny, wasForced) =>
            {
                movedEntity = entity;
                oldX = ox;
                oldY = oy;
                newX = nx;
                newY = ny;
                forced = wasForced;
                Assert.AreSame(zone, eventZone);
            };

            Assert.IsTrue(MovementSystem.TryMove(actor, zone, 1, 0));

            Assert.AreSame(actor, movedEntity);
            Assert.AreEqual((5, 5, 6, 5), (oldX, oldY, newX, newY));
            Assert.IsFalse(forced);
            Assert.AreEqual(EntityVisualFacing.East, actor.GetPart<RenderPart>().VisualFacing);
        }

        [Test]
        public void RejectedMovement_DoesNotEmitOrChangeFacing()
        {
            var zone = new Zone("blocked-visual");
            Entity actor = Actor("p", "Player");
            actor.AddPart(new PhysicsPart { Solid = true });
            actor.GetPart<RenderPart>().VisualFacing = EntityVisualFacing.North;
            zone.AddEntity(actor, 0, 0);
            int calls = 0;
            EntityVisualHooks.MovedCallback = (_, __, ___, ____, _____, ______, _______) => calls++;

            Assert.IsFalse(MovementSystem.TryMove(actor, zone, -1, 0));
            Assert.AreEqual(0, calls);
            Assert.AreEqual(EntityVisualFacing.North, actor.GetPart<RenderPart>().VisualFacing);
        }

        [Test]
        public void AcceptedAttack_EmitsOnceAndFacesTheDefender()
        {
            var zone = new Zone("attack-visual");
            Entity attacker = Actor("a", "Player");
            Entity defender = Actor("d", "Villager");
            attacker.AddPart(new MeleeWeaponPart { BaseDamage = "1d2" });
            zone.AddEntity(attacker, 4, 4);
            zone.AddEntity(defender, 4, 5);
            int calls = 0;
            EntityVisualHooks.AttackCallback = (a, d, z) =>
            {
                calls++;
                Assert.AreSame(attacker, a);
                Assert.AreSame(defender, d);
                Assert.AreSame(zone, z);
            };

            Assert.IsTrue(CombatSystem.PerformMeleeAttack(attacker, defender, zone, new System.Random(7)));
            Assert.AreEqual(1, calls);
            Assert.AreEqual(EntityVisualFacing.South, attacker.GetPart<RenderPart>().VisualFacing);
        }

        [Test]
        public void LethalDamage_EmitsDamageThenDeathAtTheOriginalCell()
        {
            var zone = new Zone("damage-visual");
            Entity source = Actor("a", "Player");
            Entity target = Actor("d", "Villager", hp: 3);
            zone.AddEntity(source, 4, 4);
            zone.AddEntity(target, 6, 7);
            string sequence = "";
            int landed = -1;
            EntityVisualHooks.DamageCallback = (damaged, attacker, eventZone, amount, lethal) =>
            {
                sequence += "damage";
                landed = amount;
                Assert.IsTrue(lethal);
                Assert.AreSame(target, damaged);
            };
            EntityVisualHooks.DeathCallback = (dead, killer, eventZone, x, y) =>
            {
                sequence += ">death";
                Assert.AreEqual((6, 7), (x, y));
                Assert.AreSame(target, dead);
                Assert.AreSame(source, killer);
                Assert.AreSame(zone, eventZone);
            };

            CombatSystem.ApplyDamage(target, 9, source, zone);

            Assert.AreEqual("damage>death", sequence);
            Assert.AreEqual(3, landed, "visual feedback reports landed HP loss, not overkill");
            Assert.IsNull(zone.GetEntityCell(target));
        }

        [Test]
        public void AnimatedRenderer_RebuildsAVisibleActorViewFromZoneState()
        {
            var root = new GameObject("AnimatedEntityRendererTest");
            try
            {
                var renderer = root.AddComponent<AnimatedEntityRenderer>();
                renderer.Init((_, __, ___) => Color.white);
                var zone = new Zone("renderer-visual");
                Entity player = Actor("p", "Player");
                player.Tags["Player"] = "";
                zone.AddEntity(player, 8, 9);
                Cell cell = zone.GetCell(8, 9);
                cell.Explored = true;
                cell.IsVisible = true;

                renderer.SetZone(zone);
                renderer.SyncZone(zone);

                Assert.AreEqual(1, renderer.ActiveViewCount);
                Assert.IsTrue(renderer.CanRender(player));
                Transform body = root.transform.Find("Visual_Player/Body");
                Assert.IsNotNull(body);
                Assert.IsNotNull(body.GetComponent<SpriteRenderer>().sprite);

                renderer.SetPresentationVisible(false);
                Assert.IsFalse(body.parent.gameObject.activeSelf);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Cast_UsesCapturedAimAfterTargetRemovalAndRetainsSelfFacing()
        {
            var zone = new Zone("cast-visual");
            Entity caster = Actor("caster", "NoAuthoredArt");
            Entity target = Actor("target", "Villager");
            zone.AddEntity(caster, 4, 4);
            zone.AddEntity(target, 8, 4);
            zone.RemoveEntity(target);
            int calls = 0;
            EntityVisualHooks.CastCallback = (actor, eventZone, spell, sx, sy, tx, ty, duration) =>
            {
                calls++;
                Assert.AreSame(caster, actor);
                Assert.AreSame(zone, eventZone);
                Assert.AreEqual("Pyromancy_Kindle", spell);
                Assert.AreEqual((4, 4), (sx, sy));
                Assert.Greater(duration, 0f);
            };

            EntityVisualHooks.EmitCast(caster, zone, "Pyromancy_Kindle", 4, 4, 8, 4);
            Assert.AreEqual(EntityVisualFacing.East, caster.GetPart<RenderPart>().VisualFacing);
            EntityVisualHooks.EmitCast(caster, zone, "Pyromancy_Kindle", 4, 4, 4, 4);
            Assert.AreEqual(EntityVisualFacing.East, caster.GetPart<RenderPart>().VisualFacing);
            Assert.AreEqual(2, calls);
            EntityVisualHooks.Reset();
            EntityVisualHooks.EmitCast(caster, zone, "Pyromancy_Kindle", 4, 4, 4, 3);
            Assert.AreEqual(2, calls, "Reset must release transient casting subscribers.");
            Assert.DoesNotThrow(() => EntityVisualHooks.EmitCast(null, null, null, 0, 0, 0, 0));
        }

        [Test]
        public void AnimatedCast_UsesCastingArtAndReturnsToIdleWithoutMovingTheCell()
        {
            var root = new GameObject("AnimatedCastTest");
            try
            {
                var renderer = root.AddComponent<AnimatedEntityRenderer>();
                renderer.Init((_, __, ___) => Color.white);
                var zone = new Zone("cast-renderer");
                Entity caster = Actor("caster", "Player");
                zone.AddEntity(caster, 6, 6);
                zone.GetCell(6, 6).Explored = true;
                zone.GetCell(6, 6).IsVisible = true;
                renderer.SyncZone(zone);
                Assert.IsTrue(EntityVisualCatalog.TryGetAsset(caster, out var asset));
                Transform body = root.transform.Find("Visual_Player/Body");
                Vector3 anchor = body.parent.position;

                Sprite idle = body.GetComponent<SpriteRenderer>().sprite;
                zone.GetCell(6, 6).IsVisible = false;
                EntityVisualHooks.EmitCast(caster, zone, "Hydromancy_JetBlast", 6, 6, 9, 6, 0.2f);
                Assert.AreSame(idle, body.GetComponent<SpriteRenderer>().sprite,
                    "A hidden source must not begin a casting pose.");
                zone.GetCell(6, 6).IsVisible = true;

                EntityVisualHooks.EmitCast(caster, zone, "Hydromancy_JetBlast", 6, 6, 9, 6, 0.2f);

                Assert.AreSame(asset.GetFrame(EntityVisualState.Cast, EntityVisualFacing.East, 0),
                    body.GetComponent<SpriteRenderer>().sprite);
                Assert.AreEqual(anchor, body.parent.position);
                Assert.AreEqual((6, 6), zone.GetEntityPosition(caster));
                renderer.ClearViews();
                renderer.SyncZone(zone);
                body = root.transform.Find("Visual_Player/Body");
                Assert.AreSame(asset.GetFrame(EntityVisualState.Idle, EntityVisualFacing.East, 0),
                    body.GetComponent<SpriteRenderer>().sprite, "Rebuild must not retain a cast frame.");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
