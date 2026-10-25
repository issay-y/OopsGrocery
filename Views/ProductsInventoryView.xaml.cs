using System.Windows.Controls;
using System.Windows.Input;

namespace OopsGrocery.Views;

public partial class ProductsInventoryView : UserControl
{
    public ProductsInventoryView()
    {
        InitializeComponent();
    }

    private void CategoryScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            if (e.Delta < 0)
            {
                scrollViewer.LineRight();
                scrollViewer.LineRight();
            }
            else
            {
                scrollViewer.LineLeft();
                scrollViewer.LineLeft();
            }
            e.Handled = true;
        }
    }
}
