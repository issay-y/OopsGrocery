using System;
using System.Windows;
using OopsGrocery.Models;
using OopsGrocery.ViewModels;
using OopsGrocery.Views;

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
        WindowState = WindowState.Maximized;
        StateChanged += (_, _) => UpdateRestoreButtonGlyph();

        _viewModel = new MainWindowViewModel(currentUser ?? new User { Username = "admin", Role = Role.Admin });
        _viewModel.SignOutRequested += OnSignOutRequested;
        DataContext = _viewModel;

        Loaded += (_, _) =>
        {
            WindowState = WindowState.Maximized;
            UpdateRestoreButtonGlyph();
        };
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeRestoreButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        UpdateRestoreButtonGlyph();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBoxWindow.Confirm(
            "Are you sure you want to exit the Oops Grocery workspace?",
            "Exit Workspace",
            confirmButtonText: "Exit",
            cancelButtonText: "Cancel",
            owner: this,
            type: MessageBoxType.Question,
            subtitle: "Store Session Management");

        if (confirm)
        {
            Close();
        }
    }

    private void UpdateRestoreButtonGlyph()
    {
        if (RestoreMaximizeBtn != null)
        {
            RestoreMaximizeBtn.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
            RestoreMaximizeBtn.ToolTip = WindowState == WindowState.Maximized ? "Restore Down" : "Maximize";
        }
    }

    private void OnSignOutRequested(object? sender, EventArgs e)
    {
        var loginWindow = new LoginWindow();
        Application.Current.MainWindow = loginWindow;
        loginWindow.Show();
        Close();
    }
}