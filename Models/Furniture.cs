using System.ComponentModel.DataAnnotations;

namespace MidnightSteepApi.Models
{
    public class Furniture
    {
        [Key]
        public int id { get; set; }
        public string name { get; set; }
        public int price { get; set; }
        public string category { get; set; }
        public int max_stack { get; set; }
        public int comfort { get; set; }
        public int unlocked_level_id { get; set; } = 0;
        public string asset_name { get; set; }
        public string placement_layer { get; set; }
    }
}
