using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace OopsGrocery.Converters;

/// <summary>
/// Crash-proof image converter that safely loads local images, pack URIs, or relative paths.
/// Downsamples decoding to thumbnail resolution to prevent OutOfMemory crashes,
/// loads with BitmapCacheOption.OnLoad so it never locks files on disk,
/// calls Freeze() for thread safety, and catches all errors gracefully.
/// </summary>
public sealed class SafeImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return null;

        var path = value.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(path)) return null;

        try
        {
            Uri uri;
            if (path.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
            {
                uri = new Uri(path, UriKind.Absolute);
            }
            else if (Path.IsPathRooted(path))
            {
                if (!File.Exists(path)) return null;
                uri = new Uri(path, UriKind.Absolute);
            }
            else
            {
                // Try relative to AppDomain base directory
                var combined = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                if (File.Exists(combined))
                {
                    uri = new Uri(combined, UriKind.Absolute);
                }
                else
                {
                    uri = new Uri(path, UriKind.RelativeOrAbsolute);
                }
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // Releases disk file lock immediately
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.DecodePixelWidth = 80; // Memory guard: tiny memory footprint prevents OutOfMemory crashes
            bitmap.UriSource = uri;
            bitmap.EndInit();
            bitmap.Freeze(); // Immutable, thread-safe, ultra-fast rendering

            return bitmap;
        }
        catch
        {
            // Fail silently: WPF will display the fallback category illustration
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
