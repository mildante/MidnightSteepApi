using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MidnightSteepApi.Data;

#nullable disable
namespace MidnightSteepApi.Migrations
{
    [DbContext(typeof(ContextDb))]
    [Migration("20261021170000_AddUnityGameApiFields")]
    public partial class AddUnityGameApiFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Inventories_Saveses_saveid", table: "Inventories");
            migrationBuilder.DropIndex(name: "IX_Inventories_saveid", table: "Inventories");
            migrationBuilder.Sql("UPDATE \"Inventories\" SET \"save_id\" = \"saveid\" WHERE \"save_id\" <= 0");
            migrationBuilder.DropColumn(name: "saveid", table: "Inventories");
            migrationBuilder.DropIndex(name: "IX_Saveses_user_id", table: "Saveses");
            migrationBuilder.AlterColumn<int>(
                name: "wallpaper_id",
                table: "Saveses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer"
            );
            migrationBuilder.AlterColumn<int>(
                name: "floor_id",
                table: "Saveses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer"
            );
            migrationBuilder.AddColumn<DateTime>(
                name: "updatedAt",
                table: "Saveses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP"
            );
            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "Recipes",
                type: "integer",
                nullable: false,
                defaultValue: 1
            );
            migrationBuilder.AddColumn<int>(
                name: "parent_placed_item_id",
                table: "PlacedItems",
                type: "integer",
                nullable: true
            );
            migrationBuilder.AddColumn<string>(
                name: "placement_layer",
                table: "PlacedItems",
                type: "text",
                nullable: false,
                defaultValue: ""
            );
            migrationBuilder.AddColumn<float>(
                name: "rotation",
                table: "PlacedItems",
                type: "real",
                nullable: false,
                defaultValue: 0f
            );
            migrationBuilder.Sql("UPDATE \"PlacedItems\" p SET \"placement_layer\" = f.\"placement_layer\" FROM \"Furnitures\" f WHERE p.\"furniture_id\" = f.\"id\"");
            migrationBuilder.AddColumn<int>(
                name: "level_number",
                table: "Levels",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );
            migrationBuilder.Sql("UPDATE \"Levels\" SET \"level_number\" = \"id\"");
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
            migrationBuilder.AddColumn<bool>(
                name: "is_available",
                table: "Furnitures",
                type: "boolean",
                nullable: false,
                defaultValue: true
            );
            migrationBuilder.AddColumn<bool>(
                name: "is_consumable",
                table: "Furnitures",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );
            migrationBuilder.CreateIndex(
                name: "IX_Saveses_user_id",
                table: "Saveses",
                column: "user_id",
                unique: true
            );
            migrationBuilder.CreateIndex(
                name: "IX_Recipes_drink_id_ingredient_id",
                table: "Recipes",
                columns: new[] {
                    "drink_id",
                    "ingredient_id"
                },
                unique: true
            );
            migrationBuilder.CreateIndex(name: "IX_PlacedItems_parent_placed_item_id", table: "PlacedItems", column: "parent_placed_item_id");
            migrationBuilder.CreateIndex(
                name: "IX_Levels_level_number",
                table: "Levels",
                column: "level_number",
                unique: true
            );
            migrationBuilder.CreateIndex(
                name: "IX_LevelProgresses_save_id_level_id",
                table: "LevelProgresses",
                columns: new[] {
                    "save_id",
                    "level_id"
                },
                unique: true
            );
            migrationBuilder.CreateIndex(
                name: "IX_Inventories_save_id_furniture_id",
                table: "Inventories",
                columns: new[] {
                    "save_id",
                    "furniture_id"
                },
                unique: true
            );
            migrationBuilder.CreateIndex(name: "IX_Inventories_save_id", table: "Inventories", column: "save_id");
            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Saveses_save_id",
                table: "Inventories",
                column: "save_id",
                principalTable: "Saveses",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
            migrationBuilder.AddForeignKey(
                name: "FK_PlacedItems_PlacedItems_parent_placed_item_id",
                table: "PlacedItems",
                column: "parent_placed_item_id",
                principalTable: "PlacedItems",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Inventories_Saveses_save_id", table: "Inventories");
            migrationBuilder.DropForeignKey(name: "FK_PlacedItems_PlacedItems_parent_placed_item_id", table: "PlacedItems");
            migrationBuilder.DropIndex(name: "IX_Saveses_user_id", table: "Saveses");
            migrationBuilder.DropIndex(name: "IX_Recipes_drink_id_ingredient_id", table: "Recipes");
            migrationBuilder.DropIndex(name: "IX_PlacedItems_parent_placed_item_id", table: "PlacedItems");
            migrationBuilder.DropIndex(name: "IX_Levels_level_number", table: "Levels");
            migrationBuilder.DropIndex(name: "IX_LevelProgresses_save_id_level_id", table: "LevelProgresses");
            migrationBuilder.DropIndex(name: "IX_Inventories_save_id_furniture_id", table: "Inventories");
            migrationBuilder.DropIndex(name: "IX_Inventories_save_id", table: "Inventories");
            migrationBuilder.CreateIndex(name: "IX_Saveses_user_id", table: "Saveses", column: "user_id");
            migrationBuilder.DropColumn(name: "updatedAt", table: "Saveses");
            migrationBuilder.DropColumn(name: "quantity", table: "Recipes");
            migrationBuilder.DropColumn(name: "parent_placed_item_id", table: "PlacedItems");
            migrationBuilder.DropColumn(name: "placement_layer", table: "PlacedItems");
            migrationBuilder.DropColumn(name: "rotation", table: "PlacedItems");
            migrationBuilder.DropColumn(name: "level_number", table: "Levels");
            migrationBuilder.DropColumn(name: "attempts_count", table: "LevelProgresses");
            migrationBuilder.DropColumn(name: "completed_at", table: "LevelProgresses");
            migrationBuilder.DropColumn(name: "is_available", table: "Furnitures");
            migrationBuilder.DropColumn(name: "is_consumable", table: "Furnitures");
            migrationBuilder.AlterColumn<int>(
                name: "wallpaper_id",
                table: "Saveses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true
            );
            migrationBuilder.AlterColumn<int>(
                name: "floor_id",
                table: "Saveses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true
            );
            migrationBuilder.AddColumn<int>(
                name: "saveid",
                table: "Inventories",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );
            migrationBuilder.Sql("UPDATE \"Inventories\" SET \"saveid\" = \"save_id\"");
            migrationBuilder.CreateIndex(name: "IX_Inventories_saveid", table: "Inventories", column: "saveid");
            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Saveses_saveid",
                table: "Inventories",
                column: "saveid",
                principalTable: "Saveses",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
