using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MidnightSteepApi.Models
{
    public class Recipes
    {
        [Key]
        public int id { get; set; }

        [ForeignKey("drink")]
        public int drink_id { get; set; }
        public Drink drink { get; set; }

        [ForeignKey("ingredient")]
        public int ingredient_id { get; set; }
        public Ingredients ingredient { get; set; }
    }
}
