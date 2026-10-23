using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using OopsGrocery.Commands;
using OopsGrocery.Models;

namespace OopsGrocery.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private object _currentViewModel;
    private string _activeNav = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? SignOutRequested;

    public User CurrentUser { get; }

    public bool IsAdmin => CurrentUser.Role == Role.Admin;
    public bool IsCashier => CurrentUser.Role == Role.Cashier;

    public string DisplayName => string.IsNullOrWhiteSpace(CurrentUser.Username) ? "User" : CurrentUser.Username;
    public string Initial => string.IsNullOrWhiteSpace(CurrentUser.Username) ? "U" : CurrentUser.Username[..1].ToUpperInvariant();
    public string RoleBadge => IsAdmin ? "STORE ADMIN" : "CASHIER";
    public string RoleGreeting => IsAdmin ? "Store Administration Workspace" : "Point of Sale (POS) Terminal — Register #01";

    public AdminViewModel AdminVM { get; }
    public CashierViewModel CashierVM { get; }

    public object CurrentViewModel
    {
        get => _currentViewModel;
        set
        {
            if (_currentViewModel == value) return;
            _currentViewModel = value;
            OnPropertyChanged();
        }
    }

    public string ActiveNav
    {
        get => _activeNav;
        set
        {
            if (_activeNav == value) return;
            _activeNav = value;
            OnPropertyChanged();
        }
    }

    public ICommand SelectNavCommand { get; }
    public ICommand SignOutCommand { get; }

    public MainWindowViewModel(User user)
    {
        CurrentUser = user;
        AdminVM = new AdminViewModel();
        CashierVM = new CashierViewModel(DisplayName);

        if (IsAdmin)
        {
            _currentViewModel = AdminVM;
            _activeNav = "Dashboard";
        }
        else
        {
            _currentViewModel = CashierVM;
            _activeNav = "Register";
        }

        SelectNavCommand = new RelayCommand<string>(nav =>
        {
            if (string.IsNullOrWhiteSpace(nav)) return;
            ActiveNav = nav;

            if (IsAdmin)
            {
                CurrentViewModel = AdminVM;
            }
            else
            {
                CurrentViewModel = CashierVM;
            }
        });

        SignOutCommand = new RelayCommand(() =>
        {
            SignOutRequested?.Invoke(this, EventArgs.Empty);
        });
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
