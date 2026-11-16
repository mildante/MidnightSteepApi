using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable
namespace MidnightSteepApi.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.id);
                }
            );
            migrationBuilder.CreateTable(
                name: "Drinks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<int>(type: "integer", nullable: false),
                    cooking_time = table.Column<int>(type: "integer", nullable: false),
                    unlock_level_id = table.Column<int>(type: "integer", nullable: false),
                    asset_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drinks", x => x.id);
                }
            );
            migrationBuilder.CreateTable(
                name: "Furnitures",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    max_stack = table.Column<int>(type: "integer", nullable: false),
                    comfort = table.Column<int>(type: "integer", nullable: false),
                    unlocked_level_id = table.Column<int>(type: "integer", nullable: false),
                    asset_name = table.Column<string>(type: "text", nullable: false),
                    placement_layer = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Furnitures", x => x.id);
                }
            );
            migrationBuilder.CreateTable(
                name: "Ingredients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    asset_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.id);
                }
            );
            migrationBuilder.CreateTable(
                name: "Levels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    total_orders = table.Column<int>(type: "integer", nullable: false),
                    required_orders = table.Column<int>(type: "integer", nullable: false),
                    required_revenue = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Levels", x => x.id);
                }
            );
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    login = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                }
            );
            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    drink_id = table.Column<int>(type: "integer", nullable: false),
                    ingredient_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.id);
                    table.ForeignKey(
                        name: "FK_Recipes_Drinks_drink_id",
                        column: x => x.drink_id,
                        principalTable: "Drinks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_Recipes_Ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "Ingredients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );
            migrationBuilder.CreateTable(
                name: "Saveses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    character_id = table.Column<int>(type: "integer", nullable: false),
                    coins = table.Column<int>(type: "integer", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    floor_id = table.Column<int>(type: "integer", nullable: false),
                    wallpaper_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Saveses", x => x.id);
                    table.ForeignKey(
                        name: "FK_Saveses_Characters_character_id",
                        column: x => x.character_id,
                        principalTable: "Characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_Saveses_Furnitures_floor_id",
                        column: x => x.floor_id,
                        principalTable: "Furnitures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_Saveses_Furnitures_wallpaper_id",
                        column: x => x.wallpaper_id,
                        principalTable: "Furnitures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_Saveses_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );
            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    save_id = table.Column<int>(type: "integer", nullable: false),
                    saveid = table.Column<int>(type: "integer", nullable: false),
                    furniture_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.id);
                    table.ForeignKey(
                        name: "FK_Inventories_Furnitures_furniture_id",
                        column: x => x.furniture_id,
                        principalTable: "Furnitures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_Inventories_Saveses_saveid",
                        column: x => x.saveid,
                        principalTable: "Saveses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );
            migrationBuilder.CreateTable(
                name: "LevelProgresses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    level_id = table.Column<int>(type: "integer", nullable: false),
                    save_id = table.Column<int>(type: "integer", nullable: false),
                    is_unlocked = table.Column<bool>(type: "boolean", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    best_revenue = table.Column<int>(type: "integer", nullable: false),
                    best_completed_orders = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LevelProgresses", x => x.id);
                    table.ForeignKey(
                        name: "FK_LevelProgresses_Levels_level_id",
                        column: x => x.level_id,
                        principalTable: "Levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_LevelProgresses_Saveses_save_id",
                        column: x => x.save_id,
                        principalTable: "Saveses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );
            migrationBuilder.CreateTable(
                name: "PlacedItems",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    furniture_id = table.Column<int>(type: "integer", nullable: false),
                    save_id = table.Column<int>(type: "integer", nullable: false),
                    position_x = table.Column<float>(type: "real", nullable: false),
                    position_y = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlacedItems", x => x.id);
                    table.ForeignKey(
                        name: "FK_PlacedItems_Furnitures_furniture_id",
                        column: x => x.furniture_id,
                        principalTable: "Furnitures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_PlacedItems_Saveses_save_id",
                        column: x => x.save_id,
                        principalTable: "Saveses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );
            migrationBuilder.CreateIndex(name: "IX_Inventories_furniture_id", table: "Inventories", column: "furniture_id");
            migrationBuilder.CreateIndex(name: "IX_Inventories_saveid", table: "Inventories", column: "saveid");
            migrationBuilder.CreateIndex(name: "IX_LevelProgresses_level_id", table: "LevelProgresses", column: "level_id");
            migrationBuilder.CreateIndex(name: "IX_LevelProgresses_save_id", table: "LevelProgresses", column: "save_id");
            migrationBuilder.CreateIndex(name: "IX_PlacedItems_furniture_id", table: "PlacedItems", column: "furniture_id");
            migrationBuilder.CreateIndex(name: "IX_PlacedItems_save_id", table: "PlacedItems", column: "save_id");
            migrationBuilder.CreateIndex(name: "IX_Recipes_drink_id", table: "Recipes", column: "drink_id");
            migrationBuilder.CreateIndex(name: "IX_Recipes_ingredient_id", table: "Recipes", column: "ingredient_id");
            migrationBuilder.CreateIndex(name: "IX_Saveses_character_id", table: "Saveses", column: "character_id");
            migrationBuilder.CreateIndex(name: "IX_Saveses_floor_id", table: "Saveses", column: "floor_id");
            migrationBuilder.CreateIndex(name: "IX_Saveses_user_id", table: "Saveses", column: "user_id");
            migrationBuilder.CreateIndex(name: "IX_Saveses_wallpaper_id", table: "Saveses", column: "wallpaper_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Inventories");
            migrationBuilder.DropTable(name: "LevelProgresses");
            migrationBuilder.DropTable(name: "PlacedItems");
            migrationBuilder.DropTable(name: "Recipes");
            migrationBuilder.DropTable(name: "Levels");
            migrationBuilder.DropTable(name: "Saveses");
            migrationBuilder.DropTable(name: "Drinks");
            migrationBuilder.DropTable(name: "Ingredients");
            migrationBuilder.DropTable(name: "Characters");
            migrationBuilder.DropTable(name: "Furnitures");
            migrationBuilder.DropTable(name: "Users");
        }
    }
}
