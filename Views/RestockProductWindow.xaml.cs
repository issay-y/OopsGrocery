using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OopsGrocery.Models;

namespace OopsGrocery.Views;

public partial class RestockProductWindow : Window
{
    private static readonly Brush NormalBorderBrush = (Brush)new BrushConverter().ConvertFromString("#CBD5E1")!;
    private static readonly Brush ErrorBorderBrush = (Brush)new BrushConverter().ConvertFromString("#EF4444")!;

    private readonly ProductItem _item;

    public int RestockedQuantity { get; private set; }

    public RestockProductWindow(ProductItem item)
    {
        _item = item ?? throw new ArgumentNullException(nameof(item));
        InitializeComponent();

        ItemNameText.Text = $"{_item.Name} ({_item.FormattedId})";
        ItemCategoryText.Text = $"Category: {_item.Category} · Current Price: {_item.FormattedPriceWithUnit}";
        CurrentStockText.Text = $"{_item.StockQuantity} {_item.Unit}";

        // Paste sanitizer to block pasting non-numeric text
        DataObject.AddPastingHandler(AddQuantityInput, OnQuantityPaste);

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
                DialogResult = false;
                Close();
            }
        };

        Loaded += (_, _) =>
        {
            UpdateResultingStock();
            AddQuantityInput.Focus();
            AddQuantityInput.SelectAll();
        };
    }

    #region Input Filtering

    /// <summary>
    /// Restock quantity: Whole positive numbers only.
    /// Letters, decimals, negative signs, and special characters are strictly blocked.
    /// </summary>
    private void AddQuantityInput_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (!e.Text.All(char.IsDigit))
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Blocks space key.
    /// </summary>
    private void NumericInput_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            e.Handled = true;
        }
    }

    private void OnQuantityPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string));
            if (string.IsNullOrEmpty(text) || !text.All(char.IsDigit))
            {
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
    }

    #endregion

    private void UpdateResultingStock()
    {
        if (int.TryParse(AddQuantityInput.Text.Trim(), out var addQty) && addQty >= 0)
        {
            var total = _item.StockQuantity + addQty;
            ResultingStockText.Text = $"{total} {_item.Unit} (+{addQty})";
        }
        else
        {
            ResultingStockText.Text = "Invalid quantity";
        }
    }

    private void AddQuantityInput_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ClearValidationErrors();
        if (IsLoaded)
        {
            UpdateResultingStock();
        }
    }

    private void QuickPreset_OnClick(object sender, RoutedEventArgs e)
    {
        ClearValidationErrors();
        if (sender is Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out var preset))
        {
            AddQuantityInput.Text = preset.ToString();
            AddQuantityInput.Focus();
            AddQuantityInput.SelectAll();
        }
    }

    private void ShowValidationError(string message)
    {
        ValidationErrorMessage.Text = message;
        ValidationErrorBanner.Visibility = Visibility.Visible;
        QuantityBorder.BorderBrush = ErrorBorderBrush;

        MessageBoxWindow.Show(message, "Restock Validation", this, MessageBoxType.Warning);

        AddQuantityInput.Focus();
        AddQuantityInput.SelectAll();
    }

    private void ClearValidationErrors()
    {
        if (ValidationErrorBanner.Visibility != Visibility.Collapsed)
        {
            ValidationErrorBanner.Visibility = Visibility.Collapsed;
        }
        QuantityBorder.BorderBrush = NormalBorderBrush;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        ClearValidationErrors();

        var qtyText = AddQuantityInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(qtyText))
        {
            ShowValidationError("Please enter the restock quantity to add.");
            return;
        }

        if (!int.TryParse(qtyText, out var addQty))
        {
            ShowValidationError("Please enter a valid whole number for restock quantity. Letters, decimals, and special characters are not allowed.");
            return;
        }

        if (addQty <= 0)
        {
            ShowValidationError("Restock quantity must be greater than 0 units.");
            return;
        }

        if (addQty > 100000)
        {
            ShowValidationError("Restock quantity cannot exceed 100,000 units per operation.");
            return;
        }

        RestockedQuantity = addQty;
        DialogResult = true;
        Close();
    }
}
