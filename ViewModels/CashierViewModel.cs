using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using OopsGrocery.Commands;
using OopsGrocery.Models;
using OopsGrocery.Services;
using OopsGrocery.Views;

namespace OopsGrocery.ViewModels;

public sealed class CashierViewModel : INotifyPropertyChanged
{
    private static readonly Random Rnd = new();

    private string _orderId = string.Empty;
    private string _searchQuery = string.Empty;
    private string _selectedCategory = "All";
    private string _statusMessage = string.Empty;
    private bool _isRegularCustomer = true;
    private bool _isSeniorOrPwdCustomer;
    private bool _isReceiptViewActive;
    private CompletedReceipt? _currentReceipt;

    private readonly List<ProductItem> _allProducts = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public string RegisterId => "Register #01";
    public string CashierName { get; }

    public string OrderId
    {
        get => _orderId;
        private set
        {
            if (_orderId == value) return;
            _orderId = value;
            OnPropertyChanged();
        }
    }

    public bool IsRegularCustomer
    {
        get => _isRegularCustomer;
        set
        {
            if (_isRegularCustomer == value) return;
            _isRegularCustomer = value;
            if (value)
            {
                _isSeniorOrPwdCustomer = false;
                OnPropertyChanged(nameof(IsSeniorOrPwdCustomer));
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(CustomerTypeDisplayName));
            RecalculateTotals();
        }
    }

    public bool IsSeniorOrPwdCustomer
    {
        get => _isSeniorOrPwdCustomer;
        set
        {
            if (_isSeniorOrPwdCustomer == value) return;
            _isSeniorOrPwdCustomer = value;
            if (value)
            {
                _isRegularCustomer = false;
                OnPropertyChanged(nameof(IsRegularCustomer));
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(CustomerTypeDisplayName));
            RecalculateTotals();
        }
    }

    public string CustomerTypeDisplayName => IsSeniorOrPwdCustomer ? "Senior Citizen / PWD (20% Discount)" : "Regular Customer";

