using System;
using System.IO;
using System.Text.Json;
using MySqlConnector;

namespace OopsGrocery.Services;

public static class DatabaseInitializer
{
    private const string DefaultConnectionString =
        "Server=localhost;Port=3306;Database=OopsGrocery;User ID=root;Password=;SslMode=None;AllowUserVariables=True;";

    private static readonly string[] CandidatePasswords = ["", "root", "admin", "123456"];

    public static string ResolveConnectionString()
    {
        // 1. Check environment variable
        var envString = Environment.GetEnvironmentVariable("OOPSGROCERY_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(envString))
        {
            return envString;
        }

        // 2. Check local appsettings.json if present
        var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (File.Exists(appSettingsPath))
        {
            try
            {
                var json = File.ReadAllText(appSettingsPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connStrings) &&
                    connStrings.TryGetProperty("DefaultConnection", out var defaultConn))
                {
                    var parsed = defaultConn.GetString();
                    if (!string.IsNullOrWhiteSpace(parsed)) return parsed;
                }
            }
            catch
            {
                // Fall back to default candidate connection
            }
        }

        return DefaultConnectionString;
    }

    /// <summary>
    /// Ensures the OopsGrocery database, Users table, Products table, and initial seed records exist.
    /// This allows the application to run smoothly even if the SQL file was not manually imported on a new device.
    /// </summary>
    public static void EnsureDatabaseInitialized(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var targetDatabase = string.IsNullOrWhiteSpace(builder.Database) ? "OopsGrocery" : builder.Database;

        // Try connecting directly to the target database first
        try
        {
            using var testConn = new MySqlConnection(builder.ConnectionString);
            testConn.Open();

            EnsureUsersTableInitialized(testConn);
            EnsureProductsTableInitialized(testConn);
            EnsureTransactionsTablesInitialized(testConn);
            return;
        }
        catch (MySqlException ex) when (ex.Number is 1049 or 1146) // 1049: Unknown database, 1146: Table doesn't exist
        {
            // Database or table missing; continue to full setup
        }
        catch (MySqlException ex) when (ex.Number is 1045) // Access denied (wrong password)
        {
            // Try fallback candidate passwords if using localhost root
            if (builder.Server.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
                builder.UserID.Equals("root", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var pwd in CandidatePasswords)
                {
                    builder.Password = pwd;
                    try
                    {
                        using var fallbackConn = new MySqlConnection(builder.ConnectionString);
                        fallbackConn.Open();
                        // Found working password!
                        EnsureDatabaseInitialized(builder.ConnectionString);
                        return;
                    }
                    catch
                    {
                        // Continue checking candidates
                    }
                }
            }
            throw;
        }

        // Connect to server without database to create OopsGrocery
        var serverBuilder = new MySqlConnectionStringBuilder(builder.ConnectionString)
        {
            Database = string.Empty
        };

        using var serverConn = new MySqlConnection(serverBuilder.ConnectionString);
        serverConn.Open();

        using (var createDbCmd = serverConn.CreateCommand())
        {
            createDbCmd.CommandText = $"""
                CREATE DATABASE IF NOT EXISTS `{targetDatabase}`
                    CHARACTER SET utf8mb4
                    COLLATE utf8mb4_unicode_ci;
                """;
            createDbCmd.ExecuteNonQuery();
        }

        serverConn.ChangeDatabase(targetDatabase);

        EnsureUsersTableInitialized(serverConn);
        EnsureProductsTableInitialized(serverConn);
        EnsureTransactionsTablesInitialized(serverConn);
    }

    public static void EnsureUsersTableInitialized(MySqlConnection connection)
    {
        using (var createTableCmd = connection.CreateCommand())
        {
            createTableCmd.CommandText = """
                CREATE TABLE IF NOT EXISTS `Users` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `Username` VARCHAR(100) NOT NULL,
                    `FullName` VARCHAR(150) NULL DEFAULT NULL,
                    `PasswordHash` VARCHAR(255) NOT NULL,
                    `Role` ENUM('Admin', 'Cashier') NOT NULL DEFAULT 'Cashier',
                    `LastLogin` DATETIME NULL DEFAULT NULL,
                    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
                    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (`Id`),
                    UNIQUE KEY `UQ_Users_Username` (`Username`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                """;
            createTableCmd.ExecuteNonQuery();
        }

        EnsureFullNameColumnExists(connection);

        using var countCmd = connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM `Users`;";
        var count = Convert.ToInt32(countCmd.ExecuteScalar());
        if (count == 0)
        {
            SeedDefaultUsers(connection);
        }
    }

