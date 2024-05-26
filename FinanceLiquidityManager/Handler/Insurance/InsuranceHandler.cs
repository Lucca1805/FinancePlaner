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
using System.Collections;

namespace FinanceLiquidityManager.Handler.Insurance
{

    public class InsuranceHandler : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly ILogger<InsuranceController> _logger;
        public InsuranceHandler(IConfiguration configuration, ILogger<InsuranceController> logger)
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



        public async Task<ActionResult> GetOneInsurance(string userId, int insuranceId)
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
                        return Ok(new List<InsuranceModel>()); // Return empty list
                    }

                    _logger.LogInformation("Retrieved AccountIds: {accountIds}", string.Join(", ", accountIds));

                    // Fetch insurances for each AccountId

                    _logger.LogInformation("Fetching standingOrders for AccountId: {accountId}", accountIds);
                    string insuranceQuery = @"SELECT * FROM finance.insurance WHERE InsuranceId = @InsuranceId";
                    var insurance = await connection.QueryAsync<InsuranceModel>(insuranceQuery, new { PolicyHolderId = accountIds, insuranceId });


                    _logger.LogInformation("All standingOrders successfully retrieved.");
                    return Ok(insurance);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving loans.");
                return new StatusCodeResult(500);
            }
        }

        public async Task<ActionResult<IEnumerable<InsuranceModel>>> GetAllInsuranceForUser(string userId)
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
                        return Ok(new List<InsuranceModel>()); // Return empty list
                    }

                    _logger.LogInformation("Retrieved AccountIds: {accountIds}", string.Join(", ", accountIds));

                    // Fetch insurances for each AccountId
                    List<InsuranceModel> allStandingOrders = new List<InsuranceModel>();
                    foreach (var accountId in accountIds)
                    {
                        _logger.LogInformation("Fetching standingOrders for AccountId: {accountId}", accountId);
                        string insuranceQuery = @"SELECT * FROM finance.insurance WHERE PolicyHolderId = @PolicyHolderId";
                        var standingOrders = await connection.QueryAsync<InsuranceModel>(insuranceQuery, new { PolicyHolderId = accountId });
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


        public async Task<ActionResult> DeleteOneInsurance(string userId, int insuranceId)
        {
            try
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
                        return Unauthorized();
                    }


                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // Delete dependent records in the files table
                            string deleteFilesQuery = @"DELETE FROM finance.files WHERE RefID = @InsuranceId";
                            await connection.ExecuteAsync(deleteFilesQuery, new { InsuranceId = insuranceId }, transaction);

                            // Delete the credit record
                            string deleteCreditQuery = @"DELETE FROM finance.insurance WHERE InsuranceId = @InsuranceId";
                            var affectedRows = await connection.ExecuteAsync(deleteCreditQuery, new { InsuranceId = insuranceId }, transaction);

                            // Commit the transaction if successful
                            await transaction.CommitAsync();

                            if (affectedRows > 0)
                            {
                                return NoContent();
                            }
                            else
                            {
                                return NotFound();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Rollback the transaction in case of any errors
                            await transaction.RollbackAsync();
                            _logger.LogError(ex, "An error occurred while deleting the credit.");
                            return StatusCode(500, new
                            {
                                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                                title = "An error occurred while processing your request.",
                                status = 500,
                                traceId = HttpContext.TraceIdentifier
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                return StatusCode(500, "Unable to process request");
            }


        }


        public async Task<ActionResult> UpdateOneInsurance(string userId, int insuranceId, InsuranceModel updatedInsurance)
        {
            // Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid user");
            }

            // Check if insuranceId in the request matches the one in the path
            if (insuranceId != updatedInsurance.InsuranceId)
            {
                return BadRequest("InsuranceId mismatch");
            }

            try
            {
                // Fetch AccountIds based on the user's name
                string query = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";
                using (var connection = new MySqlConnection(connectionString))
                {
                    var accountIds = await connection.QueryAsync<string>(query, new { Name = userId });

                    // If no AccountIds found for the user, return Unauthorized
                    if (accountIds == null || !accountIds.Any())
                    {
                        return Unauthorized("User not authorized");
                    }

                    // Perform the update only if the user is authorized
                    string updateQuery = @"
                UPDATE finance.insurance
                SET 
                    PolicyHolderId = @PolicyHolderId, 
                    InsuranceType = @InsuranceType, 
                    PaymentInstalmentAmount = @PaymentInstalmentAmount, 
                    PaymentInstalmentUnitCurrency = @PaymentInstalmentUnitCurrency, 
                    DateOpened = @DateOpened, 
                    DateClosed = @DateClosed, 
                    InsuranceState = @InsuranceState, 
                    PaymentAmount = @PaymentAmount, 
                    PaymentUnitCurrency = @PaymentUnitCurrency, 
                    Polizze = @Polizze, 
                    InsuranceCompany = @InsuranceCompany, 
                    Description = @Description, 
                    Country = @Country
                WHERE InsuranceId = @InsuranceId";

                    var affectedRows = await connection.ExecuteAsync(updateQuery, updatedInsurance);
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
                return StatusCode(500, "Unable to process request");
            }
        }

        public async Task<ActionResult> getCostHistoryData(string userId, string currency)
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

                if (accountIds == null || !accountIds.Any())
                {
                    _logger.LogWarning("No AccountIds found for user: {userId}", userId);
                    return Ok(new List<InsuranceHistoryChartRespone>());
                }

                _logger.LogInformation("Retrieved AccountIds: {accountIds}", string.Join(", ", accountIds));

                InsuranceHistoryChartRespone response = new InsuranceHistoryChartRespone { InsuranceCosts = new List<InsuranceCost>() };
                foreach (var accountId in accountIds)
                {
                    _logger.LogInformation("Fetching Insurances for AccountId: {accountId}", accountId);
                    string insuranceaccountQuery = @"SELECT * FROM finance.bank_account WHERE AccountId = @AccountId";
                    var insuranceContent = (await connection.QueryAsync<InsuranceQueryModel>(insuranceaccountQuery, new { AccountId = accountId })).ToList();
                    
                    foreach (var content in insuranceContent)
                    {
                        if (currency != content.Currency)
                        {
                            content.Cost = CurrencyConverter.Convert(currency, content.Currency, content.Cost);
                        }

                        if (content.interval == "monthly")
                        {
                            for (int monthCounter = 1; monthCounter < 13; monthCounter++)
                            {
                                response.InsuranceCosts.Add(
                                    new InsuranceCost
                                    {
                                        Cost = content.Cost,
                                        Currency = currency,
                                        Insurance = content.Type,
                                        Month = MonthConverter.GetMonthName(monthCounter)
                                    }
                                    );
                            }
                        }
                        else if (content.interval == "quarterly")
                        {
                            content.Cost = content.Cost / 4;
                            int month = 0;
                            for (int monthCounter = 1; monthCounter < 4; monthCounter++)
                            {
                                switch (monthCounter)
                                {
                                    case 1:
                                        month = 1;
                                        break;
                                    case 2:
                                        month = 4;
                                        break;
                                    case 3:
                                        month = 7;
                                        break;
                                    case 4:
                                        month = 10;
                                        break;
                                }
                                response.InsuranceCosts.Add(
                                    new InsuranceCost
                                    {
                                        Cost = content.Cost,
                                        Currency = currency,
                                        Insurance = content.Type,
                                        Month = MonthConverter.GetMonthName(month)
                                    }
                                    );
                            }
                        }
                        else
                        {
                            //assume yearly
                            content.Cost = content.Cost / 12;
                            for (int monthCounter = 1; monthCounter < 13; monthCounter++)
                            {
                                response.InsuranceCosts.Add(

                                    new InsuranceCost
                                    {
                                        Cost = content.Cost,
                                        Currency = currency,
                                        Insurance = content.Type,
                                        Month = MonthConverter.GetMonthName(monthCounter)
                                    }
                                    );
                            }
                        }
                    }
                }

                _logger.LogInformation("All insurances successfully retrieved.");
                return Ok(response);
            }
        }
    }
}

