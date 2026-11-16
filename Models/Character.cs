using System.ComponentModel.DataAnnotations;

namespace MidnightSteepApi.Models
{
    public class Character
    {
        [Key]
        public int id { get; set; }
        public string name { get; set; }
    }
}
