namespace MidnightSteepApi.Dtos;

public class DessertPurchaseRequest
{
    public int save_id { get; set; }
    public int dessert_id { get; set; }
    public Guid purchase_id { get; set; }
}
