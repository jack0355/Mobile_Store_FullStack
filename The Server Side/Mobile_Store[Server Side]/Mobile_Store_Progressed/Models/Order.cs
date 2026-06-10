namespace Mobile_Store_Progressed.Models
{
    public class Order
    {
        public int OrderID { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerAddress { get; set; }

        public DateTime OrderDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; }
    }
}
