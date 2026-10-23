using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using OopsGrocery.Commands;
using OopsGrocery.Models;
using OopsGrocery.Services;

namespace OopsGrocery.ViewModels;

public sealed class LoginViewModel : INotifyPropertyChanged
{
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _statusMessage = string.Empty;

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
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                StatusMessage = string.Empty;
            }
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
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                StatusMessage = string.Empty;
            }
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasErrorMessage));
        }
    }

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(_statusMessage);

    public ICommand LoginCommand { get; }

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(Login);
    }

    public void UpdatePassword(string password)
    {
        Password = password;
    }

    private void SetError(string message)
    {
        StatusMessage = message;
        LoginFailed?.Invoke(this, message);
    }

    private void Login()
    {
        var usernameValidation = LoginValidator.ValidateUsername(Username);
        if (!usernameValidation.IsValid)
        {
            SetError(usernameValidation.ErrorMessage!);
            return;
        }

        var passwordValidation = LoginValidator.ValidatePassword(_password, Username);
        if (!passwordValidation.IsValid)
        {
            SetError(passwordValidation.ErrorMessage!);
            return;
        }

        try
        {
            var authentication = new AuthenticationService();
            var user = authentication.AuthenticateUser(Username.Trim(), _password);
            if (user == null)
            {
                SetError("Invalid username or password. Note that username is strictly case-sensitive.");
                return;
            }

            LoggedInUser = user;
            StatusMessage = string.Empty;
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            SetError("Unable to connect to the database. Ensure MariaDB is running on port 3306.");
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
