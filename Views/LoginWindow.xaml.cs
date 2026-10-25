using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OopsGrocery.ViewModels;
using OopsGrocery.Views;

namespace OopsGrocery;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel = new();

    public LoginWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.LoginSucceeded += OnLoginSucceeded;
        _viewModel.LoginFailed += OnLoginFailed;
        Loaded += (_, _) => UsernameTextBox.Focus();
    }

    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
        {
            var user = _viewModel.LoggedInUser;
            var displayName = !string.IsNullOrWhiteSpace(user?.Username) ? user.Username : "User";
            var role = user?.Role == Models.Role.Admin ? "Administrator" : "Cashier";

            MessageBoxWindow.Show(
                $"Welcome back, {displayName}! You have logged in successfully as {role}.",
                "Login Success",
                this,
                MessageBoxType.Success,
                "Continue",
                "Store Authentication Verified");

            var mainWindow = new MainWindow(_viewModel.LoggedInUser)
            {
                WindowState = WindowState.Maximized
            };
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            mainWindow.Activate();
            Close();
        }));
    }

    // The ViewModel decides the message ("Invalid username or password." for bad
    // credentials, a separate message if the database is unreachable).
    private void OnLoginFailed(object? sender, string message)
    {
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
        {
            MessageBoxWindow.Show(
                message,
                "Login Failed",
                this);

            FocusPasswordField();
        }));
    }

    private void UsernameTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            FocusPasswordField();
        }
    }

    private void PasswordInput_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            SubmitLogin();
        }
    }

    private void RevealedPasswordTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            SubmitLogin();
        }
    }

    private void SubmitLogin()
    {
        if (_viewModel.LoginCommand.CanExecute(null))
        {
            _viewModel.LoginCommand.Execute(null);
        }
    }

    private void FocusPasswordField()
    {
        if (ShowPasswordToggle.IsChecked == true)
        {
            RevealedPasswordTextBox.Focus();
            RevealedPasswordTextBox.SelectAll();
        }
        else
        {
            PasswordInput.Focus();
            PasswordInput.SelectAll();
        }
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            UpdatePasswordPlaceholder(passwordBox.Password);
            _viewModel.UpdatePassword(passwordBox.Password);
        }
    }

    // Keeps the view model in sync while the password is shown as plain text.
    private void RevealedPasswordTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            UpdatePasswordPlaceholder(textBox.Text);
            _viewModel.UpdatePassword(textBox.Text);
        }
    }

    private void ShowPasswordToggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (ShowPasswordToggle.IsChecked == true)
        {
            RevealedPasswordTextBox.Text = PasswordInput.Password;
            PasswordInput.Visibility = Visibility.Collapsed;
            RevealedPasswordTextBox.Visibility = Visibility.Visible;
            RevealedPasswordTextBox.Focus();
            RevealedPasswordTextBox.CaretIndex = RevealedPasswordTextBox.Text.Length;
        }
        else
        {
            PasswordInput.Password = RevealedPasswordTextBox.Text;
            RevealedPasswordTextBox.Visibility = Visibility.Collapsed;
            PasswordInput.Visibility = Visibility.Visible;
            PasswordInput.Focus();
        }
    }

    // Shows "Enter your password" only while the field is empty, in either mode.
    private void UpdatePasswordPlaceholder(string password)
    {
        PasswordPlaceholder.Visibility = string.IsNullOrEmpty(password)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}