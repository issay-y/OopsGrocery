using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using OopsGrocery.Commands;
using OopsGrocery.Models;

namespace OopsGrocery.ViewModels;

public sealed class AdminViewModel : INotifyPropertyChanged
{
    private string _searchQuery = string.Empty;
    private string _notificationMessage = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string TotalRevenue => "$18,429.50";
    public int ActiveOrdersCount => 34;
    public int LowStockCount => 5;
    public int CustomerCount => 248;

    public ObservableCollection<ProductItem> Inventory { get; }
    public ObservableCollection<OrderSummary> RecentOrders { get; }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery == value) return;
            _searchQuery = value;
            OnPropertyChanged();
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

    public ICommand RestockCommand { get; }
    public ICommand QuickExportCommand { get; }

    public AdminViewModel()
    {
        Inventory = new ObservableCollection<ProductItem>
        {
            new() { Id = 101, Sku = "PRD-101", Name = "Organic Honeycrisp Apples", Category = "Fresh Produce", Price = 2.99m, Unit = "lb", StockQuantity = 145, Tag = "Organic" },
            new() { Id = 102, Sku = "PRD-102", Name = "Farm Fresh Whole Milk (1 Gal)", Category = "Dairy & Eggs", Price = 4.29m, Unit = "gal", StockQuantity = 38, Tag = "Fresh Daily" },
            new() { Id = 103, Sku = "PRD-103", Name = "Artisan Sourdough Boule", Category = "Bakery", Price = 5.50m, Unit = "loaf", StockQuantity = 6, Tag = "Low Stock" },
            new() { Id = 104, Sku = "PRD-104", Name = "Wild Caught Atlantic Salmon", Category = "Meat & Seafood", Price = 14.99m, Unit = "lb", StockQuantity = 22, Tag = "Wild" },
            new() { Id = 105, Sku = "PRD-105", Name = "Extra Virgin Olive Oil (500ml)", Category = "Pantry", Price = 16.75m, Unit = "btl", StockQuantity = 4, Tag = "Low Stock" },
            new() { Id = 106, Sku = "PRD-106", Name = "Hass Avocados (Bag of 4)", Category = "Fresh Produce", Price = 4.99m, Unit = "bag", StockQuantity = 65, Tag = "Organic" },
            new() { Id = 107, Sku = "PRD-107", Name = "Free-Range Large Eggs (1 Doz)", Category = "Dairy & Eggs", Price = 4.89m, Unit = "doz", StockQuantity = 52, Tag = "Free Range" },
            new() { Id = 108, Sku = "PRD-108", Name = "Cold-Pressed Orange Juice (1L)", Category = "Beverages", Price = 5.25m, Unit = "btl", StockQuantity = 8, Tag = "Low Stock" }
        };

        RecentOrders = new ObservableCollection<OrderSummary>
        {
            new() { OrderId = "#ORD-9021", CustomerName = "Emily Watson", TotalAmount = 48.20m, Status = "Processing", PlacedAt = "10 mins ago" },
            new() { OrderId = "#ORD-9020", CustomerName = "Marcus Chen", TotalAmount = 112.50m, Status = "Ready for Pickup", PlacedAt = "25 mins ago" },
            new() { OrderId = "#ORD-9019", CustomerName = "Sarah Jenkins", TotalAmount = 29.75m, Status = "Completed", PlacedAt = "1 hr ago" },
            new() { OrderId = "#ORD-9018", CustomerName = "David Miller", TotalAmount = 86.40m, Status = "Completed", PlacedAt = "2 hrs ago" }
        };

        RestockCommand = new RelayCommand<ProductItem>(item =>
        {
            if (item != null)
            {
                item.StockQuantity += 25;
                NotificationMessage = $"Restocked {item.Name} (+25 units). Current stock: {item.StockQuantity}.";
            }
        });

        QuickExportCommand = new RelayCommand(() =>
        {
            NotificationMessage = "Inventory & sales report successfully exported to CSV.";
        });
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
