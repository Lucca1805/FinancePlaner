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

namespace FinanceLiquidityManager.Infrastructure.BankAccount
{
    public class BankAccountHandler : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly ILogger<BankAccountController> _logger;

        public BankAccountHandler(IConfiguration configuration, ILogger<BankAccountController> logger)
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

        public async Task<ActionResult> DeleteAllBankAccounts(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                string query = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";
                using (var connection = new MySqlConnection(connectionString))
                {
                    _logger.LogInformation("Retrieving AccountIds for user: {userId}", userId);
                    var accountIds = await connection.QueryAsync<string>(query, new { Name = userId });

                    // If no AccountIds found, return empty result
                    if (accountIds == null || !accountIds.Any())
                    {
                        _logger.LogWarning("No AccountIds found for user: {userId}", userId);
                        return Ok(new List<AccountModel>()); // Return empty list
                    }

                    _logger.LogInformation("Retrieved AccountIds: {accountIds}", string.Join(", ", accountIds));

                    // Delete BankAccounts for each AccountId
                    var deleted = 0;
                    foreach (var accountId in accountIds)
                    {
                        _logger.LogInformation("Fetching loans for AccountId: {accountId}", accountId);
                        string bankAccountQuery = @"DELETE * FROM finance.bank_account WHERE AccountId = @AccountId";
                        var affectedRows = await connection.ExecuteAsync(bankAccountQuery, new { AccountId = accountId });
                        if (affectedRows > 0)
                        {
                            deleted++;
                        }

                    }
                    if (deleted > 0)
                    {
                        _logger.LogInformation("All Bankaccounts successfully deleted.");
                        return NoContent();
                    }
                    else
                    {
                        _logger.LogError("No Bankaccounts have been found.");
                        return NotFound();
                    }
                }
            }
            catch (Exception)
            {
                return new StatusCodeResult(500);
            }
        }
        public async Task<ActionResult> DeleteOneBankAccount(string bankaccountId)
        {
            try
            {
                string query = @"DELETE FROM finance.bank_account WHERE AccountId = @bankaccountId";
                using (var connection = new MySqlConnection(connectionString))
                {
                    var affectedRows = await connection.ExecuteAsync(query, new { bankaccountId = bankaccountId });
                    if (affectedRows > 0)
                    {
                        return NoContent();
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

         public async Task<ActionResult<IEnumerable<BankAccountModel>>> GetAllBankAccountsForUser(string userId)
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
                        return Ok(new List<LoanModel>()); // Return empty list
                    }

                    _logger.LogInformation("Retrieved AccountIds: {accountIds}", string.Join(", ", accountIds));

                    // Fetch loans for each AccountId
                    List<BankAccountModel> allBankAccounts = new List<BankAccountModel>();
                    foreach (var accountId in accountIds)
                    {
                        _logger.LogInformation("Fetching loans for AccountId: {accountId}", accountId);
                        string bankaccountQuery = @"SELECT * FROM finance.bank_account WHERE AccountId = @AccountId";
                        var bankAccounts = await connection.QueryAsync<BankAccountModel>(bankaccountQuery, new { AccountId = accountId });
                        allBankAccounts.AddRange(bankAccounts);
                    }

                    _logger.LogInformation("All loans successfully retrieved.");
                    return Ok(allBankAccounts);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving loans.");
                return new StatusCodeResult(500);
            }
        }
    }

    public class BankAccountModel
    {
        public int BankId { get; set; }
        public string AccountId { get; set; } = null!;
    }
}
