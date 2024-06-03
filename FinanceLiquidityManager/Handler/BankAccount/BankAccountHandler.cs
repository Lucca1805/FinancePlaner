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

namespace FinanceLiquidityManager.Handler.BankAccount
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
                        _logger.LogInformation("Fetching Insurances for AccountId: {accountId}", accountId);
                        string insuranceQuery = @"Delete From finance.insurance WHERE PolicyHolderId = @AccountId";
                        var affectedInsuranceRows = await connection.ExecuteAsync(insuranceQuery, new { AccountId = accountId });
                        if (affectedInsuranceRows > 0)
                        {
                            deleted++;
                        }
                        _logger.LogInformation("Fetching loans for AccountId: {accountId}", accountId);
                        string loanQuery = @"DELETE FROM finance.loan WHERE CreditorAccountId = @AccountId";
                        var affectedLoanRows = await connection.ExecuteAsync(loanQuery, new { AccountId = accountId });
                        if (affectedLoanRows > 0)
                        {
                            deleted++;
                        }
                        _logger.LogInformation("Fetching standingOrders for AccountId: {accountId}", accountId);
                        string standingOrderQuery = @"DELETE FROM finance.standingOrders WHERE CreditorAccountId = @AccountId";
                        var affectedOrderRows = await connection.ExecuteAsync(standingOrderQuery, new { AccountId = accountId });
                        if (affectedOrderRows > 0)
                        {
                            deleted++;
                        }
                        _logger.LogInformation("Fetching Transactions for AccountId: {accountId}", accountId);
                        string TransactionQuery = @"Delete FROM finance.transactions WHERE AccountId = @accountId";
                        var affectedTransactionRows = await connection.ExecuteAsync(TransactionQuery, new { accountId = accountId });
                        if (affectedTransactionRows > 0)
                        {
                            deleted++;
                        }
                        _logger.LogInformation("Fetching accounts for AccountId: {accountId}", accountId);
                        string bankQuery = @"DELETE FROM finance.accounts WHERE AccountId = @accountId";
                        var affectedbankRows = await connection.ExecuteAsync(bankQuery, new { accountId = accountId });
                        if (affectedbankRows > 0)
                        {
                            deleted++;
                        }
                        _logger.LogInformation("Fetching bankAccounts for AccountId: {accountId}", accountId);
                        string bankAccountQuery = @"DELETE FROM finance.bank_account WHERE accountId = @AccountId";
                        var affectedRows = await connection.ExecuteAsync(bankAccountQuery, new { accountId = accountId });
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
                        return Ok(new List<AccountModel>()); // Return empty list
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

    public class AccountModel
    {
        public string AccountId { get; set; }
    }
}
