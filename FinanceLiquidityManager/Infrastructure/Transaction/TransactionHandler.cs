using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FinanceLiquidityManager.Infrastructure.Login;
using FinanceLiquidityManager.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace FinanceLiquidityManager.Infrastructure.Transaction
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class TransactionHandler
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;

        public TransactionHandler(IConfiguration configuration)
        {
            _configuration = configuration;
            var host = _configuration["DBHOST"] ?? "localhost";
            var port = _configuration["DBPORT"] ?? "3306";
            var password = _configuration["MYSQL_PASSWORD"] ?? _configuration.GetConnectionString("MYSQL_PASSWORD");
            var userid = _configuration["MYSQL_USER"] ?? _configuration.GetConnectionString("MYSQL_USER");
            var usersDataBase = _configuration["MYSQL_DATABASE"] ?? _configuration.GetConnectionString("MYSQL_DATABASE");

            connectionString = $"server={host};userid={userid};pwd={password};port={port};database={usersDataBase}";
        }

        public async Task<IActionResult> Get()
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT * FROM transactions";

                var transactions = await connection.QueryAsync<FinanceLiquidityManager.Models.Transaction>(query);

                return new OkObjectResult(transactions);
            }
        }


    }


}

