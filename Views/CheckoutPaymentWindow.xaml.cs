using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OopsGrocery.Models;

namespace OopsGrocery.Views;

public partial class CheckoutPaymentWindow : Window
{
    private readonly decimal _finalTotal;
    private bool _isProcessing;

    public string OrderId { get; }
    public string CustomerType { get; }
    public decimal Subtotal { get; }
    public decimal Discount { get; }
    public decimal NewSubtotal { get; }
    public decimal Vat { get; }
    public decimal FinalTotal => _finalTotal;

    public decimal ConfirmedCashReceived { get; private set; }
    public decimal ConfirmedChange { get; private set; }
    public DateTime ConfirmedAt { get; private set; } = DateTime.Now;

    public CheckoutPaymentWindow(
        string orderId,
        string customerType,
        IEnumerable<CartItem> items,
        decimal subtotal,
        decimal discount,
        decimal newSubtotal,
        decimal vat,
        decimal finalTotal)
    {
        InitializeComponent();

        OrderId = orderId;
        CustomerType = customerType;
        Subtotal = subtotal;
        Discount = discount;
        NewSubtotal = newSubtotal;
        Vat = vat;
        _finalTotal = finalTotal;

        HeaderOrderIdText.Text = $"Order ID: {orderId}";
        CustomerTypeBadgeText.Text = customerType;

        if (customerType.Contains("Senior", StringComparison.OrdinalIgnoreCase) || customerType.Contains("PWD", StringComparison.OrdinalIgnoreCase))
        {
            CustomerTypeBadge.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
            CustomerTypeBadgeText.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
        }

        var itemList = items.ToList();
        OrderItemsList.ItemsSource = itemList;
        ItemCountSummaryText.Text = $"{itemList.Sum(i => i.Quantity)} Items ({itemList.Count} lines)";

        SubtotalText.Text = $"₱{subtotal:N2}";
        DiscountText.Text = discount > 0 ? $"-₱{discount:N2}" : "₱0.00";
        NewSubtotalText.Text = $"₱{newSubtotal:N2}";
        VatText.Text = $"₱{vat:N2}";
        FinalTotalText.Text = $"₱{finalTotal:N2}";

        // Initialize cash received to exact total
        CashReceivedTextBox.Text = finalTotal.ToString("F2", CultureInfo.InvariantCulture);

        MouseDown += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        };

        Loaded += (_, _) =>
        {
            CashReceivedTextBox.Focus();
            CashReceivedTextBox.SelectAll();
            UpdateTenderCalculations();
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

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CashReceivedTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateTenderCalculations();
    }

    private void CashReceivedTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            if (ConfirmPaymentButton.IsEnabled)
            {
                ConfirmPaymentButton_OnClick(sender, e);
            }
        }
    }

    private void PresetExact_Click(object sender, RoutedEventArgs e)
    {
        CashReceivedTextBox.Text = _finalTotal.ToString("F2", CultureInfo.InvariantCulture);
        CashReceivedTextBox.Focus();
        CashReceivedTextBox.SelectAll();
    }

    private void PresetAmount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tagStr && decimal.TryParse(tagStr, out var preset))
        {
            CashReceivedTextBox.Text = preset.ToString("F2", CultureInfo.InvariantCulture);
            CashReceivedTextBox.Focus();
            CashReceivedTextBox.SelectAll();
        }
    }

    private void UpdateTenderCalculations()
    {
        var rawText = CashReceivedTextBox.Text?.Trim() ?? string.Empty;

        if (decimal.TryParse(rawText, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var cash) ||
            decimal.TryParse(rawText, out cash))
        {
            if (cash >= _finalTotal)
            {
                var change = cash - _finalTotal;
                ChangeAmountText.Text = $"₱{change:N2}";
                ChangeAmountText.Foreground = (SolidColorBrush)FindResource("SuccessGreen");
                ChangeCardBorder.Background = new SolidColorBrush(Color.FromRgb(240, 253, 244));
                ChangeCardBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(134, 239, 172));
                ChangeLabelText.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
                ChangeSubtext.Text = "Return to customer in cash";

                CashErrorText.Visibility = Visibility.Collapsed;
                CashInputBorder.BorderBrush = (SolidColorBrush)FindResource("BorderBrushLight");
                ConfirmPaymentButton.IsEnabled = !_isProcessing;
            }
            else
            {
                var shortage = _finalTotal - cash;
                ChangeAmountText.Text = "-₱" + shortage.ToString("N2");
                ChangeAmountText.Foreground = (SolidColorBrush)FindResource("DangerRed");
                ChangeCardBorder.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
                ChangeCardBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202));
                ChangeLabelText.Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27));
                ChangeSubtext.Text = "Shortage amount";

                CashErrorText.Text = $"⚠️ Insufficient cash received. Short by ₱{shortage:N2}. Cash must be at least ₱{_finalTotal:N2}.";
                CashErrorText.Visibility = Visibility.Visible;
                CashInputBorder.BorderBrush = (SolidColorBrush)FindResource("DangerRed");
                ConfirmPaymentButton.IsEnabled = false;
            }
        }
        else
        {
            ChangeAmountText.Text = "₱0.00";
            ChangeCardBorder.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
            ChangeCardBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202));
            CashErrorText.Text = "⚠️ Please enter a valid cash amount in Philippine Pesos.";
            CashErrorText.Visibility = Visibility.Visible;
            CashInputBorder.BorderBrush = (SolidColorBrush)FindResource("DangerRed");
            ConfirmPaymentButton.IsEnabled = false;
        }
    }

    private void ConfirmPaymentButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_isProcessing) return; // Prevent accidental duplicate payments

        var rawText = CashReceivedTextBox.Text?.Trim() ?? string.Empty;
        if (!decimal.TryParse(rawText, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var cash) &&
            !decimal.TryParse(rawText, out cash))
        {
            UpdateTenderCalculations();
            return;
        }

        if (cash < _finalTotal)
        {
            UpdateTenderCalculations();
            return;
        }

        // Lock processing to prevent double confirmation
        _isProcessing = true;
        ConfirmPaymentButton.IsEnabled = false;

        ConfirmedCashReceived = cash;
        ConfirmedChange = cash - _finalTotal;
        ConfirmedAt = DateTime.Now;

        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
