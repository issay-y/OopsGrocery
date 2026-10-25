using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MySqlConnector;
using OopsGrocery.Commands;
using OopsGrocery.Models;
using OopsGrocery.Services;
using OopsGrocery.Views;

namespace OopsGrocery.ViewModels;

public sealed class UserManagementViewModel : INotifyPropertyChanged
{
    private string _searchQuery = string.Empty;
    private string _notificationMessage = string.Empty;
    private User? _selectedUser;
    private readonly List<User> _allUsers = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<User> FilteredUsers { get; } = [];

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery == value) return;
            _searchQuery = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public string NotificationMessage
    {
        get => _notificationMessage;
        set
        {
            if (_notificationMessage == value) return;
            _notificationMessage = value;
            OnPropertyChanged();
        }
    }

    public User? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (_selectedUser == value) return;
            _selectedUser = value;
            OnPropertyChanged();
        }
    }

    public int TotalUsersCount => _allUsers.Count;
    public int AdminCount => _allUsers.Count(u => u.Role == Role.Admin);
    public int CashierCount => _allUsers.Count(u => u.Role == Role.Cashier);
    public int ActiveCount => _allUsers.Count(u => u.IsActive);

    public ICommand RefreshCommand { get; }
    public ICommand AddUserCommand { get; }
    public ICommand EditUserCommand { get; }
    public ICommand ToggleStatusCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public UserManagementViewModel()
    {
        RefreshCommand = new RelayCommand(OnRefresh);
        AddUserCommand = new RelayCommand(OnAddUser);
        EditUserCommand = new RelayCommand<User>(OnEditUser);
        ToggleStatusCommand = new RelayCommand<User>(OnToggleStatus);
        ClearSearchCommand = new RelayCommand(() => SearchQuery = string.Empty);

        LoadUsers(isManualRefresh: false);
    }

    private void OnRefresh()
    {
        LoadUsers(isManualRefresh: true);
    }

    public void LoadUsers(bool isManualRefresh = false)
    {
        try
        {
            var connStr = DatabaseInitializer.ResolveConnectionString();
            using var connection = new MySqlConnection(connStr);
            connection.Open();

            bool hasFullName = false;
            try
            {
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = "SHOW COLUMNS FROM Users LIKE 'FullName';";
                var res = checkCmd.ExecuteScalar();
                hasFullName = res != null;
            }
            catch { }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = hasFullName
                ? "SELECT Id, Username, FullName, PasswordHash, Role, LastLogin, IsActive FROM Users ORDER BY Role ASC, Username ASC;"
                : "SELECT Id, Username, PasswordHash, Role, LastLogin, IsActive FROM Users ORDER BY Role ASC, Username ASC;";
            using var reader = cmd.ExecuteReader();

            var loadedUsers = new List<User>();
            while (reader.Read())
            {
                var roleStr = reader.GetString("Role");
                _ = Enum.TryParse<Role>(roleStr, true, out var role);

                loadedUsers.Add(new User
                {
                    Id = reader.GetInt32("Id"),
                    Username = reader.GetString("Username"),
                    FullName = hasFullName && !reader.IsDBNull(reader.GetOrdinal("FullName"))
                        ? reader.GetString("FullName")
                        : string.Empty,
                    PasswordHash = reader.GetString("PasswordHash"),
                    Role = role,
                    LastLogin = reader.IsDBNull(reader.GetOrdinal("LastLogin")) ? null : reader.GetDateTime("LastLogin"),
                    IsActive = reader.GetBoolean("IsActive")
                });
            }

            _allUsers.Clear();
            _allUsers.AddRange(loadedUsers);

            ApplyFilter();
            NotifyMetricCounts();

            if (isManualRefresh)
            {
                NotificationMessage = "User directory refreshed with latest system accounts.";
            }
        }
        catch (Exception ex)
        {
            if (isManualRefresh)
            {
                var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;
                // Appropriate feedback if refreshing fails
                MessageBoxWindow.Show(
                    $"Unable to refresh user directory: {ex.Message}. Please check your database connection.",
                    "Refresh Failed",
                    owner: activeWindow,
                    type: MessageBoxType.Error,
                    subtitle: "Database Connectivity Alert");

                NotificationMessage = "Failed to refresh user list: Database connection error.";
            }
            else
            {
                // Fallback default system accounts only if database is unreachable on initial load
                if (_allUsers.Count == 0)
                {
                    LoadFallbackUsers();
                    ApplyFilter();
                    NotifyMetricCounts();
                }
            }
        }
    }

    private void LoadFallbackUsers()
    {
        _allUsers.Clear();
        _allUsers.Add(new User
        {
            Id = 1,
            Username = "admin",
            FullName = "Administrator",
            Role = Role.Admin,
            IsActive = true,
            LastLogin = DateTime.UtcNow.AddMinutes(-12)
        });
        _allUsers.Add(new User
        {
            Id = 2,
            Username = "cashier",
            FullName = "Store Cashier",
            Role = Role.Cashier,
            IsActive = true,
            LastLogin = DateTime.UtcNow.AddHours(-3)
        });
        _allUsers.Add(new User
        {
            Id = 3,
            Username = "sarah_cashier",
            FullName = "Sarah Jenkins",
            Role = Role.Cashier,
            IsActive = true,
            LastLogin = DateTime.UtcNow.AddDays(-1)
        });
        _allUsers.Add(new User
        {
            Id = 4,
            Username = "night_supervisor",
            FullName = "Night Supervisor",
            Role = Role.Admin,
            IsActive = false,
            LastLogin = DateTime.UtcNow.AddDays(-5)
        });
    }

    private void ApplyFilter()
    {
        FilteredUsers.Clear();
        var query = SearchQuery.Trim().ToLowerInvariant();

        foreach (var user in _allUsers)
        {
            if (string.IsNullOrWhiteSpace(query) ||
                user.Id.ToString().Contains(query) ||
                user.Id.ToString("D4").Contains(query) ||
                $"#{user.Id:D4}".ToLowerInvariant().Contains(query) ||
                $"#{user.Id}".ToLowerInvariant().Contains(query) ||
                user.Username.ToLowerInvariant().Contains(query) ||
                user.FullName.ToLowerInvariant().Contains(query) ||
                user.Role.ToString().ToLowerInvariant().Contains(query))
            {
                FilteredUsers.Add(user);
            }
        }
    }

    private void OnAddUser()
    {
        var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;
        var dialog = new AddUserWindow();
        if (activeWindow != null && activeWindow.IsLoaded)
        {
            dialog.Owner = activeWindow;
        }

        var result = dialog.ShowDialog();
        if (result == true)
        {
            LoadUsers(isManualRefresh: false);
            NotificationMessage = $"Cashier account '{dialog.CreatedUsername}' successfully created and active.";
        }
    }

    private void OnEditUser(User? user)
    {
        if (user == null) return;

        var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;
        var dialog = new EditUserWindow(user);
        if (activeWindow != null && activeWindow.IsLoaded)
        {
            dialog.Owner = activeWindow;
        }

        var result = dialog.ShowDialog();
        if (result == true)
        {
            LoadUsers(isManualRefresh: false);
            if (dialog.PasswordChanged)
            {
                NotificationMessage = $"Password for staff account '{user.Username}' was successfully updated.";
            }
            else
            {
                NotificationMessage = $"Staff account '{user.Username}' verified with existing password preserved.";
            }
        }
    }

    private void OnToggleStatus(User? user)
    {
        if (user == null) return;

        var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

        // Prevent disabling the primary administrator account
        if (user.Role == Role.Admin && user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) && user.IsActive)
        {
            MessageBoxWindow.Show(
                "The primary system administrator account ('admin') cannot be disabled to prevent loss of administrative access.",
                "Action Prohibited",
                activeWindow,
                MessageBoxType.Warning,
                subtitle: "System Access Protection");
            return;
        }

        var nextStateName = user.IsActive ? "disable" : "enable";
        var title = user.IsActive ? "Disable User Account" : "Enable User Account";
        var confirmText = user.IsActive ? "Yes, Disable" : "Yes, Enable";

        var confirmed = MessageBoxWindow.Confirm(
            $"Are you sure you want to {nextStateName} the account for '{user.Username}' ({user.Role})?",
            title,
            confirmButtonText: confirmText,
            cancelButtonText: "Cancel",
            owner: activeWindow,
            type: user.IsActive ? MessageBoxType.Warning : MessageBoxType.Question,
            subtitle: "Staff Access Control");

        if (!confirmed) return;

        user.IsActive = !user.IsActive;

        try
        {
            var connStr = DatabaseInitializer.ResolveConnectionString();
            using var connection = new MySqlConnection(connStr);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Users SET IsActive = @isActive WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@isActive", user.IsActive);
            cmd.Parameters.AddWithValue("@id", user.Id);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            // Revert state if database update failed
            user.IsActive = !user.IsActive;

            MessageBoxWindow.Show(
                $"Failed to update user status in database: {ex.Message}",
                "Database Update Error",
                activeWindow,
                MessageBoxType.Error);
            return;
        }

        NotificationMessage = $"User '{user.Username}' is now {(user.IsActive ? "ACTIVE" : "DISABLED")}.";
        ApplyFilter();
        NotifyMetricCounts();
    }

    private void NotifyMetricCounts()
    {
        OnPropertyChanged(nameof(TotalUsersCount));
        OnPropertyChanged(nameof(AdminCount));
        OnPropertyChanged(nameof(CashierCount));
        OnPropertyChanged(nameof(ActiveCount));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
