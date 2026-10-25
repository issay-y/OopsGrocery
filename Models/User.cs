using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OopsGrocery.Models;

public sealed class User : INotifyPropertyChanged
{
    private int _id;
    private string _username = string.Empty;
    private string _fullName = string.Empty;
    private string _passwordHash = string.Empty;
    private Role _role = Role.Cashier;
    private DateTime? _lastLogin;
    private bool _isActive = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id
    {
        get => _id;
        set
        {
            if (_id == value) return;
            _id = value;
            OnPropertyChanged();
        }
    }

    public string Username
    {
        get => _username;
        set
        {
            if (_username == value) return;
            _username = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Initial));
        }
    }

    public string FullName
    {
        get => _fullName;
        set
        {
            if (_fullName == value) return;
            _fullName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Initial));
        }
    }

    public string PasswordHash
    {
        get => _passwordHash;
        set
        {
            if (_passwordHash == value) return;
            _passwordHash = value;
            OnPropertyChanged();
        }
    }

    public Role Role
    {
        get => _role;
        set
        {
            if (_role == value) return;
            _role = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RoleBadge));
        }
    }

    public DateTime? LastLogin
    {
        get => _lastLogin;
        set
        {
            if (_lastLogin == value) return;
            _lastLogin = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FormattedLastLogin));
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value) return;
            _isActive = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public string FormattedLastLogin => LastLogin.HasValue
        ? LastLogin.Value.ToLocalTime().ToString("MMM dd, yyyy · hh:mm tt")
        : "Never Logged In";

    public string StatusText => IsActive ? "Active" : "Disabled";

    public string RoleBadge => Role == Role.Admin ? "ADMIN" : "CASHIER";

    public string Initial => !string.IsNullOrWhiteSpace(FullName)
        ? FullName[..1].ToUpperInvariant()
        : (!string.IsNullOrWhiteSpace(Username) ? Username[..1].ToUpperInvariant() : "U");

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


