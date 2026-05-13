using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// E.5.3 — IItemEnhancement.GetEffectDescription + ExaminablePart
    /// integration pin. Tests both:
    /// (1) Each concrete enhancement returns a tier-aware effect string.
    /// (2) ExaminablePart appends one bullet line per enhancement to
    ///     the "You see a foo." message.
    /// </summary>
    public class EnhancementDescriptionTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ── Per-class effect description text ─────────────────────

        [Test]
        public void Serrated_EffectDescription_MentionsChanceAndBleeding()
        {
            var s = new EnhancementSerrated();
            s.ApplyTier(3); // 30% chance
            string desc = s.GetEffectDescription();
            StringAssert.Contains("30%", desc);
            StringAssert.Contains("Bleeding", desc);
        }

        [Test]
        public void Lacquered_EffectDescription_MentionsAvBonus()
        {
            var l = new EnhancementLacquered();
            l.ApplyTier(2); // +2 AV
            StringAssert.Contains("+2", l.GetEffectDescription());
            StringAssert.Contains("armor", l.GetEffectDescription());
        }

        [Test]
        public void Engraved_EffectDescription_MentionsRepAndFaction()
        {
            var e = new EnhancementEngraved { Faction = "Villagers" };
            e.ApplyTier(2); // +10 rep
            string desc = e.GetEffectDescription();
            StringAssert.Contains("+10", desc);
            StringAssert.Contains("Villagers", desc);
        }

        [Test]
        public void Engraved_NoFaction_EffectDescriptionSurfacesNoOp()
        {
            var e = new EnhancementEngraved(); // Faction = ""
            e.ApplyTier(3);
            // Should explicitly tell the player no effect — not silently
            // show "+15 with " (broken sentence).
            StringAssert.Contains("no faction set", e.GetEffectDescription());
        }

        [Test]
        public void PaleSalt_EffectDescription_MentionsUndeadAndBonus()
        {
            var p = new EnhancementPaleSalt();
            p.ApplyTier(3); // +6 bonus
            string desc = p.GetEffectDescription();
            StringAssert.Contains("+6", desc);
            StringAssert.Contains("Undead", desc);
        }

        [Test]
        public void ChoirIron_EffectDescription_MentionsFungalAndBonus()
        {
            var c = new EnhancementChoirIron();
            c.ApplyTier(2); // +4 bonus
            string desc = c.GetEffectDescription();
            StringAssert.Contains("+4", desc);
            StringAssert.Contains("Fungal", desc);
        }

        [Test]
        public void GlowQuartz_EffectDescription_MentionsLightRadius()
        {
            var g = new EnhancementGlowQuartz();
            g.ApplyTier(4); // +4 radius
            string desc = g.GetEffectDescription();
            StringAssert.Contains("+4", desc);
            StringAssert.Contains("light", desc);
        }

        // ── ExaminablePart integration ────────────────────────────

        private static Entity MakeExaminableWeapon()
        {
            var e = new Entity { ID = "longsword", BlueprintName = "longsword" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "longsword" });
            e.AddPart(new MeleeWeaponPart
                { BaseDamage = "1d6", Attributes = "Melee Cutting" });
            e.AddPart(new ExaminablePart());
            return e;
        }

        private static string GetLastMessage()
        {
            var recent = MessageLog.GetRecent(1);
            return recent.Count > 0 ? recent[recent.Count - 1] : "";
        }

        private static void FireExamine(Entity item)
        {
            var ev = GameEvent.New("InventoryAction");
            ev.SetParameter("Command", "Examine");
            item.FireEventAndRelease(ev);
        }

        [Test]
        public void Examine_UnEnhancedWeapon_ShowsBaseLineOnly()
        {
            var weapon = MakeExaminableWeapon();
            FireExamine(weapon);
            string msg = GetLastMessage();
            StringAssert.Contains("You see", msg);
            StringAssert.Contains("longsword", msg);
            StringAssert.DoesNotContain("•", msg,
                "No enhancements → no bullet lines.");
        }

        [Test]
        public void Examine_SerratedWeapon_AppendsEffectBullet()
        {
            var weapon = MakeExaminableWeapon();
            // Manually attach Serrated (skips factory for test isolation).
            var serrated = new EnhancementSerrated();
            serrated.ApplyTier(2);
            weapon.AddPart(serrated);

            FireExamine(weapon);
            string msg = GetLastMessage();
            StringAssert.Contains("•", msg, "Bullet appended.");
            StringAssert.Contains("Serrated", msg);
            StringAssert.Contains("20%", msg, "Tier-2 chance shown.");
        }

        [Test]
        public void Examine_TwoEnhancements_AppendsBothBullets()
        {
            // Cross-system: Serrated + PaleSalt on same weapon →
            // both effect lines appear.
            var weapon = MakeExaminableWeapon();
            var serrated = new EnhancementSerrated();
            serrated.ApplyTier(1);
            var pale = new EnhancementPaleSalt();
            pale.ApplyTier(2);
            weapon.AddPart(serrated);
            weapon.AddPart(pale);

            FireExamine(weapon);
            string msg = GetLastMessage();
            int bulletCount = 0;
            for (int i = 0; i < msg.Length - 1; i++)
                if (msg[i] == '•') bulletCount++;
            Assert.AreEqual(2, bulletCount,
                "Two enhancements → two bullet lines.");
            StringAssert.Contains("Serrated", msg);
            StringAssert.Contains("Pale-Salt", msg);
            StringAssert.Contains("Undead", msg);
        }

        // ── TinkerRecipe.Description JSON load + display ──────────

        [Test]
        public void TinkerRecipe_PaleSaltInfuse_HasDescription()
        {
            // Pin: the Description field loads from Recipes_V1.json.
            // (Test infrastructure auto-loads from Resources.)
            TinkerRecipeRegistry.ResetForTests();
            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe(
                "mod_palesalt_infuse", out var recipe));
            Assert.IsFalse(string.IsNullOrWhiteSpace(recipe.Description),
                "mod_palesalt_infuse: Description field populated.");
            StringAssert.Contains("Undead", recipe.Description);
        }

        [Test]
        public void TinkerRecipe_GlowQuartz_DescriptionMentionsLight()
        {
            TinkerRecipeRegistry.ResetForTests();
            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe(
                "mod_glowquartz_infuse", out var recipe));
            StringAssert.Contains("light", recipe.Description.ToLowerInvariant());
        }

        [Test]
        public void TinkerRecipe_NonMineralRecipe_DescriptionMayBeEmpty()
        {
            // Counter-check: Sharp (non-mineral) recipe doesn't (yet) have
            // a Description set. Pinned so adding one later is a deliberate
            // act. Empty / null are both acceptable.
            TinkerRecipeRegistry.ResetForTests();
            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe(
                "mod_sharp_melee", out var recipe));
            Assert.IsTrue(string.IsNullOrWhiteSpace(recipe.Description),
                "Sharp recipe Description is currently empty by design.");
        }
    }
}
