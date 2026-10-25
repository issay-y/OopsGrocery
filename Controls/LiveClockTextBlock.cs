using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OopsGrocery.Controls;

/// <summary>
/// A TextBlock that shows the current date and time and refreshes every second.
/// Self-contained: no code-behind or view-model changes needed.
/// </summary>
public class LiveClockTextBlock : TextBlock
{
    private readonly DispatcherTimer _timer;

    public static readonly DependencyProperty FormatProperty =
        DependencyProperty.Register(
            nameof(Format),
            typeof(string),
            typeof(LiveClockTextBlock),
            new PropertyMetadata("ddd, MMM d, yyyy  •  hh:mm:ss tt", OnFormatChanged));

    /// <summary>Standard .NET date/time format string.</summary>
    public string Format
    {
        get => (string)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    public LiveClockTextBlock()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateText();

        Loaded += (_, _) =>
        {
            UpdateText();
            _timer.Start();
        };
        Unloaded += (_, _) => _timer.Stop();
    }

    private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LiveClockTextBlock clock)
        {
            clock.UpdateText();
        }
    }

    private void UpdateText() => Text = DateTime.Now.ToString(Format);
}
