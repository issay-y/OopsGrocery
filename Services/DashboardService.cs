using System;
using System.Collections.Generic;
using System.Linq;
using MySqlConnector;
using OopsGrocery.Models;

namespace OopsGrocery.Services;

public sealed class DashboardData
{
    // Top Row: 4 Boxes
    public decimal YesterdaySales { get; set; }
    public string YesterdaySalesFormatted => $"₱{YesterdaySales:N2}";
    public string YesterdaySubtext { get; set; } = "No sales recorded";

    public decimal TodaySales { get; set; }
    public string TodaySalesFormatted => $"₱{TodaySales:N2}";
    public string TodaySubtext { get; set; } = "No sales recorded";

    public int TodayTransactionsCount { get; set; }
    public string TodayTransactionsFormatted => $"{TodayTransactionsCount} Orders";
    public decimal TodayAverageBasket { get; set; }
    public string TodayBasketSubtext { get; set; } = "₱0.00 avg basket size";

    public int LowStockProductsCount { get; set; }
    public int LowStockOnlyCount { get; set; }
    public int OutOfStockCount { get; set; }
    public string LowStockSubtext { get; set; } = "0 low · 0 out of stock";

    // Row 1 Graphs
    public List<DailySalesPoint> DailySales { get; set; } = [];
    public List<CategoryDonutSegment> CategorySegments { get; set; } = [];
    public int TotalCategoryItemsCount { get; set; }

    // Row 2 Graphs
    public List<BestSellerBarData> BestSellers { get; set; } = [];
    public List<LowestStockItemData> LowestStockItems { get; set; } = [];
}

public sealed class DailySalesPoint
{
    public DateTime Date { get; set; }
    public string DayLabel { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int OrdersCount { get; set; }
    public bool IsToday { get; set; }
}

public sealed class CategoryDonutSegment
{
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public string FormattedPercentage => $"{Percentage:F1}%";
    public string ColorHex { get; set; } = "#1B4D2B";
    public string Icon { get; set; } = "📦";
}

public sealed class BestSellerBarData
{
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public sealed class LowestStockItemData
{
    public ProductItem Product { get; set; } = new();
    public int StockQuantity { get; set; }
    public int MinimumStock { get; set; } = 20;
    public string Unit { get; set; } = "pc";
    public string DisplayText { get; set; } = string.Empty;
    public double StockRatio { get; set; }
    public bool IsOutOfStock { get; set; }
    public string StatusText { get; set; } = "Low Stock";
    public string StatusBgHex => IsOutOfStock ? "#FEE2E2" : "#FEF3C7";
    public string StatusFgHex => IsOutOfStock ? "#991B1B" : "#92400E";
    public string BarColorHex => IsOutOfStock ? "#EF4444" : "#F59E0B";
}

public static class DashboardService
{
    private static string ConnectionString => DatabaseInitializer.ResolveConnectionString();

