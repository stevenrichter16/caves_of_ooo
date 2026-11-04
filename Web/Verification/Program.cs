using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
using CavesOfOoo.Web;

// Console-level integration contracts for the real shared engine and browser
// facade. These run without Unity, DOM, WebAssembly, or synthetic game rules.
// Temporary fixtures are explicitly owned, cleaned up, and never shipped content.
internal static class Program
{
    private static readonly BrowserGame Game = new();
    private static readonly List<object> Results = new();
    private static int Failures;

    private static int Main(string[] args)
    {
        UnityEngine.Debug.Quiet = true;
        Run("canonical start uses real content, build and scheduler", CanonicalStart);
        Run("look is free and does not change the player's state", LookIsFree);
        Run("a stale input revision cannot spend a turn or move", StaleInputIsRejected);
        Run("oversized movement cannot bypass the one-step contract", OversizedStepIsRejected);
        Run("a valid step moves through the real turn pipeline", ValidStepMoves);
        Run("an indestructible adjacent wall blocks without spending a turn", BlockedWallIsFree);
        Run("wait advances the actual game scheduler", WaitAdvances);
        Run("item commands reject remote and nonexistent ownership", ItemOwnership);
        Run("a valid owned item command still executes", OwnedItemAction);
        Run("save and load restore exact position, tick, HP and inventory", SaveRoundTrip);
        Run("malformed saves leave the active session intact", MalformedLoadIsAtomic);
        Run("damage changes current HP without shrinking maximum HP", DamagePreservesMaximum);
        Run("a valid checkpoint recovers an interrupted, faulted turn", FaultedTurnRecovers);
        Run("the real opening stairs enter the cellar and return", OpeningStairsRoundTrip);
        foreach (var build in new[] { "duelist", "breaker", "stormcaller", "bombardier", "warden", "hurler", "classic" })
            Run("starting build " + build + " applies its actual gear and skills", () => StartingBuildContract(build));
        Run("a grouped pickup transfers selected items with one paid turn", GroupedPickup);
        Run("ordinary throws preview freely, validate, then pay once", OrdinaryThrow);
        Run("a changed throw target cannot consume an item or turn", ChangedThrowTarget);
        Run("Measured Throw preserves its distinct preparation and launch turns", MeasuredThrow);
        Run("cellar supplies repair the actual opening well and persist", OpeningRepair);
        Run("crafting previews and brews use native ownership and free menu timing", NativeBrew);
        if (Failures == 0 && args.Contains("--opening-snapshot", StringComparer.Ordinal))
            File.WriteAllText("/tmp/coo-opening-view.json", Game.NewGame("classic", 8128, "Wanderer"));
        Console.WriteLine(JsonSerializer.Serialize(new { total = Results.Count, passed = Results.Count - Failures, failed = Failures, cases = Results }));
        return Failures == 0 ? 0 : 1;
    }

    private static void CanonicalStart()
    {
        var state = Parse(Game.NewGame("classic", 713753, "Web contract"));
        Check(Number(state, "hp") > 0, "The new player must be alive.");
        Check(Number(state, "maxHp") >= Number(state, "hp"), "HP must have a valid maximum.");
        Check(!Boolean(state, "dead"), "The new expedition cannot be dead.");
        Check(Number(state, "x") >= 0 && Number(state, "x") < Zone.Width, "Player X is outside the canonical zone.");
        Check(Number(state, "y") >= 0 && Number(state, "y") < Zone.Height, "Player Y is outside the canonical zone.");
        Check(!string.IsNullOrWhiteSpace(Text(state, "zone")), "The new expedition must expose its actual zone.");
        Check(state["cells"] is JsonArray cells && cells.Count > 0, "The renderer needs actual generated cell data.");
        var player = Player();
        Check(player.GetProperty(StartingBuildService.PropertyName) == "classic", "The canonical Classic build was not applied.");
        Check(StartingBuildService.PrimaryHandWeapon(player)?.BlueprintName == "Dagger", "Classic must equip the real starting dagger.");
        Check(player.GetPart<InventoryPart>()?.Objects.Count > 0, "Classic must have its real inventory.");
        Check(TurnManager.Active.WaitingForInput, "The scheduler must yield to the player.");
        Check(ReferenceEquals(TurnManager.Active.CurrentActor, player), "The scheduler yielded to the wrong actor.");
    }

    private static void LookIsFree()
    {
        var before = State();
        var after = Send("look", new() { ["x"] = Number(before, "x"), ["y"] = Number(before, "y") });
        SamePlayerState(before, after, "Looking");
        SamePlayerState(after, Send("close"), "Closing the reader");
    }

