using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Mobile_Store_Progressed.Data;
using Mobile_Store_Progressed.DTOs.OrderRequest;
using System.Transactions;

namespace Mobile_Store_Progressed.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly DataBaseConnection _db;
        private readonly ILogger<OrderController> _Logger;

        public OrderController(DataBaseConnection db, ILogger<OrderController> logger)
        {
            _db = db;
            _Logger = logger;
        }


        [HttpPost]
        [Authorize]

        public async Task<IActionResult> PlaceOrder([FromBody] OrderRequest request)
        {
            //CHECKING IF THE ORDER WAS EMPTY OR IT WAS COUNT TO 0 
            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest("Order Must Have at least One Item ");
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            //TRANSACTION . 
            using var Transaction = conn.BeginTransaction();

            try
            {
                decimal totalAmount = 0;

                foreach (var item in request.Items)
                {
                    var PriceCmd = new SqlCommand(
                        "SELECT price FROM products Where product_id = @Id  AND  is_available = 1 ", conn, Transaction);
                    PriceCmd.Parameters.AddWithValue("@Id", item.ProductId);
                    var Price = await PriceCmd.ExecuteScalarAsync();
                    // missing this line

                    if (Price == null)
                        return BadRequest($"Product {item.ProductId} not found or unavaialble");

                    totalAmount += (decimal)Price * item.Qunatity;
                }

                var OrderCmd = new SqlCommand(@"
                   INSERT INTO Orders(CustomerName , CustomerPhone , CustomerAddress , OrderDate , TotalAmount , Status)
                   VALUES(@Name , @Phone , @Addres , GETDATE() , @Total , 'Pending')
                   SELECT SCOPE_IDENTITY(); ", conn, Transaction);

                OrderCmd.Parameters.AddWithValue("@Name", request.CustomerName);
                OrderCmd.Parameters.AddWithValue("@Phone", request.CusotmerPhone ?? "");
                OrderCmd.Parameters.AddWithValue("@Addres", request.CustomerAddress);
                OrderCmd.Parameters.AddWithValue("@Total", totalAmount);

                var orderId = Convert.ToInt32(await OrderCmd.ExecuteScalarAsync());
                foreach (var item in request.Items)
                {
                    var priceCmd = new SqlCommand(
                         "SELECT price , name FROM products where product_id = @Id  ", conn, Transaction);
                    priceCmd.Parameters.AddWithValue("@Id", item.ProductId);
                  



                    using var reader = await priceCmd.ExecuteReaderAsync();
                    reader.Read();
                    var price = (decimal)reader["price"];
                    var name = (string)reader["name"];

                    reader.Close();


                    var itemCmd = new SqlCommand(@"
                     INSERT INTO OrderItems(OrderID , ProductID , ProductName ,Quantity  , UnitPrice, TotalPrice)
                     VALUES(@OrderId, @ProductId, @ProductName, @Quantity, @UnitPrice, @TotalPrice)", conn, Transaction);
                    itemCmd.Parameters.AddWithValue("@OrderId", orderId);
                    itemCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    itemCmd.Parameters.AddWithValue("@ProductName", name);
                    itemCmd.Parameters.AddWithValue("@Quantity", item.Qunatity);
                    itemCmd.Parameters.AddWithValue("@UnitPrice", price);
                    itemCmd.Parameters.AddWithValue("@TotalPrice", price * item.Qunatity);
                    await itemCmd.ExecuteNonQueryAsync();

                   
                }
                _Logger.LogInformation("Order Placed :{OrderId} By {Name}", orderId, request.CustomerName);
                return Ok(new
                {
                    OrderId = orderId,
                    totalAmount = totalAmount,
                    Message = "Order placed successfully"
                });
            }



            catch (Exception ex)
            {
                await Transaction.RollbackAsync();
                _Logger.LogError(ex, "Order failed for {Name}", request.CustomerName);
                return StatusCode(500, "Order failed");
            }
        

            
        }



        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            SELECT OrderID , CustomerName , CustomerPhone , CustomerAddress,
            OrderDate , TotalAmount , Status
            FROM Orders
            ORDER BY OrderDate DESC", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            var orders = new List<Object>();

            while (reader.Read())
            {
                orders.Add(new
                {
                    OrderId = (int)reader["OrderID"],
                    CustomerName = (string)reader["CustomerName"],
                    CustomerPhone = reader["CustomerPhone"] as string,
                    CustomerAddress = (string)reader["CustomerAddress"],
                    OrderDate = (DateTime)reader["OrderDate"],
                    TotalAmount = (decimal)reader["TotalAmount"],
                    Status = reader["Status"] as string
                });
            }
            return Ok(orders);

        }
    }
}
