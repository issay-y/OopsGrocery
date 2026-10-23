USE `OopsGrocery`;

-- Default Admin account
-- Username: admin (strictly case-sensitive)
-- Password: Admin.94K (Compliant with 8-10 chars, uppercase, lowercase, digit, allowed special: .)
INSERT INTO `Users` (`Username`, `PasswordHash`, `Role`, `IsActive`)
VALUES (
    'admin',
    'PBKDF2$100000$K9KuYx9Q5RwNvMHTBFwbuw==$SzFwVug/u+Hz2pZbQBRl20LHf16hulnbtU75uafQh1U=',
    'Admin',
    TRUE
)
ON DUPLICATE KEY UPDATE
    `PasswordHash` = VALUES(`PasswordHash`),
    `Role` = VALUES(`Role`),
    `IsActive` = VALUES(`IsActive`);

-- Default Cashier account
-- Username: cashier (strictly case-sensitive)
-- Password: Cash.71Ny (Compliant with 8-10 chars, uppercase, lowercase, digit, allowed special: .)
INSERT INTO `Users` (`Username`, `PasswordHash`, `Role`, `IsActive`)
VALUES (
    'cashier',
    'PBKDF2$100000$juEStoErFzuglex33oiGQA==$0llglOiRFCzuJdqRZjJ2ybYdgvQmQmnLD3cKXc5fnEA=',
    'Cashier',
    TRUE
)
ON DUPLICATE KEY UPDATE
    `PasswordHash` = VALUES(`PasswordHash`),
    `Role` = VALUES(`Role`),
    `IsActive` = VALUES(`IsActive`);

-- Remove any old non-POS test accounts
DELETE FROM `Users` WHERE `Username` IN ('user', 'customer');
