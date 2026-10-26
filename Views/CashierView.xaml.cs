using System.Windows.Controls;
using System.Windows.Input;

namespace OopsGrocery.Views;

public partial class CashierView : UserControl
{
    public CashierView()
    {
        InitializeComponent();
    }

    private void CategoryScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            if (e.Delta < 0)
            {
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + 60);
            }
            else
            {
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - 60);
            }
            e.Handled = true;
        }
    }
}
