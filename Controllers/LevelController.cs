using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Data;
using MidnightSteepApi.Dtos;
using MidnightSteepApi.Models;

namespace MidnightSteepApi.Controllers
{
    public class LevelController
    {
        private readonly ContextDb _ContextDb;
        public LevelController(ContextDb context)
        {
            _ContextDb = context;
        }

        [HttpGet]
        [Route("levels/{save_id:int}")]
        public async Task<IActionResult> GetLevels(int save_id)
        {
            if (save_id <= 0)
            {
                return Error("Некорректный идентификатор сохранения");
            }

            if (!await _ContextDb.Saveses.AnyAsync(x => x.id == save_id))
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
            return new OkObjectResult(new
                {
                    status = true,
                    levels
                });
        }

        [HttpPost]
        [Route("levels/start")]
        public async Task<IActionResult> StartLevel([FromBody] StartLevelRequest? request)
        {
            if (request == null)
            {
                return Error("Тело запроса не передано");
            }

            if (request.save_id <= 0 || request.level_id <= 0)
            {
                return Error("Некорректный идентификатор сохранения или уровня");
            }

            if (!await _ContextDb.Saveses.AnyAsync(x => x.id == request.save_id))
            {
                return Error("Сохранение не найдено");
            }

            if (!await _ContextDb.Levels.AnyAsync(x => x.id == request.level_id))
            {
                return Error("Уровень не найден");
            }

            var progress = await _ContextDb.LevelProgresses.FirstOrDefaultAsync(x => x.save_id == request.save_id && x.level_id == request.level_id);
            if (progress == null || !progress.is_unlocked)
            {
                return Error("Уровень ещё не открыт");
            }

            return new OkObjectResult(new
                {
                    status = true,
                    request.save_id,
                    request.level_id
                });
        }

