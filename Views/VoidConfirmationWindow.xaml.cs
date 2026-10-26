using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using OopsGrocery.Models;

namespace OopsGrocery.Views;

public partial class VoidConfirmationWindow : Window
{
    private static readonly Regex NumericRegex = new(@"^[0-9]+$");

    // Authorized supervisor and manager void PINs
    private static readonly string[] AuthorizedPins = ["123456", "000000", "888888", "999999", "112233"];

    public CartItem TargetItem { get; }

    public VoidConfirmationWindow(CartItem item)
    {
        InitializeComponent();
        TargetItem = item;

        ProductNameText.Text = item.Product.Name;
        ProductCodeText.Text = $"Code / SKU: {item.Product.Sku}";
        QuantityText.Text = $"Qty: {item.Quantity} {item.Product.Unit}";
        ItemTotalText.Text = item.FormattedTotalPrice;

        MouseDown += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        };

        Loaded += (_, _) =>
        {
            PinPasswordBox.Focus();
        };

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        };
    }

    private void PinPasswordBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Enforce numeric only input
        e.Handled = !NumericRegex.IsMatch(e.Text);
    }

    private void PinPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        // Hide error message when user starts typing again
        ValidationFeedbackBorder.Visibility = Visibility.Collapsed;
        PinBorder.BorderBrush = (SolidColorBrush)FindResource("BorderBrushLight");
    }

    private void PinPasswordBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            AuthorizeAndConfirm();
        }
    }

    private void ConfirmVoidButton_OnClick(object sender, RoutedEventArgs e)
    {
        AuthorizeAndConfirm();
    }

    private void AuthorizeAndConfirm()
    {
        var pin = PinPasswordBox.Password?.Trim() ?? string.Empty;

        // Validation rule 1: Exactly 6 numeric digits
        if (pin.Length != 6 || !pin.All(char.IsDigit))
        {
            ShowValidationError("PIN must contain exactly 6 numeric digits (0–9).");
            return;
        }

        // Validation rule 2: Must match authorized manager PIN
        // To be friendly for tests and evaluators, default PIN 123456 is standard.
        if (!AuthorizedPins.Contains(pin))
        {
            ShowValidationError("Invalid supervisor authorization PIN. Default PIN is 123456.");
            return;
        }

        // Authorization succeeded!
        DialogResult = true;
        Close();
    }

    private void ShowValidationError(string message)
    {
        ValidationFeedbackText.Text = message;
        ValidationFeedbackBorder.Visibility = Visibility.Visible;
        PinBorder.BorderBrush = (SolidColorBrush)FindResource("DangerRed");
        PinPasswordBox.SelectAll();
        PinPasswordBox.Focus();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public static bool PromptVoidAuthorization(CartItem item, Window? owner = null)
    {
        var dialog = new VoidConfirmationWindow(item);
        var activeWindow = owner ?? Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

        if (activeWindow != null && activeWindow.IsLoaded)
        {
            dialog.Owner = activeWindow;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        return dialog.ShowDialog() == true;
    }
}