    private static void StaleInputIsRejected()
    {
        var before = State();
        var after = Send("step", new() { ["x"] = 1, ["y"] = 0 }, Number(before, "revision") - 1);
        SamePlayerState(before, after, "Stale input");
        Check(!string.IsNullOrWhiteSpace(Text(after, "error")), "Stale input should explain its rejection.");
    }

    private static void OversizedStepIsRejected()
    {
        var before = State();
        var after = Send("step", new() { ["x"] = 2, ["y"] = 0 });
        SamePlayerState(before, after, "Oversized movement");
        Check(!string.IsNullOrWhiteSpace(Text(after, "error")), "Invalid movement should explain its rejection.");
    }

    private static void BlockedWallIsFree()
    {
        var player = Player(); var zone = CurrentZone(player); var origin = zone.GetEntityCell(player);
        var directions = new[] { (x: 1, y: 0), (x: -1, y: 0), (x: 0, y: 1), (x: 0, y: -1) };
        var direction = directions.First(d => zone.GetCell(origin.X + d.x, origin.Y + d.y) is Cell cell
            && !cell.BlocksMovement(player) && !cell.Occupants.Any(e => e.HasTag("Creature")));
        var wall = new Entity { ID = "web-contract-wall", BlueprintName = "ContractWall" };
        wall.SetTag("Solid"); wall.SetTag("Wall"); wall.SetTag("Indestructible");
        wall.AddPart(new PhysicsPart { Solid = true });
        wall.AddPart(new RenderPart { DisplayName = "contract wall", RenderString = "#", RenderLayer = 99 });
        Check(zone.AddEntity(wall, origin.X + direction.x, origin.Y + direction.y), "The temporary wall fixture could not be placed.");
        try
        {
            var before = State();
            var after = Send("step", new() { ["x"] = direction.x, ["y"] = direction.y });
            SamePlayerState(before, after, "A blocked wall");
            Check(zone.GetEntityCell(wall) != null, "An indestructible wall must survive the bump.");
        }
        finally { zone.RemoveEntity(wall); }
    }

    private static void ValidStepMoves()
    {
        var player = Player(); var zone = CurrentZone(player); var origin = zone.GetEntityCell(player);
        var directions = new[] { (x: 1, y: 0), (x: -1, y: 0), (x: 0, y: 1), (x: 0, y: -1) };
        var direction = directions.First(d => zone.GetCell(origin.X + d.x, origin.Y + d.y) is Cell cell
            && !cell.BlocksMovement(player) && !cell.Occupants.Any(e => e.HasTag("Creature")));
        var before = State(); var after = Send("step", new() { ["x"] = direction.x, ["y"] = direction.y });
        Check(Number(after, "x") != Number(before, "x") || Number(after, "y") != Number(before, "y"), "A valid free step must move the player.");
        Check(Number(after, "tick") > Number(before, "tick"), "A valid step must advance actual scheduler ticks.");
    }

    private static void WaitAdvances()
    {
        var before = State(); var after = Send("wait");
        Check(Number(after, "tick") > Number(before, "tick"), "Waiting must advance actual scheduler ticks.");
        Check(Number(after, "x") == Number(before, "x") && Number(after, "y") == Number(before, "y"), "Waiting must not move the player.");
        Check(TurnManager.Active.WaitingForInput, "A completed wait must yield input again.");
    }

    private static void ItemOwnership()
    {
        var player = Player(); var zone = CurrentZone(player); var origin = zone.GetEntityCell(player);
        var remoteCell = Enumerable.Range(0, Zone.Width).SelectMany(x => Enumerable.Range(0, Zone.Height).Select(y => zone.GetCell(x, y)))
            .First(cell => Math.Max(Math.Abs(cell.X - origin.X), Math.Abs(cell.Y - origin.Y)) >= 8 && !cell.BlocksMovement());
        var remote = new Entity { ID = "web-contract-remote", BlueprintName = "ContractRemoteItem" };
        remote.AddPart(new PhysicsPart { Takeable = true });
        remote.AddPart(new RenderPart { DisplayName = "remote contract item", RenderString = ")", RenderLayer = 5 });
        Check(zone.AddEntity(remote, remoteCell.X, remoteCell.Y), "The remote item fixture could not be placed.");
        try
        {
            foreach (string id in new[] { remote.ID, "no-such-item-web-contract" })
            {
                var before = State(); var after = Send("item", new() { ["id"] = id, ["command"] = "Drop" });
                SamePlayerState(before, after, "An unowned item command");
                Check(!string.IsNullOrWhiteSpace(Text(after, "error")), "An unowned item should be rejected explicitly.");
                Check(!player.GetPart<InventoryPart>().Objects.Contains(remote), "The remote item was stolen into the pack.");
                var location = zone.GetEntityCell(remote);
                Check(location?.X == remoteCell.X && location.Y == remoteCell.Y, "The unowned command moved a remote world item.");
            }
        }
        finally { zone.RemoveEntity(remote); }
    }

