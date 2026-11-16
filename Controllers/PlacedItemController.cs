using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Data;
using MidnightSteepApi.Dtos;
using MidnightSteepApi.Models;

namespace MidnightSteepApi.Controllers
{
    public class PlacedItemController
    {
        private readonly ContextDb _ContextDb;
        public PlacedItemController(ContextDb context)
        {
            _ContextDb = context;
        }

        [HttpGet]
        [Route("placed-items/{save_id:int}")]
        public async Task<IActionResult> GetPlacedItems(int save_id)
        {
            if (save_id <= 0)
            {
                return Error("Некорректный идентификатор сохранения");
            }

            if (!await _ContextDb.Saveses.AnyAsync(x => x.id == save_id))
            {
                return Error("Сохранение не найдено");
            }

            var items = await _ContextDb.PlacedItems.AsNoTracking().Where(x => x.save_id == save_id).OrderBy(x => x.id).Select(x => new
                {
                    x.id,
                    x.furniture_id,
                    x.position_x,
                    x.position_y,
                    x.parent_placed_item_id,
                    item = new
                    {
                        x.furniture.id,
                        x.furniture.name,
                        x.furniture.comfort,
                        x.furniture.price,
                        x.furniture.category,
                        x.furniture.asset_name,
                        x.furniture.placement_layer
                    }
                })
                .ToListAsync();
            return new OkObjectResult(new
                {
                    status = true,
                    items
                });
        }

