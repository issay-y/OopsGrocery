-- =====================================================================
-- OopsGrocery Database Schema & Initial Data
-- Universal compatibility: MySQL 5.7+, MySQL 8.x, and MariaDB 10.x/11.x
-- =====================================================================

CREATE DATABASE IF NOT EXISTS `OopsGrocery`
	CHARACTER SET utf8mb4
	COLLATE utf8mb4_unicode_ci;

USE `OopsGrocery`;

CREATE TABLE IF NOT EXISTS `Users`
(
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

-- Seed default Admin account (admin / Admin.94K)
INSERT INTO `Users` (`Username`, `PasswordHash`, `Role`, `IsActive`)
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

-- Seed default Cashier account (cashier / Cash.71Ny)
INSERT INTO `Users` (`Username`, `PasswordHash`, `Role`, `IsActive`)
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

DELETE FROM `Users` WHERE `Username` IN ('user', 'customer');
