using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Data;
using MidnightSteepApi.Dtos;
using MidnightSteepApi.Models;

namespace MidnightSteepApi.Controllers
{
    public class SaveController
    {
        private readonly ContextDb _ContextDb;
        private static readonly (string AssetName, int Quantity)[] StarterItems =
        {
            ("PinkLamp", 1),
            ("Table", 1),
            ("RedChair", 1),
            ("HoneyKettle", 1),
            ("CrateTeaLeaves", 1),
            ("BarCounter", 1),
            ("HoneyTrashBin", 1),
            ("BlueWallpaper", 1),
            ("WoodFloor", 1)
        };
        public SaveController(ContextDb context)
        {
            _ContextDb = context;
        }

        [HttpGet]
        [Route("save/{save_id:int}/stats")]
        public async Task<IActionResult> GetRoomStats(int save_id)
        {
            var save = await _ContextDb.Saveses.AsNoTracking().Where(x => x.id == save_id).Select(x => new
                {
                    x.id,
                    x.coins
                })
                .SingleOrDefaultAsync();
            if (save == null)
            {
                return new OkObjectResult(new
                    {
                        status = false,
                        error = "Сохранение не найдено"
                    });
            }

            var comfort = await _ContextDb.PlacedItems.Where(x => x.save_id == save_id).SumAsync(x => (int?)x.furniture.comfort) ?? 0;
            return new OkObjectResult(new
                {
                    status = true,
                    save_id = save.id,
                    save.coins,
                    comfort
                });
        }

        [HttpGet]
        [Route("save/by-user/{user_id:int}")]
        public async Task<IActionResult> GetSaveByUser(int user_id)
        {
            if (user_id <= 0)
            {
                return Error("Некорректный идентификатор пользователя");
            }

            if (!await _ContextDb.Users.AnyAsync(x => x.id == user_id))
            {
                return Error("Пользователь не найден");
            }

            var save = await _ContextDb.Saveses.AsNoTracking().Where(x => x.user_id == user_id).Select(x => new
                {
                    x.id,
                    x.user_id,
                    x.character_id,
                    character = new
                    {
                        x.character.id,
                        x.character.name
                    },
                    x.coins,
                    x.floor_id,
                    floor = x.floor == null ? null : new
                        {
                            x.floor.id,
                            x.floor.name,
                            x.floor.asset_name
                        },
                    x.wallpaper_id,
                    wallpaper = x.wallpaper == null ? null : new
                        {
                            x.wallpaper.id,
                            x.wallpaper.name,
                            x.wallpaper.asset_name
                        },
                    x.createdAt,
                    x.updatedAt
                })
                .FirstOrDefaultAsync();
            if (save == null)
            {
                return new OkObjectResult(new
                    {
                        status = false,
                        code = "save_not_found",
                        error = "Сохранение пользователя не найдено"
                    });
            }

            return new OkObjectResult(new
                {
                    status = true,
                    save
                });
        }

        [HttpPost]
        [Route("save")]
        public Task<IActionResult> CreateSave([FromBody] CreateSaveRequest? request)
        {
            return CreateSaveCore(request);
        }

