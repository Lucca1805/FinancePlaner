
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


        public async Task<ActionResult<IEnumerable<StandingOrderResponse>>> GetAllStandingordersForUser(string userId, string currency)
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
                        string loanQuery = @"Select s.* , t.MerchantName AS Issuer From finance.standingOrders s JOIN finance.transactions t on t.TransactionInformation = s.Reference WHERE s.CreditorAccountId = @CreditorAccountId AND t.AccountId = @CreditorAccountId";
                        var standingOrders = await connection.QueryAsync<StandingOrder>(loanQuery, new { CreditorAccountId = accountId });
                        allStandingOrders.AddRange(standingOrders);
                    }
                    List<StandingOrderResponse> response = new List<StandingOrderResponse>();
                    foreach (StandingOrder order in allStandingOrders)
                    {
                        DateTime today = DateTime.Today;
                        DateTime nextPaymentDate = today;
                        if (order.Frequency == "Monthly")
                        {
                            DateTime nextMonth = today.AddMonths(1);
                            nextPaymentDate = new DateTime(nextMonth.Year, nextMonth.Month, 1);
                        }
                        else if (order.Frequency == "Quarterly")
                        {
                            int currentQuarter = (today.Month - 1) / 3 + 1;
                            int nextQuarter = (currentQuarter + 1) % 5;
                            nextPaymentDate = new DateTime(today.Year, (nextQuarter - 1) * 3 + 1, 1);
                        }
                        else if (order.Frequency == "Annually")
                        {
                            nextPaymentDate = new DateTime(today.Year + 1, 1, 1);
                        }
                        else if (order.Frequency == "Bi-Weekly")
                        {
                            if (today.Day <= 15)
                            {
                                // Select the 15th of the current month
                                nextPaymentDate = new DateTime(today.Year, today.Month, 15);
                            }
                            else
                            {
                                // Select the 1st of the next month
                                nextPaymentDate = today.AddMonths(1);
                                nextPaymentDate = new DateTime(nextPaymentDate.Year, nextPaymentDate.Month, 1);
                            }
                        }
                        if (order.PaymentCurrency != currency)
                        {
                            /*order.PaymentAmount = (decimal)CurrencyConverter.Convert(currency, order.PaymentCurrency, (double)order.PaymentAmount);
                            order.PaymentCurrency = currency;*/
                            decimal convertedCosts = await ConvertCurrency(order.PaymentAmount, order.PaymentCurrency, currency);
                            order.PaymentAmount = Math.Round(convertedCosts, 2);
                            order.PaymentCurrency = currency;
                        }
                        StandingOrderResponse StandingOrderRes = new StandingOrderResponse
                        {
                            OrderId = order.OrderId,
                            CreditorAccountId = order.CreditorAccountId,
                            Frequency = order.Frequency,
                            NumberOfPayments = order.NumberOfPayments,
                            FirstPaymentDateTime = order.FirstPaymentDateTime,
                            FinalPaymentDateTime = order.FinalPaymentDateTime,
                            Reference = order.Reference,
                            PaymentAmount = order.PaymentAmount,
                            PaymentCurrency = order.PaymentCurrency,
                            Issuer = order.Issuer,
                            NextPayment = nextPaymentDate
                        };
                        response.Add(StandingOrderRes);
                    }


                    _logger.LogInformation("All standingOrders successfully retrieved.");
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving loans.");
                return new StatusCodeResult(500);
            }
        }

        public async Task<ActionResult<IEnumerable<StandingOrderResponse>>> GetAllStandingordersForUserByTime(string userId, DateTime dateTime, string currency)
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
                        string loanQuery = @"Select s.* , t.MerchantName AS Issuer From finance.standingOrders s JOIN finance.transactions t on t.TransactionInformation = s.Reference WHERE s.CreditorAccountId = @CreditorAccountId AND t.AccountId = @CreditorAccountId AND @dateTime >= s.FirstPaymentDateTime";
                        var standingOrders = await connection.QueryAsync<StandingOrder>(loanQuery, new { CreditorAccountId = accountId, dateTime });
                        allStandingOrders.AddRange(standingOrders);
                    }
                    List<StandingOrderResponse> response = new List<StandingOrderResponse>();
                    foreach (var order in allStandingOrders)
                    {
                        DateTime today = DateTime.Today;
                        DateTime nextPaymentDate = today;
                        if (order.Frequency == "Monthly")
                        {
                            DateTime nextMonth = today.AddMonths(1);
                            nextPaymentDate = new DateTime(nextMonth.Year, nextMonth.Month, 1);
                        }
                        else if (order.Frequency == "Quarterly")
                        {
                            int currentQuarter = (today.Month - 1) / 3 + 1;
                            int nextQuarter = (currentQuarter + 1) % 5;
                            nextPaymentDate = new DateTime(today.Year, (nextQuarter - 1) * 3 + 1, 1);
                        }
                        else if (order.Frequency == "Annually")
                        {
                            nextPaymentDate = new DateTime(today.Year + 1, 1, 1);
                        }
                        else if (order.Frequency == "Bi-Weekly")
                        {
                            if (today.Day <= 15)
                            {
                                // Select the 15th of the current month
                                nextPaymentDate = new DateTime(today.Year, today.Month, 15);
                            }
                            else
                            {
                                // Select the 1st of the next month
                                nextPaymentDate = today.AddMonths(1);
                                nextPaymentDate = new DateTime(nextPaymentDate.Year, nextPaymentDate.Month, 1);
                            }
                        }
                        if (order.PaymentCurrency != currency)
                        {
                            //order.PaymentAmount = (decimal)CurrencyConverter.Convert(currency, order.PaymentCurrency, (double)order.PaymentAmount);

                            decimal convertedCosts = await ConvertCurrency(order.PaymentAmount, order.PaymentCurrency, currency);
                            order.PaymentAmount = Math.Round(convertedCosts, 2);
                            order.PaymentCurrency = currency;

                        }
                        StandingOrderResponse StandingOrderRes = new StandingOrderResponse
                        {
                            OrderId = order.OrderId,
                            CreditorAccountId = order.CreditorAccountId,
                            Frequency = order.Frequency,
                            NumberOfPayments = order.NumberOfPayments,
                            FirstPaymentDateTime = order.FirstPaymentDateTime,
                            FinalPaymentDateTime = order.FinalPaymentDateTime,
                            Reference = order.Reference,
                            PaymentAmount = order.PaymentAmount,
                            PaymentCurrency = order.PaymentCurrency,
                            Issuer = order.Issuer,
                            NextPayment = nextPaymentDate
                        };
                        response.Add(StandingOrderRes);
                    }


                    _logger.LogInformation("All standingOrders successfully retrieved.");
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving loans.");
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

    public class StandingOrderResponse
    {

        public int OrderId { get; set; }
        public string CreditorAccountId { get; set; } = null!;
        public string Frequency { get; set; } = null!;
        public int? NumberOfPayments { get; set; }
        public DateTime FirstPaymentDateTime { get; set; }
        public DateTime? FinalPaymentDateTime { get; set; }
        public string Reference { get; set; } = null!;
        public decimal PaymentAmount { get; set; }
        public string PaymentCurrency { get; set; }
        public string Issuer { get; set; }
        public DateTime NextPayment { get; set; }
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
        public decimal PaymentAmount { get; set; }
        public string PaymentCurrency { get; set; }
        public string Issuer { get; set; }
    }

    public class AccountModel
    {
        public string AccountId { get; set; }
    }
}
