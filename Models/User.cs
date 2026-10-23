namespace OopsGrocery.Models;

public sealed class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Role Role { get; set; } = Role.Cashier;

    public DateTime? LastLogin { get; set; }

    public bool IsActive { get; set; } = true;
}
