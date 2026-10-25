using System;
using System.Collections.Generic;
using MySqlConnector;
using OopsGrocery.Models;

namespace OopsGrocery.Services;

public static class ProductService
{
    /// <summary>
    /// Loads all inventory items from the MySQL Products table, falling back to initial catalog if database is unreachable.
    /// </summary>
    public static List<ProductItem> GetAllProducts(out bool loadedFromDatabase, out string? errorMessage)
    {
        loadedFromDatabase = false;
        errorMessage = null;

        try
        {
            var connStr = DatabaseInitializer.ResolveConnectionString();
            DatabaseInitializer.EnsureDatabaseInitialized(connStr);

            using var connection = new MySqlConnection(connStr);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                SELECT `Id`, `Sku`, `Name`, `Category`, `Price`, `Unit`, `StockQuantity`, `MinimumStock`, `IsArchived`, `Tag`, `ImagePath`, `CreatedAt`, `UpdatedAt`
                FROM `Products`
                ORDER BY `Id` ASC;
                """;

            using var reader = cmd.ExecuteReader();
            var products = new List<ProductItem>();

            while (reader.Read())
            {
                var id = reader.GetInt32("Id");
                var sku = reader.IsDBNull(reader.GetOrdinal("Sku")) ? $"PRD-{id}" : reader.GetString("Sku");
                var name = reader.GetString("Name");
                var category = reader.GetString("Category");
                var price = reader.GetDecimal("Price");
                var unit = reader.IsDBNull(reader.GetOrdinal("Unit")) ? "pc" : reader.GetString("Unit");
                var qty = reader.GetInt32("StockQuantity");

                int minStock = ProductItem.DefaultMinimumStock;
                try
                {
                    var minStockOrdinal = reader.GetOrdinal("MinimumStock");
                    if (!reader.IsDBNull(minStockOrdinal))
                    {
                        minStock = reader.GetInt32(minStockOrdinal);
                    }
                }
                catch { }

                bool isArchived = false;
                try
                {
                    var isArchivedOrdinal = reader.GetOrdinal("IsArchived");
                    if (!reader.IsDBNull(isArchivedOrdinal))
                    {
                        isArchived = reader.GetBoolean(isArchivedOrdinal);
                    }
                }
                catch { }

                var tag = reader.IsDBNull(reader.GetOrdinal("Tag")) ? null : reader.GetString("Tag");
                var imagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? null : reader.GetString("ImagePath");
                var createdAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.UtcNow : reader.GetDateTime("CreatedAt");
                var updatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? DateTime.UtcNow : reader.GetDateTime("UpdatedAt");

                // Evaluate tag based on minimum stock threshold of 20
                if (string.IsNullOrWhiteSpace(tag))
                {
                    tag = isArchived ? "Archived" : (qty <= 0 ? "Out of Stock" : (qty <= minStock ? "Low Stock" : "In Stock"));
                }

                products.Add(new ProductItem
                {
                    Id = id,
                    Sku = sku,
                    Name = name,
                    Category = category,
                    Price = price,
                    Unit = unit,
                    StockQuantity = qty,
                    MinimumStock = minStock,
                    IsArchived = isArchived,
                    Tag = tag,
                    ImagePath = imagePath,
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt
                });
            }

            loadedFromDatabase = true;
            return products;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return GetFallbackProducts();
        }
    }

    /// <summary>
    /// Inserts a new product item into the MySQL Products table and updates product.Id and product.Sku.
    /// </summary>
    public static bool AddProduct(ProductItem product, out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            var connStr = DatabaseInitializer.ResolveConnectionString();
            using var connection = new MySqlConnection(connStr);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO `Products` (`Sku`, `Name`, `Category`, `Price`, `Unit`, `StockQuantity`, `MinimumStock`, `IsArchived`, `Tag`, `ImagePath`, `CreatedAt`, `UpdatedAt`)
                VALUES (@Sku, @Name, @Category, @Price, @Unit, @StockQuantity, @MinimumStock, @IsArchived, @Tag, @ImagePath, NOW(), NOW());
                SELECT LAST_INSERT_ID();
                """;

            var placeholderSku = string.IsNullOrWhiteSpace(product.Sku) ? $"PRD-TEMP-{Guid.NewGuid():N}" : product.Sku;
            var minStock = product.MinimumStock > 0 ? product.MinimumStock : ProductItem.DefaultMinimumStock;

            cmd.Parameters.AddWithValue("@Sku", placeholderSku);
            cmd.Parameters.AddWithValue("@Name", product.Name);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Price", product.Price);
            cmd.Parameters.AddWithValue("@Unit", product.Unit);
            cmd.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
            cmd.Parameters.AddWithValue("@MinimumStock", minStock);
            cmd.Parameters.AddWithValue("@IsArchived", product.IsArchived ? 1 : 0);
            cmd.Parameters.AddWithValue("@Tag", string.IsNullOrWhiteSpace(product.Tag) ? (object)DBNull.Value : product.Tag);
            cmd.Parameters.AddWithValue("@ImagePath", string.IsNullOrWhiteSpace(product.ImagePath) ? (object)DBNull.Value : product.ImagePath);

            var newId = Convert.ToInt32(cmd.ExecuteScalar());
            product.Id = newId;

            if (string.IsNullOrWhiteSpace(product.Sku) || product.Sku.StartsWith("PRD-TEMP", StringComparison.OrdinalIgnoreCase))
            {
                product.Sku = $"PRD-{newId}";
                using var updateSkuCmd = connection.CreateCommand();
                updateSkuCmd.CommandText = "UPDATE `Products` SET `Sku` = @Sku WHERE `Id` = @Id;";
                updateSkuCmd.Parameters.AddWithValue("@Sku", product.Sku);
                updateSkuCmd.Parameters.AddWithValue("@Id", newId);
                updateSkuCmd.ExecuteNonQuery();
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Updates product price, image path, and modification timestamp in MySQL.
    /// </summary>
    public static bool UpdateProduct(ProductItem product, out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            var connStr = DatabaseInitializer.ResolveConnectionString();
            using var connection = new MySqlConnection(connStr);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                UPDATE `Products`
                SET `Price` = @Price,
                    `ImagePath` = @ImagePath,
                    `UpdatedAt` = NOW()
                WHERE `Id` = @Id;
                """;

            cmd.Parameters.AddWithValue("@Price", product.Price);
            cmd.Parameters.AddWithValue("@ImagePath", string.IsNullOrWhiteSpace(product.ImagePath) ? (object)DBNull.Value : product.ImagePath);
            cmd.Parameters.AddWithValue("@Id", product.Id);

            var affected = cmd.ExecuteNonQuery();
            return affected > 0;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Adds quantity to the existing stock balance in the MySQL database.
    /// </summary>
    public static bool RestockProduct(int productId, int additionalQuantity, out int newStockQuantity, out string? errorMessage)
    {
        newStockQuantity = 0;
        errorMessage = null;

        try
        {
            var connStr = DatabaseInitializer.ResolveConnectionString();
            using var connection = new MySqlConnection(connStr);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                UPDATE `Products`
                SET `StockQuantity` = `StockQuantity` + @AddedQty,
                    `Tag` = CASE
                        WHEN (`StockQuantity` + @AddedQty) <= 0 THEN 'Out of Stock'
                        WHEN (`StockQuantity` + @AddedQty) <= IFNULL(`MinimumStock`, 20) THEN 'Low Stock'
                        ELSE 'In Stock'
                    END,
                    `UpdatedAt` = NOW()
                WHERE `Id` = @Id;

                SELECT `StockQuantity` FROM `Products` WHERE `Id` = @Id;
                """;

            cmd.Parameters.AddWithValue("@AddedQty", additionalQuantity);
            cmd.Parameters.AddWithValue("@Id", productId);

            var result = cmd.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                newStockQuantity = Convert.ToInt32(result);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Sets product archived status in MySQL Products table.
    /// </summary>
    public static bool ArchiveProduct(int productId, bool isArchived, out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            var connStr = DatabaseInitializer.ResolveConnectionString();
            using var connection = new MySqlConnection(connStr);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                UPDATE `Products`
                SET `IsArchived` = @IsArchived,
                    `Tag` = CASE
                        WHEN @IsArchived = 1 THEN 'Archived'
                        WHEN `StockQuantity` <= 0 THEN 'Out of Stock'
                        WHEN `StockQuantity` <= IFNULL(`MinimumStock`, 20) THEN 'Low Stock'
                        ELSE 'In Stock'
                    END,
                    `UpdatedAt` = NOW()
                WHERE `Id` = @Id;
                """;

            cmd.Parameters.AddWithValue("@IsArchived", isArchived ? 1 : 0);
            cmd.Parameters.AddWithValue("@Id", productId);

            return cmd.ExecuteNonQuery() > 0;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Deletes a product from the database (optional utility).
    /// </summary>
    public static bool DeleteProduct(int productId, out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            var connStr = DatabaseInitializer.ResolveConnectionString();
            using var connection = new MySqlConnection(connStr);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM `Products` WHERE `Id` = @Id;";
            cmd.Parameters.AddWithValue("@Id", productId);

            return cmd.ExecuteNonQuery() > 0;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Seed fallback grocery items across all 11 user categories used when MySQL is offline.
    /// Minimum stock is 20 for all products.
    /// </summary>
    public static List<ProductItem> GetFallbackProducts()
    {
        return
        [
            // Beverages (🥤)
            new() { Id = 101, Sku = "BEV-101", Name = "Cold-Pressed Orange Juice (1L)", Category = "Beverages", Price = 165.00m, Unit = "btl", StockQuantity = 64, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 102, Sku = "BEV-102", Name = "Benguet Arabica Roasted Coffee (250g)", Category = "Beverages", Price = 295.00m, Unit = "pouch", StockQuantity = 34, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 103, Sku = "BEV-103", Name = "C2 Cool & Clean Apple Green Tea (500ml)", Category = "Beverages", Price = 38.00m, Unit = "btl", StockQuantity = 85, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 104, Sku = "BEV-104", Name = "San Miguel Pale Pilsen Can (330ml)", Category = "Beverages", Price = 65.00m, Unit = "can", StockQuantity = 11, MinimumStock = 20, Tag = "Low Stock" },
            new() { Id = 105, Sku = "BEV-105", Name = "Coca-Cola Original Taste (1.5L)", Category = "Beverages", Price = 82.00m, Unit = "btl", StockQuantity = 0, MinimumStock = 20, Tag = "Out of Stock" },

            // Canned Goods (🥫)
            new() { Id = 106, Sku = "CAN-106", Name = "Century Tuna Flakes in Vegetable Oil (180g)", Category = "Canned Goods", Price = 48.50m, Unit = "can", StockQuantity = 130, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 107, Sku = "CAN-107", Name = "Purefoods Star Corned Beef (260g)", Category = "Canned Goods", Price = 89.00m, Unit = "can", StockQuantity = 95, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 108, Sku = "CAN-108", Name = "Ligo Sardines in Rich Tomato Sauce (155g)", Category = "Canned Goods", Price = 32.00m, Unit = "can", StockQuantity = 7, MinimumStock = 20, Tag = "Low Stock" },
            new() { Id = 109, Sku = "CAN-109", Name = "Jolly Whole Kernel Golden Sweet Corn (425g)", Category = "Canned Goods", Price = 58.00m, Unit = "can", StockQuantity = 88, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 110, Sku = "CAN-110", Name = "Spam Classic Premium Luncheon Meat (340g)", Category = "Canned Goods", Price = 195.00m, Unit = "can", StockQuantity = 0, MinimumStock = 20, Tag = "Out of Stock" },

            // Condiments (🧂)
            new() { Id = 111, Sku = "CND-111", Name = "Datu Puti Soy Sauce & Spiced Vinegar Twin Pack", Category = "Condiments", Price = 78.00m, Unit = "pack", StockQuantity = 115, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 112, Sku = "CND-112", Name = "UFC Tamis Anghang Banana Ketchup (550g)", Category = "Condiments", Price = 42.00m, Unit = "btl", StockQuantity = 72, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 113, Sku = "CND-113", Name = "Silver Swan Special Soy Sauce (1L)", Category = "Condiments", Price = 68.00m, Unit = "btl", StockQuantity = 14, MinimumStock = 20, Tag = "Low Stock" },
            new() { Id = 114, Sku = "CND-114", Name = "Minola Pure Coconut Cooking Oil (1L Pouch)", Category = "Condiments", Price = 135.00m, Unit = "pouch", StockQuantity = 45, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 115, Sku = "CND-115", Name = "Mang Tomas All-Around Sarsa (325g)", Category = "Condiments", Price = 36.50m, Unit = "btl", StockQuantity = 0, MinimumStock = 20, Tag = "Out of Stock" },

            // Dairy (🥛)
            new() { Id = 116, Sku = "DAR-116", Name = "Magnolia Pure Fresh Whole Milk (1L)", Category = "Dairy", Price = 98.00m, Unit = "btl", StockQuantity = 74, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 117, Sku = "DAR-117", Name = "Eden Original Processed Cheese Block (165g)", Category = "Dairy", Price = 65.00m, Unit = "box", StockQuantity = 52, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 118, Sku = "DAR-118", Name = "Farm Fresh Grade A Large Brown Eggs (1 Doz)", Category = "Dairy", Price = 195.00m, Unit = "tray", StockQuantity = 9, MinimumStock = 20, Tag = "Low Stock" },
            new() { Id = 119, Sku = "DAR-119", Name = "Magnolia Gold Pure Butter Salted (225g)", Category = "Dairy", Price = 145.00m, Unit = "bar", StockQuantity = 38, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 120, Sku = "DAR-120", Name = "Nestle All-Purpose Cream (250ml)", Category = "Dairy", Price = 72.00m, Unit = "pack", StockQuantity = 0, MinimumStock = 20, Tag = "Out of Stock" },

            // Fruits & Vegetables (🍎)
            new() { Id = 121, Sku = "FRV-121", Name = "Organic Honeycrisp Sweet Apples", Category = "Fruits & Vegetables", Price = 165.00m, Unit = "kg", StockQuantity = 145, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 122, Sku = "FRV-122", Name = "Cavendish Sweet Yellow Bananas", Category = "Fruits & Vegetables", Price = 85.50m, Unit = "kg", StockQuantity = 120, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 123, Sku = "FRV-123", Name = "Red Native Onion Bulbs (1kg Pack)", Category = "Fruits & Vegetables", Price = 110.00m, Unit = "kg", StockQuantity = 12, MinimumStock = 20, Tag = "Low Stock" },
            new() { Id = 124, Sku = "FRV-124", Name = "Carabao Guimaras Sweet Mangoes", Category = "Fruits & Vegetables", Price = 180.00m, Unit = "kg", StockQuantity = 80, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 125, Sku = "FRV-125", Name = "Baguio Crisp Romaine Lettuce", Category = "Fruits & Vegetables", Price = 95.00m, Unit = "head", StockQuantity = 0, MinimumStock = 20, Tag = "Out of Stock" },

            // Household (🧼)
            new() { Id = 126, Sku = "HSH-126", Name = "Safeguard Pure White Family Soap (175g)", Category = "Household", Price = 55.00m, Unit = "bar", StockQuantity = 160, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 127, Sku = "HSH-127", Name = "Surf Cherry Blossom Detergent Powder (2.2kg)", Category = "Household", Price = 245.00m, Unit = "pack", StockQuantity = 45, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 128, Sku = "HSH-128", Name = "Scotch-Brite Heavy Duty Scrub Sponge (Pack of 3)", Category = "Household", Price = 92.00m, Unit = "pack", StockQuantity = 14, MinimumStock = 20, Tag = "Low Stock" },
            new() { Id = 129, Sku = "HSH-129", Name = "Zonrox Original Multi-Clean Bleach (1L)", Category = "Household", Price = 64.00m, Unit = "btl", StockQuantity = 90, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 130, Sku = "HSH-130", Name = "Downy Sunrise Fresh Fabric Conditioner (800ml)", Category = "Household", Price = 175.00m, Unit = "pouch", StockQuantity = 0, MinimumStock = 20, Tag = "Out of Stock" },

            // Instant Noodles (🍜)
            new() { Id = 131, Sku = "NDL-131", Name = "Lucky Me! Pancit Canton Kalamansi (80g)", Category = "Instant Noodles", Price = 16.50m, Unit = "pouch", StockQuantity = 220, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 132, Sku = "NDL-132", Name = "Lucky Me! Instant Beef Mami Soup (55g)", Category = "Instant Noodles", Price = 15.00m, Unit = "pouch", StockQuantity = 180, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 133, Sku = "NDL-133", Name = "Nissin Cup Noodles Seafood Flavor (40g)", Category = "Instant Noodles", Price = 38.00m, Unit = "cup", StockQuantity = 8, MinimumStock = 20, Tag = "Low Stock" },
            new() { Id = 134, Sku = "NDL-134", Name = "Indomie Mi Goreng Instant Noodles (85g)", Category = "Instant Noodles", Price = 22.00m, Unit = "pouch", StockQuantity = 140, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 135, Sku = "NDL-135", Name = "Payless Extra Big Pancit Canton Sweet & Spicy (130g)", Category = "Instant Noodles", Price = 24.00m, Unit = "pouch", StockQuantity = 0, MinimumStock = 20, Tag = "Out of Stock" },

            // Meat (🥩)
            new() { Id = 136, Sku = "MEA-136", Name = "Choice Lean Ground Beef (1kg)", Category = "Meat", Price = 385.00m, Unit = "kg", StockQuantity = 42, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 137, Sku = "MEA-137", Name = "Magnolia Fresh Chicken Breast Fillet (1kg)", Category = "Meat", Price = 260.00m, Unit = "kg", StockQuantity = 50, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 138, Sku = "MEA-138", Name = "Fresh Tender Pork Chop Cuts (1kg)", Category = "Meat", Price = 320.00m, Unit = "kg", StockQuantity = 6, MinimumStock = 20, Tag = "Low Stock" },
            new() { Id = 139, Sku = "MEA-139", Name = "Prime Australian Ribeye Steak (350g)", Category = "Meat", Price = 590.00m, Unit = "cut", StockQuantity = 18, MinimumStock = 20, Tag = "Low Stock" },
            new() { Id = 140, Sku = "MEA-140", Name = "Boneless Marinated Daing na Bangus (450g)", Category = "Meat", Price = 235.00m, Unit = "pack", StockQuantity = 0, MinimumStock = 20, Tag = "Out of Stock" },

            // Personal Care (🧴)
            new() { Id = 141, Sku = "PSC-141", Name = "Head & Shoulders Cool Menthol Anti-Dandruff (340ml)", Category = "Personal Care", Price = 285.00m, Unit = "btl", StockQuantity = 48, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 142, Sku = "PSC-142", Name = "Colgate Total Deep Clean Antibacterial Toothpaste (150g)", Category = "Personal Care", Price = 145.00m, Unit = "tube", StockQuantity = 95, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 143, Sku = "PSC-143", Name = "Palmolive Naturals Intensive Moisture Shampoo (180ml)", Category = "Personal Care", Price = 115.00m, Unit = "btl", StockQuantity = 10, MinimumStock = 20, Tag = "Low Stock" },
            new() { Id = 144, Sku = "PSC-144", Name = "Bioderm Antimicrobial Germicidal Family Soap (135g)", Category = "Personal Care", Price = 42.00m, Unit = "bar", StockQuantity = 110, MinimumStock = 20, Tag = "In Stock" },

            // Snacks (🍿)
            new() { Id = 145, Sku = "SNK-145", Name = "Jack 'n Jill Piattos Cheese Potato Crisps (85g)", Category = "Snacks", Price = 44.00m, Unit = "bag", StockQuantity = 125, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 146, Sku = "SNK-146", Name = "Oishi Prawn Crackers Spicy Flavor (90g)", Category = "Snacks", Price = 35.00m, Unit = "bag", StockQuantity = 75, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 147, Sku = "SNK-147", Name = "Nagaraya Cracker Nuts Original Garlic (160g)", Category = "Snacks", Price = 48.00m, Unit = "bag", StockQuantity = 13, MinimumStock = 20, Tag = "Low Stock" },
            new() { Id = 148, Sku = "SNK-148", Name = "Choc Nut Peanut Milk Chocolate (Box of 24)", Category = "Snacks", Price = 78.00m, Unit = "box", StockQuantity = 60, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 149, Sku = "SNK-149", Name = "SkyFlakes Classic Crackers (Pack of 10)", Category = "Snacks", Price = 62.00m, Unit = "pack", StockQuantity = 0, MinimumStock = 20, Tag = "Out of Stock" },

            // Other (📦)
            new() { Id = 150, Sku = "OTH-150", Name = "Oops Heavy-Duty Reusable Grocery Eco Tote Bag", Category = "Other", Price = 45.00m, Unit = "pc", StockQuantity = 85, MinimumStock = 20, Tag = "In Stock" },
            new() { Id = 151, Sku = "OTH-151", Name = "Food-Grade Clear Ice Storage Bags (100 pcs)", Category = "Other", Price = 55.00m, Unit = "pack", StockQuantity = 5, MinimumStock = 20, Tag = "Low Stock" }
        ];
    }
}
