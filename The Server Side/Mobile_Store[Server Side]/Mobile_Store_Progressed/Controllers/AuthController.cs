using Microsoft.AspNetCore.Mvc;
using Mobile_Store_Progressed.Data;
using Mobile_Store_Progressed.DTOs.Auth;
using Mobile_Store_Progressed.Models;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.RateLimiting;


namespace Mobile_Store_Progressed.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class AuthController : ControllerBase
    {

        private readonly DataBaseConnection _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _Logger;

        public AuthController(DataBaseConnection db, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _db = db;
            _configuration = configuration;
            _Logger = logger;
        }

        [HttpPost("Login")]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand("SELECT * FROM ApiUsers Where Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", request.Email);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.Read())
            {
                _Logger.LogWarning("Login Failed  -- Email Not Found -- : {Email}}", request.Email);
                return Unauthorized("Invalid credentials");
            }


            var user = new ApiUser
            {
                Id = (int)reader["UserId"],
                Email = (string)reader["Email"],
                PasswordHash = (string)reader["PasswordHash"],
                Role = (string)reader["Role"]
            };
            reader.Close();

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {

                _Logger.LogWarning("Login Failed -Wrong Password- : {Email}", request.Email);

                return Unauthorized("Invalid Credentails");
            }

            var AccessToken = GenerateAccessToken(user);
            var RefreshToken = GenerateRefreshToken();

            //Saving My  Refresh token Hashed To DB
            var UpdateCmd = new SqlCommand(@"UPDATE ApiUsers
                       SET RefreshTokenHash = @Hash , 
                        RefreshTokenExpiresAt =@Expires,
                        RefreshTokenRevokedAt = NULL
                    where UserId = @Id", conn);


            UpdateCmd.Parameters.AddWithValue("@Hash", BCrypt.Net.BCrypt.HashPassword(RefreshToken));
            UpdateCmd.Parameters.AddWithValue("@Expires", DateTime.UtcNow.AddDays(7));
            UpdateCmd.Parameters.AddWithValue("@Id", user.Id);
            await UpdateCmd.ExecuteNonQueryAsync();

            _Logger.LogInformation("Login Successful : {Email} ", request.Email);
            return Ok(new TokenResponse { AccessToken = AccessToken, RefreshToken = RefreshToken });


        }
        //GENERATIN THE ACCESS TOKEN 
        private string GenerateAccessToken(ApiUser user)
        {
            var claims = new[]
            {
                      new Claim(ClaimTypes.NameIdentifier , user.Id.ToString()),
                      new Claim(ClaimTypes.Email , user.Email),
                      new Claim(ClaimTypes.Role ,user.Role)
                   };
            var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            //HERE WE NEED GET INTO THE USER ROLE AND CREDETINALS , TO SEE WHAT HE 
            //HE CAN DO AND WHAT NOT , LIKE THE AUDIENCE --> ROLE : ZAYN --> ADMIN 
            //CHEKCED (GOOD) --> HE IS ADMIN 
            var token = new JwtSecurityToken(
                issuer: "MobileStoreApi",
                audience: "MobileStoreUsers",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds);


            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        //GENERATING THE REFRESH TOKEN 
        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand("SELECT * FROM ApiUsers WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", request.Email);

            using var reader = await cmd.ExecuteReaderAsync();
            if(!reader.Read())
            {
                return Unauthorized("Invalid Request");
            }

            var user = new ApiUser
            {
                Id = (int)reader["UserId"],
                Email = (string)reader["Email"],
                Role = (string)reader["Role"],
                RefreshTokenHash = reader["RefreshTokenHash"] as string,
                RefreshTokenExpiresAt = reader["RefreshTokenExpiresAt"] as DateTime?,
                RefreshTokenRevokedAt = reader["RefreshTokenRevokedAt"] as DateTime?
            };

            reader.Close();

            if(user.RefreshTokenRevokedAt != null)
            {
                _Logger.LogWarning("Refresh Token Revoked: {Email}" , request.Email);
                return Unauthorized("Refresh Token Revoked ");

            }


            if(user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                _Logger.LogWarning("Refresh token Expired : {Email}", request.Email);
                return Unauthorized("Refresh token Expired ");
            }

            if(!BCrypt.Net.BCrypt.Verify(request.RefreshToken , user.RefreshTokenHash!))
            {
                _Logger.LogWarning("Invalid refresh token: {Email}" ,request.Email);
                return Unauthorized("Invalid refresh token");
            }

            var NewAccessToken = GenerateAccessToken(user);
            var NewRefreshToken = GenerateRefreshToken();

            var Updatecmd = new SqlCommand(@"UPDATE ApiUsers
                        SET RefreshTokenHash  = @Hash,
                        RefreshTokenExpiresAt = @Expires,
                        RefreshTokenRevokedAt = NULL
           WHERE UserId = @Id", conn);

            Updatecmd.Parameters.AddWithValue("@Hash", BCrypt.Net.BCrypt.HashPassword(NewRefreshToken));
            Updatecmd.Parameters.AddWithValue("@Expires", DateTime.UtcNow.AddDays(7));
            Updatecmd.Parameters.AddWithValue("@Id", user.Id);
            await Updatecmd.ExecuteNonQueryAsync();

            _Logger.LogInformation("Token Refreshed: {Email}", request.Email);
            return Ok(new TokenResponse { AccessToken = NewAccessToken, RefreshToken = NewRefreshToken });

        }



        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"UPDATE ApiUsers 
        SET RefreshTokenRevokedAt = @RevokedAt 
        WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@RevokedAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@Email", request.Email);
            await cmd.ExecuteNonQueryAsync();

            _Logger.LogInformation("User logged out: {Email}", request.Email);
            return Ok("Logged out successfully");
        }



      
    }
}
