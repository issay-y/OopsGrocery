USE `OopsGrocery`;

-- Default Admin account
-- Username: admin
-- Password: Admin@123
INSERT INTO `Users` (`Username`, `PasswordHash`, `Role`, `IsActive`)
VALUES (
    'admin',
    'PBKDF2$100000$ebTDWNgJEhDIjleyKaO/8g==$jjc+WSbiV+VLAT5WuSnjwQhSvYnsD2M8vf97KNbDn+A=',
    'Admin',
    TRUE
)
ON DUPLICATE KEY UPDATE
    `PasswordHash` = VALUES(`PasswordHash`),
    `Role` = VALUES(`Role`),
    `IsActive` = VALUES(`IsActive`);

-- Default Cashier account
-- Username: cashier
-- Password: Cashier@123
INSERT INTO `Users` (`Username`, `PasswordHash`, `Role`, `IsActive`)
VALUES (
    'cashier',
    'PBKDF2$100000$CNSpaFhzG+JAJRxl+0EDnA==$e+uJ3bXQrf+2meCpm61tsz10VUNhG93sUG0FWUUe47g=',
    'Cashier',
    TRUE
)
ON DUPLICATE KEY UPDATE
    `PasswordHash` = VALUES(`PasswordHash`),
    `Role` = VALUES(`Role`),
    `IsActive` = VALUES(`IsActive`);

-- Remove any old non-POS test accounts
DELETE FROM `Users` WHERE `Username` IN ('user', 'customer');
