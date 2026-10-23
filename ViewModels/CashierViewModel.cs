using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using OopsGrocery.Commands;
using OopsGrocery.Models;

namespace OopsGrocery.ViewModels;

public sealed class CashierViewModel : INotifyPropertyChanged
{
    private string _barcodeOrSkuInput = string.Empty;
    private string _selectedCategory = "All";
    private string _statusMessage = string.Empty;
    private string _paymentMethod = "Cash";
    private decimal _amountTendered;
    private string _amountTenderedText = string.Empty;
    private int _transactionNumber = 8841;
    private readonly List<ProductItem> _allProducts;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string RegisterId => "Register #01";
    public string CashierName { get; }
    public string CurrentTransactionCode => $"#TXN-{_transactionNumber}";

    public ObservableCollection<string> Categories { get; }
    public ObservableCollection<ProductItem> DisplayedProducts { get; }
    public ObservableCollection<CartItem> RegisterItems { get; }

    public string BarcodeOrSkuInput
    {
        get => _barcodeOrSkuInput;
        set
        {
            if (_barcodeOrSkuInput == value) return;
            _barcodeOrSkuInput = value;
            OnPropertyChanged();
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory == value) return;
            _selectedCategory = value;
            OnPropertyChanged();
            FilterProducts();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string PaymentMethod
    {
        get => _paymentMethod;
        set
        {
            if (_paymentMethod == value) return;
            _paymentMethod = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCashPayment));
            OnPropertyChanged(nameof(IsCardPayment));
        }
    }

    public bool IsCashPayment => PaymentMethod == "Cash";
    public bool IsCardPayment => PaymentMethod == "Card";

    public string AmountTenderedText
    {
        get => _amountTenderedText;
        set
        {
            if (_amountTenderedText == value) return;
            _amountTenderedText = value;
            OnPropertyChanged();

            if (decimal.TryParse(value, out var amount))
            {
                _amountTendered = amount;
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                _amountTendered = 0m;
            }
            OnPropertyChanged(nameof(AmountTendered));
            OnPropertyChanged(nameof(ChangeDue));
            OnPropertyChanged(nameof(FormattedChangeDue));
        }
    }

    public decimal AmountTendered => _amountTendered;

    public int TotalItemCount => RegisterItems.Sum(i => i.Quantity);
    public decimal Subtotal => RegisterItems.Sum(i => i.TotalPrice);
    public decimal Tax => Math.Round(Subtotal * 0.08m, 2); // 8% sales tax
    public decimal Total => Subtotal + Tax;

    public decimal ChangeDue => (IsCashPayment && AmountTendered >= Total) ? (AmountTendered - Total) : 0.00m;

    public string FormattedSubtotal => $"${Subtotal:F2}";
    public string FormattedTax => $"${Tax:F2}";
    public string FormattedTotal => $"${Total:F2}";
    public string FormattedChangeDue => $"${ChangeDue:F2}";

    public ICommand ScanBarcodeCommand { get; }
    public ICommand QuickAddItemCommand { get; }
    public ICommand IncreaseQtyCommand { get; }
    public ICommand DecreaseQtyCommand { get; }
    public ICommand VoidLineCommand { get; }
    public ICommand VoidSaleCommand { get; }
    public ICommand SelectCategoryCommand { get; }
    public ICommand SetPaymentMethodCommand { get; }
    public ICommand QuickTenderCommand { get; }
    public ICommand CompleteSaleCommand { get; }

    public CashierViewModel(string cashierName = "Cashier")
    {
        CashierName = cashierName;

        Categories = new ObservableCollection<string>
        {
            "All",
            "Fresh Produce",
            "Dairy & Eggs",
            "Bakery",
            "Meat & Seafood",
            "Beverages",
            "Pantry"
        };

        _allProducts = new List<ProductItem>
        {
            new() { Id = 1, Sku = "FP-101", Name = "Organic Honeycrisp Apples", Category = "Fresh Produce", Price = 2.99m, Unit = "lb", StockQuantity = 140, Tag = "PLU 4173" },
            new() { Id = 2, Sku = "FP-102", Name = "Hass Avocados (Bag of 4)", Category = "Fresh Produce", Price = 4.99m, Unit = "bag", StockQuantity = 60, Tag = "PLU 4046" },
            new() { Id = 3, Sku = "FP-103", Name = "Cavendish Bananas", Category = "Fresh Produce", Price = 0.69m, Unit = "lb", StockQuantity = 220, Tag = "PLU 4011" },
            new() { Id = 4, Sku = "DE-201", Name = "Farm Fresh Whole Milk (1 Gal)", Category = "Dairy & Eggs", Price = 4.29m, Unit = "gal", StockQuantity = 38, Tag = "Barcode: 071111" },
            new() { Id = 5, Sku = "DE-202", Name = "Free-Range Large Eggs (1 Doz)", Category = "Dairy & Eggs", Price = 4.89m, Unit = "doz", StockQuantity = 52, Tag = "Barcode: 072222" },
            new() { Id = 6, Sku = "BK-301", Name = "Artisan Sourdough Boule", Category = "Bakery", Price = 5.50m, Unit = "loaf", StockQuantity = 18, Tag = "Baked Daily" },
            new() { Id = 7, Sku = "BK-302", Name = "All-Butter Croissants (4 Pack)", Category = "Bakery", Price = 6.25m, Unit = "pack", StockQuantity = 20, Tag = "Fresh" },
            new() { Id = 8, Sku = "MS-401", Name = "Wild Atlantic Salmon Fillet", Category = "Meat & Seafood", Price = 14.99m, Unit = "lb", StockQuantity = 25, Tag = "Meat Counter" },
            new() { Id = 9, Sku = "BV-501", Name = "Cold-Pressed Orange Juice (1L)", Category = "Beverages", Price = 5.25m, Unit = "btl", StockQuantity = 35, Tag = "Barcode: 073333" },
            new() { Id = 10, Sku = "PT-601", Name = "Extra Virgin Olive Oil (500ml)", Category = "Pantry", Price = 16.75m, Unit = "btl", StockQuantity = 15, Tag = "Barcode: 074444" }
        };

        DisplayedProducts = new ObservableCollection<ProductItem>(_allProducts);
        RegisterItems = new ObservableCollection<CartItem>();

        // Prepopulate with a ringing sample
        RingUpItem(_allProducts[0], 2);
        RingUpItem(_allProducts[3], 1);
        AmountTenderedText = "20.00";

        ScanBarcodeCommand = new RelayCommand(ScanBarcode);

        QuickAddItemCommand = new RelayCommand<ProductItem>(product =>
        {
            if (product != null)
            {
                RingUpItem(product, 1);
                StatusMessage = $"✓ Rung up {product.Name} (${product.Price:F2}).";
            }
        });

        IncreaseQtyCommand = new RelayCommand<CartItem>(item =>
        {
            if (item != null)
            {
                item.Quantity++;
                RecalculateTotals();
            }
        });

        DecreaseQtyCommand = new RelayCommand<CartItem>(item =>
        {
            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                }
                else
                {
                    RegisterItems.Remove(item);
                }
                RecalculateTotals();
            }
        });

        VoidLineCommand = new RelayCommand<CartItem>(item =>
        {
            if (item != null)
            {
                RegisterItems.Remove(item);
                RecalculateTotals();
                StatusMessage = $"Voided line item: {item.Product.Name}.";
            }
        });

        VoidSaleCommand = new RelayCommand(() =>
        {
            RegisterItems.Clear();
            AmountTenderedText = string.Empty;
            RecalculateTotals();
            StatusMessage = "Transaction voided. Register cleared for next customer.";
        });

        SelectCategoryCommand = new RelayCommand<string>(cat =>
        {
            if (cat != null) SelectedCategory = cat;
        });

        SetPaymentMethodCommand = new RelayCommand<string>(method =>
        {
            if (method != null) PaymentMethod = method;
        });

        QuickTenderCommand = new RelayCommand<string>(preset =>
        {
            if (preset == "Exact")
            {
                AmountTenderedText = Total.ToString("F2");
            }
            else if (decimal.TryParse(preset, out var amount))
            {
                AmountTenderedText = amount.ToString("F2");
            }
        });

        CompleteSaleCommand = new RelayCommand(CompleteSale);
    }

    private void ScanBarcode()
    {
        var query = BarcodeOrSkuInput.Trim();
        if (string.IsNullOrWhiteSpace(query)) return;

        var matched = _allProducts.FirstOrDefault(p =>
            p.Sku.Equals(query, StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Tag.Contains(query, StringComparison.OrdinalIgnoreCase));

        if (matched != null)
        {
            RingUpItem(matched, 1);
            StatusMessage = $"✓ Scanned {matched.Name} ({matched.FormattedPrice}).";
            BarcodeOrSkuInput = string.Empty;
        }
        else
        {
            StatusMessage = $"⚠️ Item '{query}' not found. Check SKU or PLU code.";
        }
    }

    private void RingUpItem(ProductItem product, int quantity)
    {
        var existing = RegisterItems.FirstOrDefault(i => i.Product.Id == product.Id);
        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            RegisterItems.Add(new CartItem(product, quantity));
        }
        RecalculateTotals();
    }

    private void CompleteSale()
    {
        if (RegisterItems.Count == 0)
        {
            StatusMessage = "⚠️ Register is empty. Scan an item to ring up.";
            return;
        }

        if (IsCashPayment && AmountTendered < Total)
        {
            StatusMessage = $"⚠️ Insufficient cash tendered. Total is {FormattedTotal}, received {AmountTendered:C}.";
            return;
        }

        var change = FormattedChangeDue;
        var txn = CurrentTransactionCode;
        var paidMethod = PaymentMethod;

        RegisterItems.Clear();
        AmountTenderedText = string.Empty;
        _transactionNumber++;
        RecalculateTotals();
        OnPropertyChanged(nameof(CurrentTransactionCode));

        StatusMessage = IsCashPayment
            ? $"✅ Sale Complete ({txn}) via Cash. Change Given: {change}. Receipt printed."
            : $"✅ Sale Complete ({txn}) via Card Terminal. Receipt printed.";
    }

    private void FilterProducts()
    {
        DisplayedProducts.Clear();
        var items = SelectedCategory == "All"
            ? _allProducts
            : _allProducts.Where(p => p.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));

        foreach (var item in items)
        {
            DisplayedProducts.Add(item);
        }
    }

    private void RecalculateTotals()
    {
        OnPropertyChanged(nameof(TotalItemCount));
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Tax));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(ChangeDue));
        OnPropertyChanged(nameof(FormattedSubtotal));
        OnPropertyChanged(nameof(FormattedTax));
        OnPropertyChanged(nameof(FormattedTotal));
        OnPropertyChanged(nameof(FormattedChangeDue));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
