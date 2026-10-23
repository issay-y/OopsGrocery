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
    `PasswordHash` VARCHAR(255) NOT NULL,
    `Role` ENUM('Admin', 'Cashier') NOT NULL DEFAULT 'Cashier',
    `LastLogin` DATETIME NULL DEFAULT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_Users_Username` (`Username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Seed Default Accounts
-- Default Admin Account:
--   Username: admin (strictly case-sensitive)
--   Password: Admin.94K (Compliant with 8-10 chars, uppercase, lowercase, digit, allowed special: .)
INSERT INTO `users` (`Username`, `PasswordHash`, `Role`, `IsActive`)
VALUES (
    'admin',
    'PBKDF2$100000$K9KuYx9Q5RwNvMHTBFwbuw==$SzFwVug/u+Hz2pZbQBRl20LHf16hulnbtU75uafQh1U=',
    'Admin',
    1
)
ON DUPLICATE KEY UPDATE
    `PasswordHash` = VALUES(`PasswordHash`),
    `Role` = VALUES(`Role`),
    `IsActive` = VALUES(`IsActive`);

-- Default Cashier Account:
--   Username: cashier (strictly case-sensitive)
--   Password: Cash.71Ny (Compliant with 8-10 chars, uppercase, lowercase, digit, allowed special: .)
INSERT INTO `users` (`Username`, `PasswordHash`, `Role`, `IsActive`)
VALUES (
    'cashier',
    'PBKDF2$100000$juEStoErFzuglex33oiGQA==$0llglOiRFCzuJdqRZjJ2ybYdgvQmQmnLD3cKXc5fnEA=',
    'Cashier',
    1
)
ON DUPLICATE KEY UPDATE
    `PasswordHash` = VALUES(`PasswordHash`),
    `Role` = VALUES(`Role`),
    `IsActive` = VALUES(`IsActive`);

-- Remove any old non-POS test accounts
DELETE FROM `users` WHERE `Username` IN ('user', 'customer');

SET FOREIGN_KEY_CHECKS = 1;
