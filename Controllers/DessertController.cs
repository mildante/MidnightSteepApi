using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Data;
using MidnightSteepApi.Dtos;
using MidnightSteepApi.Models;

namespace MidnightSteepApi.Controllers;

public class DessertController
{
    private readonly ContextDb db;
    public DessertController(ContextDb context)
    {
        db = context;
    }

    [HttpGet, Route("desserts")]
    public async Task<IActionResult> GetDesserts()
    {
        var desserts = await db.Desserts.AsNoTracking().OrderBy(x => x.id).ToListAsync();
        return new OkObjectResult(new
            {
                status = true,
                desserts
            });
    }

    [HttpPost, Route("desserts/purchase")]
    public async Task<IActionResult> Purchase([FromBody] DessertPurchaseRequest? request)
    {
        if (request == null ||
            request.save_id <= 0 ||
            request.dessert_id <= 0 ||
            request.purchase_id == Guid.Empty)
        {
            return Error("Некорректный запрос покупки сладостей");
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var save = await db.Saveses.FromSqlInterpolated($"SELECT * FROM \"Saveses\" WHERE id = {request.save_id} FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync();
        if (save == null)
        {
            return Error("Сохранение не найдено");
        }

        var receipt = await db.DessertPurchases.AsNoTracking().SingleOrDefaultAsync(x => x.purchase_id == request.purchase_id);
        if (receipt != null && (receipt.save_id != request.save_id || receipt.dessert_id != request.dessert_id))
        {
            return Error("Эта квитанция относится к другой покупке");
        }

        var dessert = await db.Desserts.AsNoTracking().SingleOrDefaultAsync(x => x.id == request.dessert_id);
        if (dessert == null)
        {
            return Error("Сладость не найдена");
        }

        if (receipt == null)
        {
            if (dessert.batch_size != 3 || dessert.batch_price < 0 || dessert.sale_price < 0)
            {
                return Error("Некорректная цена или размер подноса");
            }

            if (save.coins < dessert.batch_price)
            {
                return Error("Недостаточно монет");
            }

            await db.Saveses.Where(x => x.id == save.id).ExecuteUpdateAsync(update => update.SetProperty(x => x.coins, x => x.coins - dessert.batch_price).SetProperty(x => x.updatedAt, DateTime.UtcNow));
            save.coins -= dessert.batch_price;
            receipt = new DessertPurchase
            {
                purchase_id = request.purchase_id,
                save_id = save.id,
                dessert_id = dessert.id,
                quantity = dessert.batch_size,
                spent_coins = dessert.batch_price,
                sale_price = dessert.sale_price
            };
            db.DessertPurchases.Add(receipt);
            await db.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        return new OkObjectResult(new
            {
                status = true,
                receipt.purchase_id,
                receipt.save_id,
                receipt.dessert_id,
                receipt.quantity,
                receipt.spent_coins,
                receipt.sale_price,
                coins = save.coins,
                dessert.asset_name
            });
    }

    private static OkObjectResult Error(string error)
    {
        return new(new
            {
                status = false,
                error
            });
    }
}
