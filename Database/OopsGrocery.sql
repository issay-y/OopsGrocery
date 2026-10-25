-- =====================================================================
-- OopsGrocery All-In-One Database Setup Script
-- Compatible with MySQL (5.7, 8.0, 8.4+) and MariaDB (10.x, 11.x)
-- Ready for phpMyAdmin, MySQL Workbench, HeidiSQL, or CLI import
-- =====================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";

-- 1. Create and select Database (Prevents "No database selected" error)
CREATE DATABASE IF NOT EXISTS `oopsgrocery`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `oopsgrocery`;

-- 2. Create Users Table (Grocery POS Roles: Admin, Cashier)
CREATE TABLE IF NOT EXISTS `users` (
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

-- 3. Create Products Table (Inventory Catalog)
CREATE TABLE IF NOT EXISTS `products` (
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

-- 4. Seed Default Accounts
-- Default Admin Account:
--   Username: admin (strictly case-sensitive)
--   Password: Admin.94K (Compliant with 8-10 chars, uppercase, lowercase, digit, allowed special: .)
INSERT INTO `users` (`Username`, `FullName`, `PasswordHash`, `Role`, `IsActive`)
VALUES (
    'admin',
    'Administrator',
    'PBKDF2$100000$K9KuYx9Q5RwNvMHTBFwbuw==$SzFwVug/u+Hz2pZbQBRl20LHf16hulnbtU75uafQh1U=',
    'Admin',
    1
)
ON DUPLICATE KEY UPDATE
    `FullName` = VALUES(`FullName`),
    `PasswordHash` = VALUES(`PasswordHash`),
    `Role` = VALUES(`Role`),
    `IsActive` = VALUES(`IsActive`);

-- Default Cashier Account:
--   Username: cashier (strictly case-sensitive)
--   Password: Cash.71Ny (Compliant with 8-10 chars, uppercase, lowercase, digit, allowed special: .)
INSERT INTO `users` (`Username`, `FullName`, `PasswordHash`, `Role`, `IsActive`)
VALUES (
    'cashier',
    'Store Cashier',
    'PBKDF2$100000$juEStoErFzuglex33oiGQA==$0llglOiRFCzuJdqRZjJ2ybYdgvQmQmnLD3cKXc5fnEA=',
    'Cashier',
    1
)
ON DUPLICATE KEY UPDATE
    `FullName` = VALUES(`FullName`),
    `PasswordHash` = VALUES(`PasswordHash`),
    `Role` = VALUES(`Role`),
    `IsActive` = VALUES(`IsActive`);

-- Remove any old non-POS test accounts
DELETE FROM `users` WHERE `Username` IN ('user', 'customer');

-- 5. Seed Default Grocery Catalog Products
INSERT INTO `products` (`Sku`, `Name`, `Category`, `Price`, `Unit`, `StockQuantity`, `MinimumStock`, `Tag`, `ImagePath`)
VALUES
-- Beverages (🥤)
('BEV-101', 'Cold-Pressed Orange Juice (1L)', 'Beverages', 165.00, 'btl', 64, 20, 'Chilled', NULL),
('BEV-102', 'Benguet Arabica Roasted Coffee (250g)', 'Beverages', 295.00, 'pouch', 34, 20, 'Local Blend', NULL),
('BEV-103', 'C2 Cool & Clean Apple Green Tea (500ml)', 'Beverages', 38.00, 'btl', 85, 20, 'Best Seller', NULL),
('BEV-104', 'San Miguel Pale Pilsen Can (330ml)', 'Beverages', 65.00, 'can', 11, 20, 'Low Stock', NULL),
('BEV-105', 'Coca-Cola Original Taste (1.5L)', 'Beverages', 82.00, 'btl', 0, 20, 'Out of Stock', NULL),

-- Canned Goods (🥫)
('CAN-106', 'Century Tuna Flakes in Vegetable Oil (180g)', 'Canned Goods', 48.50, 'can', 130, 20, 'Staple', NULL),
('CAN-107', 'Purefoods Star Corned Beef (260g)', 'Canned Goods', 89.00, 'can', 95, 20, 'Pantry', NULL),
('CAN-108', 'Ligo Sardines in Rich Tomato Sauce (155g)', 'Canned Goods', 32.00, 'can', 7, 20, 'Low Stock', NULL),
('CAN-109', 'Jolly Whole Kernel Golden Sweet Corn (425g)', 'Canned Goods', 58.00, 'can', 88, 20, 'Pantry', NULL),
('CAN-110', 'Spam Classic Premium Luncheon Meat (340g)', 'Canned Goods', 195.00, 'can', 0, 20, 'Out of Stock', NULL),

-- Condiments (🧂)
('CND-111', 'Datu Puti Soy Sauce & Spiced Vinegar Twin Pack', 'Condiments', 78.00, 'pack', 115, 20, 'Essential', NULL),
('CND-112', 'UFC Tamis Anghang Banana Ketchup (550g)', 'Condiments', 42.00, 'btl', 72, 20, 'Pantry', NULL),
('CND-113', 'Silver Swan Special Soy Sauce (1L)', 'Condiments', 68.00, 'btl', 14, 20, 'Low Stock', NULL),
('CND-114', 'Minola Pure Coconut Cooking Oil (1L Pouch)', 'Condiments', 135.00, 'pouch', 45, 20, 'Cooking', NULL),
('CND-115', 'Mang Tomas All-Around Sarsa (325g)', 'Condiments', 36.50, 'btl', 0, 20, 'Out of Stock', NULL),

-- Dairy (🥛)
('DAR-116', 'Magnolia Pure Fresh Whole Milk (1L)', 'Dairy', 98.00, 'btl', 74, 20, 'Fresh Daily', NULL),
('DAR-117', 'Eden Original Processed Cheese Block (165g)', 'Dairy', 65.00, 'box', 52, 20, 'Popular', NULL),
('DAR-118', 'Farm Fresh Grade A Large Brown Eggs (1 Doz)', 'Dairy', 195.00, 'tray', 9, 20, 'Low Stock', NULL),
('DAR-119', 'Magnolia Gold Pure Butter Salted (225g)', 'Dairy', 145.00, 'bar', 38, 20, 'Chilled', NULL),
('DAR-120', 'Nestle All-Purpose Cream (250ml)', 'Dairy', 72.00, 'pack', 0, 20, 'Out of Stock', NULL),

-- Fruits & Vegetables (🍎)
('FRV-121', 'Organic Honeycrisp Sweet Apples', 'Fruits & Vegetables', 165.00, 'kg', 145, 20, 'Fresh Harvest', NULL),
('FRV-122', 'Cavendish Sweet Yellow Bananas', 'Fruits & Vegetables', 85.50, 'kg', 120, 20, 'Fresh Daily', NULL),
('FRV-123', 'Red Native Onion Bulbs (1kg Pack)', 'Fruits & Vegetables', 110.00, 'kg', 12, 20, 'Low Stock', NULL),
('FRV-124', 'Carabao Guimaras Sweet Mangoes', 'Fruits & Vegetables', 180.00, 'kg', 80, 20, 'Seasonal', NULL),
('FRV-125', 'Baguio Crisp Romaine Lettuce', 'Fruits & Vegetables', 95.00, 'head', 0, 20, 'Out of Stock', NULL),

-- Household (🧼)
('HSH-126', 'Safeguard Pure White Family Soap (175g)', 'Household', 55.00, 'bar', 160, 20, 'Clean', NULL),
('HSH-127', 'Surf Cherry Blossom Detergent Powder (2.2kg)', 'Household', 245.00, 'pack', 45, 20, 'Laundry', NULL),
('HSH-128', 'Scotch-Brite Heavy Duty Scrub Sponge (Pack of 3)', 'Household', 92.00, 'pack', 14, 20, 'Low Stock', NULL),
('HSH-129', 'Zonrox Original Multi-Clean Bleach (1L)', 'Household', 64.00, 'btl', 90, 20, 'Sanitize', NULL),
('HSH-130', 'Downy Sunrise Fresh Fabric Conditioner (800ml)', 'Household', 175.00, 'pouch', 0, 20, 'Out of Stock', NULL),

-- Instant Noodles (🍜)
('NDL-131', 'Lucky Me! Pancit Canton Kalamansi (80g)', 'Instant Noodles', 16.50, 'pouch', 220, 20, 'Top Seller', NULL),
('NDL-132', 'Lucky Me! Instant Beef Mami Soup (55g)', 'Instant Noodles', 15.00, 'pouch', 180, 20, 'Soup', NULL),
('NDL-133', 'Nissin Cup Noodles Seafood Flavor (40g)', 'Instant Noodles', 38.00, 'cup', 8, 20, 'Low Stock', NULL),
('NDL-134', 'Indomie Mi Goreng Instant Noodles (85g)', 'Instant Noodles', 22.00, 'pouch', 140, 20, 'Spicy', NULL),
('NDL-135', 'Payless Extra Big Pancit Canton Sweet & Spicy (130g)', 'Instant Noodles', 24.00, 'pouch', 0, 20, 'Out of Stock', NULL),

-- Meat (🥩)
('MEA-136', 'Choice Lean Ground Beef (1kg)', 'Meat', 385.00, 'kg', 42, 20, 'Fresh Cut', NULL),
('MEA-137', 'Magnolia Fresh Chicken Breast Fillet (1kg)', 'Meat', 260.00, 'kg', 50, 20, 'Lean', NULL),
('MEA-138', 'Fresh Tender Pork Chop Cuts (1kg)', 'Meat', 320.00, 'kg', 6, 20, 'Low Stock', NULL),
('MEA-139', 'Prime Australian Ribeye Steak (350g)', 'Meat', 590.00, 'cut', 18, 20, 'Low Stock', NULL),
('MEA-140', 'Boneless Marinated Daing na Bangus (450g)', 'Meat', 235.00, 'pack', 0, 20, 'Out of Stock', NULL),

-- Personal Care (🧴)
('PSC-141', 'Head & Shoulders Cool Menthol Anti-Dandruff (340ml)', 'Personal Care', 285.00, 'btl', 48, 20, 'Hair Care', NULL),
('PSC-142', 'Colgate Total Deep Clean Antibacterial Toothpaste (150g)', 'Personal Care', 145.00, 'tube', 95, 20, 'Dental', NULL),
('PSC-143', 'Palmolive Naturals Intensive Moisture Shampoo (180ml)', 'Personal Care', 115.00, 'btl', 10, 20, 'Low Stock', NULL),
('PSC-144', 'Bioderm Antimicrobial Germicidal Family Soap (135g)', 'Personal Care', 42.00, 'bar', 110, 20, 'Soap', NULL),

-- Snacks (🍿)
('SNK-145', 'Jack ''n Jill Piattos Cheese Potato Crisps (85g)', 'Snacks', 44.00, 'bag', 125, 20, 'Crispy', NULL),
('SNK-146', 'Oishi Prawn Crackers Spicy Flavor (90g)', 'Snacks', 35.00, 'bag', 75, 20, 'Snack', NULL),
('SNK-147', 'Nagaraya Cracker Nuts Original Garlic (160g)', 'Snacks', 48.00, 'bag', 13, 20, 'Low Stock', NULL),
('SNK-148', 'Choc Nut Peanut Milk Chocolate (Box of 24)', 'Snacks', 78.00, 'box', 60, 20, 'Classic Sweet', NULL),
('SNK-149', 'SkyFlakes Classic Crackers (Pack of 10)', 'Snacks', 62.00, 'pack', 0, 20, 'Out of Stock', NULL),

-- Other (📦)
('OTH-150', 'Oops Heavy-Duty Reusable Grocery Eco Tote Bag', 'Other', 45.00, 'pc', 85, 20, 'Eco Friendly', NULL),
('OTH-151', 'Food-Grade Clear Ice Storage Bags (100 pcs)', 'Other', 55.00, 'pack', 5, 20, 'Low Stock', NULL)
ON DUPLICATE KEY UPDATE
    `Price` = VALUES(`Price`),
    `Unit` = VALUES(`Unit`),
    `StockQuantity` = VALUES(`StockQuantity`),
    `MinimumStock` = VALUES(`MinimumStock`),
    `Tag` = VALUES(`Tag`);

SET FOREIGN_KEY_CHECKS = 1;
