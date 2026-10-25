using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using OopsGrocery.Services;
using OopsGrocery.ViewModels;

namespace OopsGrocery.Views;

public partial class AdminView : UserControl
{
    private AdminViewModel? _subscribedVm;

    public AdminView()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;

        LineChartCanvas.SizeChanged += (s, e) => DrawLineChart();
        DonutChartCanvas.SizeChanged += (s, e) => DrawDonutChart();
        BarChartCanvas.SizeChanged += (s, e) => DrawBarChart();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SubscribeViewModel(DataContext as AdminViewModel);
        RedrawAllCharts();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeViewModel();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        SubscribeViewModel(e.NewValue as AdminViewModel);
        RedrawAllCharts();
    }

    private void SubscribeViewModel(AdminViewModel? vm)
    {
        if (_subscribedVm == vm) return;

        UnsubscribeViewModel();
        _subscribedVm = vm;

        if (_subscribedVm != null)
        {
            _subscribedVm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void UnsubscribeViewModel()
    {
        if (_subscribedVm != null)
        {
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
            _subscribedVm = null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.InvokeAsync(RedrawAllCharts);
    }

    public void RedrawAllCharts()
    {
        DrawLineChart();
        DrawDonutChart();
        DrawBarChart();
    }

    // =========================================================================
    // a) LINE CHART - Sales over the last 7 days (Daily gross revenue)
    // =========================================================================
    private void DrawLineChart()
    {
        LineChartCanvas.Children.Clear();

        double width = LineChartCanvas.ActualWidth;
        double height = LineChartCanvas.ActualHeight;

        if (width <= 40 || height <= 40) return;

        if (DataContext is not AdminViewModel vm || vm.DailySales.Count == 0)
        {
            var emptyText = new TextBlock
            {
                Text = "No sales recorded in the last 7 days",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                FontStyle = FontStyles.Italic,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Canvas.SetLeft(emptyText, Math.Max(0, (width - 240) / 2));
            Canvas.SetTop(emptyText, Math.Max(0, (height - 20) / 2));
            LineChartCanvas.Children.Add(emptyText);
            return;
        }

        var points = vm.DailySales.ToList();

        const double marginLeft = 58.0;
        const double marginRight = 22.0;
        const double marginTop = 18.0;
        const double marginBottom = 34.0;

        double plotW = width - marginLeft - marginRight;
        double plotH = height - marginTop - marginBottom;

        if (plotW <= 10 || plotH <= 10) return;

        // Calculate clean rounded ceiling for Y axis
        double maxSales = (double)points.Max(p => p.Amount);
        if (maxSales <= 0) maxSales = 1000.0;

        double roughStep = maxSales / 4.0;
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
        if (magnitude < 1) magnitude = 1;

        double normalized = roughStep / magnitude;
        double step;
        if (normalized <= 1.5) step = 1.0 * magnitude;
        else if (normalized <= 3.0) step = 2.0 * magnitude;
        else if (normalized <= 7.0) step = 5.0 * magnitude;
        else step = 10.0 * magnitude;

        int gridSteps = (int)Math.Ceiling(maxSales / step);
        if (gridSteps < 4) gridSteps = 4;
        double niceMax = step * gridSteps;

        // 1. Draw Gridlines & Y-Axis Labels
        for (int i = 0; i <= gridSteps; i++)
        {
            double val = i * step;
            double y = marginTop + plotH - (val / niceMax) * plotH;

            var gridLine = new Line
            {
                X1 = marginLeft,
                Y1 = y,
                X2 = width - marginRight,
                Y2 = y,
                Stroke = new SolidColorBrush(Color.FromRgb(0xEE, 0xF2, 0xF6)),
                StrokeThickness = 1
            };

            if (i > 0)
            {
                gridLine.StrokeDashArray = [3, 3];
            }

            LineChartCanvas.Children.Add(gridLine);

            string yText = FormatYAxisCurrency(val);
            var yLabel = new TextBlock
            {
                Text = yText,
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                TextAlignment = TextAlignment.Right,
                Width = marginLeft - 8
            };
            Canvas.SetLeft(yLabel, 0);
            Canvas.SetTop(yLabel, y - 7);
            LineChartCanvas.Children.Add(yLabel);
        }

        // 2. Map coordinates
        var coords = new List<Point>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            double px = points.Count == 1
                ? marginLeft + plotW / 2.0
                : marginLeft + (i * (plotW / (points.Count - 1)));

            double py = marginTop + plotH - ((double)points[i].Amount / niceMax) * plotH;
            coords.Add(new Point(px, py));
        }

        // 3. Gradient Area Fill under the line
        var areaFigure = new PathFigure
        {
            StartPoint = new Point(coords[0].X, marginTop + plotH),
            IsClosed = true,
            IsFilled = true
        };
        areaFigure.Segments.Add(new LineSegment(coords[0], false));
        for (int i = 1; i < coords.Count; i++)
        {
            areaFigure.Segments.Add(new LineSegment(coords[i], true));
        }
        areaFigure.Segments.Add(new LineSegment(new Point(coords[^1].X, marginTop + plotH), false));

        var areaGeometry = new PathGeometry();
        areaGeometry.Figures.Add(areaFigure);

        var areaGradient = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        areaGradient.GradientStops.Add(new GradientStop(Color.FromArgb(90, 0x1B, 0x4D, 0x2B), 0.0));
        areaGradient.GradientStops.Add(new GradientStop(Color.FromArgb(25, 0x1B, 0x4D, 0x2B), 0.7));
        areaGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0x1B, 0x4D, 0x2B), 1.0));

        var areaPath = new Path
        {
            Data = areaGeometry,
            Fill = areaGradient,
            IsHitTestVisible = false
        };
        LineChartCanvas.Children.Add(areaPath);

        // 4. Line stroke
        var lineFigure = new PathFigure
        {
            StartPoint = coords[0],
            IsClosed = false,
            IsFilled = false
        };
        for (int i = 1; i < coords.Count; i++)
        {
            lineFigure.Segments.Add(new LineSegment(coords[i], true));
        }

        var lineGeometry = new PathGeometry();
        lineGeometry.Figures.Add(lineFigure);

        var linePath = new Path
        {
            Data = lineGeometry,
            Stroke = new SolidColorBrush(Color.FromRgb(0x1B, 0x4D, 0x2B)),
            StrokeThickness = 2.5,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            IsHitTestVisible = false
        };
        LineChartCanvas.Children.Add(linePath);

        // 5. X-axis Labels & Data Dots with Tooltips
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            var pt = coords[i];

            // X Day Label
            string dayText = string.IsNullOrWhiteSpace(p.DayLabel) ? p.Date.ToString("ddd") : p.DayLabel;
            var xLabel = new TextBlock
            {
                Text = dayText,
                FontSize = 11,
                Foreground = p.IsToday
                    ? new SolidColorBrush(Color.FromRgb(0x1B, 0x4D, 0x2B))
                    : new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                FontWeight = p.IsToday ? FontWeights.Bold : FontWeights.Normal,
                TextAlignment = TextAlignment.Center,
                Width = 46
            };
            Canvas.SetLeft(xLabel, pt.X - 23);
            Canvas.SetTop(xLabel, marginTop + plotH + 8);
            LineChartCanvas.Children.Add(xLabel);

            // Rich Tooltip
            var tooltipCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"{dayText} • {p.DateText}",
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White,
                            FontSize = 11.5
                        },
                        new TextBlock
                        {
                            Text = $"Gross Sales: ₱{p.Amount:N2}",
                            Foreground = new SolidColorBrush(Color.FromRgb(0x86, 0xEF, 0xAC)),
                            FontWeight = FontWeights.SemiBold,
                            FontSize = 12,
                            Margin = new Thickness(0, 3, 0, 1)
                        },
                        new TextBlock
                        {
                            Text = $"{p.OrdersCount} completed orders",
                            Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                            FontSize = 11
                        }
                    }
                }
            };

            // Dot - Today is highlighted and filled
            if (p.IsToday)
            {
                var halo = new Ellipse
                {
                    Width = 16,
                    Height = 16,
                    Fill = new SolidColorBrush(Color.FromArgb(130, 0xDC, 0xFC, 0xE7)),
                    Stroke = new SolidColorBrush(Color.FromRgb(0x1B, 0x4D, 0x2B)),
                    StrokeThickness = 1.5,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(halo, pt.X - 8);
                Canvas.SetTop(halo, pt.Y - 8);
                LineChartCanvas.Children.Add(halo);

                var filledDot = new Ellipse
                {
                    Width = 9,
                    Height = 9,
                    Fill = new SolidColorBrush(Color.FromRgb(0x1B, 0x4D, 0x2B)),
                    Cursor = Cursors.Hand
                };
                Canvas.SetLeft(filledDot, pt.X - 4.5);
                Canvas.SetTop(filledDot, pt.Y - 4.5);
                ToolTipService.SetToolTip(filledDot, tooltipCard);

                filledDot.MouseEnter += (s, e) => halo.Width = halo.Height = 20;
                filledDot.MouseLeave += (s, e) => halo.Width = halo.Height = 16;

                LineChartCanvas.Children.Add(filledDot);
            }
            else
            {
                var regularDot = new Ellipse
                {
                    Width = 9,
                    Height = 9,
                    Fill = Brushes.White,
                    Stroke = new SolidColorBrush(Color.FromRgb(0x1B, 0x4D, 0x2B)),
                    StrokeThickness = 2,
                    Cursor = Cursors.Hand
                };
                Canvas.SetLeft(regularDot, pt.X - 4.5);
                Canvas.SetTop(regularDot, pt.Y - 4.5);
                ToolTipService.SetToolTip(regularDot, tooltipCard);

                regularDot.MouseEnter += (s, e) => regularDot.Fill = new SolidColorBrush(Color.FromRgb(0xDC, 0xFC, 0xE7));
                regularDot.MouseLeave += (s, e) => regularDot.Fill = Brushes.White;

                LineChartCanvas.Children.Add(regularDot);
            }
        }
    }

    private static string FormatYAxisCurrency(double val)
    {
        if (val >= 1_000_000)
            return $"₱{(val / 1_000_000):0.#}M";
        if (val >= 1_000)
            return $"₱{(val / 1_000):0.#}k";
        return $"₱{val:0}";
    }

    // =========================================================================
    // b) DONUT CHART - Items by category (Top 6 + Others)
    // =========================================================================
    private void DrawDonutChart()
    {
        DonutChartCanvas.Children.Clear();

        double width = DonutChartCanvas.ActualWidth;
        double height = DonutChartCanvas.ActualHeight;

        if (width <= 20 || height <= 20) return;

        if (DataContext is not AdminViewModel vm || vm.CategorySegments.Count == 0)
        {
            return;
        }

        var segments = vm.CategorySegments.ToList();
        int total = segments.Sum(s => s.Count);

        double cx = width / 2.0;
        double cy = height / 2.0;
        double outerR = Math.Min(cx, cy) - 8.0;

        if (outerR < 25) return;
        double innerR = outerR * 0.62;

        if (total <= 0)
        {
            var emptyRing = new Ellipse
            {
                Width = outerR * 2,
                Height = outerR * 2,
                Stroke = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
                StrokeThickness = outerR - innerR,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(emptyRing, cx - outerR);
            Canvas.SetTop(emptyRing, cy - outerR);
            DonutChartCanvas.Children.Add(emptyRing);
            return;
        }

        double currentAngle = -90.0;

        foreach (var seg in segments)
        {
            if (seg.Count <= 0) continue;

            double sweepAngle = (seg.Count / (double)total) * 360.0;
            if (sweepAngle <= 0.05) continue;

            Color sliceColor = Color.FromRgb(0x1B, 0x4D, 0x2B);
            try
            {
                sliceColor = (Color)ColorConverter.ConvertFromString(seg.ColorHex);
            }
            catch { }

            // Tooltip card
            var tooltipCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 7, 10, 7),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"{seg.Icon} {seg.CategoryName}",
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White,
                            FontSize = 12
                        },
                        new TextBlock
                        {
                            Text = $"Count: {seg.Count:N0} items ({seg.FormattedPercentage})",
                            Foreground = new SolidColorBrush(Color.FromRgb(0x86, 0xEF, 0xAC)),
                            FontSize = 11,
                            Margin = new Thickness(0, 2, 0, 0)
                        }
                    }
                }
            };

            // Full ring if single segment 100%
            if (sweepAngle >= 359.9)
            {
                var fullRing = new Ellipse
                {
                    Width = outerR * 2,
                    Height = outerR * 2,
                    Stroke = new SolidColorBrush(sliceColor),
                    StrokeThickness = outerR - innerR,
                    Fill = Brushes.Transparent,
                    Cursor = Cursors.Hand
                };
                Canvas.SetLeft(fullRing, cx - outerR);
                Canvas.SetTop(fullRing, cy - outerR);
                ToolTipService.SetToolTip(fullRing, tooltipCard);
                DonutChartCanvas.Children.Add(fullRing);
                break;
            }

            double startRad = currentAngle * Math.PI / 180.0;
            double endRad = (currentAngle + sweepAngle) * Math.PI / 180.0;

            Point p1 = new(cx + outerR * Math.Cos(startRad), cy + outerR * Math.Sin(startRad));
            Point p2 = new(cx + outerR * Math.Cos(endRad), cy + outerR * Math.Sin(endRad));
            Point p3 = new(cx + innerR * Math.Cos(endRad), cy + innerR * Math.Sin(endRad));
            Point p4 = new(cx + innerR * Math.Cos(startRad), cy + innerR * Math.Sin(startRad));

            bool isLargeArc = sweepAngle > 180.0;

            var figure = new PathFigure
            {
                StartPoint = p1,
                IsClosed = true,
                IsFilled = true
            };
            figure.Segments.Add(new ArcSegment(p2, new Size(outerR, outerR), 0, isLargeArc, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(p3, true));
            figure.Segments.Add(new ArcSegment(p4, new Size(innerR, innerR), 0, isLargeArc, SweepDirection.Counterclockwise, true));

            var geom = new PathGeometry();
            geom.Figures.Add(figure);

            var slicePath = new Path
            {
                Data = geom,
                Fill = new SolidColorBrush(sliceColor),
                Stroke = Brushes.White,
                StrokeThickness = 2.0,
                Cursor = Cursors.Hand
            };

            ToolTipService.SetToolTip(slicePath, tooltipCard);

            slicePath.MouseEnter += (s, e) => slicePath.Opacity = 0.82;
            slicePath.MouseLeave += (s, e) => slicePath.Opacity = 1.0;

            DonutChartCanvas.Children.Add(slicePath);
            currentAngle += sweepAngle;
        }
    }

    // =========================================================================
    // c) BAR CHART - Best selling items (Top 5 in last 7 days)
    // =========================================================================
    private void DrawBarChart()
    {
        BarChartCanvas.Children.Clear();

        double width = BarChartCanvas.ActualWidth;
        double height = BarChartCanvas.ActualHeight;

        if (width <= 30 || height <= 30) return;

        if (DataContext is not AdminViewModel vm || vm.BestSellers.Count == 0)
        {
            return;
        }

        var items = vm.BestSellers.ToList();

        const double marginLeft = 20.0;
        const double marginRight = 20.0;
        const double marginTop = 26.0;
        const double marginBottom = 48.0;

        double plotW = width - marginLeft - marginRight;
        double plotH = height - marginTop - marginBottom;

        if (plotW <= 10 || plotH <= 10) return;

        double baseY = marginTop + plotH;

        // Baseline line
        var baseline = new Line
        {
            X1 = marginLeft - 5,
            Y1 = baseY,
            X2 = width - marginRight + 5,
            Y2 = baseY,
            Stroke = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
            StrokeThickness = 1
        };
        BarChartCanvas.Children.Add(baseline);

        int maxUnits = items.Max(x => x.UnitsSold);
        if (maxUnits <= 0) maxUnits = 1;

        int count = items.Count;
        double slotW = plotW / count;
        double barW = Math.Clamp(slotW * 0.48, 22, 50);

        for (int i = 0; i < count; i++)
        {
            var item = items[i];
            double slotCenterX = marginLeft + (i + 0.5) * slotW;

            double barHeight = (item.UnitsSold / (double)maxUnits) * plotH;
            if (barHeight < 5 && item.UnitsSold > 0) barHeight = 5;
            if (barHeight > plotH) barHeight = plotH;

            double barX = slotCenterX - barW / 2.0;
            double barY = baseY - barHeight;

            // Curated harmonious bar colors for rankings 1 through 5
            Color barColor = i switch
            {
                0 => Color.FromRgb(0x1B, 0x4D, 0x2B), // #1 Forest Brand
                1 => Color.FromRgb(0x22, 0x6E, 0x3B), // #2 Green
                2 => Color.FromRgb(0x25, 0x63, 0xEB), // #3 Blue
                3 => Color.FromRgb(0x7C, 0x3A, 0xED), // #4 Purple
                _ => Color.FromRgb(0xB4, 0x53, 0x09)  // #5 Amber
            };

            var bar = new Border
            {
                Width = barW,
                Height = barHeight,
                CornerRadius = new CornerRadius(5, 5, 0, 0),
                Background = new SolidColorBrush(barColor),
                Cursor = Cursors.Hand
            };
            Canvas.SetLeft(bar, barX);
            Canvas.SetTop(bar, barY);

            // Value label on top of bar
            var valLabel = new TextBlock
            {
                Text = $"{item.UnitsSold} sold",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(barColor),
                TextAlignment = TextAlignment.Center,
                Width = slotW
            };
            Canvas.SetLeft(valLabel, slotCenterX - slotW / 2.0);
            Canvas.SetTop(valLabel, Math.Max(2, barY - 20));

            // Truncated item name with ellipsis
            var nameLabel = new TextBlock
            {
                Text = item.ProductName,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width = slotW - 6
            };
            Canvas.SetLeft(nameLabel, slotCenterX - (slotW - 6) / 2.0);
            Canvas.SetTop(nameLabel, baseY + 6);

            // Category subtext
            var catLabel = new TextBlock
            {
                Text = item.Category,
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width = slotW - 6
            };
            Canvas.SetLeft(catLabel, slotCenterX - (slotW - 6) / 2.0);
            Canvas.SetTop(catLabel, baseY + 23);

            // Tooltip with full name and sales metrics
            var tooltipCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"#{i + 1} Best Seller",
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xC9, 0x26))
                        },
                        new TextBlock
                        {
                            Text = item.ProductName,
                            FontSize = 12,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White,
                            Margin = new Thickness(0, 2, 0, 2)
                        },
                        new TextBlock
                        {
                            Text = $"Category: {item.Category}",
                            FontSize = 11,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8))
                        },
                        new TextBlock
                        {
                            Text = $"Quantity Sold: {item.UnitsSold:N0} units",
                            FontSize = 11.5,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x86, 0xEF, 0xAC)),
                            Margin = new Thickness(0, 3, 0, 0)
                        },
                        new TextBlock
                        {
                            Text = $"Revenue: ₱{item.TotalRevenue:N2}",
                            FontSize = 11,
                            Foreground = Brushes.White
                        }
                    }
                }
            };

            ToolTipService.SetToolTip(bar, tooltipCard);
            ToolTipService.SetToolTip(nameLabel, tooltipCard);

            bar.MouseEnter += (s, e) => bar.Opacity = 0.8;
            bar.MouseLeave += (s, e) => bar.Opacity = 1.0;

            BarChartCanvas.Children.Add(bar);
            BarChartCanvas.Children.Add(valLabel);
            BarChartCanvas.Children.Add(nameLabel);
            BarChartCanvas.Children.Add(catLabel);
        }
    }
}
