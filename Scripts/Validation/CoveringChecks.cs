using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Controllers;
using MidnightSteepApi.Data;
using MidnightSteepApi.Dtos;
using MidnightSteepApi.Models;

internal static class CoveringChecks
{
    public static async Task Run(Func<ContextDb> context)
    {
        await using var db = context();
        var user = new User
        {
            login = "covering-check",
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
        int id = created.GetProperty("save").GetProperty("id").GetInt32();
        var save = await db.Saveses.SingleAsync(x => x.id == id);
        int floor = await db.Furnitures.Where(x => x.asset_name == "wood_floor").Select(x => x.id).SingleAsync();
        int wall = await db.Furnitures.Where(x => x.asset_name == "night_wallpaper").Select(x => x.id).SingleAsync();
        save.floor_id = floor;
        save.wallpaper_id = wall;
        await db.SaveChangesAsync();
        int nextFloor = await db.Furnitures.Where(x => x.asset_name == "DarkWoodFloor").Select(x => x.id).SingleAsync();
        int nextWall = await db.Furnitures.Where(x => x.asset_name == "LightBueWallpaper").Select(x => x.id).SingleAsync();
        async Task<JsonElement> Apply(UpdateCoveringsRequest? request)
        {
            await using var call = context();
            return Json(await new SaveController(call).UpdateCoverings(id, request));
        }

        Check(
            !(await Apply(null)).GetProperty("status").GetBoolean() &&
                !(await Apply(new())).GetProperty("status").GetBoolean() &&
                !(await Apply(new()
                    {
                        floor_id = 0
                    })).GetProperty("status").GetBoolean(),
            "Coverings reject missing, empty and invalid requests"
        );
        Check(
            !(await Apply(new()
                {
                    floor_id = nextFloor
                })).GetProperty("status").GetBoolean(),
            "Unowned floor cannot be applied"
        );
        Check(
            !(await Apply(new()
                {
                    floor_id = wall
                })).GetProperty("status").GetBoolean(),
            "Wallpaper cannot be used as a floor"
        );
        Check(
            Json(await new ItemController(db).Purchase(new()
                    {
                        save_id = id,
                        furniture_id = nextFloor
                    }))
                .GetProperty("status")
                .GetBoolean(),
            "New test floor is free and available"
        );
        var first = await Apply(new()
            {
                floor_id = nextFloor
            });
        Check(
            first.GetProperty("status").GetBoolean() &&
                first.GetProperty("floor_id").GetInt32() == nextFloor &&
                first.GetProperty("wallpaper_id").GetInt32() == wall,
            "Changing only the floor preserves an unowned default wallpaper"
        );
        Check(
            (await Apply(new()
                {
                    floor_id = nextFloor,
                    wallpaper_id = wall
                })).GetProperty("status")
                .GetBoolean(),
            "Keeping the existing default covering does not require buying it again"
        );
        Check(
            !(await Apply(new()
                {
                    wallpaper_id = nextWall
                })).GetProperty("status").GetBoolean(),
            "Unowned wallpaper cannot be applied"
        );
        Check(
            Json(await new ItemController(db).Purchase(new()
                    {
                        save_id = id,
                        furniture_id = nextWall
                    }))
                .GetProperty("status")
                .GetBoolean(),
            "New test wallpaper is free and available"
        );
        var second = await Apply(new()
            {
                wallpaper_id = nextWall
            });
        Check(
            second.GetProperty("status").GetBoolean() &&
                second.GetProperty("floor_id").GetInt32() == nextFloor &&
                second.GetProperty("wallpaper_id").GetInt32() == nextWall,
            "Changing only wallpaper preserves the selected floor"
        );
        Check(
            await db.Inventories.CountAsync(x => x.save_id == id &&
                    (x.furniture_id == nextFloor ||
                    x.furniture_id == nextWall) &&
                    x.quantity == 1) == 2,
            "Applying coverings never consumes inventory ownership"
        );
        await using var read = context();
        var loaded = Json(await new SaveController(read).GetSaveByUser(user.id)).GetProperty("save");
        Check(
            loaded.GetProperty("floor_id").GetInt32() == nextFloor &&
                loaded.GetProperty("wallpaper_id").GetInt32() == nextWall,
            "Signing in again restores both selected coverings"
        );
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
