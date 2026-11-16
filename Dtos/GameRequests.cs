namespace MidnightSteepApi.Dtos
{
    public class CreateSaveRequest
    {
        public int user_id { get; set; }
        public int character_id { get; set; }
    }

    public class ResetSaveRequest
    {
        public int user_id { get; set; }
    }

    public class UpdateCharacterRequest
    {
        public int character_id { get; set; }
    }

    public class UpdateCoveringsRequest
    {
        public int? floor_id { get; set; }
        public int? wallpaper_id { get; set; }
    }

    public class StartLevelRequest
    {
        public int save_id { get; set; }
        public int level_id { get; set; }
    }

    public class CompleteLevelRequest
    {
        public int save_id { get; set; }
        public int level_id { get; set; }
        public int completed_orders { get; set; }
        public int revenue { get; set; }
    }

    public class PurchaseRequest
    {
        public int save_id { get; set; }
        public int furniture_id { get; set; }
        public int quantity { get; set; } = 1;
    }

    public class ChangeInventoryQuantityRequest
    {
        public int save_id { get; set; }
        public int furniture_id { get; set; }
        public int quantity_change { get; set; }
    }

    public class SavePlacementRequest
    {
        public int save_id { get; set; }
        public List<PlacedItemRequest> items { get; set; } = new();
    }

    public class PlacedItemRequest
    {
        public int client_id { get; set; }
        public int furniture_id { get; set; }
        public float position_x { get; set; }
        public float position_y { get; set; }
        public int? parent_client_id { get; set; }
    }
}
