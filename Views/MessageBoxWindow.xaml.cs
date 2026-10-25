using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace OopsGrocery.Views;

public enum MessageBoxType
{
    Error,
    Success,
    Warning,
    Question,
    Info
}

public partial class MessageBoxWindow : Window
{
    public bool IsConfirmationMode { get; set; }
    public string? CancelButtonText { get; set; }

    public MessageBoxWindow(
        string message,
        string title = "Oops Grocery Alert",
        MessageBoxType type = MessageBoxType.Error,
        string? buttonText = null,
        string? subtitle = null)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            SubtitleText.Text = subtitle;
        }

        ConfigureType(type, buttonText);

        var createdAt = DateTime.UtcNow;

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
                DialogResult = false;
                Close();
                return;
            }

            // Only accept Enter / Space if the dialog has been open for at least 300ms,
            // preventing the form submission keystroke from immediately auto-dismissing this alert.
            if (e.Key is Key.Enter or Key.Space)
            {
                if ((DateTime.UtcNow - createdAt).TotalMilliseconds > 300)
                {
                    DialogResult = true;
                    Close();
                }
            }
        };

        Loaded += (_, _) =>
        {
            if (IsConfirmationMode)
            {
                ConfirmButton.Focus();
            }
            else
            {
                ActionButton.Focus();
            }
        };
    }

    public void ApplyConfirmationMode()
    {
        ActionButton.Visibility = Visibility.Collapsed;
        ConfirmationGrid.Visibility = Visibility.Visible;
        if (!string.IsNullOrWhiteSpace(CancelButtonText))
        {
            CancelButton.Content = CancelButtonText;
        }
    }

    private void ConfigureType(MessageBoxType type, string? buttonText)
    {
        ErrorIcon.Visibility = Visibility.Collapsed;
        SuccessIcon.Visibility = Visibility.Collapsed;
        QuestionIcon.Visibility = Visibility.Collapsed;

        switch (type)
        {
            case MessageBoxType.Success:
                IconBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
                SuccessIcon.Visibility = Visibility.Visible;
                ActionButton.Content = buttonText ?? "Continue";
                ConfirmButton.Content = buttonText ?? "Continue";
                break;

            case MessageBoxType.Warning:
            case MessageBoxType.Question:
                IconBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                QuestionIcon.Visibility = Visibility.Visible;
                ActionButton.Content = buttonText ?? "Confirm";
                ConfirmButton.Content = buttonText ?? "Confirm";
                break;

            case MessageBoxType.Info:
                IconBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF"));
                QuestionIcon.Visibility = Visibility.Visible;
                ActionButton.Content = buttonText ?? "OK";
                ConfirmButton.Content = buttonText ?? "OK";
                break;

            case MessageBoxType.Error:
            default:
                IconBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                ErrorIcon.Visibility = Visibility.Visible;
                ActionButton.Content = buttonText ?? "Got it, try again";
                ConfirmButton.Content = buttonText ?? "OK";
                break;
        }
    }

    private void ActionButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CloseIconButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public static bool? Show(
        string message,
        string title = "Oops Grocery Alert",
        Window? owner = null,
        MessageBoxType type = MessageBoxType.Error,
        string? buttonText = null,
        string? subtitle = null)
    {
        var window = new MessageBoxWindow(message, title, type, buttonText, subtitle);
        var activeWindow = owner ?? Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

        if (activeWindow != null && activeWindow.IsLoaded)
        {
            window.Owner = activeWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        return window.ShowDialog();
    }

    public static bool Confirm(
        string message,
        string title = "Oops Grocery Confirmation",
        string confirmButtonText = "Confirm",
        string cancelButtonText = "Cancel",
        Window? owner = null,
        MessageBoxType type = MessageBoxType.Question,
        string? subtitle = "Action Confirmation Required")
    {
        var window = new MessageBoxWindow(message, title, type, confirmButtonText, subtitle)
        {
            IsConfirmationMode = true,
            CancelButtonText = cancelButtonText
        };
        window.ApplyConfirmationMode();

        var activeWindow = owner ?? Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

        if (activeWindow != null && activeWindow.IsLoaded)
        {
            window.Owner = activeWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        return window.ShowDialog() == true;
    }
}


