using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using FinanceLiquidityManager.Handler.Insurance;
using FinanceLiquidityManager.Handler.Login;
using FinanceLiquidityManager.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;

namespace FinanceLiquidityManager.Handler.Transaction
{
    public class TransactionHandler : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly ILogger<TransactionHandler> _logger;

        public TransactionHandler(IConfiguration configuration, ILogger<TransactionHandler> logger)
        {
            _configuration = configuration;
            _logger = logger;
            var host = _configuration["DBHOST"] ?? "localhost";
            var port = _configuration["DBPORT"] ?? "3306";
            var password = _configuration["MYSQL_PASSWORD"] ?? _configuration.GetConnectionString("MYSQL_PASSWORD");
            var userid = _configuration["MYSQL_USER"] ?? _configuration.GetConnectionString("MYSQL_USER");
            var usersDataBase = _configuration["MYSQL_DATABASE"] ?? _configuration.GetConnectionString("MYSQL_DATABASE");

            connectionString = $"server={host};userid={userid};pwd={password};port={port};database={usersDataBase}";
        }

        public async Task<ActionResult> GetAllTransactions(string userId, TransactionModelRequest request, string Currency)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Fetch AccountIds based on the user's name
            string query = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";

            using (var connection = new MySqlConnection(connectionString))
            {
                var accountIds = await connection.QueryAsync<string>(query, new { Name = userId });

                if (accountIds == null || !accountIds.Any())
                {
                    return Ok(new List<Transaction>());
                }


                // Fetch insurances for each AccountId

                var queryBuilder = new StringBuilder(@"SELECT t.TransactionId, t.AccountId, t.AmountCurrency, t.TransactionIssuer, t.TransactionInformation, t.Amount, t.ExchangeRate, t.UnitCurrency, t.SupplementaryData, t.BookingDateTime, t.MerchantName,
                                                             b.BankId, b.bic FROM finance.transactions t
                                                            INNER JOIN finance.bank_account ba ON t.AccountId = ba.AccountId
                                                            INNER JOIN finance.bank b ON ba.BankId = b.BankId");

                if (!string.IsNullOrEmpty(request.FreeText))
                {
                    queryBuilder.Append(" AND (t.TransactionInformation LIKE @FreeText)");
                }
                if (!string.IsNullOrEmpty(request.Iban))
                {
                    queryBuilder.Append(" AND b.bic = @Iban");
                }
                if (request.Accounts != null && request.Accounts.Any())
                {
                    queryBuilder.Append(" AND t.AccountId = @Accounts");
                }
                if (request.DateFrom.HasValue)
                {
                    queryBuilder.Append(" AND t.BookingDateTime >= @DateFrom");
                }
                if (request.DateTo.HasValue)
                {
                    request.DateTo = request.DateTo.Value.AddDays(1);
                    queryBuilder.Append(" AND t.BookingDateTime <= @DateTo");
                }
                var transactions = await connection.QueryAsync<TransactionResponse>(queryBuilder.ToString(), new
                {
                    FreeText = $"%{request.FreeText}%",
                    request.Iban,
                    request.DateFrom,
                    request.DateTo,
                    request.Accounts,
                    accountIds,
                });
                foreach (TransactionResponse res in transactions)
                {
                    if (res.AmountCurrency != Currency)
                    {
                        /*res.Amount = CurrencyConverter.Convert(Currency, res.AmountCurrency, res.Amount);
                        res.AmountCurrency = Currency;*/
                        decimal convertedCosts = await ConvertCurrency((decimal)res.Amount, res.AmountCurrency, Currency);
                        res.Amount = (double)Math.Round(convertedCosts, 2);
                        res.AmountCurrency = Currency;
                    }
                }

                return Ok(transactions);
            }
        }

        public async Task<ActionResult> GetAreaValueChartData(string userId, TransactionAreaChartModelRequest request, string Currency)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var response = new TransactionAreaChartResponse
            {
                Accounts = new Dictionary<string, Account>()
            };

            DateTime adjustedDateTo = request.dateTo?.AddDays(1) ?? DateTime.MinValue;

            try
            {
                // Fetch AccountIds based on the user's name
                string accountQuery = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";

                using (var connection = new MySqlConnection(connectionString))
                {
                    var accountIds = await connection.QueryAsync<string>(accountQuery, new { Name = userId });

                    foreach (string accountId in accountIds)
                    {
                        var acc = new Account
                        {
                            AccountName = accountId,
                            Data = new List<AccountData>()
                        };

                        string transactionQuery = @"Select BookingDateTime as Date, BalanceAmount as Value, BalanceCurrency as Currency
                                            from finance.transactions 
                                            WHERE AccountId = @AccountId 
                                            AND BookingDateTime Between @dateFrom AND @dateTo";

                        var data = await connection.QueryAsync<AccountData>(transactionQuery, new { AccountId = accountId, dateFrom = request.dateFrom, dateTo = adjustedDateTo });

                        foreach (AccountData accData in data)
                        {
                            if (accData.Currency != Currency)
                            {
                                /*accData.Value = CurrencyConverter.Convert(Currency, accData.Currency, accData.Value);
                                accData.Currency = Currency;*/
                                decimal convertedCosts = await ConvertCurrency((decimal)accData.Value, accData.Currency, Currency);
                                accData.Value = (double)Math.Round(convertedCosts, 2);
                                accData.Currency = Currency;
                            }
                        }

                        acc.Data.AddRange(data);
                        response.Accounts[accountId] = acc;
                    }
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving Transactions for Area Chart.");
                return StatusCode(500);
            }
        }


