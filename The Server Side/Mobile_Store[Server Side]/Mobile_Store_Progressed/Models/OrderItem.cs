namespace Mobile_Store_Progressed.Models
{
    public class OrderItem
    {
        public int OrderItemID { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }


    }
}
