using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace OopsGrocery.Views;

public partial class ExportCompleteWindow : Window
{
    public string FilePath { get; }
    public string Period { get; }

    public ExportCompleteWindow(string filePath, string period)
    {
        InitializeComponent();
        FilePath = filePath;
        Period = period;

        FileNameText.Text = Path.GetFileName(filePath);
        FilePathText.Text = filePath;
        PeriodText.Text = period;
        TimestampText.Text = DateTime.Now.ToString("h:mm tt");

        try
        {
            if (File.Exists(filePath))
            {
                var length = new FileInfo(filePath).Length;
                FileSizeText.Text = length < 1024 ? $"{length} B" : $"{length / 1024.0:F1} KB";
            }
            else
            {
                FileSizeText.Text = "Ready";
            }
        }
        catch
        {
            FileSizeText.Text = "Ready";
        }

        MouseDown += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        };

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        };
    }

    private void OpenFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(FilePath))
            {
                Process.Start(new ProcessStartInfo(FilePath)
                {
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show($"Unable to open file: {ex.Message}", "Open File Error", type: MessageBoxType.Error);
        }

        Close();
    }

    private void OpenFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(FilePath))
            {
                Process.Start("explorer.exe", $"/select,\"{FilePath}\"");
            }
            else
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                {
                    Process.Start("explorer.exe", $"\"{dir}\"");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show($"Unable to open folder: {ex.Message}", "Open Folder Error", type: MessageBoxType.Error);
        }
    }

    private void CopyPathButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(FilePath);
            CopyPathButton.Content = "✓ Copied!";
        }
        catch
        {
            // Clipboard access might occasionally fail in concurrent environments
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    public static void ShowDialog(string filePath, string period, Window? owner = null)
    {
        var window = new ExportCompleteWindow(filePath, period);
        var activeWindow = owner
            ?? Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            ?? Application.Current.MainWindow;

        if (activeWindow != null && activeWindow.IsLoaded)
        {
            window.Owner = activeWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        window.ShowDialog();
    }
}
