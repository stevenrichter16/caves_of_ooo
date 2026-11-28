using System.Reflection;
using System.Text.Json.Nodes;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Web;

internal static partial class Program
{
    private static void RunActionControlContracts()
    {
        Run("ability details expose native cooldown and remain a free read", AbilityControlDetails);
        Run("ability slots include0and9 and replacement unbinds without swapping", AbilityControlBindings);
        Run("ability management rejects unknown ownership stale views and invalid slots freely", AbilityControlInvalid);
        Run("a deliberately empty ability hotbar survives complete save and load", AbilityControlEmptySave);
        Run("equipment choice explicitly equips the offhand and preserves native fallback", EquipmentControlOffhand);
        Run("two-handed equipment review identifies both claims and displaced items", EquipmentControlTwoHanded);
        Run("equipment confirmation rejects invalid limbs and changed anatomy ownership or displacement", EquipmentControlInvalidated);
        Run("equipment review tokens are one-use cancelled and revision checked", EquipmentControlTokens);
    }

    private static JsonObject NewControlsGame() => Parse(Game.NewGame("classic", 8128, "Action controls contract"));
    private static ActivatedAbilitiesPart ControlAbilities() => Player().GetPart<ActivatedAbilitiesPart>();
    private static JsonObject AbilityManage(string command, string id, int index = 0, int? revision = null)
        => Send("abilityManage", new() { ["command"] = command, ["id"] = id, ["index"] = index }, revision);
    private static JsonObject EquipmentManage(string command, string? id = null, int index = 0, string? token = null, int? revision = null)
        => Send("equipment", new() { ["command"] = command, ["id"] = id, ["index"] = index, ["token"] = token }, revision);
    private static JsonObject ControlObject(JsonObject state, string key) => state[key] as JsonObject ?? throw new Exception("Missing control view: " + key);
    private static JsonArray ControlArray(JsonObject state, string key) => state[key] as JsonArray ?? throw new Exception("Missing control array: " + key);
    private static string BindingIdentity() => string.Join("|", ControlAbilities().SlotAssignments.Select(id => id.ToString()));
    private static string EquipmentIdentity() => string.Join("|", Player().GetPart<Body>().GetParts().Select(p => p.ID + ":" + p._Equipped?.ID).OrderBy(x => x, StringComparer.Ordinal));
    private static BodyPart ControlHand(bool primary)
    {
        var hands = Player().GetPart<Body>().GetPartsByType("Hand");
        // Canonical primary anatomy uses DefaultPrimary, matching native combat.
        var main = hands.FirstOrDefault(p => p.Primary || p.DefaultPrimary) ?? hands.First();
        return primary ? main : hands.Single(p => !ReferenceEquals(p, main));
    }
    private static void AbilityRowsAgree(JsonObject state)
    {
        var part = ControlAbilities();
        foreach (var row in ControlArray(state, "abilities").OfType<JsonObject>())
        {
            var id = Guid.Parse(Text(row, "id"));
            Check(part.GetAbility(id) != null && Number(row, "slot") == part.GetSlotForAbility(id), "An ability row disagrees with its native hotbar binding.");
        }
    }
    private static void RejectedControl(JsonObject before, JsonObject after, string action, string inventory, string equipment)
    {
        SamePlayerState(before, after, action);
        Check(!Boolean(after, "faulted") && !string.IsNullOrWhiteSpace(Text(after, "error")), action + " must reject without faulting.");
        Check(InventoryIdentity() == inventory && EquipmentIdentity() == equipment, action + " mutated carried or equipped identities.");
    }

