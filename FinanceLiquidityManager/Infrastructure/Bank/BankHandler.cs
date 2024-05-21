using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Dapper;
using FinanceLiquidityManager.Controllers;

namespace FinanceLiquidityManager.Infrastructure.Bank
{
   
    public class BankHandler : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly ILogger<BankController> _logger;
        public BankHandler(IConfiguration configuration, ILogger<BankController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            var host = _configuration["DBHOST"] ?? "localhost";
            var port = _configuration["DBPORT"] ?? "3306";
            var password = _configuration["MYSQL_PASSWORD"] ?? _configuration.GetConnectionString("MYSQL_PASSWORD");
            var userid = _configuration["MYSQL_USER"] ?? _configuration.GetConnectionString("MYSQL_USER");
            var usersDataBase = _configuration["MYSQL_DATABASE"] ?? _configuration.GetConnectionString("MYSQL_DATABASE");

            connectionString = $"server={host}; userid={userid};pwd={password};port={port};database={usersDataBase}";
        }

        public async Task<ActionResult<BankModel>> GetOneBank(int bankId)
        {
            try
            {
                string query = @"SELECT * FROM finance.bank WHERE BankId = @BankId";
                using (var connection = new MySqlConnection(connectionString))
                {
                    var bank = await connection.QueryFirstOrDefaultAsync<BankModel>(query, new { BankId = bankId });
                    if (bank != null)
                    {
                        return Ok(bank);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception)
            {
                return StatusCode(500, "Unable To Process Request");
            }
        }
    }

    public class BankModel
    {
        public int BankId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Country { get; set; }
        public string BIC { get; set; }
        public string OrderNumber { get; set; }
        public int OrderNumberPW { get; set; }
    }
}
