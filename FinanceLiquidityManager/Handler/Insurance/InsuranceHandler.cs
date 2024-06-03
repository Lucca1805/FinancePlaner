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
using FinanceLiquidityManager.Handler.Credit;

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

        [HttpPost]
        public async Task<ActionResult> AddInsurance(string userId, [FromBody] InsuranceModelRequest newInsurance, [FromForm] IFormFile polizze)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Fetch AccountId based on the user's name
                string queryAccountId = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var accountIds = await connection.QueryAsync<string>(queryAccountId, new { Name = userId });
                    var accountId = accountIds.FirstOrDefault();

                    // Check if accountId is null or empty
                    if (string.IsNullOrEmpty(accountId))
                    {
                        return Unauthorized("User account not found.");
                    }

                    // Assign the accountId to the newInsurance's PolicyHolderId
                    newInsurance.PolicyHolderId = accountId;

                    // Read the file data into a byte array
                    byte[] polizzeBytes = null;
                    if (polizze != null && polizze.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await polizze.CopyToAsync(memoryStream);
                            polizzeBytes = memoryStream.ToArray();
                        }
                    }

                    // Include the file data in the insertion query
                    string queryInsurance = @"
                INSERT INTO finance.insurance (
                    PolicyHolderId, 
                    InsuranceType, 
                    PaymentInstalmentAmount, 
                    PaymentInstalmentUnitCurrency, 
                    DateOpened, 
                    DateClosed, 
                    PaymentAmount, 
                    PaymentUnitCurrency, 
                    Description, 
                    Frequency,
                    Polizze,
                    InsuranceState,
                    InsuranceCompany,
                    Country
                ) VALUES (
                    @PolicyHolderId, 
                    @InsuranceType, 
                    @PaymentInstalmentAmount, 
                    @PaymentInstalmentUnitCurrency, 
                    @DateOpened, 
                    @DateClosed, 
                    @PaymentAmount, 
                    @PaymentUnitCurrency, 
                    @Description, 
                    @Frequency,
                    @Polizze,
                    @InsuranceState,
                    @InsuranceCompany,
                    @Country
                );
            ";

                    var parameters = new
                    {
                        PolicyHolderId = newInsurance.PolicyHolderId,
                        InsuranceType = newInsurance.InsuranceType,
                        PaymentInstalmentAmount = newInsurance.PaymentInstalmentAmount,
                        PaymentInstalmentUnitCurrency = newInsurance.PaymentInstalmentUnitCurrency,
                        DateOpened = newInsurance.DateOpened,
                        DateClosed = newInsurance.DateClosed,
                        PaymentAmount = newInsurance.PaymentAmount,
                        PaymentUnitCurrency = newInsurance.PaymentUnitCurrency,
                        Description = newInsurance.Description,
                        Frequency = newInsurance.Frequency,
                        Polizze = polizzeBytes,
                        InsuranceState = newInsurance.InsuranceState,
                        InsuranceCompany = newInsurance.InsuranceCompany,
                        Country = newInsurance.Country
                    };

                    var affectedRows = await connection.ExecuteAsync(queryInsurance, parameters);

                    if (affectedRows > 0)
                    {
                        return Ok("Insurance added successfully.");
                    }
                    else
                    {
                        return BadRequest("Failed to add insurance.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception details (ex.Message) for debugging purposes.
                return StatusCode(500, $"Unable To Process Request: {ex.Message}");
            }
        }



        public async Task<ActionResult> GetOneInsurance(string userId, int insuranceId, string currency)
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

                    foreach(InsuranceModel ins in insurance)
                    {
                        if(ins.PaymentUnitCurrency != currency)
                        {
                            ins.PaymentAmount = (decimal)CurrencyConverter.Convert(currency,ins.PaymentUnitCurrency,(double)ins.PaymentAmount);
                            ins.PaymentUnitCurrency = currency;
                            ins.PaymentInstalmentAmount = (decimal)CurrencyConverter.Convert(currency,ins.PaymentInstalmentUnitCurrency,(double)ins.PaymentInstalmentAmount); 
                            ins.PaymentInstalmentUnitCurrency = currency;
                        }
                    }
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

        public async Task<ActionResult<IEnumerable<InsuranceResponse>>> GetAllInsuranceForUser(string userId, string currency)
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
                        return Ok(new List<InsuranceResponse>()); // Return empty list
                    }

                    _logger.LogInformation("Retrieved AccountIds: {accountIds}", string.Join(", ", accountIds));

                    // Fetch insurances for each AccountId
                    List<InsuranceResponse> allStandingOrders = new List<InsuranceResponse>();
                    foreach (var accountId in accountIds)
                    {
                        _logger.LogInformation("Fetching standingOrders for AccountId: {accountId}", accountId);
                        string insuranceQuery = @"SELECT * FROM finance.insurance WHERE PolicyHolderId = @PolicyHolderId";
                        var standingOrders = await connection.QueryAsync<InsuranceResponse>(insuranceQuery, new { PolicyHolderId = accountId });
                        allStandingOrders.AddRange(standingOrders);
                    }
                    foreach (InsuranceResponse insurance in allStandingOrders)
                    {
                        DateTime today = DateTime.Today;
                        DateTime nextPaymentDate = today;
                        if (insurance.Frequency == "Monthly")
                        {
                            DateTime nextMonth = today.AddMonths(1);
                            nextPaymentDate = new DateTime(nextMonth.Year, nextMonth.Month, 1);
                        }
                        else if (insurance.Frequency == "Quarterly")
                        {
                            int currentQuarter = (today.Month - 1) / 3 + 1;
                            int nextQuarter = (currentQuarter + 1) % 5;
                            nextPaymentDate = new DateTime(today.Year, (nextQuarter - 1) * 3 + 1, 1);
                        }
                        else if (insurance.Frequency == "Annually")
                        {
                            nextPaymentDate = new DateTime(today.Year + 1, 1, 1);
                        }
                        insurance.nextPayment = nextPaymentDate;
                        if(insurance.PaymentUnitCurrency != currency){
                            insurance.PaymentAmount = (decimal)CurrencyConverter.Convert(currency,insurance.PaymentUnitCurrency,(double)insurance.PaymentAmount);
                            insurance.PaymentUnitCurrency = currency;
                            insurance.PaymentInstalmentAmount = (decimal)CurrencyConverter.Convert(currency,insurance.PaymentInstalmentUnitCurrency,(double)insurance.PaymentInstalmentAmount); 
                            insurance.PaymentInstalmentUnitCurrency = currency;
                        }
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
                    Country = @Country,
                    Frequency = @Frequency
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
                    string insuranceaccountQuery = @"SELECT InsuranceType as type, PaymentInstalmentAmount as Cost, PaymentInstalmentUnitCurrency as Currency, Frequency as intervall  FROM finance.insurance WHERE PolicyHolderId = @AccountId";
                    var insuranceContent = (await connection.QueryAsync<InsuranceQueryModel>(insuranceaccountQuery, new { AccountId = accountId })).ToList();

                    foreach (InsuranceQueryModel content in insuranceContent)
                    {
                        if (currency != content.Currency)
                        {
                            content.Cost = CurrencyConverter.Convert(currency, content.Currency, content.Cost);
                        }

                        if (content.intervall == "Monthly")
                        {
                            for (int monthCounter = 1; monthCounter < 13; monthCounter++)
                            {
                                response.InsuranceCosts.Add(
                                    new InsuranceCost
                                    {
                                        Cost = Math.Round(content.Cost, 2),
                                        Currency = currency,
                                        Insurance = content.Type,
                                        Month = MonthConverter.GetMonthName(monthCounter)
                                    }
                                    );
                            }
                        }
                        else if (content.intervall == "Quarterly")
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
                                        Cost = Math.Round(content.Cost, 2),
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
                                        Cost = Math.Round(content.Cost, 2),
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

        public async Task<ActionResult> GetIntervallChart(string userId, string currency)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(userId);
                }

                string query = @"SELECT AccountId FROM finance.accounts WHERE Name = @Name";
                using (var connection = new MySqlConnection(connectionString))
                {
                    _logger.LogInformation("Retrieving AccountIds for user: {userId}", userId);
                    var accountIds = await connection.QueryAsync<string>(query, new { Name = userId });

                    if (accountIds == null || !accountIds.Any())
                    {
                        _logger.LogWarning("No AccountIds found for user: {userId}", userId);
                        return Ok(new List<InsuranceResponse>());
                    }

                    _logger.LogInformation("Retrieved AccountIds: {accountIds}", string.Join(", ", accountIds));

                    string insuranceQuery = @"SELECT * FROM finance.insurance WHERE PolicyHolderId IN @PolicyHolderIds";
                    var insurances = await connection.QueryAsync<InsuranceResponse>(insuranceQuery, new { PolicyHolderIds = accountIds });

                    decimal monthlyCost = 0;
                    decimal quarterCost = 0;
                    decimal yearlyCost = 0;
                    decimal NextMonthCosts = 0;
                    foreach (InsuranceResponse insurance in insurances)
                    {
                        // Calculate the interval payments
                        var totalMonths = (insurance.DateClosed.HasValue ?
                                            (insurance.DateClosed.Value - insurance.DateOpened).TotalDays :
                                            (DateTime.Now - insurance.DateOpened).TotalDays) / 30;
                        if (totalMonths == 0) totalMonths = 1; // To avoid division by zero

                        var monthlyPayment = Math.Round(insurance.PaymentInstalmentAmount / (decimal)totalMonths, 2);
                        var quarterPayment = Math.Round(monthlyPayment * 3, 2);
                        var yearlyPayment = Math.Round(monthlyPayment * 12, 2);

                        // Determine costs for the next month
                        var costsNextMonth = insurance.InsuranceState && (insurance.DateClosed == null || insurance.DateClosed > DateTime.Now)
                            ? (decimal)insurance.PaymentInstalmentAmount
                            : 0;
                        if(insurance.PaymentUnitCurrency != currency)
                        {
                            monthlyCost = (decimal)CurrencyConverter.Convert(currency,insurance.PaymentInstalmentUnitCurrency,(double)monthlyCost);
                            quarterCost = (decimal)CurrencyConverter.Convert(currency,insurance.PaymentInstalmentUnitCurrency,(double)quarterCost);
                            yearlyCost = (decimal)CurrencyConverter.Convert(currency,insurance.PaymentInstalmentUnitCurrency,(double)yearlyCost);
                            NextMonthCosts = (decimal)CurrencyConverter.Convert(currency,insurance.PaymentInstalmentUnitCurrency,(double)NextMonthCosts);
                        }
                        monthlyCost += monthlyCost + monthlyPayment;
                        quarterCost += quarterCost + quarterPayment;
                        yearlyCost += yearlyCost + yearlyPayment;
                        NextMonthCosts += NextMonthCosts + costsNextMonth;
                    }
                    var retVal = new
                    {
                        monthlyPayment = monthlyCost,
                        quarterlyPayment = quarterCost,
                        yearlyPayment = yearlyCost,
                        costsNextMonth = NextMonthCosts
                    };
                    _logger.LogInformation("All insurance details successfully retrieved.");
                    return Ok(retVal);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving insurance.");
                return new StatusCodeResult(500);
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
    public byte[]? Polizze { get; set; }
    public string InsuranceCompany { get; set; }
    public string Description { get; set; }
    public string Country { get; set; }
    public string Frequency { get; set; }
}
public class InsuranceResponse
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
    public byte[]? Polizze { get; set; }
    public string InsuranceCompany { get; set; }
    public string Description { get; set; }
    public string Country { get; set; }
    public string Frequency { get; set; }
    public DateTime nextPayment { get; set; }
    
}
public class InsuranceModelRequest
{
    public string PolicyHolderId { get; set; }
    public string InsuranceType { get; set; }
    public decimal PaymentInstalmentAmount { get; set; }
    public string PaymentInstalmentUnitCurrency { get; set; }
    public DateTime DateOpened { get; set; }
    public DateTime? DateClosed { get; set; }
    public bool InsuranceState { get; set; }
    public decimal PaymentAmount { get; set; }
    public byte[] Polizze { get; set; } = null!;
    public string PaymentUnitCurrency { get; set; }
    public string InsuranceCompany { get; set; }
    public string Description { get; set; }
    public string Country { get; set; }
    public string Frequency { get; set; }
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
    public string intervall { get; set; }
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
        if (newCurrency == "EUR" && oldCurrency == "USD")
        {
            return ConvertUsdToEur(Value);
        }
        else if (newCurrency == "USD" && oldCurrency == "EUR")
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