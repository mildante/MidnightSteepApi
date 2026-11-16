using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Data;
using MidnightSteepApi.Dtos;
using MidnightSteepApi.Models;

namespace MidnightSteepApi.Controllers
{
    public class ItemController
    {
        private readonly ContextDb _ContextDb;
        public ItemController(ContextDb context)
        {
            _ContextDb = context;
        }

        [HttpGet]
        [Route("items")]
        public async Task<IActionResult> GetItems()
        {
            var items = await ItemQuery().ToListAsync();
            return new OkObjectResult(new
                {
                    status = true,
                    items
                });
        }

        [HttpGet]
        [Route("items/available/{save_id:int}")]
        public async Task<IActionResult> GetAvailableItems(int save_id)
        {
            if (save_id <= 0)
            {
                return Error("Некорректный идентификатор сохранения");
            }

            if (!await _ContextDb.Saveses.AnyAsync(x => x.id == save_id))
            {
                return Error("Сохранение не найдено");
            }

            var items = await ItemQuery().Where(item => item.unlocked_level_id <= 0 ||
                    _ContextDb.LevelProgresses.Any(progress => progress.save_id == save_id &&
                        progress.level_id == item.unlocked_level_id &&
                        progress.is_unlocked))
                .ToListAsync();
            return new OkObjectResult(new
                {
                    status = true,
                    items
                });
        }

        [HttpPost]
        [Route("shop/purchase")]
        public async Task<IActionResult> Purchase([FromBody] PurchaseRequest? request)
        {
            if (request == null)
            {
                return Error("Тело запроса не передано");
            }

            if (request.save_id <= 0 || request.furniture_id <= 0 || request.quantity <= 0)
            {
                return Error("Некорректный идентификатор сохранения, товара или количество");
            }

            await using var transaction = await _ContextDb.Database.BeginTransactionAsync();
            var save = await _ContextDb.Saveses.FromSqlInterpolated($"SELECT * FROM \"Saveses\" WHERE id = {request.save_id} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (save == null)
            {
                return Error("Сохранение не найдено");
            }

            var item = await _ContextDb.Furnitures.FirstOrDefaultAsync(x => x.id == request.furniture_id);
            if (item == null)
            {
                return Error("Товар не найден");
            }

            if (string.Equals(item.category, "ingredient_crate", StringComparison.OrdinalIgnoreCase))
            {
                return Error("Полки с ингредиентами нельзя купить в магазине");
            }

            if (item.price < 0)
            {
                return Error("У товара указана некорректная цена");
            }

            if (item.unlocked_level_id > 0)
            {
                var unlocked = await _ContextDb.LevelProgresses.AnyAsync(x => x.save_id == request.save_id && x.level_id == item.unlocked_level_id && x.is_unlocked);
                if (!unlocked)
                {
                    return Error("Товар ещё не открыт");
                }
            }

            int totalPrice;
            try
            {
                totalPrice = checked(item.price * request.quantity);
            }
            catch (OverflowException)
            {
                return Error("Итоговая стоимость слишком велика");
            }

            if (save.coins < totalPrice)
            {
                return Error("Недостаточно монет");
            }

            var inventory = await _ContextDb.Inventories.FirstOrDefaultAsync(x => x.save_id == request.save_id && x.furniture_id == request.furniture_id);
            var currentQuantity = inventory?.quantity ?? 0;
            int newQuantity;
            try
            {
                newQuantity = checked(currentQuantity + request.quantity);
            }
            catch (OverflowException)
            {
                return Error("Количество предметов слишком велико");
            }

            if (item.max_stack > 0 && newQuantity > item.max_stack)
            {
                return Error($"Нельзя хранить больше {item.max_stack} единиц этого предмета");
            }

            save.coins -= totalPrice;
            save.updatedAt = DateTime.UtcNow;
            if (inventory == null)
            {
                inventory = new Inventory
                {
                    save_id = request.save_id,
                    furniture_id = request.furniture_id,
                    quantity = request.quantity
                };
                await _ContextDb.Inventories.AddAsync(inventory);
            }
            else
            {
                inventory.quantity = newQuantity;
            }

            await _ContextDb.SaveChangesAsync();
            await transaction.CommitAsync();
            return new OkObjectResult(new
                {
                    status = true,
                    save_id = save.id,
                    furniture_id = item.id,
                    purchased_quantity = request.quantity,
                    spent_coins = totalPrice,
                    coins = save.coins,
                    inventory_quantity = inventory.quantity
                });
        }

        private IQueryable<ItemResponse> ItemQuery()
        {
            return _ContextDb.Furnitures.AsNoTracking().Where(x => x.category.ToLower() != "ingredient_crate").OrderBy(x => x.id)
                .Select(x => new ItemResponse
                {
                    id = x.id,
                    name = x.name,
                    price = x.price,
                    category = x.category,
                    max_stack = x.max_stack,
                    comfort = x.comfort,
                    unlocked_level_id = x.unlocked_level_id,
                    asset_name = x.asset_name,
                    placement_layer = x.placement_layer
                });
        }

        private static OkObjectResult Error(string message)
        {
            return new OkObjectResult(new
                {
                    status = false,
                    error = message
                });
        }

        private sealed class ItemResponse
        {
            public int id { get; set; }
            public string name { get; set; } = string.Empty;
            public int price { get; set; }
            public string category { get; set; } = string.Empty;
            public int max_stack { get; set; }
            public int comfort { get; set; }
            public int unlocked_level_id { get; set; }
            public string asset_name { get; set; } = string.Empty;
            public string placement_layer { get; set; } = string.Empty;
        }
    }
}