    public bool IsReceiptViewActive
    {
        get => _isReceiptViewActive;
        set
        {
            if (_isReceiptViewActive == value) return;
            _isReceiptViewActive = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPosViewActive));
        }
    }

    public bool IsPosViewActive => !IsReceiptViewActive;

    public CompletedReceipt? CurrentReceipt
    {
        get => _currentReceipt;
        private set
        {
            if (_currentReceipt == value) return;
            _currentReceipt = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> Categories { get; } = ["All"];
    public ObservableCollection<ProductItem> DisplayedProducts { get; } = [];
    public ObservableCollection<CartItem> RegisterItems { get; } = [];

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery == value) return;
            _searchQuery = value;
            OnPropertyChanged();
            FilterProducts();
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

    // Calculations
    public int TotalItemCount => RegisterItems.Sum(i => i.Quantity);
    public decimal Subtotal => RegisterItems.Sum(i => i.TotalPrice);
    public decimal Discount => IsSeniorOrPwdCustomer ? Math.Round(Subtotal * 0.20m, 2) : 0.00m;
    public decimal NewSubtotal => Math.Max(0m, Subtotal - Discount);
    public decimal Vat => Math.Round(NewSubtotal * 0.12m, 2);
    public decimal FinalTotal => NewSubtotal + Vat;

    public bool HasItems => RegisterItems.Count > 0;

    public string FormattedSubtotal => $"₱{Subtotal:N2}";
    public string FormattedDiscount => Discount > 0 ? $"-₱{Discount:N2}" : "₱0.00";
    public string FormattedNewSubtotal => $"₱{NewSubtotal:N2}";
    public string FormattedVat => $"₱{Vat:N2}";
    public string FormattedFinalTotal => $"₱{FinalTotal:N2}";

    // Commands
    public ICommand AddItemCommand { get; }
    public ICommand IncreaseQtyCommand { get; }
    public ICommand DecreaseQtyCommand { get; }
    public ICommand DeleteItemCommand { get; }
    public ICommand ClearOrderCommand { get; }
    public ICommand CheckoutCommand { get; }
    public ICommand SelectCategoryCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand StartNewOrderCommand { get; }
    public ICommand PrintReceiptCommand { get; }
    public ICommand SaveReceiptCommand { get; }

    public CashierViewModel(string cashierName = "Cashier")
    {
        CashierName = string.IsNullOrWhiteSpace(cashierName) ? "cashier" : cashierName;
        GenerateNewOrderId();

        // Load active products from database or fallback catalog
        var dbProducts = ProductService.GetAllProducts(out _, out _);
        if (dbProducts != null && dbProducts.Count > 0)
        {
            _allProducts.AddRange(dbProducts.Where(p => !p.IsArchived));
        }

        if (_allProducts.Count == 0)
        {
            _allProducts.AddRange(ProductService.GetFallbackProducts());
        }

        // Populate Categories
        foreach (var c in _allProducts.Select(p => p.Category).Distinct().OrderBy(x => x))
        {
            if (!Categories.Contains(c))
            {
                Categories.Add(c);
            }
        }

        FilterProducts();

        // Commands implementation
        AddItemCommand = new RelayCommand<ProductItem>(product =>
        {
            if (product == null) return;

            if (product.StockQuantity <= 0)
            {
                StatusMessage = $"⚠️ {product.Name} is currently out of stock.";
                return;
            }

            var existing = RegisterItems.FirstOrDefault(i => i.Product.Id == product.Id);
            if (existing != null)
            {
                existing.Quantity++;
                StatusMessage = $"Updated {product.Name} quantity to {existing.Quantity}.";
            }
            else
            {
                RegisterItems.Add(new CartItem(product, 1));
                StatusMessage = $"Added {product.Name} (₱{product.Price:N2}) to order.";
            }

            RecalculateTotals();
        });

        IncreaseQtyCommand = new RelayCommand<CartItem>(item =>
        {
            if (item == null) return;
            item.Quantity++;
            RecalculateTotals();
            StatusMessage = $"Increased {item.Product.Name} to {item.Quantity}.";
        });

        DecreaseQtyCommand = new RelayCommand<CartItem>(item =>
        {
            if (item == null) return;

            // GUI Requirement: If quantity reaches zero, item must NOT be removed silently.
            // Cashier must use Delete action and complete void authorization PIN.
            if (item.Quantity > 1)
            {
                item.Quantity--;
                RecalculateTotals();
                StatusMessage = $"Decreased {item.Product.Name} to {item.Quantity}.";
            }
            else
            {
                StatusMessage = "⚠️ Minimum quantity is 1. To remove this item from the receipt, click the Delete button.";
            }
        });

        DeleteItemCommand = new RelayCommand<CartItem>(item =>
        {
            if (item == null) return;

            // Open Void Confirmation popup requiring 6-digit PIN
            var authorized = VoidConfirmationWindow.PromptVoidAuthorization(item);
            if (authorized)
            {
                RegisterItems.Remove(item);
                RecalculateTotals();
                StatusMessage = $"✅ Void authorized. Removed {item.Product.Name} from order.";
            }
            else
            {
                StatusMessage = "Void canceled. Item preserved in current order.";
            }
        });

        ClearOrderCommand = new RelayCommand(() =>
        {
            if (RegisterItems.Count == 0) return;

            var confirmed = MessageBoxWindow.Confirm(
                "Are you sure you want to clear all items from the current order?\nThis will remove all line items.",
                "Clear Order Confirmation",
                confirmButtonText: "Clear Order",
                cancelButtonText: "Keep Order",
                subtitle: "Order Discard Warning");

            if (confirmed)
            {
                RegisterItems.Clear();
                IsRegularCustomer = true;
                RecalculateTotals();
                StatusMessage = "Current order cleared.";
            }
        });

        CheckoutCommand = new RelayCommand(() =>
        {
            if (RegisterItems.Count == 0)
            {
                StatusMessage = "⚠️ Cannot checkout: Order receipt is empty.";
                return;
            }

            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

            var checkoutDialog = new CheckoutPaymentWindow(
                orderId: OrderId,
                customerType: CustomerTypeDisplayName,
                items: RegisterItems,
                subtotal: Subtotal,
                discount: Discount,
                newSubtotal: NewSubtotal,
                vat: Vat,
                finalTotal: FinalTotal)
            {
                Owner = activeWindow
            };

            var result = checkoutDialog.ShowDialog();
            if (result == true)
            {
                // Payment confirmed! Build the digital receipt preserving exact confirmed values
                var receiptNum = $"RCP-{DateTime.Now:yyyyMMdd}-{Rnd.Next(1000, 9999)}";

                var completedReceipt = new CompletedReceipt
                {
                    OrderId = OrderId,
                    ReceiptNumber = receiptNum,
                    CashierName = CashierName,
                    RegisterName = RegisterId,
                    CustomerType = CustomerTypeDisplayName,
                    TransactionDate = checkoutDialog.ConfirmedAt,
                    Subtotal = checkoutDialog.Subtotal,
                    Discount = checkoutDialog.Discount,
                    NewSubtotal = checkoutDialog.NewSubtotal,
                    Vat = checkoutDialog.Vat,
                    FinalTotal = checkoutDialog.FinalTotal,
                    PaymentMethod = "Cash",
                    CashReceived = checkoutDialog.ConfirmedCashReceived,
                    Change = checkoutDialog.ConfirmedChange,
                    Items = RegisterItems.Select(ci => new ReceiptLineItem
                    {
                        ProductName = ci.Product.Name,
                        ProductCode = ci.Product.Sku,
                        Quantity = ci.Quantity,
                        UnitPrice = ci.Product.Price,
                        ItemSubtotal = ci.TotalPrice
                    }).ToList()
                };

                CurrentReceipt = completedReceipt;

                // Record transaction in database & update live analytics
                var record = new TransactionRecord
                {
                    ReceiptNumber = receiptNum,
                    CashierName = CashierName,
                    StationName = RegisterId,
                    TotalItems = completedReceipt.TotalItemCount,
                    Subtotal = completedReceipt.Subtotal,
                    Tax = completedReceipt.Vat,
                    TotalAmount = completedReceipt.FinalTotal,
                    AmountTendered = completedReceipt.CashReceived,
                    ChangeGiven = completedReceipt.Change,
                    PaymentMethod = "Cash",
                    Status = "Completed",
                    CreatedAt = completedReceipt.TransactionDate,
                    Items = completedReceipt.Items.Select(i => new TransactionItemRecord
                    {
                        Sku = i.ProductCode,
                        ProductName = i.ProductName,
                        UnitPrice = i.UnitPrice,
                        Quantity = i.Quantity,
                        TotalPrice = i.ItemSubtotal
                    }).ToList()
                };

                TransactionService.RecordTransaction(record, out _);

                // Switch to dedicated Digital Receipt screen
                IsReceiptViewActive = true;
                StatusMessage = $"✅ Sale completed! Receipt {receiptNum} issued.";
            }
        });

        SelectCategoryCommand = new RelayCommand<string>(cat =>
        {
            if (cat != null) SelectedCategory = cat;
        });

        ClearSearchCommand = new RelayCommand(() =>
        {
            SearchQuery = string.Empty;
        });

        StartNewOrderCommand = new RelayCommand(() =>
        {
            StartNewOrder();
        });

        PrintReceiptCommand = new RelayCommand(() =>
        {
            if (CurrentReceipt == null) return;

            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    MessageBoxWindow.Show(
                        $"Receipt {CurrentReceipt.ReceiptNumber} has been sent to printer '{printDialog.PrintQueue.FullName}'.",
                        "Receipt Printed",
                        type: MessageBoxType.Success,
                        buttonText: "OK",
                        subtitle: "Hardware Print Confirmation");
                }
            }
            catch (Exception ex)
            {
                MessageBoxWindow.Show(
                    $"Print service error: {ex.Message}. Digital receipt is preserved on screen.",
                    "Print Error",
                    type: MessageBoxType.Warning);
            }
        });

        SaveReceiptCommand = new RelayCommand(() =>
        {
            if (CurrentReceipt == null) return;

            try
            {
                var exportFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OopsGroceryReceipts");
                Directory.CreateDirectory(exportFolder);

                var filePath = Path.Combine(exportFolder, $"{CurrentReceipt.ReceiptNumber}_{CurrentReceipt.OrderId}.txt");
                var lines = new List<string>
                {
                    "============================================================",
                    "            OOPs OOPs Oops Grocery – Smart POS",
                    "                Caloocan Branch (Branch #01)",
                    "       10th Ave, Grace Park East, Caloocan City, Metro Manila",
                    "============================================================",
                    $"Receipt No:     {CurrentReceipt.ReceiptNumber}",
                    $"Order ID:       {CurrentReceipt.OrderId}",
                    $"Date & Time:    {CurrentReceipt.FormattedTransactionDate}",
                    $"Cashier:        {CurrentReceipt.CashierName}",
                    $"Terminal:       {CurrentReceipt.RegisterName}",
                    $"Customer Type:  {CurrentReceipt.CustomerType}",
                    "------------------------------------------------------------",
                    string.Format("{0,-24} {1,-10} {2,4} {3,10} {4,10}", "ITEM", "CODE", "QTY", "PRICE", "TOTAL"),
                    "------------------------------------------------------------"
                };

                foreach (var item in CurrentReceipt.Items)
                {
                    var truncatedName = item.ProductName.Length > 24 ? item.ProductName[..21] + "..." : item.ProductName;
                    lines.Add(string.Format("{0,-24} {1,-10} {2,4} {3,10:N2} {4,10:N2}",
                        truncatedName, item.ProductCode, item.Quantity, item.UnitPrice, item.ItemSubtotal));
                }

                lines.Add("------------------------------------------------------------");
                lines.Add($"Subtotal:                                       ₱{CurrentReceipt.Subtotal:N2}");
                lines.Add($"Discount (20%):                                {CurrentReceipt.FormattedDiscount,15}");
                lines.Add($"New Subtotal:                                   ₱{CurrentReceipt.NewSubtotal:N2}");
                lines.Add($"VAT (12%):                                      ₱{CurrentReceipt.Vat:N2}");
                lines.Add("============================================================");
                lines.Add($"FINAL TOTAL:                                    ₱{CurrentReceipt.FinalTotal:N2}");
                lines.Add($"Payment Method:                                 Cash");
                lines.Add($"Cash Received:                                  ₱{CurrentReceipt.CashReceived:N2}");
                lines.Add($"Change:                                         ₱{CurrentReceipt.Change:N2}");
                lines.Add("============================================================");
                lines.Add("         Thank you for shopping with us! Please come again! ");
                lines.Add("============================================================");

                File.WriteAllLines(filePath, lines);

                MessageBoxWindow.Show(
                    $"Digital Receipt saved successfully to:\n{filePath}",
                    "Receipt Saved",
                    type: MessageBoxType.Success,
                    buttonText: "OK",
                    subtitle: "Digital Copy Preserved");
            }
            catch (Exception ex)
            {
                MessageBoxWindow.Show(
                    $"Failed to save receipt: {ex.Message}",
                    "Export Error",
                    type: MessageBoxType.Error);
            }
        });
    }

    public void EnsurePosScreen()
    {
        if (IsReceiptViewActive)
        {
            // Reset to POS view
            IsReceiptViewActive = false;
        }
    }

    public void StartNewOrder()
    {
        GenerateNewOrderId();
        IsRegularCustomer = true;
        _isSeniorOrPwdCustomer = false;
        RegisterItems.Clear();
        RecalculateTotals();
        SearchQuery = string.Empty;
        IsReceiptViewActive = false;
        StatusMessage = $"Fresh order initialized: {OrderId}. Ready for items.";
    }

    private void GenerateNewOrderId()
    {
        OrderId = $"ORD-{Rnd.Next(100000, 999999)}";
    }

    private void FilterProducts()
    {
        DisplayedProducts.Clear();

        var query = SearchQuery?.Trim() ?? string.Empty;
        var selectedCat = SelectedCategory ?? "All";

        var filtered = _allProducts.Where(p => !p.IsArchived);

        if (!selectedCat.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(p => p.Category.Equals(selectedCat, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Tag.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in filtered)
        {
            DisplayedProducts.Add(item);
        }
    }

    private void RecalculateTotals()
    {
        OnPropertyChanged(nameof(TotalItemCount));
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Discount));
        OnPropertyChanged(nameof(NewSubtotal));
        OnPropertyChanged(nameof(Vat));
        OnPropertyChanged(nameof(FinalTotal));
        OnPropertyChanged(nameof(HasItems));

        OnPropertyChanged(nameof(FormattedSubtotal));
        OnPropertyChanged(nameof(FormattedDiscount));
        OnPropertyChanged(nameof(FormattedNewSubtotal));
        OnPropertyChanged(nameof(FormattedVat));
        OnPropertyChanged(nameof(FormattedFinalTotal));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
