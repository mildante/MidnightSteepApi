using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MidnightSteepApi.Data;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable
namespace MidnightSteepApi.Migrations
{
    [DbContext(typeof(ContextDb))]
    partial class ContextDbModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.32").HasAnnotation("Relational:MaxIdentifierLength", 63);
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);
            modelBuilder.Entity(
                "MidnightSteepApi.Models.Character",
                b =>
                {
                    b.Property<int>("id").ValueGeneratedOnAdd().HasColumnType("integer");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id"));
                    b.Property<string>("name").IsRequired().HasColumnType("text");
                    b.HasKey("id");
                    b.ToTable("Characters");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.Drink",
                b =>
                {
                    b.Property<int>("id").ValueGeneratedOnAdd().HasColumnType("integer");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id"));
                    b.Property<string>("asset_name").IsRequired().HasColumnType("text");
                    b.Property<int>("cooking_time").HasColumnType("integer");
                    b.Property<string>("name").IsRequired().HasColumnType("text");
                    b.Property<int>("price").HasColumnType("integer");
                    b.Property<int>("unlock_level_id").HasColumnType("integer");
                    b.HasKey("id");
                    b.ToTable("Drinks");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.Furniture",
                b =>
                {
                    b.Property<int>("id").ValueGeneratedOnAdd().HasColumnType("integer");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id"));
                    b.Property<string>("asset_name").IsRequired().HasColumnType("text");
                    b.Property<string>("category").IsRequired().HasColumnType("text");
                    b.Property<int>("comfort").HasColumnType("integer");
                    b.Property<int>("max_stack").HasColumnType("integer");
                    b.Property<string>("name").IsRequired().HasColumnType("text");
                    b.Property<string>("placement_layer").IsRequired().HasColumnType("text");
                    b.Property<int>("price").HasColumnType("integer");
                    b.Property<int>("unlocked_level_id").HasColumnType("integer");
                    b.HasKey("id");
                    b.ToTable("Furnitures");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.Ingredients",
                b =>
                {
                    b.Property<int>("id").ValueGeneratedOnAdd().HasColumnType("integer");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id"));
                    b.Property<string>("asset_name").IsRequired().HasColumnType("text");
                    b.Property<string>("name").IsRequired().HasColumnType("text");
                    b.HasKey("id");
                    b.ToTable("Ingredients");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.Inventory",
                b =>
                {
                    b.Property<int>("id").ValueGeneratedOnAdd().HasColumnType("integer");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id"));
                    b.Property<int>("furniture_id").HasColumnType("integer");
                    b.Property<int>("quantity").HasColumnType("integer");
                    b.Property<int>("save_id").HasColumnType("integer");
                    b.HasKey("id");
                    b.HasIndex("furniture_id");
                    b.HasIndex("save_id");
                    b.HasIndex("save_id", "furniture_id").IsUnique();
                    b.ToTable("Inventories");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.Level",
                b =>
                {
                    b.Property<int>("id").ValueGeneratedOnAdd().HasColumnType("integer");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id"));
                    b.Property<int>("level_number").HasColumnType("integer");
                    b.Property<int>("required_orders").HasColumnType("integer");
                    b.Property<int>("required_revenue").HasColumnType("integer");
                    b.Property<int>("total_orders").HasColumnType("integer");
                    b.HasKey("id");
                    b.HasIndex("level_number").IsUnique();
                    b.ToTable("Levels");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.LevelProgress",
                b =>
                {
                    b.Property<int>("id").ValueGeneratedOnAdd().HasColumnType("integer");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id"));
                    b.Property<int>("best_completed_orders").HasColumnType("integer");
                    b.Property<int>("best_revenue").HasColumnType("integer");
                    b.Property<bool>("is_completed").HasColumnType("boolean");
                    b.Property<bool>("is_unlocked").HasColumnType("boolean");
                    b.Property<int>("level_id").HasColumnType("integer");
                    b.Property<int>("save_id").HasColumnType("integer");
                    b.HasKey("id");
                    b.HasIndex("level_id");
                    b.HasIndex("save_id");
                    b.HasIndex("save_id", "level_id").IsUnique();
                    b.ToTable("LevelProgresses");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.PlacedItem",
                b =>
                {
                    b.Property<int>("id").ValueGeneratedOnAdd().HasColumnType("integer");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id"));
                    b.Property<int>("furniture_id").HasColumnType("integer");
                    b.Property<int?>("parent_placed_item_id").HasColumnType("integer");
                    b.Property<float>("position_x").HasColumnType("real");
                    b.Property<float>("position_y").HasColumnType("real");
                    b.Property<int>("save_id").HasColumnType("integer");
                    b.HasKey("id");
                    b.HasIndex("furniture_id");
                    b.HasIndex("parent_placed_item_id");
                    b.HasIndex("save_id");
                    b.ToTable("PlacedItems");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.Recipes",
                b =>
                {
                    b.Property<int>("id").ValueGeneratedOnAdd().HasColumnType("integer");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id"));
                    b.Property<int>("drink_id").HasColumnType("integer");
                    b.Property<int>("ingredient_id").HasColumnType("integer");
                    b.HasKey("id");
                    b.HasIndex("drink_id");
                    b.HasIndex("ingredient_id");
                    b.HasIndex("drink_id", "ingredient_id").IsUnique();
                    b.ToTable("Recipes");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.Saves",
                b =>
                {
                    b.Property<int>("id").ValueGeneratedOnAdd().HasColumnType("integer");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id"));
                    b.Property<int>("character_id").HasColumnType("integer");
                    b.Property<int>("coins").HasColumnType("integer");
                    b.Property<DateTime>("createdAt").HasColumnType("timestamp with time zone");
                    b.Property<int?>("floor_id").HasColumnType("integer");
                    b.Property<int>("user_id").HasColumnType("integer");
                    b.Property<DateTime>("updatedAt").HasColumnType("timestamp with time zone");
                    b.Property<int?>("wallpaper_id").HasColumnType("integer");
                    b.HasKey("id");
                    b.HasIndex("character_id");
                    b.HasIndex("floor_id");
                    b.HasIndex("user_id").IsUnique();
                    b.HasIndex("wallpaper_id");
                    b.ToTable("Saveses");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.User",
                b =>
                {
                    b.Property<int>("id").ValueGeneratedOnAdd().HasColumnType("integer");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id"));
                    b.Property<DateTime>("createdAt").HasColumnType("timestamp with time zone");
                    b.Property<DateTime>("last_login").HasColumnType("timestamp with time zone");
                    b.Property<string>("login").IsRequired().HasColumnType("text");
                    b.Property<string>("password").IsRequired().HasColumnType("text");
                    b.HasKey("id");
                    b.ToTable("Users");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.Inventory",
                b =>
                {
                    b.HasOne("MidnightSteepApi.Models.Furniture", "furniture").WithMany().HasForeignKey("furniture_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.HasOne("MidnightSteepApi.Models.Saves", "save").WithMany().HasForeignKey("save_id").OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.Navigation("furniture");
                    b.Navigation("save");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.LevelProgress",
                b =>
                {
                    b.HasOne("MidnightSteepApi.Models.Level", "level").WithMany().HasForeignKey("level_id").OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.HasOne("MidnightSteepApi.Models.Saves", "save").WithMany().HasForeignKey("save_id").OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.Navigation("level");
                    b.Navigation("save");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.PlacedItem",
                b =>
                {
                    b.HasOne("MidnightSteepApi.Models.Furniture", "furniture").WithMany().HasForeignKey("furniture_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.HasOne("MidnightSteepApi.Models.Saves", "save").WithMany().HasForeignKey("save_id").OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.HasOne("MidnightSteepApi.Models.PlacedItem", "parent_placed_item").WithMany().HasForeignKey("parent_placed_item_id")
                        .OnDelete(DeleteBehavior.Restrict);
                    b.Navigation("furniture");
                    b.Navigation("parent_placed_item");
                    b.Navigation("save");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.Recipes",
                b =>
                {
                    b.HasOne("MidnightSteepApi.Models.Drink", "drink").WithMany().HasForeignKey("drink_id").OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.HasOne("MidnightSteepApi.Models.Ingredients", "ingredient").WithMany().HasForeignKey("ingredient_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.Navigation("drink");
                    b.Navigation("ingredient");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.Saves",
                b =>
                {
                    b.HasOne("MidnightSteepApi.Models.Character", "character").WithMany().HasForeignKey("character_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.HasOne("MidnightSteepApi.Models.Furniture", "floor").WithMany().HasForeignKey("floor_id").OnDelete(DeleteBehavior.Cascade);
                    b.HasOne("MidnightSteepApi.Models.User", "user").WithMany().HasForeignKey("user_id").OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.HasOne("MidnightSteepApi.Models.Furniture", "wallpaper").WithMany().HasForeignKey("wallpaper_id")
                        .OnDelete(DeleteBehavior.Cascade);
                    b.Navigation("character");
                    b.Navigation("floor");
                    b.Navigation("user");
                    b.Navigation("wallpaper");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.Dessert",
                b =>
                {
                    b.Property<int>("id").ValueGeneratedOnAdd().HasColumnType("integer");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id"));
                    b.Property<string>("name").IsRequired().HasColumnType("text");
                    b.Property<string>("asset_name").IsRequired().HasColumnType("text");
                    b.Property<int>("batch_size").HasColumnType("integer");
                    b.Property<int>("batch_price").HasColumnType("integer");
                    b.Property<int>("sale_price").HasColumnType("integer");
                    b.HasKey("id");
                    b.HasIndex("asset_name").IsUnique();
                    b.ToTable("Desserts");
                }
            );
            modelBuilder.Entity(
                "MidnightSteepApi.Models.DessertPurchase",
                b =>
                {
                    b.Property<Guid>("purchase_id").ValueGeneratedOnAdd().HasColumnType("uuid");
                    b.Property<int>("save_id").HasColumnType("integer");
                    b.Property<int>("dessert_id").HasColumnType("integer");
                    b.Property<int>("quantity").HasColumnType("integer");
                    b.Property<int>("spent_coins").HasColumnType("integer");
                    b.Property<int>("sale_price").HasColumnType("integer");
                    b.HasKey("purchase_id");
                    b.HasIndex("save_id");
                    b.HasIndex("dessert_id");
                    b.ToTable("DessertPurchases");
                    b.HasOne("MidnightSteepApi.Models.Saves", null).WithMany().HasForeignKey("save_id").OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.HasOne("MidnightSteepApi.Models.Dessert", null).WithMany().HasForeignKey("dessert_id").OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                }
            );
#pragma warning restore 612, 618
        }
    }
}
