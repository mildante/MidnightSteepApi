using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Models;

namespace MidnightSteepApi.Data
{
    public class ContextDb : DbContext
    {
        public ContextDb(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Dessert> Desserts { get; set; }
        public DbSet<DessertPurchase> DessertPurchases { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Level> Levels { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<Drink> Drinks { get; set; }
        public DbSet<Furniture> Furnitures { get; set; }
        public DbSet<Ingredients> Ingredients { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<LevelProgress> LevelProgresses { get; set; }
        public DbSet<PlacedItem> PlacedItems { get; set; }
        public DbSet<Recipes> Recipes { get; set; }
        public DbSet<Saves> Saveses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Dessert>().HasIndex(x => x.asset_name).IsUnique();
            modelBuilder.Entity<DessertPurchase>().HasOne<Saves>().WithMany().HasForeignKey(x => x.save_id)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<DessertPurchase>().HasOne<Dessert>().WithMany().HasForeignKey(x => x.dessert_id)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Saves>().HasIndex(x => x.user_id).IsUnique();
            modelBuilder.Entity<Saves>().HasOne(x => x.floor).WithMany().HasForeignKey(x => x.floor_id).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Saves>().HasOne(x => x.wallpaper).WithMany().HasForeignKey(x => x.wallpaper_id)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Inventory>().HasOne(x => x.save).WithMany().HasForeignKey(x => x.save_id);
            modelBuilder.Entity<Inventory>().HasIndex(x => new
                {
                    x.save_id,
                    x.furniture_id
                }).IsUnique();
            modelBuilder.Entity<Inventory>().HasIndex(x => x.save_id);
            modelBuilder.Entity<LevelProgress>().HasIndex(x => x.save_id);
            modelBuilder.Entity<Recipes>().HasIndex(x => x.drink_id);
            modelBuilder.Entity<LevelProgress>().HasIndex(x => new
                {
                    x.save_id,
                    x.level_id
                }).IsUnique();
            modelBuilder.Entity<Recipes>().HasIndex(x => new
                {
                    x.drink_id,
                    x.ingredient_id
                }).IsUnique();
            modelBuilder.Entity<Level>().HasIndex(x => x.level_number).IsUnique();
            modelBuilder.Entity<PlacedItem>().HasOne(x => x.parent_placed_item).WithMany().HasForeignKey(x => x.parent_placed_item_id)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