    public static void EnsureProductsTableInitialized(MySqlConnection connection)
    {
        using (var createTableCmd = connection.CreateCommand())
        {
            createTableCmd.CommandText = """
                CREATE TABLE IF NOT EXISTS `Products` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `Sku` VARCHAR(50) NOT NULL,
                    `Name` VARCHAR(200) NOT NULL,
                    `Category` VARCHAR(100) NOT NULL,
                    `Price` DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
                    `Unit` VARCHAR(30) NOT NULL DEFAULT 'pc',
                    `StockQuantity` INT NOT NULL DEFAULT 0,
                    `MinimumStock` INT NOT NULL DEFAULT 20,
                    `IsArchived` TINYINT(1) NOT NULL DEFAULT 0,
                    `Tag` VARCHAR(50) NULL DEFAULT NULL,
                    `ImagePath` VARCHAR(500) NULL DEFAULT NULL,
                    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    `UpdatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    PRIMARY KEY (`Id`),
                    UNIQUE KEY `UQ_Products_Sku` (`Sku`),
                    KEY `IX_Products_Category` (`Category`),
                    KEY `IX_Products_Name` (`Name`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                """;
            createTableCmd.ExecuteNonQuery();
        }

        EnsureMinimumStockColumnExists(connection);
        EnsureIsArchivedColumnExists(connection);

        using var countCmd = connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM `Products`;";
        var count = Convert.ToInt32(countCmd.ExecuteScalar());
        if (count == 0)
        {
            SeedDefaultProducts(connection);
        }
    }

    private static void EnsureIsArchivedColumnExists(MySqlConnection connection)
    {
        try
        {
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SHOW COLUMNS FROM `Products` LIKE 'IsArchived';";
            var exists = checkCmd.ExecuteScalar();
            if (exists == null)
            {
                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE `Products` ADD COLUMN `IsArchived` TINYINT(1) NOT NULL DEFAULT 0 AFTER `MinimumStock`;";
                alterCmd.ExecuteNonQuery();
            }
        }
        catch
        {
            // Table might not exist yet; safe to ignore
        }
    }

    private static void EnsureMinimumStockColumnExists(MySqlConnection connection)
    {
        try
        {
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SHOW COLUMNS FROM `Products` LIKE 'MinimumStock';";
            var exists = checkCmd.ExecuteScalar();
            if (exists == null)
            {
                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE `Products` ADD COLUMN `MinimumStock` INT NOT NULL DEFAULT 20 AFTER `StockQuantity`;";
                alterCmd.ExecuteNonQuery();
            }

            // Enforce minimum stock of 20 and update tags for all products
            using var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = """
                UPDATE `Products`
                SET `MinimumStock` = 20
                WHERE `MinimumStock` IS NULL OR `MinimumStock` = 0;

                UPDATE `Products`
                SET `Tag` = CASE
                    WHEN `StockQuantity` <= 0 THEN 'Out of Stock'
                    WHEN `StockQuantity` <= IFNULL(`MinimumStock`, 20) THEN 'Low Stock'
                    ELSE 'In Stock'
                END;
                """;
            updateCmd.ExecuteNonQuery();
        }
        catch
        {
            // Table might not exist yet; safe to ignore
        }
    }

    private static void EnsureFullNameColumnExists(MySqlConnection connection)
    {
        try
        {
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SHOW COLUMNS FROM `Users` LIKE 'FullName';";
            var exists = checkCmd.ExecuteScalar();
            if (exists == null)
            {
                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE `Users` ADD COLUMN `FullName` VARCHAR(150) NULL DEFAULT NULL AFTER `Username`;";
                alterCmd.ExecuteNonQuery();
            }

            // Update default full names for admin and cashier if they are missing
            using var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = """
                UPDATE `Users` SET `FullName` = 'Administrator' WHERE `Username` = 'admin' AND (`FullName` IS NULL OR `FullName` = '');
                UPDATE `Users` SET `FullName` = 'Store Cashier' WHERE `Username` = 'cashier' AND (`FullName` IS NULL OR `FullName` = '');
                """;
            updateCmd.ExecuteNonQuery();
        }
        catch
        {
            // Table might not exist yet or connection has different privileges; ignore safely
        }
    }