    private static void AbilityControlDetails()
    {
        NewControlsGame(); var part = ControlAbilities(); var ability = part.AbilityList.First(a => a.DisplayName == "Jet Blast");
        var previous = ability.CooldownRemaining; ability.CooldownRemaining = 7;
        try
        {
            var before = State(); var bindings = BindingIdentity();
            var after = AbilityManage("details", ability.ID.ToString()); RequireSuccess(after, "Reading a cooling ability"); SamePlayerState(before, after, "Ability details");
            var details = ControlObject(after, "abilityDetails");
            Check(Text(after, "panel") == "abilityDetails" && Text(details, "id") == ability.ID.ToString()
                && Text(details, "name") == ability.DisplayName, "Ability details opened the wrong owned power.");
            Check(!Boolean(details, "canUse") && Text(details, "details").Contains("7 remaining on cooldown", StringComparison.Ordinal)
                && Text(details, "details").Contains("Range: 2", StringComparison.Ordinal), "The details lost current cooldown or native range.");
            Check(Number(details, "slot") == part.GetSlotForAbility(ability.ID) && ability.CooldownRemaining == 7
                && BindingIdentity() == bindings, "Reading details changed cooldown or bindings.");
            SamePlayerState(before, Send("close"), "Closing ability details");
            ability.CooldownRemaining = 0;
            var ready = AbilityManage("details", ability.ID.ToString()); RequireSuccess(ready, "Reading a ready ability");
            Check(Boolean(ControlObject(ready, "abilityDetails"), "canUse"), "An off-cooldown owned ability should be usable.");
        }
        finally { ability.CooldownRemaining = previous; }
    }

    private static void AbilityControlBindings()
    {
        NewControlsGame(); var part = ControlAbilities(); var a = part.AbilityList[0]; var b = part.AbilityList[1]; var before = State();
        foreach (var assignment in new[] { (ability: a, slot: 0), (ability: b, slot: 9), (ability: a, slot: 9) })
        {
            var after = AbilityManage("assign", assignment.ability.ID.ToString(), assignment.slot);
            RequireSuccess(after, "Assigning a boundary shortcut"); SamePlayerState(before, after, "Changing a shortcut"); AbilityRowsAgree(after);
            Check(part.GetAbilityBySlot(assignment.slot) == assignment.ability, "The native shortcut did not receive its selected ability.");
        }
        Check(part.GetAbilityBySlot(0) == null && part.GetSlotForAbility(b.ID) == -1 && part.GetSlotForAbility(a.ID) == 9,
            "Replacing an occupied slot must unbind its previous power without swapping or duplicating assignments.");
        var bindings = BindingIdentity(); var checkpoint = Game.Save(); var restored = Parse(Game.Load(checkpoint));
        SamePlayerState(before, restored, "Restoring nondefault shortcuts"); AbilityRowsAgree(restored);
        Check(BindingIdentity() == bindings, "Nondefault shortcut bindings did not survive save/load.");
        var unassigned = AbilityManage("unassign", a.ID.ToString()); RequireSuccess(unassigned, "Unassigning a shortcut");
        SamePlayerState(before, unassigned, "Unassigning a shortcut"); AbilityRowsAgree(unassigned);
        Check(ControlAbilities().GetSlotForAbility(a.ID) == -1 && ControlAbilities().GetAbility(a.ID) != null, "Unassigning a shortcut removed its owned ability.");
    }

    private static void AbilityControlInvalid()
    {
        NewControlsGame(); var ability = ControlAbilities().AbilityList[0]; var unknown = Guid.NewGuid().ToString();
        var before = State(); var bindings = BindingIdentity(); var inventory = InventoryIdentity(); var equipment = EquipmentIdentity();
        foreach (var command in new[] { "details", "assign", "unassign" })
        {
            var after = AbilityManage(command, unknown); RejectedControl(before, after, "Unknown ability " + command, inventory, equipment);
            Check(BindingIdentity() == bindings, "Unknown ability management changed bindings.");
        }
        foreach (var slot in new[] { -1, 10, int.MaxValue })
        {
            var after = AbilityManage("assign", ability.ID.ToString(), slot); RejectedControl(before, after, "Out-of-range shortcut", inventory, equipment);
            Check(BindingIdentity() == bindings, "An invalid slot changed bindings.");
        }
        var current = State(); var stale = AbilityManage("assign", ability.ID.ToString(), 9, Number(current, "revision") - 1);
        RejectedControl(current, stale, "Stale shortcut assignment", inventory, equipment);
        Check(BindingIdentity() == bindings, "Stale management changed bindings.");
    }

