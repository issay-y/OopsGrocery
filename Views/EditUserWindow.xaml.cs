using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MySqlConnector;
using OopsGrocery.Models;
using OopsGrocery.Services;

namespace OopsGrocery.Views;

public partial class EditUserWindow : Window
{
    private readonly User _user;

    public bool PasswordChanged { get; private set; }

    public EditUserWindow(User user)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));

        InitializeComponent();

        MouseDown += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        };

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                HandleCancel();
            }
        };

        Loaded += (_, _) =>
        {
            MaxHeight = Math.Min(1000, SystemParameters.WorkArea.Height - 30);
            PopulateUserDetails();
            NewPasswordInput.Focus();
        };
    }

    private void PopulateUserDetails()
    {
        // 1. Name: Display Full Name or Username fallback (Read-Only)
        NameTextBox.Text = !string.IsNullOrWhiteSpace(_user.FullName)
            ? _user.FullName
            : (!string.IsNullOrWhiteSpace(_user.Username) ? _user.Username : "—");

        // 2. Username: Display Username (Read-Only)
        UsernameTextBox.Text = _user.Username;

        // 3. Role: Display Role badge & caption (Read-Only)
        if (_user.Role == Role.Admin)
        {
            RoleBadgeBorder.Background = (SolidColorBrush)FindResource("PillPurpleBg");
            RoleBadgeText.Foreground = (SolidColorBrush)FindResource("PillPurpleText");
            RoleBadgeText.Text = "👑 ADMIN";
            RoleSubtext.Text = "Full Access";
        }
        else
        {
            RoleBadgeBorder.Background = (SolidColorBrush)FindResource("PillGreenBg");
            RoleBadgeText.Foreground = (SolidColorBrush)FindResource("PillGreenText");
            RoleBadgeText.Text = "🛒 CASHIER";
            RoleSubtext.Text = "POS Terminal";
        }
    }

    private string GetNewPassword()
    {
        return ShowNewPasswordToggle.IsChecked == true
            ? RevealedNewPasswordTextBox.Text
            : NewPasswordInput.Password;
    }

    private string GetConfirmPassword()
    {
        return ShowConfirmPasswordToggle.IsChecked == true
            ? RevealedConfirmPasswordTextBox.Text
            : ConfirmPasswordInput.Password;
    }

    private void NewPasswordInput_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        NewPasswordPlaceholder.Visibility = string.IsNullOrEmpty(NewPasswordInput.Password)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RevealedNewPasswordTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        NewPasswordPlaceholder.Visibility = string.IsNullOrEmpty(RevealedNewPasswordTextBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ShowNewPasswordToggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (ShowNewPasswordToggle.IsChecked == true)
        {
            RevealedNewPasswordTextBox.Text = NewPasswordInput.Password;
            NewPasswordInput.Visibility = Visibility.Collapsed;
            RevealedNewPasswordTextBox.Visibility = Visibility.Visible;
            RevealedNewPasswordTextBox.Focus();
            RevealedNewPasswordTextBox.CaretIndex = RevealedNewPasswordTextBox.Text.Length;
        }
        else
        {
            NewPasswordInput.Password = RevealedNewPasswordTextBox.Text;
            RevealedNewPasswordTextBox.Visibility = Visibility.Collapsed;
            NewPasswordInput.Visibility = Visibility.Visible;
            NewPasswordInput.Focus();
        }
    }

    private void ConfirmPasswordInput_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        ConfirmPasswordPlaceholder.Visibility = string.IsNullOrEmpty(ConfirmPasswordInput.Password)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RevealedConfirmPasswordTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ConfirmPasswordPlaceholder.Visibility = string.IsNullOrEmpty(RevealedConfirmPasswordTextBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ShowConfirmPasswordToggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (ShowConfirmPasswordToggle.IsChecked == true)
        {
            RevealedConfirmPasswordTextBox.Text = ConfirmPasswordInput.Password;
            ConfirmPasswordInput.Visibility = Visibility.Collapsed;
            RevealedConfirmPasswordTextBox.Visibility = Visibility.Visible;
            RevealedConfirmPasswordTextBox.Focus();
            RevealedConfirmPasswordTextBox.CaretIndex = RevealedConfirmPasswordTextBox.Text.Length;
        }
        else
        {
            ConfirmPasswordInput.Password = RevealedConfirmPasswordTextBox.Text;
            RevealedConfirmPasswordTextBox.Visibility = Visibility.Collapsed;
            ConfirmPasswordInput.Visibility = Visibility.Visible;
            ConfirmPasswordInput.Focus();
        }
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        HandleCancel();
    }

    private void CloseIconButton_OnClick(object sender, RoutedEventArgs e)
    {
        HandleCancel();
    }

    private void HandleCancel()
    {
        var hasPasswordInput = !string.IsNullOrEmpty(GetNewPassword()) ||
                               !string.IsNullOrEmpty(GetConfirmPassword());

        if (hasPasswordInput)
        {
            var shouldDiscard = MessageBoxWindow.Confirm(
                "Are you sure you want to cancel? Any entered password will be discarded.",
                "Discard Password Changes",
                confirmButtonText: "Yes, Discard",
                cancelButtonText: "Keep Editing",
                owner: this,
                type: MessageBoxType.Warning,
                subtitle: "User Security Modification");

            if (!shouldDiscard)
            {
                return;
            }
        }

        DialogResult = false;
        Close();
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var newPassword = GetNewPassword();
        var confirmPassword = GetConfirmPassword();

        bool hasNew = !string.IsNullOrEmpty(newPassword);
        bool hasConfirm = !string.IsNullOrEmpty(confirmPassword);

        // Requirement 4: If the password fields are left blank, DO NOT change the existing password.
        // Allow the user to save cleanly when no password change is intended.
        if (!hasNew && !hasConfirm)
        {
            PasswordChanged = false;

            MessageBoxWindow.Show(
                $"No password changes were made for staff user '{_user.Username}'. The existing password remains active.",
                "User Details Preserved",
                this,
                MessageBoxType.Info,
                "OK",
                "Password Unchanged");

            DialogResult = true;
            Close();
            return;
        }

        // Validate partial inputs
        if (hasNew && !hasConfirm)
        {
            MessageBoxWindow.Show(
                "Please confirm the new password by entering it in the Confirm Password field.",
                "Missing Required Field",
                this,
                MessageBoxType.Error);
            FocusConfirmPassword();
            return;
        }

        if (!hasNew && hasConfirm)
        {
            MessageBoxWindow.Show(
                "Please enter the new password in the New Password field.",
                "Missing Required Field",
                this,
                MessageBoxType.Error);
            FocusNewPassword();
            return;
        }

        // Validate password against the existing password requirements policy
        var passwordValidation = LoginValidator.ValidatePassword(newPassword, _user.Username);
        if (!passwordValidation.IsValid)
        {
            MessageBoxWindow.Show(
                passwordValidation.ErrorMessage ?? "Password does not meet the security requirements.",
                "Password Policy Violation",
                this,
                MessageBoxType.Error);
            FocusNewPassword();
            return;
        }

        // Require Confirm Password to match New Password exactly
        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            MessageBoxWindow.Show(
                "Passwords do not match. Please ensure both password fields are identical.",
                "Password Mismatch",
                this,
                MessageBoxType.Error);
            FocusConfirmPassword();
            return;
        }

        // Confirmation prompt before persisting password change
        var confirmed = MessageBoxWindow.Confirm(
            $"Are you sure you want to update the password for staff user '{_user.Username}'?",
            "Confirm Password Update",
            confirmButtonText: "Update Password",
            cancelButtonText: "Keep Editing",
            owner: this,
            type: MessageBoxType.Question,
            subtitle: "Staff Credential Update");

        if (!confirmed)
        {
            return;
        }

        // Requirement 5: Update ONLY that user's password in the database.
        // Do not modify the name, username, role, or any other user information.
        try
        {
            var hashedPassword = PasswordHasher.Default.HashPassword(newPassword);
            var connStr = DatabaseInitializer.ResolveConnectionString();
            using var connection = new MySqlConnection(connStr);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE `Users` SET `PasswordHash` = @passwordHash WHERE `Id` = @id;";
            cmd.Parameters.AddWithValue("@passwordHash", hashedPassword);
            cmd.Parameters.AddWithValue("@id", _user.Id);
            cmd.ExecuteNonQuery();

            _user.PasswordHash = hashedPassword;
            PasswordChanged = true;

            // Success message using the application's existing UI style
            MessageBoxWindow.Show(
                $"The password for staff user '{_user.Username}' has been successfully updated.",
                "Password Updated Successfully",
                this,
                MessageBoxType.Success,
                "Done",
                "Security Credentials Updated");

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(
                $"Failed to update password in database: {ex.Message}. Please ensure the database service is running.",
                "Database Update Error",
                this,
                MessageBoxType.Error);
        }
    }

    private void FocusNewPassword()
    {
        if (ShowNewPasswordToggle.IsChecked == true)
        {
            RevealedNewPasswordTextBox.Focus();
            RevealedNewPasswordTextBox.SelectAll();
        }
        else
        {
            NewPasswordInput.Focus();
            NewPasswordInput.SelectAll();
        }
    }

    private void FocusConfirmPassword()
    {
        if (ShowConfirmPasswordToggle.IsChecked == true)
        {
            RevealedConfirmPasswordTextBox.Focus();
            RevealedConfirmPasswordTextBox.SelectAll();
        }
        else
        {
            ConfirmPasswordInput.Focus();
            ConfirmPasswordInput.SelectAll();
        }
    }
}
