using System.Windows;
using System.Windows.Input;
using OopsGrocery.Models;

namespace OopsGrocery.Views;

public partial class ViewProfileWindow : Window
{
    public User CurrentUser { get; }

    public bool IsAdmin => CurrentUser.Role == Role.Admin;
    public bool IsCashier => CurrentUser.Role == Role.Cashier;

    public string FormattedUserId => $"USR-{CurrentUser.Id:D4}";
    public string DisplayName => !string.IsNullOrWhiteSpace(CurrentUser.FullName) ? CurrentUser.FullName : CurrentUser.Username;
    public string Initial => CurrentUser.Initial;
    public string RoleBadge => CurrentUser.Role == Role.Admin ? "STORE ADMINISTRATOR" : "CASHIER POS OPERATOR";
    public string FormattedLastLogin => CurrentUser.FormattedLastLogin;
    public string AccountStatus => CurrentUser.IsActive ? "Active" : "Inactive";
    public string RoleDescription => CurrentUser.Role == Role.Admin
        ? "Full supermarket administration, product catalog management, inventory restocks, financial reporting & system auditing."
        : "Point-of-sale checkout terminal, rapid barcode scanning, cash handling, and customer receipt issuance.";

    public ViewProfileWindow(User user)
    {
        InitializeComponent();
        CurrentUser = user;
        DataContext = this;
    }

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
