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
    /// Ensures the OopsGrocery database, Users table, and default admin/cashier accounts exist.
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

            // Verify Users table exists and is populated
            using var cmd = testConn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users;";
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            if (count > 0)
            {
                return; // Database and users are already present and ready
            }

            // If table is empty, seed it
            SeedDefaultUsers(testConn);
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

        using (var createTableCmd = serverConn.CreateCommand())
        {
            createTableCmd.CommandText = """
                CREATE TABLE IF NOT EXISTS `Users` (
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
                """;
            createTableCmd.ExecuteNonQuery();
        }

        SeedDefaultUsers(serverConn);
    }

    private static void SeedDefaultUsers(MySqlConnection connection)
    {
        using var seedCmd = connection.CreateCommand();
        seedCmd.CommandText = """
            INSERT INTO `Users` (`Username`, `PasswordHash`, `Role`, `IsActive`)
            VALUES
            (
                'admin',
                'PBKDF2$100000$K9KuYx9Q5RwNvMHTBFwbuw==$SzFwVug/u+Hz2pZbQBRl20LHf16hulnbtU75uafQh1U=',
                'Admin',
                1
            ),
            (
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
            """;
        seedCmd.ExecuteNonQuery();
    }
}
