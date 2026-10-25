using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySqlConnector;
using OopsGrocery.Services;

namespace OopsGrocery.Views;

public partial class AddUserWindow : Window
{
    private static readonly Regex AllowedNameCharRegex = new(@"^[a-zA-Z\s\.\-]+$");

    public string CreatedUsername { get; private set; } = string.Empty;

    public AddUserWindow()
    {
        InitializeComponent();

        DataObject.AddPastingHandler(FullNameTextBox, OnFullNamePasting);

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
            FullNameTextBox.Focus();
        };
    }

    private void OnFullNamePasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string));
            if (!string.IsNullOrEmpty(text) && !AllowedNameCharRegex.IsMatch(text))
            {
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
    }

    private void FullNameTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Allow only alphabetic letters, spaces, periods (.), and hyphens (-)
        e.Handled = !AllowedNameCharRegex.IsMatch(e.Text);
    }

    private string GetPassword()
    {
        return ShowPasswordToggle.IsChecked == true
            ? RevealedPasswordTextBox.Text
            : PasswordInput.Password;
    }

    private string GetConfirmPassword()
    {
        return ShowConfirmPasswordToggle.IsChecked == true
            ? RevealedConfirmPasswordTextBox.Text
            : ConfirmPasswordInput.Password;
    }

    private void FullNameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        FullNamePlaceholder.Visibility = string.IsNullOrWhiteSpace(FullNameTextBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UsernameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        UsernamePlaceholder.Visibility = string.IsNullOrWhiteSpace(UsernameTextBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void PasswordInput_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordInput.Password)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RevealedPasswordTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        PasswordPlaceholder.Visibility = string.IsNullOrEmpty(RevealedPasswordTextBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
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
        var hasInput = !string.IsNullOrWhiteSpace(FullNameTextBox.Text) ||
                       !string.IsNullOrWhiteSpace(UsernameTextBox.Text) ||
                       !string.IsNullOrEmpty(GetPassword()) ||
                       !string.IsNullOrEmpty(GetConfirmPassword());

        if (hasInput)
        {
            var shouldDiscard = MessageBoxWindow.Confirm(
                "Are you sure you want to cancel? Any entered information will be discarded.",
                "Discard New Account",
                confirmButtonText: "Yes, Discard",
                cancelButtonText: "Keep Editing",
                owner: this,
                type: MessageBoxType.Warning,
                subtitle: "User Provisioning Notice");

            if (!shouldDiscard)
            {
                return;
            }
        }

        DialogResult = false;
        Close();
    }

    private void CreateAccountButton_OnClick(object sender, RoutedEventArgs e)
    {
        var fullName = FullNameTextBox.Text.Trim();
        var username = UsernameTextBox.Text.Trim();
        var password = GetPassword();
        var confirmPassword = GetConfirmPassword();

        // 1. Validate Full Name (Required field)
        if (string.IsNullOrWhiteSpace(fullName))
        {
            MessageBoxWindow.Show(
                "Please enter the staff member's full name. This field cannot be left blank.",
                "Missing Required Field",
                this,
                MessageBoxType.Error);
            FullNameTextBox.Focus();
            return;
        }

        // 1b. Validate Full Name Characters
        if (!AllowedNameCharRegex.IsMatch(fullName))
        {
            MessageBoxWindow.Show(
                "Full Name can only contain alphabetic letters, spaces, periods (.), and hyphens (-). Numbers and other symbols are not permitted.",
                "Invalid Name Format",
                this,
                MessageBoxType.Error);
            FullNameTextBox.Focus();
            FullNameTextBox.SelectAll();
            return;
        }

        // 1c. Validate Full Name Format (Must include at least First Name and Last Name)
        var nameParts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var hasAtLeastTwoValidParts = nameParts.Length >= 2 && nameParts.All(p => p.Any(char.IsLetter));
        if (!hasAtLeastTwoValidParts)
        {
            MessageBoxWindow.Show(
                "Please enter the complete name following the format:\nFirst Name, Middle Initial, Last Name\n(e.g., Maria C. Santos or John-Paul Smith).",
                "Incomplete Name Format",
                this,
                MessageBoxType.Error);
            FullNameTextBox.Focus();
            FullNameTextBox.SelectAll();
            return;
        }

        // 2. Validate Username (Required field)
        if (string.IsNullOrWhiteSpace(username))
        {
            MessageBoxWindow.Show(
                "Please enter a username for the cashier account. This field cannot be left blank.",
                "Missing Required Field",
                this,
                MessageBoxType.Error);
            UsernameTextBox.Focus();
            return;
        }

        var usernameValidation = LoginValidator.ValidateUsername(username);
        if (!usernameValidation.IsValid)
        {
            MessageBoxWindow.Show(
                usernameValidation.ErrorMessage ?? "Please enter a valid username.",
                "Invalid Username",
                this,
                MessageBoxType.Error);
            UsernameTextBox.Focus();
            return;
        }

        // 2b. Check for unique username in Database (strictly case-sensitive)
        try
        {
            var connStr = DatabaseInitializer.ResolveConnectionString();
            using var connection = new MySqlConnection(connStr);
            connection.Open();

            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE BINARY Username = @username;";
            checkCmd.Parameters.AddWithValue("@username", username);
            var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

            if (exists)
            {
                MessageBoxWindow.Show(
                    $"The username '{username}' is already in use by another store account. Please choose a different username.",
                    "Username Already Taken",
                    this,
                    MessageBoxType.Error);
                UsernameTextBox.Focus();
                UsernameTextBox.SelectAll();
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(
                $"Unable to verify database connection: {ex.Message}. Ensure MariaDB/MySQL is running.",
                "Database Connection Error",
                this,
                MessageBoxType.Error);
            return;
        }

        // 3. Validate Password (Required field)
        if (string.IsNullOrEmpty(password))
        {
            MessageBoxWindow.Show(
                "Please enter a password for the account. This field cannot be left blank.",
                "Missing Required Field",
                this,
                MessageBoxType.Error);
            FocusPassword();
            return;
        }

        var passwordValidation = LoginValidator.ValidatePassword(password, username);
        if (!passwordValidation.IsValid)
        {
            MessageBoxWindow.Show(
                passwordValidation.ErrorMessage ?? "Password does not meet the security requirements.",
                "Password Policy Violation",
                this,
                MessageBoxType.Error);
            FocusPassword();
            return;
        }

        // 4. Validate Confirm Password (Required field)
        if (string.IsNullOrEmpty(confirmPassword))
        {
            MessageBoxWindow.Show(
                "Please confirm the password by entering it in the Confirm Password field.",
                "Missing Required Field",
                this,
                MessageBoxType.Error);
            FocusConfirmPassword();
            return;
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            MessageBoxWindow.Show(
                "Passwords do not match. Please ensure both password fields are identical.",
                "Password Mismatch",
                this,
                MessageBoxType.Error);
            FocusConfirmPassword();
            return;
        }

        // 5. Confirmation message box
        var confirmed = MessageBoxWindow.Confirm(
            $"Are you sure you want to provision a new Cashier account for '{username}' ({fullName})?",
            "Confirm Account Creation",
            confirmButtonText: "Create Account",
            cancelButtonText: "Review Details",
            owner: this,
            type: MessageBoxType.Question,
            subtitle: "Staff Account Provisioning");

        if (!confirmed)
        {
            return;
        }

        // 6. Hash Password & Persist into Database
        try
        {
            var hashedPassword = PasswordHasher.Default.HashPassword(password);
            var connStr = DatabaseInitializer.ResolveConnectionString();
            using var connection = new MySqlConnection(connStr);
            connection.Open();

            // Attempt to ensure FullName column is present in Users table
            try
            {
                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE `Users` ADD COLUMN `FullName` VARCHAR(150) NULL DEFAULT NULL AFTER `Username`;";
                alterCmd.ExecuteNonQuery();
            }
            catch { }

            using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = """
                INSERT INTO `Users` (`Username`, `FullName`, `PasswordHash`, `Role`, `IsActive`)
                VALUES (@username, @fullName, @passwordHash, 'Cashier', 1);
                """;
            insertCmd.Parameters.AddWithValue("@username", username);
            insertCmd.Parameters.AddWithValue("@fullName", fullName);
            insertCmd.Parameters.AddWithValue("@passwordHash", hashedPassword);

            try
            {
                insertCmd.ExecuteNonQuery();
            }
            catch (MySqlException ex) when (ex.Number == 1054) // Fallback if FullName column could not be added
            {
                using var fallbackCmd = connection.CreateCommand();
                fallbackCmd.CommandText = "INSERT INTO `Users` (`Username`, `PasswordHash`, `Role`, `IsActive`) VALUES (@username, @passwordHash, 'Cashier', 1);";
                fallbackCmd.Parameters.AddWithValue("@username", username);
                fallbackCmd.Parameters.AddWithValue("@passwordHash", hashedPassword);
                fallbackCmd.ExecuteNonQuery();
            }

            CreatedUsername = username;

            // 7. Success message box
            MessageBoxWindow.Show(
                $"Cashier account for '{username}' ({fullName}) has been successfully created and is ready for POS login.",
                "Account Created Successfully",
                this,
                MessageBoxType.Success,
                "Done",
                "Staff Provisioning Complete");

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(
                $"Failed to save user account to database: {ex.Message}",
                "Database Save Error",
                this,
                MessageBoxType.Error);
        }
    }

    private void FocusPassword()
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
