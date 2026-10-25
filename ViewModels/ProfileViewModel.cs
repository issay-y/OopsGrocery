using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using OopsGrocery.Commands;
using OopsGrocery.Models;

namespace OopsGrocery.ViewModels;

public sealed class ProfileViewModel : INotifyPropertyChanged
{
    private string _notificationMessage = string.Empty;
    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public User CurrentUser { get; }

    public string Username => CurrentUser.Username;
    public string RoleBadge => CurrentUser.Role == Role.Admin ? "SYSTEM ADMINISTRATOR" : "CASHIER POS";
    public string Initial => string.IsNullOrWhiteSpace(CurrentUser.Username) ? "U" : CurrentUser.Username[..1].ToUpperInvariant();
    public string FormattedLastLogin => CurrentUser.FormattedLastLogin;
    public string StatusText => CurrentUser.StatusText;
    public string AccountId => $"ACC-00{CurrentUser.Id}";
    public string StoreBranch => "Main Branch — Store #101";

    public string CurrentPassword
    {
        get => _currentPassword;
        set
        {
            if (_currentPassword == value) return;
            _currentPassword = value;
            OnPropertyChanged();
        }
    }

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (_newPassword == value) return;
            _newPassword = value;
            OnPropertyChanged();
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (_confirmPassword == value) return;
            _confirmPassword = value;
            OnPropertyChanged();
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

    public ICommand SaveChangesCommand { get; }

    public ProfileViewModel(User user)
    {
        CurrentUser = user;

        SaveChangesCommand = new RelayCommand(() =>
        {
            NotificationMessage = "Profile security details updated successfully.";
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        });
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
