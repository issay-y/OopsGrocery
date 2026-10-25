using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using OopsGrocery.Commands;
using OopsGrocery.Services;
using OopsGrocery.Views;

namespace OopsGrocery.ViewModels;

public sealed class SalesTrendBar
{
    public string Label { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string FormattedAmount => $"₱{Amount:N0}";
    public int TransactionsCount { get; init; }
    public double BarHeight { get; init; }
    public bool IsPeak { get; init; }
    public string BarColorHex => IsPeak ? "#FFC926" : (Amount > 0 ? "#1B4D2B" : "#CBD5E1");
    public Brush BarBrush => (Brush)new BrushConverter().ConvertFromString(BarColorHex)!;
    public string TooltipText => $"{Label}: ₱{Amount:N2} ({TransactionsCount} orders)";
}

public sealed class CategorySalesMetric
{
    public string CategoryName { get; init; } = string.Empty;
    public string Icon { get; init; } = "📦";
    public decimal Revenue { get; init; }
    public string FormattedRevenue => $"₱{Revenue:N2}";
    public double Percentage { get; init; }
    public string FormattedPercentage => $"{Percentage:F1}%";
    public string BarColorHex { get; init; } = "#1B4D2B";
    public Brush BarBrush => (Brush)new BrushConverter().ConvertFromString(BarColorHex)!;
    public GridLength ShareLength => new GridLength(Math.Max(0.001, Math.Min(100.0, Percentage)), GridUnitType.Star);
    public GridLength RemainingLength => new GridLength(Math.Max(0.001, 100.0 - Math.Min(100.0, Percentage)), GridUnitType.Star);
}

public sealed class CashierAuditMetric
{
    public string CashierName { get; init; } = string.Empty;
    public string Initials { get; init; } = "CS";
    public string RoleBadge { get; init; } = "CASHIER";
    public string StationName { get; init; } = "Register #01";
    public decimal TotalSales { get; init; }
    public string FormattedTotalSales => $"₱{TotalSales:N2}";
    public int ReceiptsProcessed { get; init; }
    public int ItemsSold { get; init; }
    public string SpeedRating { get; init; } = "99.2% Accuracy";
    public string AvatarBgHex { get; init; } = "#1B4D2B";
    public Brush AvatarBgBrush => (Brush)new BrushConverter().ConvertFromString(AvatarBgHex)!;
}

public sealed class BestSellerMetric
{
    public int Rank { get; init; }
    public string RankBadge => Rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"#{Rank}" };
    public string ItemName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string CategoryIcon { get; init; } = "📦";
    public int UnitsSold { get; init; }
    public decimal Revenue { get; init; }
    public string FormattedRevenue => $"₱{Revenue:N2}";
}

public sealed class TransactionsReportsViewModel : INotifyPropertyChanged
{
    private string _selectedPeriod = "Today";
    private string _notificationMessage = "Live database sync active. Reporting real-time transactions.";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> Periods { get; } = ["Today", "This Week", "This Month"];
    public ObservableCollection<SalesTrendBar> SalesTrendBars { get; } = [];
    public ObservableCollection<CategorySalesMetric> CategorySales { get; } = [];
    public ObservableCollection<CashierAuditMetric> CashierAudits { get; } = [];
    public ObservableCollection<BestSellerMetric> BestSellers { get; } = [];

    public bool HasCategorySales => CategorySales.Count > 0;
    public bool HasNoCategorySales => CategorySales.Count == 0;
    public bool HasCashierAudits => CashierAudits.Count > 0;
    public bool HasNoCashierAudits => CashierAudits.Count == 0;
    public bool HasBestSellers => BestSellers.Count > 0;
    public bool HasNoBestSellers => BestSellers.Count == 0;

