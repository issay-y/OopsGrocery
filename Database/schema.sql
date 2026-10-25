-- =====================================================================
-- OopsGrocery Database Schema & Initial Data
-- Universal compatibility: MySQL 5.7+, MySQL 8.x, and MariaDB 10.x/11.x
-- =====================================================================

CREATE DATABASE IF NOT EXISTS `OopsGrocery`
	CHARACTER SET utf8mb4
	COLLATE utf8mb4_unicode_ci;

USE `OopsGrocery`;

-- 1. Users Table (System Accounts)
CREATE TABLE IF NOT EXISTS `Users`
(
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

-- 2. Products Table (Inventory Catalog)
CREATE TABLE IF NOT EXISTS `Products`
(
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

-- Seed default Admin account (admin / Admin.94K)
INSERT INTO `Users` (`Username`, `FullName`, `PasswordHash`, `Role`, `IsActive`)
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

-- Seed default Cashier account (cashier / Cash.71Ny)
INSERT INTO `Users` (`Username`, `FullName`, `PasswordHash`, `Role`, `IsActive`)
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

DELETE FROM `Users` WHERE `Username` IN ('user', 'customer');

-- 3. Transactions Table (Live Completed Sales)
CREATE TABLE IF NOT EXISTS `Transactions`
(
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

-- 4. Transaction Items Table (Line Items Breakdown)
CREATE TABLE IF NOT EXISTS `Transaction_Items`
(
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
	KEY `IX_TransactionItems_Sku` (`Sku`),
	CONSTRAINT `FK_TransactionItems_Transaction` FOREIGN KEY (`TransactionId`) REFERENCES `Transactions` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

