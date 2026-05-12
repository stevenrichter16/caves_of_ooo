using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.2.1 — unit tests for
    /// <see cref="ItemEnhancementDispatch"/>, the static helper that
    /// fans enhancement hooks (OnAttackerHit / OnEquipped / OnUnequipped)
    /// out to every <see cref="IItemEnhancement"/> Part on an item.
    ///
    /// <para><b>What's pinned here:</b></para>
    /// <list type="bullet">
    ///   <item>Each dispatcher iterates the item's Parts and calls the
    ///         right hook exactly once per enhancement Part.</item>
    ///   <item>Non-enhancement Parts (RenderPart, PhysicsPart) are skipped.</item>
    ///   <item>Multiple enhancements on the same item all dispatch.</item>
    ///   <item>Null-safety: null item / null actor / null defender / null
    ///         Parts list never throw.</item>
    ///   <item>An item with zero enhancement Parts is a no-op.</item>
    /// </list>
    ///
    /// <para><b>Not pinned here (deferred to concrete-enhancement tests):</b>
    /// integration with <c>CombatSystem.PerformSingleAttack</c>,
    /// <c>EquipCommand</c>, <c>UnequipCommand</c>. Those call sites are
    /// one-line invocations exercised by the per-enhancement test fixtures
    /// (EnhancementSerratedTests touches the combat path,
    /// EnhancementLacqueredTests touches the equip path, etc.).</para>
    /// </summary>
    public class ItemEnhancementDispatchTests
    {
        // ── Stub enhancements that count their calls ─────────────────

        public class CallCountingHitEnh : IItemEnhancement
        {
            public override string Name => nameof(CallCountingHitEnh);
            public int OnAttackerHitCount;
            public Entity LastDefender;
            public Entity LastAttacker;
            public Damage LastDamage;
            public int LastActualDamage;
            public Zone LastZone;
            public Random LastRng;

            public override void OnAttackerHit(
                Entity defender, Entity attacker, Damage damage,
                int actualDamage, Zone zone, Random rng)
            {
                OnAttackerHitCount++;
                LastDefender = defender;
                LastAttacker = attacker;
                LastDamage = damage;
                LastActualDamage = actualDamage;
                LastZone = zone;
                LastRng = rng;
            }
        }

        public class CallCountingEquipEnh : IItemEnhancement
        {
            public override string Name => nameof(CallCountingEquipEnh);
            public int OnEquippedCount;
            public int OnUnequippedCount;
            public Entity LastEquippedActor;
            public Entity LastEquippedItem;
            public Entity LastUnequippedActor;
            public Entity LastUnequippedItem;

            public override void OnEquipped(Entity actor, Entity item)
            {
                OnEquippedCount++;
                LastEquippedActor = actor;
                LastEquippedItem = item;
            }
            public override void OnUnequipped(Entity actor, Entity item)
            {
                OnUnequippedCount++;
                LastUnequippedActor = actor;
                LastUnequippedItem = item;
            }
        }

        private static Entity MakeItem(string id = "weapon")
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Takeable = true });
            return e;
        }

        private static Entity MakeActor(string id = "hero")
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.AddPart(new RenderPart { DisplayName = id });
            return e;
        }

        // ════════════════════════════════════════════════════════════════
        // DispatchOnHit — fans to every enhancement, skips non-enhancements
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void DispatchOnHit_WithOneEnhancement_CallsOnAttackerHitOnce()
        {
            var weapon = MakeItem();
            var enh = new CallCountingHitEnh();
            weapon.AddPart(enh);

            var attacker = MakeActor("attacker");
            var defender = MakeActor("defender");
            var damage = new Damage(5);
            var rng = new Random(0);

            ItemEnhancementDispatch.DispatchOnHit(
                weapon, defender, attacker, damage, 5, zone: null, rng);

            Assert.AreEqual(1, enh.OnAttackerHitCount,
                "OnAttackerHit fires exactly once per dispatch.");
            Assert.AreSame(defender, enh.LastDefender);
            Assert.AreSame(attacker, enh.LastAttacker);
            Assert.AreSame(damage, enh.LastDamage);
            Assert.AreEqual(5, enh.LastActualDamage);
            Assert.AreSame(rng, enh.LastRng);
        }

        [Test]
        public void DispatchOnHit_WithTwoEnhancements_FiresBothOnce()
        {
            var weapon = MakeItem();
            var a = new CallCountingHitEnh();
            var b = new CallCountingHitEnh();
            weapon.AddPart(a);
            weapon.AddPart(b);

            ItemEnhancementDispatch.DispatchOnHit(
                weapon, MakeActor(), MakeActor(),
                new Damage(1), 1, zone: null, rng: new Random(0));

            Assert.AreEqual(1, a.OnAttackerHitCount);
            Assert.AreEqual(1, b.OnAttackerHitCount);
        }

        [Test]
        public void DispatchOnHit_NonEnhancementParts_AreSkipped()
        {
            // Item has Render + Physics Parts but NO IItemEnhancement.
            // No throw, no side effect — silent no-op.
            var weapon = MakeItem();
            Assert.DoesNotThrow(() => ItemEnhancementDispatch.DispatchOnHit(
                weapon, MakeActor(), MakeActor(),
                new Damage(1), 1, zone: null, rng: new Random(0)));
        }

        [Test]
        public void DispatchOnHit_NullWeapon_NoThrow()
        {
            Assert.DoesNotThrow(() => ItemEnhancementDispatch.DispatchOnHit(
                weaponItem: null, defender: MakeActor(), attacker: MakeActor(),
                damage: new Damage(1), actualDamage: 1, zone: null,
                rng: new Random(0)));
        }

        [Test]
        public void DispatchOnHit_NullDefender_StillCallsEnhancement_HookHandlesNull()
        {
            // Contract: dispatcher does NOT pre-filter null defender —
            // it's the enhancement's responsibility to no-op on null.
            // This lets dispatch fire for hooks that don't care about
            // the defender (rare but possible). Counter-check pair with
            // the "null defender → no crash" expectation.
            var weapon = MakeItem();
            var enh = new CallCountingHitEnh();
            weapon.AddPart(enh);

            Assert.DoesNotThrow(() => ItemEnhancementDispatch.DispatchOnHit(
                weapon, defender: null, attacker: MakeActor(),
                damage: new Damage(1), actualDamage: 1, zone: null,
                rng: new Random(0)));
            Assert.AreEqual(1, enh.OnAttackerHitCount,
                "Hook still fired — concrete enhancements that need a " +
                "non-null defender must guard themselves.");
            Assert.IsNull(enh.LastDefender);
        }

        // ════════════════════════════════════════════════════════════════
        // DispatchOnEquip — symmetric pair to DispatchOnUnequip
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void DispatchOnEquip_WithOneEnhancement_CallsOnEquippedOnce()
        {
            var item = MakeItem();
            var enh = new CallCountingEquipEnh();
            item.AddPart(enh);
            var actor = MakeActor();

            ItemEnhancementDispatch.DispatchOnEquip(actor, item);

            Assert.AreEqual(1, enh.OnEquippedCount);
            Assert.AreEqual(0, enh.OnUnequippedCount,
                "DispatchOnEquip does NOT cross-fire OnUnequipped.");
            Assert.AreSame(actor, enh.LastEquippedActor);
            Assert.AreSame(item, enh.LastEquippedItem);
        }

        [Test]
        public void DispatchOnUnequip_WithOneEnhancement_CallsOnUnequippedOnce()
        {
            var item = MakeItem();
            var enh = new CallCountingEquipEnh();
            item.AddPart(enh);
            var actor = MakeActor();

            ItemEnhancementDispatch.DispatchOnUnequip(actor, item);

            Assert.AreEqual(1, enh.OnUnequippedCount);
            Assert.AreEqual(0, enh.OnEquippedCount,
                "DispatchOnUnequip does NOT cross-fire OnEquipped.");
            Assert.AreSame(actor, enh.LastUnequippedActor);
            Assert.AreSame(item, enh.LastUnequippedItem);
        }

        [Test]
        public void DispatchOnEquip_NullItem_NoThrow()
        {
            Assert.DoesNotThrow(() => ItemEnhancementDispatch.DispatchOnEquip(
                actor: MakeActor(), item: null));
        }

        [Test]
        public void DispatchOnUnequip_NullItem_NoThrow()
        {
            Assert.DoesNotThrow(() => ItemEnhancementDispatch.DispatchOnUnequip(
                actor: MakeActor(), item: null));
        }

        [Test]
        public void DispatchOnEquip_NullActor_HookStillFires()
        {
            // Same contract as DispatchOnHit with null defender: hook
            // fires with null, enhancement must guard if it cares.
            var item = MakeItem();
            var enh = new CallCountingEquipEnh();
            item.AddPart(enh);
            ItemEnhancementDispatch.DispatchOnEquip(actor: null, item: item);
            Assert.AreEqual(1, enh.OnEquippedCount);
            Assert.IsNull(enh.LastEquippedActor);
        }

        [Test]
        public void DispatchOnEquip_MultipleEnhancements_AllFire()
        {
            var item = MakeItem();
            var a = new CallCountingEquipEnh();
            var b = new CallCountingEquipEnh();
            item.AddPart(a);
            item.AddPart(b);

            ItemEnhancementDispatch.DispatchOnEquip(MakeActor(), item);

            Assert.AreEqual(1, a.OnEquippedCount);
            Assert.AreEqual(1, b.OnEquippedCount);
        }

        [Test]
        public void DispatchOnUnequip_MultipleEnhancements_AllFire()
        {
            var item = MakeItem();
            var a = new CallCountingEquipEnh();
            var b = new CallCountingEquipEnh();
            item.AddPart(a);
            item.AddPart(b);

            ItemEnhancementDispatch.DispatchOnUnequip(MakeActor(), item);

            Assert.AreEqual(1, a.OnUnequippedCount);
            Assert.AreEqual(1, b.OnUnequippedCount);
        }

        // ════════════════════════════════════════════════════════════════
        // Cross-dispatcher isolation — each dispatcher hits its own hook
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Dispatchers_AreIsolated_NoCrossFiring()
        {
            // Pair-check: with one item carrying enhancement(s) that
            // count BOTH hit and equip calls, dispatching one variant
            // must not increment the other variant's counter.
            var item = MakeItem();
            var hit = new CallCountingHitEnh();
            var equip = new CallCountingEquipEnh();
            item.AddPart(hit);
            item.AddPart(equip);

            ItemEnhancementDispatch.DispatchOnHit(
                item, MakeActor(), MakeActor(),
                new Damage(1), 1, zone: null, rng: new Random(0));
            Assert.AreEqual(1, hit.OnAttackerHitCount);
            Assert.AreEqual(0, equip.OnEquippedCount);
            Assert.AreEqual(0, equip.OnUnequippedCount);

            ItemEnhancementDispatch.DispatchOnEquip(MakeActor(), item);
            Assert.AreEqual(1, hit.OnAttackerHitCount,
                "DispatchOnEquip does not re-fire OnAttackerHit.");
            Assert.AreEqual(1, equip.OnEquippedCount);
            Assert.AreEqual(0, equip.OnUnequippedCount);

            ItemEnhancementDispatch.DispatchOnUnequip(MakeActor(), item);
            Assert.AreEqual(1, hit.OnAttackerHitCount);
            Assert.AreEqual(1, equip.OnEquippedCount);
            Assert.AreEqual(1, equip.OnUnequippedCount);
        }
    }
}