    private static void OwnedItemAction()
    {
        var player = Player(); var inventory = player.GetPart<InventoryPart>();
        var item = inventory.Objects.First(i => i.GetPart<PhysicsPart>()?.Takeable == true);
        var before = State(); var after = Send("item", new() { ["id"] = item.ID, ["command"] = "Drop" });
        Check(string.IsNullOrEmpty(Text(after, "error")), "A current owned item must accept Drop.");
        Check(!inventory.Objects.Contains(item), "The owned item remained carried after Drop.");
        var zone = CurrentZone(player); var playerCell = zone.GetEntityCell(player); var itemCell = zone.GetEntityCell(item);
        Check(itemCell?.X == playerCell.X && itemCell.Y == playerCell.Y, "Drop must place the item at the player's feet.");
        Check(Number(after, "tick") == Number(before, "tick"), "Drop should remain free, matching the native inventory policy.");
    }

    private static void SaveRoundTrip()
    {
        Send("close");
        var before = State(); var inventoryBefore = InventoryIdentity(); var saved = Game.Save();
        Check(saved.Length > 100, "The save should contain the real session graph.");
        Send("wait");
        Check(Number(State(), "tick") > Number(before, "tick"), "The roundtrip control needs a changed live session.");
        var after = Parse(Game.Load(saved));
        SamePlayerState(before, after, "Loading the checkpoint");
        Check(InventoryIdentity() == inventoryBefore, "Inventory identity changed across save/load.");
        Check(TurnManager.Active.WaitingForInput, "The loaded scheduler must remain ready for input.");
    }

    private static void MalformedLoadIsAtomic()
    {
        var valid = Game.Save();
        var badHeader = (byte[])valid.Clone(); badHeader[0] ^= 0x7F;
        foreach (var invalid in new[] { Array.Empty<byte>(), new byte[] { 1, 2, 3, 4 }, badHeader, valid[..(valid.Length - 8)], SaveWithMissingPlayer() })
        {
            var before = State(); var actor = Player(); var turns = TurnManager.Active; var inventory = InventoryIdentity();
            bool rejected = false;
            try { var returned = Parse(Game.Load(invalid)); rejected = !string.IsNullOrWhiteSpace(Text(returned, "error")); }
            catch (Exception) { rejected = true; }
            Check(rejected, "Malformed save input was accepted.");
            SamePlayerState(before, State(), "Rejected load");
            Check(ReferenceEquals(actor, Player()) && ReferenceEquals(turns, TurnManager.Active), "A rejected load replaced live core identities.");
            Check(InventoryIdentity() == inventory, "A rejected load changed inventory.");
        }
    }

    private static void DamagePreservesMaximum()
    {
        var before = Parse(Game.NewGame("classic", 8128, "HP contract"));
        var player = Player();
        CombatSystem.ApplyDamage(player, 7, null, CurrentZone(player));
        var after = State();
        Check(Number(after, "hp") == Number(before, "hp") - 7, "Actual damage did not lower current HP by seven.");
        Check(Number(after, "maxHp") == Number(before, "maxHp"), "Damage shrank the displayed HP maximum.");
        Check(Number(after, "maxHp") == player.GetStat("Hitpoints").Max, "The view disagrees with the native maximum.");
    }

    private static void FaultedTurnRecovers()
    {
        var before = Parse(Game.NewGame("classic", 8128, "Recovery contract"));
        var checkpoint = Game.Save(); var originalPlayer = Player();
        var failure = new EndTurnProbe { ThrowOnEnd = true }; originalPlayer.AddPart(failure);
        try
        {
            var failed = Send("wait");
            Check(failure.Count == 1, "The fault fixture must interrupt one actual player EndTurn.");
            Check(Boolean(failed, "faulted"), "An unexpected turn exception must pause the facade.");
            Check(!TurnManager.Active.WaitingForInput, "The test must exercise a genuinely unsettled scheduler.");
            bool refusedSave = false;
            try { Game.Save(); } catch (InvalidOperationException) { refusedSave = true; }
            Check(refusedSave, "A faulted expedition must not replace its healthy checkpoint.");
            var restored = Parse(Game.Load(checkpoint));
            SamePlayerState(before, restored, "Recovery from an interrupted turn");
            Check(!Boolean(restored, "faulted") && TurnManager.Active.WaitingForInput, "Restoring a valid checkpoint must clear the fault and resume input.");
            var resumed = Send("wait");
            Check(Number(resumed, "tick") > Number(restored, "tick") && !Boolean(resumed, "faulted"), "The restored expedition cannot take another real turn.");
        }
        finally { originalPlayer.RemovePart(failure); }
    }

