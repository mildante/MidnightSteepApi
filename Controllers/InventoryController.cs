using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Data;
using MidnightSteepApi.Dtos;

namespace MidnightSteepApi.Controllers
{
    public class InventoryController
    {
        private readonly ContextDb _ContextDb;
        public InventoryController(ContextDb context)
        {
            _ContextDb = context;
        }

        [HttpGet]
        [Route("inventory/{save_id:int}")]
        public async Task<IActionResult> GetInventory(int save_id)
        {
            if (save_id <= 0)
            {
                return Error("Некорректный идентификатор сохранения");
            }

            if (!await _ContextDb.Saveses.AnyAsync(x => x.id == save_id))
            {
                return Error("Сохранение не найдено");
            }

            var inventory = await _ContextDb.Inventories.AsNoTracking().Where(x => x.save_id == save_id).OrderBy(x => x.furniture_id)
                .Select(x => new
                {
                    x.furniture_id,
                    x.quantity,
                    item = new
                    {
                        x.furniture.id,
                        x.furniture.name,
                        x.furniture.price,
                        x.furniture.category,
                        x.furniture.max_stack,
                        x.furniture.comfort,
                        x.furniture.unlocked_level_id,
                        x.furniture.asset_name,
                        x.furniture.placement_layer
                    }
                })
                .ToListAsync();
            return new OkObjectResult(new
                {
                    status = true,
                    inventory
                });
        }

        [HttpPut]
        [Route("inventory/quantity")]
        public async Task<IActionResult> ChangeQuantity([FromBody] ChangeInventoryQuantityRequest? request)
        {
            if (request == null)
            {
                return Error("Тело запроса не передано");
            }

            if (request.save_id <= 0 || request.furniture_id <= 0)
            {
                return Error("Некорректный идентификатор сохранения или предмета");
            }

            if (request.quantity_change >= 0)
            {
                return Error("Через этот метод можно только расходовать предметы; используйте отрицательное quantity_change");
            }

            if (!await _ContextDb.Saveses.AnyAsync(x => x.id == request.save_id))
            {
                return Error("Сохранение не найдено");
            }

            var inventory = await _ContextDb.Inventories.FirstOrDefaultAsync(x => x.save_id == request.save_id && x.furniture_id == request.furniture_id);
            if (inventory == null)
            {
                return Error("Предмет отсутствует в инвентаре");
            }

            int newQuantity;
            try
            {
                newQuantity = checked(inventory.quantity + request.quantity_change);
            }
            catch (OverflowException)
            {
                return Error("Некорректное изменение количества");
            }

            if (newQuantity < 0)
            {
                return Error("Количество предмета не может быть отрицательным");
            }

            var placedQuantity = await _ContextDb.PlacedItems.CountAsync(x => x.save_id == request.save_id && x.furniture_id == request.furniture_id);
            if (newQuantity < placedQuantity)
            {
                return Error("Сначала уберите размещённые экземпляры этого предмета");
            }

            var save = await _ContextDb.Saveses.FirstAsync(x => x.id == request.save_id);
            if (newQuantity == 0 &&
                (save.floor_id == request.furniture_id ||
                save.wallpaper_id == request.furniture_id))
            {
                return Error("Нельзя израсходовать используемое покрытие комнаты");
            }

            if (newQuantity == 0)
            {
                _ContextDb.Inventories.Remove(inventory);
            }
            else
            {
                inventory.quantity = newQuantity;
            }

            await _ContextDb.SaveChangesAsync();
            return new OkObjectResult(new
                {
                    status = true,
                    request.save_id,
                    request.furniture_id,
                    quantity = newQuantity,
                    removed = newQuantity == 0
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
    }
}
