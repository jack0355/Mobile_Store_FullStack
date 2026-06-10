using Microsoft.AspNetCore.Mvc;
using Mobile_Store_Progressed.Data;
using Mobile_Store_Progressed.DTOs.Auth;
using Mobile_Store_Progressed.Models;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;



namespace Mobile_Store_Progressed.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ProductsContorller : ControllerBase
    {
        private readonly DataBaseConnection _db;
        private readonly IConfiguration _config;
        private readonly ILogger<ProductsContorller> _logger;

        public ProductsContorller(DataBaseConnection db , IConfiguration configuration , ILogger<ProductsContorller>Logger)
        {
            _db = db;
            _config = configuration;
            _logger = Logger;

        }


        [HttpGet]
        public async Task<IActionResult>GetAll()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
              SELECT p.product_id , p.name , p.price , p.image_url , p.description , p.is_available , 
                     p.StockQunatity , b.name as BrandName , c.name as CategoryName
              FROM products p
              JOIN brands b ON p.brand_id = b.brand_id
              JOIN categories c ON p.category_id = c.category_id 
              WHERE p.is_available = 1 ", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            var products = new List<Object>();

            while(reader.Read())
            {
                products.Add(new
                {
                    ProductId = (int)reader["product_id"],
                    Name= (string)reader["name"],
                    Price = (decimal)reader["price"],
                    ImageUrl = reader["image_url"] as string,
                    Description = reader["description"]as string,
                    BrandName = (string)reader["BrandName"],
                    CategoryName = (string)reader["CategoryName"],
                    StockQunatity = reader["StockQunatity"] ==  DBNull.Value? 0 : (int)reader["StockQuantity"]

                });
            }
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult>GetById(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();


            var cmd = new SqlCommand(@"
            SELECT   p.product_id , p.name , p.price , p.image_url , p.description , 
            b.name as BrandName , c.name as CategoryName ,
            s.storage , s.ram , s.screen_size , s.battery , s.color , s.os
            FROM products p 
            JOIN brands b ON p.brand_id = b.brand_id
            JOIN categories c ON p.category_id = c.category_id
            LEFT JOIN product_specs s on p.product_id = s.product_id
            WHERE p.product_id = @Id", conn);


            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = await cmd.ExecuteReaderAsync();

            if(!reader.Read())
            {
                return NotFound("Product Not Found");
            }

            return Ok(new
            {
                ProductId = (int)reader["product_id"],
                Name = (string)reader["name"],
                Price = (decimal)reader["price"],
                BrandName = (string)reader["BrandName"],
                CategoryName = (string)reader["CategoryName"],
                Specs = new
                {
                    Storage = reader["storage"] as string,
                    Ram = reader["ram"] as string,
                    ScreenSize = reader["screen_size"] as string,
                    Battery = reader["battery"] as string,
                    Color = reader["color"] as string,
                    OS = reader["os"] as string
                }
            });
        }

        [HttpGet("brand/{brandId}")]
        public async Task<IActionResult>GetByBrand(int brandId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
              SELECT p.product_id ,p.name , p.price ,p.image_url , 
               b.name as BrandName , c.name as CategoryName
              FROM products p 
              JOIN brands b ON p.brand_id = b.brand_id
              JOIN categories c ON p.category_id = c.category_id
              where p.brand_id = @BrandId AND p.is_available = 1", conn);


            cmd.Parameters.AddWithValue("@BrandId", brandId);
            using var reader = await cmd.ExecuteReaderAsync();

            var products = new List<object>();

            while (reader.Read())
            {
                products.Add(new
                {
                    ProductId = (int)reader["product_id"],
                    Name = (string)reader["name"],
                    Price = (decimal)reader["price"],
                    BrandName = (string)reader["BrandName"],
                    CategoryName = (string)reader["CategoryName"]
                });

            }

            return Ok(products);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult>GetByCategory(int categoryId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            SELECT p.product_id , p.name , p.price , p.image_url , b.name as BrandName,
            c.name as CategoryName
            FROM products p 
            JOIN brands b ON p.brand_id = b.brand_id
            JOIN categories c ON p.category_id = c.category_id
            WHERE p.category_id = @CategoryId and p.is_available  = 1 ", conn);


            cmd.Parameters.AddWithValue("@CategoryId" , categoryId);
            using var reader = await cmd.ExecuteReaderAsync();
            var products = new List<object>();
            while (reader.Read())
            {
                products.Add(new
                {
                    ProductId = (int)reader["product_id"],
                    Name = (string)reader["name"],
                    Price = (decimal)reader["price"],
                    ImageUrl = reader["image_url"] as string,
                    BrandName = (string)reader["BrandName"],
                    CategoryName = (string)reader["CategoryName"]
                });
            }
            return Ok(products);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddProduct([FromBody]AddProductRequest request)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
             INSERT INTO products(brand_id , category_id , name , price , image_url , description , is_available , StockQunatity)
             VALUES(@BrandId ,@CategoryId , @Name , @Price ,@ImageUrl , @Description , 1,@Stock)
             SELECT SCOPE_IDENTITY();", conn);

            cmd.Parameters.AddWithValue("@BrandId", request.BrandId);
            cmd.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            cmd.Parameters.AddWithValue("@Name", request.Name);
            cmd.Parameters.AddWithValue("@Price", request.Price);
            cmd.Parameters.AddWithValue("@ImageUrl", request.ImageUrl ?? "");
            cmd.Parameters.AddWithValue("@Description", request.Description ?? "");
            cmd.Parameters.AddWithValue("@Stock", request.StockQuantity);

            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            _logger.LogInformation("Product added :{Name} by admin", request.Name);
            return Ok(new {ProductId = newId , Message  = "Product added Successfully"});

        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult>DeleteProduct(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            //HERE i didn't use the [ DELETE ] in the command 
            // Because we need to  protect old orders , like if user orders this product ---> and it was deleted
            //                               WE'RE GONNA HAVE ERROR FOR THAT 
            var cmd = new SqlCommand(@"UPDATE products SET is_available  = 0  WHERE  product_id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0)
                return NotFound("Product Not Found !");



            _logger.LogInformation("Product deleted : {Id} By admin", id);


            return Ok("Product removed Successfully"); 

        }



        [HttpGet("Compare")]
        public async Task<IActionResult> Compare([FromQuery]int id1 , [FromQuery]int id2)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
          SELECT p.product_id , p.name , p.price , b.name as BrandName , c.name as CategoryName ,
          s.storage , s.ram , s.screen_size , s.battery , s.color , s.os
          FROM products p
          JOIN brands b  ON p.brand_id = b.brand_id
          JOIN categories c ON p.category_id = c.category_id
          LEFT JOIN  product_specs s ON p.product_id = s.product_id
          WHERE p.product_id IN (@Id1 , @Id2)", conn);



            cmd.Parameters.AddWithValue("@Id1", id1);
            cmd.Parameters.AddWithValue("@Id2", id2);

            using var reader =await cmd.ExecuteReaderAsync();
            var results = new List<object>();

            while (reader.Read())
            {
                results.Add(new { 
                
                    ProductId = (int)reader["product_id"],
                    Name = (string)reader["name"],
                    Price = (decimal)reader["price"],
                    BrandName = (string)reader["BrandName"],
                    CategoryName =(string)reader["CategoryName"],
                    Specs = new
                    {
                        Storage = reader["storage"] as string ,
                        Ram = reader["ram"] as string,
                        ScreenSize = reader["screen_size"] as string,
                        Battery = reader["battery"] as string,
                        Color = reader["color"] as string,
                        OS = reader["os"] as string
                    }
                });

            }
            if (id1 == id2) return BadRequest("You Cannot request Same IDs ");
            if (results.Count == 0) return NotFound("Products not found .");

            
            return Ok(results);
        }




        //------------------------------------------------------------------

        //------------------------------------------------------------------

        //------------------------------------------------------------------

        //------------------------------------------------------------------
        public class AddProductRequest
        {
            public string Name { get; set; } = "";
            public int BrandId { get; set; }
            public int CategoryId { get; set; }
            public decimal Price { get; set; }
            public string? ImageUrl { get; set; }
            public string? Description { get; set; }
            public int StockQuantity { get; set; }
            public bool IsAvailable { get; set; } = true;
        }

    }
}
