using System.ComponentModel.DataAnnotations;

namespace MidnightSteepApi.Models;

public class Dessert
{
    [Key]
    public int id { get; set; }
    public string name { get; set; } = "";
    public string asset_name { get; set; } = "";
    public int batch_size { get; set; } = 3;
    public int batch_price { get; set; }
    public int sale_price { get; set; }
}
