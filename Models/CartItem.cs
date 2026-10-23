using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OopsGrocery.Models;

public sealed class CartItem : INotifyPropertyChanged
{
    private int _quantity;

    public ProductItem Product { get; }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity == value) return;
            _quantity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(FormattedTotalPrice));
        }
    }

    public decimal TotalPrice => Product.Price * Quantity;
    public string FormattedTotalPrice => $"${TotalPrice:F2}";

    public CartItem(ProductItem product, int quantity = 1)
    {
        Product = product;
        Quantity = quantity;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