    private static void OpeningStairsRoundTrip()
    {
        var initial = Parse(Game.NewGame("classic", 8128, "Stairs contract"));
        var surface = CurrentZone(Player());
        var rejected = Send("stairs", new() { ["value"] = "down" });
        SamePlayerState(initial, rejected, "Stairs away from the entrance");
        Check(!string.IsNullOrEmpty(Text(rejected, "error")), "Stairs must require the real entrance underfoot.");
        WalkTo(GleanersDistrict.EntranceX, GleanersDistrict.EntranceY);
        var before = State(); var below = Send("stairs", new() { ["value"] = "down" });
        Check(Text(below, "zone") == GleanersCellarBuilder.ZoneID, "The entrance did not build the native cellar.");
        Check(Number(below, "x") == GleanersCellarBuilder.StairsX && Number(below, "y") == GleanersCellarBuilder.StairsY, "The cellar arrival ignored its real paired endpoint.");
        Check(Number(below, "tick") > Number(before, "tick"), "Descending must spend a turn.");
        var cellar = CurrentZone(Player());
        var cellarCreatures = cellar.GetEntitiesWithTag("Creature").Where(e => e != Player()).ToArray();
        var returned = Send("stairs", new() { ["value"] = "up" });
        Check(Text(returned, "zone") == GleanersDistrict.SurfaceID, "The return stair did not reach the glade.");
        Check(Number(returned, "x") == GleanersDistrict.EntranceX && Number(returned, "y") == GleanersDistrict.EntranceY, "The return stair ignored its surface endpoint.");
        Check(ReferenceEquals(CurrentZone(Player()), surface), "Returning replaced the retained surface zone.");
        Check(cellarCreatures.All(e => !TurnManager.Active.IsRegistered(e)), "Inactive cellar creatures remained on the active scheduler.");
        Check(TurnManager.Active.WaitingForInput, "The stair turn did not settle.");
    }

    private static void StartingBuildContract(string id)
    {
        var state = Parse(Game.NewGame(id, 8128, "Build " + id)); var player = Player();
        var definition = StartingBuildRegistry.Get(id);
        Check(StartingBuildRegistry.All.Count == 7 && definition != null, "The embedded registry must contain the seven shipped builds.");
        Check(Text(state, "build") == id && player.GetProperty(StartingBuildService.PropertyName) == id, "The selected build was not applied.");
        Check(Number(state, "hp") > 0 && !Boolean(state, "dead") && !Boolean(state, "faulted"), "The build did not start alive.");
        Check(TurnManager.Active.WaitingForInput && state["cells"] is JsonArray cells && cells.Count > 0, "The build did not enter a rendered, playable zone.");
        var inventory = player.GetPart<InventoryPart>();
        var all = inventory.Objects.Concat(inventory.GetAllEquipped()).Distinct().ToArray();
        if (definition!.IsClassic)
        {
            Check(StartingBuildService.PrimaryHandWeapon(player)?.BlueprintName == "Dagger", "Classic lost its native starting weapon.");
            Check(player.GetPart<ActivatedAbilitiesPart>().AbilityList.Count == 6, "Classic lost its six native opening abilities.");
            return;
        }
        var attributes = definition.Attributes;
        foreach (var pair in new[] { ("Strength", attributes.Strength), ("Agility", attributes.Agility), ("Toughness", attributes.Toughness), ("Ego", attributes.Ego) })
            Check(player.GetStat(pair.Item1).BaseValue == pair.Item2, id + " has the wrong " + pair.Item1 + ".");
        foreach (var group in definition.Items.GroupBy(item => item.Blueprint))
            Check(all.Where(item => item.BlueprintName == group.Key).Sum(item => item.GetPart<StackerPart>()?.StackCount ?? 1) >= group.Sum(item => item.EffectiveCount), id + " is missing starting " + group.Key + ".");
        foreach (var item in definition.Items.Where(item => item.Equip))
            Check(inventory.GetAllEquipped().Any(e => e.BlueprintName == item.Blueprint), id + " did not equip " + item.Blueprint + ".");
        foreach (var skill in definition.Skills)
            Check(player.GetPart<SkillsPart>()?.HasSkill(skill) == true, id + " is missing native skill " + skill + ".");
    }

