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

        public async Task<ActionResult<IEnumerable<BankAccount>>> GetAllAccountsForUser(string userId, string CurrencyPreference)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(userId);
                }

                string query = @"
            SELECT DISTINCT a.AccountId, a.Nickname AS AccountName, b.DisplayName AS BankName
            FROM finance.accounts AS a
            JOIN finance.bank_account AS ba ON ba.AccountId = a.AccountId
            JOIN finance.bank AS b ON b.BankId = ba.BankId
            WHERE a.Name = @Name";

                using (var connection = new MySqlConnection(connectionString))
                {
                    _logger.LogInformation("Retrieving Accounts for user: {userId}", userId);
                    List<BankAccountRequest> accounts = (await connection.QueryAsync<BankAccountRequest>(query, new { Name = userId })).ToList();

                    if (accounts == null || !accounts.Any())
                    {
                        _logger.LogWarning("No Accounts found for user: {userId}", userId);
                        return Ok(new List<BankAccount>());
                    }

                    _logger.LogInformation("Retrieved Accounts: {accountIds}", string.Join(", ", accounts.Select(a => a.accountId)));

                    List<BankAccount> response = new List<BankAccount>();
                    foreach (BankAccountRequest request in accounts)
                    {
                        string bankQuery = @"
                    SELECT t.BalanceAmount as Balance, t.BalanceCurrency as Currency
                    FROM finance.transactions AS t
                    WHERE t.AccountId = @AccountId
                    ORDER BY t.ValueDateTime DESC
                    LIMIT 1";

                        TransactionsForAccount transaction = await connection.QueryFirstOrDefaultAsync<TransactionsForAccount>(bankQuery, new { AccountId = request.accountId });
                        if (transaction != null && transaction.currency != CurrencyPreference)
                        {
                            
                            decimal convertedCosts = await ConvertCurrency((decimal)transaction.balance, transaction.currency, CurrencyPreference);
                            transaction.balance = (double)Math.Round(convertedCosts,2);
                            /*transaction.balance = CurrencyConverter.Convert(CurrencyPreference, transaction.currency, transaction.balance);
                            transaction.currency = CurrencyPreference;*/
                        }
                        _logger.LogInformation("Retrieved transaction for AccountId: {accountId}, BalanceAmount: {balanceAmount}, BalanceCurrency: {balanceCurrency}",
                        request.accountId, transaction.balance, transaction.currency);

                        response.Add(new BankAccount
                        {
                            accountId = request.accountId,
                            accountName = request.accountName,
                            bankName = request.bankName,
                            balance = transaction?.balance ?? 0 // Handle case where transaction is null
                        });
                    }

                    _logger.LogInformation("All Bank Account Information successfully retrieved.");
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving bank accounts.");
                return new StatusCodeResult(500);
            }
        }

        public async Task<decimal> ConvertCurrency(decimal amount, string baseCurrency, string targetCurrency)
        {
            try
            {
                // Use the CurrencyExchangeService to convert the amount
                CurrencyExchangeService exchangeService = new CurrencyExchangeService();
                return await exchangeService.ConvertCurrency(amount, baseCurrency, targetCurrency);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error converting currency: {ex.Message}");
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

    public class BankAccount
    {
        public string accountId { get; set; }
        public string accountName { get; set; }
        public double balance { get; set; }
        public string bankName { get; set; }
    }

    public class BankAccountRequest
    {
        public string accountId { get; set; }
        public string accountName { get; set; }
        public string bankName { get; set; }

    }

    public class TransactionsForAccount
    {
        public double balance { get; set; }
        public string currency { get; set; }
    }
}
