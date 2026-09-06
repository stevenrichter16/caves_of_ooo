using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // 26 dedicated cases; the separate 3 alias cases cover shared saved references.
    // Positive factory cases NEVER call UpdateBodyParts/RegenerateDefaultEquipment.
    public class GameAuditNaturalWeaponActivationAdversarialTests
    {
        // Literal actor/recipe baseline: 38 direct declarations, 2 inherited actors.
        static readonly string[] Declared = {
            "Snapjaw|SnapjawClaw", "SnapjawScavenger|SnapjawClaw", "SnapjawHunter|SnapjawHunterClaw",
            "ChoirTendril|ChoirLash", "CaveBat|BatBite", "CaveSlime|SlimePseudopod", "CaveBear|CaveBearClaw",
            "Glowmaw|GlowmawBite", "Scorpion|ScorpionSting", "DesertBandit|BanditBlade", "SandWurm|WurmBite",
            "GiantSpider|SpiderBite", "Viper|ViperBite", "JungleApe|ApeFist", "RuinScavenger|ScavengerClaw",
            "SkeletalSentry|BoneBlade", "StoneGolem|GolemFist", "PaleStalker|StalkerTalon", "ObsidianBrute|ObsidianFist",
            "SnapjawChieftain|SnapjawClaw", "SnapjawWarlord|WarlordCleaver", "Mosshulk|MosshulkSlam",
            "DesertProwler|ProwlerClaw", "DuneLurker|LurkerMaw", "BrittleHound|BrittleFangs", "JungleStalker|StalkerClaw",
            "Rotling|RotlingClaw", "CanopyStrangler|StranglerLash", "AncientGuardian|GuardianFist", "VaultSentinel|SentinelHalberd",
            "BrassHusk|HuskFist", "GlassScorpion|GlassSting", "SporeShambler|SporeTouch", "IceWight|WightTouch",
            "CharredHusk|HuskTouch", "SleepingTroll|TrollFist", "MimicChest|MimicBite", "AmbushBandit|BanditBlade",
            "RuneCultist|CultistKnife", "SunStriker|DefaultBite"
        };
        // Literal recipe dice baseline prevents a nonnull premature DefaultFist from passing.
        static readonly Dictionary<string,string> RecipeDice = new Dictionary<string,string> {
            {"SnapjawClaw","1d4"},{"SnapjawHunterClaw","1d6"},{"ChoirLash","2d4"},{"BatBite","1d2"},
            {"SlimePseudopod","1d3"},{"CaveBearClaw","2d4"},{"GlowmawBite","2d4"},{"ScorpionSting","1d3"},
            {"BanditBlade","1d6"},{"WurmBite","2d6+1"},{"SpiderBite","1d4"},{"ViperBite","1d3"},
            {"ApeFist","1d6+1"},{"ScavengerClaw","1d4"},{"BoneBlade","1d6+1"},{"GolemFist","2d6"},
            {"StalkerTalon","2d5"},{"ObsidianFist","3d6"},{"WarlordCleaver","2d5"},{"MosshulkSlam","2d5"},
            {"ProwlerClaw","2d4"},{"LurkerMaw","2d6"},{"BrittleFangs","1d6"},{"StalkerClaw","2d4"},
            {"RotlingClaw","1d3"},{"StranglerLash","2d4"},{"GuardianFist","2d6+2"},{"SentinelHalberd","2d6"},
            {"HuskFist","1d6"},{"GlassSting","1d4"},{"SporeTouch","1d4"},{"WightTouch","1d6"},
            {"HuskTouch","1d6"},{"TrollFist","2d6"},{"MimicBite","1d8"},{"CultistKnife","1d4"},{"DefaultBite","1d3+1"}
        };
        static List<BodyPart> Hands(Entity actor) => actor.GetPart<Body>().GetPartsByType("Hand");
        static Entity[] Items(Entity actor) => actor.GetPart<InventoryPart>().Objects.Concat(actor.GetPart<InventoryPart>().GetAllEquipped()).Distinct().ToArray();
        static string[] ItemSignature(Entity actor) => Items(actor).Select(e => e.ID + ":" + e.BlueprintName + ":" + (e.GetPart<StackerPart>()?.StackCount ?? 1)).OrderBy(s => s).ToArray();
        static List<MeleeWeaponPart> Weapons(Entity actor)
        {
            var slots = (IEnumerable)typeof(CombatSystem).GetMethod("GatherMeleeWeapons", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { actor, actor.GetPart<Body>() });
            // Copy the private shared scratch result immediately.
            return slots.Cast<object>().Select(x => (MeleeWeaponPart)x.GetType().GetField("Weapon").GetValue(x)).ToList();
        }
        static Entity RoundTrip(Entity actor)
        {
            var zone = new Zone("Overworld.10.10.0"); Assert.IsTrue(zone.AddEntity(actor, 5, 5));
            var manager = new OverworldZoneManager(null, 333);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { zone.ZoneID, zone } }, zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
            var turns = new TurnManager(); turns.RestoreSavedState(17, true, actor,
                new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = actor, Energy = 1000 } });
            return HotbarSaveFixture.RoundTrip(GameSessionState.Capture(Guid.NewGuid().ToString("N"), "natural adversarial", manager, turns, actor)).Player;
        }

        [Test]
        public void EveryResolvedAuthoredRecipeIsMaterializedBeforeAnyMaintenanceCall()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var resolved = f.Factory.Blueprints.Values.Where(b => b.Props.TryGetValue("NaturalWeapon", out var recipe) && !string.IsNullOrEmpty(recipe))
                    .Select(b => b.Name + "|" + b.Props["NaturalWeapon"]).ToArray();
                Assert.AreEqual(40, resolved.Length); CollectionAssert.AreEquivalent(Declared, resolved);
                foreach (string row in Declared)
                {
                    var fields = row.Split('|'); var actor = f.Create(fields[0]); var hands = Hands(actor);
                    Assert.AreEqual(2, hands.Count, row);
                    foreach (var hand in hands)
                    {
                        Assert.AreEqual(fields[1], hand.DefaultBehaviorBlueprint, row); Assert.NotNull(hand._DefaultBehavior, row);
                        Assert.IsTrue(hand._DefaultBehavior.HasTag("Natural"), row); Assert.IsTrue(hand.FirstSlotForDefaultBehavior, row);
                        var weapon = hand._DefaultBehavior.GetPart<MeleeWeaponPart>(); Assert.NotNull(weapon, row);
                        Assert.AreEqual(RecipeDice[fields[1]], weapon.BaseDamage, row);
                    }
                    Assert.AreNotSame(hands[0]._DefaultBehavior, hands[1]._DefaultBehavior, row);
                }
            }
        }
        [Test]
        public void SameRecipeActorsAndHandsOwnIndependentNaturalPayloads()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var a = f.Create("Viper"); var b = f.Create("Viper"); var all = Hands(a).Concat(Hands(b)).Select(p => p._DefaultBehavior).ToArray();
                Assert.IsTrue(all.All(e => e != null)); Assert.AreEqual(4, all.Distinct().Count());
                all[0].GetPart<MeleeWeaponPart>().BaseDamage = "7d9";
                Assert.IsTrue(all.Skip(1).All(e => e.GetPart<MeleeWeaponPart>().BaseDamage == "1d3"));
                Assert.AreEqual(2, Hands(a).Count(h => h.FirstSlotForDefaultBehavior)); Assert.AreEqual(2, Hands(b).Count(h => h.FirstSlotForDefaultBehavior));
            }
        }
        [Test]
        public void OrdinaryPlayerWithNoExplicitNaturalOverrideGetsTwoFists()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("Player"); Assert.IsTrue(string.IsNullOrEmpty(actor.GetProperty("NaturalWeapon")));
                Assert.AreEqual(2, Hands(actor).Count); Assert.AreEqual(2, Weapons(actor).Count);
                foreach (var hand in Hands(actor))
                { Assert.AreEqual("DefaultFist", hand.DefaultBehaviorBlueprint); Assert.NotNull(hand._DefaultBehavior); Assert.AreEqual("1d2", hand._DefaultBehavior.GetPart<MeleeWeaponPart>().BaseDamage); Assert.AreEqual("Bludgeoning Unarmed", hand._DefaultBehavior.GetPart<MeleeWeaponPart>().Attributes); }
            }
        }
        [Test]
        public void MaterializedNaturalsAreNotCarriedItemsEquipmentOrGroundLoot()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("SnapjawWarlord"); var naturals = Hands(actor).Select(h => h._DefaultBehavior).ToArray();
                Assert.IsTrue(naturals.All(e => e != null)); Assert.AreEqual(3, Items(actor).Length);
                var zone = new Zone("natural-not-loot"); Assert.IsTrue(zone.AddEntity(actor, 5, 5));
                Assert.IsTrue(naturals.All(e => !Items(actor).Contains(e) && zone.GetEntityCell(e) == null && e.GetPart<PhysicsPart>() == null));
                Assert.IsFalse(actor.GetPart<InventoryPart>().GetAllEquipped().Any(e => e.HasTag("Natural")));
            }
        }

        [TestCase("Viper", "Poisoned,75,1d6,8,0", "Poisoned", 75, "1d6", 8, 1)]
        [TestCase("Scorpion", "Poisoned,50,1d4,6,0", "Poisoned", 50, "1d4", 6, 1)]
        [TestCase("GiantSpider", "Poisoned,35,1d4,6,0;Paralyzed,20,0,2,0", "Poisoned", 35, "1d4", 6, 2)]
        [TestCase("BrassHusk", "Electrified,20,,3,1.0", "Electrified", 20, "", 3, 1)]
        [TestCase("BrittleHound", "Bleeding,25,1d2,10,0", "Bleeding", 25, "1d2", 10, 1)]
        [TestCase("Rotling", "Poisoned,10,1d2,4,0", "Poisoned", 10, "1d2", 4, 1)]
        public void FreshActualEffectRecipesReachNaturalPayloadAndParser(string blueprint, string raw, string effect, int chance, string dice, int duration, int count)
        {
            // Literal recipe/parser checks, not claims about effect runtime duration or future ticks.
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create(blueprint); Assert.AreEqual(2, Hands(actor).Count);
                foreach (var hand in Hands(actor))
                {
                    Assert.NotNull(hand._DefaultBehavior); var weapon = hand._DefaultBehavior.GetPart<MeleeWeaponPart>();
                    Assert.AreEqual(raw, weapon.OnHitEffectsRaw); var specs = weapon.OnHitEffectsCachedSpecs;
                    Assert.AreEqual(count, specs.Count); Assert.AreEqual(effect, specs[0].EffectName); Assert.AreEqual(chance, specs[0].ChancePercent);
                    Assert.AreEqual(dice, specs[0].DamageDice); Assert.AreEqual(duration, specs[0].DurationTurns); Assert.AreSame(specs, weapon.OnHitEffectsCachedSpecs);
                    if (count == 2) { Assert.AreEqual("Paralyzed", specs[1].EffectName); Assert.AreEqual(20, specs[1].ChancePercent); Assert.AreEqual(2, specs[1].DurationTurns); }
                }
            }
        }
        [Test]
        public void FreshSporeShamblerCarriesItsLiteralGasRecipeAndDefaults()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("SporeShambler");
                Assert.AreEqual(2, Hands(actor).Count);
                foreach (var hand in Hands(actor))
                {
                    Assert.NotNull(hand._DefaultBehavior); var weapon = hand._DefaultBehavior.GetPart<MeleeWeaponPart>();
                    Assert.AreEqual("fungal-spores,25", weapon.EmitGasOnHitRaw); var specs = weapon.EmitGasOnHitCachedSpecs; Assert.AreEqual(1, specs.Count);
                    Assert.AreEqual("fungal-spores", specs[0].GasId); Assert.AreEqual(25, specs[0].ChancePercent);
                    Assert.AreEqual(30, specs[0].CellDensity); Assert.AreEqual(15, specs[0].AdjacentDensity); Assert.AreEqual(1, specs[0].GasLevel);
                    Assert.AreSame(specs, weapon.EmitGasOnHitCachedSpecs); Assert.AreEqual(0, weapon.OnHitEffectsCachedSpecs.Count);
                }
            }
        }
        [Test]
        public void PlainBoneBladeCountercheckDoesNotAcquireOtherRecipesEffectsOrGas()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("SkeletalSentry"); Assert.AreEqual(2, Hands(actor).Count); foreach (var hand in Hands(actor))
                { Assert.NotNull(hand._DefaultBehavior); var weapon = hand._DefaultBehavior.GetPart<MeleeWeaponPart>(); Assert.AreEqual("1d6+1", weapon.BaseDamage); Assert.AreEqual("", weapon.OnHitEffectsRaw); Assert.AreEqual("", weapon.EmitGasOnHitRaw); Assert.AreEqual(0, weapon.OnHitEffectsCachedSpecs.Count); Assert.AreEqual(0, weapon.EmitGasOnHitCachedSpecs.Count); }
            }
        }
        [Test]
        public void ObjectCreatedAndBeforeEquipSeeFinalOverrideAndSameNaturalObjects()
        {
            using (var f = new LoadoutLifecycleFixture("Dagger"))
            {
                f.ActorBlueprint.Props["NaturalWeapon"] = "WarlordCleaver"; Entity[] observed = null; int created = 0, before = 0;
                LoadoutAuditProbe.AuditHook = (probe, e) => {
                    if (e.ID == "ObjectCreated")
                    { created++; Assert.AreEqual(0, Items(probe.ParentEntity).Length); observed = Hands(probe.ParentEntity).Select(h => h._DefaultBehavior).ToArray(); Assert.IsTrue(observed.All(x => x != null && x.GetPart<MeleeWeaponPart>().BaseDamage == "2d5")); }
                    if (e.ID == "BeforeEquip") { before++; CollectionAssert.AreEqual(observed, Hands(probe.ParentEntity).Select(h => h._DefaultBehavior).ToArray()); }
                    return true;
                };
                var actor = f.Create(); Assert.AreEqual(1, created); Assert.AreEqual(1, before);
                CollectionAssert.AreEqual(observed, Hands(actor).Select(h => h._DefaultBehavior).ToArray()); LoadoutLifecycleFixture.Equipped(actor, LoadoutLifecycleFixture.OnlyItem(actor));
            }
        }
        [Test]
        public void NestedLoadoutItemCreationSeesAlreadyCompleteParentNaturalState()
        {
            using (var f = new LoadoutLifecycleFixture("Dagger"))
            {
                f.ActorBlueprint.Props["NaturalWeapon"] = "ViperBite";
                f.Factory.Blueprints["Dagger"].Parts[nameof(LoadoutAuditProbe)] = new Dictionary<string,string>();
                Entity parent = null; Entity[] defaults = null; int nested = 0;
                LoadoutAuditProbe.AuditHook = (probe, e) => {
                    if (e.ID != "ObjectCreated") return true;
                    if (probe.ParentEntity.BlueprintName == f.ActorBlueprint.Name) { parent = probe.ParentEntity; defaults = Hands(parent).Select(h => h._DefaultBehavior).ToArray(); }
                    else if (probe.ParentEntity.BlueprintName == "Dagger")
                    { nested++; Assert.NotNull(parent); Assert.IsTrue(defaults.All(x => x != null && x.GetPart<MeleeWeaponPart>().OnHitEffectsRaw == "Poisoned,75,1d6,8,0")); CollectionAssert.AreEqual(defaults, Hands(parent).Select(h => h._DefaultBehavior).ToArray()); }
                    return true;
                };
                var actor = f.Create(); Assert.AreEqual(1, nested); Assert.AreSame(parent, actor); Assert.AreEqual(1, actor.GetPart<InventoryPart>().GetAllEquipped().Count);
            }
        }

        [TestCase("Dagger", 2, 1, 1)]
        [TestCase("Greatsword", 1, 1, 0)]
        [TestCase("Buckler", 2, 0, 2)]
        public void OccupiedWeaponSupportHandCannotAddNaturalButShieldFallbackRemains(string itemName, int total, int actualWeapons, int naturals)
        {
            // Greatsword is the new RED hypothesis. Dagger/free offhand and
            // Buckler/non-melee occupied slot pin the existing separate policies.
            using (var f = new LoadoutLifecycleFixture(itemName))
            {
                var actor = f.Create(); var item = LoadoutLifecycleFixture.OnlyItem(actor); var defaults = Hands(actor).Select(h => h._DefaultBehavior).ToArray();
                Assert.IsTrue(defaults.All(e => e != null)); LoadoutLifecycleFixture.Equipped(actor, item, itemName == "Greatsword" ? 2 : 1);
                var weapons = Weapons(actor); Assert.AreEqual(total, weapons.Count);
                Assert.AreEqual(actualWeapons, weapons.Count(w => ReferenceEquals(w.ParentEntity, item)));
                Assert.AreEqual(naturals, weapons.Count(w => defaults.Contains(w.ParentEntity)));
                Assert.IsTrue(InventorySystem.UnequipItem(actor, item));
                var after = Weapons(actor); Assert.AreEqual(2, after.Count); CollectionAssert.AreEquivalent(defaults, after.Select(w => w.ParentEntity));
            }
        }
        [Test]
        public void DismemberVetoKeepsExactNaturalAndPersonalEquipment()
        {
            using (var f = new LoadoutLifecycleFixture("Dagger"))
            {
                var actor = f.Create(); var hand = Hands(actor).Single(h => h._Equipped != null); var natural = hand._DefaultBehavior; var item = hand._Equipped;
                var zone = new Zone("natural-veto"); Assert.IsTrue(zone.AddEntity(actor, 5, 5)); int observed = 0;
                LoadoutAuditProbe.AuditHook = (probe, e) => { if (e.ID == "BeforeDismember") { observed++; return false; } return true; };
                Assert.IsFalse(actor.GetPart<Body>().Dismember(hand, zone)); Assert.AreEqual(1, observed);
                Assert.AreSame(natural, hand._DefaultBehavior); Assert.NotNull(natural); Assert.NotNull(hand.ParentPart); LoadoutLifecycleFixture.Equipped(actor, item);
                Assert.AreEqual(0, actor.GetPart<Body>().DismemberedParts.Count); Assert.IsFalse(zone.GetAllEntities().Any(e => e.HasTag("Natural") || e.BlueprintName == "SeveredLimb"));
            }
        }
        [Test]
        public void RealSeverAndRegrowKeepExactNaturalWhileOnlyPersonalWeaponDrops()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("Snapjaw"); var body = actor.GetPart<Body>(); var hand = Hands(actor).Single(h => h._Equipped?.BlueprintName == "Dagger");
                var natural = hand._DefaultBehavior; var dagger = hand._Equipped; Assert.NotNull(natural);
                var zone = new Zone("natural-sever"); Assert.IsTrue(zone.AddEntity(actor, 5, 5));
                Assert.IsTrue(body.Dismember(hand, zone)); Assert.AreSame(natural, hand._DefaultBehavior);
                Assert.AreEqual((5, 5), zone.GetEntityPosition(dagger)); Assert.IsNull(dagger.GetPart<PhysicsPart>().Equipped);
                Assert.IsFalse(zone.GetAllEntities().Any(e => e.HasTag("Natural"))); Assert.IsTrue(zone.GetAllEntities().Any(e => e.BlueprintName == "SeveredLimb"));
                Assert.IsTrue(body.RegenerateLimb("Hand")); Assert.AreSame(natural, Hands(actor).Single(h => h.ID == hand.ID)._DefaultBehavior);
                Assert.AreEqual(2, Weapons(actor).Count(w => w.ParentEntity.HasTag("Natural"))); Assert.AreEqual((5, 5), zone.GetEntityPosition(dagger));
                Assert.IsFalse(Items(actor).Contains(natural)); Assert.IsFalse(Items(actor).Contains(dagger));
            }
        }
        [Test]
        public void RepeatedBodyLoadHookFillsMissingOnceAndDoesNotRegrantOrAnnounceGear()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("SnapjawWarlord"); var body = actor.GetPart<Body>(); var hands = Hands(actor); var kept = hands[0]._DefaultBehavior;
                Assert.NotNull(kept); hands[1]._DefaultBehavior = null; hands[1].FirstSlotForDefaultBehavior = false;
                var gear = ItemSignature(actor); int penalty = actor.GetStat("Speed").Penalty; f.Messages.Clear();
                body.OnAfterLoad(null); var repaired = hands[1]._DefaultBehavior; Assert.NotNull(repaired); Assert.AreNotSame(kept, repaired);
                for (int i = 0; i < 3; i++) body.OnAfterLoad(null);
                Assert.AreSame(kept, hands[0]._DefaultBehavior); Assert.AreSame(repaired, hands[1]._DefaultBehavior); Assert.IsTrue(hands[1].FirstSlotForDefaultBehavior);
                CollectionAssert.AreEqual(gear, ItemSignature(actor)); Assert.AreEqual(penalty, actor.GetStat("Speed").Penalty); Assert.AreEqual(0, f.Messages.Count);
            }
        }
        [Test]
        public void RepeatedCompleteSavesRepairOldNaturalsWithoutAddingNewPersonalKit()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("SnapjawWarlord"); var inventory = actor.GetPart<InventoryPart>();
                foreach (var item in inventory.GetAllEquipped().ToArray()) { Assert.IsTrue(InventorySystem.UnequipItem(actor, item)); Assert.IsTrue(inventory.RemoveObject(item)); }
                foreach (var hand in Hands(actor)) { hand._DefaultBehavior = null; hand.FirstSlotForDefaultBehavior = false; }
                Assert.AreEqual(0, Items(actor).Length); f.Messages.Clear();
                var once = RoundTrip(actor); var twice = RoundTrip(once);
                foreach (var loaded in new[] { once, twice })
                { Assert.AreEqual(0, Items(loaded).Length); Assert.NotNull(loaded.GetPart<LoadoutPart>()); Assert.AreEqual(0, loaded.GetStat("Speed").Penalty); Assert.AreEqual(2, Weapons(loaded).Count); Assert.IsTrue(Hands(loaded).All(h => h._DefaultBehavior.GetPart<MeleeWeaponPart>().BaseDamage == "2d5")); }
                Assert.IsFalse(f.Messages.Any(x => x.Contains(" equips ")));
            }
        }
        [Test]
        public void CompleteSaveKeepsDistinctSameLookingNaturalObjectsAndDifferentCustomPayloads()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("SnapjawWarlord"); var hands = Hands(actor); Assert.IsTrue(hands.All(h => h._DefaultBehavior != null));
                hands[0]._DefaultBehavior.GetPart<MeleeWeaponPart>().PenBonus = 7; hands[1]._DefaultBehavior.GetPart<MeleeWeaponPart>().OnHitEffectsRaw = "Stunned,17,,4,0";
                var loaded = RoundTrip(actor); var restored = Hands(loaded);
                Assert.AreNotSame(restored[0]._DefaultBehavior, restored[1]._DefaultBehavior); Assert.AreEqual(2, restored.Count(h => h.FirstSlotForDefaultBehavior));
                Assert.AreEqual(7, restored[0]._DefaultBehavior.GetPart<MeleeWeaponPart>().PenBonus); Assert.AreEqual(2, restored[1]._DefaultBehavior.GetPart<MeleeWeaponPart>().PenBonus);
                Assert.AreEqual("", restored[0]._DefaultBehavior.GetPart<MeleeWeaponPart>().OnHitEffectsRaw); Assert.AreEqual("Stunned,17,,4,0", restored[1]._DefaultBehavior.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
                // Reference distinction/payload is the invariant: old anonymous IDs are repaired on first read.
            }
        }
        [Test]
        public void NoBodyWithNaturalPropertyKeepsSupportedCreationAndCarriedLoadoutFallback()
        {
            using (var f = new LoadoutLifecycleFixture("Dagger", body: false))
            {
                f.ActorBlueprint.Props["NaturalWeapon"] = "WarlordCleaver"; var actor = f.Create();
                Assert.IsNull(actor.GetPart<Body>()); LoadoutLifecycleFixture.Carried(actor, LoadoutLifecycleFixture.OnlyItem(actor));
                Assert.AreEqual(0, actor.GetPart<LoadoutAuditProbe>().BeforeCount); Assert.IsFalse(Items(actor).Any(e => e.HasTag("Natural")));
            }
        }
        [Test]
        public void EmptyDeclaredMissingDefaultsRemainEmptyAcrossLoadWithoutChangingFlags()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("Player"); foreach (var hand in Hands(actor)) { hand.DefaultBehaviorBlueprint = ""; hand._DefaultBehavior = null; hand.FirstSlotForDefaultBehavior = false; }
                var flags = Hands(actor).Select(h => h.Flags).ToArray(); var loaded = RoundTrip(actor);
                Assert.IsTrue(Hands(loaded).All(h => h._DefaultBehavior == null && h.DefaultBehaviorBlueprint == ""));
                CollectionAssert.AreEqual(flags, Hands(loaded).Select(h => h.Flags).ToArray()); Assert.AreEqual(0, Weapons(loaded).Count);
            }
        }
        [Test]
        public void ExistingCustomDefaultKeepsEverySavedFlagBitInsteadOfBlanketRecalculation()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("SnapjawWarlord"); var hand = Hands(actor)[0]; Assert.NotNull(hand._DefaultBehavior);
                hand.DefaultBehaviorBlueprint = ""; hand.Flags = BodyPart.FLAG_CONTACT; int exactFlags = hand.Flags;
                var loaded = RoundTrip(actor); var restored = Hands(loaded).Single(h => h.ID == hand.ID);
                Assert.NotNull(restored._DefaultBehavior); Assert.AreEqual(exactFlags, restored.Flags); Assert.IsFalse(restored.FirstSlotForDefaultBehavior);
            }
        }
        [Test]
        public void UnsupportedMissingRecipeUsesExistingFactoryFallbackWithoutCreatingPersonalGear()
        {
            using (var f = new LoadoutLifecycleFixture())
            {
                f.ActorBlueprint.Props["NaturalWeapon"] = "UnsupportedNaturalAuditRecipe"; var actor = f.Create();
                foreach (var hand in Hands(actor))
                { Assert.NotNull(hand._DefaultBehavior); Assert.AreEqual("UnsupportedNaturalAuditRecipe", hand.DefaultBehaviorBlueprint); Assert.AreEqual("1d2", hand._DefaultBehavior.GetPart<MeleeWeaponPart>().BaseDamage); Assert.AreEqual("", hand._DefaultBehavior.GetPart<MeleeWeaponPart>().Attributes); }
                Assert.AreEqual(0, Items(actor).Length); Assert.AreEqual(2, Weapons(actor).Count);
            }
        }
    }
}
