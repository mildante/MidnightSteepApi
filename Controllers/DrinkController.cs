using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Data;

namespace MidnightSteepApi.Controllers
{
    public class DrinkController
    {
        private readonly ContextDb _ContextDb;
        public DrinkController(ContextDb context)
        {
            _ContextDb = context;
        }

        [HttpGet]
        [Route("drinks")]
        public async Task<IActionResult> GetDrinks()
        {
            var drinks = await LoadDrinks();
            return new OkObjectResult(new
                {
                    status = true,
                    drinks
                });
        }

        [HttpGet]
        [Route("drinks/available/{save_id:int}")]
        public async Task<IActionResult> GetAvailableDrinks(int save_id)
        {
            if (save_id <= 0)
            {
                return Error("Некорректный идентификатор сохранения");
            }

            if (!await _ContextDb.Saveses.AnyAsync(x => x.id == save_id))
            {
                return Error("Сохранение не найдено");
            }

            var availableDrinkIds = await _ContextDb.Drinks.AsNoTracking().Where(drink => drink.unlock_level_id <= 0 ||
                    _ContextDb.LevelProgresses.Any(progress => progress.save_id == save_id &&
                        progress.level_id == drink.unlock_level_id &&
                        progress.is_unlocked))
                .Select(x => x.id)
                .ToListAsync();
            var drinks = await LoadDrinks(availableDrinkIds);
            return new OkObjectResult(new
                {
                    status = true,
                    drinks
                });
        }

        private async Task<List<DrinkResponse>> LoadDrinks(List<int>? drinkIds = null)
        {
            var query = _ContextDb.Drinks.AsNoTracking().AsQueryable();
            if (drinkIds != null)
            {
                query = query.Where(x => drinkIds.Contains(x.id));
            }

            var drinks = await query.OrderBy(x => x.id).Select(x => new DrinkResponse
                {
                    id = x.id,
                    name = x.name,
                    price = x.price,
                    cooking_time = x.cooking_time,
                    unlock_level_id = x.unlock_level_id,
                    asset_name = x.asset_name
                })
                .ToListAsync();
            var ids = drinks.Select(x => x.id).ToList();
            var ingredients = await _ContextDb.Recipes.AsNoTracking().Where(x => ids.Contains(x.drink_id)).OrderBy(x => x.ingredient_id)
                .Select(x => new RecipeResponse
                {
                    drink_id = x.drink_id,
                    ingredient_id = x.ingredient_id,
                    name = x.ingredient.name,
                    asset_name = x.ingredient.asset_name
                })
                .ToListAsync();
            foreach (var drink in drinks)
            {
                drink.ingredients = ingredients.Where(x => x.drink_id == drink.id).ToList();
            }

            return drinks;
        }

        private static OkObjectResult Error(string message)
        {
            return new OkObjectResult(new
                {
                    status = false,
                    error = message
                });
        }

        private sealed class DrinkResponse
        {
            public int id { get; set; }
            public string name { get; set; } = string.Empty;
            public int price { get; set; }
            public int cooking_time { get; set; }
            public int unlock_level_id { get; set; }
            public string asset_name { get; set; } = string.Empty;
            public List<RecipeResponse> ingredients { get; set; } = new();
        }

        private sealed class RecipeResponse
        {
            public int drink_id { get; set; }
            public int ingredient_id { get; set; }
            public string name { get; set; } = string.Empty;
            public string asset_name { get; set; } = string.Empty;
        }
    }
}