    public string SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (_selectedPeriod == value) return;
            _selectedPeriod = value;
            OnPropertyChanged();
            LoadAnalyticsData();
        }
    }

    public string NotificationMessage
    {
        get => _notificationMessage;
        set
        {
            if (_notificationMessage == value) return;
            _notificationMessage = value;
            OnPropertyChanged();
        }
    }

    // Top Summary Statistical Cards
    public string TotalGrossSales { get; private set; } = "₱0.00";
    public string GrossSalesSubtitle { get; private set; } = string.Empty;
    public string TotalTransactionsCount { get; private set; } = "0 Orders";
    public string TransactionsSubtitle { get; private set; } = string.Empty;
    public string AverageBasketSize { get; private set; } = "₱0.00";
    public string BasketSubtitle { get; private set; } = string.Empty;
    public string TotalTaxCollected { get; private set; } = "₱0.00";
    public string TaxSubtitle { get; private set; } = string.Empty;

    public string PeriodDateRangeSubtitle { get; private set; } = string.Empty;
    public string PeakHourText { get; private set; } = string.Empty;
    public string LastRefreshedText { get; private set; } = $"Synced: {DateTime.Now:h:mm:ss tt}";

    public ICommand SelectPeriodCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand RefreshCommand { get; }

    public TransactionsReportsViewModel()
    {
        SelectPeriodCommand = new RelayCommand<string>(p =>
        {
            if (!string.IsNullOrWhiteSpace(p))
            {
                SelectedPeriod = p;
            }
        });

        ExportReportCommand = new RelayCommand(OnExportReport);
        RefreshCommand = new RelayCommand(() =>
        {
            LoadAnalyticsData();
            LastRefreshedText = $"Synced: {DateTime.Now:h:mm:ss tt}";
            OnPropertyChanged(nameof(LastRefreshedText));
            NotificationMessage = $"✅ Analytics refreshed live at {DateTime.Now:h:mm:ss tt} for '{SelectedPeriod}'. ({TotalTransactionsCount} recorded in MySQL)";
        });

        LoadAnalyticsData();
    }

    public void LoadAnalyticsData()
    {
        SalesTrendBars.Clear();
        CategorySales.Clear();
        CashierAudits.Clear();
        BestSellers.Clear();

        var liveResult = TransactionService.GetAnalytics(SelectedPeriod, out var err);
        if (liveResult != null)
        {
            TotalGrossSales = liveResult.FormattedTotalGrossSales;
            GrossSalesSubtitle = liveResult.GrossSalesSubtitle;
            TotalTransactionsCount = liveResult.FormattedTransactionsCount;
            TransactionsSubtitle = liveResult.TransactionsSubtitle;
            AverageBasketSize = liveResult.FormattedAverageBasketSize;
            BasketSubtitle = liveResult.BasketSubtitle;
            TotalTaxCollected = liveResult.FormattedTotalTaxCollected;
            TaxSubtitle = liveResult.TaxSubtitle;
            PeriodDateRangeSubtitle = liveResult.PeriodDateRangeSubtitle;
            PeakHourText = liveResult.PeakPeriodText;

            foreach (var b in liveResult.TrendBars)
            {
                SalesTrendBars.Add(new SalesTrendBar
                {
                    Label = b.Label,
                    Amount = b.Amount,
                    TransactionsCount = b.TransactionsCount,
                    BarHeight = b.BarHeight,
                    IsPeak = b.IsPeak
                });
            }

            foreach (var c in liveResult.CategorySales)
            {
                CategorySales.Add(new CategorySalesMetric
                {
                    CategoryName = c.CategoryName,
                    Icon = c.Icon,
                    Revenue = c.Revenue,
                    Percentage = c.Percentage,
                    BarColorHex = c.BarColorHex
                });
            }

            foreach (var ca in liveResult.CashierAudits)
            {
                CashierAudits.Add(new CashierAuditMetric
                {
                    CashierName = ca.CashierName,
                    Initials = ca.Initials,
                    StationName = ca.StationName,
                    TotalSales = ca.TotalSales,
                    ReceiptsProcessed = ca.ReceiptsProcessed,
                    ItemsSold = ca.ItemsSold,
                    SpeedRating = ca.SpeedRating,
                    AvatarBgHex = ca.AvatarBgHex
                });
            }

            foreach (var bs in liveResult.BestSellers)
            {
                BestSellers.Add(new BestSellerMetric
                {
                    Rank = bs.Rank,
                    ItemName = bs.ItemName,
                    Category = bs.Category,
                    CategoryIcon = bs.CategoryIcon,
                    UnitsSold = bs.UnitsSold,
                    Revenue = bs.Revenue
                });
            }

            if (liveResult.TotalTransactionsCount == 0)
            {
                NotificationMessage = "Live database sync active: 0 orders recorded. Awaiting store checkout transactions.";
            }
            else
            {
                NotificationMessage = $"Live database sync active: {liveResult.TotalTransactionsCount} orders loaded from MySQL.";
            }
        }
        else
        {
            SetZeroAnalyticsState(err);
        }

        NotifyAllProperties();
    }

    private void SetZeroAnalyticsState(string? reason = null)
    {
        TotalGrossSales = "₱0.00";
        GrossSalesSubtitle = "No completed sales recorded yet";
        TotalTransactionsCount = "0 Orders";
        TransactionsSubtitle = "Awaiting store register transactions";
        AverageBasketSize = "₱0.00";
        BasketSubtitle = "0.0 items per receipt average";
        TotalTaxCollected = "₱0.00";
        TaxSubtitle = "12% Output VAT standard rate";
        PeakHourText = "No transactions recorded";

        if (SelectedPeriod == "This Week")
            PeriodDateRangeSubtitle = "Showing 7-day sales audit (Monday – Sunday)";
        else if (SelectedPeriod == "This Month")
            PeriodDateRangeSubtitle = $"Showing monthly financial report for {DateTime.Today:MMMM yyyy}";
        else
            PeriodDateRangeSubtitle = $"Showing live POS register batches for {DateTime.Today:MMMM dd, yyyy}";

        SalesTrendBars.Clear();
        var defaultLabels = SelectedPeriod switch
        {
            "This Week" => new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
            "This Month" => new[] { "Week 1", "Week 2", "Week 3", "Week 4" },
            _ => new[] { "8 AM", "10 AM", "12 PM", "1 PM", "3 PM", "5 PM", "7 PM", "9 PM" }
        };

        foreach (var lbl in defaultLabels)
        {
            SalesTrendBars.Add(new SalesTrendBar
            {
                Label = lbl,
                Amount = 0m,
                TransactionsCount = 0,
                BarHeight = 8.0,
                IsPeak = false
            });
        }

        CategorySales.Clear();
        CashierAudits.Clear();
        BestSellers.Clear();

        if (!string.IsNullOrWhiteSpace(reason))
        {
            NotificationMessage = $"Database status: {reason}";
        }
    }

    private void OnExportReport()
    {
        try
        {
            var defaultFileName = $"OopsGrocery_SalesReport_{SelectedPeriod.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Sales & Analytics Report",
                Filter = "CSV (Comma delimited) (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = defaultFileName,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            };

            if (saveDialog.ShowDialog() != true)
            {
                return; // User cancelled
            }

            var filePath = saveDialog.FileName;
            var detailedTransactions = TransactionService.GetTransactionsWithItems(SelectedPeriod, out _);

            using (var writer = new StreamWriter(filePath, false, new System.Text.UTF8Encoding(true)))
            {
                writer.WriteLine("\"OopsGrocery Sales & Analytics Report\"");
                writer.WriteLine($"\"Selected Period\",\"{SelectedPeriod}\"");
                writer.WriteLine($"\"Generated At\",\"{DateTime.Now:yyyy-MM-dd hh:mm:ss tt}\"");
                writer.WriteLine("\"Payment Policy\",\"100% Cash Tender Only\"");
                writer.WriteLine();

                // 1. Executive Summary
                writer.WriteLine("\"--- EXECUTIVE FINANCIAL SUMMARY ---\"");
                writer.WriteLine("\"Metric\",\"Value\"");
                writer.WriteLine($"\"Total Gross Sales (PHP)\",\"{TotalGrossSales}\"");
                writer.WriteLine($"\"Completed Orders\",\"{TotalTransactionsCount}\"");
                writer.WriteLine($"\"Average Basket Size\",\"{AverageBasketSize}\"");
                writer.WriteLine($"\"Average Items Per Receipt\",\"{BasketSubtitle}\"");
                writer.WriteLine($"\"Output VAT (12%)\",\"{TotalTaxCollected}\"");
                writer.WriteLine($"\"Peak Sales Period\",\"{PeakHourText}\"");
                writer.WriteLine();

                // 2. Department Category Breakdown
                writer.WriteLine("\"--- DEPARTMENT / CATEGORY REVENUE ---\"");
                writer.WriteLine("\"Category\",\"Gross Revenue (PHP)\",\"Share Percentage\"");
                if (CategorySales.Count == 0)
                {
                    writer.WriteLine("\"No category sales recorded for this period\",0.00,0.0%");
                }
                else
                {
                    foreach (var cat in CategorySales)
                    {
                        writer.WriteLine($"\"{cat.CategoryName}\",{cat.Revenue:F2},{cat.Percentage:F1}%");
                    }
                }
                writer.WriteLine();

                // 3. Cashier Performance
                writer.WriteLine("\"--- CASHIER TERMINAL AUDITS ---\"");
                writer.WriteLine("\"Cashier Name\",\"Terminal Station\",\"Total Sales (PHP)\",\"Receipts Count\",\"Items Scanned\",\"Speed / Accuracy Rating\"");
                if (CashierAudits.Count == 0)
                {
                    writer.WriteLine("\"No cashier shift sales recorded for this period\",0.00,0,0,\"N/A\"");
                }
                else
                {
                    foreach (var c in CashierAudits)
                    {
                        writer.WriteLine($"\"{c.CashierName}\",\"{c.StationName}\",{c.TotalSales:F2},{c.ReceiptsProcessed},{c.ItemsSold},\"{c.SpeedRating}\"");
                    }
                }
                writer.WriteLine();

                // 4. Best Selling Products
                writer.WriteLine("\"--- TOP MOVING PRODUCTS (BEST SELLERS) ---\"");
                writer.WriteLine("\"Rank\",\"Product Name\",\"Category\",\"Units Sold\",\"Total Revenue (PHP)\"");
                if (BestSellers.Count == 0)
                {
                    writer.WriteLine("\"No product sales recorded for this period\",\"N/A\",0,0.00");
                }
                else
                {
                    foreach (var b in BestSellers)
                    {
                        writer.WriteLine($"{b.Rank},\"{b.ItemName}\",\"{b.Category}\",{b.UnitsSold},{b.Revenue:F2}");
                    }
                }
                writer.WriteLine();

                // 5. Individual Completed Transactions
                writer.WriteLine("\"--- ITEMIZATION OF COMPLETED TRANSACTIONS ---\"");
                writer.WriteLine("\"Receipt Number\",\"Date & Time\",\"Cashier\",\"Station\",\"Total Items\",\"Subtotal (PHP)\",\"Tax (PHP)\",\"Total Amount (PHP)\",\"Amount Tendered (PHP)\",\"Change Given (PHP)\",\"Payment Method\",\"Status\"");
                if (detailedTransactions.Count == 0)
                {
                    writer.WriteLine("\"No completed checkout transactions recorded yet\",\"-\",\"-\",\"-\",0,0.00,0.00,0.00,0.00,0.00,\"-\",\"-\"");
                }
                else
                {
                    foreach (var t in detailedTransactions)
                    {
                        writer.WriteLine($"\"{t.ReceiptNumber}\",\"{t.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{t.CashierName}\",\"{t.StationName}\",{t.TotalItems},{t.Subtotal:F2},{t.Tax:F2},{t.TotalAmount:F2},{t.AmountTendered:F2},{t.ChangeGiven:F2},\"{t.PaymentMethod}\",\"{t.Status}\"");
                    }
                }
                writer.WriteLine();

                // 6. Detailed Sold Items Ledger
                if (detailedTransactions.Any(t => t.Items.Count > 0))
                {
                    writer.WriteLine("\"--- DETAILED TRANSACTION LINE ITEMS ---\"");
                    writer.WriteLine("\"Receipt Number\",\"SKU\",\"Product Name\",\"Category\",\"Unit Price (PHP)\",\"Quantity\",\"Line Total (PHP)\"");
                    foreach (var t in detailedTransactions)
                    {
                        foreach (var item in t.Items)
                        {
                            writer.WriteLine($"\"{t.ReceiptNumber}\",\"{item.Sku}\",\"{item.ProductName}\",\"{item.Category}\",{item.UnitPrice:F2},{item.Quantity},{item.TotalPrice:F2}");
                        }
                    }
                }
            }

            NotificationMessage = $"✅ Report successfully exported to: {Path.GetFileName(filePath)}";

            // Launch custom modern OopsGrocery Export Complete modal window
            ExportCompleteWindow.ShowDialog(filePath, SelectedPeriod);
        }
        catch (Exception ex)
        {
            NotificationMessage = $"❌ Export failed: {ex.Message}";
            MessageBoxWindow.Show($"Failed to export report: {ex.Message}", "Export Error", type: MessageBoxType.Error);
        }
    }

    private void NotifyAllProperties()
    {
        OnPropertyChanged(nameof(TotalGrossSales));
        OnPropertyChanged(nameof(GrossSalesSubtitle));
        OnPropertyChanged(nameof(TotalTransactionsCount));
        OnPropertyChanged(nameof(TransactionsSubtitle));
        OnPropertyChanged(nameof(AverageBasketSize));
        OnPropertyChanged(nameof(BasketSubtitle));
        OnPropertyChanged(nameof(TotalTaxCollected));
        OnPropertyChanged(nameof(TaxSubtitle));
        OnPropertyChanged(nameof(PeriodDateRangeSubtitle));
        OnPropertyChanged(nameof(PeakHourText));
        OnPropertyChanged(nameof(LastRefreshedText));
        OnPropertyChanged(nameof(HasCategorySales));
        OnPropertyChanged(nameof(HasNoCategorySales));
        OnPropertyChanged(nameof(HasCashierAudits));
        OnPropertyChanged(nameof(HasNoCashierAudits));
        OnPropertyChanged(nameof(HasBestSellers));
        OnPropertyChanged(nameof(HasNoBestSellers));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
