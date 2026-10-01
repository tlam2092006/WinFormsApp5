using System;
using System.Drawing;

namespace WinFormsApp5
{
    public class Product
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public Image Avatar { get; set; }
    }
}
