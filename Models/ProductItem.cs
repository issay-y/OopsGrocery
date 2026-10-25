using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace OopsGrocery.Models;

public sealed class ProductItem : INotifyPropertyChanged
{
    private int _id;
    private string _sku = string.Empty;
    private string _name = string.Empty;
    private string _category = "Other";
    private decimal _price;
    private string _unit = "pc";
    private int _stockQuantity;
    private string _tag = "In Stock";
    private string? _imagePath;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id
    {
        get => _id;
        set
        {
            if (_id == value) return;
            _id = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FormattedId));
        }
    }

    public string Sku
    {
        get => _sku;
        set
        {
            if (_sku == value) return;
            _sku = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    public string Category
    {
        get => _category;
        set
        {
            if (_category == value) return;
            _category = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CategoryIcon));
            OnPropertyChanged(nameof(CategoryBackground));
        }
    }

    public decimal Price
    {
        get => _price;
        set
        {
            if (_price == value) return;
            _price = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FormattedPrice));
            OnPropertyChanged(nameof(FormattedPriceWithUnit));
        }
    }

    public string Unit
    {
        get => _unit;
        set
        {
            if (_unit == value) return;
            _unit = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FormattedPriceWithUnit));
        }
    }

    public int StockQuantity
    {
        get => _stockQuantity;
        set
        {
            if (_stockQuantity == value) return;
            _stockQuantity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsAvailable));
            OnPropertyChanged(nameof(IsLowStock));
            OnPropertyChanged(nameof(IsOutOfStock));
            SyncComputedTag();
        }
    }

    public string Tag
    {
        get => _tag;
        set
        {
            if (_tag == value) return;
            _tag = value;
            OnPropertyChanged();
        }
    }

    public string? ImagePath
    {
        get => _imagePath;
        set
        {
            if (_imagePath == value) return;
            _imagePath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasCustomImage));
        }
    }

    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime _updatedAt = DateTime.UtcNow;

    public DateTime CreatedAt
    {
        get => _createdAt;
        set
        {
            if (_createdAt == value) return;
            _createdAt = value;
            OnPropertyChanged();
        }
    }

    public DateTime UpdatedAt
    {
        get => _updatedAt;
        set
        {
            if (_updatedAt == value) return;
            _updatedAt = value;
            OnPropertyChanged();
        }
    }

    public bool HasCustomImage => !string.IsNullOrWhiteSpace(_imagePath) && (File.Exists(_imagePath) || _imagePath.StartsWith("pack://", StringComparison.OrdinalIgnoreCase));

    public string FormattedId => $"INV-{Id:D4}";

    public string FormattedPrice => $"₱{Price:N2}";

    public string FormattedPriceWithUnit => $"₱{Price:N2} / {Unit}";

    public const int DefaultMinimumStock = 20;
    private int _minimumStock = DefaultMinimumStock;

    public int MinimumStock
    {
        get => _minimumStock;
        set
        {
            if (_minimumStock == value) return;
            _minimumStock = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsAvailable));
            OnPropertyChanged(nameof(IsLowStock));
            SyncComputedTag();
        }
    }

    private bool _isArchived;

    public bool IsArchived
    {
        get => _isArchived;
        set
        {
            if (_isArchived == value) return;
            _isArchived = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsAvailable));
            OnPropertyChanged(nameof(IsLowStock));
            OnPropertyChanged(nameof(IsOutOfStock));
            OnPropertyChanged(nameof(ArchiveIconGlyph));
            OnPropertyChanged(nameof(ArchiveTooltip));
            SyncComputedTag();
        }
    }

    private void SyncComputedTag()
    {
        var computedTag = IsArchived ? "Archived" : (StockQuantity <= 0 ? "Out of Stock" : (StockQuantity <= MinimumStock ? "Low Stock" : "In Stock"));
        if (_tag != computedTag)
        {
            _tag = computedTag;
            OnPropertyChanged(nameof(Tag));
        }
    }

    public string ArchiveIconGlyph => IsArchived ? "\uE7A7" : "\uEA35";

    public string ArchiveTooltip => IsArchived ? "Restore product to active inventory" : "Archive product";

    public string Status => IsArchived ? "Archived" : (StockQuantity <= 0 ? "Out of Stock" : (StockQuantity <= MinimumStock ? "Low Stock" : "Available"));

    public bool IsAvailable => !IsArchived && StockQuantity > MinimumStock;

    public bool IsLowStock => !IsArchived && StockQuantity > 0 && StockQuantity <= MinimumStock;

    public bool IsOutOfStock => !IsArchived && StockQuantity <= 0;

    /// <summary>
    /// Safe category illustration icon based on user's exact category taxonomy.
    /// </summary>
    public string CategoryIcon => GetCategoryIcon(Category);

    public static string GetCategoryIcon(string? category) => category?.Trim().ToLowerInvariant() switch
    {
        "beverages" => "🥤",
        "canned goods" or "canned godds" => "🥫",
        "condiments" => "🧂",
        "dairy" => "🥛",
        "fruits & vegetables" => "🍎",
        "household" => "🧼",
        "instant noodles" => "🍜",
        "meat" => "🥩",
        "personal care" => "🧴",
        "snacks" => "🍿",
        _ => "📦"
    };

    /// <summary>
    /// Soft pastel background color for category badge.
    /// </summary>
    public string CategoryBackground => GetCategoryBackground(Category);

    public static string GetCategoryBackground(string? category) => category?.Trim().ToLowerInvariant() switch
    {
        "beverages" => "#EFF6FF",           // Soft light blue
        "canned goods" or "canned godds" => "#FEF2F2", // Soft red
        "condiments" => "#FFFBEB",          // Soft amber
        "dairy" => "#F0FDF4",               // Soft mint green
        "fruits & vegetables" => "#ECFDF5", // Soft emerald
        "household" => "#F0FDFA",           // Soft teal
        "instant noodles" => "#FFF7ED",     // Soft warm orange
        "meat" => "#FFF1F2",                // Soft rose
        "personal care" => "#FAF5FF",       // Soft lavender
        "snacks" => "#FEFCE8",              // Soft yellow
        _ => "#F1F5F9"                      // Neutral slate
    };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
