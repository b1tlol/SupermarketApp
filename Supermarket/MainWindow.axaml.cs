using System;
using System.Data;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Microsoft.Data.SqlClient;

namespace SupermarketApp;

public partial class MainWindow : Window
{
    private readonly string _cs =
        "Server=localhost,1433;Database=SupermarketDB;" +
        "User Id=sa;Password=YourPass123!;" +
        "TrustServerCertificate=True;Encrypt=false;";

    public MainWindow()
    {
        InitializeComponent();

        // Всі товари
        btnAll.Click += (_, _) =>
            Run("SELECT * FROM Products");

        // Топ 10 за ціною
        btnSort.Click += (_, _) =>
            Run("SELECT TOP 10 * FROM Products ORDER BY price DESC");

        // JOIN Products + Suppliers
        btnJoin.Click += (_, _) =>
            Run("SELECT Products.name AS ProductName, Suppliers.name AS SupplierName " +
                "FROM Products " +
                "JOIN Suppliers ON Products.supplier_id = Suppliers.id");

        // JOIN Products + Characteristics
        btnJoinChar.Click += (_, _) =>
            Run("SELECT p.name AS Product, c.name AS Characteristic, c.value " +
                "FROM Products p " +
                "JOIN Product_Characteristics c ON p.id = c.product_id");

        // Агрегати AVG MAX MIN
        btnAggregates.Click += (_, _) =>
            Run("SELECT AVG(price) AS AvgPrice, MAX(price) AS MaxPrice, MIN(price) AS MinPrice FROM Products");

        // GROUP BY
        btnGroup.Click += (_, _) =>
            Run("SELECT supplier_id, COUNT(*) AS ProductCount FROM Products GROUP BY supplier_id");

        // Фільтр BETWEEN
        btnFilter.Click += Filter_Click;

        Opened += (_, _) => Run("SELECT * FROM Products");
    }

    private void Run(string sql)
    {
        try
        {
            using var con     = new SqlConnection(_cs);
            con.Open();
            using var cmd     = new SqlCommand(sql, con);
            using var adapter = new SqlDataAdapter(cmd);
            var table = new DataTable();
            adapter.Fill(table);
            BuildGrid(table);
            lblStatus.Text = $"Рядків: {table.Rows.Count}";
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Помилка: {ex.Message}";
        }
    }

    private void BuildGrid(DataTable table)
    {
        gridData.Children.Clear();
        gridData.ColumnDefinitions.Clear();
        gridData.RowDefinitions.Clear();

        int cols = table.Columns.Count;
        int rows = table.Rows.Count;

        for (int c = 0; c < cols; c++)
            gridData.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });

        gridData.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (int r = 0; r < rows; r++)
            gridData.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header
        for (int c = 0; c < cols; c++)
        {
            var cell = MakeCell(table.Columns[c].ColumnName, true, 0, c);
            gridData.Children.Add(cell);
        }

        // Data
        for (int r = 0; r < rows; r++)
        {
            bool even = r % 2 == 0;
            for (int c = 0; c < cols; c++)
            {
                var cell = MakeCell(table.Rows[r][c]?.ToString() ?? "", false, r + 1, c, even);
                gridData.Children.Add(cell);
            }
        }
    }

    private static Border MakeCell(string text, bool isHeader, int row, int col, bool even = true)
    {
        var border = new Border
        {
            Background = isHeader
                ? new SolidColorBrush(Color.Parse("#4A90D9"))
                : new SolidColorBrush(even ? Color.Parse("#FFFFFF") : Color.Parse("#F0F5FF")),
            BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC")),
            BorderThickness = new Avalonia.Thickness(0, 0, 1, 1),
            Padding = new Avalonia.Thickness(8, 5)
        };
        border.Child = new TextBlock
        {
            Text       = text,
            Foreground = isHeader ? Brushes.White : Brushes.Black,
            FontWeight = isHeader ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal
        };
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        return border;
    }

    private void Filter_Click(object? sender, RoutedEventArgs e)
    {
        var style = NumberStyles.Number | NumberStyles.AllowDecimalPoint;
        if (!decimal.TryParse(txtMin.Text?.Trim(), style, CultureInfo.InvariantCulture, out decimal min))
        { lblStatus.Text = "Невірне значення min (приклад: 10.00)"; return; }
        if (!decimal.TryParse(txtMax.Text?.Trim(), style, CultureInfo.InvariantCulture, out decimal max))
        { lblStatus.Text = "Невірне значення max (приклад: 99.99)"; return; }
        if (min > max)
        { lblStatus.Text = "min має бути <= max"; return; }

        try
        {
            using var con = new SqlConnection(_cs);
            con.Open();
            using var cmd = new SqlCommand(
                "SELECT * FROM Products WHERE price BETWEEN @min AND @max", con);
            cmd.Parameters.AddWithValue("@min", min);
            cmd.Parameters.AddWithValue("@max", max);
            using var adapter = new SqlDataAdapter(cmd);
            var table = new DataTable();
            adapter.Fill(table);
            BuildGrid(table);
            lblStatus.Text = $"Рядків: {table.Rows.Count}";
        }
        catch (Exception ex) { lblStatus.Text = $"Помилка: {ex.Message}"; }
    }
}
