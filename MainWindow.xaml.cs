using System.Windows;
using OopsGrocery.Models;
using OopsGrocery.ViewModels;

namespace OopsGrocery;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow() : this(new User { Username = "admin", Role = Role.Admin })
    {
    }

    public MainWindow(User? currentUser)
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel(currentUser ?? new User { Username = "admin", Role = Role.Admin });
        _viewModel.SignOutRequested += OnSignOutRequested;
        DataContext = _viewModel;
    }

    private void OnSignOutRequested(object? sender, EventArgs e)
    {
        var loginWindow = new LoginWindow();
        Application.Current.MainWindow = loginWindow;
        loginWindow.Show();
        Close();
    }
}