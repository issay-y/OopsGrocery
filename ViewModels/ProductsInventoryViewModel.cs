using System;
using System.Collections.Generic;
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

public sealed class ProductsInventoryViewModel : INotifyPropertyChanged
{
    private string _searchQuery = string.Empty;
    private string _selectedCategory = "All Categories";
    private string _notificationMessage = string.Empty;
    private int _currentPage = 1;
    private int _pageSize = 6;
    private readonly List<ProductItem> _masterCatalog = [];
    private readonly List<ProductItem> _matchingCatalog = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ProductItem> DisplayedProducts { get; } = [];
    public ObservableCollection<string> Categories { get; } = [];
    public ObservableCollection<int> PageSizes { get; } = [10, 25, 50];
    public ObservableCollection<int> PageNumbers { get; } = [];

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery == value) return;
            _searchQuery = value;
            OnPropertyChanged();
            CurrentPage = 1;
            ApplyFilterAndPagination();
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
            CurrentPage = 1;
            ApplyFilterAndPagination();
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

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (_currentPage == value) return;
            _currentPage = value;
            OnPropertyChanged();
            ApplyFilterAndPagination();
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (_pageSize == value) return;
            _pageSize = value;
            OnPropertyChanged();
            CurrentPage = 1;
            ApplyFilterAndPagination();
        }
    }

    private bool _showArchivedOnly;

    public bool ShowArchivedOnly
    {
        get => _showArchivedOnly;
        set
        {
            if (_showArchivedOnly == value) return;
            _showArchivedOnly = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ArchiveToggleText));
            OnPropertyChanged(nameof(TableSectionTitle));
            CurrentPage = 1;
            ApplyFilterAndPagination();
        }
    }

    public string ArchiveToggleText => ShowArchivedOnly ? $"Active Items ({ActiveCount})" : $"Archived ({ArchivedCount})";
    public string TableSectionTitle => ShowArchivedOnly ? "ARCHIVED PRODUCTS" : "INVENTORY ITEMS";

    public int ArchivedCount => _masterCatalog.Count(p => p.IsArchived);
    public int ActiveCount => _masterCatalog.Count(p => !p.IsArchived);

    // 1. Summary Cards (Reflects active products)
    public int InventoryItemsCount => _masterCatalog.Count(p => !p.IsArchived);
    public int AvailableCount => _masterCatalog.Count(p => !p.IsArchived && p.IsAvailable);
    public int LowStockCount => _masterCatalog.Count(p => !p.IsArchived && p.IsLowStock);
    public int OutOfStockCount => _masterCatalog.Count(p => !p.IsArchived && p.IsOutOfStock);

    // Pagination metrics
    public int TotalFilteredCount => _matchingCatalog.Count;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalFilteredCount / PageSize));
    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => TotalFilteredCount > 0 && CurrentPage < TotalPages;
    public bool HasNoResults => TotalFilteredCount == 0;

    public string ShowingRecordCountText
    {
        get
        {
            if (TotalFilteredCount == 0)
            {
                return "Showing 0 items";
            }

            int start = (CurrentPage - 1) * PageSize + 1;
            int end = Math.Min(CurrentPage * PageSize, TotalFilteredCount);
            return $"Showing {start}–{end} of {TotalFilteredCount} items";
        }
    }

    // Commands
    public ICommand SelectCategoryCommand { get; }
    public ICommand ClearFilterCommand { get; }
    public ICommand ToggleArchiveViewCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand GoToPageCommand { get; }
    public ICommand AddProductCommand { get; }
    public ICommand EditProductCommand { get; }
    public ICommand RestockCommand { get; }
    public ICommand ArchiveProductCommand { get; }
    public ICommand RefreshCommand { get; }

    public ProductsInventoryViewModel()
    {
        Categories.Add("All Categories");
        Categories.Add("Beverages");
        Categories.Add("Canned Goods");
        Categories.Add("Condiments");
        Categories.Add("Dairy");
        Categories.Add("Fruits & Vegetables");
        Categories.Add("Household");
        Categories.Add("Instant Noodles");
        Categories.Add("Meat");
        Categories.Add("Personal Care");
        Categories.Add("Snacks");
        Categories.Add("Other");

        SelectCategoryCommand = new RelayCommand<string>(cat =>
        {
            if (!string.IsNullOrWhiteSpace(cat))
            {
                SelectedCategory = cat;
            }
        });

        ClearFilterCommand = new RelayCommand(() =>
        {
            _searchQuery = string.Empty;
            _selectedCategory = "All Categories";
            _showArchivedOnly = false;
            _currentPage = 1;
            OnPropertyChanged(nameof(SearchQuery));
            OnPropertyChanged(nameof(SelectedCategory));
            OnPropertyChanged(nameof(ShowArchivedOnly));
            OnPropertyChanged(nameof(ArchiveToggleText));
            ApplyFilterAndPagination();
            NotificationMessage = "Search, category, and archive filters have been reset.";
        });

        ToggleArchiveViewCommand = new RelayCommand(() => ShowArchivedOnly = !ShowArchivedOnly);

        PreviousPageCommand = new RelayCommand(() =>
        {
            if (CanGoPrevious)
            {
                CurrentPage--;
            }
        });

        NextPageCommand = new RelayCommand(() =>
        {
            if (CanGoNext)
            {
                CurrentPage++;
            }
        });

        GoToPageCommand = new RelayCommand<int>(page =>
        {
            if (page >= 1 && page <= TotalPages && page != CurrentPage)
            {
                CurrentPage = page;
            }
        });

        AddProductCommand = new RelayCommand(OnAddProduct);
        EditProductCommand = new RelayCommand<ProductItem>(OnEditProduct);
        RestockCommand = new RelayCommand<ProductItem>(OnRestockProduct);
        ArchiveProductCommand = new RelayCommand<ProductItem>(OnArchiveProduct);
        RefreshCommand = new RelayCommand(() => LoadProducts(isManualRefresh: true));

        // Load real inventory records from the MySQL database
        LoadProducts(isManualRefresh: false);
    }

    /// <summary>
    /// Loads inventory products directly from MySQL Products table.
    /// </summary>
    public void LoadProducts(bool isManualRefresh = false)
    {
        var items = ProductService.GetAllProducts(out bool loadedFromDb, out string? error);
        _masterCatalog.Clear();
        _masterCatalog.AddRange(items);

        ApplyFilterAndPagination();

        if (isManualRefresh)
        {
            if (loadedFromDb)
            {
                NotificationMessage = $"Inventory catalog synchronized with database ({_masterCatalog.Count} items).";
            }
            else
            {
                NotificationMessage = $"Loaded cached inventory. Database note: {error}";
            }
        }
    }

    private void ApplyFilterAndPagination()
    {
        _matchingCatalog.Clear();
        var query = SearchQuery.Trim().ToLowerInvariant();

        foreach (var item in _masterCatalog)
        {
            if (item.IsArchived != ShowArchivedOnly)
                continue;

            var matchCategory = SelectedCategory == "All Categories" ||
                                item.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase);

            var matchQuery = string.IsNullOrWhiteSpace(query) ||
                             item.Name.ToLowerInvariant().Contains(query) ||
                             item.Id.ToString().Contains(query) ||
                             item.FormattedId.ToLowerInvariant().Contains(query) ||
                             item.Sku.ToLowerInvariant().Contains(query) ||
                             item.Category.ToLowerInvariant().Contains(query);

            if (matchCategory && matchQuery)
            {
                _matchingCatalog.Add(item);
            }
        }

        // Clamp CurrentPage if filtered results are fewer
        var maxPages = TotalPages;
        if (_currentPage > maxPages)
        {
            _currentPage = maxPages;
            OnPropertyChanged(nameof(CurrentPage));
        }
        else if (_currentPage < 1)
        {
            _currentPage = 1;
            OnPropertyChanged(nameof(CurrentPage));
        }

        // Extract page items
        DisplayedProducts.Clear();
        var pageItems = _matchingCatalog
            .Skip((_currentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pageItems)
        {
            DisplayedProducts.Add(item);
        }

        UpdatePageNumbers();
        NotifyPagingAndMetrics();
    }

    private void UpdatePageNumbers()
    {
        PageNumbers.Clear();
        if (TotalFilteredCount == 0) return;

        int total = TotalPages;
        for (int i = 1; i <= total; i++)
        {
            PageNumbers.Add(i);
        }
    }

    private void NotifyPagingAndMetrics()
    {
        OnPropertyChanged(nameof(InventoryItemsCount));
        OnPropertyChanged(nameof(AvailableCount));
        OnPropertyChanged(nameof(LowStockCount));
        OnPropertyChanged(nameof(OutOfStockCount));
        OnPropertyChanged(nameof(ArchivedCount));
        OnPropertyChanged(nameof(ActiveCount));
        OnPropertyChanged(nameof(ArchiveToggleText));
        OnPropertyChanged(nameof(TotalFilteredCount));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(ShowingRecordCountText));
        OnPropertyChanged(nameof(HasNoResults));
    }

    private void OnAddProduct()
    {
        var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;
        var existingUnits = _masterCatalog.Select(p => p.Unit).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
        var dialog = new AddProductWindow(Categories.Where(c => c != "All Categories").ToList(), existingUnits);
        if (activeWindow != null && activeWindow.IsLoaded)
        {
            dialog.Owner = activeWindow;
        }

        if (dialog.ShowDialog() == true && dialog.CreatedProduct != null)
        {
            var success = ProductService.AddProduct(dialog.CreatedProduct, out var error);
            if (success)
            {
                _masterCatalog.Insert(0, dialog.CreatedProduct);
                NotificationMessage = $"Item '{dialog.CreatedProduct.Name}' ({dialog.CreatedProduct.FormattedId}) added and saved to database.";
            }
            else
            {
                dialog.CreatedProduct.Id = _masterCatalog.Count > 0 ? _masterCatalog.Max(p => p.Id) + 1 : 101;
                dialog.CreatedProduct.Sku = $"PRD-{dialog.CreatedProduct.Id}";
                _masterCatalog.Insert(0, dialog.CreatedProduct);
                NotificationMessage = $"Item added locally. Database warning: {error}";
            }

            // If user was viewing archived products or has active filter, reset to active inventory to show newly created item
            if (_showArchivedOnly)
            {
                _showArchivedOnly = false;
                OnPropertyChanged(nameof(ShowArchivedOnly));
                OnPropertyChanged(nameof(ArchiveToggleText));
                OnPropertyChanged(nameof(TableSectionTitle));
            }
            _searchQuery = string.Empty;
            OnPropertyChanged(nameof(SearchQuery));
            _selectedCategory = "All Categories";
            OnPropertyChanged(nameof(SelectedCategory));
            _currentPage = 1;
            OnPropertyChanged(nameof(CurrentPage));

            ApplyFilterAndPagination();
        }
    }

    private void OnEditProduct(ProductItem? item)
    {
        if (item == null) return;

        var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;
        var dialog = new EditProductWindow(item, Categories.Where(c => c != "All Categories").ToList());
        if (activeWindow != null && activeWindow.IsLoaded)
        {
            dialog.Owner = activeWindow;
        }

        if (dialog.ShowDialog() == true)
        {
            var success = ProductService.UpdateProduct(item, out var error);
            if (success)
            {
                NotificationMessage = $"Inventory item '{item.Name}' ({item.FormattedId}) details updated in database.";
            }
            else
            {
                NotificationMessage = $"Inventory item '{item.Name}' updated locally. Database warning: {error}";
            }

            ApplyFilterAndPagination();
        }
    }

    private void OnRestockProduct(ProductItem? item)
    {
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
                NotificationMessage = $"Restocked +{dialog.RestockedQuantity} units for '{item.Name}'. New stock level: {item.StockQuantity} (saved to database).";
            }
            else
            {
                item.StockQuantity += dialog.RestockedQuantity;
                NotificationMessage = $"Restocked +{dialog.RestockedQuantity} units locally. Database warning: {error}";
            }

            ApplyFilterAndPagination();
        }
    }

    private void OnArchiveProduct(ProductItem? item)
    {
        if (item == null) return;

        var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

        if (!item.IsArchived)
        {
            var confirmed = MessageBoxWindow.Confirm(
                $"Are you sure you want to archive '{item.Name}' ({item.FormattedId})?\n\nArchived products will be hidden from active inventory and sales.",
                "Archive Product",
                confirmButtonText: "Yes, Archive",
                cancelButtonText: "Cancel",
                owner: activeWindow,
                type: MessageBoxType.Warning,
                subtitle: "Inventory Management");

            if (!confirmed) return;

            var success = ProductService.ArchiveProduct(item.Id, true, out var error);
            if (success)
            {
                item.IsArchived = true;
                NotificationMessage = $"Product '{item.Name}' ({item.FormattedId}) has been archived.";
            }
            else
            {
                item.IsArchived = true;
                NotificationMessage = $"Product '{item.Name}' archived locally. Database warning: {error}";
            }
        }
        else
        {
            var confirmed = MessageBoxWindow.Confirm(
                $"Restore '{item.Name}' ({item.FormattedId}) back to active inventory?",
                "Restore Product",
                confirmButtonText: "Yes, Restore",
                cancelButtonText: "Cancel",
                owner: activeWindow,
                type: MessageBoxType.Question,
                subtitle: "Inventory Management");

            if (!confirmed) return;

            var success = ProductService.ArchiveProduct(item.Id, false, out var error);
            if (success)
            {
                item.IsArchived = false;
                NotificationMessage = $"Product '{item.Name}' ({item.FormattedId}) restored to active inventory.";
            }
            else
            {
                item.IsArchived = false;
                NotificationMessage = $"Product '{item.Name}' restored locally. Database warning: {error}";
            }
        }

        ApplyFilterAndPagination();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
