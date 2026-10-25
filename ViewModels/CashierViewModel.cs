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
    public decimal Tax => Math.Round(Subtotal * 0.12m, 2); // 12% VAT
    public decimal Total => Subtotal + Tax;

    public decimal ChangeDue => (IsCashPayment && AmountTendered >= Total) ? (AmountTendered - Total) : 0.00m;

    public string FormattedSubtotal => $"₱{Subtotal:N2}";
    public string FormattedTax => $"₱{Tax:N2}";
    public string FormattedTotal => $"₱{Total:N2}";
    public string FormattedChangeDue => $"₱{ChangeDue:N2}";

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

        var dbProducts = Services.ProductService.GetAllProducts(out _, out _);
        if (dbProducts != null && dbProducts.Count > 0)
        {
            _allProducts = dbProducts.Where(p => !p.IsArchived).ToList();
        }
        else
        {
            _allProducts = new List<ProductItem>();
        }

        Categories = new ObservableCollection<string> { "All" };
        foreach (var c in _allProducts.Select(p => p.Category).Distinct().OrderBy(x => x))
        {
            Categories.Add(c);
        }

        DisplayedProducts = new ObservableCollection<ProductItem>(_allProducts);
        RegisterItems = new ObservableCollection<CartItem>();

        if (_allProducts.Count > 0)
        {
            RingUpItem(_allProducts[0], 2);
        }
        if (_allProducts.Count > 1)
        {
            RingUpItem(_allProducts[1], 1);
        }
        AmountTenderedText = "1000.00";

        ScanBarcodeCommand = new RelayCommand(ScanBarcode);

        QuickAddItemCommand = new RelayCommand<ProductItem>(product =>
        {
            if (product != null)
            {
                RingUpItem(product, 1);
                StatusMessage = $"✓ Rung up {product.Name} (₱{product.Price:N2}).";
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

        // Record real-time transaction in database
        var record = new TransactionRecord
        {
            ReceiptNumber = $"RCP-{DateTime.Now:yyyyMMdd}-{_transactionNumber:D4}",
            CashierName = string.IsNullOrWhiteSpace(CashierName) ? "cashier" : CashierName,
            StationName = RegisterId,
            TotalItems = TotalItemCount,
            Subtotal = Subtotal,
            Tax = Tax,
            TotalAmount = Total,
            AmountTendered = AmountTendered,
            ChangeGiven = ChangeDue,
            PaymentMethod = "Cash",
            Status = "Completed",
            CreatedAt = DateTime.Now,
            Items = RegisterItems.Select(ci => new TransactionItemRecord
            {
                ProductId = ci.Product.Id,
                Sku = ci.Product.Sku,
                ProductName = ci.Product.Name,
                Category = ci.Product.Category,
                UnitPrice = ci.Product.Price,
                Quantity = ci.Quantity,
                TotalPrice = ci.TotalPrice
            }).ToList()
        };

        Services.TransactionService.RecordTransaction(record, out _);

        RegisterItems.Clear();
        AmountTenderedText = string.Empty;
        _transactionNumber++;
        RecalculateTotals();
        OnPropertyChanged(nameof(CurrentTransactionCode));

        StatusMessage = $"✅ Sale Complete ({record.ReceiptNumber}) via Cash. Total: {record.TotalAmount:C}, Change Given: {change}. Transaction recorded live!";
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
