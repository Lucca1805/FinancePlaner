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
using FinanceLiquidityManager.Handler.File;
using Microsoft.VisualBasic.FileIO;

namespace FinanceLiquidityManager.Handler.Insurance
{

    public class InsuranceHandler : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly ILogger<InsuranceController> _logger;
        private readonly FileHandler _file;
        public InsuranceHandler(FileHandler file, IConfiguration configuration, ILogger<InsuranceController> logger)
        {
            _file = file;
            _configuration = configuration;
            _logger = logger;
            var host = _configuration["DBHOST"] ?? "localhost";
            var port = _configuration["DBPORT"] ?? "3306";
            var password = _configuration["MYSQL_PASSWORD"] ?? _configuration.GetConnectionString("MYSQL_PASSWORD");
            var userid = _configuration["MYSQL_USER"] ?? _configuration.GetConnectionString("MYSQL_USER");
            var usersDataBase = _configuration["MYSQL_DATABASE"] ?? _configuration.GetConnectionString("MYSQL_DATABASE");

            connectionString = $"server={host}; userid={userid};pwd={password};port={port};database={usersDataBase}";

        }

        public async Task<ActionResult> AddInsurance(string userId, [FromBody] InsuranceModelRequest newInsurance)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID is missing.");
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

                    // Initialize FileRequests property if it's null
                    if (newInsurance.FileRequests == null)
                    {
                        newInsurance.FileRequests = new List<FileRequest>();
                    }

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert the insurance record and retrieve the generated InsuranceId
                            string queryInsurance = @"
                        INSERT INTO finance.insurance (
                            PolicyHolderId, 
                            InsuranceType, 
                            DateOpened, 
                            DateClosed, 
                            InsuranceState, 
                            PaymentAmount, 
                            PaymentUnitCurrency, 
                            InsuranceCompany, 
                            Description, 
                            Frequency, 
                            AdditionalInformation,
                            Country
                        ) VALUES (
                            @PolicyHolderId, 
                            @InsuranceType, 
                            @DateOpened, 
                            @DateClosed, 
                            @InsuranceState, 
                            @PaymentAmount, 
                            @PaymentUnitCurrency, 
                            @InsuranceCompany, 
                            @Description, 
                            @Frequency, 
                            @AdditionalInformation,
                            @Country
                        );
                        SELECT LAST_INSERT_ID();
                    ";

                            var parameters = new
                            {
                                PolicyHolderId = newInsurance.PolicyHolderId,
                                InsuranceType = newInsurance.Name,
                                DateOpened = newInsurance.StartDate,
                                DateClosed = (DateTime?)null,
                                InsuranceState = !newInsurance.IsPaused,
                                PaymentAmount = newInsurance.Payment,
                                PaymentUnitCurrency = newInsurance.PaymentUnitCurrency,
                                InsuranceCompany = newInsurance.InsuranceCompany,
                                Description = newInsurance.AdditionalInformation,
                                Frequency = newInsurance.PaymentRate,
                                AdditionalInformation = newInsurance.AdditionalInformation,
                                Country = newInsurance.Country
                            };

                            // Execute query to insert insurance record and get insuranceId
                            var insuranceId = await connection.QuerySingleAsync<int>(queryInsurance, parameters, transaction: transaction);

                            // Log the generated insuranceId
                            Console.WriteLine($"Generated InsuranceId: {insuranceId}");

                            // Handle file uploads if FileRequests is provided and contains valid data
                            foreach (var fileRequest in newInsurance.FileRequests)
                            {
                                // Check if the necessary fields are present in the FileRequest object
                                if (fileRequest == null || fileRequest.FileInfo == null || fileRequest.FileInfo.Length == 0 || string.IsNullOrEmpty(fileRequest.FileType))
                                {
                                    transaction.Rollback();
                                    return BadRequest("Invalid file request data.");
                                }

                                // Get and sanitize the file name (if available in FileRequest)
                                string originalFileName = string.IsNullOrEmpty(fileRequest.FileName) ? "Unknown_File" : fileRequest.FileName;

                                Console.WriteLine($"Original file name: {originalFileName}");

                                // Insert the file data
                                var insertFileQuery = @"
                            INSERT INTO files (FileInfo, FileType, FileName, RefID) 
                            VALUES (@FileInfo, @FileType, @FileName, @RefID);
                            SELECT LAST_INSERT_ID();
                        ";

                                var fileId = await connection.QuerySingleAsync<int>(insertFileQuery, new
                                {
                                    FileInfo = fileRequest.FileInfo,
                                    FileType = fileRequest.FileType,
                                    FileName = originalFileName,
                                    RefID = insuranceId // Use the insuranceId as RefID for file association
                                }, transaction: transaction);

                                if (fileId <= 0)
                                {
                                    transaction.Rollback();
                                    return BadRequest("Failed to upload one or more files.");
                                }
                            }

