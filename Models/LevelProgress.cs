using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MidnightSteepApi.Models
{
    public class LevelProgress
    {
        [Key]
        public int id { get; set; }

        [ForeignKey("level")]
        public int level_id { get; set; }
        public Level level { get; set; }

        [ForeignKey("save")]
        public int save_id { get; set; }
        public Saves save { get; set; }
        public bool is_unlocked { get; set; }
        public bool is_completed { get; set; }
        public int best_revenue { get; set; }
        public int best_completed_orders { get; set; }
    }
}