    public static DashboardData GetDashboardData(out string? errorMessage)
    {
        errorMessage = null;
        var data = new DashboardData();

        try
        {
            DatabaseInitializer.EnsureDatabaseInitialized(ConnectionString);

            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1).AddTicks(-1);
            var yesterdayStart = todayStart.AddDays(-1);
            var yesterdayEnd = todayStart.AddTicks(-1);
            var dayBeforeStart = todayStart.AddDays(-2);
            var dayBeforeEnd = yesterdayStart.AddTicks(-1);
            var last7DaysStart = todayStart.AddDays(-6);

            // 1. TOP BOX 1: Yesterday's Sales & Day Before
            decimal yesterdaySales = 0m;
            using (var cmdYesterday = conn.CreateCommand())
            {
                cmdYesterday.CommandText = """
                    SELECT COALESCE(SUM(`TotalAmount`), 0.00)
                    FROM `transactions`
                    WHERE `CreatedAt` >= @start AND `CreatedAt` <= @end;
                    """;
                cmdYesterday.Parameters.AddWithValue("@start", yesterdayStart);
                cmdYesterday.Parameters.AddWithValue("@end", yesterdayEnd);
                yesterdaySales = Convert.ToDecimal(cmdYesterday.ExecuteScalar() ?? 0m);
            }
            data.YesterdaySales = yesterdaySales;

            decimal dayBeforeSales = 0m;
            using (var cmdDayBefore = conn.CreateCommand())
            {
                cmdDayBefore.CommandText = """
                    SELECT COALESCE(SUM(`TotalAmount`), 0.00)
                    FROM `transactions`
                    WHERE `CreatedAt` >= @start AND `CreatedAt` <= @end;
                    """;
                cmdDayBefore.Parameters.AddWithValue("@start", dayBeforeStart);
                cmdDayBefore.Parameters.AddWithValue("@end", dayBeforeEnd);
                dayBeforeSales = Convert.ToDecimal(cmdDayBefore.ExecuteScalar() ?? 0m);
            }

            if (yesterdaySales <= 0)
            {
                data.YesterdaySubtext = "No sales recorded";
            }
            else if (dayBeforeSales > 0)
            {
                var pct = ((yesterdaySales - dayBeforeSales) / dayBeforeSales) * 100m;
                var arrow = pct >= 0 ? "▲" : "▼";
                data.YesterdaySubtext = $"{arrow} {Math.Abs(pct):F1}% vs day before";
            }
            else
            {
                data.YesterdaySubtext = "Compared to the day before";
            }

            // 2. TOP BOX 2 & 3: Today's Sales & Transactions
            int todayOrders = 0;
            decimal todaySales = 0m;
            decimal todayAvgBasket = 0m;

            using (var cmdToday = conn.CreateCommand())
            {
                cmdToday.CommandText = """
                    SELECT 
                        COUNT(*),
                        COALESCE(SUM(`TotalAmount`), 0.00),
                        COALESCE(AVG(`TotalAmount`), 0.00)
                    FROM `transactions`
                    WHERE `CreatedAt` >= @start AND `CreatedAt` <= @end;
                    """;
                cmdToday.Parameters.AddWithValue("@start", todayStart);
                cmdToday.Parameters.AddWithValue("@end", todayEnd);

                using var reader = cmdToday.ExecuteReader();
                if (reader.Read())
                {
                    todayOrders = reader.GetInt32(0);
                    todaySales = reader.GetDecimal(1);
                    todayAvgBasket = reader.GetDecimal(2);
                }
            }

            data.TodaySales = todaySales;
            data.TodayTransactionsCount = todayOrders;
            data.TodayAverageBasket = todayAvgBasket;

            if (todaySales <= 0)
            {
                data.TodaySubtext = "No sales recorded";
            }
            else if (yesterdaySales > 0)
            {
                var pct = ((todaySales - yesterdaySales) / yesterdaySales) * 100m;
                var arrow = pct >= 0 ? "▲" : "▼";
                data.TodaySubtext = $"{arrow} {Math.Abs(pct):F1}% vs yesterday";
            }
            else
            {
                data.TodaySubtext = "▲ 100% vs yesterday";
            }

            data.TodayBasketSubtext = todayOrders > 0
                ? $"Avg. basket: ₱{todayAvgBasket:N2}"
                : "₱0.00 avg basket size";

            // 3. TOP BOX 4: Low Stock Products (StockQuantity <= MinimumStock, min = 20)
            using (var cmdLow = conn.CreateCommand())
            {
                cmdLow.CommandText = """
                    SELECT 
                        COUNT(*),
                        COALESCE(SUM(CASE WHEN `StockQuantity` > 0 THEN 1 ELSE 0 END), 0),
                        COALESCE(SUM(CASE WHEN `StockQuantity` <= 0 THEN 1 ELSE 0 END), 0)
                    FROM `Products`
                    WHERE `IsArchived` = 0 AND `StockQuantity` <= IFNULL(`MinimumStock`, 20);
                    """;

                using var reader = cmdLow.ExecuteReader();
                if (reader.Read())
                {
                    data.LowStockProductsCount = reader.GetInt32(0);
                    data.LowStockOnlyCount = Convert.ToInt32(reader.GetValue(1));
                    data.OutOfStockCount = Convert.ToInt32(reader.GetValue(2));
                }
            }
            data.LowStockSubtext = $"{data.LowStockOnlyCount} low · {data.OutOfStockCount} out of stock";

            // 4. GRAPH A: LINE CHART (Daily Gross Revenue over last 7 days)
            var dailyMap = new Dictionary<DateTime, (decimal Amount, int Count)>();
            using (var cmd7Days = conn.CreateCommand())
            {
                cmd7Days.CommandText = """
                    SELECT DATE(`CreatedAt`) as Dt, COALESCE(SUM(`TotalAmount`), 0.00), COUNT(*)
                    FROM `transactions`
                    WHERE `CreatedAt` >= @start AND `CreatedAt` <= @end
                    GROUP BY Dt;
                    """;
                cmd7Days.Parameters.AddWithValue("@start", last7DaysStart);
                cmd7Days.Parameters.AddWithValue("@end", todayEnd);

                using var reader = cmd7Days.ExecuteReader();
                while (reader.Read())
                {
                    var dt = reader.GetDateTime(0).Date;
                    var amt = reader.GetDecimal(1);
                    var cnt = reader.GetInt32(2);
                    dailyMap[dt] = (amt, cnt);
                }
            }

            for (int i = 6; i >= 0; i--)
            {
                var dt = todayStart.AddDays(-i);
                decimal amt = dailyMap.TryGetValue(dt, out var val) ? val.Amount : 0m;
                int cnt = dailyMap.TryGetValue(dt, out val) ? val.Count : 0;

                data.DailySales.Add(new DailySalesPoint
                {
                    Date = dt,
                    DayLabel = dt.ToString("ddd"), // Mon, Tue...
                    DateText = dt.ToString("MMM d"), // Oct 25
                    Amount = amt,
                    OrdersCount = cnt,
                    IsToday = (i == 0)
                });
            }

            // 5. GRAPH B: DONUT CHART (Items by category from inventory)
            var categoryRaw = new List<(string Name, int Count)>();
            using (var cmdCat = conn.CreateCommand())
            {
                cmdCat.CommandText = """
                    SELECT `Category`, COUNT(*) as Cnt
                    FROM `Products`
                    WHERE `IsArchived` = 0
                    GROUP BY `Category`
                    ORDER BY Cnt DESC;
                    """;

                using var reader = cmdCat.ExecuteReader();
                while (reader.Read())
                {
                    categoryRaw.Add((reader.GetString(0), reader.GetInt32(1)));
                }
            }

            int totalCatItems = categoryRaw.Sum(c => c.Count);
            data.TotalCategoryItemsCount = totalCatItems;

            var sliceColors = new[]
            {
                "#1B4D2B", // Forest Green
                "#2563EB", // Royal Blue
                "#F59E0B", // Amber
                "#E11D48", // Rose Red
                "#10B981", // Emerald
                "#8B5CF6", // Purple
                "#64748B"  // Slate / Others
            };

            if (categoryRaw.Count <= 7)
            {
                int colorIdx = 0;
                foreach (var c in categoryRaw)
                {
                    double pct = totalCatItems > 0 ? (c.Count / (double)totalCatItems) * 100.0 : 0.0;
                    data.CategorySegments.Add(new CategoryDonutSegment
                    {
                        CategoryName = c.Name,
                        Count = c.Count,
                        Percentage = Math.Round(pct, 1),
                        ColorHex = sliceColors[colorIdx % sliceColors.Length],
                        Icon = ProductItem.GetCategoryIcon(c.Name)
                    });
                    colorIdx++;
                }
            }
            else
            {
                // Top 6 + Others
                for (int i = 0; i < 6; i++)
                {
                    var c = categoryRaw[i];
                    double pct = totalCatItems > 0 ? (c.Count / (double)totalCatItems) * 100.0 : 0.0;
                    data.CategorySegments.Add(new CategoryDonutSegment
                    {
                        CategoryName = c.Name,
                        Count = c.Count,
                        Percentage = Math.Round(pct, 1),
                        ColorHex = sliceColors[i],
                        Icon = ProductItem.GetCategoryIcon(c.Name)
                    });
                }

                int othersCount = categoryRaw.Skip(6).Sum(c => c.Count);
                double othersPct = totalCatItems > 0 ? (othersCount / (double)totalCatItems) * 100.0 : 0.0;
                data.CategorySegments.Add(new CategoryDonutSegment
                {
                    CategoryName = "Others",
                    Count = othersCount,
                    Percentage = Math.Round(othersPct, 1),
                    ColorHex = sliceColors[6],
                    Icon = "📦"
                });
            }

            // 6. GRAPH C: BAR CHART (Best Selling Items in last 7 days)
            using (var cmdBest = conn.CreateCommand())
            {
                cmdBest.CommandText = """
                    SELECT 
                        ti.`ProductName`,
                        ti.`Category`,
                        COALESCE(SUM(ti.`Quantity`), 0) as UnitsSold,
                        COALESCE(SUM(ti.`TotalPrice`), 0.00) as TotalRevenue
                    FROM `transaction_items` ti
                    JOIN `transactions` t ON ti.`TransactionId` = t.`Id`
                    WHERE t.`CreatedAt` >= @start AND t.`CreatedAt` <= @end
                    GROUP BY ti.`ProductName`, ti.`Category`
                    ORDER BY UnitsSold DESC
                    LIMIT 5;
                    """;
                cmdBest.Parameters.AddWithValue("@start", last7DaysStart);
                cmdBest.Parameters.AddWithValue("@end", todayEnd);

                using var reader = cmdBest.ExecuteReader();
                while (reader.Read())
                {
                    data.BestSellers.Add(new BestSellerBarData
                    {
                        ProductName = reader.GetString(0),
                        Category = reader.GetString(1),
                        UnitsSold = Convert.ToInt32(reader.GetValue(2)),
                        TotalRevenue = reader.GetDecimal(3)
                    });
                }
            }

            // 7. GRAPH D: HORIZONTAL BARS (Lowest-Stock Items closest to running out)
            using (var cmdLowItems = conn.CreateCommand())
            {
                cmdLowItems.CommandText = """
                    SELECT `Id`, `Sku`, `Name`, `Category`, `Price`, `Unit`, `StockQuantity`, `MinimumStock`, `IsArchived`, `Tag`, `ImagePath`
                    FROM `Products`
                    WHERE `IsArchived` = 0
                    ORDER BY `StockQuantity` ASC, `MinimumStock` ASC
                    LIMIT 5;
                    """;

                using var reader = cmdLowItems.ExecuteReader();
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var sku = reader.IsDBNull(1) ? $"PRD-{id}" : reader.GetString(1);
                    var name = reader.GetString(2);
                    var cat = reader.GetString(3);
                    var price = reader.GetDecimal(4);
                    var unit = reader.IsDBNull(5) ? "pc" : reader.GetString(5);
                    var sq = reader.GetInt32(6);
                    var ms = reader.IsDBNull(7) ? 20 : reader.GetInt32(7);
                    var isArch = !reader.IsDBNull(8) && reader.GetBoolean(8);
                    var tag = reader.IsDBNull(9) ? null : reader.GetString(9);
                    var img = reader.IsDBNull(10) ? null : reader.GetString(10);

                    var item = new ProductItem
                    {
                        Id = id,
                        Sku = sku,
                        Name = name,
                        Category = cat,
                        Price = price,
                        Unit = unit,
                        StockQuantity = sq,
                        MinimumStock = ms,
                        IsArchived = isArch,
                        Tag = tag ?? (sq <= 0 ? "Out of Stock" : (sq <= ms ? "Low Stock" : "In Stock")),
                        ImagePath = img
                    };

                    double ratio = ms > 0 ? Math.Clamp(sq / (double)ms, 0.0, 1.0) : 0.0;

                    data.LowestStockItems.Add(new LowestStockItemData
                    {
                        Product = item,
                        StockQuantity = sq,
                        MinimumStock = ms,
                        Unit = unit,
                        DisplayText = $"{sq} / {ms} {unit}",
                        StockRatio = ratio,
                        IsOutOfStock = sq <= 0,
                        StatusText = sq <= 0 ? "Out of Stock" : (sq <= ms ? "Low Stock" : "In Stock")
                    });
                }
            }

            return data;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return data;
        }
    }
}
