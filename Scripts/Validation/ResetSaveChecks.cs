using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Controllers;
using MidnightSteepApi.Data;
using MidnightSteepApi.Models;

internal static class ResetSaveChecks
{
    public static async Task Run(Func<ContextDb> createContext)
    {
        await using var db = createContext();
        var user = new User
        {
            login = "reset-save-check",
            password = "test-only",
            last_login = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var created = Json(await new SaveController(db).CreateSave(new()
                {
                    user_id = user.id,
                    character_id = 1
                }));
        var oldId = created.GetProperty("save").GetProperty("id").GetInt32();
        var save = await db.Saveses.SingleAsync(x => x.id == oldId);
        save.coins = 400;
        var progress = await db.LevelProgresses.SingleAsync(x => x.save_id == oldId);
        progress.is_completed = true;
        progress.best_revenue = 300;
        var tableId = await db.Furnitures.Where(x => x.asset_name == "Table").Select(x => x.id).SingleAsync();
        var lampId = await db.Furnitures.Where(x => x.asset_name == "PinkLamp").Select(x => x.id).SingleAsync();
        var parent = new PlacedItem
        {
            save_id = oldId,
            furniture_id = tableId,
            position_x = 2
        };
        db.PlacedItems.Add(parent);
        await db.SaveChangesAsync();
        db.PlacedItems.Add(new PlacedItem
            {
                save_id = oldId,
                furniture_id = lampId,
                parent_placed_item_id = parent.id,
                position_x = 2
            });
        db.DessertPurchases.Add(new DessertPurchase
            {
                purchase_id = Guid.NewGuid(),
                save_id = oldId,
                dessert_id = await db.Desserts.Select(x => x.id).FirstAsync(),
                quantity = 3,
                spent_coins = 10
            });
        await db.SaveChangesAsync();
        async Task<JsonElement> Reset(int id, int owner)
        {
            await using var requestDb = createContext();
            return Json(await new SaveController(requestDb).ResetSave(
                    id,
                    new()
                    {
                        user_id = owner
                    }
                ));
        }

        Check(
            !Json(await new SaveController(db).ResetSave(oldId, null)).GetProperty("status").GetBoolean(),
            "Reset rejects a missing body"
        );
        Check(
            !(await Reset(0, user.id)).GetProperty("status").GetBoolean() &&
                !(await Reset(oldId, 0)).GetProperty("status").GetBoolean() &&
                !(await Reset(oldId, int.MaxValue)).GetProperty("status").GetBoolean(),
            "Reset rejects invalid save and user IDs"
        );
        int otherUser = await db.Users.Where(x => x.id != user.id).Select(x => x.id).FirstAsync();
        Check(
            !(await Reset(oldId, otherUser)).GetProperty("status").GetBoolean(),
            "Reset cannot delete another user's save"
        );
        await db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION reset_failure_check() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'Forced reset failure'; END $$;
            CREATE TRIGGER reset_failure_check BEFORE DELETE ON "Saveses"
            FOR EACH ROW EXECUTE FUNCTION reset_failure_check();
            """);
        try
        {
            Check(!(await Reset(oldId, user.id)).GetProperty("status").GetBoolean(), "A reset write failure is reported");
            Check(
                await db.Saveses.AsNoTracking().AnyAsync(x => x.id == oldId &&
                        x.coins == 400) &&
                    await db.LevelProgresses.AsNoTracking().AnyAsync(x => x.save_id == oldId &&
                        x.is_completed) &&
                    await db.Inventories.CountAsync(x => x.save_id == oldId) == 9 &&
                    await db.PlacedItems.CountAsync(x => x.save_id == oldId) == 2 &&
                    await db.DessertPurchases.CountAsync(x => x.save_id == oldId) == 1,
                "Failed reset restores the original wallet, progress, inventory, nested layout and purchases"
            );
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("""
                DROP TRIGGER reset_failure_check ON "Saveses";
                DROP FUNCTION reset_failure_check();
                """);
        }

        var result = await Reset(oldId, user.id);
        Check(
            result.GetProperty("status").GetBoolean() &&
                result.GetProperty("user_id").GetInt32() == user.id &&
                result.GetProperty("save_id").GetInt32() == oldId,
            "Reset succeeds without choosing a character"
        );
        Check(
            !await db.Saveses.AsNoTracking().AnyAsync(x => x.id == oldId) &&
                !await db.LevelProgresses.AnyAsync(x => x.save_id == oldId) &&
                !await db.Inventories.AnyAsync(x => x.save_id == oldId) &&
                !await db.PlacedItems.AnyAsync(x => x.save_id == oldId) &&
                !await db.DessertPurchases.AnyAsync(x => x.save_id == oldId),
            "Old progress, inventory, nested layout and purchases are removed together"
        );
        Check(
            await db.Users.AnyAsync(x => x.id == user.id &&
                    x.login == user.login) &&
                !await db.Saveses.AsNoTracking().AnyAsync(x => x.user_id == user.id),
            "The account remains registered without an automatically recreated save"
        );
        await using (var loginAfterReset = createContext())
        {
            var empty = Json(await new SaveController(loginAfterReset).GetSaveByUser(user.id));
            Check(
                !empty.GetProperty("status").GetBoolean() &&
                    empty.GetProperty("code").GetString() == "save_not_found",
                "Signing in after reset returns the new-player character-selection state"
            );
        }

        Check(
            (await Reset(oldId, user.id)).GetProperty("status").GetBoolean(),
            "A retry after a lost reset response succeeds without recreating a save"
        );
        int newId;
        await using (var creation = createContext())
        {
            var fresh = Json(await new SaveController(creation).CreateSave(new()
                    {
                        user_id = user.id,
                        character_id = 2
                    }));
            Check(fresh.GetProperty("status").GetBoolean(), "Character selection creates a fresh save normally");
            newId = fresh.GetProperty("save").GetProperty("id").GetInt32();
        }

        var replacement = await db.Saveses.AsNoTracking().SingleAsync(x => x.user_id == user.id);
        Check(
            newId != oldId && replacement.id == newId && replacement.character_id == 2 && replacement.coins == 0,
            "The chosen character starts with a new save ID and zero coins"
        );
        var firstLevel = await db.Levels.OrderBy(x => x.level_number).Select(x => x.id).FirstAsync();
        Check(
            await db.LevelProgresses.CountAsync(x => x.save_id == newId) == 1 &&
                await db.LevelProgresses.AnyAsync(x => x.save_id == newId &&
                    x.level_id == firstLevel &&
                    x.is_unlocked &&
                    !x.is_completed &&
                    x.best_revenue == 0 &&
                    x.best_completed_orders == 0) &&
                await db.Inventories.CountAsync(x => x.save_id == newId &&
                    x.quantity == 1) == 9 &&
                !await db.PlacedItems.AnyAsync(x => x.save_id == newId),
            "New save has only the first level unlocked, the starter pack and an empty layout"
        );
        Check(
            (await Reset(oldId, user.id)).GetProperty("status").GetBoolean() &&
                await db.Saveses.AsNoTracking().AnyAsync(x => x.id == newId &&
                    x.character_id == 2),
            "Repeating an old reset request preserves the subsequently created save"
        );
        await using var loginDb = createContext();
        var loaded = Json(await new SaveController(loginDb).GetSaveByUser(user.id));
        Check(loaded.GetProperty("save").GetProperty("id").GetInt32() == newId, "Signing in again loads the new save");
    }

    private static JsonElement Json(IActionResult result)
    {
        return JsonSerializer.SerializeToElement(((OkObjectResult)result).Value);
    }

    private static void Check(bool value, string message)
    {
        if (!value)
        {
            throw new InvalidOperationException(message);
        }

        Console.WriteLine("PASS: " + message);
    }
}
