using System.ComponentModel.DataAnnotations;

namespace MidnightSteepApi.Models
{
    public class User
    {
        [Key]
        public int id { get; set; }
        public string login { get; set; }
        public string password { get; set; }
        public DateTime createdAt { get; set; } = DateTime.UtcNow;
        public DateTime last_login { get; set; }
    }
}
