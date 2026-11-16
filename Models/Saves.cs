using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MidnightSteepApi.Models
{
    public class Saves
    {
        [Key]
        public int id { get; set; }

        [ForeignKey("user")]
        public int user_id { get; set; }
        public User user { get; set; }

        [ForeignKey("character")]
        public int character_id { get; set; }
        public Character character { get; set; }
        public int coins { get; set; }
        public DateTime createdAt { get; set; } = DateTime.UtcNow;
        public DateTime updatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("floor")]
        public int? floor_id { get; set; }
        public Furniture? floor { get; set; }

        [ForeignKey("wallpaper")]
        public int? wallpaper_id { get; set; }
        public Furniture? wallpaper { get; set; }
    }
}
