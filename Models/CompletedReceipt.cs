using System;
using System.Collections.Generic;

namespace OopsGrocery.Models;

public sealed class ReceiptLineItem
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ItemSubtotal { get; set; }

    public string FormattedUnitPrice => $"₱{UnitPrice:N2}";
    public string FormattedItemSubtotal => $"₱{ItemSubtotal:N2}";
}

public sealed class CompletedReceipt
{
    public string OrderId { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;
    public string StoreBrand { get; set; } = "OOPs OOPs Oops Grocery";
    public string BranchName { get; set; } = "Caloocan Branch";
    public string BranchAddress { get; set; } = "10th Ave, Grace Park East, Caloocan City, Metro Manila";
    public string BranchTel { get; set; } = "Tel: (02) 8294-0012";
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    public string CashierName { get; set; } = "cashier";
    public string RegisterName { get; set; } = "Register #01";
    public string CustomerType { get; set; } = "Regular Customer";

    public List<ReceiptLineItem> Items { get; set; } = [];

    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal NewSubtotal { get; set; }
    public decimal Vat { get; set; }
    public decimal FinalTotal { get; set; }

    public string PaymentMethod { get; set; } = "Cash";
    public decimal CashReceived { get; set; }
    public decimal Change { get; set; }

    public string FormattedTransactionDate => TransactionDate.ToString("MMMM dd, yyyy  hh:mm:ss tt");
    public string FormattedSubtotal => $"₱{Subtotal:N2}";
    public string FormattedDiscount => Discount > 0 ? $"-₱{Discount:N2}" : "₱0.00";
    public string FormattedNewSubtotal => $"₱{NewSubtotal:N2}";
    public string FormattedVat => $"₱{Vat:N2}";
    public string FormattedFinalTotal => $"₱{FinalTotal:N2}";
    public string FormattedCashReceived => $"₱{CashReceived:N2}";
    public string FormattedChange => $"₱{Change:N2}";

    public int TotalItemCount => Items.Sum(i => i.Quantity);
}