        [HttpPut]
        [Route("placed-items")]
        public async Task<IActionResult> SavePlacement([FromBody] SavePlacementRequest? request)
        {
            if (request == null)
            {
                return Error("Тело запроса не передано");
            }

            if (request.save_id <= 0)
            {
                return Error("Некорректный идентификатор сохранения");
            }

            if (request.items == null)
            {
                return Error("Список размещённых предметов не передан");
            }

            if (!await _ContextDb.Saveses.AnyAsync(x => x.id == request.save_id))
            {
                return Error("Сохранение не найдено");
            }

            if (request.items.Any(x => x.client_id <= 0 || x.furniture_id <= 0))
            {
                return Error("client_id и furniture_id должны быть положительными");
            }

            if (request.items.Select(x => x.client_id).Distinct().Count() != request.items.Count)
            {
                return Error("client_id размещённых предметов должны быть уникальными в запросе");
            }

            if (request.items.Any(x => !float.IsFinite(x.position_x) || !float.IsFinite(x.position_y)))
            {
                return Error("Координаты должны быть конечными числами");
            }

            var requestedQuantities = request.items.GroupBy(x => x.furniture_id).ToDictionary(group => group.Key, group => group.Count());
            var inventory = await _ContextDb.Inventories.AsNoTracking().Where(x => x.save_id == request.save_id && requestedQuantities.Keys.Contains(x.furniture_id))
                .ToDictionaryAsync(x => x.furniture_id, x => x.quantity);
            foreach (var requested in requestedQuantities)
            {
                if (!inventory.TryGetValue(requested.Key, out var ownedQuantity))
                {
                    return Error($"Предмет {requested.Key} отсутствует в инвентаре сохранения");
                }

                if (requested.Value > ownedQuantity)
                {
                    return Error($"Размещённое количество предмета {requested.Key} превышает количество в инвентаре");
                }
            }

            var furnitureIds = requestedQuantities.Keys.ToList();
            var furniture = await _ContextDb.Furnitures.AsNoTracking().Where(x => furnitureIds.Contains(x.id)).ToDictionaryAsync(x => x.id);
            if (furniture.Count != furnitureIds.Count)
            {
                return Error("Один или несколько размещаемых предметов не найдены");
            }

            var requestsByClientId = request.items.ToDictionary(x => x.client_id);
            foreach (var itemRequest in request.items)
            {
                if (!itemRequest.parent_client_id.HasValue)
                {
                    continue;
                }

                if (!requestsByClientId.TryGetValue(itemRequest.parent_client_id.Value, out var parentRequest))
                {
                    return Error($"Родитель {itemRequest.parent_client_id.Value} отсутствует в расстановке");
                }

                if (parentRequest.client_id == itemRequest.client_id)
                {
                    return Error("Предмет не может быть родителем самому себе");
                }

                var category = furniture[itemRequest.furniture_id].category;
                if (!string.Equals(category, "decor", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(category, "decoration", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(category, "technic", StringComparison.OrdinalIgnoreCase))
                {
                    return Error("Только декор и оборудование могут быть размещены на другом предмете");
                }

                if (!string.Equals(furniture[parentRequest.furniture_id].category, "furniture", StringComparison.OrdinalIgnoreCase))
                {
                    return Error("Декор и оборудование можно размещать только на предмете категории furniture");
                }

                if (parentRequest.parent_client_id.HasValue)
                {
                    return Error("Родительский предмет не может сам находиться на другом предмете");
                }
            }

            await using var transaction = await _ContextDb.Database.BeginTransactionAsync();
            try
            {
                var oldItems = await _ContextDb.PlacedItems.Where(x => x.save_id == request.save_id).ToListAsync();
                _ContextDb.PlacedItems.RemoveRange(oldItems);
                await _ContextDb.SaveChangesAsync();
                var entities = request.items.Select(itemRequest => new PlacedItem
                    {
                        save_id = request.save_id,
                        furniture_id = itemRequest.furniture_id,
                        position_x = itemRequest.position_x,
                        position_y = itemRequest.position_y
                    })
                    .ToList();
                await _ContextDb.PlacedItems.AddRangeAsync(entities);
                await _ContextDb.SaveChangesAsync();
                var entitiesByClientId = request.items.Select((itemRequest, index) => new
                    {
                        itemRequest.client_id,
                        entity = entities[index]
                    })
                    .ToDictionary(x => x.client_id, x => x.entity);
                foreach (var itemRequest in request.items.Where(x => x.parent_client_id.HasValue))
                {
                    entitiesByClientId[itemRequest.client_id].parent_placed_item_id = entitiesByClientId[itemRequest.parent_client_id!.Value].id;
                }

                await _ContextDb.SaveChangesAsync();
                await transaction.CommitAsync();
                var items = request.items.Select((itemRequest, index) => new
                    {
                        client_id = itemRequest.client_id,
                        id = entities[index].id,
                        entities[index].furniture_id,
                        entities[index].position_x,
                        entities[index].position_y,
                        entities[index].parent_placed_item_id,
                        item = new
                        {
                            furniture[entities[index].furniture_id].id,
                            furniture[entities[index].furniture_id].name,
                            furniture[entities[index].furniture_id].comfort,
                            furniture[entities[index].furniture_id].price,
                            furniture[entities[index].furniture_id].category,
                            furniture[entities[index].furniture_id].asset_name,
                            furniture[entities[index].furniture_id].placement_layer
                        }
                    });
                return new OkObjectResult(new
                    {
                        status = true,
                        items
                    });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return Error("Не удалось сохранить расстановку");
            }
        }

        [HttpDelete]
        [Route("placed-items/{save_id:int}/{placed_item_id:int}")]
        public async Task<IActionResult> DeletePlacedItem(int save_id, int placed_item_id)
        {
            if (save_id <= 0 || placed_item_id <= 0)
            {
                return Error("Некорректный идентификатор сохранения или размещённого предмета");
            }

            if (!await _ContextDb.Saveses.AnyAsync(x => x.id == save_id))
            {
                return Error("Сохранение не найдено");
            }

            var item = await _ContextDb.PlacedItems.FirstOrDefaultAsync(x => x.id == placed_item_id && x.save_id == save_id);
            if (item == null)
            {
                return Error("Размещённый предмет не найден в этом сохранении");
            }

            if (await _ContextDb.PlacedItems.AnyAsync(x => x.parent_placed_item_id == placed_item_id))
            {
                return Error("Сначала удалите предметы, размещённые на этом предмете");
            }

            _ContextDb.PlacedItems.Remove(item);
            await _ContextDb.SaveChangesAsync();
            return new OkObjectResult(new
                {
                    status = true,
                    deleted_id = placed_item_id
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
