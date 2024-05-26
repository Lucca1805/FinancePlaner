
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using FinanceLiquidityManager.Controllers;
using FinanceLiquidityManager.Models;
using FinanceLiquidityManager.Handler.Credit;

namespace FinanceLiquidityManager.Handler.StandingOrder
{

    public class StandingOrderHandler : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly ILogger<StandingOrderController> _logger;
        public StandingOrderHandler(IConfiguration configuration, ILogger<StandingOrderController> logger)
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

        
        public async Task<ActionResult<IEnumerable<StandingOrder>>> GetAllStandingordersForUser(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(userId);
                }

                // Fetch AccountIds based on the user's name
                string query = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";
                using (var connection = new MySqlConnection(connectionString))
                {
                    _logger.LogInformation("Retrieving AccountIds for user: {userId}", userId);
                    var accountIds = await connection.QueryAsync<string>(query, new { Name = userId });

                    // If no AccountIds found, return empty result
                    if (accountIds == null || !accountIds.Any())
                    {
                        _logger.LogWarning("No AccountIds found for user: {userId}", userId);
                        return Ok(new List<StandingOrder>()); // Return empty list
                    }

                    _logger.LogInformation("Retrieved AccountIds: {accountIds}", string.Join(", ", accountIds));

                    // Fetch loans for each AccountId
                    List<StandingOrder> allStandingOrders = new List<StandingOrder>();
                    foreach (var accountId in accountIds)
                    {
                        _logger.LogInformation("Fetching standingOrders for AccountId: {accountId}", accountId);
                        string loanQuery = @"SELECT * FROM finance.standingOrders WHERE CreditorAccountId = @CreditorAccountId";
                        var standingOrders = await connection.QueryAsync<StandingOrder>(loanQuery, new { CreditorAccountId = accountId });
                        allStandingOrders.AddRange(standingOrders);
                    }

                    _logger.LogInformation("All standingOrders successfully retrieved.");
                    return Ok(allStandingOrders);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving loans.");
                return new StatusCodeResult(500);
            }
        }

        public async Task<ActionResult<IEnumerable<StandingOrder>>> GetAllStandingordersForUserByTime(string userId, DateTime dateTime)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(userId);
                }

                // Fetch AccountIds based on the user's name
                string query = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";
                using (var connection = new MySqlConnection(connectionString))
                {
                    _logger.LogInformation("Retrieving AccountIds for user: {userId}", userId);
                    var accountIds = await connection.QueryAsync<string>(query, new { Name = userId });

                    // If no AccountIds found, return empty result
                    if (accountIds == null || !accountIds.Any())
                    {
                        _logger.LogWarning("No AccountIds found for user: {userId}", userId);
                        return Ok(new List<StandingOrder>()); // Return empty list
                    }

                    _logger.LogInformation("Retrieved AccountIds: {accountIds}", string.Join(", ", accountIds));

                    // Fetch standingOrders for each AccountId
                    List<StandingOrder> allStandingOrders = new List<StandingOrder>();
                    foreach (var accountId in accountIds)
                    {
                        _logger.LogInformation("Fetching standingOrders for AccountId: {accountId}", accountId);
                        string loanQuery = @"SELECT * FROM standingOrders WHERE CreditorAccountId = @CreditorAccountId AND @dateTime >= FirstPaymentDateTime"; ;
                        var standingOrders = await connection.QueryAsync<StandingOrder>(loanQuery, new { CreditorAccountId = accountId, dateTime});
                        allStandingOrders.AddRange(standingOrders);
                    }

            

                    _logger.LogInformation("All standingOrders successfully retrieved.");
                    return Ok(allStandingOrders);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving loans.");
                return new StatusCodeResult(500);
            }
        }


    }

    public class StandingOrder
    {
        public int OrderId { get; set; }
        public string CreditorAccountId { get; set; } = null!;
        public string Frequency { get; set; } = null!;
        public int? NumberOfPayments { get; set; }
        public DateTime FirstPaymentDateTime { get; set; }
        public DateTime? FinalPaymentDateTime { get; set; }
        public string Reference { get; set; } = null!;
    }

    public class AccountModel
    {
        public string AccountId { get; set; }
    }
}