    private static void AbilityControlEmptySave()
    {
        // Characterize the native loader independently of the new UI protocol:
        // an explicitly present ten-slot empty array is a modern intentional state.
        NewControlsGame(); Array.Clear(ControlAbilities().SlotAssignments);
        var directCheckpoint = Game.Save(); Game.Load(directCheckpoint);
        Check(ControlAbilities().SlotAssignments.All(id => id == Guid.Empty), "The native loader rebinds an explicitly present empty ten-slot hotbar.");
        var before = NewControlsGame(); var ids = ControlAbilities().AbilityList.Select(a => a.ID).ToArray();
        foreach (var id in ids) RequireSuccess(AbilityManage("unassign", id.ToString()), "Clearing a shortcut");
        Check(ControlAbilities().SlotAssignments.All(id => id == Guid.Empty), "The unassign protocol did not empty the hotbar.");
        SamePlayerState(before, State(), "Clearing the hotbar"); var checkpoint = Game.Save(); Send("close"); Send("wait");
        var restored = Parse(Game.Load(checkpoint)); SamePlayerState(before, restored, "Restoring an intentionally empty hotbar"); AbilityRowsAgree(restored);
        Check(ControlAbilities().SlotAssignments.All(id => id == Guid.Empty), "Loading mistook intentionally empty modern bindings for a legacy save.");
        Check(ControlAbilities().AbilityList.Select(a => a.ID).SequenceEqual(ids), "An empty hotbar lost owned abilities or their identities.");
        Check(ControlArray(restored, "abilities").OfType<JsonObject>().All(row => Number(row, "slot") == -1), "The restored view silently rebound an empty hotbar.");
    }

    private static JsonObject ChooseEquipment(Entity item)
    {
        var before = State(); var inventory = InventoryIdentity(); var equipment = EquipmentIdentity();
        var after = EquipmentManage("choose", item.ID); RequireSuccess(after, "Choosing an equipment target"); SamePlayerState(before, after, "Equipment slot preview");
        Check(Text(after, "panel") == "equipment" && Text(ControlObject(after, "equipment"), "itemId") == item.ID, "The equipment picker opened the wrong item.");
        Check(InventoryIdentity() == inventory && EquipmentIdentity() == equipment, "The equipment picker performed its preview."); return after;
    }
    private static JsonObject ReviewEquipment(Entity item, BodyPart hand)
    {
        var before = State(); var inventory = InventoryIdentity(); var equipment = EquipmentIdentity();
        var after = EquipmentManage("review", item.ID, hand.ID); RequireSuccess(after, "Reviewing a concrete equipment plan"); SamePlayerState(before, after, "Equipment review");
        var review = ControlObject(after, "equipReview");
        Check(Text(review, "itemId") == item.ID && Text(review, "name") == item.GetDisplayName()
            && !string.IsNullOrWhiteSpace(Text(review, "token")), "Equipment review lacks its item identity or one-use token.");
        Check(InventoryIdentity() == inventory && EquipmentIdentity() == equipment, "Equipment review mutated its proposed plan."); return review;
    }
    private static void EquipmentControlOffhand()
    {
        NewControlsGame(); using var items = new ControlItemFixture(); var dagger = items.Add("Dagger");
        var primary = ControlHand(true); var offhand = ControlHand(false); var original = primary._Equipped;
        var natural = offhand._DefaultBehavior; Check(natural != null, "The canonical offhand needs its actual natural weapon fallback.");
        var before = State(); var chooser = ControlObject(ChooseEquipment(dagger), "equipment");
        var slot = ControlArray(chooser, "slots").OfType<JsonObject>().Single(row => Number(row, "id") == offhand.ID);
        Check(Boolean(slot, "valid") && !Boolean(slot, "primary") && Text(slot, "name") == offhand.GetDisplayName(), "The actual offhand is not an explicit usable choice.");
        Check(chooser["comparison"] != null, "Equipment preview must include its native comparison.");
        var review = ReviewEquipment(dagger, offhand);
        Check(ControlArray(review, "claimed").Select(n => n!.GetValue<string>()).SequenceEqual(new[] { offhand.GetDisplayName() }), "Offhand review claims the wrong anatomy.");
        var equipped = EquipmentManage("confirm", token: Text(review, "token")); RequireSuccess(equipped, "Confirming the explicit offhand"); SamePlayerState(before, equipped, "Equipping the offhand");
        Check(ReferenceEquals(offhand._Equipped, dagger) && ReferenceEquals(primary._Equipped, original), "Explicit offhand equip displaced the main hand instead.");
        Check(InventorySystem.UnequipItem(Player(), dagger), "The native fallback fixture could not unequip.");
        Check(offhand._Equipped == null && ReferenceEquals(offhand._DefaultBehavior, natural), "Unequipping lost the hand's native natural-weapon fallback.");
        var fallback = Send("item", new() { ["id"] = dagger.ID, ["command"] = "Equip" }); RequireSuccess(fallback, "Opening equipment through the existing item action");
        SamePlayerState(before, fallback, "The ordinary Equip action");
        Check(Text(fallback, "panel") == "equipment" && !InventorySystem.IsEquipped(Player(), dagger), "The ordinary Equip action must open the chooser before committing.");
    }

