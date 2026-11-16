using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MidnightSteepApi.Controllers;
using MidnightSteepApi.Data;
using MidnightSteepApi.Dtos;
using MidnightSteepApi.Models;
using Npgsql;

const string previous = "20261021170000_AddUnityGameApiFields";
const string current = "20261109160000_AddDesserts";
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "appsettings.json")));
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? settings.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()!;
var mode = args.FirstOrDefault() ?? "test";
ContextDb CreateContext(string connection, string? historySchema = null) => new(new DbContextOptionsBuilder<ContextDb>().UseNpgsql(connection, options => options.MigrationsHistoryTable("__EFMigrationsHistory", historySchema))
        .Options);
await using var live = CreateContext(connectionString);
if (mode == "inspect")
{
    Console.WriteLine("Known migrations: " + string.Join(", ", live.Database.GetMigrations()));
    Console.WriteLine("Applied migrations: " + string.Join(", ", await live.Database.GetAppliedMigrationsAsync()));
    Console.WriteLine("Pending migrations: " + string.Join(", ", await live.Database.GetPendingMigrationsAsync()));
    if ((await live.Database.GetAppliedMigrationsAsync()).Contains(current))
    {
        foreach (var dessert in await live.Desserts.AsNoTracking().OrderBy(x => x.id).ToListAsync())
        {
            Console.WriteLine($"{dessert.asset_name}: {dessert.batch_size} pcs / {dessert.batch_price} coins, sale {dessert.sale_price}");
        }
    }

    return;
}