        [HttpPost]
        [Route("levels/complete")]
        public async Task<IActionResult> CompleteLevel([FromBody] CompleteLevelRequest? request)
        {
            if (request == null)
            {
                return Error("Тело запроса не передано");
            }

            if (request.save_id <= 0 || request.level_id <= 0)
            {
                return Error("Некорректный идентификатор сохранения или уровня");
            }

            if (request.completed_orders < 0 || request.revenue < 0)
            {
                return Error("Количество заказов и выручка не могут быть отрицательными");
            }

            await using var transaction = await _ContextDb.Database.BeginTransactionAsync();
            var save = await _ContextDb.Saveses.FromSqlInterpolated($"SELECT * FROM \"Saveses\" WHERE id = {request.save_id} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (save == null)
            {
                return Error("Сохранение не найдено");
            }

            var level = await _ContextDb.Levels.FirstOrDefaultAsync(x => x.id == request.level_id);
            if (level == null)
            {
                return Error("Уровень не найден");
            }

            if (request.completed_orders > level.total_orders)
            {
                return Error("Количество выполненных заказов превышает число заказов уровня");
            }

            var progress = await _ContextDb.LevelProgresses.FirstOrDefaultAsync(x => x.save_id == request.save_id && x.level_id == request.level_id);
            if (progress == null || !progress.is_unlocked)
            {
                return Error("Уровень ещё не открыт");
            }

            var completed = request.completed_orders >= level.required_orders && request.revenue >= level.required_revenue;
            progress.best_completed_orders = Math.Max(progress.best_completed_orders, request.completed_orders);
            progress.best_revenue = Math.Max(progress.best_revenue, request.revenue);
            if (completed && !progress.is_completed)
            {
                progress.is_completed = true;
            }

            int? openedNextLevelId = null;
            if (completed)
            {
                var nextLevel = await _ContextDb.Levels.Where(x => x.level_number > level.level_number).OrderBy(x => x.level_number)
                    .ThenBy(x => x.id)
                    .FirstOrDefaultAsync();
                if (nextLevel != null)
                {
                    openedNextLevelId = nextLevel.id;
                    var nextProgress = await _ContextDb.LevelProgresses.FirstOrDefaultAsync(x => x.save_id == request.save_id && x.level_id == nextLevel.id);
                    if (nextProgress == null || !nextProgress.is_unlocked)
                    {
                        var newIngredientKeys = await _ContextDb.Recipes.AsNoTracking().Where(recipe => recipe.drink.unlock_level_id == nextLevel.id &&
                                !_ContextDb.Recipes.Any(known => known.ingredient_id == recipe.ingredient_id &&
                                    (known.drink.unlock_level_id <= 0 ||
                                    _ContextDb.LevelProgresses.Any(p => p.save_id == request.save_id &&
                                        p.is_unlocked &&
                                        p.level_id == known.drink.unlock_level_id))))
                            .Select(recipe => recipe.ingredient.asset_name)
                            .Distinct()
                            .ToListAsync();
                        var crateKeys = newIngredientKeys.SelectMany(CrateKeyAliases).Distinct().ToList();
                        var crates = await _ContextDb.Furnitures.AsNoTracking().Where(item => crateKeys.Contains(item.asset_name)).ToListAsync();
                        foreach (var ingredientKey in newIngredientKeys)
                        {
                            if (crates.Count(item => CrateKeyAliases(ingredientKey).Contains(item.asset_name)) != 1)
                            {
                                return Error($"В каталоге должен быть ровно один ящик ингредиента {ingredientKey}. Выполните Scripts/Seed/02_furniture_catalog.sql");
                            }
                        }

                        var crateIds = crates.Select(item => item.id).ToList();
                        var inventory = await _ContextDb.Inventories.Where(item => item.save_id == request.save_id && crateIds.Contains(item.furniture_id))
                            .ToListAsync();
                        foreach (var crate in crates)
                        {
                            var owned = inventory.SingleOrDefault(item => item.furniture_id == crate.id);
                            if (owned == null)
                            {
                                _ContextDb.Inventories.Add(new Inventory
                                    {
                                        save_id = request.save_id,
                                        furniture_id = crate.id,
                                        quantity = 1
                                    });
                            }
                            else if (owned.quantity < 1)
                            {
                                owned.quantity = 1;
                            }
                        }
                    }

                    if (nextProgress == null)
                    {
                        await _ContextDb.LevelProgresses.AddAsync(new LevelProgress
                            {
                                save_id = request.save_id,
                                level_id = nextLevel.id,
                                is_unlocked = true
                            });
                    }
                    else
                    {
                        nextProgress.is_unlocked = true;
                    }
                }
            }

            try
            {
                save.coins = checked(save.coins + request.revenue);
            }
            catch (OverflowException)
            {
                await transaction.RollbackAsync();
                return Error("Баланс монет достиг максимального значения");
            }

            save.updatedAt = DateTime.UtcNow;
            await _ContextDb.SaveChangesAsync();
            await transaction.CommitAsync();
            return new OkObjectResult(new
                {
                    status = true,
                    completed,
                    coins_earned = request.revenue,
                    coins = save.coins,
                    opened_next_level_id = openedNextLevelId,
                    progress = new
                    {
                        progress.is_unlocked,
                        progress.is_completed,
                        progress.best_completed_orders,
                        progress.best_revenue
                    }
                });
        }

        private static string[] CrateKeyAliases(string ingredientKey)
        {
            return ingredientKey switch
            {
                "TeaLeaves" or "CrateTeaLeaves" or "crate_tea_leaves" => new[]
                {
                    "CrateTeaLeaves",
                    "crate_tea_leaves"
                },
                "Sugar" or "CrateSugar" or "crate_sugar" => new[]
                {
                    "CrateSugar",
                    "crate_sugar"
                },
                "Milk" or "CrateMilk" or "crate_milk" => new[]
                {
                    "CrateMilk",
                    "crate_milk"
                },
                "Lemon" or "CrateLemon" or "crate_lemon" => new[]
                {
                    "CrateLemon",
                    "crate_lemon"
                },
                "Honey" or "CrateHoney" or "crate_honey" => new[]
                {
                    "CrateHoney",
                    "crate_honey"
                },
                "Berries" or "CrateBerries" or "crate_berries" => new[]
                {
                    "CrateBerries",
                    "crate_berries"
                },
                "Lavender" or "CrateLavender" or "crate_lavender" => new[]
                {
                    "CrateLavender",
                    "crate_lavender"
                },
                "RosePetals" or "CrateRosePetals" or "crate_rose_petals" => new[]
                {
                    "CrateRosePetals",
                    "crate_rose_petals"
                },
                _ => new[]
                {
                    ingredientKey
                }
            };
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
