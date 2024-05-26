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
using MySqlConnector;

namespace FinanceLiquidityManager.Handler.Transaction
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class TransactionHandler : ControllerBase
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

        

        public async Task<ActionResult> GetAllTransactions(string userId, TransactionModelRequest request)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(userId);
            }

            // Fetch AccountIds based on the user's name
            string query = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";

            using (var connection = new MySqlConnection(connectionString))
            {
                var accountIds = await connection.QueryAsync<string>(query, new { Name = userId });

                // If no AccountIds found, return empty result
                if (accountIds == null || !accountIds.Any())
                {
                    return Ok(new List<Transaction>()); // Return empty list
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
                }) ;
               

                return Ok(transactions);
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


    public class Transaction
    {
        public string TransactionId { get; set; } = null!;
        public string AccountId { get; set; } = null!;
    }

}