    private static void GroupedPickup()
    {
        Game.NewGame("hurler", 8128, "Pickup contract");
        var player = Player(); var inventory = player.GetPart<InventoryPart>(); var zone = CurrentZone(player);
        var items = inventory.Objects.Where(e => e.GetPart<PhysicsPart>()?.Takeable == true).Take(2).ToArray();
        Check(items.Length == 2, "The real build needs two carried items for this contract.");
        foreach (var item in items) RequireSuccess(Send("item", new() { ["id"] = item.ID, ["command"] = "Drop" }), "Dropping the pickup control");
        Send("close");
        var probe = new EndTurnProbe(); player.AddPart(probe);
        try
        {
            var before = State(); var after = Send("pickup", new() { ["ids"] = items.Select(e => e.ID).ToArray() });
            RequireSuccess(after, "Grouped pickup");
            Check(probe.Count == 1 && Number(after, "tick") > Number(before, "tick"), "A grouped pickup must pay exactly one player EndTurn.");
            Check(items.All(e => inventory.Objects.Contains(e) && zone.GetEntityCell(e) == null), "The selected ground items were not transferred to the real pack.");
        }
        finally { player.RemovePart(probe); }
    }

    private static void OrdinaryThrow()
    {
        Game.NewGame("hurler", 8128, "Throw contract");
        var player = Player(); var zone = CurrentZone(player); var item = CarriedDagger();
        var target = FreeThrowCell(player, zone); var before = State();
        var invalid = Send("throwPreview", new() { ["id"] = item.ID, ["x"] = -1, ["y"] = -1, ["value"] = "Ordinary" });
        SamePlayerState(before, invalid, "An invalid throw preview");
        Check(!string.IsNullOrEmpty(Text(invalid, "error")), "An invalid throw target needs a rejection.");
        var preview = Send("throwPreview", new() { ["id"] = item.ID, ["x"] = target.X, ["y"] = target.Y, ["command"] = "ground", ["value"] = "Ordinary" });
        RequireSuccess(preview, "Ordinary throw preview"); SamePlayerState(before, preview, "Throw preview");
        Check(preview["throwPreview"] is JsonObject view && !string.IsNullOrWhiteSpace(Text(view, "description")), "A throw needs its actual trajectory preview before confirmation.");
        int countBefore = CarriedCount(player, "Dagger");
        var stale = Send("throw", revision: Number(preview, "revision") - 1);
        SamePlayerState(preview, stale, "Stale throw confirmation");
        Check(CarriedCount(player, "Dagger") == countBefore, "Stale confirmation consumed ammunition.");
        var probe = new EndTurnProbe(); player.AddPart(probe);
        try
        {
            var after = Send("throw"); RequireSuccess(after, "Confirmed ordinary throw");
            Check(probe.Count == 1 && Number(after, "tick") > Number(preview, "tick"), "An ordinary launch must spend exactly one player turn.");
            Check(CarriedCount(player, "Dagger") == countBefore - 1, "The native throw did not extract exactly one dagger.");
            Check(zone.GetAllEntities().Any(e => e.BlueprintName == "Dagger" && e.GetPart<ThrownRecoveryPart>() != null), "The actual projectile did not land in the world.");
        }
        finally { player.RemovePart(probe); }
    }

    private static void ChangedThrowTarget()
    {
        Game.NewGame("hurler", 8128, "Throw identity contract");
        var player = Player(); var zone = CurrentZone(player); var target = AddThrowTarget(player, zone); var item = CarriedDagger();
        try
        {
            var cell = zone.GetEntityCell(target);
            var preview = Send("throwPreview", new() { ["id"] = item.ID, ["x"] = cell.X, ["y"] = cell.Y, ["value"] = "Ordinary" });
            RequireSuccess(preview, "Targeted throw preview");
            int quantity = CarriedCount(player, "Dagger"); zone.RemoveEntity(target);
            var after = Send("throw"); SamePlayerState(preview, after, "Throw against a removed target");
            Check(!string.IsNullOrEmpty(Text(after, "error")), "A vanished target must invalidate its preview.");
            Check(CarriedCount(player, "Dagger") == quantity, "Invalid target identity consumed ammunition.");
        }
        finally { zone.RemoveEntity(target); }
    }

