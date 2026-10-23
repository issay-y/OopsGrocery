using System.Windows;
using System.Windows.Input;

namespace OopsGrocery.Views;

public partial class MessageBoxWindow : Window
{
    public MessageBoxWindow(string message, string title = "Oops Grocery Alert")
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;

        // Allow dragging the window anywhere by clicking on the background
        MouseDown += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        };

        KeyDown += (_, e) =>
        {
            if (e.Key is Key.Enter or Key.Escape or Key.Space)
            {
                DialogResult = true;
                Close();
            }
        };

        Loaded += (_, _) => ActionButton.Focus();
    }

    private void ActionButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    public static bool? Show(string message, string title = "Oops Grocery Alert", Window? owner = null)
    {
        var window = new MessageBoxWindow(message, title);
        var activeWindow = owner ?? Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

        if (activeWindow != null && activeWindow.IsLoaded)
        {
            window.Owner = activeWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        return window.ShowDialog();
    }
}