    private static void SeedDefaultUsers(MySqlConnection connection)
    {
        using var seedCmd = connection.CreateCommand();
        seedCmd.CommandText = """
            INSERT INTO `Users` (`Username`, `FullName`, `PasswordHash`, `Role`, `IsActive`)
            VALUES
            (
                'admin',
                'Administrator',
                'PBKDF2$100000$K9KuYx9Q5RwNvMHTBFwbuw==$SzFwVug/u+Hz2pZbQBRl20LHf16hulnbtU75uafQh1U=',
                'Admin',
                1
            ),
            (
                'cashier',
                'Store Cashier',
                'PBKDF2$100000$juEStoErFzuglex33oiGQA==$0llglOiRFCzuJdqRZjJ2ybYdgvQmQmnLD3cKXc5fnEA=',
                'Cashier',
                1
            )
            ON DUPLICATE KEY UPDATE
                `FullName` = IF(`FullName` IS NULL OR `FullName` = '', VALUES(`FullName`), `FullName`),
                `PasswordHash` = VALUES(`PasswordHash`),
                `Role` = VALUES(`Role`),
                `IsActive` = VALUES(`IsActive`);

            DELETE FROM `Users` WHERE `Username` IN ('user', 'customer');
            """;
        seedCmd.ExecuteNonQuery();
    }