    private static void MeasuredThrow()
    {
        Game.NewGame("hurler", 8128, "Measured throw contract");
        var player = Player(); var zone = CurrentZone(player); var target = AddThrowTarget(player, zone); var item = CarriedDagger();
        var probe = new EndTurnProbe(); player.AddPart(probe);
        try
        {
            var ability = player.GetPart<ActivatedAbilitiesPart>().AbilityList.Single(a => a.Command == "CommandMeasuredThrow");
            var before = State(); var selection = Send("ability", new() { ["id"] = ability.ID.ToString() });
            RequireSuccess(selection, "Measured Throw selection"); SamePlayerState(before, selection, "Selecting Measured Throw");
            Check(Text(selection, "panel") == "throwing", "The skill did not open its native selection mode.");
            var cell = zone.GetEntityCell(target); int quantity = CarriedCount(player, "Dagger");
            var preview = Send("throwPreview", new() { ["id"] = item.ID, ["x"] = cell.X, ["y"] = cell.Y, ["value"] = "Measured" });
            RequireSuccess(preview, "Measured Throw preview"); SamePlayerState(before, preview, "Measured preview");
            var prepared = Send("throw"); RequireSuccess(prepared, "Measured Throw preparation");
            Check(probe.Count == 1 && Number(prepared, "tick") > Number(before, "tick"), "Preparing aim must spend its own turn.");
            Check(CarriedCount(player, "Dagger") == quantity && player.GetPart<ThrowingStatePart>()?.Prepared == true, "Preparation must retain the item and native aimed state.");
            Check(prepared["throwPreview"] is JsonObject next && Text(next, "mode") == "Ordinary", "Prepared aim must request a separate launch confirmation.");
            var launched = Send("throw"); RequireSuccess(launched, "Measured Throw launch");
            Check(probe.Count == 2 && Number(launched, "tick") > Number(prepared, "tick"), "Launching after preparation must spend the second distinct turn.");
            Check(CarriedCount(player, "Dagger") == quantity - 1 && player.GetPart<ThrowingStatePart>()?.Prepared != true, "The launch must consume one item and settle the aim.");
        }
        finally { zone.RemoveEntity(target); player.RemovePart(probe); }
    }

