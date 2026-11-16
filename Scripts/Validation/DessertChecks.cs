using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Controllers;
using MidnightSteepApi.Data;
using MidnightSteepApi.Dtos;

internal static class DessertChecks
{
    public static async Task Run(ContextDb db, Func<ContextDb> createContext, int saveId)
    {
        var api = new DessertController(db);
        var catalog = Json(await api.GetDesserts());
        Check(catalog.GetProperty("desserts").GetArrayLength() == 4, "Four desserts are seeded");
        var cookie = await db.Desserts.SingleAsync(x => x.asset_name == "dessert.cookie");
        var request = new DessertPurchaseRequest
        {
            save_id = saveId,
            dessert_id = cookie.id,
            purchase_id = Guid.NewGuid()
        };
        Check(!Json(await api.Purchase(request)).GetProperty("status").GetBoolean(), "Insufficient funds rejected");
        Check(!await db.DessertPurchases.AnyAsync(), "Rejected purchase creates no receipt");
        await db.Saveses.Where(x => x.id == saveId).ExecuteUpdateAsync(x => x.SetProperty(s => s.coins, 20));
        var bought = Json(await api.Purchase(request));
        Check(
            bought.GetProperty("status").GetBoolean() &&
                bought.GetProperty("quantity").GetInt32() == 3 &&
                bought.GetProperty("coins").GetInt32() == 10 &&
                bought.GetProperty("sale_price").GetInt32() == 6,
            "Ten coins buys three desserts with six-coin sale price"
        );
        var replay = Json(await api.Purchase(request));
        Check(
            replay.GetProperty("coins").GetInt32() == 10 && await db.DessertPurchases.CountAsync() == 1,
            "Retrying the same purchase does not charge twice"
        );
        var wrong = new DessertPurchaseRequest
        {
            save_id = saveId,
            dessert_id = cookie.id + 1,
            purchase_id = request.purchase_id
        };
        Check(
            !Json(await api.Purchase(wrong)).GetProperty("status").GetBoolean(),
            "Receipt cannot purchase another dessert"
        );
        var empty = new DessertPurchaseRequest
        {
            save_id = saveId,
            dessert_id = cookie.id
        };
        Check(
            !Json(await api.Purchase(empty)).GetProperty("status").GetBoolean(),
            "Missing receipt identity rejected"
        );
        async Task<JsonElement> Buy(Guid purchaseId)
        {
            await using var other = createContext();
            return Json(await new DessertController(other).Purchase(new()
                    {
                        save_id = saveId,
                        dessert_id = cookie.id,
                        purchase_id = purchaseId
                    }));
        }

        var concurrent = await Task.WhenAll(Buy(Guid.NewGuid()), Buy(Guid.NewGuid()));
        Check(
            concurrent.Count(x => x.GetProperty("status").GetBoolean()) == 1 &&
                await db.Saveses.AsNoTracking().Where(x => x.id == saveId).Select(x => x.coins).SingleAsync() == 0,
            "Concurrent purchases cannot overdraw the wallet"
        );
        var retryId = Guid.NewGuid();
        await db.Saveses.Where(x => x.id == saveId).ExecuteUpdateAsync(x => x.SetProperty(s => s.coins, 10));
        concurrent = await Task.WhenAll(Buy(retryId), Buy(retryId));
        Check(
            concurrent.All(x => x.GetProperty("status").GetBoolean()) &&
                await db.DessertPurchases.CountAsync(x => x.purchase_id == retryId) == 1,
            "Concurrent retry returns one paid batch"
        );
        var furniture = new MidnightSteepApi.Models.Furniture
        {
            name = "Purchase concurrency check",
            asset_name = "dessert_purchase_check",
            price = 7,
            category = "decor",
            placement_layer = "Table",
            max_stack = 10
        };
        db.Furnitures.Add(furniture);
        await db.SaveChangesAsync();
        await db.Saveses.Where(x => x.id == saveId).ExecuteUpdateAsync(x => x.SetProperty(s => s.coins, 20));
        async Task BuyFurniture()
        {
            await using var other = createContext();
            Check(
                Json(await new ItemController(other).Purchase(new()
                        {
                            save_id = saveId,
                            furniture_id = furniture.id,
                            quantity = 1
                        }))
                    .GetProperty("status")
                    .GetBoolean(),
                "Furniture purchase succeeds alongside dessert purchase"
            );
        }

        await Task.WhenAll(Buy(Guid.NewGuid()), BuyFurniture());
        Check(
            await db.Saveses.AsNoTracking().Where(x => x.id == saveId).Select(x => x.coins).SingleAsync() == 3,
            "Dessert and furniture purchases preserve both wallet debits"
        );
        await db.Saveses.Where(x => x.id == saveId).ExecuteUpdateAsync(x => x.SetProperty(s => s.coins, 0));
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
