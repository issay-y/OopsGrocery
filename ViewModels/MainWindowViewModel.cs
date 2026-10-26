using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using OopsGrocery.Commands;
using OopsGrocery.Models;
using OopsGrocery.Views;

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

    public string RoleGreeting => IsAdmin ? ActiveNav switch
    {
        "Dashboard" => "Store Administration Workspace · Dashboard Overview",
        "Products" => "Inventory Catalog & Stock Management",
        "Transactions" => "Sales Pipeline & Audit Reports",
        "UserManagement" => "User Management · Access Control & Staff Directory",
        "Profile" => "Administrator Profile & Security Preferences",
        _ => "Store Administration Workspace"
    } : "Point of Sale (POS) Terminal — Register #01";

    public AdminViewModel AdminVM { get; }
    public ProductsInventoryViewModel ProductsVM { get; }
    public TransactionsReportsViewModel TransactionsVM { get; }
    public UserManagementViewModel UserManagementVM { get; }
    public ProfileViewModel ProfileVM { get; }
    public CashierViewModel CashierVM { get; }

    public bool IsDashboardActive => ActiveNav == "Dashboard";
    public bool IsProductsActive => ActiveNav == "Products";
    public bool IsTransactionsActive => ActiveNav == "Transactions";
    public bool IsUserManagementActive => ActiveNav == "UserManagement";
    public bool IsProfileActive => ActiveNav == "Profile";

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
            OnPropertyChanged(nameof(RoleGreeting));
            OnPropertyChanged(nameof(IsDashboardActive));
            OnPropertyChanged(nameof(IsProductsActive));
            OnPropertyChanged(nameof(IsTransactionsActive));
            OnPropertyChanged(nameof(IsUserManagementActive));
            OnPropertyChanged(nameof(IsProfileActive));
        }
    }

    public ICommand SelectNavCommand { get; }
    public ICommand SignOutCommand { get; }
    public ICommand OpenProfileDialogCommand { get; }

    public MainWindowViewModel(User user)
    {
        CurrentUser = user;
        AdminVM = new AdminViewModel();
        ProductsVM = new ProductsInventoryViewModel();
        TransactionsVM = new TransactionsReportsViewModel();
        UserManagementVM = new UserManagementViewModel();
        ProfileVM = new ProfileViewModel(user);
        CashierVM = new CashierViewModel(DisplayName);

        OpenProfileDialogCommand = new RelayCommand(() =>
        {
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;
            var dialog = new ViewProfileWindow(CurrentUser);
            if (activeWindow != null && activeWindow.IsLoaded)
            {
                dialog.Owner = activeWindow;
            }
            dialog.ShowDialog();
        });

        SelectNavCommand = new RelayCommand<string>(nav =>
        {
            if (string.IsNullOrWhiteSpace(nav)) return;

            if (IsAdmin)
            {
                switch (nav)
                {
                    case "Dashboard":
                        AdminVM.LoadDashboardData();
                        CurrentViewModel = AdminVM;
                        ActiveNav = "Dashboard";
                        break;
                    case "Products":
                    case "Inventory":
                        ProductsVM.LoadProducts(isManualRefresh: false);
                        CurrentViewModel = ProductsVM;
                        ActiveNav = "Products";
                        break;
                    case "Transactions":
                    case "Reports":
                        TransactionsVM.LoadAnalyticsData();
                        CurrentViewModel = TransactionsVM;
                        ActiveNav = "Transactions";
                        break;
                    case "UserManagement":
                    case "Users":
                        CurrentViewModel = UserManagementVM;
                        ActiveNav = "UserManagement";
                        break;
                    case "Profile":
                        OpenProfileDialogCommand.Execute(null);
                        break;
                    default:
                        CurrentViewModel = AdminVM;
                        ActiveNav = "Dashboard";
                        break;
                }
            }
            else
            {
                if (nav.Equals("Profile", StringComparison.OrdinalIgnoreCase))
                {
                    OpenProfileDialogCommand.Execute(null);
                }
                else
                {
                    ActiveNav = "NewOrderPOS";
                    CashierVM.EnsurePosScreen();
                    CurrentViewModel = CashierVM;
                }
            }
        });

        AdminVM.RequestNavigateToInventory += () =>
        {
            SelectNavCommand.Execute("Products");
        };

        if (IsAdmin)
        {
            _currentViewModel = AdminVM;
            _activeNav = "Dashboard";
        }
        else
        {
            _currentViewModel = CashierVM;
            _activeNav = "NewOrderPOS";
        }

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
