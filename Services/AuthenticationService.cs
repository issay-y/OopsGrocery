using MySqlConnector;
using OopsGrocery.Models;

namespace OopsGrocery.Services;

public sealed class AuthenticationService
{
    private const string DefaultConnectionString =
        "Server=localhost;Port=3306;Database=OopsGrocery;User ID=root;Password=;SslMode=None;";

    private readonly string _connectionString =
        DatabaseInitializer.ResolveConnectionString();

    private readonly IPasswordHasher _passwordHasher;
    private static bool _hasEnsuredDatabase;

    public AuthenticationService(IPasswordHasher? passwordHasher = null)
    {
        _passwordHasher = passwordHasher ?? PasswordHasher.Default;
    }

    public User? AuthenticateUser(string username, string password)
    {
        if (!_hasEnsuredDatabase)
        {
            try
            {
                DatabaseInitializer.EnsureDatabaseInitialized(_connectionString);
                _hasEnsuredDatabase = true;
            }
            catch
            {
                // If auto-init fails (e.g. specific access restrictions), continue to try standard connection
            }
        }

        using var connection = new MySqlConnection(_connectionString);
        try
        {
            connection.Open();
        }
        catch (MySqlException ex) when (ex.Number is 1049 or 1146)
        {
            // Database or table missing on this device; attempt initialization
            DatabaseInitializer.EnsureDatabaseInitialized(_connectionString);
            _hasEnsuredDatabase = true;
            connection.Open();
        }

        bool hasFullName = false;
        try
        {
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SHOW COLUMNS FROM Users LIKE 'FullName';";
            hasFullName = checkCmd.ExecuteScalar() != null;
        }
        catch { }

        using var command = connection.CreateCommand();
        command.CommandText = hasFullName
            ? """
                SELECT Id, Username, FullName, PasswordHash, Role, LastLogin, IsActive
                FROM Users
                WHERE BINARY Username = @username AND IsActive = TRUE
                LIMIT 1;
                """
            : """
                SELECT Id, Username, PasswordHash, Role, LastLogin, IsActive
                FROM Users
                WHERE BINARY Username = @username AND IsActive = TRUE
                LIMIT 1;
                """;
        command.Parameters.AddWithValue("@username", username);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var dbUsername = reader.GetString("Username");
        // Strictly case-sensitive username validation
        if (!string.Equals(dbUsername, username, StringComparison.Ordinal))
        {
            return null;
        }

        var storedHash = reader.GetString("PasswordHash");
        if (!_passwordHasher.VerifyPassword(password, storedHash))
        {
            return null;
        }

        var roleStr = reader.GetString("Role");
        _ = Enum.TryParse<Role>(roleStr, true, out var role);

        var user = new User
        {
            Id = reader.GetInt32("Id"),
            Username = reader.GetString("Username"),
            FullName = hasFullName && !reader.IsDBNull(reader.GetOrdinal("FullName"))
                ? reader.GetString("FullName")
                : string.Empty,
            PasswordHash = storedHash,
            Role = role,
            LastLogin = reader.IsDBNull(reader.GetOrdinal("LastLogin")) ? null : reader.GetDateTime("LastLogin"),
            IsActive = reader.GetBoolean("IsActive")
        };
        reader.Close();

        using var update = connection.CreateCommand();
        update.CommandText = "UPDATE Users SET LastLogin = UTC_TIMESTAMP() WHERE Id = @id";
        update.Parameters.AddWithValue("@id", user.Id);
        update.ExecuteNonQuery();

        return user;
    }

    public bool Authenticate(string username, string password)
    {
        return AuthenticateUser(username, password) != null;
    }
}