using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using OopsGrocery.Commands;
using OopsGrocery.Models;
using OopsGrocery.Services;

namespace OopsGrocery.ViewModels;

public sealed class LoginViewModel : INotifyPropertyChanged
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";

    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _isLoggingIn;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LoginSucceeded;
    public event EventHandler<string>? LoginFailed;

    public User? LoggedInUser { get; private set; }

    public string Username
    {
        get => _username;
        set
        {
            if (_username == value) return;
            _username = value;
            OnPropertyChanged();
        }
    }

    public string Password
    {
        private get => _password;
        set
        {
            if (_password == value) return;
            _password = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoginCommand { get; }

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(Login);
    }

    public void UpdatePassword(string password)
    {
        Password = password;
    }

    private void Login()
    {
        if (_isLoggingIn) return;
        _isLoggingIn = true;

        try
        {
            // Login only compares credentials. No password-format rules here.
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(_password))
            {
                LoginFailed?.Invoke(this, InvalidCredentialsMessage);
                return;
            }

            try
            {
                var authentication = new AuthenticationService();
                var user = authentication.AuthenticateUser(Username.Trim(), _password);
                if (user == null)
                {
                    LoginFailed?.Invoke(this, InvalidCredentialsMessage);
                    return;
                }

                LoggedInUser = user;
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception)
            {
                LoginFailed?.Invoke(this, "Unable to connect to the database. Ensure MariaDB is running on port 3306.");
            }
        }
        finally
        {
            _isLoggingIn = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}