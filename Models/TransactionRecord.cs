using System;
using System.Collections.Generic;

namespace OopsGrocery.Models;

public sealed class TransactionRecord
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string CashierName { get; set; } = "cashier";
    public string StationName { get; set; } = "Register #01";
    public int TotalItems { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountTendered { get; set; }
    public decimal ChangeGiven { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string Status { get; set; } = "Completed";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<TransactionItemRecord> Items { get; set; } = [];
}

public sealed class TransactionItemRecord
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int? ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal TotalPrice { get; set; }
}