                            // Commit transaction after successful file insertions
                            transaction.Commit();

                            return Ok(new { InsuranceId = insuranceId, Message = "Insurance and files added successfully." });
                        }
                        catch (Exception ex)
                        {
                            // Rollback the transaction in case of any failure
                            transaction.Rollback();
                            // Log the exception details (ex.Message) for debugging purposes.
                            Console.WriteLine($"Exception: {ex.Message}");
                            return StatusCode(500, $"Unable To Process Request: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception details (ex.Message) for debugging purposes.
                Console.WriteLine($"Exception: {ex.Message}");
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

                    foreach (InsuranceModel ins in insurance)
                    {
                        if (ins.PaymentUnitCurrency != currency)
                        {

                            decimal convertedAmount = await ConvertCurrency(ins.PaymentAmount, ins.PaymentUnitCurrency, currency);
                            ins.PaymentAmount = Math.Round(convertedAmount,2);
                            ins.PaymentUnitCurrency = currency;
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
                        if (insurance.PaymentUnitCurrency != currency)
                        {
                            decimal convertedAmount = await ConvertCurrency(insurance.PaymentAmount, insurance.PaymentUnitCurrency, currency);
                            insurance.PaymentAmount = Math.Round(convertedAmount,2);
                            insurance.PaymentUnitCurrency = currency;
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
                    DateOpened = @DateOpened, 
                    DateClosed = @DateClosed,
                    InsuranceState = @InsuranceState, 
                    PaymentAmount = @PaymentAmount, 
                    PaymentUnitCurrency = @PaymentUnitCurrency, 
                    InsuranceCompany = @InsuranceCompany, 
                    Description = @Description, 
                    Country = @Country,
                    Frequency = @Frequency,
                    AdditionalInformation = @AdditionalInformation
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
                    string insuranceaccountQuery = @"SELECT InsuranceType as type, PaymentAmount as Cost, PaymentUnitCurrency as Currency, Frequency as intervall  FROM finance.insurance WHERE PolicyHolderId = @AccountId And InsuranceState = 1";
                    var insuranceContent = (await connection.QueryAsync<InsuranceQueryModel>(insuranceaccountQuery, new { AccountId = accountId })).ToList();

                    foreach (InsuranceQueryModel content in insuranceContent)
                    {
                        if (currency != content.Currency)
                        {
                            decimal convertedAmount = await ConvertCurrency((decimal)content.Cost, content.Currency, currency);
                            content.Cost =(double) Math.Round(convertedAmount,2);
                            content.Currency = currency;
                        }
                        // Get the current date
                        DateTime currentDate = DateTime.Now;

                        // Extract the month component from the current date
                        int currentMonth = currentDate.Month + 1;
                        if (content.intervall == "Monthly")
                        {
                            for (int monthCounter = 1; monthCounter < currentMonth; monthCounter++)
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
                            // Calculate the number of quarters until the current month
                            int numberOfQuarters = (currentMonth - 1) / 3 + 1;

                            for (int quarterCounter = 1; quarterCounter <= numberOfQuarters; quarterCounter++)
                            {
                                int startingMonthOfQuarter = (quarterCounter - 1) * 3 + 1;
                                int endingMonthOfQuarter = quarterCounter * 3;

                                if (currentMonth >= startingMonthOfQuarter)
                                {
                                    double adjustedCost = content.Cost / 3; // Divide the yearly cost by 4 quarters

                                    response.InsuranceCosts.Add(
                                        new InsuranceCost
                                        {
                                            Cost = Math.Round(adjustedCost, 2),
                                            Currency = currency,
                                            Insurance = content.Type,
                                            Month = $"Q{quarterCounter}"
                                        }
                                    );
                                }
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

                    string insuranceQuery = @"SELECT * FROM finance.insurance WHERE PolicyHolderId IN @PolicyHolderIds AND InsuranceState = 1";
                    var insurances = await connection.QueryAsync<InsuranceResponse>(insuranceQuery, new { PolicyHolderIds = accountIds });

                    decimal monthlyCost = 0;
                    decimal quarterCost = 0;
                    decimal yearlyCost = 0;
                    decimal NextMonthCosts = 0;
                    decimal monthlyPayment = 0;
                    decimal quarterPayment = 0;
                    decimal yearlyPayment = 0;
                    foreach (InsuranceResponse insurance in insurances)
                    {
                        if (insurance.Frequency == "Monthly")
                        {
                            monthlyPayment = Math.Round(insurance.PaymentAmount);
                        }
                        else if (insurance.Frequency == "Quaterly")
                        {
                            quarterPayment = Math.Round(insurance.PaymentAmount);
                        }
                        else
                        {
                            yearlyPayment = Math.Round(insurance.PaymentAmount);
                        }
                        // Calculate the interval payments
                        /* var totalMonths = (insurance.DateClosed.HasValue ?
                                             (insurance.DateClosed.Value - insurance.DateOpened).TotalDays :
                                             (DateTime.Now - insurance.DateOpened).TotalDays) / 30;
                         if (totalMonths == 0) totalMonths = 1; // To avoid division by zero

                         var monthlyPayment = Math.Round(insurance.PaymentAmount / (decimal)totalMonths, 2);
                         var quarterPayment = Math.Round(monthlyPayment * 3, 2);
                         var yearlyPayment = Math.Round(monthlyPayment * 12, 2);
 */
                        // Determine costs for the next month
                        var costsNextMonth = monthlyPayment + (quarterPayment / 4) + (yearlyPayment / 12);
                        if (insurance.PaymentUnitCurrency != currency)
                        {
                            decimal convertedMonthlyAmount = await ConvertCurrency(monthlyPayment, insurance.PaymentUnitCurrency, currency);
                            monthlyCost = Math.Round(convertedMonthlyAmount,2);
                            decimal convertedQuaterlyCost = await ConvertCurrency(quarterPayment, insurance.PaymentUnitCurrency, currency);
                            quarterCost = Math.Round(convertedQuaterlyCost,2);
                            decimal convertedYearlyCost = await ConvertCurrency(yearlyPayment, insurance.PaymentUnitCurrency, currency);
                            yearlyCost = Math.Round(convertedYearlyCost,2);
                            decimal convertedNextMonthCost = await ConvertCurrency(costsNextMonth, insurance.PaymentUnitCurrency, currency);
                            NextMonthCosts = Math.Round(convertedNextMonthCost,2);
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
}

public class InsuranceModel
{
    public int InsuranceId { get; set; }
    public string PolicyHolderId { get; set; }
    public string InsuranceType { get; set; }
    public DateTime DateOpened { get; set; }
    public DateTime? DateClosed { get; set; }
    public bool InsuranceState { get; set; }
    public decimal PaymentAmount { get; set; }
    public string PaymentUnitCurrency { get; set; }
    public string InsuranceCompany { get; set; }
    public string Description { get; set; }
    public string Country { get; set; }
    public string Frequency { get; set; }
    public string AdditionalInformation { get; set; }
}
public class InsuranceResponse
{
    public int InsuranceId { get; set; }
    public string PolicyHolderId { get; set; }
    public string InsuranceType { get; set; }
    public DateTime DateOpened { get; set; }
    public DateTime? DateClosed { get; set; }
    public bool InsuranceState { get; set; }
    public decimal PaymentAmount { get; set; }
    public string PaymentUnitCurrency { get; set; }
    public string InsuranceCompany { get; set; }
    public string Description { get; set; }
    public string Country { get; set; }
    public string Frequency { get; set; }
    public string AdditionalInformation { get; set; }
    public DateTime nextPayment { get; set; }

}
public class InsuranceModelRequest
{

    public int Id { get; set; }
    public string Iban { get; set; }
    public string InsuranceCompany { get; set; }
    public string Name { get; set; }
    public string PaymentRate { get; set; }
    public decimal Payment { get; set; }
    public string PaymentUnitCurrency { get; set; }
    public DateTime StartDate { get; set; }
    public bool IsPaused { get; set; }
    public string AdditionalInformation { get; set; }
    public DateTime NextPayment { get; set; }
    public string InsuranceType { get; set; }
    public string PolicyHolderId { get; set; }
    public string Country { get; set; }
    public List<FileRequest> FileRequests { get; set; } = new List<FileRequest>();


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
/*
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
*/
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