    private static void EquipmentControlTwoHanded()
    {
        NewControlsGame(); using var items = new ControlItemFixture(); var spare = items.Add("Dagger"); var greatsword = items.Add("Greatsword");
        var primary = ControlHand(true); var offhand = ControlHand(false); var original = primary._Equipped;
        Check(original != null && InventorySystem.Equip(Player(), spare, offhand), "The two-hand fixture needs two actual equipped weapons.");
        var before = State(); var chooser = ControlObject(ChooseEquipment(greatsword), "equipment");
        var slot = ControlArray(chooser, "slots").OfType<JsonObject>().Single(row => Number(row, "id") == primary.ID);
        Check(Boolean(slot, "valid") && Boolean(slot, "primary"), "The native primary hand must remain a valid two-handed starting slot.");
        var review = ReviewEquipment(greatsword, primary);
        var claimed = ControlArray(review, "claimed").Select(n => n!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
        Check(claimed.SetEquals(new[] { primary.GetDisplayName(), offhand.GetDisplayName() }), "Two-handed review did not name both claimed hands.");
        var displaced = ControlArray(review, "displaced").OfType<JsonObject>().ToArray();
        Check(displaced.Select(d => Text(d, "id")).ToHashSet().SetEquals(new[] { original!.ID, spare.ID })
            && displaced.All(d => !string.IsNullOrWhiteSpace(Text(d, "name")) && !string.IsNullOrWhiteSpace(Text(d, "slot"))), "The review lost displaced item identities or their concrete slots.");
        var equipped = EquipmentManage("confirm", token: Text(review, "token")); RequireSuccess(equipped, "Confirming the two-handed replacement"); SamePlayerState(before, equipped, "Two-handed equipment");
        Check(ReferenceEquals(primary._Equipped, greatsword) && ReferenceEquals(offhand._Equipped, greatsword), "Native confirmation did not claim both hands with the same item.");
        var pack = Player().GetPart<InventoryPart>(); Check(pack.Objects.Contains(original) && pack.Objects.Contains(spare), "Replaced gear was lost instead of returned to the pack.");
    }

    private static void EquipmentControlInvalidated()
    {
        foreach (var change in new[] { "anatomy", "displacement", "ownership" })
        {
            NewControlsGame(); using var items = new ControlItemFixture(); var candidate = items.Add(change == "displacement" ? "Greatsword" : "Dagger");
            var offhand = ControlHand(false); var target = change == "displacement" ? ControlHand(true) : offhand;
            var beforeInvalid = State(); var inventory = InventoryIdentity(); var equipment = EquipmentIdentity();
            RejectedControl(beforeInvalid, EquipmentManage("choose", "no-such-carried-item"), "Choosing an unowned item", inventory, equipment);
            RejectedControl(beforeInvalid, EquipmentManage("review", candidate.ID, -1), "Choosing an invalid limb identity", inventory, equipment);
            var head = Player().GetPart<Body>().GetPartByType("Head");
            RejectedControl(beforeInvalid, EquipmentManage("review", candidate.ID, head.ID), "Equipping a weapon on an incompatible limb", inventory, equipment);
            ChooseEquipment(candidate); var review = ReviewEquipment(candidate, target); var parent = target.ParentPart;
            try
            {
                // Owned live-state changes between preview and commit. A fresh
                // protocol revision below prevents stale-revision rejection from
                // hiding missing identity/anatomy revalidation.
                if (change == "anatomy") Check(parent.RemovePart(target), "The detached-limb fixture could not detach its offhand.");
                else if (change == "displacement") Check(InventorySystem.Equip(Player(), items.Add("Dagger"), target), "The displacement fixture could not replace the reviewed occupant.");
                else Check(InventorySystem.Drop(Player(), candidate, CurrentZone(Player())), "The ownership fixture could not drop the reviewed item.");
                var changed = State(); var changedInventory = InventoryIdentity(); var changedEquipment = EquipmentIdentity();
                var rejected = EquipmentManage("confirm", token: Text(review, "token"));
                RejectedControl(changed, rejected, "Confirming changed " + change, changedInventory, changedEquipment);
                Check(!InventorySystem.IsEquipped(Player(), candidate), "An invalidated plan equipped its candidate.");
            }
            finally { if (change == "anatomy" && !Player().GetPart<Body>().GetParts().Contains(target)) parent.AddPart(target); }
        }
    }

    private static void EquipmentControlTokens()
    {
        NewControlsGame(); using var items = new ControlItemFixture(); var dagger = items.Add("Dagger"); var offhand = ControlHand(false);
        ChooseEquipment(dagger); var review = ReviewEquipment(dagger, offhand); var token = Text(review, "token");
        var before = State(); var inventory = InventoryIdentity(); var equipment = EquipmentIdentity();
        SamePlayerState(before, Send("close"), "Cancelling equipment review");
        RejectedControl(before, EquipmentManage("confirm", token: token), "Replaying a cancelled review", inventory, equipment);
        ChooseEquipment(dagger); review = ReviewEquipment(dagger, offhand); token = Text(review, "token"); var current = State();
        RejectedControl(current, EquipmentManage("confirm", token: token, revision: Number(current, "revision") - 1), "Confirming from an older view", inventory, equipment);
        var confirmed = EquipmentManage("confirm", token: token); RequireSuccess(confirmed, "Confirming the still-current review"); SamePlayerState(before, confirmed, "Equipment confirmation");
        Check(ReferenceEquals(offhand._Equipped, dagger), "The current review did not equip the intended item.");
        RejectedControl(confirmed, EquipmentManage("confirm", token: token), "Replaying a consumed review", InventoryIdentity(), EquipmentIdentity());
    }

    // All supplied gear is created from shipped blueprints, owned only by this
    // isolated test expedition, and removed in Dispose; no content is modified.
    private sealed class ControlItemFixture : IDisposable
    {
        private readonly Entity actor = Player();
        private readonly List<Entity> items = new();
        public Entity Add(string blueprint)
        {
            var factory = typeof(BrowserGame).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(field => field.FieldType == typeof(EntityFactory)).GetValue(Game) as EntityFactory;
            var item = factory!.CreateEntity(blueprint);
            Check(item != null && actor.GetPart<InventoryPart>().AddObject(item), "Cannot grant owned equipment fixture " + blueprint + ".");
            items.Add(item!); return item!;
        }
        public void Dispose()
        {
            var pack = actor.GetPart<InventoryPart>(); var zone = CurrentZone(actor);
            foreach (var item in items)
            {
                if (InventorySystem.IsEquipped(actor, item)) InventorySystem.UnequipItem(actor, item);
                pack.RemoveObject(item); zone.RemoveEntity(item);
            }
        }
    }
}
