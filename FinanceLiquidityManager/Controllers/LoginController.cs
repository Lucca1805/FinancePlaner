using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
namespace LoginController.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly ILogger<LoginController> _logger;
        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration; var host = _configuration["DBHOST"] ?? "localhost";
            var port = _configuration["DBPORT"] ?? "3306";
            var password = _configuration["MYSQL_PASSWORD"] ?? _configuration.GetConnectionString("MYSQL_PASSWORD");
            var userid = _configuration["MYSQL_USER"] ?? _configuration.GetConnectionString("MYSQL_USER");
            var usersDataBase = _configuration["MYSQL_DATABASE"] ?? _configuration.GetConnectionString("MYSQL_DATABASE");

            connectionString = $"server={host}; userid={userid};pwd={password};port={port};database={usersDataBase}";
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto userLogin)
        {
            // Validate the user credentials
            if (userLogin.Username == "admin" && userLogin.Password == "password") // Dummy check for example
            {
                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, userLogin.Username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("CurrencyPreference","€")
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expiry = DateTime.Now.AddMinutes(30);

                var token = new JwtSecurityToken(
                    _configuration["Jwt:Issuer"],
                    _configuration["Jwt:Audience"],
                    claims,
                    expires: expiry,
                    signingCredentials: creds
                );

                return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
            }
            else if (userLogin == null || string.IsNullOrWhiteSpace(userLogin.Username) || string.IsNullOrWhiteSpace(userLogin.Password))
            {
                return BadRequest("Username and password cannot be empty!");
            }
            using (var connection = new MySqlConnection(connectionString))
            {
                string query = @"SELECT * FROM person WHERE UserName = @UserName";
                var user = await connection.QueryFirstOrDefaultAsync<FinanceLiquidityManager.Models.Person>(query, new { UserName = userLogin.Username });

                if (user != null && BCrypt.Net.BCrypt.Verify(userLogin.Password, user.Password))
                {
                    var claims = new[]
                    {
                            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim("CurrencyPreference","€"),
                            new Claim("UserId", user.PersonId.ToString())
                        };

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    //var expiry = DateTime.Now.AddMinutes(30);

                    var token = new JwtSecurityToken(
                        _configuration["Jwt:Issuer"],
                        _configuration["Jwt:Audience"],
                        claims,
                        expires: null,
                        signingCredentials: creds
                    );

                    return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
                }
                else
                {
                    return NotFound();
                }
            }
        }

        [HttpPost("NewUser")]
        public async Task<IActionResult> newUser([FromBody] CreateUserRequest request)
        {
            // check if the Username already exists -> Duplicate protection
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var query = @"INSERT INTO person (UserName, Email, Password) VALUES (@UserName, @Email, @Password);
                          SELECT LAST_INSERT_ID();";
                var hashedPassword = HashPassword(request.Password); // Implement this method based on your security requirements
                var userId = await connection.QuerySingleAsync<int>(query, new
                {
                    UserName = request.UserName,
                    Email = request.Email,
                    Password = hashedPassword
                });

                if (userId > 0)
                {
                    return Ok(new { UserId = userId, Message = "User created successfully." });
                }

                return BadRequest("Failed to create user.");
            }
        }
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }

    public class UserLoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class CreateUserRequest
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string CurrencyPreference { get; set; } // This should be received as plain text and hashed before storage
    }
}
