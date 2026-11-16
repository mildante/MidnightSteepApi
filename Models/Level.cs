using System.ComponentModel.DataAnnotations;

namespace MidnightSteepApi.Models
{
    public class Level
    {
        [Key]
        public int id { get; set; }
        public int level_number { get; set; }
        public int total_orders { get; set; }
        public int required_orders { get; set; }
        public int required_revenue { get; set; }
    }
}
