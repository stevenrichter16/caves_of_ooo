using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Dedicated A03 adversarial surface: field omissions, multiplicity, history and integration.</summary>
    public class GameAuditStackIdentityAdversarialTests : StackIdentityFixture
    {
        private Entity Payload(Type type)
        {
            var item = Item("Starapple"); var part = (Part)Activator.CreateInstance(type); item.AddPart(part);
            if (part is WeaponTemperPart temper) temper.TemperCount = 1;
            return item;
        }
        [TestCase(typeof(TonicPart))]
        [TestCase(typeof(BrewItemPart))]
        [TestCase(typeof(StatusTonicPart))]
        [TestCase(typeof(CureTonicPart))]
        [TestCase(typeof(MeleeWeaponPart))]
        [TestCase(typeof(WeaponTemperPart))]
        [TestCase(typeof(EnhancementPaleSalt))]
        [TestCase(typeof(EnhancementChoirIron))]
        [TestCase(typeof(EnhancementSerrated))]
        [TestCase(typeof(EnhancementLacquered))]
        [TestCase(typeof(EnhancementGlowQuartz))]
        [TestCase(typeof(EnhancementEngraved))]
        public void EveryPublicPayloadFieldHasAMutationAndRestoredCountercheck(Type type)
        {
            var a = Payload(type); var b = a.CloneForStack(); Compatible(a, b, true);
            var part = b.Parts.Single(p => p.GetType() == type);
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.DeclaringType != typeof(Part)))
            {
                object before = field.GetValue(part);
                object after = field.FieldType == typeof(string) ? (string)before + "different" :
                    field.FieldType == typeof(int) ? (object)((int)before + 1) :
                    field.FieldType == typeof(bool) ? (object)!(bool)before :
                    field.FieldType == typeof(float) ? (object)((float)before + 1f) : throw new Exception("Uncovered payload field " + field.Name);
                field.SetValue(part, after);
                Assert.IsFalse(a.GetPart<StackerPart>().CanStackWith(b), type.Name + "." + field.Name);
                Assert.IsFalse(b.GetPart<StackerPart>().CanStackWith(a), "symmetric " + field.Name);
                field.SetValue(part, before); Compatible(a, b, true);
            }
        }
        [TestCase(typeof(TonicPart))]
        [TestCase(typeof(BrewItemPart))]
        [TestCase(typeof(StatusTonicPart))]
        [TestCase(typeof(CureTonicPart))]
        [TestCase(typeof(MeleeWeaponPart))]
        [TestCase(typeof(WeaponTemperPart))]
        [TestCase(typeof(EnhancementPaleSalt))]
        [TestCase(typeof(EnhancementChoirIron))]
        [TestCase(typeof(EnhancementSerrated))]
        [TestCase(typeof(EnhancementLacquered))]
        [TestCase(typeof(EnhancementGlowQuartz))]
        [TestCase(typeof(EnhancementEngraved))]
        public void PartPresenceAndDuplicateOccurrenceAreBothSignificant(Type type)
        {
            var a = Payload(type); var b = Item("Starapple"); Compatible(a, b, false);
            var copy = a.CloneForStack(); var part = copy.Parts.Single(p => p.GetType() == type);
            copy.RemovePart(part); b.AddPart(part); Compatible(a, b, true);
            var extra = (Part)Activator.CreateInstance(type);
            if (extra is WeaponTemperPart temper) temper.TemperCount = 1;
            b.AddPart(extra); Compatible(a, b, false);
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void DispatchOrderAndDuplicateSecondPayloadAreNotSortedAway(bool enhancement, bool reverse)
        {
            var a = Item("Starapple");
            a.AddPart(enhancement ? (Part)new EnhancementPaleSalt { BonusDamage = 2 } : new StatusTonicPart { EffectName = "Burning" });
            a.AddPart(enhancement ? (Part)new EnhancementPaleSalt { BonusDamage = 4 } : new StatusTonicPart { EffectName = "Frozen" });
            var b = a.CloneForStack();
            if (reverse) { int n = b.Parts.Count; (b.Parts[n - 2], b.Parts[n - 1]) = (b.Parts[n - 1], b.Parts[n - 2]); }
            Compatible(a, b, !reverse);
        }
        [TestCase(typeof(UnknownEnhancement))] [TestCase(typeof(UnknownTonic))] [TestCase(typeof(UnknownTemper))]
        public void UnknownPayloadSubclassDoesNotLoseUncomparedState(Type type)
        { var a = Payload(type); var b = a.CloneForStack(); Compatible(a, b, false); }
        [TestCase(false)] [TestCase(true)] public void UnrelatedHistoryAndOwnersDoNotDefinePayload(bool light)
        {
            var a = Infused(1); var b = Infused(1); b.Properties["unrelated"] = "history";
            if (light) b.AddPart(new LightSourcePart { Radius = 0 });
            a.GetPart<StackerPart>().StackCount = 2; b.GetPart<StackerPart>().StackCount = 7;
            b.GetPart<PhysicsPart>().InInventory = Actor(); Compatible(a, b, true);
        }
        [TestCase(false)] [TestCase(true)] public void CachedParsingDoesNotFreezeFunctionalIdentity(bool melee)
        {
            var a = melee ? Tempered(false) : Brew(Actor(), false, false); var b = a.CloneForStack();
            if (melee) { var ignored = b.GetPart<MeleeWeaponPart>().OnHitEffectsCachedSpecs; }
            else b.GetPart<BrewItemPart>().GetEffects();
            Compatible(a, b, true);
            if (melee) b.GetPart<MeleeWeaponPart>().OnHitEffectsRaw += ";Burning,100,,0,1";
            else b.GetPart<BrewItemPart>().EffectsRaw += ";Burning:1";
            Compatible(a, b, false);
        }
        [TestCase(false)] [TestCase(true)] public void ContainerKeepsDifferentPayloadAndMergesEquivalent(bool different)
        {
            var a = Infused(1); var b = Infused(different ? 2 : 1); var sack = Item("Sack"); var contents = sack.GetPart<ContainerPart>();
            Assert.IsTrue(contents.AddItem(a)); Assert.IsTrue(contents.AddItem(b));
            Assert.AreEqual(different ? 2 : 1, contents.Contents.Count); Assert.AreEqual(different ? 1 : 2, a.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(different ? 1 : 0, b.GetPart<StackerPart>().StackCount);
        }
        [TestCase(false)] [TestCase(true)] public void PickupRollbackPreservesBothPayloadsAndQuantities(bool different)
        {
            var actor = Actor(); var inv = actor.GetPart<InventoryPart>(); var a = Infused(1); var b = Infused(different ? 2 : 1);
            a.GetPart<StackerPart>().StackCount = 2; b.GetPart<StackerPart>().StackCount = 3;
            Assert.IsTrue(inv.AddObject(a)); var zone = new Zone("StackAudit"); zone.AddEntity(actor, 10, 10); zone.AddEntity(b, 11, 10);
            var outer = new FailAfter(new PickupCommand(b), () =>
            {
                Assert.AreEqual(different ? 2 : 1, inv.Objects.Count, "inner pickup actually reached destination");
                Assert.AreEqual(different ? 2 : 5, a.GetPart<StackerPart>().StackCount);
                Assert.AreEqual(different ? 3 : 0, b.GetPart<StackerPart>().StackCount);
                Assert.IsNull(zone.GetEntityCell(b));
            });
            var result = InventorySystem.ExecuteCommand(outer, actor, zone);
            Assert.IsTrue(outer.InnerSucceeded); Assert.IsFalse(result.Success); Assert.AreEqual("Injected outer failure.", result.ErrorMessage);
            Assert.AreEqual(2, a.GetPart<StackerPart>().StackCount); Assert.AreEqual(3, b.GetPart<StackerPart>().StackCount);
            Assert.AreSame(a, inv.Objects.Single()); Assert.AreEqual((11, 10), zone.GetEntityPosition(b));
            Assert.AreEqual(1, Upgrades(a)); Assert.AreEqual(different ? 2 : 1, Upgrades(b));
            Compatible(a, b, !different);
        }
        private static int Upgrades(Entity item) => item.Parts.OfType<EnhancementPaleSalt>().Count();
        [TestCase(false)] [TestCase(true)] public void ChargedBooksStillNeverStack(bool chargedOnBoth)
        {
            var a = Item("Starapple"); var b = Item("Starapple"); a.AddPart(new GrimoireChargePart());
            if (chargedOnBoth) b.AddPart(new GrimoireChargePart()); Compatible(a, b, false);
        }
        [TestCase(false)] [TestCase(true)] public void DisplayAndBlueprintGatesRemain(bool blueprint)
        {
            var a = Infused(1); var b = a.CloneForStack();
            if (blueprint) b.BlueprintName = "Other"; else b.GetPart<RenderPart>().DisplayName += " other";
            Compatible(a, b, false);
        }
        [TestCase(false)] [TestCase(true)] public void MismatchDiagnosticsRespectPreferencesAndExplainPayload(bool enabled)
        {
            var a = Infused(1); var b = Infused(2); Diag.ResetAll(); Diag.SetChannel("event", enabled);
            Assert.IsFalse(a.GetPart<StackerPart>().CanStackWith(b));
            var records = DiagQuery.Apply(new DiagQuery.Filter { Kind = "StackPayloadMismatch", Actor = a.ID, Target = b.ID }).Records;
            Assert.AreEqual(enabled ? 1 : 0, records.Count);
            if (enabled) StringAssert.Contains("different_crafted_payload", records.Single().PayloadJson);
            var copy = a.CloneForStack(); Compatible(a, copy, true);
            Assert.AreEqual(enabled ? 1 : 0, DiagQuery.Count(new DiagQuery.Filter { Kind = "StackPayloadMismatch" }).Count);
        }
        [TestCase(false)] [TestCase(true)] public void RealReforgeHistoryDoesNotPreventEquivalentPayloadMerge(bool enhancement)
        {
            var actorA = Actor(); var actorB = Actor();
            var a = Reforged(actorA, true, enhancement); var b = Reforged(actorB, false, enhancement);
            Assert.AreEqual(Name(a), Name(b)); Compatible(a, b, true);
            if (enhancement)
            {
                Assert.Less(a.Parts.FindIndex(p => p is WeaponTemperPart), a.Parts.FindIndex(p => p is EnhancementPaleSalt));
                Assert.Greater(b.Parts.FindIndex(p => p is WeaponTemperPart), b.Parts.FindIndex(p => p is EnhancementPaleSalt));
            }
            else { Assert.NotNull(a.GetPart<WeaponTemperPart>()); Assert.IsNull(b.GetPart<WeaponTemperPart>()); }
            Assert.IsTrue(actorB.GetPart<InventoryPart>().RemoveObject(b)); Assert.IsTrue(actorA.GetPart<InventoryPart>().AddObject(b));
            Assert.AreEqual(2, a.GetPart<StackerPart>().StackCount); Assert.IsFalse(actorA.GetPart<InventoryPart>().Contains(b));
        }
        private Entity Reforged(Entity actor, bool temperFirst, bool enhancement)
        {
            var blade = Give(actor, "SteelBladeComponent"); var haft = Give(actor, "OakHaftComponent"); var binding = Give(actor, "LeatherBindingComponent");
            Assert.IsTrue(WeaponForgingService.TryForge(actor, Factory, blade, haft, binding, out var weapon, out var reason), reason);
            if (!enhancement && !temperFirst) return weapon;
            if (temperFirst) Temper();
            if (enhancement)
            { Give(actor, "PaleSalt"); Assert.IsTrue(TinkeringService.TryApplyModification(actor, "mod_palesalt_infuse", weapon, out reason), reason); }
            if (!temperFirst) Temper();
            var oak = Give(actor, "OakHaftComponent");
            Assert.IsTrue(WeaponForgingService.TryReforge(actor, Factory, weapon, oak, out var returned, out reason), reason);
            Assert.AreEqual("OakHaftComponent", returned.BlueprintName); Assert.AreEqual(0, weapon.GetPart<WeaponTemperPart>().TemperCount);
            return weapon;
            void Temper() { var brew = Brew(actor, false, false); Assert.IsTrue(WeaponTemperingService.TryTemper(actor, weapon, brew, out var why), why); }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void EmptyTemperNormalizationPreservesFirstMarkerCapacity(int shape)
        {
            var a = Item("Dagger"); var b = Item("Dagger");
            a.AddPart(new WeaponTemperPart());
            if (shape > 0)
            {
                a.AddPart(new WeaponTemperPart { TemperCount = 1, AppliedSpecsRaw = "Burning,30,,0,1" });
                b.AddPart(new WeaponTemperPart { TemperCount = 1, AppliedSpecsRaw = "Burning,30,,0,1" });
                if (shape == 2) b.AddPart(new WeaponTemperPart());
                Assert.AreEqual(0, a.GetPart<WeaponTemperPart>().TemperCount);
                Assert.AreEqual(1, b.GetPart<WeaponTemperPart>().TemperCount);
            }
            Compatible(a, b, shape == 0);
        }
        [Test] public void UnknownDefaultEmptyTemperIsNotNormalizedAway()
        { var a = Item("Dagger"); var b = Item("Dagger"); a.AddPart(new UnknownTemper()); b.AddPart(new UnknownTemper()); Compatible(a, b, false); }
        [TestCase("blueprint")] [TestCase("name")] [TestCase("charge")]
        public void EarlierRefusalGatesDoNotMisreportPayloadMismatch(string gate)
        {
            var a = Infused(1); var b = Infused(2); Diag.ResetAll(); Diag.SetChannel("event", true);
            if (gate == "blueprint") b.BlueprintName = "Other";
            if (gate == "name") b.GetPart<RenderPart>().DisplayName += " other";
            if (gate == "charge") b.AddPart(new GrimoireChargePart());
            Compatible(a, b, false); Assert.AreEqual(0, DiagQuery.Count(new DiagQuery.Filter { Kind = "StackPayloadMismatch" }).Count);
        }
        public sealed class UnknownEnhancement : EnhancementPaleSalt { public int Extra = 2; }
        public sealed class UnknownTonic : TonicPart { public int Extra = 2; }
        public sealed class UnknownTemper : WeaponTemperPart { public int Extra = 2; }
        private sealed class FailAfter : IInventoryCommand
        {
            private readonly IInventoryCommand _inner; private readonly Action _beforeRefusal;
            internal bool InnerSucceeded;
            internal FailAfter(IInventoryCommand inner, Action beforeRefusal) { _inner = inner; _beforeRefusal = beforeRefusal; }
            public string Name => "StackAuditOuterFailure";
            public InventoryValidationResult Validate(InventoryContext context) => _inner.Validate(context);
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            {
                var result = _inner.Execute(context, transaction); InnerSucceeded = result.Success;
                if (!result.Success) return result;
                _beforeRefusal(); return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Injected outer failure.");
            }
        }
    }
}
