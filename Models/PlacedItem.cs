using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MidnightSteepApi.Models
{
    public class PlacedItem
    {
        [Key]
        public int id { get; set; }

        [ForeignKey("furniture")]
        public int furniture_id { get; set; }
        public Furniture furniture { get; set; }

        [ForeignKey("save")]
        public int save_id { get; set; }
        public Saves save { get; set; }
        public float position_x { get; set; }
        public float position_y { get; set; }

        [ForeignKey("parent_placed_item")]
        public int? parent_placed_item_id { get; set; }
        public PlacedItem? parent_placed_item { get; set; }
    }
}
