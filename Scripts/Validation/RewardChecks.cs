using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Controllers;
using MidnightSteepApi.Data;
using MidnightSteepApi.Dtos;
using MidnightSteepApi.Models;

internal static class RewardChecks
{
    public static async Task Run(ContextDb db, Func<ContextDb> createContext)
    {
        var user = new User
        {
            login = "reward-check",
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
        var saveId = created.GetProperty("save").GetProperty("id").GetInt32();
        var levels = await db.Levels.AsNoTracking().OrderBy(x => x.level_number).ToListAsync();
        var milkId = await db.Furnitures.Where(x => x.asset_name == "crate_milk").Select(x => x.id).SingleAsync();
        var lemonId = await db.Furnitures.Where(x => x.asset_name == "crate_lemon").Select(x => x.id).SingleAsync();
        CompleteLevelRequest Request(int index, bool passed = true) => new()
        {
            save_id = saveId,
            level_id = levels[index].id,
            completed_orders = passed ? levels[index].required_orders : 0,
            revenue = passed ? levels[index].required_revenue : 0
        };
        async Task<Dictionary<string, int>> Inventory() => await db.Inventories.AsNoTracking().Where(x => x.save_id == saveId).Select(x => new
            {
                x.furniture.asset_name,
                x.quantity
            })
            .ToDictionaryAsync(x => x.asset_name, x => x.quantity);
        async Task<JsonElement> Complete(int index)
        {
            await using var fresh = createContext();
            return Json(await new LevelController(fresh).CompleteLevel(Request(index)));
        }

        var failed = Json(await new LevelController(db).CompleteLevel(Request(0, false)));
        Check(
            failed.GetProperty("status").GetBoolean() &&
                !failed.GetProperty("completed").GetBoolean() &&
                (await Inventory()).Count == 9,
            "Failed level grants no crates"
        );
        await db.Furnitures.Where(x => x.id == milkId).ExecuteUpdateAsync(x => x.SetProperty(f => f.asset_name, "missing_milk"));
        Check(
            !Json(await new LevelController(db).CompleteLevel(Request(0))).GetProperty("status").GetBoolean(),
            "Missing reward crate rejects completion"
        );
        db.ChangeTracker.Clear();
        Check(
            !await db.LevelProgresses.AnyAsync(x => x.save_id == saveId &&
                    (x.is_completed ||
                    x.level_id == levels[1].id)) &&
                await db.Saveses.Where(x => x.id == saveId).Select(x => x.coins).SingleAsync() == 0 &&
                (await Inventory()).Count == 9,
            "Missing catalog leaves progress, coins and inventory unchanged"
        );
        await db.Furnitures.Where(x => x.id == milkId).ExecuteUpdateAsync(x => x.SetProperty(f => f.asset_name, "crate_milk"));
        var milkIngredient = await db.Ingredients.SingleAsync(x => x.asset_name == "crate_milk");
        var lemonDrink = await db.Drinks.SingleAsync(x => x.asset_name == "drink_lemon_tea");
        var sharedRecipe = new Recipes
        {
            drink_id = lemonDrink.id,
            ingredient_id = milkIngredient.id
        };
        db.Recipes.Add(sharedRecipe);
        db.Inventories.Add(new Inventory
            {
                save_id = saveId,
                furniture_id = lemonId,
                quantity = 1
            });
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync($"ALTER TABLE \"Inventories\" ADD CONSTRAINT reward_failure_check CHECK (save_id <> {saveId} OR furniture_id <> {milkId})");
        try
        {
            await Complete(0);
            throw new InvalidOperationException("Expected inventory write failure");
        }
        catch (DbUpdateException)
        {
        }

        Check(
            !await db.LevelProgresses.AnyAsync(x => x.save_id == saveId &&
                    (x.is_completed ||
                    x.level_id == levels[1].id)) &&
                await db.Saveses.Where(x => x.id == saveId).Select(x => x.coins).SingleAsync() == 0 &&
                (await Inventory()).Count == 10,
            "Reward write failure rolls back completion, coins and crates"
        );
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Inventories\" DROP CONSTRAINT reward_failure_check");
        var concurrent = await Task.WhenAll(Complete(0), Complete(0));
        var inventory = await Inventory();
        Check(
            concurrent.All(x => x.GetProperty("status").GetBoolean()) &&
                inventory.GetValueOrDefault("crate_milk") == 1 &&
                inventory.GetValueOrDefault("crate_lemon") == 1 &&
                inventory.Count == 11,
            "Concurrent completions grant each new ingredient once across all recipes"
        );
        Check(
            !inventory.ContainsKey("crate_tea_leaves") && !inventory.ContainsKey("crate_sugar"),
            "Ingredients from already unlocked recipes do not grant crates"
        );
        await db.Inventories.Where(x => x.save_id == saveId && x.furniture_id == milkId).ExecuteDeleteAsync();
        await Complete(0);
        Check(!(await Inventory()).ContainsKey("crate_milk"), "Replay cannot restore a previously granted crate");
        var expected = new[]
        {
            new[]
            {
                "crate_honey",
                "crate_berries"
            },
            new[]
            {
                "crate_lavender",
                "crate_rose_petals"
            },
            Array.Empty<string>(),
            Array.Empty<string>()
        };
        for (var index = 1; index < levels.Count; index++)
        {
            var before = await Inventory();
            var result = await Complete(index);
            inventory = await Inventory();
            Check(
                result.GetProperty("status").GetBoolean() &&
                    inventory.Count == before.Count + expected[index - 1].Length &&
                    expected[index - 1].All(key => inventory.GetValueOrDefault(key) == 1) &&
                    before.All(pair => inventory.GetValueOrDefault(pair.Key) == pair.Value),
                $"Level {levels[index].level_number} rewards only ingredients absent from all earlier recipes"
            );
        }

        var legacyUser = new User
        {
            login = "legacy-unlock-check",
            password = "test-only",
            last_login = DateTime.UtcNow
        };
        db.Users.Add(legacyUser);
        await db.SaveChangesAsync();
        var legacySave = Json(await new SaveController(db).CreateSave(new()
                {
                    user_id = legacyUser.id,
                    character_id = 1
                }))
            .GetProperty("save")
            .GetProperty("id")
            .GetInt32();
        await db.Ingredients.Where(x => x.asset_name == "crate_milk").ExecuteUpdateAsync(x => x.SetProperty(i => i.asset_name, "Milk"));
        await db.Furnitures.Where(x => x.id == milkId).ExecuteUpdateAsync(x => x.SetProperty(i => i.asset_name, "CrateMilk"));
        try
        {
            await using var fresh = createContext();
            var result = Json(await new LevelController(fresh).CompleteLevel(new()
                    {
                        save_id = legacySave,
                        level_id = levels[0].id,
                        completed_orders = levels[0].required_orders,
                        revenue = levels[0].required_revenue
                    }));
            Check(
                result.GetProperty("status").GetBoolean() &&
                    result.GetProperty("completed").GetBoolean() &&
                    await fresh.LevelProgresses.AnyAsync(x => x.save_id == legacySave &&
                        x.level_id == levels[1].id &&
                        x.is_unlocked) &&
                    await fresh.Inventories.AnyAsync(x => x.save_id == legacySave &&
                        x.furniture_id == milkId &&
                        x.quantity == 1),
                "Legacy Milk/CrateMilk keys unlock the next level and grant the matching crate"
            );
        }
        finally
        {
            await db.Ingredients.Where(x => x.asset_name == "Milk").ExecuteUpdateAsync(x => x.SetProperty(i => i.asset_name, "crate_milk"));
            await db.Furnitures.Where(x => x.id == milkId).ExecuteUpdateAsync(x => x.SetProperty(i => i.asset_name, "crate_milk"));
        }

        db.Recipes.Remove(sharedRecipe);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static JsonElement Json(IActionResult result)
    {
        return JsonSerializer.SerializeToElement(((OkObjectResult)result).Value);
    }

    private static void Check(bool ok, string message)
    {
        if (!ok)
        {
            throw new InvalidOperationException(message);
        }

        Console.WriteLine("PASS: " + message);
    }
}
