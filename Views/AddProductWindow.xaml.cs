using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OopsGrocery.Models;

namespace OopsGrocery.Views;

public partial class AddProductWindow : Window
{
    private static readonly Brush NormalBorderBrush = (Brush)new BrushConverter().ConvertFromString("#CBD5E1")!;
    private static readonly Brush ErrorBorderBrush = (Brush)new BrushConverter().ConvertFromString("#EF4444")!;

    public static readonly List<string> PreExistingUnits = new()
    {
        "pc",
        "pack",
        "btl",
        "can",
        "box",
        "kg",
        "g",
        "pouch",
        "bag",
        "roll",
        "tub",
        "bar",
        "sachet",
        "tray",
        "head",
        "bundle",
        "dz"
    };

    public ProductItem? CreatedProduct { get; private set; }

    public AddProductWindow(List<string> categories, IEnumerable<string>? existingUnits = null)
    {
        InitializeComponent();

        // 1. Categories dropdown
        CategoryCombo.ItemsSource = categories;
        if (categories.Count > 0)
        {
            CategoryCombo.SelectedIndex = 0;
        }

        // 2. Pre-existing units dropdown
        var unitsList = new List<string>(PreExistingUnits);
        if (existingUnits != null)
        {
            foreach (var u in existingUnits)
            {
                if (!string.IsNullOrWhiteSpace(u))
                {
                    var clean = u.Trim().ToLowerInvariant();
                    if (!unitsList.Contains(clean, StringComparer.OrdinalIgnoreCase))
                    {
                        unitsList.Add(clean);
                    }
                }
            }
        }
        UnitCombo.ItemsSource = unitsList;
        UnitCombo.SelectedItem = unitsList.Contains("pc") ? "pc" : unitsList.FirstOrDefault();

        // 3. Paste sanitizers
        DataObject.AddPastingHandler(NameInput, OnNamePaste);
        DataObject.AddPastingHandler(PriceInput, OnPricePaste);
        DataObject.AddPastingHandler(QuantityInput, OnQuantityPaste);

        // 4. Live previews & error resets
        UpdatePreviewCategory();
        CategoryCombo.SelectionChanged += (_, _) =>
        {
            ClearValidationErrors();
            UpdatePreviewCategory();
        };

        UnitCombo.SelectionChanged += (_, _) => ClearValidationErrors();
        NameInput.TextChanged += (_, _) => ClearValidationErrors();
        PriceInput.TextChanged += (_, _) => ClearValidationErrors();
        QuantityInput.TextChanged += (_, _) => ClearValidationErrors();
        ImagePathInput.TextChanged += (_, _) => UpdateImagePreview(ImagePathInput.Text);

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

        Loaded += (_, _) => NameInput.Focus();
    }

    #region Real-Time Input Filtering

