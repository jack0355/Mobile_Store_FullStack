namespace Mobile_Store_Progressed.DTOs.OrderRequest
{
    public class OrderRequest
    {
        public string CustomerName { get; set; } = "";
        public string CusotmerPhone { get; set; } = "";
        public string CustomerAddress { get;  set; } = "";

        public List<OrderItemRequest> Items { get; set; } = new();


        public class OrderItemRequest
        {
            public int ProductId {  get; set; }
            public int Qunatity {  get; set; }
        }

    }
}