if (mode == "seed-coverings")
{
    var backupFolder = Path.Combine(root, ".backups");
    Directory.CreateDirectory(backupFolder);
    var backupFile = Path.Combine(backupFolder, "catalog-before-coverings-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + ".json");
    await File.WriteAllTextAsync(
        backupFile,
        JsonSerializer.Serialize(await live.Furnitures.AsNoTracking().OrderBy(x => x.id).ToListAsync())
    );
    await SqlFile(live, Path.Combine(root, "Scripts/Seed/05_room_coverings.sql"));
    foreach (var item in await live.Furnitures.AsNoTracking().Where(x => x.asset_name == "DarkWoodFloor" || x.asset_name == "LightBueWallpaper")
        .ToListAsync())
    {
        Console.WriteLine($"{item.id}: {item.asset_name}, {item.category}, price {item.price}");
    }

    Console.WriteLine("Catalog backup: " + backupFile);
    return;
}

if (mode == "apply")
{
    var pending = (await live.Database.GetPendingMigrationsAsync()).ToArray();
    if (pending.Length == 0)
    {
        Console.WriteLine("Database is already current.");
        return;
    }

    Check(
        pending.SequenceEqual(new[] {
                current
            }),
        "Only the dessert migration may be pending"
    );
    var backupFolder = Path.Combine(root, ".backups", "database-before-desserts-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
    Directory.CreateDirectory(backupFolder);
    var backupFile = Path.Combine(backupFolder, "MidnightSteep.dump");
    var cs = new NpgsqlConnectionStringBuilder(connectionString);
    var start = new ProcessStartInfo(@"C:\Program Files\PostgreSQL\18\bin\pg_dump.exe")
    {
        UseShellExecute = false,
        CreateNoWindow = true
    };
    foreach (var argument in new[]
    {
        "--host",
        cs.Host!,
        "--port",
        cs.Port.ToString(),
        "--username",
        cs.Username!,
        "--dbname",
        cs.Database!,
        "--format=custom",
        "--file",
        backupFile,
        "--no-password"
    }

    )
    {
        start.ArgumentList.Add(argument);
    }

    start.Environment["PGPASSWORD"] = cs.Password;
    using var dump = Process.Start(start)!;
    await dump.WaitForExitAsync();
    Check(dump.ExitCode == 0 && new FileInfo(backupFile).Length > 0, "Database backup completed");
    var before = await Counts(live);
    await live.Database.MigrateAsync();
    Check(before == await Counts(live), "All table row counts preserved");
    await CheckColumns(live);
    Ok(await new ItemController(live).GetItems());
    Ok(await new DrinkController(live).GetDrinks());
    foreach (var saveId in await live.Saveses.Select(x => x.id).ToListAsync())
    {
        Ok(await new SaveController(live).GetFullSave(saveId));
        Ok(await new PlacedItemController(live).GetPlacedItems(saveId));
    }

    Console.WriteLine("Backup: " + backupFile);
    Console.WriteLine("Working database migrated and read checks passed.");
    return;
}

Check(mode == "test", "Mode must be test, inspect or apply");
var schema = "schema_cleanup_check_" + Guid.NewGuid().ToString("N");
await live.Database.ExecuteSqlRawAsync($"CREATE SCHEMA \"{schema}\"");
try
{
    var cs = new NpgsqlConnectionStringBuilder(connectionString)
    {
        SearchPath = schema
    };
    await using var db = CreateContext(cs.ConnectionString, schema);
    await db.GetService<IMigrator>().MigrateAsync(previous);
    await SqlFile(db, Path.Combine(root, "Scripts/Seed/01_reference_data.sql"));
    await SqlFile(db, Path.Combine(root, "Scripts/Seed/02_furniture_catalog.sql"));
    await SqlFile(db, Path.Combine(root, "Scripts/Seed/04_starter_furniture.sql"));
    await db.Database.ExecuteSqlRawAsync("UPDATE \"Recipes\" SET quantity = 2");
    var existingUser = new User
    {
        login = "existing-schema-check",
        password = "test-only",
        last_login = DateTime.UtcNow
    };
    db.Users.Add(existingUser);
    await db.SaveChangesAsync();
    var existingCreated = Ok(await new SaveController(db).CreateSave(new()
            {
                user_id = existingUser.id,
                character_id = 1
            }));
    var existingSaveId = existingCreated.GetProperty("save").GetProperty("id").GetInt32();
    await db.Database.ExecuteSqlRawAsync("UPDATE \"LevelProgresses\" SET attempts_count = 4, completed_at = CURRENT_TIMESTAMP, best_revenue = 35");
    var before = await Counts(db);
    await db.Database.MigrateAsync();
    Check(before == await Counts(db), "Upgrade preserves existing catalog and recipes");
    Check(
        await db.LevelProgresses.AnyAsync(x => x.save_id == existingSaveId && x.best_revenue == 35 && x.is_unlocked),
        "Existing player progress is preserved"
    );
    Check(!db.Database.HasPendingModelChanges(), "Snapshot matches the EF model");
    await CheckColumns(db);
    for (var i = 0; i < 2; i++)
    {
        await SqlFile(db, Path.Combine(root, "Scripts/Seed/01_reference_data.sql"));
        await SqlFile(db, Path.Combine(root, "Scripts/Seed/02_furniture_catalog.sql"));
        await SqlFile(db, Path.Combine(root, "Scripts/Seed/04_starter_furniture.sql"));
        Check(before == await Counts(db), "Updated seeds are repeatable without duplicates");
    }

    var user = new User
    {
        login = "schema-check",
        password = "test-only",
        last_login = DateTime.UtcNow
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    var saves = new SaveController(db);
    var created = Ok(await saves.CreateSave(new()
            {
                user_id = user.id,
                character_id = 1
            }));
    var saveId = created.GetProperty("save").GetProperty("id").GetInt32();
    var starter = await db.Inventories.AsNoTracking().Where(x => x.save_id == saveId).Select(x => new
        {
            x.furniture.asset_name,
            x.quantity
        })
        .ToDictionaryAsync(x => x.asset_name, x => x.quantity);
    var expectedStarter = new Dictionary<string, int>
    {
        ["PinkLamp"] = 1,
        ["Table"] = 1,
        ["RedChair"] = 1,
        ["HoneyKettle"] = 1,
        ["CrateTeaLeaves"] = 1,
        ["BarCounter"] = 1,
        ["HoneyTrashBin"] = 1,
        ["BlueWallpaper"] = 1,
        ["WoodFloor"] = 1
    };
    Check(
        starter.Count == expectedStarter.Count &&
            expectedStarter.All(x => starter.GetValueOrDefault(x.Key) == x.Value),
        "New save receives exactly the nine starter types and nine items"
    );
    Check(
        created.GetProperty("save").GetProperty("floor_id").GetInt32() == await db.Furnitures.Where(x => x.asset_name == "WoodFloor").Select(x => x.id).SingleAsync() &&
            created.GetProperty("save").GetProperty("wallpaper_id").GetInt32() == await db.Furnitures.Where(x => x.asset_name == "BlueWallpaper").Select(x => x.id).SingleAsync(),
        "Default coverings resolve by asset name and belong to the starter inventory"
    );
    Check(
        created.GetProperty("save").GetProperty("coins").GetInt32() == 0 &&
            !await db.PlacedItems.AnyAsync(x => x.save_id == saveId),
        "Starter furniture is free and available in inventory without an invented layout"
    );
    Failed(await saves.CreateSave(new()
            {
                user_id = user.id,
                character_id = 1
            }));
    Check(
        await db.Inventories.Where(x => x.save_id == saveId).SumAsync(x => x.quantity) == 9,
        "Retrying save creation cannot grant the starter pack twice"
    );
    var failureUser = new User
    {
        login = "starter-failure-check",
        password = "test-only",
        last_login = DateTime.UtcNow
    };
    db.Users.Add(failureUser);
    await db.SaveChangesAsync();
    var beforeFailure = await Counts(db);
    await db.Database.ExecuteSqlRawAsync("UPDATE \"Furnitures\" SET asset_name = 'MissingBarCounter' WHERE asset_name = 'BarCounter'");
    Failed(await saves.CreateSave(new()
            {
                user_id = failureUser.id,
                character_id = 1
            }));
    Check(beforeFailure == await Counts(db), "Missing starter catalog item does not create a partial save");
    await db.Database.ExecuteSqlRawAsync("UPDATE \"Furnitures\" SET asset_name = 'BarCounter' WHERE asset_name = 'MissingBarCounter'");
    var lastSaveId = await db.Saveses.MaxAsync(x => x.id);
    await db.Database.ExecuteSqlRawAsync($"ALTER TABLE \"Inventories\" ADD CONSTRAINT starter_failure_check CHECK (save_id <= {lastSaveId} OR quantity <> 1)");
    Failed(await saves.CreateSave(new()
            {
                user_id = failureUser.id,
                character_id = 1
            }));
    db.ChangeTracker.Clear();
    Check(
        beforeFailure == await Counts(db),
        "Inventory write failure rolls back the save, level and whole starter pack"
    );
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Inventories\" DROP CONSTRAINT starter_failure_check");
    await RewardChecks.Run(db, () => CreateContext(cs.ConnectionString, schema));
    await DessertChecks.Run(db, () => CreateContext(cs.ConnectionString, schema), saveId);
    await ResetSaveChecks.Run(() => CreateContext(cs.ConnectionString, schema));
    var firstLevel = created.GetProperty("opened_level_id").GetInt32();
    var levels = new LevelController(db);
    Ok(await levels.GetLevels(saveId));
    var startResponse = Ok(await levels.StartLevel(new()
            {
                save_id = saveId,
                level_id = firstLevel
            }));
    Check(!startResponse.TryGetProperty("attempts_count", out _), "Start does not expose a removed counter");
    var secondLevel = await db.Levels.Where(x => x.level_number == 2).Select(x => x.id).SingleAsync();
    Failed(await levels.StartLevel(new()
            {
                save_id = saveId,
                level_id = secondLevel
            }));
    var items = new ItemController(db);
    var stove = await db.Furnitures.SingleAsync(x => x.asset_name == "stove");
    var cup = await db.Furnitures.SingleAsync(x => x.asset_name == "tea_cup");
    var milk = await db.Furnitures.SingleAsync(x => x.asset_name == "crate_milk");
    Failed(await items.Purchase(new()
            {
                save_id = saveId,
                furniture_id = milk.id,
                quantity = 1
            }));
    var complete = Ok(await levels.CompleteLevel(new()
            {
                save_id = saveId,
                level_id = firstLevel,
                completed_orders = 3,
                revenue = 60
            }));
    Check(
        complete.GetProperty("completed").GetBoolean() &&
            complete.GetProperty("opened_next_level_id").GetInt32() == secondLevel,
        "Completion unlocks the next level"
    );
    Failed(await new ItemController(db).Purchase(new()
            {
                save_id = existingSaveId,
                furniture_id = milk.id,
                quantity = 1
            }));
    var progress = complete.GetProperty("progress");
    Check(
        !progress.TryGetProperty("completed_at", out _) && !progress.TryGetProperty("attempts_count", out _),
        "Progress contract is clean"
    );
    Ok(await items.GetAvailableItems(saveId));
    Ok(await items.Purchase(new()
            {
                save_id = saveId,
                furniture_id = milk.id,
                quantity = 1
            }));
    Ok(await items.Purchase(new()
            {
                save_id = saveId,
                furniture_id = stove.id,
                quantity = 1
            }));
    var purchase = Ok(await items.Purchase(new()
            {
                save_id = saveId,
                furniture_id = cup.id,
                quantity = 2
            }));
    Check(purchase.GetProperty("inventory_quantity").GetInt32() == 2, "Inventory quantity remains supported");
    var drinks = Ok(await new DrinkController(db).GetDrinks());
    Check(
        drinks.GetProperty("drinks").EnumerateArray().SelectMany(x => x.GetProperty("ingredients").EnumerateArray())
            .All(x => !x.TryGetProperty("quantity", out _)),
        "Recipes contain ingredient identities without quantities"
    );
    Ok(await new DrinkController(db).GetAvailableDrinks(saveId));
    var placement = new PlacedItemController(db);
    var layout = new SavePlacementRequest
    {
        save_id = saveId,
        items = new()
        {
            new()
            {
                client_id = 1,
                furniture_id = stove.id,
                position_x = 1,
                position_y = 2
            },
            new()
            {
                client_id = 2,
                furniture_id = cup.id,
                position_x = 1,
                position_y = 2,
                parent_client_id = 1
            }
        }
    };
    Ok(await placement.SavePlacement(layout));
    db.ChangeTracker.Clear();
    var placed = Ok(await placement.GetPlacedItems(saveId)).GetProperty("items");
    Check(
        placed.EnumerateArray().All(x => !x.TryGetProperty("rotation", out _) &&
                !x.TryGetProperty("placement_layer", out _) &&
                x.GetProperty("item").TryGetProperty("placement_layer", out _)),
        "Placed items derive the layer from furniture"
    );
    Check(
        placed[1].GetProperty("parent_placed_item_id").GetInt32() == placed[0].GetProperty("id").GetInt32(),
        "Decoration parent is preserved"
    );
    var roomStats = Ok(await saves.GetRoomStats(saveId));
    Check(
        roomStats.GetProperty("coins").GetInt32() == await db.Saveses.AsNoTracking().Where(x => x.id == saveId).Select(x => x.coins).SingleAsync() &&
            roomStats.GetProperty("comfort").GetInt32() == stove.comfort + cup.comfort,
        "Room stats return the saved wallet and comfort of placed furniture including children"
    );
    Check(
        placed[0].GetProperty("item").GetProperty("comfort").GetInt32() == stove.comfort &&
            placed[1].GetProperty("item").GetProperty("comfort").GetInt32() == cup.comfort,
        "Loaded layout includes database comfort values for local editing"
    );
    Ok(await placement.SavePlacement(layout));
    db.ChangeTracker.Clear();
    foreach (var category in new[]
    {
        "technic",
        "TECHNIC"
    }

    )
    {
        await db.Furnitures.Where(x => x.id == cup.id).ExecuteUpdateAsync(x => x.SetProperty(f => f.category, category));
        Ok(await placement.SavePlacement(layout));
        db.ChangeTracker.Clear();
        var equipmentLayout = Ok(await placement.GetPlacedItems(saveId)).GetProperty("items");
        var equipment = equipmentLayout.EnumerateArray().Single(x => x.GetProperty("furniture_id").GetInt32() == cup.id);
        var support = equipmentLayout.EnumerateArray().Single(x => x.GetProperty("furniture_id").GetInt32() == stove.id);
        Check(
            equipment.GetProperty("item").GetProperty("category").GetString() == category &&
                equipment.GetProperty("parent_placed_item_id").GetInt32() == support.GetProperty("id").GetInt32(),
            "Equipment saves on furniture without changing category: " + category
        );
    }

    await db.Furnitures.Where(x => x.id == cup.id).ExecuteUpdateAsync(x => x.SetProperty(f => f.category, "ingredient_crate"));
    Failed(await placement.SavePlacement(layout));
    await db.Furnitures.Where(x => x.id == cup.id).ExecuteUpdateAsync(x => x.SetProperty(f => f.category, cup.category));
    var inventory = new InventoryController(db);
    Ok(await inventory.ChangeQuantity(new()
            {
                save_id = saveId,
                furniture_id = cup.id,
                quantity_change = -1
            }));
    Failed(await inventory.ChangeQuantity(new()
            {
                save_id = saveId,
                furniture_id = cup.id,
                quantity_change = -1
            }));
    Failed(await inventory.ChangeQuantity(new()
            {
                save_id = saveId,
                furniture_id = cup.id,
                quantity_change = -2
            }));
    Ok(await inventory.GetInventory(saveId));
    Ok(await saves.GetFullSave(saveId));
    Ok(await placement.SavePlacement(new()
            {
                save_id = saveId,
                items = new()
            }));
    Check(
        Ok(await saves.GetRoomStats(saveId)).GetProperty("comfort").GetInt32() == 0,
        "Unplaced inventory does not add room comfort"
    );
    db.ChangeTracker.Clear();
    Ok(await inventory.ChangeQuantity(new()
            {
                save_id = saveId,
                furniture_id = cup.id,
                quantity_change = -1
            }));
    Check(
        !await db.Inventories.AnyAsync(x => x.save_id == saveId && x.furniture_id == cup.id),
        "Unplaced inventory can be consumed to zero"
    );
    var floorId = await db.Furnitures.Where(x => x.asset_name == "wood_floor").Select(x => x.id).SingleAsync();
    var wallpaperId = await db.Furnitures.Where(x => x.asset_name == "night_wallpaper").Select(x => x.id).SingleAsync();
    Ok(await items.Purchase(new()
            {
                save_id = saveId,
                furniture_id = floorId,
                quantity = 1
            }));
    Ok(await items.Purchase(new()
            {
                save_id = saveId,
                furniture_id = wallpaperId,
                quantity = 1
            }));
    Ok(await saves.UpdateCoverings(
            saveId,
            new()
            {
                floor_id = floorId,
                wallpaper_id = wallpaperId
            }
        ));
    await SqlFile(db, Path.Combine(root, "Scripts/Seed/05_room_coverings.sql"));
    await SqlFile(db, Path.Combine(root, "Scripts/Seed/05_room_coverings.sql"));
    await CoveringChecks.Run(() => CreateContext(cs.ConnectionString, schema));
    Failed(await inventory.ChangeQuantity(new()
            {
                save_id = saveId,
                furniture_id = floorId,
                quantity_change = -1
            }));
    var counts = await Counts(db);
    await db.GetService<IMigrator>().MigrateAsync(previous);
    await db.Database.MigrateAsync();
    Check(counts == await Counts(db), "Migration rollback and reapply preserve rows");
    Console.WriteLine("All integration checks passed in an isolated PostgreSQL schema.");
}
finally
{
    await live.Database.ExecuteSqlRawAsync($"DROP SCHEMA \"{schema}\" CASCADE");
}

static void Check(bool value, string message)
{
    if (!value)
    {
        throw new InvalidOperationException(message);
    }

    Console.WriteLine("PASS: " + message);
}

static JsonElement Result(IActionResult result) => JsonSerializer.SerializeToElement(((OkObjectResult)result).Value);
static JsonElement Ok(IActionResult result)
{
    var json = Result(result);
    if (!json.GetProperty("status").GetBoolean())
    {
        throw new InvalidOperationException(json.ToString());
    }

    return json;
}

static void Failed(IActionResult result) => Check(!Result(result).GetProperty("status").GetBoolean(), "Invalid operation rejected");
static async Task SqlFile(ContextDb db, string path) => await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(path));
static async Task<string> Counts(ContextDb db)
{
    var result = new List<string>();
    foreach (var table in new[]
    {
        "Users",
        "Saveses",
        "LevelProgresses",
        "Levels",
        "Inventories",
        "PlacedItems",
        "Furnitures",
        "Recipes",
        "Drinks",
        "Ingredients",
        "Characters"
    }

    )
    {
        result.Add(table + ":" + await db.Database.SqlQueryRaw<int>($"SELECT count(*)::int AS \"Value\" FROM \"{table}\"").SingleAsync());
    }

    return string.Join(",", result);
}

static async Task CheckColumns(ContextDb db)
{
    var forbidden = await db.Database.SqlQueryRaw<int>("""
        SELECT count(*)::int AS "Value" FROM information_schema.columns
        WHERE table_schema = current_schema() AND (
            (table_name = 'LevelProgresses' AND column_name IN ('attempts_count', 'completed_at')) OR
            (table_name = 'Recipes' AND column_name = 'quantity') OR
            (table_name = 'Furnitures' AND column_name IN ('is_consumable', 'is_available')) OR
            (table_name = 'PlacedItems' AND column_name IN ('rotation', 'placement_layer')))
        """)
        .SingleAsync();
    Check(forbidden == 0, "Removed columns are absent from the database");
}
