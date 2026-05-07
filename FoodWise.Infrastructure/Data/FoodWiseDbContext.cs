using FoodWise.Domain.Entities;
using FoodWise.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FoodWise.Infrastructure.Data;

public class FoodWiseDbContext : IdentityDbContext<ApplicationUser>
{
    public FoodWiseDbContext(DbContextOptions<FoodWiseDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<StockItem> StockItems { get; set; }
    public DbSet<WasteRiskPrediction> WasteRiskPredictions { get; set; }

    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<RecipeRecommendation> RecipeRecommendations { get; set; }

    public DbSet<DeliveryPoint> DeliveryPoints { get; set; }
    public DbSet<ShareListing> ShareListings { get; set; }
    public DbSet<ShareRequest> ShareRequests { get; set; }
    public DbSet<Delivery> Deliveries { get; set; }
    // Teslim noktalarındaki fiziksel kutu/bölme kayıtlarını temsil eder.
    public DbSet<DeliveryBox> DeliveryBoxes { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<CarbonReport> CarbonReports { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>()
            .Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Entity<Category>()
            .Property(x => x.Description)
            .HasMaxLength(500);

        builder.Entity<Unit>()
            .Property(x => x.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Entity<Unit>()
            .Property(x => x.ShortName)
            .HasMaxLength(20)
            .IsRequired();

        builder.Entity<Product>()
            .Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Entity<Product>()
            .Property(x => x.CarbonFactor)
            .HasColumnType("decimal(10,2)");

        builder.Entity<Product>()
            .HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockItem>()
            .Property(x => x.Quantity)
            .HasColumnType("decimal(10,2)");

        builder.Entity<StockItem>()
            .Property(x => x.ImageUrl)
            .HasMaxLength(500);

        builder.Entity<StockItem>()
            .Property(x => x.Note)
            .HasMaxLength(500);

        builder.Entity<StockItem>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockItem>()
            .HasOne(x => x.Product)
            .WithMany(x => x.StockItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockItem>()
            .HasOne(x => x.Unit)
            .WithMany(x => x.StockItems)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WasteRiskPrediction>()
            .Property(x => x.RecommendationText)
            .HasMaxLength(500);

        builder.Entity<WasteRiskPrediction>()
            .HasOne(x => x.StockItem)
            .WithMany(x => x.WasteRiskPredictions)
            .HasForeignKey(x => x.StockItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Recipe>()
            .Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Entity<Recipe>()
            .Property(x => x.Description)
            .HasMaxLength(500);

        builder.Entity<Recipe>()
            .Property(x => x.Instructions)
            .HasMaxLength(3000)
            .IsRequired();

        builder.Entity<Recipe>()
            .Property(x => x.ImageUrl)
            .HasMaxLength(500);

        builder.Entity<Recipe>()
            .Property(x => x.ExternalApiId)
            .HasMaxLength(100);

        builder.Entity<RecipeIngredient>()
            .Property(x => x.Quantity)
            .HasColumnType("decimal(10,2)");

        builder.Entity<RecipeIngredient>()
            .HasOne(x => x.Recipe)
            .WithMany(x => x.RecipeIngredients)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RecipeIngredient>()
            .HasOne(x => x.Product)
            .WithMany(x => x.RecipeIngredients)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RecipeIngredient>()
            .HasOne(x => x.Unit)
            .WithMany(x => x.RecipeIngredients)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RecipeRecommendation>()
            .Property(x => x.RecommendationReason)
            .HasMaxLength(500);

        builder.Entity<RecipeRecommendation>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RecipeRecommendation>()
            .HasOne(x => x.StockItem)
            .WithMany(x => x.RecipeRecommendations)
            .HasForeignKey(x => x.StockItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RecipeRecommendation>()
            .HasOne(x => x.Recipe)
            .WithMany(x => x.RecipeRecommendations)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DeliveryPoint>()
            .Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Entity<DeliveryPoint>()
            .Property(x => x.Description)
            .HasMaxLength(500);

        builder.Entity<DeliveryPoint>()
            .Property(x => x.Neighborhood)
            .HasMaxLength(100);

        builder.Entity<DeliveryPoint>()
            .Property(x => x.WorkingHours)
            .HasMaxLength(100);

        builder.Entity<DeliveryPoint>()
            .Property(x => x.StorageType)
            .HasMaxLength(100);

        builder.Entity<ShareListing>()
            .Property(x => x.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Entity<ShareListing>()
            .Property(x => x.Description)
            .HasMaxLength(500);

        builder.Entity<ShareListing>()
            .Property(x => x.Quantity)
            .HasColumnType("decimal(10,2)");

        builder.Entity<ShareListing>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.DonorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ShareListing>()
            .HasOne(x => x.StockItem)
            .WithMany(x => x.ShareListings)
            .HasForeignKey(x => x.StockItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ShareListing>()
            .HasOne(x => x.DeliveryPoint)
            .WithMany(x => x.ShareListings)
            .HasForeignKey(x => x.DeliveryPointId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ShareRequest>()
            .Property(x => x.MatchScore)
            .HasColumnType("decimal(10,2)");

        builder.Entity<ShareRequest>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.RequesterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ShareRequest>()
            .HasOne(x => x.ShareListing)
            .WithMany(x => x.ShareRequests)
            .HasForeignKey(x => x.ShareListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Delivery>()
            .Property(x => x.QrToken)
            .HasMaxLength(200)
            .IsRequired();

        builder.Entity<Delivery>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.DonorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Delivery>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ReceiverUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Delivery>()
            .HasOne(x => x.ShareListing)
            .WithOne(x => x.Delivery)
            .HasForeignKey<Delivery>(x => x.ShareListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Delivery>()
            .HasOne(x => x.ShareRequest)
            .WithOne(x => x.Delivery)
            .HasForeignKey<Delivery>(x => x.ShareRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Delivery>()
            .HasOne(x => x.DeliveryPoint)
            .WithMany(x => x.Deliveries)
            .HasForeignKey(x => x.DeliveryPointId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Notification>()
            .Property(x => x.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Entity<Notification>()
            .Property(x => x.Message)
            .HasMaxLength(500)
            .IsRequired();

        builder.Entity<Notification>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CarbonReport>()
            .Property(x => x.SavedFoodKg)
            .HasColumnType("decimal(10,2)");

        builder.Entity<CarbonReport>()
            .Property(x => x.EstimatedCarbonSaved)
            .HasColumnType("decimal(10,2)");

        builder.Entity<CarbonReport>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        // DeliveryBox ayarları: Her kutu bir teslim noktasına bağlıdır.
        // QrCodeValue benzersizdir; aynı QR değeri iki kutuda kullanılmaz.
        builder.Entity<DeliveryBox>()
            .Property(x => x.BoxCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Entity<DeliveryBox>()
            .Property(x => x.QrCodeValue)
            .HasMaxLength(200)
            .IsRequired();

        builder.Entity<DeliveryBox>()
            .Property(x => x.Description)
            .HasMaxLength(300);

        builder.Entity<DeliveryBox>()
            .HasIndex(x => x.QrCodeValue)
            .IsUnique();

        builder.Entity<DeliveryBox>()
            .HasOne(x => x.DeliveryPoint)
            .WithMany(x => x.DeliveryBoxes)
            .HasForeignKey(x => x.DeliveryPointId)
            .OnDelete(DeleteBehavior.Restrict);

        // Delivery kaydı opsiyonel olarak bir teslim kutusuna bağlanır.
        builder.Entity<Delivery>()
            .HasOne(x => x.DeliveryBox)
            .WithMany(x => x.Deliveries)
            .HasForeignKey(x => x.DeliveryBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Delivery>()
            .Property(x => x.DropOffImageUrl)
            .HasMaxLength(500);
    }
}