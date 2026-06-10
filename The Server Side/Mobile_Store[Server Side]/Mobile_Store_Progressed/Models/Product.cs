namespace Mobile_Store_Progressed.Models
{
    public class Product
    {
        //THE MAIN INFORMATION FOR MY PRODUCT : 
        public int Product_Id { get; set; }
        public string Name { get; set; }
        public int BrandId { get; set; }
        public int CategoryId { get; set; }


        //THE STATUS OF MY [PRODUCT] : 
        public bool Is_avaliable { get; set; }
        public decimal Price { get; set; }
        public string Image_URL { get; set; }
        public string description { get; set; }
        public int StockQunatity { get; set; }

    }
}