    /// <summary>
    /// Item Name: letters, numbers, spaces, and safe grocery punctuation (-, &, ., (, ), ,) only.
    /// Disallows special characters like < > ; " ' \ / = + ~ ^ % $ @ ! { } [ ] * ? | `
    /// </summary>
    private void NameInput_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        foreach (char c in e.Text)
        {
            if (!char.IsLetterOrDigit(c) && c != ' ' && c != '-' && c != '&' && c != '.' && c != '(' && c != ')' && c != ',')
            {
                e.Handled = true;
                return;
            }
        }
    }

    /// <summary>
    /// Price: Numbers and at most one decimal point with max 2 decimal places.
    /// Letters, negative signs, spaces, and special characters are strictly blocked.
    /// </summary>
    private void PriceInput_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox tb) return;

        // Disallow anything other than digits and '.'
        if (!e.Text.All(c => char.IsDigit(c) || c == '.'))
        {
            e.Handled = true;
            return;
        }

        // Test prospective text after insertion
        int selStart = tb.SelectionStart;
        int selLen = tb.SelectionLength;
        string current = tb.Text;
        string prospective = current.Remove(selStart, selLen).Insert(selStart, e.Text);

        // Regex: only digits and at most one dot with up to 2 decimals
        if (!Regex.IsMatch(prospective, @"^\d*(\.\d{0,2})?$"))
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Stock Quantity: Whole positive numbers only.
    /// Letters, decimals, negative signs, and special characters are strictly blocked.
    /// </summary>
    private void QuantityInput_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (!e.Text.All(char.IsDigit))
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Blocks space key on numeric inputs (Price and Quantity).
    /// </summary>
    private void NumericInput_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            e.Handled = true;
        }
    }

    private void OnNamePaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string));
            if (!string.IsNullOrEmpty(text) && text.Any(c => !char.IsLetterOrDigit(c) && c != ' ' && c != '-' && c != '&' && c != '.' && c != '(' && c != ')' && c != ','))
            {
                e.CancelCommand();
            }
        }
    }

    private void OnPricePaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string));
            if (sender is TextBox tb && !string.IsNullOrEmpty(text))
            {
                int selStart = tb.SelectionStart;
                int selLen = tb.SelectionLength;
                string current = tb.Text;
                string prospective = current.Remove(selStart, selLen).Insert(selStart, text);

                if (!Regex.IsMatch(prospective, @"^\d*(\.\d{0,2})?$"))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
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
    }

    #endregion

    #region Validation Helpers

    private void ShowValidationError(string message, Control targetControl, Border? targetBorder = null)
    {
        ValidationErrorMessage.Text = message;
        ValidationErrorBanner.Visibility = Visibility.Visible;

        if (targetBorder != null)
        {
            targetBorder.BorderBrush = ErrorBorderBrush;
        }

        MessageBoxWindow.Show(message, "Validation Error", this, MessageBoxType.Warning);

        targetControl.Focus();
        if (targetControl is TextBox tb)
        {
            tb.SelectAll();
        }
    }

    private void ClearValidationErrors()
    {
        if (ValidationErrorBanner.Visibility != Visibility.Collapsed)
        {
            ValidationErrorBanner.Visibility = Visibility.Collapsed;
        }

        NameBorder.BorderBrush = NormalBorderBrush;
        PriceBorder.BorderBrush = NormalBorderBrush;
        QuantityBorder.BorderBrush = NormalBorderBrush;
    }

    #endregion

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

    private void PreviewContainer_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        BrowseImage_OnClick(sender, e);
    }

    private void BrowseImage_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Product Image",
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp|All Files (*.*)|*.*"
        };
        if (dlg.ShowDialog(this) == true)
        {
            ImagePathInput.Text = dlg.FileName;
        }
    }

    private void ClearImage_OnClick(object sender, RoutedEventArgs e)
    {
        ImagePathInput.Text = string.Empty;
    }

    private void UpdatePreviewCategory()
    {
        var category = CategoryCombo.SelectedItem?.ToString() ?? "Other";
        ImagePreviewFallbackIcon.Text = ProductItem.GetCategoryIcon(category);
        UpdateImagePreview(ImagePathInput.Text);
    }

    private void UpdateImagePreview(string? path)
    {
        var category = CategoryCombo.SelectedItem?.ToString() ?? "Other";
        var fallbackBrush = GetCategoryBrush(category);

        var trimmed = path?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            ImagePreviewContainer.Background = fallbackBrush;
            ImagePreviewFallbackIcon.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            Uri uri;
            if (trimmed.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
            {
                uri = new Uri(trimmed, UriKind.Absolute);
            }
            else if (Path.IsPathRooted(trimmed))
            {
                if (!File.Exists(trimmed))
                {
                    ImagePreviewContainer.Background = fallbackBrush;
                    ImagePreviewFallbackIcon.Visibility = Visibility.Visible;
                    return;
                }
                uri = new Uri(trimmed, UriKind.Absolute);
            }
            else
            {
                var combined = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, trimmed);
                if (File.Exists(combined))
                {
                    uri = new Uri(combined, UriKind.Absolute);
                }
                else
                {
                    uri = new Uri(trimmed, UriKind.RelativeOrAbsolute);
                }
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.DecodePixelWidth = 144;
            bitmap.UriSource = uri;
            bitmap.EndInit();
            bitmap.Freeze();

            ImagePreviewContainer.Background = new ImageBrush(bitmap)
            {
                Stretch = Stretch.UniformToFill
            };
            ImagePreviewFallbackIcon.Visibility = Visibility.Collapsed;
        }
        catch
        {
            ImagePreviewContainer.Background = fallbackBrush;
            ImagePreviewFallbackIcon.Visibility = Visibility.Visible;
        }
    }

    private static Brush GetCategoryBrush(string category)
    {
        try
        {
            var hex = ProductItem.GetCategoryBackground(category);
            return (Brush)new BrushConverter().ConvertFromString(hex)!;
        }
        catch
        {
            return new SolidColorBrush(Color.FromRgb(241, 245, 249));
        }
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        ClearValidationErrors();

        var name = NameInput.Text.Trim();
        var category = CategoryCombo.SelectedItem?.ToString()?.Trim();
        var unit = UnitCombo.SelectedItem?.ToString()?.Trim();
        var priceText = PriceInput.Text.Trim();
        var qtyText = QuantityInput.Text.Trim();

        // 1. Validate Item Name
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowValidationError("Please enter the product item name.", NameInput, NameBorder);
            return;
        }

        if (name.Length < 2)
        {
            ShowValidationError("Product item name must be at least 2 characters long.", NameInput, NameBorder);
            return;
        }

        if (name.Length > 100)
        {
            ShowValidationError("Product item name cannot exceed 100 characters.", NameInput, NameBorder);
            return;
        }

        if (!Regex.IsMatch(name, @"^[a-zA-Z0-9\s\-&.,()]+$"))
        {
            ShowValidationError("Product item name contains invalid characters. Only letters, numbers, spaces, and standard punctuation (-, &, ., (, ), ,) are allowed. Special characters are prohibited.", NameInput, NameBorder);
            return;
        }

        // 2. Validate Category
        if (string.IsNullOrWhiteSpace(category) || category == "All Categories")
        {
            ShowValidationError("Please select a valid product category from the dropdown.", CategoryCombo);
            return;
        }

        // 3. Validate Price (numbers and dot only, > 0)
        if (string.IsNullOrWhiteSpace(priceText))
        {
            ShowValidationError("Please enter the product price.", PriceInput, PriceBorder);
            return;
        }

        if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) &&
            !decimal.TryParse(priceText, out price))
        {
            ShowValidationError("Please enter a valid product price in numbers (e.g. 125.00). Letters and special characters are not allowed.", PriceInput, PriceBorder);
            return;
        }

        if (price <= 0)
        {
            ShowValidationError("Product price must be greater than ₱0.00.", PriceInput, PriceBorder);
            return;
        }

        if (price > 999999.99m)
        {
            ShowValidationError("Product price cannot exceed ₱999,999.99.", PriceInput, PriceBorder);
            return;
        }

        // 4. Validate Unit (pre-existing dropdown selection, letters only)
        if (string.IsNullOrWhiteSpace(unit))
        {
            ShowValidationError("Please select a measurement unit from the pre-existing options.", UnitCombo);
            return;
        }

        if (!unit.All(char.IsLetter))
        {
            ShowValidationError("Measurement unit can only contain letters (e.g. pc, pack, btl, kg). Numbers and special characters are not allowed.", UnitCombo);
            return;
        }

        // 5. Validate Initial Stock Quantity (whole numbers only)
        if (string.IsNullOrWhiteSpace(qtyText))
        {
            ShowValidationError("Please enter the initial stock quantity.", QuantityInput, QuantityBorder);
            return;
        }

        if (!int.TryParse(qtyText, out var qty))
        {
            ShowValidationError("Please enter a valid whole number for stock quantity. Letters, decimals, and special characters are not allowed.", QuantityInput, QuantityBorder);
            return;
        }

        if (qty < 0)
        {
            ShowValidationError("Initial stock quantity cannot be negative.", QuantityInput, QuantityBorder);
            return;
        }

        if (qty > 100000)
        {
            ShowValidationError("Initial stock quantity cannot exceed 100,000 units.", QuantityInput, QuantityBorder);
            return;
        }

        // 6. Validate Image Path (Optional)
        var imagePath = string.IsNullOrWhiteSpace(ImagePathInput.Text) ? null : ImagePathInput.Text.Trim();
        if (imagePath != null)
        {
            var validExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp" };
            var ext = Path.GetExtension(imagePath).ToLowerInvariant();
            if (!validExtensions.Contains(ext))
            {
                ShowValidationError("Product image must be a valid image file (.png, .jpg, .jpeg, .webp, .bmp).", ImagePathInput);
                return;
            }
        }

        CreatedProduct = new ProductItem
        {
            Name = name,
            Category = category,
            Price = price,
            Unit = unit,
            StockQuantity = qty,
            MinimumStock = ProductItem.DefaultMinimumStock,
            ImagePath = imagePath,
            Tag = qty > ProductItem.DefaultMinimumStock ? "In Stock" : (qty > 0 ? "Low Stock" : "Out of Stock")
        };

        DialogResult = true;
        Close();
    }
}
