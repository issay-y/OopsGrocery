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

public partial class EditProductWindow : Window
{
    private readonly ProductItem _item;

    public EditProductWindow(ProductItem item, List<string> categories)
    {
        _item = item ?? throw new ArgumentNullException(nameof(item));
        InitializeComponent();

        // Read-only fields
        ItemIdDisplay.Text = $"{_item.FormattedId} ({_item.Sku})";
        ItemNameDisplay.Text = _item.Name;
        CategoryDisplay.Text = _item.Category;
        UnitDisplay.Text = _item.Unit;

        // Editable fields
        PriceInput.Text = _item.Price.ToString("F2");
        ImagePathInput.Text = _item.ImagePath ?? string.Empty;

        // Live image preview setup
        ImagePreviewFallbackIcon.Text = _item.CategoryIcon;
        ImagePreviewContainer.Background = GetFallbackBrush();

        UpdateImagePreview(ImagePathInput.Text);
        ImagePathInput.TextChanged += (_, _) => UpdateImagePreview(ImagePathInput.Text);

        DataObject.AddPastingHandler(PriceInput, OnPricePaste);

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
            PriceInput.Focus();
            PriceInput.SelectAll();
        };
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

    private void UpdateImagePreview(string? path)
    {
        var fallbackBrush = GetFallbackBrush();

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

    private Brush GetFallbackBrush()
    {
        try
        {
            var hex = ProductItem.GetCategoryBackground(_item.Category);
            return (Brush)new BrushConverter().ConvertFromString(hex)!;
        }
        catch
        {
            return new SolidColorBrush(Color.FromRgb(241, 245, 249));
        }
    }

    private void PriceInput_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox tb) return;

        if (!e.Text.All(c => char.IsDigit(c) || c == '.'))
        {
            e.Handled = true;
            return;
        }

        int selStart = tb.SelectionStart;
        int selLen = tb.SelectionLength;
        string current = tb.Text;
        string prospective = current.Remove(selStart, selLen).Insert(selStart, e.Text);

        if (!Regex.IsMatch(prospective, @"^\d*(\.\d{0,2})?$"))
        {
            e.Handled = true;
        }
    }

    private void NumericInput_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            e.Handled = true;
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

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var priceText = PriceInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(priceText))
        {
            MessageBoxWindow.Show("Please enter the product price.", "Validation Error", this, MessageBoxType.Warning);
            PriceInput.Focus();
            return;
        }

        if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) &&
            !decimal.TryParse(priceText, out price))
        {
            MessageBoxWindow.Show("Please enter a valid product price in numbers (e.g. 125.00). Letters and special characters are not allowed.", "Validation Error", this, MessageBoxType.Warning);
            PriceInput.Focus();
            PriceInput.SelectAll();
            return;
        }

        if (price <= 0)
        {
            MessageBoxWindow.Show("Product price must be greater than ₱0.00.", "Validation Error", this, MessageBoxType.Warning);
            PriceInput.Focus();
            PriceInput.SelectAll();
            return;
        }

        if (price > 999999.99m)
        {
            MessageBoxWindow.Show("Product price cannot exceed ₱999,999.99.", "Validation Error", this, MessageBoxType.Warning);
            PriceInput.Focus();
            PriceInput.SelectAll();
            return;
        }

        var imagePath = string.IsNullOrWhiteSpace(ImagePathInput.Text) ? null : ImagePathInput.Text.Trim();
        if (imagePath != null)
        {
            var validExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp" };
            var ext = Path.GetExtension(imagePath).ToLowerInvariant();
            if (!validExtensions.Contains(ext))
            {
                MessageBoxWindow.Show("Product image must be a valid image file (.png, .jpg, .jpeg, .webp, .bmp).", "Validation Error", this, MessageBoxType.Warning);
                ImagePathInput.Focus();
                return;
            }
        }

        // Only price and image are editable. Name, category, unit and stock are left untouched.
        _item.Price = price;
        _item.ImagePath = imagePath;

        DialogResult = true;
        Close();
    }
}