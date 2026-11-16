using System.ComponentModel.DataAnnotations;

namespace MidnightSteepApi.Models
{
    public class Drink
    {
        [Key]
        public int id { get; set; }
        public string name { get; set; }
        public int price { get; set; }
        public int cooking_time { get; set; }
        public int unlock_level_id { get; set; }
        public string asset_name { get; set; }
    }
}
