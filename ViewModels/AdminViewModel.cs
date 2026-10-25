using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using OopsGrocery.Commands;
using OopsGrocery.Models;
using OopsGrocery.Services;
using OopsGrocery.Views;

namespace OopsGrocery.ViewModels;

public sealed class AdminViewModel : INotifyPropertyChanged
{
    private string _notificationMessage = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? RequestNavigateToInventory;

    // Top 4 Boxes
    public string YesterdaySalesText { get; private set; } = "₱0.00";
    public string YesterdaySalesSubtext { get; private set; } = "No sales recorded";

    public string TodaySalesText { get; private set; } = "₱0.00";
    public string TodaySalesSubtext { get; private set; } = "No sales recorded";

    public string TodayTransactionsCountText { get; private set; } = "0 Orders";
    public string TodayTransactionsSubtext { get; private set; } = "₱0.00 avg basket size";

    public string LowStockProductsCountText { get; private set; } = "0 items";
    public string LowStockProductsSubtext { get; private set; } = "0 low · 0 out of stock";

    public int TotalCategoryItemsCount { get; private set; }
    public string TotalSevenDaySalesFormatted { get; private set; } = "₱0.00";

    // Chart Data Collections
    public ObservableCollection<DailySalesPoint> DailySales { get; } = [];
    public ObservableCollection<CategoryDonutSegment> CategorySegments { get; } = [];
    public ObservableCollection<BestSellerBarData> BestSellers { get; } = [];
    public ObservableCollection<LowestStockItemData> LowestStockItems { get; } = [];

    public bool HasBestSellers => BestSellers.Count > 0;
    public bool HasNoBestSellers => BestSellers.Count == 0;
    public bool HasLowestStockItems => LowestStockItems.Count > 0;

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

    public ICommand NavigateToInventoryCommand { get; }
    public ICommand RestockCommand { get; }
    public ICommand RefreshCommand { get; }

    public AdminViewModel()
    {
        NavigateToInventoryCommand = new RelayCommand(() =>
        {
            RequestNavigateToInventory?.Invoke();
        });

        RestockCommand = new RelayCommand<object>(OnRestockItem);
        RefreshCommand = new RelayCommand(() =>
        {
            LoadDashboardData();
            NotificationMessage = $"Dashboard refreshed live at {DateTime.Now:h:mm:ss tt}.";
        });

        LoadDashboardData();
    }

    public void LoadDashboardData()
    {
        var data = DashboardService.GetDashboardData(out string? err);

        YesterdaySalesText = data.YesterdaySalesFormatted;
        YesterdaySalesSubtext = data.YesterdaySubtext;

        TodaySalesText = data.TodaySalesFormatted;
        TodaySalesSubtext = data.TodaySubtext;

        TodayTransactionsCountText = data.TodayTransactionsFormatted;
        TodayTransactionsSubtext = data.TodayBasketSubtext;

        LowStockProductsCountText = $"{data.LowStockProductsCount} items";
        LowStockProductsSubtext = data.LowStockSubtext;

        TotalCategoryItemsCount = data.TotalCategoryItemsCount;

        decimal total7Days = data.DailySales.Sum(d => d.Amount);
        TotalSevenDaySalesFormatted = $"₱{total7Days:N2}";

        DailySales.Clear();
        foreach (var d in data.DailySales)
        {
            DailySales.Add(d);
        }

        CategorySegments.Clear();
        foreach (var c in data.CategorySegments)
        {
            CategorySegments.Add(c);
        }

        BestSellers.Clear();
        foreach (var b in data.BestSellers)
        {
            BestSellers.Add(b);
        }

        LowestStockItems.Clear();
        foreach (var item in data.LowestStockItems)
        {
            LowestStockItems.Add(item);
        }

        if (!string.IsNullOrWhiteSpace(err))
        {
            NotificationMessage = $"Database notice: {err}";
        }

        NotifyAllProperties();
    }

    private void OnRestockItem(object? param)
    {
        ProductItem? item = null;
        if (param is LowestStockItemData lowItem)
        {
            item = lowItem.Product;
        }
        else if (param is ProductItem p)
        {
            item = p;
        }

        if (item == null) return;

        var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;
        var dialog = new RestockProductWindow(item);
        if (activeWindow != null && activeWindow.IsLoaded)
        {
            dialog.Owner = activeWindow;
        }

        if (dialog.ShowDialog() == true && dialog.RestockedQuantity > 0)
        {
            var success = ProductService.RestockProduct(item.Id, dialog.RestockedQuantity, out int newStock, out var error);
            if (success)
            {
                item.StockQuantity = newStock;
                NotificationMessage = $"Restocked +{dialog.RestockedQuantity} units for '{item.Name}'. New stock level: {newStock} (saved to database).";
            }
            else
            {
                item.StockQuantity += dialog.RestockedQuantity;
                NotificationMessage = $"Restocked +{dialog.RestockedQuantity} units locally. Database warning: {error}";
            }

            LoadDashboardData();
        }
    }

    private void NotifyAllProperties()
    {
        OnPropertyChanged(nameof(YesterdaySalesText));
        OnPropertyChanged(nameof(YesterdaySalesSubtext));
        OnPropertyChanged(nameof(TodaySalesText));
        OnPropertyChanged(nameof(TodaySalesSubtext));
        OnPropertyChanged(nameof(TodayTransactionsCountText));
        OnPropertyChanged(nameof(TodayTransactionsSubtext));
        OnPropertyChanged(nameof(LowStockProductsCountText));
        OnPropertyChanged(nameof(LowStockProductsSubtext));
        OnPropertyChanged(nameof(TotalCategoryItemsCount));
        OnPropertyChanged(nameof(TotalSevenDaySalesFormatted));
        OnPropertyChanged(nameof(HasBestSellers));
        OnPropertyChanged(nameof(HasNoBestSellers));
        OnPropertyChanged(nameof(HasLowestStockItems));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