public class InsuranceModel
{
    public int InsuranceId { get; set; }
    public string PolicyHolderId { get; set; }
    public string InsuranceType { get; set; }
    public decimal PaymentInstalmentAmount { get; set; }
    public string PaymentInstalmentUnitCurrency { get; set; }
    public DateTime DateOpened { get; set; }
    public DateTime? DateClosed { get; set; }
    public bool InsuranceState { get; set; }
    public decimal PaymentAmount { get; set; }
    public string PaymentUnitCurrency { get; set; }
    public byte[] Polizze { get; set; }
    public string InsuranceCompany { get; set; }
    public string Description { get; set; }
    public string Country { get; set; }
}
public class InsuranceHistoryChartRespone
{
    public List<InsuranceCost> InsuranceCosts { get; set; }
}

public class InsuranceCost
{
    public string Month { get; set; }
    public string Insurance { get; set; }
    public double Cost { get; set; }
    public string Currency { get; set; }

}

public class InsuranceQueryModel
{
    public string interval { get; set; }
    public double Cost { get; set; }
    public string Type { get; set; }
    public string Currency { get; set; }
}
public class CurrencyConverter
{
    // Assume exchange rate from USD to EUR
    private const double UsdToEurExchangeRate = 0.85; // 1 USD = 0.85 EUR

    public static double Convert(string newCurrency, string oldCurrency, double Value)
    {
        if (newCurrency == "€" && oldCurrency == "USD")
        {
            return ConvertUsdToEur(Value);
        }
        else if (newCurrency == "USD" && oldCurrency == "€")
        {
            return ConvertEurToUsd(Value);
        }
        else
        {
            return (Value);
        }
    }
    // Convert USD to EUR
    public static double ConvertUsdToEur(double amountInUsd)
    {
        return amountInUsd * UsdToEurExchangeRate;
    }

    // Convert EUR to USD
    public static double ConvertEurToUsd(double amountInEur)
    {
        return amountInEur / UsdToEurExchangeRate;
    }
}

public class MonthConverter
{
    public static string GetMonthName(int monthNumber)
    {
        switch (monthNumber)
        {
            case 1:
                return "January";
            case 2:
                return "February";
            case 3:
                return "March";
            case 4:
                return "April";
            case 5:
                return "May";
            case 6:
                return "June";
            case 7:
                return "July";
            case 8:
                return "August";
            case 9:
                return "September";
            case 10:
                return "October";
            case 11:
                return "November";
            case 12:
                return "December";
            default:
                return "Invalid month value";
        }
    }
}