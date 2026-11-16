using System.ComponentModel.DataAnnotations;

namespace MidnightSteepApi.Models;

public class DessertPurchase
{
    [Key]
    public Guid purchase_id { get; set; }
    public int save_id { get; set; }
    public int dessert_id { get; set; }
    public int quantity { get; set; }
    public int spent_coins { get; set; }
    public int sale_price { get; set; }
}