    private static void OpeningRepair()
    {
        Game.NewGame("classic", 8128, "Repair contract");
        var surface = CurrentZone(Player()); var well = surface.GetAllEntities().Single(e => e.BlueprintName == "RepairLinedWell");
        Check(!well.GetPart<WellPart>().IsUsable && !well.GetPart<RepairablePart>().Repaired, "The opening well must begin physically broken.");
        WalkTo(GleanersDistrict.EntranceX, GleanersDistrict.EntranceY);
        RequireSuccess(Send("stairs", new() { ["value"] = "down" }), "Entering the supply cellar");
        var cellar = CurrentZone(Player());
        var crate = cellar.GetAllEntities().Single(e => e.GetProperty(GleanersCellarBuilder.RoleKey) == "supplies");
        var beam = cellar.GetAllEntities().Single(e => e.GetProperty(GleanersCellarBuilder.RoleKey) == "beam");
        int sideY = cellar.GetEntityCell(beam).Y, detourY = sideY < 12 ? 2 : 22;
        var crateCell = cellar.GetEntityCell(crate);
        // Follow the real, covered service passage and its permanent beam detour.
        foreach (var point in new[] { (40, sideY), (46, sideY), (46, detourY), (50, detourY), (50, sideY), (crateCell.X, sideY), (crateCell.X, crateCell.Y + (sideY < 12 ? -1 : 1)) }) WalkTo(point.Item1, point.Item2);
        var opened = Send("world", new() { ["id"] = crate.ID, ["command"] = "OpenContainer" }); RequireSuccess(opened, "Opening the supply crate");
        var clay = crate.GetPart<ContainerPart>().Contents.Where(e => e.BlueprintName == "FireClay").ToArray();
        Check(clay.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1) == 2, "The cellar must contain its two finite native clay units.");
        RequireSuccess(Send("container", new() { ["ids"] = clay.Select(e => e.ID).ToArray() }), "Taking the finite clay");
        Check(CarriedCount(Player(), "FireClay") == 2, "The actual container transfer did not yield the repair material.");
        foreach (var point in new[] { (crateCell.X, sideY), (50, sideY), (50, detourY), (46, detourY), (46, sideY), (40, sideY), (GleanersCellarBuilder.StairsX, GleanersCellarBuilder.StairsY) }) WalkTo(point.Item1, point.Item2);
        RequireSuccess(Send("stairs", new() { ["value"] = "up" }), "Returning with repair supplies");
        var wellCell = surface.GetEntityCell(well); WalkAdjacent(wellCell.X, wellCell.Y);
        var before = State(); var repaired = Send("world", new() { ["id"] = well.ID, ["command"] = RepairablePart.RepairCommand });
        RequireSuccess(repaired, "Repairing the opening well");
        var repair = well.GetPart<RepairablePart>();
        Check(repair.Repaired && well.GetPart<WellPart>().IsUsable && ReferenceEquals(repair.RepairedBy, Player()), "The real repair did not restore the well's function and author.");
        Check(CarriedCount(Player(), "FireClay") == 0 && Number(repaired, "tick") > Number(before, "tick"), "Repair must consume both finite clay units and pay its native turn.");
        Check(!string.IsNullOrEmpty(repair.RepairCauseID), "The repair must retain its native provenance.");
        var again = Send("world", new() { ["id"] = well.ID, ["command"] = RepairablePart.RepairCommand });
        SamePlayerState(repaired, again, "Repeating an already-completed repair");
        var saved = Game.Save(); Game.Load(saved);
        var retained = CurrentZone(Player()).GetAllEntities().Single(e => e.ID == well.ID);
        Check(retained.GetPart<WellPart>().IsUsable && retained.GetPart<RepairablePart>().RepairCauseID == repair.RepairCauseID, "Save/load lost the repaired well or its provenance.");
    }

    private static void NativeBrew()
    {
        Game.NewGame("classic", 8128, "Crafting contract");
        var player = Player(); var pack = player.GetPart<InventoryPart>();
        // One explicitly granted fixture from shipped content; ingredient
        // acquisition is covered separately by the real cellar route above.
        var factory = typeof(BrowserGame).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(field => field.FieldType == typeof(EntityFactory)).GetValue(Game) as EntityFactory;
        var reagent = factory!.CreateEntity("MendleafSprig");
        Check(reagent != null && pack.AddObject(reagent), "The native reagent fixture could not enter the pack.");
        var before = State(); var beforeInventory = InventoryIdentity();
        var empty = Send("craft", new() { ["command"] = "brew" });
        SamePlayerState(before, empty, "An empty brew");
        Check(!string.IsNullOrEmpty(Text(empty, "error")) && InventoryIdentity() == beforeInventory, "An unselected brew must reject without consuming materials.");
        var unowned = Send("craft", new() { ["command"] = "mark", ["id"] = "no-such-reagent" });
        SamePlayerState(before, unowned, "An unowned crafting selection");
        Check(!string.IsNullOrEmpty(Text(unowned, "error")), "Crafting cannot mark an unowned item.");
        var marked = Send("craft", new() { ["command"] = "mark", ["id"] = reagent!.ID });
        RequireSuccess(marked, "Marking the carried reagent"); SamePlayerState(before, marked, "Crafting preview");
        Check(CraftingMarkPart.IsMarked(reagent) && pack.Objects.Contains(reagent), "Preview consumed its marked reagent.");
        var preview = marked["crafting"]?["brew"] as JsonObject;
        Check(preview != null && Boolean(preview, "IsValid") && !string.IsNullOrWhiteSpace(Text(preview, "DisplayName")), "The view did not expose the native resolved brew preview.");
        var probe = new EndTurnProbe(); player.AddPart(probe);
        try
        {
            var brewed = Send("craft", new() { ["command"] = "brew" });
            RequireSuccess(brewed, "Brewing the marked native reagent"); SamePlayerState(before, brewed, "Native free crafting");
            Check(probe.Count == 0 && !pack.Objects.Contains(reagent), "Crafting must consume its ingredient without charging a player turn.");
            var product = pack.Objects.Single(e => e.BlueprintName == BrewingService.BrewBlueprintName);
            Check(product.GetPart<TonicPart>()?.Healing == "1d4", "The brewed mendleaf lost its native 1d4 healing effect.");
            Check(product.GetDisplayName() == Text(preview!, "DisplayName"), "The native brew disagrees with its preview: actual '" + product.GetDisplayName() + "', expected '" + Text(preview!, "DisplayName") + "'.");
        }
        finally { player.RemovePart(probe); }
    }

    private static void WalkTo(int x, int y)
    {
        for (int n = 0; n < 160; n++)
        {
            var player = Player(); var zone = CurrentZone(player); var here = zone.GetEntityCell(player);
            if (here.X == x && here.Y == y) return;
            if (Text(State(), "panel") != "") Send("close");
            var path = StairTravel.PathTo(zone, player, x, y, ignoreCreatures: false);
            Check(path != null && path.Count > 0, "No actual walkable route to " + x + "," + y + " from " + here.X + "," + here.Y + ".");
            var next = path![0]; var state = Send("step", new() { ["x"] = next.dx, ["y"] = next.dy });
            RequireSuccess(state, "Walking through the actual zone");
            Check(!Boolean(state, "dead"), "The player died while following the covered route.");
        }
        throw new Exception("The bounded route to " + x + "," + y + " did not finish.");
    }
    private static void WalkAdjacent(int x, int y)
    {
        var player = Player(); var zone = CurrentZone(player);
        var route = new[] { (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1) }
            .Select(p => (point: p, path: StairTravel.PathTo(zone, player, p.Item1, p.Item2, ignoreCreatures: false)))
            .Where(p => p.path != null).OrderBy(p => p.path.Count).First();
        WalkTo(route.point.Item1, route.point.Item2);
    }
    private static Entity CarriedDagger() => Player().GetPart<InventoryPart>().Objects.First(e => e.BlueprintName == "Dagger");
    private static int CarriedCount(Entity player, string blueprint) => player.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
    private static Cell FreeThrowCell(Entity player, Zone zone)
    {
        var here = zone.GetEntityCell(player);
        return new[] { (2, 0), (-2, 0), (0, 2), (0, -2), (1, 1), (-1, 1), (1, -1), (-1, -1) }
            .Select(d => zone.GetCell(here.X + d.Item1, here.Y + d.Item2))
            .First(c => c != null && c.IsVisible && !c.BlocksMovement(player) && !c.Occupants.Any(e => e.HasTag("Creature")));
    }
    private static Entity AddThrowTarget(Entity player, Zone zone)
    {
        var cell = FreeThrowCell(player, zone); var target = new Entity { ID = "web-contract-target", BlueprintName = "ContractThrowTarget" };
        target.AddPart(new PhysicsPart { Solid = true, Weight = 60 });
        target.AddPart(new RenderPart { DisplayName = "contract throwing target", RenderString = "T", RenderLayer = 99 });
        target.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", Owner = target, BaseValue = 100, Max = 100 };
        Check(zone.AddEntity(target, cell.X, cell.Y), "The throwing fixture could not be placed.");
        return target;
    }
    private sealed class EndTurnProbe : Part
    {
        public int Count; public bool ThrowOnEnd;
        public override string Name => "WebContractTurnProbe";
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "EndTurn")
            {
                Count++;
                if (ThrowOnEnd) throw new InvalidOperationException("Expected contract fixture: interrupted EndTurn.");
            }
            return true;
        }
    }
    private static void RequireSuccess(JsonObject state, string action)
    {
        Check(!Boolean(state, "faulted"), action + " faulted the expedition.");
        Check(string.IsNullOrEmpty(Text(state, "error")), action + ": " + Text(state, "error"));
    }

    private static JsonObject Send(string kind, Dictionary<string, object?>? args = null, int? revision = null)
    {
        args ??= new(); args["kind"] = kind; args["revision"] = revision ?? Number(State(), "revision");
        return Parse(Game.Dispatch(JsonSerializer.Serialize(args)));
    }
    private static JsonObject State() => Parse(Game.Snapshot());
    private static JsonObject Parse(string json) => JsonNode.Parse(json) as JsonObject ?? throw new Exception("Expected a JSON state object.");
    private static int Number(JsonObject state, string name) => state[name]?.GetValue<int>() ?? throw new Exception("Missing number: " + name);
    private static string Text(JsonObject state, string name) => state[name]?.GetValue<string>() ?? "";
    private static bool Boolean(JsonObject state, string name) => state[name]?.GetValue<bool>() ?? throw new Exception("Missing boolean: " + name);
    private static Entity Player() => TurnManager.Active?.CurrentActor is Entity actor && actor.HasTag("Player") ? actor : throw new Exception("No current player actor.");
    private static Zone CurrentZone(Entity player) => typeof(Entity).GetField("SpatialZone", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(player) as Zone ?? throw new Exception("The real player is not placed in a zone.");
    private static byte[] SaveWithMissingPlayer()
    {
        // A complete binary document can still be an invalid expedition. Keep
        // the actual graph/clock but omit its player reference, so truncation
        // checks alone cannot satisfy the atomic import boundary.
        var zones = typeof(BrowserGame).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(field => field.FieldType == typeof(OverworldZoneManager)).GetValue(Game) as OverworldZoneManager;
        Check(zones != null, "The incomplete-session fixture needs the actual zone manager.");
        var candidate = GameSessionState.Capture("contract-invalid-player", "standalone-web-1", zones, TurnManager.Active, null, 0, TurnManager.World);
        using var stream = new MemoryStream(); candidate.Save(new SaveWriter(stream)); return stream.ToArray();
    }
    private static string InventoryIdentity() => string.Join("|", Player().GetPart<InventoryPart>().Objects.Select(e => e.ID + ":" + e.BlueprintName + ":" + (e.GetPart<StackerPart>()?.StackCount ?? 1)).OrderBy(s => s, StringComparer.Ordinal));
    private static void SamePlayerState(JsonObject before, JsonObject after, string action)
    {
        foreach (string key in new[] { "x", "y", "tick", "hp", "maxHp", "zone", "dead" })
            Check(JsonNode.DeepEquals(before[key], after[key]), action + " changed " + key + ": " + before[key] + " -> " + after[key]);
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Run(string name, Action test)
    {
        var timer = Stopwatch.StartNew();
        try { test(); Results.Add(new { name, status = "passed", durationMs = timer.ElapsedMilliseconds }); Console.WriteLine("PASS " + name); }
        catch (Exception error) { Failures++; Results.Add(new { name, status = "failed", durationMs = timer.ElapsedMilliseconds, error = error.ToString() }); Console.WriteLine("FAIL " + name + "\n" + error); }
    }
}
