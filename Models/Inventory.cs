using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MidnightSteepApi.Models
{
    public class Inventory
    {
        [Key]
        public int id { get; set; }

        [ForeignKey("saves")]
        public int save_id { get; set; }
        public Saves save { get; set; }

        [ForeignKey("furniture")]
        public int furniture_id { get; set; }
        public Furniture furniture { get; set; }
        public int quantity { get; set; }
    }
}
