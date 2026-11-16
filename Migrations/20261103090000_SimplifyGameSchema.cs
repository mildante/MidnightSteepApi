using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace MidnightSteepApi.Migrations
{
    public partial class SimplifyGameSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "attempts_count", table: "LevelProgresses");
            migrationBuilder.DropColumn(name: "completed_at", table: "LevelProgresses");
            migrationBuilder.DropColumn(name: "quantity", table: "Recipes");
            migrationBuilder.DropColumn(name: "is_consumable", table: "Furnitures");
            migrationBuilder.DropColumn(name: "is_available", table: "Furnitures");
            migrationBuilder.DropColumn(name: "rotation", table: "PlacedItems");
            migrationBuilder.DropColumn(name: "placement_layer", table: "PlacedItems");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attempts_count",
                table: "LevelProgresses",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );
            migrationBuilder.AddColumn<DateTime>(
                name: "completed_at",
                table: "LevelProgresses",
                type: "timestamp with time zone",
                nullable: true
            );
            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "Recipes",
                type: "integer",
                nullable: false,
                defaultValue: 1
            );
            migrationBuilder.AddColumn<bool>(
                name: "is_consumable",
                table: "Furnitures",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );
            migrationBuilder.AddColumn<bool>(
                name: "is_available",
                table: "Furnitures",
                type: "boolean",
                nullable: false,
                defaultValue: true
            );
            migrationBuilder.AddColumn<float>(
                name: "rotation",
                table: "PlacedItems",
                type: "real",
                nullable: false,
                defaultValue: 0f
            );
            migrationBuilder.AddColumn<string>(
                name: "placement_layer",
                table: "PlacedItems",
                type: "text",
                nullable: false,
                defaultValue: ""
            );
            migrationBuilder.Sql("UPDATE \"PlacedItems\" p SET \"placement_layer\" = f.\"placement_layer\" FROM \"Furnitures\" f WHERE p.\"furniture_id\" = f.\"id\"");
        }
    }
}