        public async Task<ActionResult> GetExpenseRevenueChartData(string userId, TransactionAreaChartModelRequest request, string Currency)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            DateTime adjustedDateTo = request.dateTo?.AddDays(1) ?? DateTime.MinValue;
            List<TransactionexpenseRevenueChartResponseModel> response = new List<TransactionexpenseRevenueChartResponseModel>();

            try
            {
                string query = @"Select BalanceCreditDebitIndicator as Indicator, InstructedAmount as Amount, InstructedCurrency as Currency, BookingDateTime as Date
                         from finance.transactions 
                         WHERE AccountId IN (SELECT AccountId FROM finance.accounts WHERE Name = @Name) 
                         AND BookingDateTime BETWEEN @dateFrom AND @dateTo";

                using (var connection = new MySqlConnection(connectionString))
                {
                    var data = await connection.QueryAsync<TransactionExepenseRevenueModel>(query, new { Name = userId, dateFrom = request.dateFrom, dateTo = adjustedDateTo });

                    foreach (TransactionExepenseRevenueModel accData in data)
                    {
                        if (accData.Currency != Currency)
                        {
                            /*accData.amount = CurrencyConverter.Convert(Currency, accData.Currency, accData.amount);
                            accData.Currency = Currency;*/
                            decimal convertedCosts = await ConvertCurrency((decimal)accData.amount, accData.Currency, Currency);
                            accData.amount = (double)Math.Round(convertedCosts, 2);
                            accData.Currency = Currency;
                        }

                        var existingModel = response.FirstOrDefault(model => model.date == accData.Date);

                        if (existingModel != null)
                        {
                            if (accData.Indicator == "Credit")
                            {
                                existingModel.currency = accData.Currency;
                                existingModel.revenue += accData.amount;
                            }
                            else
                            {
                                existingModel.currency = accData.Currency;
                                existingModel.expense += accData.amount;
                            }
                        }
                        else
                        {
                            var newModel = new TransactionexpenseRevenueChartResponseModel
                            {
                                currency = accData.Currency,
                                date = accData.Date
                            };

                            if (accData.Indicator == "Credit")
                            {
                                newModel.revenue = accData.amount;
                            }
                            else
                            {
                                newModel.expense = accData.amount;
                            }

                            response.Add(newModel);
                        }
                    }
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving Transactions for Area Chart.");
                return StatusCode(500);
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

    public class TransactionModelRequest
    {
        public string? FreeText { get; set; }
        public string? Iban { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string[] Accounts { get; set; }
    }

    public class TransactionResponse
    {
        public string TransactionId { get; set; }
        public string AccountId { get; set; }
        public double Amount { get; set; }
        public DateTime BookingDateTime { get; set; }
        public string TransactionInformation { get; set; }
        public string AmountCurrency { get; set; }
        public string TransactionIssuer { get; set; }
        public string SupplementaryData { get; set; }
        public string MerchantName { get; set; }
    }

    public class TransactionAreaChartModelRequest
    {
        public string? freeText { get; set; }
        public string? iban { get; set; }
        public string[] account { get; set; }
        public DateTime? dateFrom { get; set; }
        public DateTime? dateTo { get; set; }
        //public Transactiontype? type { get; set; }
        //[TransactionTypeValidation(ErrorMessage = "Transaction type must be either 'Revenue' or 'Expense'.")]
        //public string type { get; set; }
    }

    public class TransactionexpenseRevenueChartResponseModel
    {
        public DateTime date { get; set; }
        public double expense { get; set; }
        public double revenue { get; set; }
        public string currency { get; set; }
    }

    public class TransactionExepenseRevenueModel
    {
        public string Indicator { get; set; }
        public double amount { get; set; }
        public string Currency { get; set; }
        public DateTime Date { get; set; }
    }

    public class TransactionAreaChartResponse
    {
        public Dictionary<string, Account> Accounts { get; set; }
    }

    public class Account
    {
        public string AccountName { get; set; }
        public List<AccountData> Data { get; set; }
    }

    public class AccountData
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
        public string Currency { get; set; }
    }

    public class Transaction
    {
        public string TransactionId { get; set; } = null!;
        public string AccountId { get; set; } = null!;
    }

    /*public enum Transactiontype
    {
        Revenue, Expense
    }*/

    /*public class TransactionTypeValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var transactionType = value as string;

            if (transactionType != null && (transactionType.Equals("Revenue", StringComparison.OrdinalIgnoreCase) || transactionType.Equals("Expense", StringComparison.OrdinalIgnoreCase)))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(ErrorMessage);
        }
    }*/
}
