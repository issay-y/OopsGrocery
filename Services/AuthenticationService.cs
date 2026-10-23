using MySqlConnector;
using OopsGrocery.Models;

namespace OopsGrocery.Services;

public sealed class AuthenticationService
{
    private const string DefaultConnectionString =
        "Server=localhost;Port=3306;Database=OopsGrocery;User ID=root;Password=;SslMode=None;";

    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("OOPSGROCERY_CONNECTION_STRING")
        ?? DefaultConnectionString;

    private readonly IPasswordHasher _passwordHasher;

    public AuthenticationService(IPasswordHasher? passwordHasher = null)
    {
        _passwordHasher = passwordHasher ?? PasswordHasher.Default;
    }

    public User? AuthenticateUser(string username, string password)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
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