using System.ComponentModel.DataAnnotations;

namespace MidnightSteepApi.Models
{
    public class Ingredients
    {
        [Key]
        public int id { get; set; }
        public string name { get; set; }
        public string asset_name { get; set; }
    }
}