    private static void SeedDefaultProducts(MySqlConnection connection)
    {
        using var seedCmd = connection.CreateCommand();
        seedCmd.CommandText = """
            INSERT INTO `Products` (`Sku`, `Name`, `Category`, `Price`, `Unit`, `StockQuantity`, `MinimumStock`, `Tag`, `ImagePath`)
            VALUES
            -- Beverages (🥤)
            ('BEV-101', 'Cold-Pressed Orange Juice (1L)', 'Beverages', 165.00, 'btl', 64, 20, 'In Stock', NULL),
            ('BEV-102', 'Benguet Arabica Roasted Coffee (250g)', 'Beverages', 295.00, 'pouch', 34, 20, 'In Stock', NULL),
            ('BEV-103', 'C2 Cool & Clean Apple Green Tea (500ml)', 'Beverages', 38.00, 'btl', 85, 20, 'In Stock', NULL),
            ('BEV-104', 'San Miguel Pale Pilsen Can (330ml)', 'Beverages', 65.00, 'can', 11, 20, 'Low Stock', NULL),
            ('BEV-105', 'Coca-Cola Original Taste (1.5L)', 'Beverages', 82.00, 'btl', 0, 20, 'Out of Stock', NULL),

            -- Canned Goods (🥫)
            ('CAN-106', 'Century Tuna Flakes in Vegetable Oil (180g)', 'Canned Goods', 48.50, 'can', 130, 20, 'In Stock', NULL),
            ('CAN-107', 'Purefoods Star Corned Beef (260g)', 'Canned Goods', 89.00, 'can', 95, 20, 'In Stock', NULL),
            ('CAN-108', 'Ligo Sardines in Rich Tomato Sauce (155g)', 'Canned Goods', 32.00, 'can', 7, 20, 'Low Stock', NULL),
            ('CAN-109', 'Jolly Whole Kernel Golden Sweet Corn (425g)', 'Canned Goods', 58.00, 'can', 88, 20, 'In Stock', NULL),
            ('CAN-110', 'Spam Classic Premium Luncheon Meat (340g)', 'Canned Goods', 195.00, 'can', 0, 20, 'Out of Stock', NULL),

            -- Condiments (🧂)
            ('CND-111', 'Datu Puti Soy Sauce & Spiced Vinegar Twin Pack', 'Condiments', 78.00, 'pack', 115, 20, 'In Stock', NULL),
            ('CND-112', 'UFC Tamis Anghang Banana Ketchup (550g)', 'Condiments', 42.00, 'btl', 72, 20, 'In Stock', NULL),
            ('CND-113', 'Silver Swan Special Soy Sauce (1L)', 'Condiments', 68.00, 'btl', 14, 20, 'Low Stock', NULL),
            ('CND-114', 'Minola Pure Coconut Cooking Oil (1L Pouch)', 'Condiments', 135.00, 'pouch', 45, 20, 'In Stock', NULL),
            ('CND-115', 'Mang Tomas All-Around Sarsa (325g)', 'Condiments', 36.50, 'btl', 0, 20, 'Out of Stock', NULL),

            -- Dairy (🥛)
            ('DAR-116', 'Magnolia Pure Fresh Whole Milk (1L)', 'Dairy', 98.00, 'btl', 74, 20, 'In Stock', NULL),
            ('DAR-117', 'Eden Original Processed Cheese Block (165g)', 'Dairy', 65.00, 'box', 52, 20, 'In Stock', NULL),
            ('DAR-118', 'Farm Fresh Grade A Large Brown Eggs (1 Doz)', 'Dairy', 195.00, 'tray', 9, 20, 'Low Stock', NULL),
            ('DAR-119', 'Magnolia Gold Pure Butter Salted (225g)', 'Dairy', 145.00, 'bar', 38, 20, 'In Stock', NULL),
            ('DAR-120', 'Nestle All-Purpose Cream (250ml)', 'Dairy', 72.00, 'pack', 0, 20, 'Out of Stock', NULL),

            -- Fruits & Vegetables (🍎)
            ('FRV-121', 'Organic Honeycrisp Sweet Apples', 'Fruits & Vegetables', 165.00, 'kg', 145, 20, 'In Stock', NULL),
            ('FRV-122', 'Cavendish Sweet Yellow Bananas', 'Fruits & Vegetables', 85.50, 'kg', 120, 20, 'In Stock', NULL),
            ('FRV-123', 'Red Native Onion Bulbs (1kg Pack)', 'Fruits & Vegetables', 110.00, 'kg', 12, 20, 'Low Stock', NULL),
            ('FRV-124', 'Carabao Guimaras Sweet Mangoes', 'Fruits & Vegetables', 180.00, 'kg', 80, 20, 'In Stock', NULL),
            ('FRV-125', 'Baguio Crisp Romaine Lettuce', 'Fruits & Vegetables', 95.00, 'head', 0, 20, 'Out of Stock', NULL),

            -- Household (🧼)
            ('HSH-126', 'Safeguard Pure White Family Soap (175g)', 'Household', 55.00, 'bar', 160, 20, 'In Stock', NULL),
            ('HSH-127', 'Surf Cherry Blossom Detergent Powder (2.2kg)', 'Household', 245.00, 'pack', 45, 20, 'In Stock', NULL),
            ('HSH-128', 'Scotch-Brite Heavy Duty Scrub Sponge (Pack of 3)', 'Household', 92.00, 'pack', 14, 20, 'Low Stock', NULL),
            ('HSH-129', 'Zonrox Original Multi-Clean Bleach (1L)', 'Household', 64.00, 'btl', 90, 20, 'In Stock', NULL),
            ('HSH-130', 'Downy Sunrise Fresh Fabric Conditioner (800ml)', 'Household', 175.00, 'pouch', 0, 20, 'Out of Stock', NULL),

            -- Instant Noodles (🍜)
            ('NDL-131', 'Lucky Me! Pancit Canton Kalamansi (80g)', 'Instant Noodles', 16.50, 'pouch', 220, 20, 'In Stock', NULL),
            ('NDL-132', 'Lucky Me! Instant Beef Mami Soup (55g)', 'Instant Noodles', 15.00, 'pouch', 180, 20, 'In Stock', NULL),
            ('NDL-133', 'Nissin Cup Noodles Seafood Flavor (40g)', 'Instant Noodles', 38.00, 'cup', 8, 20, 'Low Stock', NULL),
            ('NDL-134', 'Indomie Mi Goreng Instant Noodles (85g)', 'Instant Noodles', 22.00, 'pouch', 140, 20, 'In Stock', NULL),
            ('NDL-135', 'Payless Extra Big Pancit Canton Sweet & Spicy (130g)', 'Instant Noodles', 24.00, 'pouch', 0, 20, 'Out of Stock', NULL),

            -- Meat (🥩)
            ('MEA-136', 'Choice Lean Ground Beef (1kg)', 'Meat', 385.00, 'kg', 42, 20, 'In Stock', NULL),
            ('MEA-137', 'Magnolia Fresh Chicken Breast Fillet (1kg)', 'Meat', 260.00, 'kg', 50, 20, 'In Stock', NULL),
            ('MEA-138', 'Fresh Tender Pork Chop Cuts (1kg)', 'Meat', 320.00, 'kg', 6, 20, 'Low Stock', NULL),
            ('MEA-139', 'Prime Australian Ribeye Steak (350g)', 'Meat', 590.00, 'cut', 18, 20, 'Low Stock', NULL),
            ('MEA-140', 'Boneless Marinated Daing na Bangus (450g)', 'Meat', 235.00, 'pack', 0, 20, 'Out of Stock', NULL),

            -- Personal Care (🧴)
            ('PSC-141', 'Head & Shoulders Cool Menthol Anti-Dandruff (340ml)', 'Personal Care', 285.00, 'btl', 48, 20, 'In Stock', NULL),
            ('PSC-142', 'Colgate Total Deep Clean Antibacterial Toothpaste (150g)', 'Personal Care', 145.00, 'tube', 95, 20, 'In Stock', NULL),
            ('PSC-143', 'Palmolive Naturals Intensive Moisture Shampoo (180ml)', 'Personal Care', 115.00, 'btl', 10, 20, 'Low Stock', NULL),
            ('PSC-144', 'Bioderm Antimicrobial Germicidal Family Soap (135g)', 'Personal Care', 42.00, 'bar', 110, 20, 'In Stock', NULL),

            -- Snacks (🍿)
            ('SNK-145', 'Jack ''n Jill Piattos Cheese Potato Crisps (85g)', 'Snacks', 44.00, 'bag', 125, 20, 'In Stock', NULL),
            ('SNK-146', 'Oishi Prawn Crackers Spicy Flavor (90g)', 'Snacks', 35.00, 'bag', 75, 20, 'In Stock', NULL),
            ('SNK-147', 'Nagaraya Cracker Nuts Original Garlic (160g)', 'Snacks', 48.00, 'bag', 13, 20, 'Low Stock', NULL),
            ('SNK-148', 'Choc Nut Peanut Milk Chocolate (Box of 24)', 'Snacks', 78.00, 'box', 60, 20, 'In Stock', NULL),
            ('SNK-149', 'SkyFlakes Classic Crackers (Pack of 10)', 'Snacks', 62.00, 'pack', 0, 20, 'Out of Stock', NULL),

            -- Other (📦)
            ('OTH-150', 'Oops Heavy-Duty Reusable Grocery Eco Tote Bag', 'Other', 45.00, 'pc', 85, 20, 'In Stock', NULL),
            ('OTH-151', 'Food-Grade Clear Ice Storage Bags (100 pcs)', 'Other', 55.00, 'pack', 5, 20, 'Low Stock', NULL)
            ON DUPLICATE KEY UPDATE
                `Price` = VALUES(`Price`),
                `Unit` = VALUES(`Unit`),
                `StockQuantity` = VALUES(`StockQuantity`),
                `MinimumStock` = VALUES(`MinimumStock`),
                `Tag` = VALUES(`Tag`);
            """;
        seedCmd.ExecuteNonQuery();
    }