        [HttpPost]
        [Route("save/{save_id:int}/reset")]
        public async Task<IActionResult> ResetSave(int save_id, [FromBody] ResetSaveRequest? request)
        {
            if (request == null)
            {
                return Error("Тело запроса не передано");
            }

            if (save_id <= 0 || request.user_id <= 0)
            {
                return Error("Некорректный идентификатор сохранения или пользователя");
            }

            if (!await _ContextDb.Users.AnyAsync(x => x.id == request.user_id))
            {
                return Error("Пользователь не найден");
            }

            await using var transaction = await _ContextDb.Database.BeginTransactionAsync();
            var save = await _ContextDb.Saveses.FromSqlInterpolated($"SELECT * FROM \"Saveses\" WHERE id = {save_id} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (save != null && save.user_id != request.user_id)
            {
                return Error("Сохранение не принадлежит пользователю");
            }

            try
            {
                if (save != null)
                {
                    _ContextDb.Saveses.Remove(save);
                    await _ContextDb.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return new OkObjectResult(new
                    {
                        status = true,
                        request.user_id,
                        save_id
                    });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return Error("Не удалось сбросить прогресс. Предыдущее сохранение сохранено.");
            }
        }

        private async Task<IActionResult> CreateSaveCore(CreateSaveRequest? request)
        {
            if (request == null)
            {
                return Error("Тело запроса не передано");
            }

            if (request.user_id <= 0 || request.character_id <= 0)
            {
                return Error("Некорректный идентификатор пользователя или персонажа");
            }

            if (!await _ContextDb.Users.AnyAsync(x => x.id == request.user_id))
            {
                return Error("Пользователь не найден");
            }

            if (!await _ContextDb.Characters.AnyAsync(x => x.id == request.character_id))
            {
                return Error("Персонаж не найден");
            }

            if (await _ContextDb.Saveses.AnyAsync(x => x.user_id == request.user_id))
            {
                return Error("У пользователя уже есть сохранение");
            }

            var firstLevel = await _ContextDb.Levels.OrderBy(x => x.level_number).ThenBy(x => x.id).FirstOrDefaultAsync();
            if (firstLevel == null)
            {
                return Error("В базе данных нет уровней");
            }

            var starterKeys = StarterItems.Select(x => x.AssetName).ToArray();
            var starterCatalog = await _ContextDb.Furnitures.AsNoTracking().Where(x => starterKeys.Contains(x.asset_name)).ToListAsync();
            foreach (var starter in StarterItems)
            {
                var matches = starterCatalog.Where(x => x.asset_name == starter.AssetName).ToList();
                if (matches.Count != 1)
                {
                    return Error($"В каталоге должен быть ровно один стартовый предмет {starter.AssetName}. Выполните Scripts/Seed/04_starter_furniture.sql");
                }

                if (matches[0].max_stack > 0 && matches[0].max_stack < starter.Quantity)
                {
                    return Error($"Лимит предмета {starter.AssetName} меньше количества в стартовом наборе");
                }
            }

            await using var transaction = await _ContextDb.Database.BeginTransactionAsync();
            try
            {
                var save = new Saves
                {
                    user_id = request.user_id,
                    character_id = request.character_id,
                    coins = 0,
                    createdAt = DateTime.UtcNow,
                    updatedAt = DateTime.UtcNow,
                    wallpaper_id = starterCatalog.Single(x => x.asset_name == "BlueWallpaper").id,
                    floor_id = starterCatalog.Single(x => x.asset_name == "WoodFloor").id
                };
                await _ContextDb.Saveses.AddAsync(save);
                await _ContextDb.SaveChangesAsync();
                await _ContextDb.LevelProgresses.AddAsync(new LevelProgress
                    {
                        save_id = save.id,
                        level_id = firstLevel.id,
                        is_unlocked = true
                    });
                _ContextDb.Inventories.AddRange(StarterItems.Select(starter => new Inventory
                        {
                            save_id = save.id,
                            furniture_id = starterCatalog.Single(x => x.asset_name == starter.AssetName).id,
                            quantity = starter.Quantity
                        }));
                await _ContextDb.SaveChangesAsync();
                await transaction.CommitAsync();
                return new OkObjectResult(new
                    {
                        status = true,
                        save = new
                        {
                            save.id,
                            save.user_id,
                            save.character_id,
                            save.coins,
                            save.floor_id,
                            save.wallpaper_id,
                            save.createdAt,
                            save.updatedAt
                        },
                        opened_level_id = firstLevel.id
                    });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return Error("Не удалось создать сохранение: проверьте, что у пользователя ещё нет сохранения");
            }
        }

        [HttpGet]
        [Route("save/{save_id:int}/full")]
        public async Task<IActionResult> GetFullSave(int save_id)
        {
            if (save_id <= 0)
            {
                return Error("Некорректный идентификатор сохранения");
            }

            var save = await _ContextDb.Saveses.AsNoTracking().Where(x => x.id == save_id).Select(x => new
                {
                    x.id,
                    x.user_id,
                    x.coins,
                    x.character_id,
                    character = new
                    {
                        x.character.id,
                        x.character.name
                    },
                    x.floor_id,
                    floor = x.floor == null ? null : new
                        {
                            x.floor.id,
                            x.floor.name,
                            x.floor.asset_name
                        },
                    x.wallpaper_id,
                    wallpaper = x.wallpaper == null ? null : new
                        {
                            x.wallpaper.id,
                            x.wallpaper.name,
                            x.wallpaper.asset_name
                        },
                    x.createdAt,
                    x.updatedAt
                })
                .FirstOrDefaultAsync();
            if (save == null)
            {
                return Error("Сохранение не найдено");
            }

            var levels = await _ContextDb.Levels.AsNoTracking().OrderBy(x => x.level_number).Select(level => new
                {
                    level.id,
                    level.level_number,
                    level.total_orders,
                    level.required_orders,
                    level.required_revenue,
                    progress = _ContextDb.LevelProgresses.Where(progress => progress.save_id == save_id && progress.level_id == level.id)
                        .Select(progress => new
                        {
                            progress.is_unlocked,
                            progress.is_completed,
                            progress.best_completed_orders,
                            progress.best_revenue
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();
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
                    save,
                    levels,
                    inventory
                });
        }

        [HttpPut]
        [Route("save/{save_id:int}/character")]
        public async Task<IActionResult> UpdateCharacter(int save_id, [FromBody] UpdateCharacterRequest? request)
        {
            if (request == null)
            {
                return Error("Тело запроса не передано");
            }

            if (save_id <= 0 || request.character_id <= 0)
            {
                return Error("Некорректный идентификатор сохранения или персонажа");
            }

            var save = await _ContextDb.Saveses.FirstOrDefaultAsync(x => x.id == save_id);
            if (save == null)
            {
                return Error("Сохранение не найдено");
            }

            if (!await _ContextDb.Characters.AnyAsync(x => x.id == request.character_id))
            {
                return Error("Персонаж не найден");
            }

            save.character_id = request.character_id;
            save.updatedAt = DateTime.UtcNow;
            await _ContextDb.SaveChangesAsync();
            return new OkObjectResult(new
                {
                    status = true,
                    save_id = save.id,
                    character_id = save.character_id
                });
        }

        [HttpPut]
        [Route("save/{save_id:int}/coverings")]
        public async Task<IActionResult> UpdateCoverings(int save_id, [FromBody] UpdateCoveringsRequest? request)
        {
            if (request == null)
            {
                return Error("Тело запроса не передано");
            }

            if (save_id <= 0 ||
                (request.floor_id == null &&
                request.wallpaper_id == null) ||
                request.floor_id is <= 0 ||
                request.wallpaper_id is <= 0)
            {
                return Error("Некорректный идентификатор сохранения, пола или обоев");
            }

            await using var transaction = await _ContextDb.Database.BeginTransactionAsync();
            var save = await _ContextDb.Saveses.FromSqlInterpolated($"SELECT * FROM \"Saveses\" WHERE id = {save_id} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (save == null)
            {
                return Error("Сохранение не найдено");
            }

            if (request.floor_id.HasValue)
            {
                var floor = await _ContextDb.Furnitures.AsNoTracking().FirstOrDefaultAsync(x => x.id == request.floor_id);
                if (floor == null)
                {
                    return Error("Предмет пола не найден");
                }

                if (!string.Equals(floor.category, "floor", StringComparison.OrdinalIgnoreCase))
                {
                    return Error("Выбранный предмет не относится к категории floor");
                }
            }

            if (request.wallpaper_id.HasValue)
            {
                var wallpaper = await _ContextDb.Furnitures.AsNoTracking().FirstOrDefaultAsync(x => x.id == request.wallpaper_id);
                if (wallpaper == null)
                {
                    return Error("Предмет обоев не найден");
                }

                if (!string.Equals(wallpaper.category, "wallpaper", StringComparison.OrdinalIgnoreCase))
                {
                    return Error("Выбранный предмет не относится к категории wallpaper");
                }
            }

            var ownedIds = await _ContextDb.Inventories.Where(x => x.save_id == save_id &&
                    x.quantity > 0 &&
                    (x.furniture_id == request.floor_id ||
                    x.furniture_id == request.wallpaper_id))
                .Select(x => x.furniture_id)
                .ToListAsync();
            if (request.floor_id.HasValue &&
                request.floor_id != save.floor_id &&
                !ownedIds.Contains(request.floor_id.Value))
            {
                return Error("Выбранный пол не принадлежит этому сохранению");
            }

            if (request.wallpaper_id.HasValue &&
                request.wallpaper_id != save.wallpaper_id &&
                !ownedIds.Contains(request.wallpaper_id.Value))
            {
                return Error("Выбранные обои не принадлежат этому сохранению");
            }

            if (request.floor_id.HasValue)
            {
                save.floor_id = request.floor_id;
            }

            if (request.wallpaper_id.HasValue)
            {
                save.wallpaper_id = request.wallpaper_id;
            }

            save.updatedAt = DateTime.UtcNow;
            await _ContextDb.SaveChangesAsync();
            await transaction.CommitAsync();
            return new OkObjectResult(new
                {
                    status = true,
                    save_id = save.id,
                    save.floor_id,
                    save.wallpaper_id
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
