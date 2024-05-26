using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FinanceLiquidityManager.Handler.Login;
using FinanceLiquidityManager.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace FinanceLiquidityManager.Handler.Person
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class PersonHandler
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;

        public PersonHandler(IConfiguration configuration)
        {
            _configuration = configuration;
            var host = _configuration["DBHOST"] ?? "localhost";
            var port = _configuration["DBPORT"] ?? "3306";
            var password = _configuration["MYSQL_PASSWORD"] ?? _configuration.GetConnectionString("MYSQL_PASSWORD");
            var userid = _configuration["MYSQL_USER"] ?? _configuration.GetConnectionString("MYSQL_USER");
            var usersDataBase = _configuration["MYSQL_DATABASE"] ?? _configuration.GetConnectionString("MYSQL_DATABASE");

            connectionString = $"server={host};userid={userid};pwd={password};port={port};database={usersDataBase}";
        }

        public async Task<IActionResult> Update(UpdateUserRequest request)
        {

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var query = @"UPDATE person SET UserName = @UserName, Email = @Email WHERE PersonId = @PersonId";
                var result = await connection.ExecuteAsync(query, new
                {
                    PersonId = request.PersonId,
                    UserName = request.UserName,
                    Email = request.Email,
                });

                if (result > 0)
                {
                    return new OkObjectResult(new { Message = "User updated successfully." });
                }
                else
                {
                    return new BadRequestObjectResult("Failed to update user.");
                }
            }
        }

        

    }

    public class UpdateUserRequest
    {
        public int PersonId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }

}