    public static void EnsureTransactionsTablesInitialized(MySqlConnection connection)
    {
        using (var createTxnCmd = connection.CreateCommand())
        {
            createTxnCmd.CommandText = """
                CREATE TABLE IF NOT EXISTS `transactions` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `ReceiptNumber` VARCHAR(50) NOT NULL,
                    `CashierName` VARCHAR(100) NOT NULL,
                    `StationName` VARCHAR(50) NOT NULL DEFAULT 'Register #01',
                    `TotalItems` INT NOT NULL DEFAULT 0,
                    `Subtotal` DECIMAL(12, 2) NOT NULL DEFAULT 0.00,
                    `Tax` DECIMAL(12, 2) NOT NULL DEFAULT 0.00,
                    `TotalAmount` DECIMAL(12, 2) NOT NULL DEFAULT 0.00,
                    `AmountTendered` DECIMAL(12, 2) NOT NULL DEFAULT 0.00,
                    `ChangeGiven` DECIMAL(12, 2) NOT NULL DEFAULT 0.00,
                    `PaymentMethod` VARCHAR(50) NOT NULL DEFAULT 'Cash',
                    `Status` VARCHAR(50) NOT NULL DEFAULT 'Completed',
                    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (`Id`),
                    UNIQUE KEY `UQ_Transactions_Receipt` (`ReceiptNumber`),
                    KEY `IX_Transactions_CreatedAt` (`CreatedAt`),
                    KEY `IX_Transactions_Cashier` (`CashierName`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                """;
            createTxnCmd.ExecuteNonQuery();
        }

        using (var createItemsCmd = connection.CreateCommand())
        {
            createItemsCmd.CommandText = """
                CREATE TABLE IF NOT EXISTS `transaction_items` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `TransactionId` INT NOT NULL,
                    `ProductId` INT NULL DEFAULT NULL,
                    `Sku` VARCHAR(50) NOT NULL,
                    `ProductName` VARCHAR(200) NOT NULL,
                    `Category` VARCHAR(100) NOT NULL,
                    `UnitPrice` DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
                    `Quantity` INT NOT NULL DEFAULT 1,
                    `TotalPrice` DECIMAL(12, 2) NOT NULL DEFAULT 0.00,
                    PRIMARY KEY (`Id`),
                    KEY `IX_TransactionItems_TxnId` (`TransactionId`),
                    KEY `IX_TransactionItems_Category` (`Category`),
                    KEY `IX_TransactionItems_Sku` (`Sku`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                """;
            createItemsCmd.ExecuteNonQuery();
        }
    }
}
